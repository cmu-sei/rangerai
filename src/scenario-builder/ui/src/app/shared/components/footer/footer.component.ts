import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { VersionService } from '../../../core/services/version.service';

/** The standard footer, made small: the notice and the API's version, on every page. */
@Component({
  selector: 'app-footer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer class="footer">
      <span class="notice">Ranger Scenario Builder.@if (version()) { v{{ version() }}.}</span>
      <span class="notice">CERT © 2017. All rights reserved.</span>
    </footer>`,
  styles: [`
    :host { display: block; flex: none; }
    .footer {
      display: flex; align-items: center; justify-content: space-between; gap: 16px;
      height: 24px; padding: 0 16px;
      border-top: 1px solid var(--sb-line); background: var(--sb-surface);
      font-size: 11px; color: var(--sb-faint);
    }
    .notice { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  `]
})
export class FooterComponent {
  protected readonly version = inject(VersionService).version;
}
