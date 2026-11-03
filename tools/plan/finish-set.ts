// The road finish set (brief 055 road test, Vela 2026-10-24): which finish the plan lays on its carriageways. One key, standards.streetscape.roads.finish.set:
//   "enamel"  (default, today's road: the testbed's powder-blue enamel, pearl lozenges and chrome seams)
//   "asphalt" (the option: dark asphalt, white enamel lane marks by rule, the same pearl kerb, shoulder and ribbon)
// A set other than the base lists its own `finish.sets.<name>.byClass` (class -> key in finish.keys), `marks` ('lozenges' | 'lanes') and `drop` (families it does not lay); the base set is finish.byClass with lozenges, so a plan
// without `set` or `sets` reads exactly as before (a pinned plan replays as it was made).
export type FinishSet = { name: string; byClass: Record<string, string>; marks: 'lozenges' | 'lanes'; /** road finish families this set does not lay (ribbon, shoulder; the pinstripe is retired, 072-G): the clean asphalt is the pearl kerb and the road, nothing stacked beside it */ drop: string[] };

/** The finish set the plan's roads standard names (or the base set). Throws on a set the plan does not list. */
export function finishSet(R: any): FinishSet {
  const fin = R?.finish ?? {}, name: string = fin.set ?? 'enamel', sets = fin.sets ?? {};
  if (name === 'enamel' && !sets.enamel) return { name, byClass: fin.byClass ?? {}, marks: 'lozenges', drop: [] };
  const s = sets[name];
  if (!s) throw new Error(`roads.finish.set is ${JSON.stringify(name)}, which roads.finish.sets does not list (${Object.keys(sets).join(', ') || 'none'}; the base set is "enamel")`);
  return { name, byClass: s.byClass ?? {}, marks: s.marks ?? 'lozenges', drop: s.drop ?? [] };
}

/** Every set the plan lists, the base first (the plan checker holds each). */
export function finishSets(R: any): FinishSet[] {
  const fin = R?.finish ?? {}, names = new Set<string>(['enamel', ...Object.keys(fin.sets ?? {})]);
  return [...names].map(n => finishSet({ finish: { ...fin, set: n } }));
}

/** A copy of the plan with the finish set switched (the export's --road-finish, a test): the committed plan's own `set` is the default. */
export function withFinishSet<P extends { standards: any }>(plan: P, name: string | undefined): P {
  if (!name) return plan;
  const p = structuredClone(plan), R = p.standards?.streetscape?.roads;
  if (!R?.finish) throw new Error('the plan has no standards.streetscape.roads.finish: no finish set to switch');
  R.finish.set = name; finishSet(R);
  return p;
}
