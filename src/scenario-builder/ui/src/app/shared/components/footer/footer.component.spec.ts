import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { FooterComponent } from './footer.component';

describe('FooterComponent', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));

  const notices = (root: HTMLElement) => [...root.querySelectorAll('footer span')].map(s => s.textContent?.trim());

  it('carries the standard notice and the API version, with no GHOSTS name or links', async () => {
    const fixture = TestBed.createComponent(FooterComponent);
    TestBed.inject(HttpTestingController).expectOne('/api/version').flush({ version: '0.1.0' });
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;

    expect(notices(root)).toEqual(['Ranger Scenario Builder. v0.1.0.', 'CERT © 2017. All rights reserved.']);
    expect(root.querySelectorAll('a')).toHaveLength(0);
  });

  it('leaves the version off when the API can\'t say', async () => {
    const fixture = TestBed.createComponent(FooterComponent);
    TestBed.inject(HttpTestingController).expectOne('/api/version').flush('down', { status: 502, statusText: 'Bad Gateway' });
    await fixture.whenStable();

    expect(notices(fixture.nativeElement)[0]).toBe('Ranger Scenario Builder.');
  });
});
