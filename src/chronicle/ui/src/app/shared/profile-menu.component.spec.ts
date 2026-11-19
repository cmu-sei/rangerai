import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Me, MeService } from '../core/me.service';
import { ProfileMenuComponent } from './profile-menu.component';

async function render(me: Me | null) {
  TestBed.configureTestingModule({ providers: [{ provide: MeService, useValue: { me: signal(me) } }] });
  const fixture = TestBed.createComponent(ProfileMenuComponent);
  document.body.appendChild(fixture.nativeElement);
  await fixture.whenStable();
  return fixture.nativeElement as HTMLElement;
}

const settle = () => TestBed.inject(ApplicationRef).whenStable();

describe('ProfileMenuComponent', () => {
  afterEach(() => document.body.querySelectorAll('app-profile-menu, div[ng-version]').forEach(e => e.remove()));

  it('is a pill with the initial and name, and opens to light or dark mode and Sign out', async () => {
    const root = await render({ user: 'admin', signedIn: true });
    const pill = root.querySelector<HTMLButtonElement>('button.pill')!;
    expect(pill.querySelector('.avatar')!.textContent?.trim()).toBe('a');
    expect(pill.querySelector('.name')!.textContent).toBe('admin');
    expect(root.querySelector('[role="menu"]')).toBeNull();

    pill.click();
    await settle();
    expect(pill.getAttribute('aria-expanded')).toBe('true');
    expect(root.querySelector('.who')!.textContent).toContain('admin');
    const items = [...root.querySelectorAll('[role="menuitem"]')].map(i => i.textContent?.trim());
    expect(items).toEqual([expect.stringMatching(/^(Dark|Light) mode$/), 'Sign out']);
    const form = root.querySelector('form')!;
    expect(form.getAttribute('method')).toBe('post');
    expect(form.getAttribute('action')).toBe('/api/auth/logout');
  });

  it('switches between light and dark from the menu', async () => {
    localStorage.setItem('ch-theme', 'light');
    const root = await render({ user: 'admin', signedIn: true });
    root.querySelector<HTMLButtonElement>('button.pill')!.click();
    await settle();
    const theme = root.querySelector<HTMLButtonElement>('.item.theme')!;
    expect(theme.textContent?.trim()).toBe('Dark mode');
    theme.click();
    await settle();
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(theme.textContent?.trim()).toBe('Light mode');
    localStorage.removeItem('ch-theme');
  });

  it('offers no Sign out without a Keycloak login, and shows a person in place of an initial', async () => {
    const root = await render({ user: null, signedIn: false });
    expect(root.querySelector('.avatar svg')).not.toBeNull();
    expect(root.querySelector('.name')).toBeNull();
    root.querySelector<HTMLButtonElement>('button.pill')!.click();
    await settle();
    expect(root.querySelector('.who')).toBeNull();
    expect([...root.querySelectorAll('[role="menuitem"]')].map(i => i.textContent?.trim())).toEqual([expect.stringMatching(/mode$/)]);
  });

  it('closes on Escape and on a click outside', async () => {
    const root = await render({ user: 'admin', signedIn: true });
    const pill = root.querySelector<HTMLButtonElement>('button.pill')!;
    pill.click();
    await settle();
    root.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await settle();
    expect(root.querySelector('[role="menu"]')).toBeNull();

    pill.click();
    await settle();
    document.body.click();
    await settle();
    expect(root.querySelector('[role="menu"]')).toBeNull();
  });
});
