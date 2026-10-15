import sys
from lib import *
import b_data
for role in sys.argv[1:]:
    for p in b_data.ROLES[role]:
        qwen(f"B-{role}-{p['i']:02d}", f"B/{role}", b_data.prompt(role, p), 1024, 1536, setname="B", hook=p["hook"].replace("[blend] ", ""),
             meta=dict(role=role, blend=p["blend"], accent=p["accent"]), est=0.0145)
