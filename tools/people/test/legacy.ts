// The people rules as they were before the lover tie and the secret (brief 011): what a pinned older run carries. A test helper: the expansion of a table under these rules is the
// one it always was (no secret field, no lover kind, the need `secret` drawn as written).
import type { Rules } from '../rules';
export function legacyRules(rules: Rules): Rules {
  const old = JSON.parse(JSON.stringify(rules)) as Record<string, any>;
  delete old.secrets; delete old.ties.kinds.lover; delete old.ties.betweenKinds['routine-meet'];
  old.tiers.simulated.carries = old.tiers.simulated.carries.filter((f: string) => f !== 'secret'); delete old.person.readBy.fields.secret; old.person.is = old.person.is.slice(0, 4);
  return old as Rules;
}
