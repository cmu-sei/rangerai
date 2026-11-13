import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import {
  FollowView,
  GraySpaceOffers,
  GsAttach,
  GsHost,
  GsManifestHost,
  GsManifestSite,
  SliceSpec
} from '../../../../core/models/grayspace.model';

/** Where a host stands: drafted only, being built, or built and in GraySpace's manifest. */
export type HostState = 'planned' | 'building' | 'running' | 'stopped' | 'failed';

/** A host of the spec as drawn: one chip, even with a count, and the manifest hosts it became. */
export interface HostChip {
  name: string;
  count: number;
  image: string;
  imageType: string;
  roles: string[];
  publish: boolean;
  internet: boolean;
  labels: { key: string; value: string }[];
  built: GsManifestHost[];
  running: number;
  state: HostState;
  ip?: string;
  publicIp?: string;
  fqdn?: string;
}

export interface SegmentLane {
  name: string;
  cidr?: string;
  hosts: HostChip[];
}

export interface SiteCard {
  name: string;
  routed: boolean;
  wanIp?: string;
  edge?: string;
  segments: SegmentLane[];
}

/** One place on the base where the enclave joins it, and everything that joins there. */
export interface AttachColumn {
  key: string;
  router?: string;
  label: string;
  sites: SiteCard[];
  internetHosts: HostChip[];
  weight: number;
}

export interface StageStep {
  id: string;
  title: string;
  state: 'done' | 'current' | 'failed' | 'todo';
}

/** Labels worth showing on a chip, in this order; the rest stay in the spec. */
const SHOWN_LABELS = ['ghosts.pool', 'purpose', 'vendor', 'pandora.site'];

const DEFAULT_STAGES = ['validate', 'allocate', 'render', 'apply', 'wait', 'configure', 'dns', 'conditions', 'check', 'ready'];

/**
 * The enclave as a picture: the base it joins, where each site's edge and each internet host attaches, the segments
 * and hosts behind each edge, and the names and certificates it serves. With a follow view it shows the build as
 * GraySpace reports it: the job's stages, and each host's addresses and status as the manifest fills in.
 */
@Component({
  selector: 'app-enclave-diagram',
  standalone: true,
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './enclave-diagram.component.html',
  styleUrls: ['./enclave-diagram.component.scss']
})
export class EnclaveDiagramComponent {
  readonly spec = input.required<SliceSpec>();
  readonly follow = input<FollowView | null>(null);
  readonly offers = input<GraySpaceOffers | null>(null);

  protected readonly job = computed(() => {
    const job = this.follow()?.job;
    return job && job.kind !== 'plan_slice' && job.kind !== 'plan' ? job : null;
  });
  protected readonly instance = computed(() => this.follow()?.instance ?? null);
  /** Hosts are being built: a job that makes or changes the instance is running. */
  protected readonly building = computed(() => {
    const job = this.job();
    return !!job && !job.finished && (job.state === 'running' || job.state === 'queued');
  });
  protected readonly live = computed(() => {
    const instance = this.instance();
    return !!instance && instance.state !== 'destroyed' && !!instance.hosts?.length;
  });

  protected readonly baseName = computed(() => {
    const o = this.offers();
    return o?.base?.version?.name ?? o?.server?.base?.name ?? null;
  });
  protected readonly routers = computed(() => this.offers()?.base?.routers?.expected ?? null);

  protected readonly stages = computed<StageStep[]>(() => {
    const job = this.job();
    if (!job) return [];
    const ids = job.stages?.length ? job.stages : DEFAULT_STAGES;
    const done = new Set(this.instance()?.stages_done ?? []);
    const at = job.stage ? ids.indexOf(job.stage) : -1;
    return ids.map((id, i) => {
      let state: StageStep['state'] = 'todo';
      if (job.state === 'succeeded' || done.has(id) || (at >= 0 && i < at)) state = 'done';
      else if (i === at) state = job.state === 'failed' || job.state === 'cancelled' ? 'failed' : 'current';
      return { id, title: job.stage_titles?.[id] ?? id, state };
    });
  });
  protected readonly stageIndex = computed(() => {
    const steps = this.stages();
    const current = steps.findIndex(s => s.state === 'current' || s.state === 'failed');
    return current >= 0 ? current + 1 : steps.filter(s => s.state === 'done').length;
  });

  protected readonly columns = computed<AttachColumn[]>(() => {
    const spec = this.spec();
    const manifestSites = this.instance()?.sites ?? [];
    const columns = new Map<string, AttachColumn>();
    const column = (attach: GsAttach | null | undefined): AttachColumn => {
      const key = this.attachKey(attach);
      let c = columns.get(key);
      if (!c) {
        c = { key, router: attach?.router, label: this.attachLabel(attach), sites: [], internetHosts: [], weight: 0 };
        columns.set(key, c);
      }
      return c;
    };

    for (const site of spec.sites ?? []) {
      const attach = Array.isArray(site.attach) ? site.attach[0] : site.attach;
      const built = manifestSites.find(m => m.name === site.name);
      const c = column(attach);
      if (built?.router && !c.router) c.router = built.router;
      const segments: SegmentLane[] = site.segments.map(seg => ({
        name: seg.name,
        cidr: this.segmentCidr(built, seg.name) ?? (seg.cidr && seg.cidr !== 'auto' ? seg.cidr : undefined),
        hosts: (spec.hosts ?? []).filter(h => !h.internet && h.segment === seg.name).map(h => this.chip(h))
      }));
      c.sites.push({ name: site.name, routed: !!site.routed, wanIp: built?.wan_ip, edge: built?.edge, segments });
      c.weight += Math.max(1, segments.reduce((n, s) => n + Math.max(1, s.hosts.length), 0));
    }
    for (const host of spec.hosts ?? []) {
      if (host.internet) {
        const c = column(host.internet);
        c.internetHosts.push(this.chip(host));
        c.weight += 1;
      }
    }
    return [...columns.values()];
  });

  protected readonly hostTotals = computed(() => {
    const chips = this.columns().flatMap(c => [...c.internetHosts, ...c.sites.flatMap(s => s.segments.flatMap(g => g.hosts))]);
    return {
      hosts: chips.reduce((n, c) => n + c.count, 0),
      running: chips.reduce((n, c) => n + c.running, 0),
      sites: this.spec().sites?.length ?? 0
    };
  });

  protected readonly domains = computed(() => {
    const records = this.spec().dns?.records ?? [];
    return (this.spec().dns?.domains ?? []).map(d => ({
      domain: d.domain,
      server: d.hosting_server,
      records: records.filter(r => r.name === d.domain || r.name.endsWith('.' + d.domain)).length
    }));
  });

  protected readonly certificates = computed(() => {
    const cas = this.offers()?.capabilities?.cas ?? [];
    const issued = this.instance()?.certificates ?? [];
    return (this.spec().tls ?? []).map(t => ({
      names: t.names.join(', '),
      host: t.host,
      ca: cas.find(c => c.hostname === t.ca)?.name ?? t.ca,
      issued: issued.some(i => i.host === t.host)
    }));
  });

  private attachKey(attach: GsAttach | null | undefined): string {
    if (!attach) return 'any';
    return attach.router ? `router:${attach.router}` : `${attach.isp ?? ''}/${attach.location ?? ''}`;
  }

  /** "Comcast · Portland" for a router GraySpace lists; the ISP or location as given otherwise. */
  private attachLabel(attach: GsAttach | null | undefined): string {
    if (!attach) return 'Any attachment point';
    if (attach.router) {
      const point = this.offers()?.attachmentPoints?.find(p => p.router === attach.router);
      return point ? [point.isp, point.location].filter(Boolean).join(' · ') : attach.router;
    }
    return [attach.isp, attach.location].filter(Boolean).join(' · ') || 'Any attachment point';
  }

  private segmentCidr(site: GsManifestSite | undefined, segment: string): string | undefined {
    return site?.segments?.find(s => s.name === segment)?.cidr;
  }

  /** The manifest hosts a spec host became: itself, or <name>-01 and on with a count. */
  private builtFor(host: GsHost): GsManifestHost[] {
    const all = this.instance()?.hosts ?? [];
    const copies = new RegExp(`^${host.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}-\\d+$`);
    return all.filter(m => m.name === host.name || copies.test(m.name));
  }

  private chip(host: GsHost): HostChip {
    const count = host.count ?? 1;
    const built = this.builtFor(host);
    const running = built.filter(b => b.status === 'running').length;
    const job = this.job();
    let state: HostState = 'planned';
    if (built.length && running >= count) state = 'running';
    else if (this.building()) state = 'building';
    else if (job?.state === 'failed') state = 'failed';
    else if (built.length) state = running ? 'running' : 'stopped';
    const first = built[0];
    const labels = host.labels ?? {};
    return {
      name: host.name,
      count,
      image: host.image,
      imageType: this.offers()?.server?.images.find(i => i.name === host.image)?.type ?? this.guessType(host.image),
      roles: host.roles ?? [],
      publish: !!host.publish,
      internet: !!host.internet,
      labels: SHOWN_LABELS.filter(k => labels[k]).map(k => ({ key: k === 'ghosts.pool' ? 'pool' : k === 'pandora.site' ? 'site' : k, value: labels[k] })),
      built,
      running,
      state,
      ip: count > 1 && built.length > 1 ? `${built.length} addresses` : first?.ip,
      publicIp: first?.public_ip ?? undefined,
      fqdn: first?.fqdns?.[0]
    };
  }

  private guessType(image: string): string {
    if (image.endsWith('-vm')) return 'vm';
    if (image.endsWith('-lxc') || image.startsWith('alpine')) return 'lxc';
    return 'oci';
  }

  protected track(chip: HostChip): string {
    return chip.name;
  }
}
