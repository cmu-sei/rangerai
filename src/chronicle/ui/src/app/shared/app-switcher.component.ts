import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, computed, inject, input, signal } from '@angular/core';
import { AppsService, RangerApp } from '../core/apps.service';

/**
 * A small grid in front of the product's name that opens the other Ranger apps, on hover or click. Each links to its
 * UI, from the API's appsettings.json; one without a URL there isn't listed. Hidden when there are no others.
 */
@Component({
  selector: 'app-switcher',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(mouseenter)': 'show()', '(mouseleave)': 'hideSoon()', '(focusout)': 'blurred($event)', '(keydown.escape)': 'escape()',
    '(document:click)': 'outside($event)' },
  template: `
    @if (others().length) {
      <button type="button" class="dots" aria-label="Ranger apps" title="Ranger apps" aria-haspopup="true"
        [attr.aria-expanded]="open()" (click)="toggle()">
        <svg viewBox="0 0 12 12" aria-hidden="true">
          @for (y of grid; track y) { @for (x of grid; track x) { <circle [attr.cx]="x" [attr.cy]="y" r="1.1" /> } }
        </svg>
      </button>
      @if (open()) {
        <nav class="menu" aria-label="Ranger apps">
          <div class="title">Ranger apps</div>
          @for (app of others(); track app.id) {
            <a class="row" [href]="app.url!" target="_blank" rel="noopener">
              <span class="badge" aria-hidden="true">{{ initial(app) }}</span>
              <span class="name">{{ app.name }}</span>
            </a>
          }
        </nav>
      }
    }`,
  styles: [`
    :host { position: relative; display: flex; align-items: center; }
    :host(:empty) { display: none; }
    .dots {
      display: grid; place-items: center; width: 22px; height: 22px; padding: 0; margin-left: -4px;
      border: 0; border-radius: 6px; background: none; color: var(--ch-faint); cursor: pointer;
      transition: color 120ms, background 120ms;
      &:hover, &[aria-expanded='true'] { color: var(--ch-coral); background: var(--ch-coral-soft); }
      &:focus-visible { outline: 2px solid var(--ch-coral); outline-offset: 1px; }
      svg { width: 12px; height: 12px; fill: currentColor; }
    }
    .menu {
      position: absolute; top: calc(100% + 6px); left: -4px; z-index: 50; min-width: 220px; padding: 6px;
      background: var(--ch-surface); border: 1px solid var(--ch-line); border-radius: var(--ch-radius);
      box-shadow: 0 12px 32px -12px rgb(var(--ch-shadow) / 0.45);
      /* The gap above is part of the hover target, so moving down to the menu doesn't close it. */
      &::before { content: ''; position: absolute; left: 0; right: 0; top: -8px; height: 8px; }
    }
    .title { padding: 4px 8px 6px; font-size: var(--ch-small); color: var(--ch-muted); }
    .row {
      display: flex; align-items: center; gap: 10px; padding: 6px 8px; border-radius: 6px;
      color: var(--ch-ink); text-decoration: none;
    }
    .row:hover, .row:focus-visible { background: var(--ch-coral-soft); outline: none; }
    .badge {
      display: grid; place-items: center; flex: none; width: 22px; height: 22px; border-radius: 6px;
      background: var(--ch-canvas); border: 1px solid var(--ch-line); font-size: 11px; font-weight: 600; text-transform: uppercase;
    }
    .name { font-weight: 500; white-space: nowrap; }
  `]
})
export class AppSwitcherComponent implements OnDestroy {
  /** This app's id, which the list leaves out. */
  readonly self = input.required<string>();

  private readonly apps = inject(AppsService).apps;
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private timer?: ReturnType<typeof setTimeout>;

  protected readonly grid = [2, 6, 10];
  protected readonly open = signal(false);
  /** Opened by a click, so it stays open when the pointer leaves, until a click outside, Escape or the focus leaving. */
  private pinned = false;
  protected readonly others = computed(() => this.apps().filter(a => a.id !== this.self() && !!a.url));

  protected initial(app: RangerApp): string {
    return app.name.trim()[0] ?? '?';
  }

  protected show(): void {
    clearTimeout(this.timer);
    this.open.set(true);
  }

  protected hideSoon(): void {
    clearTimeout(this.timer);
    if (!this.pinned) this.timer = setTimeout(() => this.open.set(false), 200);
  }

  protected toggle(): void {
    clearTimeout(this.timer);
    this.pinned = !this.pinned;
    this.open.set(this.pinned);
  }

  protected outside(event: MouseEvent): void {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node | null)) this.close();
  }

  protected blurred(event: FocusEvent): void {
    if (event.relatedTarget && !this.host.nativeElement.contains(event.relatedTarget as Node)) this.close();
  }

  protected escape(): void {
    if (!this.open()) return;
    this.close();
    this.host.nativeElement.querySelector<HTMLButtonElement>('button.dots')?.focus();
  }

  private close(): void {
    clearTimeout(this.timer);
    this.pinned = false;
    this.open.set(false);
  }

  ngOnDestroy(): void {
    clearTimeout(this.timer);
  }
}
