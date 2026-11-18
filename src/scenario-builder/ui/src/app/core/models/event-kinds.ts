import { EventKind } from './builder.model';

/** The kinds of event, in the order scenario types are listed by kind, each summed up in a sentence. */
export const EVENT_KINDS: { kind: EventKind; label: string; summary: string }[] = [
  { kind: 'training', label: 'Training', summary: 'One learner builds a skill at their own pace, with each task checked as they go.' },
  { kind: 'exercise', label: 'Exercises', summary: 'Teams of people work through a scenario together, from a discussion at the table to live fire on a range.' },
  { kind: 'experiment', label: 'Experiments', summary: 'Only machines take part, so agents and automation can be tested and measured on the range.' }
];

/** <paramref name="types"/> by kind of event, in {@link EVENT_KINDS} order, leaving out kinds with none. No kind means exercise. */
export function byKind<T extends { kind?: EventKind }>(types: T[]) {
  return EVENT_KINDS.map(k => ({ ...k, types: types.filter(t => (t.kind ?? 'exercise') === k.kind) })).filter(g => g.types.length);
}
