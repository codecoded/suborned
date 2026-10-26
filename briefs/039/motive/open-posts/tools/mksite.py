# Motive 039 open-posts: the layout trade as a probe. One low piece (a bench or a planter, 1.05 m high, 3.25 m long, north-south) standing
# between the two south posts (ambush-1 (21.75, -44.25), ambush-3 (22, -42)) and the getaway car (8.5, -48.2), its centre line at x 20.0,
# y -44.75..-41.5. Only its two car-facing cover points go into the site's cover (the rival side, 0.5 m east of it, shielding against the
# west), at the quality given: >= 600 full (x mod-cover), 350-599 partial (x mod-cover-partial). It is low, so it blocks no sight; it is not
# drawn into the walk grid (a body can step over it: the probe's one approximation).
# usage: python3 -I mksite.py <sim/data/handover.site.json> <out> <q>
import json, sys
d = json.load(open(sys.argv[1])); q = int(sys.argv[3])
for i, y in enumerate((-4425, -4200)):
  d['cover']['points'].append({'id': f'cv-p{i+1}', 'at': [2050, y], 'faces': [-1000, 0], 'arc': 54, 'cos': 587, 'h': 105, 'against': 'fire', 'q': q, 'kind': 'planter-run', 'of': 'probe/low-piece'})
json.dump(d, open(sys.argv[2], 'w'))
