import sys
from lib import *
import d_data
idx = [int(x) for x in sys.argv[1:]] or range(6)
for k in idx:
    d = d_data.D[k]
    codex(f"D-codex-{d['id']}", "D", d["prompt"], d["w"], d["h"], setname="D", hook=d["hook"], meta=dict(pair=d["id"], role=d["role"]), keep_png=False)
