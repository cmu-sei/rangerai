import { ChangeDetectionStrategy, Component, ElementRef, Injector, afterNextRender, computed, inject, signal } from '@angular/core';
import { MeService } from '../core/me.service';
import { ThemeService } from '../core/theme.service';

/**
 * The person's pill at the end of the top bar: an initial and their name, opening on click to light or dark mode and,
 * signed in with Keycloak, Sign out. Arrow keys move through the menu; Escape, a click outside or the focus leaving
 * closes it.
 */
@Component({
  selector: 'app-profile-menu',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(focusout)': 'blurred($event)', '(keydown)': 'key($event)', '(document:click)': 'outside($event)' },
  template: `
    <button type="button" class="pill" aria-haspopup="menu" [attr.aria-expanded]="open()" [attr.aria-label]="label()" [title]="label()"
      (click)="toggle()">
      <span class="avatar" aria-hidden="true">
        @if (user()) { {{ initial() }} } @else {
          <svg viewBox="0 0 16 16"><circle cx="8" cy="5.5" r="2.6" /><path d="M3 13.5c.6-2.6 2.6-4 5-4s4.4 1.4 5 4" /></svg>
        }
      </span>
      @if (user()) { <span class="name">{{ user() }}</span> }
      <svg class="chevron" viewBox="0 0 10 10" aria-hidden="true"><path d="M2.5 4l2.5 2.5L7.5 4" /></svg>
    </button>
    @if (open()) {
      <div class="menu" role="menu" [attr.aria-label]="label()">
        @if (signedIn()) {
          <div class="who" role="presentation">Signed in as <strong>{{ user() }}</strong></div>
        }
        <button type="button" class="item theme" role="menuitem" (click)="theme.toggle()">
          @if (theme.theme() === 'dark') {
            <svg viewBox="0 0 16 16" aria-hidden="true"><circle cx="8" cy="8" r="3" /><path d="M8 1.5v1.6M8 12.9v1.6M1.5 8h1.6M12.9 8h1.6M3.4 3.4l1.1 1.1M11.5 11.5l1.1 1.1M3.4 12.6l1.1-1.1M11.5 4.5l1.1-1.1" /></svg>
            Light mode
          } @else {
            <svg viewBox="0 0 16 16" aria-hidden="true"><path d="M13.5 9.6A5.6 5.6 0 0 1 6.4 2.5a5.6 5.6 0 1 0 7.1 7.1Z" /></svg>
            Dark mode
          }
        </button>
        @if (signedIn()) {
          <form method="post" action="/api/auth/logout" role="presentation">
            <button type="submit" class="item sign-out" role="menuitem">
              <svg viewBox="0 0 16 16" aria-hidden="true"><path d="M6 2.5H3.5v11H6M10.5 5l3 3-3 3M13.5 8H6.5" /></svg>
              Sign out
            </button>
          </form>
        }
      </div>
    }`,
  styles: [`
    :host { position: relative; display: flex; align-items: center; }
    .pill {
      display: flex; align-items: center; gap: 6px; height: 28px; padding: 0 8px 0 3px;
      border: 1px solid var(--ch-line); border-radius: 999px; background: var(--ch-surface);
      font: 500 var(--ch-small) var(--ch-font); color: var(--ch-muted); cursor: pointer;
      transition: border-color 120ms, color 120ms, background 120ms;
      &:hover, &[aria-expanded='true'] { border-color: var(--ch-coral); color: var(--ch-coral); background: var(--ch-coral-soft); }
      &:focus-visible { outline: 2px solid var(--ch-coral); outline-offset: 1px; }
    }
    .avatar {
      display: grid; place-items: center; width: 20px; height: 20px; border-radius: 50%;
      background: var(--ch-coral); color: var(--ch-on-accent);
      font: 600 11px/1 var(--ch-font); text-transform: uppercase;
      svg { width: 12px; height: 12px; fill: none; stroke: currentColor; stroke-width: 1.5; stroke-linecap: round; }
    }
    .name { max-width: 16ch; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .chevron { width: 10px; height: 10px; fill: none; stroke: currentColor; stroke-width: 1.4; stroke-linecap: round; stroke-linejoin: round; }
    .menu {
      position: absolute; top: calc(100% + 6px); right: 0; z-index: 50; min-width: 200px; padding: 6px;
      background: var(--ch-surface); border: 1px solid var(--ch-line); border-radius: var(--ch-radius);
      box-shadow: 0 12px 32px -12px rgb(var(--ch-shadow) / 0.45);
    }
    .who {
      padding: 4px 8px 8px; margin-bottom: 4px; border-bottom: 1px solid var(--ch-line);
      font-size: var(--ch-small); color: var(--ch-muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
      strong { color: var(--ch-ink); font-weight: 600; }
    }
    form { margin: 0; }
    .item {
      display: flex; align-items: center; gap: 10px; width: 100%; padding: 6px 8px; border: 0; border-radius: 6px;
      background: none; font: 500 var(--ch-text) var(--ch-font); color: var(--ch-ink); text-align: left; cursor: pointer;
      &:hover, &:focus-visible { background: var(--ch-coral-soft); outline: none; }
      svg { flex: none; width: 14px; height: 14px; fill: none; stroke: currentColor; stroke-width: 1.5; stroke-linecap: round; stroke-linejoin: round; color: var(--ch-muted); }
    }
  `]
})
export class ProfileMenuComponent {
  private readonly me = inject(MeService).me;
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  protected readonly theme = inject(ThemeService);

  protected readonly open = signal(false);
  protected readonly user = computed(() => this.me()?.user ?? null);
  protected readonly signedIn = computed(() => !!this.me()?.signedIn);
  protected readonly initial = computed(() => this.user()?.trim()[0] ?? '');
  protected readonly label = computed(() => (this.user() ? `Account: ${this.user()}` : 'Account'));

  protected toggle(): void {
    this.open.update(open => !open);
  }

  protected outside(event: MouseEvent): void {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node | null)) this.open.set(false);
  }

  protected blurred(event: FocusEvent): void {
    if (event.relatedTarget && !this.host.nativeElement.contains(event.relatedTarget as Node)) this.open.set(false);
  }

  protected key(event: KeyboardEvent): void {
    if (event.key === 'Escape' && this.open()) {
      this.open.set(false);
      this.host.nativeElement.querySelector<HTMLButtonElement>('button.pill')?.focus();
      return;
    }
    if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
    event.preventDefault();
    const step = event.key === 'ArrowDown' ? 1 : -1;
    const move = () => {
      const items = [...this.host.nativeElement.querySelectorAll<HTMLButtonElement>('[role="menuitem"]')];
      if (!items.length) return;
      const at = items.indexOf(document.activeElement as HTMLButtonElement);
      items[at < 0 ? (step > 0 ? 0 : items.length - 1) : (at + step + items.length) % items.length].focus();
    };
    if (this.open()) {
      move();
    } else {
      // Opened from the keyboard: its items exist once it has rendered.
      this.open.set(true);
      afterNextRender({ read: move }, { injector: this.injector });
    }
  }
}
