import { TestBed } from '@angular/core/testing';
import { THEME_KEY, ThemeService } from './theme.service';

describe('ThemeService', () => {
  const root = document.documentElement;
  beforeEach(() => {
    localStorage.removeItem(THEME_KEY);
    delete root.dataset['theme'];
  });
  afterEach(() => localStorage.removeItem(THEME_KEY));

  it('starts from a stored choice and marks <html> with it', () => {
    localStorage.setItem(THEME_KEY, 'dark');
    const theme = TestBed.inject(ThemeService);
    expect(theme.theme()).toBe('dark');
    expect(root.dataset['theme']).toBe('dark');
  });

  it('toggles, and keeps the choice', () => {
    localStorage.setItem(THEME_KEY, 'light');
    const theme = TestBed.inject(ThemeService);
    theme.toggle();
    expect(theme.theme()).toBe('dark');
    expect(root.dataset['theme']).toBe('dark');
    expect(localStorage.getItem(THEME_KEY)).toBe('dark');
    theme.toggle();
    expect(root.dataset['theme']).toBe('light');
    expect(localStorage.getItem(THEME_KEY)).toBe('light');
  });

  it('ignores a stored value it does not know', () => {
    localStorage.setItem(THEME_KEY, 'sepia');
    expect(['light', 'dark']).toContain(TestBed.inject(ThemeService).theme());
  });
});
