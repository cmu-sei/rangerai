import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ChronicleApiService, errorMessage } from '../../core/chronicle-api.service';
import { Audience, ChronicleRecord, Item, PARTS, PartKey } from '../../core/chronicle.model';

/** The API leaves empty lists out, so a part with nothing in it, or a history-less view, arrives absent. */
function complete(record: ChronicleRecord): ChronicleRecord {
  const lists = Object.fromEntries([...PARTS.map(p => p.key), 'history'].map(k => [k, record[k as keyof ChronicleRecord] ?? []]));
  return { ...record, ...lists } as ChronicleRecord;
}

/**
 * One event: its header, its five parts as the chosen audience may see them, and the latest entries. The audience
 * starts at participant, as the API's does, so ground truth shows only when asked for.
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

  protected readonly parts = computed(() => {
    const record = this.record();
    return PARTS.map(p => ({ ...p, items: record ? record[p.key] : [] }));
  });

  /** The newest entries first, a few at a time. */
  protected readonly recent = computed(() => [...(this.record()?.history ?? [])].reverse().slice(0, 12));

  constructor() {
    effect(() => {
      const id = this.id();
      const audience = this.audience();
      this.error.set(null);
      this.api.getChronicle(id, audience).subscribe({
        next: record => this.record.set(complete(record)),
        error: e => {
          this.record.set(null);
          this.error.set(errorMessage(e));
        }
      });
    });
  }

  protected partLabel(key: PartKey): string {
    return PARTS.find(p => p.key === key)?.label ?? key;
  }

  /** The words that best name an item, whatever its kind. */
  protected title(item: Item): string {
    const event = item['event'] as { class?: string } | undefined;
    for (const value of [item['name'], item['title'], item['text'], event?.class, item['type'], item['cve'], item['of']]) {
      if (typeof value === 'string' && value) return value;
    }
    return item.id;
  }

  /** A second line, when the item has one. */
  protected detail(item: Item): string | null {
    for (const value of [item['text'], item['description'], item['recommendation'], item['note'], item['body']]) {
      if (typeof value === 'string' && value && value !== this.title(item)) return value;
    }
    return null;
  }

  /** Scrolls to a part without touching the route. */
  protected jump(key: PartKey): void {
    document.getElementById(key)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  /** camelCase kinds, as words. */
  protected words(kind: string): string {
    return kind.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase();
  }
}
