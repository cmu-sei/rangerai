import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MeService } from '../core/me.service';
import { TopBarComponent } from './top-bar.component';

describe('TopBarComponent', () => {
  it('ends with the person, whose menu holds light or dark and Sign out', async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), { provide: MeService, useValue: { me: signal({ user: 'lee', signedIn: true }) } }]
    });
    const fixture = TestBed.createComponent(TopBarComponent);
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('header > :last-child')!.tagName.toLowerCase()).toBe('app-profile-menu');
    expect(root.querySelector('.name')!.textContent).toBe('lee');
    expect(root.querySelector('header > button.theme')).toBeNull();
  });
});
