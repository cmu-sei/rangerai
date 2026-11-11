import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FooterComponent } from './shared/footer.component';
import { TopBarComponent } from './shared/top-bar.component';

/** The bar over every page, the routed page, and the footer under it. */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, TopBarComponent, FooterComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<app-top-bar /><router-outlet /><app-footer />`,
  styles: [`
    :host { display: flex; flex-direction: column; height: 100vh; }
    :host > :not(app-top-bar):not(router-outlet):not(app-footer) { flex: 1 1 0; min-height: 0; }
  `]
})
export class App {}
