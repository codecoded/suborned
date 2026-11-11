# brief 106: the Air's -tplong rows. Every row sets all three switches; views x variants x 2 repeats, the variant order rotated per repeat
# so a drift of the machine does not land on one variant. Size 0 x 0 = the full screen as it comes (CaptureSize). Usage: make_rows.py OUTDIR > rows.json
import json, sys
out = sys.argv[1]
views = [
  dict(name="play", view="overhead", yaw=30, zoom=10),
  dict(name="tp-street", view="follow", agent="scout", walk=[0, -20], faceHeading=180),
  dict(name="tp-worst", view="follow", agent="scout", walk=[-10, -25], faceHeading=90),
]
V = lambda o, m, s, pct=0, **k: dict(occlusion=o, meshLod=m, stp=s, stpScalePct=pct, **k)  # meshLod "on" = threshold 16 as built (BuiltIns)
variants = [
  ("base",   V("off", "off", "off")),
  ("occ",    V("on",  "off", "off")),
  ("mlod16", V("off", "on",  "off")),
  ("mlod8",  V("off", "on",  "off", meshLodThreshold=8)),
  ("stp75",  V("off", "off", "on", 75)),
  ("stp70",  V("off", "off", "on", 70)),
  ("all75",  V("on",  "on",  "on", 75)),
]
def row(v, name, var, settle=3.0, seconds=8.0):
  r = dict(name=name, view=v["view"], kind="third" if v["view"] == "follow" else "play", agent=v.get("agent", "scout"), walk=v.get("walk", [0, 0]),
           faceHeading=v.get("faceHeading", -1), yaw=v.get("yaw", 30), zoom=v.get("zoom", 10), settleSeconds=settle, seconds=seconds, w=0, h=0, variant=var)
  return r
rows = []
for i, (vn, var) in enumerate(variants):  # warm-up: each variant's pipelines compiled once, not counted
  rows.append(row(views[1], f"warm-{vn}", var, 2.0, 2.0))
for rep in range(2):
  for v in views:
    order = variants[rep:] + variants[:rep] if rep == 0 else list(reversed(variants))
    for vn, var in order:
      rows.append(row(v, f"{v['name']}.{vn}.{rep + 1}", var))
for v in [views[0], dict(views[0], name="play-y210", yaw=210), views[1]]:  # STP stills: the same frame off and on, then in motion (TpLongStills)
  rows.append(row(v, f"{v['name']}.stills", V("off", "off", "off", 75, stills="on")))
for v in views[:2]:  # per-pass GPU time (samplers on: not timed against the rest)
  for vn, var in variants[1:4]:
    rows.append(row(v, f"{v['name']}.{vn}.gpu", dict(var, gpuSplit="on")))
print(json.dumps(dict(schema="suborned.tplong.resolved/1", outDir=out, rows=rows), indent=1))
