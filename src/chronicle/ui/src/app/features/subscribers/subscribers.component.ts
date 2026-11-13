import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { ChronicleApiService, errorMessage } from '../../core/chronicle-api.service';
import { Audience, ChronicleRecord, Item, PARTS } from '../../core/chronicle.model';
import { Activity, LiveState, Subscriber } from '../../core/live.model';
import { LiveService } from '../../core/live.service';
import { appOf } from '../event-detail/item-view';

/** One agent run, as the feed tells it: what it read, what it wrote so far, and what it published. */
export interface Run {
  runId: string;
  items?: number;
  total?: number;
  withheld?: number;
  tokens?: number;
  query?: string;
  sample: string[];
  text: string;
  done: boolean;
  failed?: string;
  published?: { sequence: number; part: string; kind: string };
}

/** How each kind of activity is marked in the feed. */
const KINDS: Record<string, { glyph: string; label: string }> = {
  browse: { glyph: '↗', label: 'browse' },
  email: { glyph: '✉', label: 'email' },
  file: { glyph: '▤', label: 'file' },
  chat: { glyph: '◍', label: 'chat' },
  login: { glyph: '⏻', label: 'login' },
  inject: { glyph: '⚑', label: 'inject' },
  'agent.started': { glyph: '✦', label: 'reading' },
  'agent.published': { glyph: '✓', label: 'published' },
  'agent.failed': { glyph: '!', label: 'failed' }
};

const FEED = 80;

/**
 * An event's subscribers, live: each app and agent reading the event through its own view, what each sees and
 * writes as it writes it, and every activity apps report, as it happens, over one WebSocket.
 */
@Component({
  selector: 'app-subscribers',
  standalone: true,
  imports: [DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './subscribers.component.html',
  styleUrls: ['./subscribers.component.scss']
})
export class SubscribersComponent {
  private readonly api = inject(ChronicleApiService);
  private readonly live = inject(LiveService);

  /** The event's id, from the route. */
  readonly id = input.required<string>();

  protected readonly audiences: { key: Audience; label: string }[] = [
    { key: 'participant', label: 'Participant' },
    { key: 'excon', label: 'Exercise control' },
    { key: 'designer', label: 'Designer' }
  ];
  protected readonly audience = signal<Audience>('excon');
  protected readonly state = signal<LiveState>('connecting');
  protected readonly record = signal<ChronicleRecord | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly subscribers = signal<Subscriber[]>([]);
  protected readonly feed = signal<Activity[]>([]);
  protected readonly runs = signal<Record<string, Run>>({});
  /** Activity ids that arrived after the page opened, for the slide-in. */
  protected readonly fresh = signal<Set<string>>(new Set());
  protected readonly starting = signal<Set<string>>(new Set());

  protected readonly agents = computed(() => this.subscribers().filter(s => s.hasAgent));
  protected readonly sources = computed(() => this.subscribers().filter(s => !s.hasAgent));
  protected readonly parts = computed(() => {
    const r = this.record();
    return PARTS.filter(p => p.key !== 'lessons' || (r?.lessons.length ?? 0) > 0).map(p => ({ ...p, count: r ? r[p.key].length : 0 }));
  });
  protected readonly entries = computed(() => this.record()?.history.length ?? 0);
  /** Names for the ids an activity or item mentions: personas, organizations, devices. */
  private readonly names = computed(() => {
    const r = this.record();
    const map = new Map<string, string>();
    for (const p of PARTS) for (const item of r?.[p.key] ?? []) {
      const name = item['name'] ?? item['title'];
      if (typeof name === 'string') map.set(item.id, name);
    }
    return map;
  });

  private socket?: Subscription;
  private refresh?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => {
      const id = this.id();
      const audience = this.audience();
      this.load(id, audience);
      this.connect(id, audience);
    });
    inject(DestroyRef).onDestroy(() => {
      this.socket?.unsubscribe();
      clearTimeout(this.refresh);
    });
  }

  private load(id: string, audience: Audience): void {
    this.api.getChronicle(id, audience).subscribe({
      next: r => {
        this.record.set({ ...r, world: r.world ?? [], intent: r.intent ?? [], asBuilt: r.asBuilt ?? [], asRun: r.asRun ?? [], lessons: r.lessons ?? [], history: r.history ?? [] });
        this.error.set(null);
      },
      error: e => this.error.set(errorMessage(e))
    });
  }

  private connect(id: string, audience: Audience): void {
    this.socket?.unsubscribe();
    this.socket = this.live.connect(id, audience).subscribe(event => {
      if (event.type === 'state') {
        this.state.set(event.state);
        return;
      }
      const m = event.message;
      switch (m.type) {
        case 'hello':
          this.subscribers.set(m.subscribers);
          this.feed.set([...m.activity].reverse().slice(0, FEED));
          this.runs.set({});
          for (const a of m.activity) this.track(a);
          return;
        case 'subscriber':
          this.subscribers.update(list => {
            const at = list.findIndex(s => s.id === m.subscriber.id);
            return at < 0 ? [...list, m.subscriber] : list.map((s, i) => (i === at ? m.subscriber : s));
          });
          return;
        case 'activity':
          this.fresh.update(set => new Set(set).add(m.activity.id));
          this.feed.update(list => [m.activity, ...list].slice(0, FEED));
          this.track(m.activity);
          this.count(m.activity);
          return;
        case 'token':
          this.runs.update(runs => {
            const run = runs[m.subscriberId];
            if (!run || run.runId !== m.runId) return runs;
            return { ...runs, [m.subscriberId]: { ...run, text: clean(run.text + m.text) } };
          });
          return;
        case 'entry':
          clearTimeout(this.refresh);
          this.refresh = setTimeout(() => this.load(this.id(), this.audience()), 250);
          return;
      }
    });
  }

  /** Follows agent runs through the activity they report. */
  private track(a: Activity): void {
    if (!a.subscriberId || !a.kind.startsWith('agent.')) return;
    const data = (a.data ?? {}) as Record<string, unknown>;
    const runId = String(data['runId'] ?? '');
    this.runs.update(runs => {
      const run = runs[a.subscriberId!];
      if (a.kind === 'agent.started')
        return { ...runs, [a.subscriberId!]: {
          runId, items: num(data['items']), total: num(data['total']), withheld: num(data['withheld']), tokens: num(data['tokens']),
          query: String(data['query'] ?? ''), sample: (data['sample'] as string[] | undefined) ?? [], text: '', done: false } };
      const base: Run = run && run.runId === runId ? run : { runId, sample: [], text: '', done: false };
      if (a.kind === 'agent.published')
        return { ...runs, [a.subscriberId!]: { ...base, done: true, text: String(data['text'] ?? base.text),
          published: { sequence: a.sequence ?? 0, part: String(data['part'] ?? ''), kind: String(data['kind'] ?? '') } } };
      if (a.kind === 'agent.failed')
        return { ...runs, [a.subscriberId!]: { ...base, done: true, failed: a.text ?? 'failed' } };
      return runs;
    });
    if (a.kind !== 'agent.started')
      this.starting.update(set => { const next = new Set(set); next.delete(a.subscriberId!); return next; });
  }

  /** Counts an activity against the agentless subscriber that reported it, as the API does. */
  private count(a: Activity): void {
    this.subscribers.update(list => {
      const target = a.subscriberId ? list.find(s => s.id === a.subscriberId) : list.find(s => s.app === a.source && !s.hasAgent);
      if (!target) return list;
      return list.map(s => (s === target ? { ...s, activityCount: (s.activityCount ?? 0) + 1, lastSeen: a.at, lastActivity: a.text ?? a.kind } : s));
    });
  }

  protected start(s: Subscriber): void {
    this.starting.update(set => new Set(set).add(s.id));
    this.live.run(this.id(), s.id).subscribe({
      error: e => {
        this.starting.update(set => { const next = new Set(set); next.delete(s.id); return next; });
        this.error.set(errorMessage(e));
      }
    });
  }

  protected app(name: string) {
    return appOf(name);
  }

  protected letter(s: Subscriber): string {
    return appOf(s.app).label.replace(/[^A-Za-z]/g, '')[0] ?? 'A';
  }

  protected kind(kind: string) {
    return KINDS[kind] ?? { glyph: '•', label: kind };
  }

  protected actor(id?: string): string | null {
    return id ? (this.names().get(id) ?? id) : null;
  }

  /** The subscription's read, with the audience and viewers picked out. */
  protected query(q: string): { text: string; key: boolean }[] {
    return q.split(/(audience=[\w]+|viewers=[\w%.-]+|parts=\w+)/).filter(Boolean).map(text => ({ text, key: /^(audience|viewers|parts)=/.test(text) }));
  }

  protected activityOf(s: Subscriber): number {
    return s.activityCount ?? 0;
  }

  protected isStarting(s: Subscriber): boolean {
    return this.starting().has(s.id);
  }

  protected running(s: Subscriber): boolean {
    const run = this.runs()[s.id];
    return this.isStarting(s) || (!!run && !run.done);
  }

  protected partLabel(key: string): string {
    return PARTS.find(p => p.key === key)?.label ?? key;
  }

  protected modelOf(s: Subscriber): string {
    return (s.model ?? 'model').replace(/-32k$/, '');
  }

  protected sampleOf(item: Item): string {
    return String(item['name'] ?? item['title'] ?? item.id);
  }
}

function num(v: unknown): number | undefined {
  return typeof v === 'number' ? v : undefined;
}

/** Model output without markdown, as the API publishes it. */
export function clean(text: string): string {
  return text.replace(/\*\*/g, '').replace(/^#+\s*/gm, '').replace(/^[ \t]*[*-][ \t]+/gm, '• ');
}
