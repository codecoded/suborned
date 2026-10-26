# Make a scratch engine root with a rulebook variant: python3 -I mkroot.py <name> key=value ...
# (values are the rulebook's `values` entries' v; the base root's other inputs are linked, nothing in contracts/ is touched)
import json, os, sys, shutil
here = os.path.dirname(os.path.abspath(__file__)); base = os.path.join(here, '..', 'root')
name = sys.argv[1]; out = os.path.join(here, '..', 'roots', name)
if os.path.exists(out): shutil.rmtree(out)
shutil.copytree(base, out, symlinks=False)
rp = os.path.join(out, 'contracts/rules/rulebook.json')
rb = json.load(open(rp, encoding='utf-8'))
for kv in sys.argv[2:]:
    k, v = kv.split('=')
    assert k in rb['values'], k
    rb['values'][k]['v'] = int(v)
json.dump(rb, open(rp, 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print(out)
