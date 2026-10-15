from lib import *
import c_data, d_data
for t in c_data.TEAMS:
    qwen(f"C-{t['id']}", "C", c_data.prompt(t), 1536, 1024, setname="C", hook=t["hook"], meta=dict(team=t["name"], accent=t["accent"]), est=0.0145)
for d in d_data.D:
    qwen(f"D-qwen-{d['id']}", "D", d["prompt"], d["w"], d["h"], setname="D", hook=d["hook"], meta=dict(pair=d["id"], role=d["role"]), est=0.0145)
