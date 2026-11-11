export type EventKind = 'training' | 'exercise' | 'simulation';
export type Audience = 'participant' | 'excon' | 'designer';
export type PartKey = 'world' | 'intent' | 'asBuilt' | 'asRun' | 'lessons';

/** The Scenario Builder's scenario type an event was designed from. */
export interface ScenarioTypeRef {
  id: string;
  version?: string;
}

/** What `GET /api/chronicles` returns for each event. */
export interface ChronicleSummary {
  id: string;
  name: string;
  kind: EventKind;
  scenarioType?: ScenarioTypeRef;
  created: string;
  entries: number;
}

/** One item in a part. Each kind has its own fields; the list only needs a few common ones. */
export interface Item {
  kind: string;
  id: string;
  visibility: Audience;
  standing?: 'stated' | 'proposed';
  [field: string]: unknown;
}

export interface Entry {
  id: string;
  sequence: number;
  recordedAt: string;
  part: PartKey;
  kind: string;
  op: string;
  itemId: string;
  author: { app: string; actor?: string; via: string };
}

/** What `GET /api/chronicles/{id}` returns: one event, as an audience sees it. */
export interface ChronicleRecord {
  schemaVersion: string;
  id: string;
  name: string;
  kind: EventKind;
  scenarioType?: ScenarioTypeRef;
  description?: string;
  created: string;
  seededFrom?: string[];
  world: Item[];
  intent: Item[];
  asBuilt: Item[];
  asRun: Item[];
  lessons: Item[];
  history: Entry[];
}

/** The five parts, in the order an event fills them. */
export const PARTS: { key: PartKey; label: string; stage: string }[] = [
  { key: 'world', label: 'World', stage: 'Imagine' },
  { key: 'intent', label: 'Intent', stage: 'Imagine' },
  { key: 'asBuilt', label: 'As Built', stage: 'Build' },
  { key: 'asRun', label: 'As Run', stage: 'Run' },
  { key: 'lessons', label: 'Lessons', stage: 'Remember' }
];
