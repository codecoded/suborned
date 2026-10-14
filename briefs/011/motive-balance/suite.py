# Run every scenario on a variant root; print the ones whose outcome or checks differ from HEAD.
import json, os, subprocess, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from harness import mkvar, DLL, ENV, HEAD, clock
var = json.loads(sys.argv[1])
book = None
if var.get('nowatch'):
    def book(rb):
        for e in rb['events']:
            if e['id'] == 'scout.listen': e['needs'] = [c for c in e['needs'] if not (c.get('k') == 'known' and c.get('what') == 'routine')]
root = mkvar(var['name'], var.get('values'), var.get('site'), book)
ids = [l.split('\t')[0] for l in subprocess.run(['dotnet', DLL, 'list'], cwd=HEAD, env=ENV, capture_output=True, text=True).stdout.splitlines()]
def run(r, s):
    return json.loads(subprocess.run(['dotnet', DLL, 'play', s, '--json'], cwd=r, env=ENV, capture_output=True, text=True).stdout)
changed = 0
for s in ids:
    a, b = run(HEAD, s), run(root, s)
    fa = [c['what'] for c in a['checks'] if not c['ok']]; fb = [c['what'] for c in b['checks'] if not c['ok']]
    if (a['outcome'], a['tick'], a['fail']) != (b['outcome'], b['tick'], b['fail']) or fa != fb:
        changed += 1
        print(f"{s}: HEAD {a['outcome']} {a['fail']} {clock(a['tick'])} → {b['outcome']} {b['fail']} {clock(b['tick'])}; failed checks now: {fb}")
print(f"{len(ids)} scenarios, {changed} changed")
