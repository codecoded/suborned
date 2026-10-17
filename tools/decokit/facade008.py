#!/usr/bin/env python3
"""Brief 008 follow-up (14 Oct): the corrected facade patch, applied to a COPY of facade.json and checked. No Blender, nothing under unity/ or contracts/ is written.

Reads unity/Assets/Suborned/Editor/World/facade.json (Latch's, with the 14 Oct patch) and the patch in out/decokit-008/civic-colour-roles.json (`facade_patch`),
writes out/decokit-008/facade.patched.json and out/decokit-008/facade-patch.check.txt:
  - every changed or added key (the list Latch applies),
  - dE2000 of every changed or added swatch from the cones, guard and rivals colours (a report; no gate since 2026-10-16),
  - the host colours the civic masses get before and after, by replaying the builder's own dice (FacadeKit.Fill: FNV-1a seed, xorshift32) for the
    four limestone civic masses, so the diagnosis (which fin / band / plinth colour each mass drew) is checked, not guessed.
Run: python3 tools/decokit/facade008.py"""

import json
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).parent))
from deltae import de  # noqa: E402

FACADE = REPO / "unity/Assets/Suborned/Editor/World/facade.json"
OUT = REPO / "out/decokit-008"
SIG = {"cones": "#E03A2F", "guard": "#F0A020", "rivals": "#8A4FD0"}
CIVIC_HOSTS = ["civic/cafe", "civic/civic-office", "civic/civic-office/tower-public", "civic/civic-office/tower-official"]
M = 0xFFFFFFFF


class Dice:
    """Suborned.World.Dice (FacadeSpec.cs): FNV-1a seed, xorshift32, two warm-up draws."""

    def __init__(self, id_, salt=0):
        h = (2166136261 ^ salt) & M
        for ch in id_.encode():
            h = ((h ^ ch) * 16777619) & M
        self.s = h or 1
        self.next(), self.next()

    def next(self):
        s = self.s
        s ^= (s << 13) & M
        s ^= s >> 17
        s ^= (s << 5) & M
        self.s = s
        return s

    def int(self, n):
        return self.next() % n


def fill(spec, host, area):
    d = Dice(host)
    names = list(spec["areas"][area]["families"])
    w = [spec["areas"][area]["families"][k] for k in names]
    r, k = d.int(sum(w)), 0
    while r >= w[k]:
        r -= w[k]
        k += 1
    fam = spec["families"][names[k]]
    roof = spec["roofs"][d.int(len(spec["roofs"]))] if d.int(3) == 0 else fam[2]  # C#: d.Int(3) == 0 ? roofs[d.Int(n)] : fam[2], the first draw is the test
    en = spec["areas"][area]["enamel"]
    ia = d.int(len(en))
    return dict(family=names[k], wall=fam[0], plinth=fam[1], roof=roof, A=en[ia], B=en[(ia + 1 + d.int(len(en) - 1)) % len(en)])


def area_of(spec, host):
    return next((a for a in spec["areas"] if a in host), "records" if host.startswith("civic") else "harbour")


def hexof(spec, key):
    return spec["swatches"].get(key, key)


def main():
    before = json.loads(FACADE.read_text())
    fp = json.loads((OUT / "civic-colour-roles.json").read_text())["facade_patch"]
    after = json.loads(FACADE.read_text())
    changes = []
    for k, v in fp["swatches_add"].items():
        after["swatches"][k] = v
        changes.append(f"swatches.{k}: add {v}")
    for k, v in fp["swatches_change"].items():
        assert before["swatches"][k].lower() == v["now"].lower(), (k, before["swatches"][k], v["now"])
        after["swatches"][k] = v["to"]
        changes.append(f"swatches.{k}: {v['now']} -> {v['to']}")
    for k, v in fp["families_add"].items():
        after["families"][k] = v
        changes.append(f"families.{k}: add {v}")
    for k, v in fp["areas_add"].items():
        after["areas"][k] = v
        changes.append(f"areas.{k}: add {json.dumps(v)}")
    rec = fp["areas.records.enamel"]
    assert before["areas"]["records"]["enamel"] == rec["now"], before["areas"]["records"]["enamel"]
    after["areas"]["records"]["enamel"] = rec["to"]
    changes.append(f"areas.records.enamel: {rec['now']} -> {rec['to']}")
    g = fp["paletteOverride.gold_trim"]
    assert before["paletteOverride"]["gold_trim"].lower() == g["now"].lower()
    after["paletteOverride"]["gold_trim"] = g["to"]
    changes.append(f"paletteOverride.gold_trim: {g['now']} -> {g['to']}  [district wide: {g['district_wide']}]")
    opt = []
    for k, v in fp["optional_district_wide"].items():
        assert before["swatches"][k].lower() == v["now"].lower(), k
        opt.append(f"swatches.{k}: {v['now']} -> {v['to']}")
    lines = ["# facade.json keys that change (Latch)", *changes, "", "# optional, district wide (the gold rule reaches roof spheres and boxes through these)", *opt, ""]
    lines.append("# dE2000 of every added or changed swatch from the signal colours (a report; no gate since 2026-10-16)")
    rows = []
    touched = list(fp["swatches_add"]) + list(fp["swatches_change"]) + [k for k in fp["optional_district_wide"]]
    for k in touched:
        h = after["swatches"][k] if k in after["swatches"] else k
        if k in fp["optional_district_wide"]:
            h = fp["optional_district_wide"][k]["to"]
        d = {n: round(de(h, s), 1) for n, s in SIG.items()}
        rows.append((k, h, d, min(d.values())))
    h = after["paletteOverride"]["gold_trim"]
    rows.append(("paletteOverride.gold_trim", h, {n: round(de(h, s), 1) for n, s in SIG.items()}, min(round(de(h, s), 1) for s in SIG.values())))
    bad = 0
    for k, h, d, m in rows:
        bad += m < 20
        lines.append(f"{k:28s} {h}  cones {d['cones']:5.1f}  guard {d['guard']:5.1f}  rivals {d['rivals']:5.1f}  min {m:5.1f}")
    # every swatch of the patched file, the way out/palette/facade-safety.ts does it
    allbad = [(k, h) for k, h in after["swatches"].items() if min(de(h, s) for s in SIG.values()) < 20]
    lines.append(f"whole patched swatch table: {len(after['swatches'])} swatches, below 20: {len(allbad)} {allbad}")
    lines += ["", "# host colours of the four limestone civic masses (FacadeKit.Fill replayed)"]
    for host in CIVIC_HOSTS:
        b, a = fill(before, host, area_of(before, host)), fill(after, host, area_of(after, host))
        pr = lambda x, sp: f"area {area_of(sp, host)}, wall {x['wall']} {hexof(sp, x['wall'])}, plinth {x['plinth']} {hexof(sp, x['plinth'])}, fins A {x['A']} {hexof(sp, x['A'])}, bands B {x['B']} {hexof(sp, x['B'])}"  # noqa: E731
        lines.append(f"{host}\\n   before: {pr(b, before)}\\n   after : {pr(a, after)}".replace("\\n", "\n"))
    (OUT / "facade.patched.json").write_text(json.dumps(after, indent=2) + "\n")
    (OUT / "facade-patch.check.txt").write_text("\n".join(lines) + "\n")
    print("\n".join(lines))
    sys.exit(0)  # no signal-distance gate since 2026-10-16: the dE table above is a report


if __name__ == "__main__":
    main()
