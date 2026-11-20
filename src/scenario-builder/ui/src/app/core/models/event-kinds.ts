import { EventKind } from './builder.model';

/** The kinds of event, in the order scenario types are listed by kind, each summed up in a sentence. */
export const EVENT_KINDS: { kind: EventKind; label: string; summary: string }[] = [
  { kind: 'training', label: 'Training', summary: 'An individual practices a skill at their own pace. Each task includes a check.' },
  { kind: 'exercise', label: 'Exercises', summary: 'A team works through a tabletop discussion or trains on a range.' },
  { kind: 'experiment', label: 'Experiments', summary: 'Test agents and automation on a range with machine participants only.' }
];

/** <paramref name="types"/> by kind of event, in {@link EVENT_KINDS} order, leaving out kinds with none. No kind means exercise. */
export function byKind<T extends { kind?: EventKind }>(types: T[]) {
  return EVENT_KINDS.map(k => ({ ...k, types: types.filter(t => (t.kind ?? 'exercise') === k.kind) })).filter(g => g.types.length);
}
