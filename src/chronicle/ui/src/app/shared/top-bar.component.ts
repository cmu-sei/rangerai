import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AppSwitcherComponent } from './app-switcher.component';
import { ProfileMenuComponent } from './profile-menu.component';

/** The other Ranger apps and the product, top left, and the person's menu (light or dark, Sign out) on the right, as Scenario Builder's bar has. */
@Component({
  selector: 'app-top-bar',
  standalone: true,
  imports: [RouterLink, AppSwitcherComponent, ProfileMenuComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="bar">
      <app-switcher self="chronicle" />
      <a class="brand" routerLink="/"><span class="mark" aria-hidden="true"></span>Chronicle</a>
      <app-profile-menu />
    </header>`,
  styles: [`
    :host { display: block; flex: none; }
    .bar {
      display: flex; align-items: center; gap: 8px; height: 48px; padding: 0 16px;
      background: var(--ch-surface); border-bottom: 1px solid var(--ch-line);
    }
    .brand { display: flex; align-items: center; gap: 8px; font-weight: 600; color: var(--ch-ink); text-decoration: none; }
    .mark { width: 14px; height: 14px; border-radius: 4px; background: var(--ch-coral); }
    app-profile-menu { margin-left: auto; }
  `]
})
export class TopBarComponent {}
