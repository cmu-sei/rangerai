import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Me } from '../../../core/models/builder.model';
import { MeService } from '../../../core/services/me.service';
import { TopBarComponent } from './top-bar.component';

async function render(me: Me | null) {
  TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), { provide: MeService, useValue: { me: signal(me) } }] });
  const fixture = TestBed.createComponent(TopBarComponent);
  await fixture.whenStable();
  return fixture.nativeElement as HTMLElement;
}

const adminLink = (root: HTMLElement) => Array.from(root.querySelectorAll('a')).find(a => a.textContent?.trim() === 'Admin');

describe('TopBarComponent', () => {
  it('shows Admin only to admins', async () => {
    expect(adminLink(await render({ user: 'dev', admin: true }))!.getAttribute('href')).toBe('/admin');
    TestBed.resetTestingModule();
    expect(adminLink(await render({ user: 'lee', admin: false }))).toBeUndefined();
    TestBed.resetTestingModule();
    expect(adminLink(await render(null))).toBeUndefined();
  });

  it('ends with Admin, then the Guide, then the person', async () => {
    const root = await render({ user: 'admin', admin: true, signedIn: true });
    const order = [...root.querySelectorAll('.tools > a.admin, .tools > button.guide, .tools > app-profile-menu')].map(e => e.className || e.tagName.toLowerCase());
    expect(order).toEqual(['admin', 'guide', 'app-profile-menu']);
    expect(root.querySelector('header > button.theme')).toBeNull();
  });
});
