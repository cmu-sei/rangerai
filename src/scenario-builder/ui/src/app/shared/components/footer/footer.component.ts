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
      <span class="notice"><a class="repo" href="https://github.com/cmu-sei/rangerai" target="_blank" rel="noopener"
           aria-label="Ranger on GitHub" title="Ranger on GitHub"><svg viewBox="0 0 16 16" aria-hidden="true"><path fill="currentColor" d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z"/></svg></a> CERT © 2022. All rights reserved.</span>
    </footer>`,
  styles: [`
    :host { display: block; flex: none; }
    .footer {
      display: flex; align-items: center; justify-content: space-between; gap: 16px;
      height: 24px; padding: 0 16px;
      border-top: 1px solid var(--sb-line); background: var(--sb-surface);
      font-size: 11px; color: var(--sb-faint);
    }
    .repo { display: inline-flex; vertical-align: -2px; margin-right: 4px; color: inherit; }
    .repo:hover { color: var(--sb-ink); }
    .repo svg { width: 12px; height: 12px; }
    .notice { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  `]
})
export class FooterComponent {
  protected readonly version = inject(VersionService).version;
}
