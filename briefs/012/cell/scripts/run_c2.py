from lib import *
import c_data
for t in c_data.TEAMS:
    if t["id"] == "visitors": continue  # picks-based already; keep 11 + a v2 visitors anyway below
    qwen(f"C2-{t['id']}", "C", c_data.prompt2(t), 1536, 1024, setname="C", hook=t["hook"], meta=dict(team=t["name"], accent=t["accent"], round=2), est=0.0145)
