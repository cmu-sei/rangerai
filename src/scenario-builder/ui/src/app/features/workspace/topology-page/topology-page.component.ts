import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { GraySpaceStatus } from '../../../core/models/grayspace.model';
import { BuilderApiService } from '../../../core/services/builder-api.service';
import { WorkspaceStore } from '../workspace.store';

/** Navigation state a target screen's back link sets, to stay on this page rather than go straight back. */
export const OVERVIEW = { overview: true };

/**
 * Where the range a scenario needs is built and deployed, for a scenario type that has a topology.
 * GraySpace deploys; Updraft opens a read-only screen of its choices, and the others aren't built yet.
 * Once a deployment is built for one, the scenario's pull document names it and the others are grayed out:
 * a scenario deploys to one target for now. With nothing else to choose, the page goes straight on to that target's screen,
 * unless it was opened from there to see them all (the target screen's back link says so with {@link OVERVIEW}).
 */
@Component({
  selector: 'app-topology-page',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './topology-page.component.html',
  styleUrls: ['./topology-page.component.scss']
})
export class TopologyPageComponent {
  protected readonly store = inject(WorkspaceStore);

  /** Whether an admin has connected GraySpace, and whether it answered just now; none while the check runs. */
  protected readonly grayspace = signal<GraySpaceStatus | null>(null);
  protected readonly grayspaceState = computed(() => {
    const s = this.grayspace();
    return !s ? 'Checking…' : s.answering ? 'Connected' : s.connected ? 'Not answering' : 'Not connected';
  });
  /** The target this scenario's deployment was built for, from a fresh pull document; none until one is. */
  protected readonly built = signal<string | null>(null);
  protected readonly builtName = computed(() => this.targets.find(t => t.id === this.built())?.name ?? this.built());

  constructor() {
    const api = inject(BuilderApiService);
    const router = inject(Router);
    const route = inject(ActivatedRoute);
    const overview = !!router.currentNavigation()?.extras.state?.['overview'];
    api.getGraySpace().subscribe({ next: s => this.grayspace.set(s), error: () => this.grayspace.set({ connected: false }) });
    effect(() => {
      const id = this.store.scenarioId();
      if (id) {
        untracked(() =>
          api.pull(id).subscribe(doc => {
            const built = (doc['deployments'] as { target: string }[] | undefined)?.[0]?.target ?? null;
            this.built.set(built);
            const open = this.targets.find(t => t.id === built)?.open;
            if (open && !overview) {
              router.navigate([open], { relativeTo: route, replaceUrl: true });
            }
          })
        );
      }
    });
  }

  /** Grayed out: a deployment is built for another target. */
  protected unavailable(id: string): boolean {
    return !!this.built() && this.built() !== id;
  }

  /** A target with a screen opens it; the rest aren't built yet. */
  protected readonly targets: { id: string; name: string; description: string; open?: string }[] = [
    { id: 'grayspace', name: 'GraySpace', description: 'Builds the range through GraySpace’s Terrain API.', open: 'grayspace' },
    { id: 'updraft', name: 'Updraft', description: 'Builds the lab through the Updraft CLI: Gitea, Terraform and Packer on AWS.', open: 'updraft' },
    { id: 'crucible', name: 'Crucible', description: 'Builds the range through Crucible’s APIs.' },
    { id: 'other', name: 'Other Range', description: 'Builds the range through another range’s API.' }
  ];
}
