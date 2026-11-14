import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ThemeService } from '../core/theme.service';

/** The product, top left, as the Scenario Builder's bar does, and light or dark on the right. */
@Component({
  selector: 'app-top-bar',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="bar">
      <a class="brand" routerLink="/"><span class="mark" aria-hidden="true"></span>Chronicle</a>
      <button type="button" class="theme" [attr.aria-label]="themeLabel()" [title]="themeLabel()" (click)="theme.toggle()">
        @if (theme.theme() === 'dark') {
          <svg viewBox="0 0 16 16" aria-hidden="true"><circle cx="8" cy="8" r="3" /><path d="M8 1.5v1.6M8 12.9v1.6M1.5 8h1.6M12.9 8h1.6M3.4 3.4l1.1 1.1M11.5 11.5l1.1 1.1M3.4 12.6l1.1-1.1M11.5 4.5l1.1-1.1" /></svg>
        } @else {
          <svg viewBox="0 0 16 16" aria-hidden="true"><path d="M13.5 9.6A5.6 5.6 0 0 1 6.4 2.5a5.6 5.6 0 1 0 7.1 7.1Z" /></svg>
        }
      </button>
    </header>`,
  styles: [`
    :host { display: block; flex: none; }
    .bar {
      display: flex; align-items: center; gap: 14px; height: 48px; padding: 0 16px;
      background: var(--ch-surface); border-bottom: 1px solid var(--ch-line);
    }
    .brand { display: flex; align-items: center; gap: 8px; font-weight: 600; color: var(--ch-ink); text-decoration: none; }
    .mark { width: 14px; height: 14px; border-radius: 4px; background: var(--ch-coral); }
    .theme {
      display: grid; place-items: center; width: 28px; height: 28px; margin-left: auto; padding: 0;
      border: 1px solid var(--ch-line); border-radius: 50%; background: var(--ch-surface); color: var(--ch-muted); cursor: pointer;
      transition: border-color 120ms, color 120ms, background 120ms;
      &:hover { border-color: var(--ch-coral); color: var(--ch-coral); background: var(--ch-coral-soft); }
      svg { width: 14px; height: 14px; fill: none; stroke: currentColor; stroke-width: 1.5; stroke-linecap: round; stroke-linejoin: round; }
    }
  `]
})
export class TopBarComponent {
  protected readonly theme = inject(ThemeService);
  protected readonly themeLabel = computed(() => (this.theme.theme() === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'));
}
