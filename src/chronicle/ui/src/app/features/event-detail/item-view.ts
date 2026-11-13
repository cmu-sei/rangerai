import { Author, Item } from '../../core/chronicle.model';

/** One labelled value shown under an item, such as an address or a hostname. */
export interface Meta {
  label: string;
  value: string;
  mono?: boolean;
}

/** How one item reads in a part: the words that name it, what follows, and the facts worth a glance. */
export interface ItemView {
  title: string;
  detail: string | null;
  /** Long text (an article's body, a message), clamped to a few lines. */
  preview: string | null;
  /** A small line above the title, such as an article's format or a deployment's builder. */
  eyebrow: string | null;
  /** Who to whom, for messages, injects and posts. */
  route: { from: string | null; to: string[]; channel: string | null } | null;
  /** A deployment's state. */
  state: string | null;
  meta: Meta[];
}

type Lookup = (id: string) => string;

const str = (value: unknown): string | null => (typeof value === 'string' && value.trim() ? value : null);
const list = (value: unknown): string[] => (Array.isArray(value) ? value.filter((v): v is string => typeof v === 'string') : []);
const obj = (value: unknown): Record<string, unknown> | null =>
  value && typeof value === 'object' && !Array.isArray(value) ? (value as Record<string, unknown>) : null;

/** camelCase as words: publicIp → public ip. */
export function words(text: string): string {
  return text.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/[._-]+/g, ' ').toLowerCase();
}

/** The words that best name an item, whatever its kind. */
export function titleOf(item: Item): string {
  const event = obj(item['event']);
  for (const value of [item['name'], item['title'], item['text'], event?.['class'], item['type'], item['cve'], item['of']]) {
    const s = str(value);
    if (s) return s;
  }
  return item.id;
}

/** Long enough to read as a passage rather than a line. */
const LONG = 160;

function short(version: string): string {
  return version.startsWith('sha256:') ? version.slice(7, 19) : version;
}

/** The view of one item. `name` turns an id the item names into that item's title, when the reader can see it. */
export function viewOf(item: Item, name: Lookup): ItemView {
  const view: ItemView = { title: titleOf(item), detail: null, preview: null, eyebrow: null, route: null, state: null, meta: [] };
  const add = (label: string, value: unknown, mono = true) => {
    const s = typeof value === 'number' || typeof value === 'boolean' ? String(value) : str(value);
    if (s) view.meta.push({ label, value: s, mono });
  };

  switch (item.kind) {
    case 'device': {
      view.detail = str(item['description']) ?? str(item['role']);
      if (str(item['description']) && str(item['role'])) add('role', item['role'], false);
      add('id', item['deviceIdentifier']);
      for (const nic of (item['networkInterfaces'] as unknown[] | undefined) ?? []) {
        const n = obj(nic);
        if (n && str(n['address'])) add(str(n['name']) ?? name(String(n['relatedObjectId'] ?? '')), n['address']);
      }
      const types = list(item['deviceTypes']);
      if (types.length) add('type', types.map(words).join(', '), false);
      break;
    }
    case 'network':
      view.detail = str(item['description']);
      add('cidr', item['cidr']);
      break;
    case 'service':
      view.detail = str(item['description']);
      add('service', item['serviceType'], false);
      add('address', item['address']);
      break;
    case 'deployment': {
      view.eyebrow = str(item['builder']);
      view.title = str(item['reference']) ?? `${item['builder'] ?? 'Build'}`;
      view.state = str(item['state']);
      const builds = list(item['builds']);
      if (builds.length) view.detail = `Builds ${builds.length} planned item${builds.length === 1 ? '' : 's'}: ${builds.slice(0, 4).map(name).join(', ')}${builds.length > 4 ? '…' : ''}`;
      if (str(item['version'])) add('version', short(String(item['version'])));
      break;
    }
    case 'realization': {
      const ids = obj(item['identifiers']) ?? {};
      const of = str(item['of']);
      view.title = str(ids['hostname']) ?? str(ids['name']) ?? (of ? name(of) : item.id);
      view.detail = [of ? `Built from ${name(of)}` : null, str(item['deviation'])].filter(Boolean).join(' · ') || null;
      const order = ['ip', 'address', 'publicIp', 'public_ip', 'fqdn', 'fqdns', 'site', 'segment', 'image', 'vmId'];
      const keys = Object.keys(ids).filter(k => k !== 'hostname' && k !== 'name')
        .sort((a, b) => (order.indexOf(a) + 1 || 99) - (order.indexOf(b) + 1 || 99));
      for (const key of keys) add(words(key), ids[key]);
      break;
    }
    case 'content': {
      const format = str(item['format']);
      view.eyebrow = format ? ({ 'text/markdown': 'article', 'text/html': 'web page', 'text/plain': 'text' }[format] ?? format.split('/').pop()!) : null;
      view.title = str(item['title']) ?? str(item['format']) ?? item.id;
      view.preview = str(item['body']);
      const by = str(item['attributedTo']);
      const on = str(item['serviceId']);
      if (by || on) view.route = { from: by ? name(by) : null, to: [], channel: on ? name(on) : null };
      add('uri', item['uri']);
      break;
    }
    case 'influenceActivity': {
      const a = obj(item['activity']) ?? {};
      view.eyebrow = str(a['action']);
      const text = str(a['text']);
      view.title = text && text.length <= 90 ? text : `${name(String(a['actorId'] ?? ''))} ${str(a['action']) ?? 'acted'}`;
      if (text && text.length > 90) view.preview = text;
      view.route = { from: str(a['actorId']) ? name(String(a['actorId'])) : null, to: [], channel: str(a['serviceId']) ? name(String(a['serviceId'])) : null };
      break;
    }
    case 'cyberActivity': {
      const e = obj(item['event']) ?? {};
      view.eyebrow = str(e['phase']);
      const actors = list(e['actorIds']).map(name);
      const targets = list(e['targetIds']).map(name);
      if (actors.length || targets.length) view.route = { from: actors.join(', ') || null, to: targets, channel: null };
      const mitre = list(e['mitreTechniqueIds']);
      if (mitre.length) add('att&ck', mitre.join(', '));
      add('seen by', item['observedBy'], false);
      break;
    }
    case 'plannedEvent': {
      const inject = obj(item['inject']);
      if (inject) {
        view.route = { from: str(inject['from']) ? name(String(inject['from'])) : null, to: list(inject['to']).map(name), channel: str(inject['channel']) };
        const content = str(inject['content']);
        if (content) (content.length > LONG ? (view.preview = content) : (view.detail = content));
      }
      if (!view.detail && !view.preview) view.detail = str(item['expectedResponse']) ?? str(item['condition']);
      break;
    }
    case 'injectDelivery':
      view.title = str(item['plannedEventId']) ? name(String(item['plannedEventId'])) : 'Inject delivered';
      view.eyebrow = 'delivered';
      view.route = { from: null, to: list(item['to']).map(name), channel: null };
      view.detail = str(item['note']);
      break;
    case 'participantAction':
    case 'observation': {
      const text = str(item['text']) ?? '';
      view.route = { from: str(item['by']) ? name(String(item['by'])) : null, to: list(item['about']).map(name), channel: null };
      if (text.length > LONG) {
        view.title = item.kind === 'observation' ? 'Observation' : 'Action';
        view.preview = text;
      }
      break;
    }
    case 'persona': {
      view.detail = str(item['description']) ?? str(item['role']);
      if (str(item['description'])) add('role', item['role'], false);
      if (str(item['organizationId'])) add('works for', name(String(item['organizationId'])), false);
      if (str(item['poolId'])) add('pool', name(String(item['poolId'])), false);
      if (str(item['origin'])) add('origin', words(String(item['origin'])), false);
      for (const account of (item['accounts'] as unknown[] | undefined) ?? []) {
        const a = obj(account);
        if (a && str(a['username'])) add(name(String(a['serviceId'] ?? 'account')), a['username']);
      }
      break;
    }
    case 'extension': {
      const data = obj(item['data']) ?? {};
      view.eyebrow = str(item['type']);
      view.title = str(data['title']) ?? str(data['name']) ?? str(item['type']) ?? item.id;
      const text = str(data['text']) ?? str(data['body']) ?? str(data['summary']);
      if (text) (text.length > LONG ? (view.preview = text) : (view.detail = text));
      break;
    }
    default: {
      for (const value of [item['text'], item['description'], item['recommendation'], item['note'], item['body']]) {
        const s = str(value);
        if (s && s !== view.title) {
          view.detail = s;
          break;
        }
      }
    }
  }

  // Paragraph breaks read as spaces in a clamped preview.
  if (view.preview) view.preview = view.preview.replace(/\s*\n+\s*/g, ' ');
  // A title that runs long reads better as a passage under a short heading.
  if (!view.preview && view.title.length > LONG) {
    view.preview = view.title;
    view.title = words(item.kind).replace(/^\w/, c => c.toUpperCase());
  }
  return view;
}

/** The apps the page knows by colour; any other app gets the neutral chip. */
export const APPS: { key: string; label: string; color: string }[] = [
  { key: 'scenario-builder', label: 'Scenario Builder', color: '#4f46e5' },
  { key: 'grayspace', label: 'GraySpace', color: '#0e7490' },
  { key: 'ghosts', label: 'GHOSTS', color: '#15803d' },
  { key: 'pandora', label: 'Pandora', color: '#c026d3' },
  { key: 'excon-assistant', label: 'ExCon Assistant', color: '#d97706' },
  { key: 'aar-analyst', label: 'AAR Analyst', color: '#7c3aed' }
];

/** The known app an author's app string belongs to: exact, or a prefix such as ghosts-npc → ghosts. */
export function appOf(app: string): { key: string; label: string; color: string } {
  const lower = app.toLowerCase();
  return APPS.find(a => lower === a.key || lower.startsWith(a.key + '-') || lower.startsWith(a.key + '.') || lower.startsWith(a.key + ':'))
    ?? { key: lower, label: app, color: '#64748b' };
}

/** The chip's words: the app, and "agent" when an AI made the entry. */
export function authorLabel(author: Author): string {
  return `${author.app}${author.via === 'agent' ? ' · agent' : ''}`;
}
