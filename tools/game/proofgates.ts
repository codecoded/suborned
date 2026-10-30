// What the built player's proof must say (`bun run game:check`, and the batch's game-check step through it). The proof (GameBoot.cs, -gameProof) writes one JSON object per
// section under `frames` in proof.json; a gated section carries `ok`: true passes, false fails, null ("not reached": the fight gave no hostile in range, say) is reported and
// goes on, and a section the build wrote as null (its code threw or never ran) fails. A section the proof does not have is an older build: said, not failed.
// Latch 2026-10-21 (ProofInput.cs) wrote the first of these; r118 is batch 0.2.118's (cameras, signals, Glint's frames); r059 is brief 059's (the rival car's way in, the Watch,
// Vale driven round, the Take, the Drive; Proof059.cs, 0.2.123), gated from 2026-10-26 (Splice, briefs/067/splice.md item 3); r067 is brief 067's (Sky's notes on 0.2.122: the
// square and the cars, cones on demand, the portrait and third person, locked ways, the squad Board and Drive, the conversation from the ground, the lock reticle, the host
// record; Proof067.cs, 0.2.123, run after r059), gated the same day (briefs/067/splice.md follow-up 2). r123 (Proof123.cs: host round 4's looks and fights) is NOT gated: its pictures are in
// tools/game/proofset.json (budget and 2x crops) from 2026-10-26, and its section joins GATED when Glyph agrees its density (Aster).

/** The proof sections that gate the check, in the order GameBoot writes them. */
export const GATED = ['driveClick', 'fireStop', 'ringCheck', 'inputMap', 'act', 'fire', 'cars', 'hudClick', 'dblClick', 'holdActs', 'holdRange', 'leash', 'done', 'rings', 'levels', 'r29', 'r118', 'r059', 'r067'] as const;

export type Gates = { ok: boolean; lines: string[]; failed: string[]; notReached: string[]; missing: string[]; /** a delta proof's sections it did not run: never a pass; the gate takes only what planProof derives from the ledger (tools/game/proofdelta.ts) */ skipped: string[] };

/** A section's fields after `ok`, as the gate line shows them: a section made of parts that each carry their own `ok` (r059's arrive, watch and pickup; r067's nine) shows the parts that
 *  failed first and names them, so the cut at 600 characters never hides the one that failed. */
function partsLine(rest: Record<string, unknown>, failed: boolean): string {
  const bad = failed ? Object.entries(rest).filter(([, v]) => v !== null && typeof v === 'object' && (v as { ok?: unknown }).ok === false) : [];
  if (!bad.length) return JSON.stringify(rest).slice(0, 600);
  const first = Object.fromEntries(bad), others = Object.fromEntries(Object.entries(rest).filter(([key]) => !(key in first)));
  return `failing parts ${bad.map(([key]) => key).join(', ')}: ${JSON.stringify({ ...first, ...others }).slice(0, 600)}`;
}

/** One line per section (the section's own fields after `ok`, cut at 600 characters, a failed part first) and whether every present section passes. `frames` is proof.json's `frames`. */
export function judgeGates(frames: Record<string, unknown> | undefined | null, gated: readonly string[] = GATED, skips: Record<string, string> = {}): Gates {
  const out: Gates = { ok: true, lines: [], failed: [], notReached: [], missing: [], skipped: [] };
  for (const k of gated) {
    const r = frames?.[k];
    if (r === undefined) { out.lines.push(`game: ${k}: not in this proof (an older build)`); out.missing.push(k); continue; }
    if (r === null && skips[k]) { out.lines.push(`game: ${k}: ${skips[k]}`); out.skipped.push(k); continue; } // null: the delta proof did not run it; a skip with no earlier pass is not in `skips`, so it falls through and fails
    if (r === null) { out.lines.push(`game: ${k} FAILED: the section is null (its code threw or never ran)`); out.ok = false; out.failed.push(k); continue; }
    const { ok, ...rest } = r as { ok?: unknown } & Record<string, unknown>;
    out.lines.push(`game: ${k} ${ok === true ? 'ok' : ok === false ? 'FAILED' : 'not reached'}: ${partsLine(rest, ok === false)}`);
    if (ok === null || ok === undefined) out.notReached.push(k);
    if (ok === false) { out.ok = false; out.failed.push(k); }
  }
  return out;
}
