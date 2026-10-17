// Open needs (brief 017, phase 1): the kinds and the catalogue of records as the expander reads them. A person's need stays a kind (the engine and
// every rule read the kind); the record is drawn after it, from the person's own stream, among the catalogue's records of that kind in their area.
// Absent (a pinned older run, a ctx made without them) there are no records and the people are what they were. Checked by needs-check.ts (N-1..N-8).
import { Catalogue, NeedsRules, parseCatalogue, parseNeedsRules, type NeedRecord, type SecretRecord } from '../../contracts/people/needs.schema';
import { rng } from '../world/gen/common';
import type { Rules } from './rules';

export const NEEDS_RULES_FILE = 'contracts/people/needs.rules.json';
export const catalogueFileOf = (planId: string) => `contracts/people/${planId}.needs.json`;
export type Needs = { rules: NeedsRules; catalogue: Catalogue };
export type NeedsTexts = { rules: string; catalogue: string };
export { Catalogue, NeedsRules };

export const parseNeeds = (t: NeedsTexts): Needs => ({ rules: parseNeedsRules(t.rules), catalogue: parseCatalogue(t.catalogue) });

/** The effective approaches of a record: its kind's, narrowed by the record's own (never widened: N-5). */
export const metByOf = (n: Needs, r: NeedRecord): readonly string[] => { const k = n.rules.kinds[r.kind]?.metBy ?? []; return r.metBy ? r.metBy.filter(a => (k as readonly string[]).includes(a)) : k; };

/** The records of a kind in an area that a person of `temperament` can still be helped by (the record's metBy meets an approach they answer to). */
export const needCandidates = (n: Needs, rules: Rules, area: string, kind: string, temperament: string): NeedRecord[] => {
  const t = rules.temperaments[temperament as keyof Rules['temperaments']];
  return n.catalogue.needs.filter(r => r.area === area && r.kind === kind && (!t || metByOf(n, r).some(a => (t.respondsTo as readonly string[]).includes(a))));
};
export const secretCandidates = (n: Needs, area: string, kind: string): SecretRecord[] => n.catalogue.secrets.filter(r => r.area === area && r.kind === kind);

/** The record a person draws: seeded by their id alone (a stream no other draw uses, so nothing else in the expansion moves). undefined: the catalogue has none for them (the expansion reports it). */
export const drawNeedRecord = (n: Needs, rules: Rules, id: string, area: string, kind: string, temperament: string): NeedRecord | undefined => {
  const c = needCandidates(n, rules, area, kind, temperament);
  return c.length ? rng(`${id}/need-record`).pick(c) : undefined;
};
export const drawSecretRecord = (n: Needs, id: string, area: string, kind: string): SecretRecord | undefined => {
  const c = secretCandidates(n, area, kind);
  return c.length ? rng(`${id}/secret-record`).pick(c) : undefined;
};
