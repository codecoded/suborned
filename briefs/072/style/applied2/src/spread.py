#!/usr/bin/env python3
"""Brief 072 (Sky 2026-10-28, "yeah, I more colors needed"; Vela): the per-building colour spread, before and after the second palette push.

Replays the builder's own colour picks (no Unity): FacadeKit.Fill (FNV-1a + xorshift32 Dice, area from the host's first frontage, hostAreas first)
and ProcGrammar.Assign (48 tries per host, neighbours along a street edge penalised; WallFam, A, B from the winning Make), so the wall, plinth,
roof and enamel each building draws are counted, not guessed. Float picks (WeightedFam) run in float32 as in C#. The export's own skin accents
(world.skins[].accent, accent2) are read as they are.

python3 -I spread.py <facade.json> <world.json> [--json out.json]
"""
import json, struct, sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[5] / "tools/decokit"))
from deltae import de  # noqa: E402

M32 = 0xFFFFFFFF
def f32(x): return struct.unpack("f", struct.pack("f", x))[0]


class Dice:
    def __init__(self, id_, salt=0):
        h = (2166136261 ^ salt) & M32
        for ch in id_:
            h = ((h ^ ord(ch)) * 16777619) & M32
        self.s = h or 1
        self.next(); self.next()

    def next(self):
        s = self.s
        s ^= (s << 13) & M32; s ^= s >> 17; s ^= (s << 5) & M32
        self.s = s & M32
        return self.s

    def int(self, n): return self.next() % n
    def f(self): return f32((self.next() & 0xFFFFFF) / float(0x1000000))


RHYTHMS = ["A", "AB", "AAB", "ABA", "AABB", "ABAC", "AAAB", "ABBA", "AC", "AAC"]
CORNERS = ["square", "radius", "chamfer", "tower"]
CROWNS = ["cornice", "boomerang", "butterfly", "folded", "spire", "stepped"]
CORNICES = ["classic", "streamline", "stepped", "coved"]
BAYS = [4.0, 4.8, 6.0, 7.5]
FAM_BY_USE = {
    "residential": [("punched", 2), ("balcony", 4), ("eyebrow", 3), ("louvre", 2), ("glassblock", 2), ("fins", 1), ("porthole", 1)],
    "commercial": [("ribbon", 4), ("brise", 3), ("fins", 2), ("porthole", 3), ("sawtooth", 1), ("eyebrow", 1)],
    "mixed": [("ribbon", 2), ("eyebrow", 2), ("punched", 2), ("sawtooth", 2), ("balcony", 2), ("porthole", 2), ("louvre", 1), ("brise", 1)],
    "industrial": [("sawtooth", 3), ("louvre", 3), ("glassblock", 2), ("ribbon", 2), ("porthole", 2)],
    "tower": [("fins", 4), ("ribbon", 3), ("porthole", 1), ("brise", 1)],
    "other": [("punched", 3), ("fins", 2), ("eyebrow", 2), ("ribbon", 1), ("balcony", 1)],
}
AREA_BIAS = {
    "harbour": {"porthole": 2, "sawtooth": 1.6, "louvre": 1.6}, "docks": {"porthole": 2, "sawtooth": 2.0, "louvre": 1.5},
    "gardens": {"balcony": 2, "brise": 2, "glassblock": 1.5}, "crescent": {"balcony": 2, "brise": 1.6, "glassblock": 1.5},
    "lantern": {"eyebrow": 2, "balcony": 1.6, "punched": 1.3}, "lamplight": {"eyebrow": 2, "balcony": 1.4, "louvre": 1.3},
    "records": {"fins": 2.5, "punched": 1.6, "glassblock": 1.3},
}


def use_for(s): return {"residential": "residential", "commercial": "commercial", "mixed": "mixed", "tower": "tower", "industrial": "industrial", "port": "industrial"}.get(s, "other")


def weighted_fam(d, use, area, a1, a2):
    lst, bias = FAM_BY_USE[use], AREA_BIAS.get(area or "")
    w = []; tot = f32(0)
    for name, wt in lst:
        x = f32(wt * (bias.get(name, 1.0) if bias else 1.0))
        if name in (a1, a2): x = f32(x * f32(0.02))
        w.append(x); tot = f32(tot + x)
    r = f32(d.f() * tot)
    for (name, _), x in zip(lst, w):
        r = f32(r - x)
        if r <= 0: return name
    return lst[-1][0]


def pick(d, l, w):
    r = d.int(sum(w))
    for x, wt in zip(l, w):
        r -= wt
        if r < 0: return x
    return l[-1]


def make(spec, hid, area, use, salt):
    d = Dice(hid, (101 + salt * 7919) & M32)
    g = {}
    g["Rhythm"] = RHYTHMS[d.int(len(RHYTHMS))]; g["Mirror"] = d.int(3) == 0; g["Bay"] = BAYS[d.int(len(BAYS))]
    if use == "tower": g["Rhythm"] = "A" if d.int(2) == 0 else "AAB"
    g["Fam"] = weighted_fam(d, use, area, None, None)
    if "C" in g["Rhythm"]: weighted_fam(d, use, area, g["Fam"], None)
    d.int(5); d.int(6); d.int(6)                                   # BaseFam, TopFam, PodFam
    g["Corner"] = pick(d, CORNERS, [2, 4, 3, 2])
    d.f()                                                          # Radius
    if g["Corner"] == "radius": d.int(3)
    d.int(4)                                                       # CornerIx
    if g["Corner"] == "radius": d.int(2)
    g["Crown"] = pick(d, CROWNS, [3, 2, 2, 2, 1, 1]); d.int(len(CORNICES))
    names = list(spec["areas"][area]["families"]); w = [spec["areas"][area]["families"][k] for k in names]
    g["WallFam"] = pick(d, names, w)
    en = spec["areas"][area]["enamel"]; ia = d.int(len(en))
    g["A"] = en[ia]; g["B"] = en[(ia + 1 + d.int(len(en) - 1)) % len(en)]
    g["Coarse"] = f'{g["Rhythm"]}{"m" if g["Mirror"] else ""}@{g["Bay"]:.1f}|{g["Fam"]}|{g["WallFam"]}/{g["A"]}/{g["B"]}|{g["Corner"]}'
    return g


def fill(spec, hid, area):
    for k, v in spec["hostAreas"].items():
        if hid == k or hid.startswith(k + "/"): area = v
    area = area if area in spec["areas"] else "harbour"
    d = Dice(hid)
    names = list(spec["areas"][area]["families"]); w = [spec["areas"][area]["families"][k] for k in names]
    r, k = d.int(sum(w)), 0
    while r >= w[k]: r -= w[k]; k += 1
    fam = spec["families"][names[k]]
    roof = spec["roofs"][d.int(len(spec["roofs"]))] if d.int(3) == 0 else fam[2]
    return {"area": area, "fill_family": names[k], "roof": roof}


def host_of(mid):
    i = mid.rfind("/")
    return mid[:i] if i > 0 and mid[i:] in ("/podium", "/body", "/upper", "/lower") else mid


def run(spec, world):
    hosts = {}
    for f in world["frontages"]:
        if f["host"] not in hosts: hosts[f["host"]] = fill(spec, f["host"], f.get("area"))
    tall = {}
    for m in world["buildings"]:
        i = host_of(m["id"])
        if i not in tall or m["base"] + m["height"] > tall[i]["base"] + tall[i]["height"]: tall[i] = m
    groups = {}
    for f in world["frontages"]:
        if f["level"] != "street": continue
        n, a = f["normal"], f["a"]
        key = f'{f["street"]}|{round(n[0])},{round(n[1])}|{round(a[0]) if abs(n[0]) > abs(n[1]) else round(a[1])}'
        groups.setdefault(key, []).append(f)
    edges = []
    for k in sorted(groups):
        l = sorted(groups[k], key=lambda f: ((f["a"][0] + f["b"][0]) / 2 * f["normal"][1] - (f["a"][1] + f["b"][1]) / 2 * f["normal"][0]))
        hs = []
        for f in l:
            if not hs or hs[-1] != f["host"]: hs.append(f["host"])
        edges.append([h for h in hs if h in tall])
    nb = {}
    for e in edges:
        for a, b in zip(e, e[1:]):
            nb.setdefault(a, set()).add(b); nb.setdefault(b, set()).add(a)
    G = {}
    for hid in sorted(tall):
        if hid not in hosts:
            area = next((a for a in spec["areas"] if a in hid), "records" if hid.startswith("civic") else "harbour")
            hosts[hid] = fill(spec, hid, area)
        h = hosts[hid]; use = use_for(tall[hid]["surface"]); best, bs = None, -10 ** 9
        for t in range(48):
            g = make(spec, hid, h["area"], use, t); s = 0
            for o in nb.get(hid, ()):
                og = G.get(o)
                if not og: continue
                if og["Coarse"] == g["Coarse"]: s -= 100
                if og["Fam"] == g["Fam"]: s -= 6
                if og["WallFam"] == g["WallFam"]: s -= 4
                if og["A"] == g["A"]: s -= 2
                if og["Corner"] == g["Corner"] and og["Crown"] == g["Crown"]: s -= 3
                if og["Rhythm"] == g["Rhythm"]: s -= 1
            if s > bs: bs, best = s, g
            if s == 0: break
        G[hid] = best
    sw, fams = spec["swatches"], spec["families"]
    rows = {}
    for hid, g in G.items():
        fam = fams[g["WallFam"]]
        rows[hid] = {"area": hosts[hid]["area"], "family": g["WallFam"], "wall": sw[fam[0]].lower(), "plinth": sw[fam[1]].lower(), "roof": sw[hosts[hid]["roof"]].lower(),
                     "A": sw.get(g["A"], g["A"]).lower(), "B": sw.get(g["B"], g["B"]).lower()}
    return rows, nb


def groups_by_de(hexes, t=8.0):
    """Greedy: how many colours stay once every colour within dE2000 t of a kept one folds into it (heaviest first)."""
    kept = []
    for h, _ in Counter(hexes).most_common():
        if all(de(h, k) >= t for k in kept): kept.append(h)
    return kept


def main():
    a = sys.argv[1:]
    spec, world = json.loads(Path(a[0]).read_text()), json.loads(Path(a[1]).read_text())
    rows, nb = run(spec, world)
    pairs = sorted({tuple(sorted((x, y))) for x in nb for y in nb[x] if x in rows and y in rows})
    sk = {s["building"]: s for s in world["skins"]}
    acc = Counter(s["accent"] for s in world["skins"]); acc2 = Counter(s["accent2"] for s in world["skins"])
    sk_pairs = [(x, y) for x, y in pairs if x in sk and y in sk]
    out = {
        "buildings": len(rows), "neighbour_pairs": len(pairs),
        "wall_family": dict(Counter(r["family"] for r in rows.values()).most_common()),
        "distinct": {k: len({r[k] for r in rows.values()}) for k in ("wall", "plinth", "roof", "A", "B")},
        "distinct_dE8": {k: len(groups_by_de([r[k] for r in rows.values()])) for k in ("wall", "plinth", "roof")},
        "distinct_enamel_dE8": len(groups_by_de([r[k] for r in rows.values() for k in ("A", "B")])),
        "neighbours_same": {k: sum(rows[x][k] == rows[y][k] for x, y in pairs) for k in ("family", "wall", "plinth", "roof", "A")},
        "neighbour_wall_dE": sorted(round(de(rows[x]["wall"], rows[y]["wall"]), 1) for x, y in pairs),
        "export_accent": dict(acc.most_common()), "export_accent2": dict(acc2.most_common()),
        "export_accent_neighbours_same": sum(sk[x]["accent"] == sk[y]["accent"] for x, y in sk_pairs), "export_accent_pairs": len(sk_pairs),
        "spec": {"families": len(spec["families"]), "swatches": len(spec["swatches"]),
                 "wall_swatches": len({spec["swatches"][f[0]].lower() for n, f in spec["families"].items() if n != "civic"}),
                 "plinth_swatches": len({spec["swatches"][f[1]].lower() for n, f in spec["families"].items() if n != "civic"}),
                 "area_enamel": len({spec["swatches"][e].lower() for n, ar in spec["areas"].items() if n != "civic" for e in ar["enamel"]})},
    }
    d = out["neighbour_wall_dE"]
    out["neighbour_wall_dE_summary"] = {"min": d[0] if d else None, "median": d[len(d) // 2] if d else None, "under5": sum(x < 5 for x in d)}
    del out["neighbour_wall_dE"]
    if "--json" in a:
        Path(a[a.index("--json") + 1]).write_text(json.dumps({**out, "rows": rows}, indent=1))
    print(json.dumps(out, indent=1))


if __name__ == "__main__":
    main()
