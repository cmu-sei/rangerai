import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ChronicleApiService } from '../../core/chronicle-api.service';
import { ChronicleSummary } from '../../core/chronicle.model';
import { EventsListComponent } from './events-list.component';

const event = (id: string, name: string, extra: Partial<ChronicleSummary> = {}): ChronicleSummary => ({
  id, name, kind: 'exercise', scenarioType: { id: 'crucible-tabletop', version: '0.3.0' }, created: '2026-11-02T14:00:00Z', entries: 10, ...extra
});

const harbor = event('harbor-ttx', 'Harbor Ransomware Tabletop', { entries: 33, created: '2026-11-02T14:00:00Z' });
const rail = event('rail-sim', 'Rail Freeze', { scenarioType: { id: 'small-unit-team-exercise' }, entries: 5, created: '2026-11-09T14:00:00Z' });

async function render(getChronicles = () => of([harbor, rail])) {
  TestBed.configureTestingModule({
    providers: [provideRouter([]), { provide: ChronicleApiService, useValue: { getChronicles } }]
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

  it('says when there are none, and when the API is down', async () => {
    const empty = await render(() => of([]));
    expect(empty.root.textContent).toContain('No events yet.');
    TestBed.resetTestingModule();

    const down = await render(() => throwError(() => new HttpErrorResponse({ status: 0 })));
    expect(down.root.querySelector('[role="alert"]')!.textContent).toContain('not reachable');
  });
});
