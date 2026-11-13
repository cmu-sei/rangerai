import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { NEVER, Subject, of, throwError } from 'rxjs';
import { ChronicleApiService } from '../../core/chronicle-api.service';
import { Audience, ChronicleRecord, StreamEvent } from '../../core/chronicle.model';
import { EventDetailComponent } from './event-detail.component';

const author = { app: 'scenario-builder', actor: 'designer@example.org', via: 'person' };

function record(audience: Audience): ChronicleRecord {
  const groundTruth = { kind: 'fact', id: 'patient-zero', visibility: 'excon' as const, text: 'Phishing got them in.' };
  return {
    schemaVersion: '0.2.0', id: 'harbor-ttx', name: 'Harbor Ransomware Tabletop', kind: 'exercise',
    description: 'Port leaders respond to ransomware.', created: '2026-11-02T14:00:00Z',
    world: [
      { kind: 'organization', id: 'port-authority', visibility: 'participant', name: 'Bayport Port Authority' },
      ...(audience === 'participant' ? [] : [groundTruth])
    ],
    intent: [{ kind: 'objective', id: 'obj-escalate', visibility: 'participant', text: 'Leaders decide when to escalate.' }],
    asBuilt: [], asRun: [],
    lessons: [{ kind: 'lesson', id: 'lesson-owner', visibility: 'participant', text: 'Ownership is unclear.', recommendation: 'Name an owner.' }],
    history: [
      { id: 'e1', sequence: 1, recordedAt: '2026-11-02T14:00:00Z', part: 'world', kind: 'organization', op: 'add', itemId: 'port-authority', author },
      { id: 'e2', sequence: 2, recordedAt: '2026-11-02T14:05:00Z', part: 'intent', kind: 'objective', op: 'add', itemId: 'obj-escalate', author }
    ]
  };
}

async function render(getChronicle = vi.fn((_id: string, audience: Audience) => of(record(audience))), stream = vi.fn((_id: string, _a: Audience) => NEVER as unknown as Subject<StreamEvent>)) {
  TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: ChronicleApiService, useValue: { getChronicle, stream } }] });
  const fixture = TestBed.createComponent(EventDetailComponent);
  fixture.componentRef.setInput('id', 'harbor-ttx');
  await fixture.whenStable();
  return { fixture, getChronicle, stream, root: fixture.nativeElement as HTMLElement };
}

const titles = (root: HTMLElement, part: string) =>
  Array.from(root.querySelectorAll(`#${part} .title`)).map(t => t.textContent?.trim());

describe('EventDetailComponent', () => {
  it('shows the event, its five parts and its latest entries', async () => {
    const { root, getChronicle } = await render();

    expect(getChronicle).toHaveBeenCalledWith('harbor-ttx', 'participant');
    expect(root.querySelector('h1')!.textContent).toBe('Harbor Ransomware Tabletop');
    expect(root.querySelector('.description')!.textContent).toContain('ransomware');
    expect(Array.from(root.querySelectorAll('.layers strong')).map(s => s.textContent))
      .toEqual(['World', 'Intent', 'As Built', 'As Run', 'Lessons']);
    expect(titles(root, 'world')).toEqual(['Bayport Port Authority']);
    expect(titles(root, 'intent')).toEqual(['Leaders decide when to escalate.']);
    expect(root.querySelector('#lessons .detail')!.textContent).toBe('Name an owner.');
    expect(root.querySelector('#asBuilt .none')).not.toBeNull();
    expect(Array.from(root.querySelectorAll('.entries .seq')).map(s => s.textContent)).toEqual(['#2', '#1']);
  });

  it('asks again as another audience, and marks what only that audience sees', async () => {
    const { fixture, root, getChronicle } = await render();

    const excon = Array.from(root.querySelectorAll<HTMLButtonElement>('.audience button')).find(b => b.textContent === 'Exercise control')!;
    excon.click();
    await fixture.whenStable();

    expect(getChronicle).toHaveBeenLastCalledWith('harbor-ttx', 'excon');
    expect(excon.getAttribute('aria-checked')).toBe('true');
    expect(titles(root, 'world')).toEqual(['Bayport Port Authority', 'Phishing got them in.']);
    expect(root.querySelector('#world li:last-child .tag')!.textContent).toBe('excon');
  });

  it('treats the parts and history the API leaves out as empty', async () => {
    const { world, intent, history, ...bare } = record('participant');
    const { root } = await render(vi.fn(() => of(bare as unknown as ChronicleRecord)));

    expect(root.querySelector('[role="alert"]')).toBeNull();
    expect(Array.from(root.querySelectorAll('.layers .count')).map(c => c.textContent)).toEqual(['0', '0', '0', '0', '1']);
    expect(root.querySelector('.entries')).toBeNull();
  });

  it('subscribes as the audience, reads the record again for a newer entry, and highlights what arrived', async () => {
    const events = new Subject<StreamEvent>();
    let current = record('participant');
    const getChronicle = vi.fn(() => of(current));
    const { fixture, root, stream } = await render(getChronicle, vi.fn(() => events));
    vi.useFakeTimers();
    try {
      expect(stream).toHaveBeenCalledWith('harbor-ttx', 'participant');

      events.next({ type: 'open' });
      events.next({ type: 'entry', entry: current.history[1] });   // already shown: no new read
      vi.advanceTimersByTime(400);
      expect(getChronicle).toHaveBeenCalledTimes(1);

      const built = { kind: 'realization', id: 'real-web', visibility: 'participant' as const, of: 'port-authority', deploymentId: 'dep-1',
        identifiers: { hostname: 'bay-web', ip: '10.0.0.5' } };
      const entry = { id: 'e3', sequence: 3, recordedAt: '2026-11-02T15:00:00Z', part: 'asBuilt' as const, kind: 'realization', op: 'add',
        itemId: 'real-web', item: built, author: { app: 'grayspace', via: 'app' } };
      current = { ...current, asBuilt: [built], history: [...current.history, entry] };
      events.next({ type: 'entry', entry });
      vi.advanceTimersByTime(400);
      fixture.detectChanges();

      expect(getChronicle).toHaveBeenCalledTimes(2);
      expect(root.querySelector('[data-live]')!.getAttribute('data-state')).toBe('live');
      const row = root.querySelector('#asBuilt [data-item-id="real-web"]')!;
      expect(row.classList).toContain('fresh');
      expect(row.querySelector('.title')!.textContent).toBe('bay-web');
      expect(row.querySelector('.author')!.textContent).toBe('grayspace');
      expect(root.querySelector('[data-part-tile="asBuilt"]')!.classList).toContain('glow');
      expect(root.querySelector('.entries [data-seq="3"]')!.classList).toContain('fresh');
    } finally {
      vi.useRealTimers();
    }
  });

  it('says when there is no such event', async () => {
    const { root } = await render(vi.fn(() => throwError(() => new HttpErrorResponse({ status: 404 }))));
    expect(root.querySelector('[role="alert"]')!.textContent).toContain('There is no such event.');
  });
});
