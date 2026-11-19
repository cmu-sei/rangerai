import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Me, MeService } from '../core/me.service';
import { TopBarComponent } from './top-bar.component';

async function render(me: Me | null) {
  TestBed.configureTestingModule({
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), { provide: MeService, useValue: { me: signal(me) } }]
  });
  const fixture = TestBed.createComponent(TopBarComponent);
  await fixture.whenStable();
  return fixture.nativeElement as HTMLElement;
}

describe('TopBarComponent', () => {
  it('offers Sign out, as a form post, only when signed in with Keycloak', async () => {
    const form = (await render({ user: 'lee', signedIn: true })).querySelector<HTMLFormElement>('form.user')!;
    expect(form.getAttribute('method')).toBe('post');
    expect(form.getAttribute('action')).toBe('/api/auth/logout');
    expect(form.querySelector('.name')!.textContent).toBe('lee');
    TestBed.resetTestingModule();
    expect((await render({ user: null, signedIn: false })).querySelector('form.user')).toBeNull();
    TestBed.resetTestingModule();
    expect((await render(null)).querySelector('form.user')).toBeNull();
  });
});
