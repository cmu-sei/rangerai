import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ChronicleApiService } from '../../core/chronicle-api.service';
import { ChronicleSummary } from '../../core/chronicle.model';
import { EventsListComponent } from './events-list.component';

const event = (id: string, name: string, extra: Partial<ChronicleSummary> = {}): ChronicleSummary => ({
  id, name, kind: 'exercise', scenarioType: { id: 'crucible-tabletop', version: '0.3.0' }, created: '2026-11-02T14:00:00Z', entries: 10, archived: false, ...extra
});

const harbor = event('harbor-ttx', 'Harbor Ransomware Tabletop', { entries: 33, created: '2026-11-02T14:00:00Z' });
const rail = event('rail-sim', 'Rail Freeze', { scenarioType: { id: 'small-unit-team-exercise' }, entries: 5, created: '2026-11-09T14:00:00Z' });

const old = event('old-drill', 'Old Drill', { archived: true, created: '2026-10-01T14:00:00Z' });

async function render(getChronicles = () => of([harbor, rail]), archive = vi.fn()) {
  TestBed.configureTestingModule({
    providers: [provideRouter([]), { provide: ChronicleApiService, useValue: { getChronicles, archive } }]
  });
  const fixture = TestBed.createComponent(EventsListComponent);
  await fixture.whenStable();
  return { fixture, root: fixture.nativeElement as HTMLElement };
}

const names = (root: HTMLElement) => Array.from(root.querySelectorAll('tbody .title')).map(t => t.textContent?.trim());

describe('EventsListComponent', () => {
  it('explains Chronicle above the grid of events', async () => {
    const { root } = await render();
    expect(root.querySelector('.hero h1')!.textContent).toBe('Chronicle');
    expect(root.querySelector('.hero .deck')!.textContent).toContain('One living record of an event');
    expect(root.querySelectorAll('.stages li')).toHaveLength(4);
    expect(root.querySelector('#events-title')!.textContent).toBe('Events');
  });

  it('lists events, newest first, each linking to its page', async () => {
    const { root } = await render();
    expect(names(root)).toEqual(['Rail Freeze', 'Harbor Ransomware Tabletop']);
    expect(root.querySelector('tbody .title')!.getAttribute('href')).toBe('/events/rail-sim');
    expect(root.querySelector('tbody td:nth-child(2)')!.textContent).toBe('small-unit-team-exercise');
  });

  it('opens an event when its row is clicked', async () => {
    const { fixture, root } = await render();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    root.querySelectorAll<HTMLElement>('tbody tr')[1].click();
    await fixture.whenStable();
    expect(navigate).toHaveBeenCalledWith(['/events', 'harbor-ttx']);
  });

  it('searches by name, id or scenario type', async () => {
    const { fixture, root } = await render();
    const search = root.querySelector<HTMLInputElement>('input[type="search"]')!;
    search.value = 'small-unit';
    search.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    expect(names(root)).toEqual(['Rail Freeze']);
  });

  it('sorts by a column, and again in reverse', async () => {
    const { fixture, root } = await render();
    const entries = Array.from(root.querySelectorAll('button.sort')).find(b => b.textContent?.trim() === 'Entries') as HTMLButtonElement;
    entries.click();
    await fixture.whenStable();
    expect(names(root)).toEqual(['Rail Freeze', 'Harbor Ransomware Tabletop']);
    entries.click();
    await fixture.whenStable();
    expect(names(root)).toEqual(['Harbor Ransomware Tabletop', 'Rail Freeze']);
  });

  it('hides archived events until asked, and marks them', async () => {
    const { fixture, root } = await render(() => of([harbor, rail, old]));
    expect(names(root)).toEqual(['Rail Freeze', 'Harbor Ransomware Tabletop']);
    expect(root.querySelector('.archived-toggle')!.textContent).toContain('Show archived (1)');

    const toggle = root.querySelector<HTMLInputElement>('.archived-toggle input')!;
    toggle.checked = true;
    toggle.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    expect(names(root)).toEqual(['Rail Freeze', 'Harbor Ransomware Tabletop', 'Old Drill']);
    expect(root.querySelector('tr.archived .pill')!.textContent).toBe('Archived');
  });

  it('archives an event without opening it, and restores one', async () => {
    const archive = vi.fn((id: string, archived: boolean) => of({ ...(id === 'rail-sim' ? rail : old), archived }));
    const { fixture, root } = await render(() => of([harbor, rail, old]), archive);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    root.querySelector<HTMLButtonElement>('button[aria-label="Archive Rail Freeze"]')!.click();
    await fixture.whenStable();
    expect(archive).toHaveBeenCalledWith('rail-sim', true);
    expect(navigate).not.toHaveBeenCalled();
    expect(names(root)).toEqual(['Harbor Ransomware Tabletop']);
    expect(root.querySelector('.archived-toggle')!.textContent).toContain('Show archived (2)');

    const toggle = root.querySelector<HTMLInputElement>('.archived-toggle input')!;
    toggle.checked = true;
    toggle.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    root.querySelector<HTMLButtonElement>('button[aria-label="Restore Old Drill"]')!.click();
    await fixture.whenStable();
    expect(archive).toHaveBeenCalledWith('old-drill', false);
    expect(root.querySelector('.archived-toggle')!.textContent).toContain('Show archived (1)');
  });

  it('says when every event is archived', async () => {
    const { root } = await render(() => of([old]));
    expect(root.textContent).toContain('Every event is archived.');
  });

  it('says when there are none, and when the API is down', async () => {
    const empty = await render(() => of([]));
    expect(empty.root.textContent).toContain('No events yet.');
    TestBed.resetTestingModule();

    const down = await render(() => throwError(() => new HttpErrorResponse({ status: 0 })));
    expect(down.root.querySelector('[role="alert"]')!.textContent).toContain('not reachable');
  });
});
