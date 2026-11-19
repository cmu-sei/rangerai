import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

/** Admin's sections, in the top bar of every Admin page, styled like a scenario's page links. */
@Component({
  selector: 'app-admin-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="pages" aria-label="Admin">
      <a routerLink="/admin/scenario-types" routerLinkActive="active" ariaCurrentWhenActive="page" data-label="Scenario Types">Scenario Types</a>
      <a routerLink="/admin/libraries" routerLinkActive="active" ariaCurrentWhenActive="page" data-label="Template Libraries">Template Libraries</a>
    </nav>`,
  styles: [`
    :host { display: flex; height: 100%; }
    .pages { display: flex; gap: 4px; height: 100%; }
    a {
      display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 0 12px;
      color: var(--sb-muted); font-weight: 400; line-height: normal; text-decoration: none;
      /* The top border balances the active underline, so the label centres on the bar, as a scenario's page links do. */
      border-top: 2px solid transparent; border-bottom: 2px solid transparent;
      &:hover { color: var(--sb-ink); }
      &.active { color: var(--sb-ink); font-weight: 600; border-bottom-color: var(--sb-person); }
      /* Only the active section is bold; an invisible bold copy of each label holds its width, so the others don't shift. */
      &::after { content: attr(data-label) / ''; height: 0; overflow: hidden; visibility: hidden; font-weight: 600; pointer-events: none; }
    }
  `]
})
export class AdminNavComponent {}
