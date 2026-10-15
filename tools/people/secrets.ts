// Secrets (brief 011, Motive briefs/011/motive-lovers.md §2): the need `secret` retires and its weight moves to the row's `record` share. Pure: the same table gives the same table.
// Active only when the rules carry `secrets` (a pinned older rulebook has no such section and its tables are read as they were written), so recorded runs replay unchanged.
import type { Cast, Row } from '../../contracts/people/cast.schema';
import type { Rules } from './rules';

const share = (xs: { weight: number }[], pick: (x: never) => boolean) => { const t = xs.reduce((n, x) => n + x.weight, 0); return t ? xs.filter(x => pick(x as never)).reduce((n, x) => n + x.weight, 0) / t : 0; };

/** The share of a row's people whose drawn need would have been `secret`: per position (the row's position weights), the secret weight over the offered weights. */
export function legacySecretShare(row: Row): number {
  let num = 0, den = 0;
  for (const p of row.positions) {
    const offered = row.needs.filter(n => !n.positions || n.positions.includes(p.position));
    num += p.weight * share(offered, (n: { need: string }) => n.need === 'secret'); den += p.weight;
  }
  return den ? Math.round((num / den) * 10000) / 10000 : 0;
}

/** The tables as the rulebook with `secrets` reads them: no row weights the need `secret`; a row that did carries a `record` share of the same weight (unless it already names one). */
export function withSecrets(cast: Cast, rules: Pick<Rules, 'secrets'>): Cast {
  if (!rules.secrets || !cast.rows.some(r => r.needs.some(n => n.need === 'secret'))) return cast;
  return {
    ...cast,
    rows: cast.rows.map(r => {
      if (!r.needs.some(n => n.need === 'secret')) return r;
      const moved = legacySecretShare(r), own = r.secrets ?? [];
      return { ...r, needs: r.needs.filter(n => n.need !== 'secret'), secrets: own.some(s => s.secret === 'record') || !moved ? own : [...own, { secret: 'record', share: moved }] };
    }),
  };
}
