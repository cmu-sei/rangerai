// The builder API's GraySpace views (camelCase), and GraySpace's own JSON as its Terrain API 1.7.0 writes it (snake_case),
// which the builder API passes through. Only the fields the Deploy to GraySpace page reads are typed.

/** GET /api/grayspace. The token is never in it. The address is a deployment's base URL (http://host/deployments/abc). */
export interface GraySpaceStatus {
  connected: boolean;
  url?: string;
  connectedBy?: string;
  since?: string;
  /** Whether the connected GraySpace answered just now, and why not when it didn't. */
  answering?: boolean;
  problem?: string;
}

export interface GsImage {
  name: string;
  type: string;
  os_family?: string;
  description?: string;
  cores?: number;
  memory?: number;
  disk?: number;
}

export interface GsRoleVariable {
  description: string;
  required?: boolean;
}

export interface GsRole {
  name: string;
  description: string;
  os_families: string[];
  variables?: Record<string, GsRoleVariable>;
}

export interface GsServer {
  hostname: string;
}

/** The server part of GET /capabilities: this GraySpace, with or without a deployed base. */
export interface GsServerInfo {
  jobs: boolean;
  jobs_unavailable_reason?: string | null;
  base_jobs?: boolean;
  base_jobs_unavailable_reason?: string | null;
  slice_features: string[];
  management_subnet: string;
  slice_pool: string;
  images: GsImage[];
  roles: GsRole[];
  capacity?: { node: string; cpus?: number | null; memory_free?: number | null; memory_total?: number | null } | null;
  /** The deployed base, or null before one is. */
  base?: { name: string; scope: string } | null;
}

/** GET /capabilities: what the deployed base offers. Only its base parts are read here; the rest are read as the server part. */
export interface GsCapabilities {
  dns: { tlds: string[]; hosting: GsServer[]; resolvers: GsServer[] };
  cas: { hostname: string; name: string }[];
  isps: { name: string; asn?: number | null; locations: string[] }[];
}

export interface GsBase {
  version?: { name?: string | null; scope: string } | null;
  status: string;
  routers: { expected: number; running?: number | null };
  bgp: { total?: number | null; established?: number | null };
}

/** A base version GraySpace ships; one whose files fail to load lists its problems. */
export interface GsBaseVersion {
  name: string;
  deployed: boolean;
  problems?: string[];
}

/** GET /api/grayspace/base/preflight: whether GraySpace's site can deploy a base version at a scope. */
export interface GsPreflight {
  ready: boolean;
  version?: string | null;
  scope?: string | null;
  checks: { name: string; status: 'ok' | 'warning' | 'error' | 'unchecked'; message: string }[];
}

export interface GsAttachmentPoint {
  router: string;
  isp: string;
  asn?: number | null;
  location?: string | null;
}

export interface GsSliceSummary {
  slug: string;
  kind: string;
  description: string;
  latest?: string | null;
  versions?: { version: string; created: string }[];
}

/** GET /api/grayspace/offers. A part GraySpace couldn't give is missing, with its reason under problems. */
export interface GraySpaceOffers {
  server?: GsServerInfo;
  capabilities?: GsCapabilities;
  base?: GsBase;
  versions?: GsBaseVersion[];
  attachmentPoints?: GsAttachmentPoint[];
  map?: { features: { id: string; properties: { kind: string; type?: string; state?: string } }[] };
  slices?: GsSliceSummary[];
  problems: Record<string, string>;
}

/** Where a site's edge, or an internet host, joins the base: an ISP, a location or an exact router. */
export interface GsAttach {
  isp?: string;
  location?: string;
  router?: string;
}

export interface GsSegment {
  name: string;
  cidr?: string;
}

export interface GsSite {
  name: string;
  attach: GsAttach;
  routed?: boolean;
  segments: GsSegment[];
}

export interface GsHost {
  name: string;
  image: string;
  count?: number | null;
  segment?: string | null;
  internet?: GsAttach | null;
  roles?: string[];
  size?: { cores: number; memory: number; disk: number } | null;
  labels?: Record<string, string>;
  publish?: boolean;
}

export interface GsDnsDomain {
  domain: string;
  hosting_server: string;
}

export interface GsDnsRecord {
  name: string;
  type: string;
  host?: string | null;
  data?: string | null;
  ttl?: number;
}

/** A certificate a host serves, issued by a base CA. */
export interface GsTls {
  host: string;
  names: string[];
  ca: string;
}

/** The slice spec (grayspace-slice-v1) the page edits. Fields it doesn't edit are kept as they are. */
export interface SliceSpec {
  apiVersion: string;
  kind: string;
  slug: string;
  prefix: string;
  description?: string;
  owner?: string | null;
  sites?: GsSite[];
  hosts?: GsHost[];
  role_vars?: Record<string, Record<string, string>>;
  dns?: { domains?: GsDnsDomain[]; records?: GsDnsRecord[] };
  tls?: GsTls[];
  [other: string]: unknown;
}

/** GET/PUT /api/scenarios/{id}/enclave */
export interface EnclaveView {
  spec: SliceSpec;
  version?: string;
  instanceId?: string;
  jobId?: string;
  updatedBy?: string;
  updated?: string;
}

/** What GraySpace answered an enclave action, and the enclave after it. */
export interface EnclaveAnswer<T = unknown> {
  enclave: EnclaveView;
  graySpace?: T;
}

export interface GsFinding {
  code: string;
  severity: string;
  path: string;
  message: string;
  suggestion?: unknown;
}

export interface GsValidateResult {
  valid: boolean;
  version: string;
  findings: GsFinding[];
}

export interface GsPutSliceResult {
  slug: string;
  version: string;
  created: boolean;
  findings: GsFinding[];
}

export interface GsJob {
  id: string;
  kind: string;
  state: 'queued' | 'running' | 'succeeded' | 'failed' | 'cancelled';
  stages?: string[];
  stage?: string | null;
  attempt?: string | null;
  error?: string | null;
  finished?: string | null;
  result?: Record<string, unknown> | null;
  /** Each stage's title, keyed by stage. */
  stage_titles?: Record<string, string>;
}

export interface GsInstance {
  id: string;
  state: string;
  error?: string | null;
  /** The manifest, as GraySpace's InstanceView gives it: filled in as the instance's job allocates and builds it. */
  sites?: GsManifestSite[];
  hosts?: GsManifestHost[];
  dns?: { name: string; type: string; data: string }[];
  certificates?: { host: string; names: string[]; ca: string; serial?: string | null }[];
  stages_done?: string[];
}

export interface GsManifestSegment {
  name: string;
  cidr: string;
  gateway?: string;
}

export interface GsManifestSite {
  name: string;
  router: string;
  isp?: string;
  location?: string | null;
  wan_ip?: string;
  routed?: boolean;
  public_prefix?: string | null;
  edge?: string;
  segments?: GsManifestSegment[];
}

/** One built host; a host with a count is several, named <name>-01 and on. */
export interface GsManifestHost {
  name: string;
  hostname?: string;
  site?: string | null;
  segment?: string | null;
  ip?: string;
  public_ip?: string | null;
  mgmt_ip?: string | null;
  fqdns?: string[];
  roles?: string[];
  labels?: Record<string, string>;
  image?: string;
  /** Proxmox status: running, stopped or unknown. */
  status?: string;
}

/** GET /api/grayspace/base/job: GraySpace's latest base job and its log; neither before the first. */
export interface BaseJobView {
  job?: GsJob | null;
  log?: string | null;
}

/** GET /api/scenarios/{id}/enclave/instance */
export interface FollowView {
  enclave: EnclaveView;
  instance?: GsInstance;
  job?: GsJob;
  log?: string;
}
