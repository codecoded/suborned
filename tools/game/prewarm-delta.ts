// Brief 096 (Latch): the shipped shader warm-up (unity/Assets/Suborned/Resources/Prewarm/play-metal.graphicsstate, ShaderPrewarm.cs) is the
// states the perspective views add to the play view: a recording of the whole run (HitchTrace096 with -gscTrace, both orders, appended) minus a
// recording of the play view alone (-hitchStop map). The play view's own states compile with the street's first frames as before, so the loading bar
// waits only for the third-person, drive and cutaway states (the cascaded-shadow variants). A GraphicsStateCollection file is JSON: variants keyed by
// hash, each with its graphics states (vertex layout, render pass, render state ids, ...); a state is dropped when the base has the same variant with it.
// usage: bun tools/game/prewarm-delta.ts <full.graphicsstate> <base.graphicsstate> <out.graphicsstate>
import { readFileSync, writeFileSync } from 'node:fs';

type Entry<K, V> = { first: K; second: V };
type Variant = { shaderName: string; keywordNames: string; graphicsStateInfoSet: unknown[] };
type Gsc = { m_VariantInfoMap: Entry<{ Hash: string }, Variant>[]; m_VertexLayoutInfoMap: Entry<string, unknown>[]; m_RenderPassInfoMap: Entry<string, unknown>[]; m_RenderStateMap: Entry<string, unknown>[]; [k: string]: unknown };

/** Ids stay strings: they are 64-bit and JSON numbers would round them. */
const read = (f: string): { gsc: Gsc; raw: string } => {
  const raw = readFileSync(f, 'utf8');
  return { raw, gsc: JSON.parse(raw.replace(/("(?:first|vertexLayout|renderPass|renderState)":\s*)(\d+)/g, '$1"$2"')) };
};
const write = (f: string, g: Gsc) => writeFileSync(f, JSON.stringify(g, null, 4).replace(/("(?:first|vertexLayout|renderPass|renderState)":\s*)"(\d+)"/g, '$1$2') + '\n');

export function delta(full: Gsc, base: Gsc): Gsc {
  const had = new Map<string, Set<string>>();
  for (const v of base.m_VariantInfoMap) had.set(v.first.Hash, new Set(v.second.graphicsStateInfoSet.map(s => JSON.stringify(s))));
  const variants = full.m_VariantInfoMap
    .map(v => ({ ...v, second: { ...v.second, graphicsStateInfoSet: v.second.graphicsStateInfoSet.filter(s => !had.get(v.first.Hash)?.has(JSON.stringify(s))) } }))
    .filter(v => v.second.graphicsStateInfoSet.length > 0 || !had.has(v.first.Hash)); // a variant recorded with no state (its program alone) stays unless the base has it
  const used = (k: 'vertexLayout' | 'renderPass' | 'renderState') => new Set(variants.flatMap(v => v.second.graphicsStateInfoSet.map(s => String((s as Record<string, unknown>)[k]))));
  const vl = used('vertexLayout'), rp = used('renderPass'), rs = used('renderState');
  return { ...full, m_VariantInfoMap: variants, m_VertexLayoutInfoMap: full.m_VertexLayoutInfoMap.filter(e => vl.has(String(e.first))), m_RenderPassInfoMap: full.m_RenderPassInfoMap.filter(e => rp.has(String(e.first))), m_RenderStateMap: full.m_RenderStateMap.filter(e => rs.has(String(e.first))) };
}

if (import.meta.main) {
  const [fullF, baseF, outF] = process.argv.slice(2);
  if (!fullF || !baseF || !outF) { console.error('usage: bun tools/game/prewarm-delta.ts <full.graphicsstate> <base.graphicsstate> <out.graphicsstate>'); process.exit(2); }
  const full = read(fullF).gsc, base = read(baseF).gsc, out = delta(full, base);
  const states = (g: Gsc) => g.m_VariantInfoMap.reduce((n, v) => n + v.second.graphicsStateInfoSet.length, 0);
  write(outF, out);
  console.log(`prewarm-delta: full ${full.m_VariantInfoMap.length} variants / ${states(full)} states, base ${base.m_VariantInfoMap.length} / ${states(base)}, kept ${out.m_VariantInfoMap.length} / ${states(out)} → ${outF}`);
}
