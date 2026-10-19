import { z } from 'zod';
import { hash } from '../rag/corpus';

const identifier = z.string().regex(/^[a-z][a-z0-9-]{2,79}$/);
export const RequirementSchema = z.object({
  id: identifier, title: z.string().min(8), section: z.string(), quote: z.string().min(20),
  acceptance: z.array(z.string().min(12)).min(1).max(8), terms: z.array(z.string().min(2)).min(2).max(8),
  dependsOn: z.array(identifier), value: z.number().int().min(1).max(5), effort: z.number().int().min(1).max(5), risk: z.number().int().min(1).max(5),
  rationale: z.string().min(12),
}).strict();
export const CatalogSchema = z.object({
  summary: z.string(),
  coverage: z.array(z.object({ section: z.string(), disposition: z.enum(['requirements', 'outside-domain']), reason: z.string().min(12) }).strict()),
  requirements: z.array(RequirementSchema).min(1).max(120),
}).strict();
export type Requirement = z.infer<typeof RequirementSchema>;
export type Catalog = z.infer<typeof CatalogSchema>;
export const InspectionSchema = z.object({
  state: z.enum(['inspect', 'implemented', 'missing', 'partial', 'blocked', 'unknown']), explanation: z.string().min(12),
  evidence: z.array(z.object({ path: z.string(), quote: z.string().min(12) }).strict()),
  reads: z.array(z.object({ path: z.string(), from: z.number().int().positive(), to: z.number().int().positive() }).strict()).max(10),
  searches: z.array(z.string().min(2)).max(8),
  tests: z.array(z.string().regex(/^[A-Za-z_][A-Za-z0-9_.]+$/)).max(12),
  writable: z.array(z.string()).max(10),
}).strict();
export type Inspection = z.infer<typeof InspectionSchema>;
export type Entry = { requirement: Requirement; anchor: string; status: 'uninspected' | 'verified' | 'missing' | 'partial' | 'blocked' | 'unknown';
  explanation: string; inspection?: Inspection; inspectedHashes: Record<string, string>; checks?: string[]; checkMinimums?: Record<string, number>;
  proof?: { testPath: string; run: string }; featureDirectory?: string; history: { run: string; status: string; reason: string }[] };
export type Backlog = { version: 2; gddHash: string; humanHash: string; gddVersion: string; goal?: string; catalog: Catalog; catalogOrigin?: string; indexHash?: string; entries: Entry[]; retired: Entry[] };
type Section = { id: string; text: string };

export function validateCatalog(catalog: Catalog, sections: Section[]) {
  const ids = new Set(catalog.requirements.map(r => r.id));
  if (ids.size !== catalog.requirements.length) throw new Error('Duplicate requirement identity');
  if (catalog.coverage.length !== sections.length || new Set(catalog.coverage.map(c => c.section)).size !== sections.length
      || sections.some(s => !catalog.coverage.some(c => c.section === s.id))) throw new Error('Every GDD section must be accounted for exactly once');
  for (const c of catalog.coverage) if (c.disposition === 'requirements' && !catalog.requirements.some(r => r.section === c.section)) throw new Error(`Section ${c.section} claims requirements but has none`);
  for (const r of catalog.requirements) {
    if (!sections.find(s => s.id === r.section)?.text.includes(r.quote)) throw new Error(`Requirement ${r.id} has no literal GDD source`);
    if (catalog.coverage.find(c => c.section === r.section)?.disposition !== 'requirements') throw new Error(`Requirement in excluded section ${r.section}`);
    if (r.dependsOn.some(id => !ids.has(id) || id === r.id)) throw new Error(`Bad dependency for ${r.id}`);
  }
  const visiting = new Set<string>(), visited = new Set<string>();
  const visit = (id: string) => {
    if (visiting.has(id)) throw new Error('Requirement dependency cycle');
    if (visited.has(id)) return;
    visiting.add(id);
    for (const d of catalog.requirements.find(x => x.id === id)!.dependsOn) visit(d);
    visiting.delete(id); visited.add(id);
  };
  for (const r of catalog.requirements) visit(r.id);
}

export const anchorOf = (r: Requirement) => hash(JSON.stringify([r.section, r.quote, r.title, r.acceptance]));
export function reconcile(catalog: Catalog, gddHash: string, humanHash: string, gddVersion: string, previous?: Backlog): Backlog {
  const same = previous?.gddHash === gddHash && previous?.humanHash === humanHash;
  const entries: Entry[] = catalog.requirements.map(requirement => {
    const anchor = anchorOf(requirement), prior = previous?.entries.find(e => e.anchor === anchor);
    return prior && same ? { ...prior, requirement } : { requirement, anchor, status: 'uninspected', explanation: 'Needs current code inspection', inspectedHashes: {}, history: prior?.history ?? [] };
  });
  return { version: 2, gddHash, humanHash, gddVersion, catalog, entries, ...(same ? { catalogOrigin: previous.catalogOrigin, indexHash: previous.indexHash } : {}),
    retired: [...(previous?.retired ?? []), ...(previous?.entries.filter(e => !entries.some(n => n.anchor === e.anchor)) ?? [])] };
}

export function invalidateChanged(backlog: Backlog, current: Record<string, string>) {
  for (const e of backlog.entries) if (e.status !== 'uninspected' && (Object.keys(e.inspectedHashes).length === 0 || Object.entries(e.inspectedHashes).some(([p, sha]) => current[p] !== sha))) {
    e.status = 'uninspected'; e.explanation = 'Implementation evidence changed; previous verification is stale';
  }
}
export function priorities(backlog: Backlog) {
  return backlog.entries.map(e => {
    const r = e.requirement, unblocks = backlog.entries.filter(x => x.requirement.dependsOn.includes(r.id)).length;
    return { id: r.id, score: 4 * r.value + 2 * unblocks - r.effort - r.risk, value: r.value, unblocks, effort: r.effort, risk: r.risk,
      status: e.status, eligible: !['verified', 'blocked'].includes(e.status) && r.dependsOn.every(d => backlog.entries.find(x => x.requirement.id === d)?.status === 'verified'), reason: r.rationale };
  }).sort((a, b) => b.score - a.score || a.id.localeCompare(b.id));
}
export const complete = (backlog: Backlog) => backlog.entries.length > 0 && backlog.entries.every(e => e.status === 'verified');

export const TestPlanSchema = z.object({ summary: z.string(), className: z.string().regex(/^Goal[A-Za-z0-9]+Tests$/), code: z.string().min(100).max(40000), minTests: z.number().int().min(1).max(20), covers: z.array(z.string()).min(1) }).strict();
export type TestPlan = z.infer<typeof TestPlanSchema>;
