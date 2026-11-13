import { Audience, Entry, PartKey } from './chronicle.model';

/** Something an app did, as it happened (GET/POST /api/chronicles/{id}/activity). */
export interface Activity {
  id: string;
  chronicleId: string;
  source: string;
  subscriberId?: string;
  actor?: string;
  kind: string;
  text?: string;
  host?: string;
  at: string;
  visibility: Audience;
  data?: Record<string, unknown>;
  /** A notable activity's stored entry. */
  sequence?: number;
}

/** What a subscriber's agent writes. Exercise control and designers only. */
export interface AgentSpec {
  system: string;
  task: string;
  maxTokens?: number;
  output: { part: PartKey; item: Record<string, unknown>; author: { app: string; actor?: string; via: string }; reason?: string };
}

/** An app or agent reading the event through its own view. */
export interface Subscriber {
  id: string;
  chronicleId: string;
  app: string;
  name: string;
  role?: string;
  audience: Audience;
  viewers?: string[];
  parts?: PartKey[];
  kinds?: string[];
  model?: string;
  agent?: AgentSpec;
  hasAgent?: boolean;
  query: string;
  registered: string;
  lastSeen?: string;
  lastActivity?: string;
  activityCount?: number;
  published?: number;
  lastSequence?: number;
}

/** What the live WebSocket sends. */
export type LiveMessage =
  | { type: 'hello'; chronicleId: string; audience: Audience; subscribers: Subscriber[]; activity: Activity[] }
  | { type: 'entry'; entry: Entry }
  | { type: 'activity'; activity: Activity }
  | { type: 'subscriber'; subscriber: Subscriber }
  | { type: 'token'; subscriberId: string; runId: string; text: string };

/** The connection's state, for the LIVE pill. */
export type LiveState = 'connecting' | 'live' | 'reconnecting' | 'off';
