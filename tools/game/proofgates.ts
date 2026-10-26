// What the built player's proof must say (`bun run game:check`, and the batch's game-check step through it). The proof (GameBoot.cs, -gameProof) writes one JSON object per
// section under `frames` in proof.json; a gated section carries `ok`: true passes, false fails, null ("not reached": the fight gave no hostile in range, say) is reported and
// goes on, and a section the build wrote as null (its code threw or never ran) fails. A section the proof does not have is an older build: said, not failed.
// Latch 2026-10-21 (ProofInput.cs) wrote the first of these; r118 is batch 0.2.118's (cameras, signals, Glint's frames); r059 is brief 059's (the rival car's way in, the Watch,
// Vale driven round, the Take, the Drive; Proof059.cs, 0.2.123), gated from 2026-10-26 (Splice, briefs/067/splice.md item 3).

/** The proof sections that gate the check, in the order GameBoot writes them. */
export const GATED = ['driveClick', 'fireStop', 'ringCheck', 'inputMap', 'act', 'fire', 'cars', 'hudClick', 'dblClick', 'holdActs', 'holdRange', 'leash', 'done', 'rings', 'levels', 'r29', 'r118', 'r059'] as const;

export type Gates = { ok: boolean; lines: string[]; failed: string[]; notReached: string[]; missing: string[] };

/** One line per section (the section's own fields after `ok`, cut at 600 characters) and whether every present section passes. `frames` is proof.json's `frames`. */
export function judgeGates(frames: Record<string, unknown> | undefined | null, gated: readonly string[] = GATED): Gates {
  const out: Gates = { ok: true, lines: [], failed: [], notReached: [], missing: [] };
  for (const k of gated) {
    const r = frames?.[k];
    if (r === undefined) { out.lines.push(`game: ${k}: not in this proof (an older build)`); out.missing.push(k); continue; }
    const { ok, ...rest } = (r ?? {}) as { ok?: unknown } & Record<string, unknown>;
    out.lines.push(`game: ${k} ${ok === true ? 'ok' : ok === false ? 'FAILED' : 'not reached'}: ${JSON.stringify(rest).slice(0, 600)}`);
    if (ok === null || ok === undefined) out.notReached.push(k);
    if (ok === false || r === null) { out.ok = false; out.failed.push(k); }
  }
  return out;
}
