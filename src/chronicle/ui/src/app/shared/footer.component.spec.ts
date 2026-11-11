import { TestBed } from '@angular/core/testing';
import { FooterComponent } from './footer.component';

describe('FooterComponent', () => {
  it('carries the standard notice and a link to the repository', async () => {
    const fixture = TestBed.createComponent(FooterComponent);
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;

    expect([...root.querySelectorAll('footer span')].map(s => s.textContent?.trim()))
      .toEqual(['Ranger Chronicle.', 'CERT © 2022. All rights reserved.']);
    const link = root.querySelector('a')!;
    expect(link.getAttribute('href')).toBe('https://github.com/cmu-sei/rangerai');
    expect(link.getAttribute('aria-label')).toBe('Ranger on GitHub');
  });
});
