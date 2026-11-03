# Datum's decisions on the swap2 plan-match classes (brief 075, 2026-10-29). Reads match-swap2.json, writes match-swap2-decisions.json.
# Run: python3 -I briefs/075/oldbuild/tools/decide-swap2.py
import json, os
D = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
m = json.load(open(os.path.join(D, 'match-swap2.json')))
mm = {x['id']: x for x in m['mismatches']}
CLASS = {
  'rounded-corner': ('apply to plan (standard), value measured first', 'One geometry: the old 8 x 8 corner cell is a StreamCorner whose outside corner is an arc; the plan already lays those cells (blocks[].corners, caps corner) but draws their outside corner square. Datum adds standards.blocks.corner.radius (the outside radius of a perimeter block\'s corner cell) once Splice measures it from the adopted StreamCorner outline (the 1.6 / 2.3 m deviations suggest about 5.5 m = 2.3 / (sqrt 2 - 1)); the matcher then rounds the plan\'s outside corners by it. Not written now: a guessed radius would move the pass line on a number nobody measured.'),
  'handover-street': ('old gives way (Latch)', 'Protected by the brief: KEEP_ROADS and KEEP_PLOTS are byte for byte. The swap cut takes old kerb stones, flags and city mass too, and the kept plots carry the export\'s masses as the zone does.'),
  'old-flags': ('accept as drift (confirmed)', 'A plan plot runs from the kerb: its pavement and yard are plot ground, so old flags inside a plot are plot paving. The matcher counts them so; only flags off every plot and off the plan pavements fail.'),
  'doors': ('accept as information outside the civic block (confirmed)', 'Doors are no plan field outside the civic block (26 ops rejected in round 1); our generator and the old builders each place them by bay. Inside the Handover zone they stay failures.'),
  'southside': ('old gives way (Latch)', 'Market Street is T1\'s tram street and carries the Handover\'s official and getaway routes (M-1 1124 / 447 m), and Apollo Way keeps the Boulevard\'s 60 m junction spacing; the plan keeps both lines, so the old diagonal kerb runs and the flags off the plan pavements give way. No street takes an old line.'),
  'kerb-lines': ('per road, below: none taken this round beyond the own pavements', 'Each road decided in the per-id list.'),
  'megastructure': ('old gives way (confirmed)', 'The plan keeps the Lantern and West Terraces megastructures (multilevel, MS-1); round 1 rejected RL2/RB1 on the same ground.'),
  'other': ('op by op, below', 'E1 and N2 re-laid this round (own pavements); the rest kept, with the reason per group.'),
  'landmark-outline': ('matcher: compare by centre and containment (agreed)', 'Plan landmarks are reservations (centre, r). Note for Latch: in plan 0.5.6 (form ours) the Sphere stands at (-172, 124), 42 m west of the old Put; move the SphereStation Put to old (-172, 0, 330) or the centre test fails there.'),
  'spacehq-form': ('applied', 'Plan 0.5.6 is form "ours": the SpaceHQ_Portal at (0, 244), ground x +-40, y 204..284, S1 through the drum. The old HQ\'s ground (x +-54, y 178..298) is gone from the plan.'),
}
def kerb(x):
  el, near, at = x.get('el') or '', x.get('near') or '', x.get('at') or [0, 0]
  road = el if x['side'] == 'plan-only' else near
  if x['kind'] == 'pavement':
    p = el
    if 'garden-row-east' in p: return 'reject', 'a Handover plot (KEEP_PLOTS): never moves'
    if any(k in p for k in ('east-wall', 'west-wall-n', 'lamplight-east', 'n2')):
      return 'reject', 'plot ground runs from the kerb to 2 m behind the old back walls (x 48 / the crescent chords): its edge is a plot line, not a flag edge; the old flags there are plot paving (old-flags)'
    if any(k in p for k in ('orbit-square', 'crown-forecourt', 'ss1', 'starline-green', 'boulevard-west', 'gardens-gate', 'crown-parade')):
      return 'reject', 'an open square, forecourt, green or walk mouth: its edge on the pavement is a plot or walk line at pavement level with no flag edge to meet'
    return 'reject', 'a plot line, not a flag edge'
  if 'crown-avenue' in road: return 'reject (old gives way)', 'Crown Avenue ends at n_avenue (y 136), its end kerb on the forecourt\'s edge (y 144); the forecourt is open ground at pavement level, so the old stones up the forecourt (to y 174) go'
  if 'boulevard' in road:
    if x['side'] == 'built-only' and abs(at[1]) in (306, 356) or (x['side'] == 'built-only' and abs(at[1] + 306.5) < 1):
      return 'reject (old gives way)', 'the old kerbs across the Boulevard at Orbit Square: the plan keeps the Boulevard through as T1\'s tram street (stops at the circus and Orbit) to the Sea Gates, so the square stays two halves either side of it'
    if x['side'] == 'built-only': return 'reject (old gives way)', 'the old tram stop island (IslandZ) and its noses: T1\'s circus stop is laid by sections.tramStreet.district (edge 3.2 + island 3.0 + 0.5 + lane 3.3), so the old island gives way'
    return 'reject', 'the Boulevard\'s kerbs at x +-10 (20 m, T1\'s stop islands need 10 m a half); round 1 kept the 20 m section'
  if 'sunset-east' in road or 'harbour-market' in road:
    if abs(at[0] - 46) < 1: return 'accept as drift (matcher)', 'the side street ends on a kerb at the terrace (x 46) in both: the matcher should read a road\'s end as its kerb'
    return 'accept as drift', 'the corner return (KR-1, R 2) against the old corner stone, within 1.6 m; the side streets\' own pavements (roads[].pavement) landed this round'
  if 'circus' in road: return 'accept as drift', 'the circus mouth\'s kerb return against the old stone, 0.9 m'
  return 'reject', 'kept'
def other(x):
  el, near = x.get('el') or '', x.get('near') or ''
  b = el if x['side'] == 'plan-only' else near
  if not b: return 'matcher', 'old city mass beyond the sheet edge (y 298..300): compare on the sheet only'
  if any(k in b for k in ('lamplight-south', 'garden-row-east', 'pylon-row-east', 'rocket-west')): return 'reject (old gives way)', 'a Handover plot (KEEP_PLOTS): its buildings never move; the old pieces standing on it give way (handover-street)'
  if 'east-wall-s' in b: return 'applied', 'E1 re-laid: 13 bays at y -242.75..-148.75 between Sunset Way\'s own 3.25 m and Market Street\'s own 2.75 m pavements (old -242..-148)'
  if b.startswith('bld/n2'):
    if abs((x.get('at') or [0])[0] - 54) < 2: return 'reject', 'old E1 on the harbour terrace (x 46..74): no plot can carry it (R-F1: Orbit Square East fronts only the Boulevard, even with frontageSquare.reach); it stands as built'
    return 'applied', 'N2 re-laid: y -301.25..-261.25 (old -302..-262)'
  if 'east-wall/' in b or b.startswith('bld/east-wall/0') or b.startswith('bld/east-wall/2'):
    if 'wing' in b: return 'reject', 'a crescent end wing: the crescents are the old walls exactly (chords on r 35); the built wing\'s eyebrow and the chord differ by its curve (rounded-corner)'
    return 'reject', 'E2 keeps 7 of its 8 old bays: from Market Street\'s 2.75 m line (y -127.25, R-PV1\'s narrowest) to the east lane (-72) is 55.25 m, under 56; the lane and E3\'s arc end fix the north end'
  if 'wing' in b: return 'reject', 'a crescent end wing: the crescents are the old walls exactly (chords on r 35); built eyebrows and chords differ by the curve (rounded-corner)'
  if 'terrace-s' in b: return 'reject', 'round 1 op 16: the old west terrace bar (14 m) against the grammar\'s 28 m back-to-back block, its ends at Lamplight Lane and the Gardens Gate'
  if 'west-wall-n/4' in b or 'east-wall-n/7' in b or ('wall-n' in b and abs((x.get('at') or [0, 0])[1] - 141) < 3): return 'reject', 'the avenue walls\' north corners on the forecourt line (y 144): the old corner tower\'s overhang (to 141.3 at x +-50) is a piece, not the plot line'
  if 'boulevard-west' in b or 'lamplight-east' in b: return 'reject', 'the old W1 / W2 walls: under 1 m at the lane mouths (bays on the 2 m module), and W1\'s back at x -46 against the block\'s 28 m band, which BG-3 needs back to back'
  if 'wall-n' in b: return 'reject', 'the avenue walls (old W4 / E4) as the old walls give them; the built difference is the corner cell\'s curve (rounded-corner) and the old tower overhangs'
  return 'reject', 'kept'
out = {'by': 'Datum, brief 075 remap step 2, 2026-10-29', 'match': 'briefs/075/oldbuild/match-swap2.json', 'plan': 'harbour-district plan 0.5.6 (form ours)', 'classes': [], 'ids': {}}
for c in m['classes']['list']:
  d, why = CLASS[c['id']]
  out['classes'].append({'id': c['id'], 'n': c['n'], 'proposal': c['decision'], 'decision': d, 'why': why})
  if c['id'] in ('kerb-lines', 'other'):
    for i in c['ids']:
      dd, ww = (kerb if c['id'] == 'kerb-lines' else other)(mm[i])
      out['ids'][i] = {'class': c['id'], 'decision': dd, 'why': ww}
json.dump(out, open(os.path.join(D, 'match-swap2-decisions.json'), 'w'), indent=1, ensure_ascii=False)
from collections import Counter
print(Counter((v['class'], v['decision']) for v in out['ids'].values()))
