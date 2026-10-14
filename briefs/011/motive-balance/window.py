# Route A's entry windows: every second from the call to the guard's return, does an entry (no reroute) win?
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from harness import mkvar, play, clock
from fast import M, O, tail
var = json.loads(sys.argv[1]); nowatch = var.get('nowatch')
book = None
if nowatch:
    def book(rb):
        for e in rb['events']:
            if e['id'] == 'scout.listen': e['needs'] = [c for c in e['needs'] if not (c.get('k') == 'known' and c.get('what') == 'routine')]
root = mkvar(var['name'], var.get('values'), var.get('site'), book)
h = [M(0, "scout", "beside-inez"), M(0, "tech", "cabinet"), M(0, "fixer", "d:Courtyard"), M(0, "enforcer", [4900, -1700]), O(283, "tech", "loop", target="entrance")]
t = 183
if not nowatch: h.append(O(t, "scout", "watch", target="waitress")); t += (var.get("values") or {}).get("watch-s", 52) * 10
h.append(O(t, "scout", "listen", target="waitress")); t += 420
h.append(O(t, "fixer", "turn", target="waitress", approach="help")); t += 500
h += [O(t, "fixer", "call", target="waitress"), M(t, "enforcer", "d:Office entrance")]
spans, cur = [], None
for e in range(t + 10, t + 2700, 10):
    r = play(root, h + tail(e))
    ok = r['outcome'] == 'success' and not r['bad']
    if ok and cur is None: cur = [e, e]
    elif ok: cur[1] = e
    elif cur: spans.append(cur); cur = None
    if r['outcome'] == 'failure' and r['fail'] == 'entry-seen-guard' and e > t + 1500: break
if cur: spans.append(cur)
print(f"call {clock(t)}: entry wins in " + ', '.join(f"{clock(a)}–{clock(b)} ({(b - a) // 10 + 1} s)" for a, b in spans) + f"; open seconds {sum((b - a) // 10 + 1 for a, b in spans)}")
