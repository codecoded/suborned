# Motive balance harness (brief 011, 2026-10-15): plays order scripts on the pinned HEAD engine against copies of the rules with
# value overrides. Never touches the repo's contracts/ or sim/. Run with python3 -I.
import json, os, shutil, subprocess, sys

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
# The pinned engine: git archive 846bfc5 sim contracts/rules contracts/people/people.rules.json | tar -x -C out/motive/engine/head,
# then dotnet build out/motive/engine/head/sim/cli -c Release. Variants are written under out/motive/engine/var/.
HERE = os.environ.get('SUBORNED_BALANCE_OUT', os.path.join(REPO, 'out', 'motive', 'engine'))
HEAD = os.path.join(HERE, 'head')
DLL = os.path.join(HEAD, 'sim/cli/bin/Release/net10.0/suborned-sim.dll')
ENV = dict(os.environ, DOTNET_ROOT='/opt/homebrew/opt/dotnet/libexec')


def mkvar(name, values=None, site=None, book=None):
    """A root the engine can load: HEAD's rules and site with overrides. values: {value-id: v}; site: {schedule-key: v};
    book: a function editing the parsed rulebook (for non-value changes)."""
    d = os.path.join(HERE, 'var', name)
    if os.path.exists(d): shutil.rmtree(d)
    for sub in ['contracts/rules', 'contracts/people', 'sim/data']: os.makedirs(os.path.join(d, sub))
    rb = json.load(open(os.path.join(HEAD, 'contracts/rules/rulebook.json')))
    for k, v in (values or {}).items():
        if k not in rb['values']: raise KeyError(k)
        rb['values'][k]['v'] = v
    if book: book(rb)
    json.dump(rb, open(os.path.join(d, 'contracts/rules/rulebook.json'), 'w'), ensure_ascii=False, indent=1)
    shutil.copy(os.path.join(HEAD, 'contracts/people/people.rules.json'), os.path.join(d, 'contracts/people/'))
    st = json.load(open(os.path.join(HEAD, 'sim/data/handover.site.json')))
    for k, v in (site or {}).items(): st['schedule'][k] = v
    json.dump(st, open(os.path.join(d, 'sim/data/handover.site.json'), 'w'), ensure_ascii=False)
    shutil.copy(os.path.join(HEAD, 'sim/data/handover.people.json'), os.path.join(d, 'sim/data/')) if os.path.exists(os.path.join(HEAD, 'sim/data/handover.people.json')) else None
    return d


def play(root, orders, until=None, start=None):
    s = {"schema": "suborned.sim.script/1", "mission": "handover", "start": start or {}, "orders": orders}
    if until: s["until"] = until
    p = os.path.join(root, 'script.json')
    json.dump(s, open(p, 'w'))
    out = subprocess.run(['dotnet', DLL, 'play', p, '--json'], cwd=root, env=ENV, capture_output=True, text=True)
    if not out.stdout.strip(): raise RuntimeError(out.stderr)
    r = json.loads(out.stdout)
    r['bad'] = [(o['order']['tick'], o['order']['op'], o['order']['do'], o.get('why', '')) for o in r['orders'] if not o['ok']]
    return r


def clock(t):
    s = t / 10
    return f"{int(s // 60)}:{s % 60:04.1f}"


def line(name, r):
    return f"{name:44s} {r['outcome']:8s} {r['fail'] or '':22s} {clock(r['tick']):>7s}  T {r['trail']}  A {r['alert']}  ${r['cash']:,}  shots {r['gunfire']}" + (f"  BAD {r['bad']}" if r['bad'] else '')


if __name__ == '__main__':
    root = mkvar('base')
    for s in sys.argv[1:]:
        print(line(s, play(root, json.load(open(s))['orders'])))
