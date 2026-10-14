# The fastest each route can be (every order at the first tick it can be given), on a variant.
#   python3 -I fast.py '{"name": "...", "values": {...}, "site": {...}, "nowatch": true}'
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from harness import mkvar, play, line, clock

M = lambda t, op, to: {"tick": t, "op": op, "do": "move", "to": to}
O = lambda t, op, do, **k: dict({"tick": t, "op": op, "do": do}, **k)


def tail(enter_t, inside=380, walk=388):
    return [O(enter_t, "enforcer", "enter"), M(enter_t + inside, "enforcer", "d:Getaway bay"), O(enter_t + inside + walk, "enforcer", "extract")]


def first_win(root, head, lo, hi, step=5, inside=380):
    last = None
    for t in range(lo, hi, step):
        r = play(root, head + tail(t, inside))
        if r['outcome'] == 'success' and not r['bad']: return t, r
        last = r
    return None, last


def loud(root, v, inside):
    loop_s, rr_s = v.get('loop-s', 62) * 10, v.get('reroute-s', 45) * 10
    shot = v.get('shot-every-s', 6) * 10
    h0 = [M(0, "tech", "cabinet"), M(0, "enforcer", "d:Office approach"), O(283, "tech", "loop", target="entrance"), O(283 + loop_s, "tech", "reroute", target="patrol")]
    f0 = 283 + loop_s + rr_s
    last = None
    for f in range(f0, f0 + 300, 5):
        hits = v.get('hits-to-drop', 2)
        h = h0 + [O(f + i * shot, "enforcer", "fire", target="guard") for i in range(hits)]
        t, r = first_win(root, h, f + (hits - 1) * shot, f + (hits - 1) * shot + 40, 5, inside)
        if t: return f, t, r
        last = r
    return None, None, last


def route_a(root, v, nowatch, inside):
    watch, listen, help_ = v.get('watch-s', 52) * 10, v.get('listen-s', 42) * 10, 500
    h = [M(0, "scout", "beside-inez"), M(0, "tech", "cabinet"), M(0, "fixer", "d:Courtyard"), M(0, "enforcer", [4900, -1700]),
         O(283, "tech", "loop", target="entrance")]
    t = 183
    if not nowatch:
        h.append(O(t, "scout", "watch", target="waitress")); t += watch
    h.append(O(t, "scout", "listen", target="waitress")); t += listen
    h.append(O(t, "fixer", "turn", target="waitress", approach="help")); t += help_
    h += [O(t, "fixer", "call", target="waitress"), M(t, "enforcer", "d:Office entrance")]
    call = t
    best = None
    for rr in [None] + list(range(283 + v.get('loop-s', 62) * 10, call + 10, 10)):
        hh = h + ([O(rr, "tech", "reroute", target="patrol")] if rr else [])
        e, r = first_win(root, hh, call + 10, call + 1200 if rr is None else call + 200, 10, inside)
        if e and (best is None or e < best[1]): best = (rr, e, r)
        if e and rr is not None: break
    return call, best


def route_b(root, v, nowatch):
    s = "suborned.sim.script/1"
    orders = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'S20.json')))['orders']
    if nowatch:  # the Scout listens at the car without watching first
        orders = [o for o in orders if o['do'] != 'watch']
        for o in orders:
            if o['do'] == 'listen': o['tick'] = 591
            if o['do'] == 'turn': o['tick'] = 591 + 420
    return play(root, orders)


if __name__ == '__main__':
    var = json.loads(sys.argv[1]) if len(sys.argv) > 1 else {}
    v = var.get('values', {})
    book = None
    if var.get('nowatch'):
        def book(rb):
            for e in rb['events']:
                if e['id'] == 'scout.listen': e['needs'] = [c for c in e['needs'] if not (c.get('k') == 'known' and c.get('what') == 'routine')]
    root = mkvar(var.get('name', 'base'), v, var.get('site'), book)
    inside = v.get('enter-s', 38) * 10
    f, t, r = loud(root, v, inside); print(line(f"loud (fire {clock(f) if f else '-'}, enter {clock(t) if t else '-'})", r))
    call, best = route_a(root, v, var.get('nowatch'), inside)
    rr, e, r = best if best else (None, None, None)
    print(line(f"route A (call {clock(call)}, rr {clock(rr) if rr else 'none'}, enter {clock(e) if e else '-'})", r) if r else f"route A: no win (call {clock(call)})")
    print(line("route B (S20 orders)", route_b(root, v, var.get('nowatch'))))
