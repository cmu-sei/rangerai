import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MeService } from '../../../core/services/me.service';
import { HelpService } from '../../help/help.service';
import { AppSwitcherComponent } from '../app-switcher/app-switcher.component';
import { ProfileMenuComponent } from '../profile-menu/profile-menu.component';

/**
 * The thin bar across the top: the other Ranger apps, the product, where you are and the page's own navigation on the left;
 * Admin (for admins), the Guide and the person's menu on the right.
 */
@Component({
  selector: 'app-top-bar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, AppSwitcherComponent, ProfileMenuComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="bar">
      <app-switcher self="scenario-builder" />
      <a class="brand" routerLink="/">
        <span class="mark" aria-hidden="true"></span>
        Scenario Builder
      </a>
      @if (crumb()) {
        <span class="sep" aria-hidden="true">/</span>
        <span class="crumb">{{ crumb() }}</span>
      }
      <div class="page"><ng-content /></div>
      <div class="tools">
        @if (me()?.admin) {
          <a class="admin" routerLink="/admin" routerLinkActive="active">Admin</a>
        }
        <button type="button" class="guide" data-tour="guide" title="Guide (?)" [attr.aria-expanded]="help.open()" (click)="help.toggle()">
          <span class="glyph" aria-hidden="true">?</span>Guide
        </button>
        <app-profile-menu />
      </div>
    </header>`,
  styles: [`
    :host { display: block; flex: none; }
    .bar {
      display: flex; align-items: center; gap: 10px; height: 48px; padding: 0 16px;
      /* One line height for everything in the bar, so the product, the name, the page's links and the pills share a centre. */
      line-height: normal;
      background: var(--sb-surface); border-bottom: 1px solid var(--sb-line);
    }
    .brand { display: flex; align-items: center; gap: 8px; font-weight: 600; color: var(--sb-ink); text-decoration: none; }
    .mark {
      width: 14px; height: 14px; border-radius: 4px;
      background: linear-gradient(135deg, var(--sb-person) 0 50%, var(--sb-agent) 50% 100%);
    }
    .sep { color: var(--sb-faint); }
    .brand, .sep { flex: none; }
    /* A long scenario name gives way before the page's navigation does. */
    .crumb { min-width: 0; color: var(--sb-muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 40vw; }
    /* Kept clear of Admin on the right, which would otherwise read as one more of the page's links. */
    .page { flex: none; display: flex; align-items: center; height: 100%; margin-right: 14px; }
    .page:empty { display: none; }
    /* Product / where you are / the page's links: the second separator, silent to screen readers as the first is. */
    /* No margin after it: the first link's own padding is the gap. */
    .page::before { content: '/' / ''; color: var(--sb-faint); }
    .tools { flex: none; display: flex; align-items: center; gap: 10px; margin-left: auto; }
    .guide {
      display: flex; align-items: center; gap: 6px; height: 28px; padding: 0 10px 0 5px;
      border: 1px solid var(--sb-line); border-radius: 999px; background: var(--sb-surface);
      font: 500 var(--sb-small) var(--sb-font); color: var(--sb-muted); cursor: pointer;
      transition: border-color 120ms, color 120ms, background 120ms;
      &:hover, &[aria-expanded='true'] { border-color: var(--sb-person); color: var(--sb-person); background: var(--sb-person-soft); }
      &:hover .glyph, &[aria-expanded='true'] .glyph { background: var(--sb-person); color: var(--sb-on-accent); }
    }
    .glyph {
      display: grid; place-items: center; width: 18px; height: 18px; border-radius: 50%;
      background: var(--sb-canvas); font: 600 11px/1 var(--sb-serif); color: var(--sb-ink);
      transition: background 120ms, color 120ms;
    }
    .admin {
      padding: 4px 8px; border-radius: var(--sb-pill); font: 500 var(--sb-small) var(--sb-font);
      color: var(--sb-muted); text-decoration: none;
      &:hover, &.active { color: var(--sb-ink); background: var(--sb-canvas); }
    }
  `]
})
export class TopBarComponent {
  readonly crumb = input<string | null>(null);
  protected readonly me = inject(MeService).me;
  protected readonly help = inject(HelpService);
}
