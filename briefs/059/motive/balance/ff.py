# Motive 059 balance pass (2026-10-26): focus-fire variants of a plan. usage: python3 -I ff.py <in.plan.json> <out.plan.json> <anchor-label|step#> <locks,comma> [ops,comma] [extra-json-steps]
# Each op fires on locks[0] at the spring (fight.ambush), then on each next lock once someone is down (fight.down). The steps go in after the anchor step.
import json, sys
src, dst, anchor, locks = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4].split(',')
ops = (sys.argv[5] if len(sys.argv) > 5 and sys.argv[5] else 'scout,fixer,tech').split(',')
extra = json.loads(sys.argv[6]) if len(sys.argv) > 6 else []
p = json.load(open(src)); st = p['steps']
at = int(anchor) - 1 if anchor.isdigit() else [k for k, s in enumerate(st) if s.get('label') == anchor][0]
new = list(extra)
for k, t in enumerate(locks):  # interleaved: every op's lock k before any op's lock k+1 (the composer places steps in order)
    for op in ops:
        s = {'op': op, 'do': 'fire', 'target': t, 'label': f'{op}-l{k+1}', 'every': 0.5}
        if k == 0: s['when'] = 'fight.ambush'; s['after'] = [st[at].get('label', op)] if st[at].get('label') else [op]
        else: s['after'] = [f'{op}-l{k}']; s['when'] = 'fight.down'
        new.append(s)
st[at+1:at+1] = new
p['steps'] = st; p['note'] = f"probe 2026-10-26 focus fire ({','.join(ops)} lock {' > '.join(locks)} from the spring) on: " + p.get('note', '')[:300]
json.dump(p, open(dst, 'w'), indent=1)
