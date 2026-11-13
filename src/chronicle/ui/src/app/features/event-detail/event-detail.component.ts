import { ChangeDetectionStrategy, Component, DestroyRef, WritableSignal, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { ChronicleApiService, errorMessage } from '../../core/chronicle-api.service';
import { Audience, Author, ChronicleRecord, Entry, Item, PARTS, PartKey } from '../../core/chronicle.model';
import { ItemView, appOf, authorLabel, titleOf, viewOf, words } from './item-view';

/** The API leaves empty lists out, so a part with nothing in it, or a history-less view, arrives absent. */
function complete(record: ChronicleRecord): ChronicleRecord {
  const lists = Object.fromEntries([...PARTS.map(p => p.key), 'history'].map(k => [k, record[k as keyof ChronicleRecord] ?? []]));
  return { ...record, ...lists } as ChronicleRecord;
}

const maxSequence = (history: Entry[]) => history.reduce((max, e) => Math.max(max, e.sequence), 0);

/** How long an item that just arrived stays highlighted, in ms. */
const FRESH_MS = 2600;
/** How long new entries are gathered before the record is read again, in ms. */
const DEBOUNCE_MS = 250;

type LiveState = 'connecting' | 'live' | 'reconnecting' | 'off';

interface Chip {
  label: string;
  color: string;
  agent: boolean;
}

interface Row {
  item: Item;
  view: ItemView;
  chip: Chip | null;
  fresh: boolean;
}

/**
 * One event, live: its header, its five parts as the chosen audience may see them, and the entries as they arrive.
 * The page subscribes to the event's entry stream as that audience and reads the record again whenever a newer
 * entry arrives, so what other apps publish appears without a reload. Items and entries that arrive after the
 * first load (or that a wider audience reveals) are highlighted for a moment.
 */
@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './event-detail.component.html',
  styleUrls: ['./event-detail.component.scss']
})
export class EventDetailComponent {
  private readonly api = inject(ChronicleApiService);

  /** The event's id, from the route. */
  readonly id = input.required<string>();

  protected readonly audiences: { key: Audience; label: string }[] = [
    { key: 'participant', label: 'Participant' },
    { key: 'excon', label: 'Exercise control' },
    { key: 'designer', label: 'Designer' }
  ];

  protected readonly audience = signal<Audience>('participant');
  protected readonly record = signal<ChronicleRecord | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly live = signal<LiveState>('connecting');
  /** True while a new audience's view is on its way. */
  protected readonly switching = signal(false);

  /** Item ids and entry sequences that arrived since the first load, until their highlight fades. */
  private readonly freshItems = signal<ReadonlySet<string>>(new Set());
  private readonly freshEntries = signal<ReadonlySet<number>>(new Set());
  /** Parts whose count just changed. */
  protected readonly glowing = signal<ReadonlySet<PartKey>>(new Set());

  /** The latest entry for each item: who wrote what is there now. */
  private readonly authors = computed(() => {
    const map = new Map<string, Author>();
    for (const entry of this.record()?.history ?? []) map.set(entry.itemId, entry.author);
    return map;
  });

  /** Every visible item's title by id, for naming what items refer to. */
  private readonly names = computed(() => {
    const map = new Map<string, string>();
    const record = this.record();
    if (record) for (const p of PARTS) for (const item of record[p.key]) map.set(item.id, titleOf(item));
    return map;
  });

  protected readonly parts = computed(() => {
    const record = this.record();
    const fresh = this.freshItems();
    const authors = this.authors();
    const names = this.names();
    const name = (id: string) => names.get(id) ?? id;
    return PARTS.map(p => {
      const items = record ? record[p.key] : [];
      const rows: Row[] = items.map(item => {
        const author = authors.get(item.id);
        return { item, view: viewOf(item, name), chip: author ? this.chip(author) : null, fresh: fresh.has(item.id) };
      });
      const apps = [...new Map(rows.filter(r => r.chip).map(r => [r.chip!.color + r.chip!.label.split(' · ')[0], r.chip!])).values()];
      return { ...p, items, rows, apps };
    });
  });

  /** Each app that wrote to the record, with how many entries it made, most first. */
  protected readonly contributors = computed(() => {
    const counts = new Map<string, { app: ReturnType<typeof appOf>; name: string; entries: number; agent: boolean }>();
    for (const entry of this.record()?.history ?? []) {
      const app = appOf(entry.author.app);
      const c = counts.get(app.key) ?? { app, name: entry.author.app, entries: 0, agent: false };
      c.entries++;
      c.agent ||= entry.author.via === 'agent';
      counts.set(app.key, c);
    }
    return [...counts.values()].sort((a, b) => b.entries - a.entries);
  });

  /** The newest entries first, a few at a time. */
  protected readonly recent = computed(() => {
    const fresh = this.freshEntries();
    const names = this.names();
    return [...(this.record()?.history ?? [])].reverse().slice(0, 14).map(entry => ({
      entry,
      chip: this.chip(entry.author),
      title: entry.item ? viewOf(entry.item, id => names.get(id) ?? id).title : names.get(entry.itemId) ?? entry.itemId,
      fresh: fresh.has(entry.sequence)
    }));
  });

  private stream?: Subscription;
  private refreshTimer?: ReturnType<typeof setTimeout>;
  private readonly timers = new Set<ReturnType<typeof setTimeout>>();
  /** The highest sequence the stream has told of, and the highest the shown record holds. */
  private heard = 0;
  private shown = 0;
  /** What the last record held, to tell what is new in the next one; null before the first load. */
  private known: { items: Set<string>; entries: Set<number>; counts: Map<PartKey, number> } | null = null;
  private loading?: Subscription;

  constructor() {
    effect(() => {
      const id = this.id();
      const audience = this.audience();
      untracked(() => {
        this.load(id, audience, true);
        this.subscribe(id, audience);
      });
    });
    inject(DestroyRef).onDestroy(() => {
      this.stream?.unsubscribe();
      this.loading?.unsubscribe();
      clearTimeout(this.refreshTimer);
      this.timers.forEach(clearTimeout);
    });
  }

  protected setAudience(audience: Audience): void {
    if (audience === this.audience()) return;
    this.switching.set(true);
    this.audience.set(audience);
  }

  /** Reads the record as the audience sees it, and marks what wasn't there before. */
  private load(id: string, audience: Audience, first = false): void {
    if (first) this.error.set(null);
    this.loading?.unsubscribe();
    this.loading = this.api.getChronicle(id, audience).subscribe({
      next: raw => {
        const record = complete(raw);
        this.mark(record);
        this.record.set(record);
        this.switching.set(false);
        this.shown = maxSequence(record.history);
        if (this.heard > this.shown) this.schedule();
      },
      error: e => {
        this.switching.set(false);
        this.record.set(null);
        this.error.set(errorMessage(e));
      }
    });
  }

  /** Listens for new entries as this audience; any newer than the record shown brings a fresh read. */
  private subscribe(id: string, audience: Audience): void {
    this.stream?.unsubscribe();
    this.heard = 0;
    this.live.set('connecting');
    const stream = this.api.stream?.(id, audience);
    if (!stream) {
      this.live.set('off');
      return;
    }
    this.stream = stream.subscribe(event => {
      if (event.type === 'open') this.live.set('live');
      else if (event.type === 'error') this.live.set('reconnecting');
      else {
        this.live.set('live');
        this.heard = Math.max(this.heard, event.entry.sequence);
        if (this.record() && event.entry.sequence > this.shown) this.schedule();
      }
    });
  }

  private schedule(): void {
    clearTimeout(this.refreshTimer);
    this.refreshTimer = setTimeout(() => this.load(this.id(), this.audience()), DEBOUNCE_MS);
  }

  /** Compares the new record with the last: new items and entries get highlighted, changed counts glow. */
  private mark(record: ChronicleRecord): void {
    const items = new Set(PARTS.flatMap(p => record[p.key].map(i => i.id)));
    const entries = new Set(record.history.map(e => e.sequence));
    const counts = new Map(PARTS.map(p => [p.key, record[p.key].length] as [PartKey, number]));
    // A revise brings no new id, but its entry is new: highlight the item it touched as well.
    const previous = this.known;
    this.known = { items, entries, counts };
    if (!previous) return;

    const newEntries = record.history.filter(e => !previous.entries.has(e.sequence));
    const newItems = new Set([...items].filter(id => !previous.items.has(id)));
    for (const e of newEntries) if (items.has(e.itemId)) newItems.add(e.itemId);
    const changed = PARTS.map(p => p.key).filter(k => counts.get(k) !== previous.counts.get(k));

    this.flash(this.freshItems, newItems);
    this.flash(this.freshEntries, new Set(newEntries.map(e => e.sequence)));
    this.flash(this.glowing, new Set(changed), 1800);
  }

  private flash<T>(target: WritableSignal<ReadonlySet<T>>, added: Set<T>, ms = FRESH_MS): void {
    if (!added.size) return;
    target.update(s => new Set([...s, ...added]));
    const timer = setTimeout(() => {
      this.timers.delete(timer);
      target.update(s => new Set([...s].filter(v => !added.has(v))));
    }, ms);
    this.timers.add(timer);
  }

  protected chip(author: Author): Chip {
    const app = appOf(author.app);
    return { label: authorLabel(author), color: app.color, agent: author.via === 'agent' };
  }

  protected partLabel(key: PartKey): string {
    return PARTS.find(p => p.key === key)?.label ?? key;
  }

  /** Scrolls to a part without touching the route. */
  protected jump(key: PartKey): void {
    document.getElementById(key)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  /** camelCase kinds, as words. */
  protected words(kind: string): string {
    return words(kind);
  }

  protected liveLabel(): string {
    switch (this.live()) {
      case 'live': return 'Live';
      case 'reconnecting': return 'Reconnecting';
      case 'off': return 'Offline';
      default: return 'Connecting';
    }
  }

  protected audienceLabel(): string {
    return this.audiences.find(a => a.key === this.audience())?.label.toLowerCase() ?? this.audience();
  }
}
