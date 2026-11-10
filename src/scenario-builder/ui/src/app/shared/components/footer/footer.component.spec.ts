import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { FooterComponent } from './footer.component';

describe('FooterComponent', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));

  const notices = (root: HTMLElement) => [...root.querySelectorAll('footer span')].map(s => s.textContent?.trim());

  it('carries the standard notice, the API version and a link to the repository, with no GHOSTS name', async () => {
    const fixture = TestBed.createComponent(FooterComponent);
    TestBed.inject(HttpTestingController).expectOne('/api/version').flush({ version: '0.1.0' });
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;

    expect(notices(root)).toEqual(['Ranger Scenario Builder. v0.1.0.', 'CERT © 2022. All rights reserved.']);
    const links = [...root.querySelectorAll('a')];
    expect(links.map(a => a.getAttribute('href'))).toEqual(['https://github.com/cmu-sei/rangerai']);
    expect(links[0].getAttribute('aria-label')).toBe('Ranger on GitHub');
    expect(links[0].querySelector('svg')).not.toBeNull();
  });

  it('leaves the version off when the API can\'t say', async () => {
    const fixture = TestBed.createComponent(FooterComponent);
    TestBed.inject(HttpTestingController).expectOne('/api/version').flush('down', { status: 502, statusText: 'Bad Gateway' });
    await fixture.whenStable();

    expect(notices(fixture.nativeElement)[0]).toBe('Ranger Scenario Builder.');
  });
});
