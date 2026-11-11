import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ChronicleApiService, errorMessage } from '../../core/chronicle-api.service';
import { ChronicleSummary } from '../../core/chronicle.model';

type SortKey = 'name' | 'scenarioType' | 'entries' | 'created';

/** What Chronicle is, then every event it records as a small grid to search, sort and open. */
@Component({
  selector: 'app-events-list',
  standalone: true,
  imports: [DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './events-list.component.html',
  styleUrls: ['./events-list.component.scss']
})
export class EventsListComponent implements OnInit {
  private readonly api = inject(ChronicleApiService);
  private readonly router = inject(Router);

  protected readonly stages = [
    { n: '01', name: 'Imagine', adds: 'World, Intent', by: 'Scenario Builder' },
    { n: '02', name: 'Build', adds: 'As Built', by: 'GraySpace, GHOSTS, Pandora' },
    { n: '03', name: 'Run', adds: 'As Run', by: 'GHOSTS, exercise apps' },
    { n: '04', name: 'Remember', adds: 'Lessons', by: 'After-action review' }
  ];

  protected readonly columns: { key: SortKey; label: string }[] = [
    { key: 'name', label: 'Name' },
    { key: 'scenarioType', label: 'Scenario Type' },
    { key: 'entries', label: 'Entries' },
    { key: 'created', label: 'Created' }
  ];

  protected readonly events = signal<ChronicleSummary[]>([]);
  protected readonly loaded = signal(false);
  protected readonly query = signal('');
  protected readonly sort = signal<{ key: SortKey; ascending: boolean }>({ key: 'created', ascending: false });
  protected readonly error = signal<string | null>(null);

  protected readonly rows = computed(() => {
    const words = this.query().trim().toLowerCase();
    const { key, ascending } = this.sort();
    return this.events()
      .filter(e => !words || [e.name, e.id, e.kind, e.scenarioType?.id ?? ''].some(v => v.toLowerCase().includes(words)))
      .sort((a, b) => (ascending ? 1 : -1) * compare(value(a, key), value(b, key)));
  });

  ngOnInit(): void {
    this.api.getChronicles().subscribe({
      next: events => {
        this.events.set(events);
        this.loaded.set(true);
      },
      error: e => {
        this.error.set(errorMessage(e));
        this.loaded.set(true);
      }
    });
  }

  /** The whole row opens the event; the name is also a real link, for the keyboard and new tabs. */
  protected open(event: ChronicleSummary): void {
    this.router.navigate(['/events', event.id]);
  }

  /** A column sorts ascending first; the same column again reverses it. */
  protected sortBy(key: SortKey): void {
    this.sort.update(sort => ({ key, ascending: sort.key === key ? !sort.ascending : true }));
  }

  protected ariaSort(key: SortKey): string | null {
    const sort = this.sort();
    return sort.key === key ? (sort.ascending ? 'ascending' : 'descending') : null;
  }

  protected searched(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }
}

function value(e: ChronicleSummary, key: SortKey): string | number {
  return key === 'scenarioType' ? (e.scenarioType?.id ?? '') : e[key];
}

function compare(a: string | number, b: string | number): number {
  return typeof a === 'number' && typeof b === 'number'
    ? a - b
    : String(a).localeCompare(String(b), undefined, { sensitivity: 'base' });
}
