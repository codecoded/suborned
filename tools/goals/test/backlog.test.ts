import { describe, expect, test } from 'bun:test';
import { CatalogSchema, complete, invalidateChanged, priorities, reconcile, validateCatalog, type Backlog, type Catalog } from '../backlog';

const sections = [
  { id: '01', text: 'The same recorded orders always give the same replay result.' },
  { id: '05', text: 'A container servicing time is learnable by the Scout using Watch.' },
  { id: '15', text: 'The street uses cream stone and seafoam bands.' },
];
function catalog(): Catalog {
  return {
    summary: 'Simulation requirements extracted from the design.',
    coverage: sections.map(s => ({ section: s.id, disposition: s.id === '15' ? 'outside-domain' : 'requirements', reason: s.id === '15' ? 'Visual asset creation is outside the authorized code domain.' : 'Runtime simulation behavior can be implemented and tested.' })),
    requirements: [
      { id: 'deterministic-replay', title: 'Deterministic replay', section: '01', quote: sections[0]!.text, acceptance: ['Recorded orders reproduce the final state hash.'], terms: ['replay', 'hash'], dependsOn: [], value: 4, effort: 2, risk: 2, rationale: 'Replay makes all generated simulation behavior reproducible.' },
      { id: 'container-watch', title: 'Scout learns container servicing time', section: '05', quote: sections[1]!.text, acceptance: ['A completed Scout Watch learns the servicing time.'], terms: ['container', 'Watch'], dependsOn: ['deterministic-replay'], value: 4, effort: 2, risk: 2, rationale: 'Knowledge enables the player to time an existing simulation system.' },
    ],
  };
}
function backlog(): Backlog { return reconcile(catalog(), 'agent-v1', 'human-v1', '0.24.0'); }
function verified(b: Backlog, index: number): void {
  const e = b.entries[index]!;
  e.status = 'verified'; e.inspectedHashes = { 'sim/Game.cs': 'source-v1', 'sim/tests.cs': 'tests-v1' };
  e.history = [{ run: 'runs/goals/fixture', status: 'verified', reason: 'Source inspection plus passing acceptance tests.' }];
  e.checks = ['passing-check']; e.proof = { testPath: 'sim/tests.cs', run: 'runs/goals/fixture' };
}

describe('discovered backlog contracts', () => {
  test('a catalog accounts for every design section without requiring art implementation', () => {
    expect(CatalogSchema.parse(catalog())).toEqual(catalog());
    expect(() => validateCatalog(catalog(), sections)).not.toThrow();
    const missing = catalog(); missing.coverage.pop();
    expect(() => validateCatalog(missing, sections)).toThrow('Every GDD section');
    const duplicate = catalog(); duplicate.coverage[2] = duplicate.coverage[0]!;
    expect(() => validateCatalog(duplicate, sections)).toThrow('Every GDD section');
  });

  test('a source quote must actually occur in its declared design section', () => {
    const invented = catalog(); invented.requirements[1]!.quote = 'Container Watch completes instantly and reveals all nearby containers.';
    expect(() => validateCatalog(invented, sections)).toThrow('no literal GDD source');
    const wrongSection = catalog(); wrongSection.requirements[1]!.section = '01';
    expect(() => validateCatalog(wrongSection, sections)).toThrow();
  });

  test('coverage and requirement declarations must agree', () => {
    const unbacked = catalog(); unbacked.coverage[2]!.disposition = 'requirements';
    expect(() => validateCatalog(unbacked, sections)).toThrow('claims requirements but has none');
    const excluded = catalog(); excluded.coverage[1]!.disposition = 'outside-domain';
    expect(() => validateCatalog(excluded, sections)).toThrow('excluded section');
  });

  test('identities and dependencies reject duplicates, missing references, self edges and cycles', () => {
    const duplicate = catalog(); duplicate.requirements.push({ ...duplicate.requirements[0]! });
    expect(() => validateCatalog(duplicate, sections)).toThrow('Duplicate requirement');
    const absent = catalog(); absent.requirements[1]!.dependsOn = ['invented-rule'];
    expect(() => validateCatalog(absent, sections)).toThrow();
    const self = catalog(); self.requirements[0]!.dependsOn = ['deterministic-replay'];
    expect(() => validateCatalog(self, sections)).toThrow('Bad dependency');
    const cycle = catalog(); cycle.requirements[0]!.dependsOn = ['container-watch'];
    expect(() => validateCatalog(cycle, sections)).toThrow('cycle');
  });
});

describe('persistent backlog reconciliation', () => {
  test('unchanged design and requirements retain verification evidence on a rerun', () => {
    const previous = backlog(); verified(previous, 0);
    const next = reconcile(catalog(), 'agent-v1', 'human-v1', '0.24.0', previous);
    expect(next.entries[0]!.status).toBe('verified');
    expect(next.entries[0]!.proof).toEqual(previous.entries[0]!.proof);
    expect(next.entries[0]!.history).toEqual(previous.entries[0]!.history);
    expect(next.entries[1]!.status).toBe('uninspected');
  });

  test('either authoritative design hash changing invalidates cached verification', () => {
    const previous = backlog(); verified(previous, 0);
    for (const [agent, human] of [['agent-v2', 'human-v1'], ['agent-v1', 'human-v2']]) {
      const next = reconcile(catalog(), agent!, human!, '0.24.0', previous);
      expect(next.entries[0]!.status).toBe('uninspected');
      expect(next.entries[0]!.proof).toBeUndefined();
      expect(next.entries[0]!.history).toEqual(previous.entries[0]!.history);
    }
  });

  test('changed acceptance criteria require new verification even with the same design quote', () => {
    const previous = backlog(); verified(previous, 0);
    const updated = catalog(); updated.requirements[0]!.acceptance.push('Replay from a mid-mission snapshot yields the same final state hash.');
    const next = reconcile(updated, 'agent-v1', 'human-v1', '0.24.0', previous);
    expect(next.entries[0]!.status).toBe('uninspected');
    expect(next.entries[0]!.proof).toBeUndefined();
  });

  test('removed requirements remain auditable as retired, never active work', () => {
    const previous = backlog(); verified(previous, 1);
    const changed = catalog(); changed.requirements.pop(); changed.coverage[1]!.disposition = 'outside-domain';
    const next = reconcile(changed, 'agent-v2', 'human-v2', '0.25.0', previous);
    expect(next.entries).toHaveLength(1);
    expect(next.retired.map(e => e.requirement.id)).toEqual(['container-watch']);
    expect(next.retired[0]!.proof).toEqual(previous.entries[1]!.proof);
  });
});

describe('evidence freshness and deterministic priority', () => {
  test('changed, deleted, or empty inspected evidence makes verification stale', () => {
    const variants: Record<string, string>[] = [{ 'sim/Game.cs': 'source-v2', 'sim/tests.cs': 'tests-v1' }, { 'sim/Game.cs': 'source-v1' }, {}];
    for (const current of variants) {
      const b = backlog(); verified(b, 0); invalidateChanged(b, current);
      expect(b.entries[0]!.status).toBe('uninspected'); expect(complete(b)).toBe(false);
    }
    const empty = backlog(); verified(empty, 0); empty.entries[0]!.inspectedHashes = {};
    invalidateChanged(empty, {}); expect(empty.entries[0]!.status).toBe('uninspected');
    const matching = backlog(); verified(matching, 0);
    invalidateChanged(matching, { 'sim/Game.cs': 'source-v1', 'sim/tests.cs': 'tests-v1', 'sim/Other.cs': 'irrelevant' });
    expect(matching.entries[0]!.status).toBe('verified');
  });

  test('a gap assessment is stale after its inspected source changes too', () => {
    for (const status of ['missing', 'partial', 'blocked'] as const) {
      const b = backlog(); b.entries[0]!.status = status; b.entries[0]!.inspectedHashes = { 'sim/Game.cs': 'source-v1' };
      invalidateChanged(b, { 'sim/Game.cs': 'source-v2' });
      expect(String(b.entries[0]!.status)).toBe('uninspected');
    }
  });

  test('prerequisite verification gates work and priority explains every score component', () => {
    const b = backlog(); let rows = priorities(b);
    expect(rows.map(r => [r.id, r.score, r.eligible])).toEqual([['deterministic-replay', 14, true], ['container-watch', 12, false]]);
    expect(rows[0]).toMatchObject({ value: 4, unblocks: 1, effort: 2, risk: 2 });
    verified(b, 0); rows = priorities(b);
    expect(rows.find(r => r.id === 'deterministic-replay')!.eligible).toBe(false);
    expect(rows.find(r => r.id === 'container-watch')!.eligible).toBe(true);
    b.entries[1]!.status = 'blocked'; expect(priorities(b).find(r => r.id === 'container-watch')!.eligible).toBe(false);
  });

  test('equal scores use stable IDs and no empty, unknown, or partial backlog is complete', () => {
    const b = backlog(); b.entries[1]!.requirement.dependsOn = [];
    expect(priorities(b).map(r => r.id)).toEqual(['container-watch', 'deterministic-replay']);
    for (const status of ['uninspected', 'missing', 'partial', 'blocked', 'unknown'] as const) {
      verified(b, 0); b.entries[1]!.status = status; expect(complete(b)).toBe(false);
    }
    verified(b, 1); expect(complete(b)).toBe(true);
    b.entries = []; expect(complete(b)).toBe(false);
  });
});
