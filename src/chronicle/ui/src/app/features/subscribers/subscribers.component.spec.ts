import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of } from 'rxjs';
import { ChronicleApiService } from '../../core/chronicle-api.service';
import { Audience, ChronicleRecord } from '../../core/chronicle.model';
import { Subscriber } from '../../core/live.model';
import { LiveEvent, LiveService } from '../../core/live.service';
import { SubscribersComponent, clean } from './subscribers.component';

const record: ChronicleRecord = {
  schemaVersion: '0.3.0', id: 'salt', name: 'SALT LANTERN', kind: 'exercise', created: '2026-11-11T21:00:00Z',
  world: [{ kind: 'persona', id: 'persona-dana', visibility: 'participant', name: 'Dana Whitlock' }],
  intent: [], asBuilt: [], asRun: [], lessons: [], history: []
};

const dana: Subscriber = {
  id: 'dana', chronicleId: 'salt', app: 'ghosts', name: 'GHOSTS · Dana Whitlock', role: 'Plant operator', audience: 'participant',
  viewers: ['persona-dana'], hasAgent: true, model: 'gemma4:26b-32k', registered: '2026-11-11T21:00:00Z',
  query: 'GET /api/chronicles/salt?audience=participant&viewers=persona-dana'
};
const ghosts: Subscriber = { ...dana, id: 'ghosts', name: 'GHOSTS', hasAgent: false, viewers: [], activityCount: 2, query: 'GET /api/chronicles/salt?audience=participant' };

async function render() {
  const feed = new Subject<LiveEvent>();
  const connect = vi.fn((_id: string, _a: Audience) => feed);
  const run = vi.fn(() => of({ runId: 'r1', subscriberId: 'dana' }));
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      { provide: ChronicleApiService, useValue: { getChronicle: vi.fn(() => of(record)) } },
      { provide: LiveService, useValue: { connect, run } }
    ]
  });
  const fixture = TestBed.createComponent(SubscribersComponent);
  fixture.componentRef.setInput('id', 'salt');
  await fixture.whenStable();
  const send = async (e: LiveEvent) => { feed.next(e); fixture.detectChanges(); await fixture.whenStable(); };
  return { fixture, feed, send, connect, run, root: fixture.nativeElement as HTMLElement };
}

describe('SubscribersComponent', () => {
  it('opens as exercise control, and shows the subscribers the hello names', async () => {
    const { connect, send, root } = await render();
    expect(connect).toHaveBeenCalledWith('salt', 'excon');
    await send({ type: 'state', state: 'live' });
    await send({ type: 'message', message: { type: 'hello', chronicleId: 'salt', audience: 'excon', subscribers: [dana, ghosts], activity: [] } });
    expect(root.querySelector('[data-live]')?.getAttribute('data-state')).toBe('live');
    expect(root.querySelector('[data-subscriber="dana"] .name')?.textContent).toContain('Dana Whitlock');
    expect(root.querySelector('[data-subscriber="ghosts"] .n b')?.textContent).toBe('2');
  });

  it('slides activity into the feed, names the actor, and counts it against GHOSTS', async () => {
    const { send, root } = await render();
    await send({ type: 'message', message: { type: 'hello', chronicleId: 'salt', audience: 'excon', subscribers: [dana, ghosts], activity: [] } });
    await send({ type: 'message', message: { type: 'activity', activity: {
      id: 'a1', chronicleId: 'salt', source: 'ghosts', actor: 'persona-dana', kind: 'email', text: 'Mailed IT', host: 'saltlan-eng-ws01',
      at: '2026-11-11T21:01:00Z', visibility: 'participant', sequence: 7 } } });
    const item = root.querySelector('[data-feed] li')!;
    expect(item.classList).toContain('fresh');
    expect(item.querySelector('.actor')?.textContent).toBe('Dana Whitlock');
    expect(item.querySelector('[data-recorded]')?.textContent).toContain('#7');
    expect(root.querySelector('[data-subscriber="ghosts"] .n b')?.textContent).toBe('3');
  });

  it('runs an agent, streams its words, and shows what it published', async () => {
    const { send, run, root } = await render();
    await send({ type: 'message', message: { type: 'hello', chronicleId: 'salt', audience: 'excon', subscribers: [dana], activity: [] } });
    (root.querySelector('[data-run="dana"]') as HTMLButtonElement).click();
    expect(run).toHaveBeenCalledWith('salt', 'dana');
    const at = '2026-11-11T21:02:00Z';
    await send({ type: 'message', message: { type: 'activity', activity: { id: 'a2', chronicleId: 'salt', source: 'ghosts', subscriberId: 'dana', kind: 'agent.started', at, visibility: 'participant', data: { runId: 'r1', items: 48, total: 80, withheld: 32, tokens: 1183, sample: [] } } } });
    await send({ type: 'message', message: { type: 'token', subscriberId: 'dana', runId: 'r1', text: '**Setpoint** moved' } });
    expect(root.querySelector('[data-subscriber="dana"] [data-v="withheld"]')?.textContent).toBe('32');
    expect(root.querySelector('[data-subscriber="dana"] [data-output]')?.textContent).toContain('Setpoint moved');
    await send({ type: 'message', message: { type: 'activity', activity: { id: 'a3', chronicleId: 'salt', source: 'ghosts', subscriberId: 'dana', kind: 'agent.published', at, visibility: 'participant', sequence: 97, data: { runId: 'r1', part: 'asRun', kind: 'injectDelivery', text: 'Setpoint moved' } } } });
    expect(root.querySelector('[data-subscriber="dana"] [data-published]')?.textContent).toContain('As Run · #97');
  });

  it('cleans markdown out of what a model writes', () => {
    expect(clean('## **Brief**\n* one\n- two')).toBe('Brief\n• one\n• two');
  });
});
