# Motive brief 067: the patrol move alone (item A, Vantage's points of 2026-10-26) on HEAD's site, for the probe.
# usage: python3 -I mksite.py <HEAD's sim/data/handover.site.json> <out>
# Moves patrol-a, patrol-b and patrol-reroute to Vantage's points (mission.json, block frame -> site cm) and turns the sweep to face the
# wing's east face (site patrol.facing [0, -100]) and the reroute away from the yard ([-100, 0]), as his reasons say; tools/sim/site.ts
# still hard-codes [0, 100] / [0, -100] (Splice). Nothing else in the site moves (no planters, butler or stand points: items J, K, L).
import json, sys
d = json.load(open(sys.argv[1])); pl = d['places']
pl['patrol-a']['at'] = [6125, -725]; pl['patrol-b']['at'] = [7625, -725]; pl['patrol-reroute']['at'] = [4625, -925]
d['patrol']['facing'] = [0, -100]; d['patrol']['rerouteFacing'] = [-100, 0]
json.dump(d, open(sys.argv[2], 'w'))
