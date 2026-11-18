import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RangerApp } from '../core/apps.service';
import { AppSwitcherComponent } from './app-switcher.component';

const builder: RangerApp = { id: 'scenario-builder', name: 'Scenario Builder', url: 'http://localhost:4310' };
const chronicle: RangerApp = { id: 'chronicle', name: 'Chronicle', url: 'http://localhost:4320' };
const ghosts: RangerApp = { id: 'ghosts', name: 'GHOSTS' };

async function render() {
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const fixture = TestBed.createComponent(AppSwitcherComponent);
  fixture.componentRef.setInput('self', 'chronicle');
  await fixture.whenStable();
  return { root: fixture.nativeElement as HTMLElement, http: TestBed.inject(HttpTestingController) };
}

const settle = () => TestBed.inject(ApplicationRef).whenStable();
const names = (root: HTMLElement) => [...root.querySelectorAll('.row .name')].map(n => n.textContent?.trim());

describe('AppSwitcherComponent', () => {
  it('lists the other apps that have a URL', async () => {
    const { root, http } = await render();
    http.expectOne('/api/apps').flush([builder, chronicle, ghosts]);
    await settle();
    root.querySelector<HTMLButtonElement>('button.dots')!.click();
    await settle();

    expect(names(root)).toEqual(['Scenario Builder']);
    expect(root.querySelector('a.row')!.getAttribute('href')).toBe('http://localhost:4310');
    expect(root.querySelector('a.row')!.getAttribute('target')).toBe('_blank');
    expect(root.querySelector('a.row')!.getAttribute('rel')).toBe('noopener');
  });

  it('is hidden when no other app has a URL', async () => {
    const { root, http } = await render();
    http.expectOne('/api/apps').flush([chronicle, ghosts]);
    await settle();
    expect(root.querySelector('button.dots')).toBeNull();
  });

  it('closes on Escape, a click opens it again and keeps it open', async () => {
    const { root, http } = await render();
    http.expectOne('/api/apps').flush([builder]);
    await settle();
    const button = root.querySelector<HTMLButtonElement>('button.dots')!;
    button.click();
    await settle();
    expect(button.getAttribute('aria-expanded')).toBe('true');

    root.dispatchEvent(new MouseEvent('mouseleave'));
    await new Promise(r => setTimeout(r, 250));
    await settle();
    expect(root.querySelector('nav.menu')).not.toBeNull();

    button.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await settle();
    expect(root.querySelector('nav.menu')).toBeNull();
  });
});
