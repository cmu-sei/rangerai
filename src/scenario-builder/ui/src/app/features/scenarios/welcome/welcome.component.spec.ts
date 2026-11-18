import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ScenarioType } from '../../../core/models/builder.model';
import { BuilderApiService } from '../../../core/services/builder-api.service';
import { HelpService } from '../../../shared/help/help.service';
import { WelcomeComponent } from './welcome.component';

const q = (id: string) => ({ id, text: `${id}?`, why: 'w', answer: 'text' as const, path: `/${id}` });
const types: Record<string, ScenarioType> = {
  ttx: { id: 'ttx', version: '1', name: 'Tabletop', drafts: [], stages: [{ number: 0, title: 'A', questions: [q('a'), q('b')] }, { number: 1, title: 'B', questions: [q('c')] }] },
  range: { id: 'range', version: '2', name: 'Range Drill', drafts: [], topology: true, description: 'A team on a range.', stages: [{ number: 0, title: 'A', questions: [q('a')] }] },
  bots: { id: 'bots', version: '1', name: 'Bot Trial', drafts: [], topology: true, kind: 'experiment', stages: [{ number: 0, title: 'A', questions: [q('a')] }] }
};

async function render() {
  localStorage.clear();
  const api = { getScenarioType: vi.fn((id: string) => of(types[id])) };
  TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: BuilderApiService, useValue: api }] });
  const fixture = TestBed.createComponent(WelcomeComponent);
  fixture.componentRef.setInput('scenarioTypes', [{ id: 'ttx', version: '1', name: 'Tabletop' }, { id: 'range', version: '2', name: 'Range Drill' }, { id: 'bots', version: '1', name: 'Bot Trial' }]);
  await fixture.whenStable();
  return { fixture, api, root: fixture.nativeElement as HTMLElement };
}

describe('WelcomeComponent', () => {
  it('shows the four steps and the scenario types grouped by kind of event', async () => {
    const { api, root } = await render();

    expect(root.querySelectorAll('.steps li').length).toBe(4);
    expect(api.getScenarioType).toHaveBeenCalledWith('range', '2');
    const groups = Array.from(root.querySelectorAll('.group')).map(g => ({
      name: g.querySelector('.group-name')?.textContent?.trim(),
      types: Array.from(g.querySelectorAll('.type')).map(t => t.textContent?.trim())
    }));
    expect(groups).toEqual([
      { name: 'Exercises', types: ['Tabletop', 'Range Drill'] },
      { name: 'Experiments', types: ['Bot Trial'] }
    ]);
    expect(root.querySelector('.type[title]')?.getAttribute('title')).toBe('A team on a range.');
  });

  it('hides once dismissed, and stays hidden', async () => {
    const { fixture, root } = await render();

    (Array.from(root.querySelectorAll('button')).find(b => b.textContent?.trim() === 'Got it') as HTMLButtonElement).click();
    await fixture.whenStable();

    expect(root.querySelector('.welcome')).toBeNull();
    expect(TestBed.inject(HelpService).welcomeDismissed()).toBe(true);
    expect(localStorage.getItem('sb.help.welcomeDismissed')).toBe('1');
  });
});
