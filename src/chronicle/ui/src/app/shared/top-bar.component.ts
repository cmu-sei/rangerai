import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** The product, top left, as the Scenario Builder's bar does. */
@Component({
  selector: 'app-top-bar',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="bar">
      <a class="brand" routerLink="/"><span class="mark" aria-hidden="true"></span>Chronicle</a>
    </header>`,
  styles: [`
    :host { display: block; flex: none; }
    .bar {
      display: flex; align-items: center; gap: 14px; height: 48px; padding: 0 16px;
      background: var(--ch-surface); border-bottom: 1px solid var(--ch-line);
    }
    .brand { display: flex; align-items: center; gap: 8px; font-weight: 600; color: var(--ch-ink); text-decoration: none; }
    .mark { width: 14px; height: 14px; border-radius: 4px; background: var(--ch-coral); }
  `]
})
export class TopBarComponent {}
