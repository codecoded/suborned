# brief 107: weather and night rows for -tplong. Every row sets weather and night (PlayView107 overrides) on the 106 harness; views x looks x 2 repeats,
# the order reversed the second time. Size 0 x 0 = the screen as it comes (full screen on the Air); pass W H to size a window. Usage: make_rows.py OUTDIR [W H]
import json, sys
out = sys.argv[1]; w, h = (int(sys.argv[2]), int(sys.argv[3])) if len(sys.argv) > 3 else (0, 0)
views = [dict(name="play", view="overhead", yaw=30, zoom=10), dict(name="tp-street", view="follow", agent="scout", walk=[0, -20], faceHeading=180)]
looks = [("clear", "clear", "off"), ("overcast", "overcast", "off"), ("rain", "rain", "off"), ("mist", "mist", "off"), ("night", "clear", "on"), ("rain-night", "rain", "on")]
def row(v, name, wx, nt, settle=3.0, seconds=8.0):
  return dict(name=name, view=v["view"], kind="third" if v["view"] == "follow" else "play", agent=v.get("agent", "scout"), walk=v.get("walk", [0, 0]), faceHeading=v.get("faceHeading", -1),
              yaw=v.get("yaw", 30), zoom=v.get("zoom", 10), settleSeconds=settle, seconds=seconds, w=w, h=h, variant=dict(weather=wx, night=nt))
rows = [row(views[1], f"warm-{n}", wx, nt, 2.0, 2.0) for n, wx, nt in looks]
for rep in range(2):
  for v in views:
    for n, wx, nt in (looks if rep == 0 else list(reversed(looks))):
      rows.append(row(v, f"{v['name']}.{n}.{rep + 1}", wx, nt))
print(json.dumps(dict(schema="suborned.tplong.resolved/1", outDir=out, rows=rows), indent=1))
