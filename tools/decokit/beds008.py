#!/usr/bin/env python3
"""Brief 008 follow-up (14 Oct): the civic planting-bed recipe, no Blender. Writes out/decokit-008/civic-beds.json.

The block's seven free-standing beds (export civic masses of surface `park` with no kit ref) are irregular rings, 10 to 22 m round and
0.9 to 1.9 m wide: no planter run fits them. The recipe fits any ring, from three things the builder already has (a ring, an inward
offset, a station along a polyline) and the BedTuft_A/B/C pieces:
  kerb    the ring prism in DK_Limestone to the mass's own height (0.52 m): the low cream stone kerb of gp-entrance
  hedge   the ring offset 0.22 m inward, a prism from the kerb top up 0.22 m in DK_Leaf_CivicDark: the clipped hedge
  tufts   BedTuft_A/B/C (clipped cushion + blooms in the contract's coral, flamingo, peach) every 0.55 m along the ring offset 0.50 m,
          and a second row every 0.62 m along the offset 1.05 m where the ring is 0.9 m or more away; variant (station + bed index) % 3,
          yaw seeded from the bed id
Reads the export (out/world/harbour-district.world.json, derived) only to count; nothing under contracts/ or tools/world is written."""

import json
import math
import random
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "out/decokit-008"
INSET_HEDGE, HEDGE_RISE, ROW1, STEP1, ROW2, STEP2, ROW2_MIN = 0.22, 0.22, 0.50, 0.55, 1.05, 0.62, 0.9


def ccw(ring):
    a = sum(ring[i][0] * ring[(i + 1) % len(ring)][1] - ring[(i + 1) % len(ring)][0] * ring[i][1] for i in range(len(ring)))
    return ring if a > 0 else ring[::-1]


def offset_closed(ring, d):
    """The ring moved d to the left of travel (inward on a CCW ring), per-point averaged tangent: plaza_parts.offset(closed=True)."""
    n, out = len(ring), []
    for i, p in enumerate(ring):
        a, b = ring[i - 1], ring[(i + 1) % n]
        tx, ty = b[0] - a[0], b[1] - a[1]
        L = math.hypot(tx, ty) or 1.0
        out.append((p[0] - ty / L * d, p[1] + tx / L * d))
    return out


def stations(path, step):
    P = path + path[:1]
    L = sum(math.dist(a, b) for a, b in zip(P, P[1:]))
    k = max(4, round(L / step))
    out = []
    for i in range(k):
        d, acc = L * i / k, 0.0
        for a, b in zip(P, P[1:]):
            seg = math.dist(a, b)
            if acc + seg >= d:
                f = (d - acc) / max(seg, 1e-9)
                out.append((a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f))
                break
            acc += seg
    return out


def dist_ring(p, ring):
    best = 1e9
    for a, b in zip(ring, ring[1:] + ring[:1]):
        ab = (b[0] - a[0], b[1] - a[1])
        t = max(0.0, min(1.0, ((p[0] - a[0]) * ab[0] + (p[1] - a[1]) * ab[1]) / max(ab[0] ** 2 + ab[1] ** 2, 1e-9)))
        best = min(best, math.dist(p, (a[0] + ab[0] * t, a[1] + ab[1] * t)))
    return best


def fnv1a(s):
    h = 2166136261
    for ch in s.encode():
        h = ((h ^ ch) * 16777619) & 0xFFFFFFFF
    return h


def main():
    w = json.loads((REPO / "out/world/harbour-district.world.json").read_text())
    man = {a["name"]: a for a in json.loads((OUT / "decokit_manifest.json").read_text())["assets"]}
    tris = {k: man[f"BedTuft_{k}"]["tris"] for k in "ABC"}
    beds = [m for m in w["civic"] if m["surface"] == "park" and not m.get("kit")]
    per, total, total_tris = [], 0, 0
    for bi, m in enumerate(sorted(beds, key=lambda m: m["id"])):
        ring = ccw([tuple(p) for p in m["ring"]])
        n = len(ring)
        perim = sum(math.dist(ring[i], ring[(i + 1) % n]) for i in range(n))
        area = abs(sum(ring[i][0] * ring[(i + 1) % n][1] - ring[(i + 1) % n][0] * ring[i][1] for i in range(n))) / 2
        r1 = stations(offset_closed(ring, ROW1), STEP1)
        r2 = [q for q in stations(offset_closed(ring, ROW2), STEP2) if dist_ring(q, ring) >= ROW2_MIN]
        k = [(i + bi) % 3 for i in range(len(r1) + len(r2))]
        t = sum(tris["ABC"[v]] for v in k)
        total += len(r1) + len(r2)
        total_tris += t
        per.append(dict(id=m["id"], vertices=n, perimeter_m=round(perim, 1), area_m2=round(area, 1), mean_width_m=round(2 * area / perim, 2), height_m=m["height"],
                        base_z=m["base"], seed=fnv1a(m["id"]), tufts=dict(row1=len(r1), row2=len(r2)), tuft_tris=t))
    doc = {
        "schema": "suborned.civic-beds/1", "brief": "008 follow-up (14 Oct, Vela)", "date": "2026-10-14",
        "applies_to": "export civic masses of surface `park` with no `kit`: " + ", ".join(p["id"] for p in per) + " (the fountain-garden beds and the two monument beds already carry kit pieces)",
        "frame": "the mass ring as exported, taken counter-clockwise; every offset is to the inside; heights above the mass base",
        "kerb": {"material": "DK_Limestone", "hex": "#E7DECA", "form": "the ring prism from the base to the mass height (0.52 m), as the park prism is drawn today, in this material",
                 "painting": "gp-entrance (kerb of the beds beside the stairs: cream stone, about 0.4 m, flat coping)"},
        "hedge": {"material": "DK_Leaf_CivicDark", "hex": "#46794C", "form": f"the ring offset {INSET_HEDGE} m inward, a prism from the kerb top up {HEDGE_RISE} m, flat top",
                  "painting": "gp-entrance, mission-block (clipped hedge between the kerb and the flowers)"},
        "tufts": {"pieces": ["BedTuft_A", "BedTuft_B", "BedTuft_C"], "variant": "(station index + bed index) % 3, so neighbours differ; A coral + cream, B flamingo + peach, C cream + peach (the planter runs' plantings)",
                  "base_z": f"hedge top - 0.04 m (= kerb top + {HEDGE_RISE - 0.04:.2f} m)",
                  "rows": [{"inset_m": ROW1, "step_m": STEP1, "rule": "every station along the closed offset path, starting at its first vertex"},
                           {"inset_m": ROW2, "step_m": STEP2, "rule": f"every station whose distance to the ring is at least {ROW2_MIN} m (the narrow ends of a bed keep one row)"}],
                  "yaw": "uniform 0..360 degrees from a PRNG seeded with the bed's `seed` (FNV-1a of the mass id), one draw per tuft in station order",
                  "tuft_tris": tris},
        "beds": per,
        "totals": {"tufts": total, "tris": total_tris, "note": "LOD0 triangles the beds add; CD-3 counts them once Splice lists the tufts under civic-look pieces"},
        "not_a_fit": "PlanterRun_2/4/Corner/End are straight 0.68 m troughs on a 4 m, 2 m or R 1 m module: a 36-vertex bed ring cannot be tiled with them without gaps and overlaps, so they stay for the straight runs the block already places",
    }
    (OUT / "civic-beds.json").write_text(json.dumps(doc, indent=1) + "\n")
    print(f"civic-beds.json: {len(per)} beds, {total} tufts, {total_tris} tris")
    for p in per:
        print(f"  {p['id']:34s} per {p['perimeter_m']:5.1f} w {p['mean_width_m']:.2f} tufts {p['tufts']['row1']}+{p['tufts']['row2']} = {p['tuft_tris']} tris")


if __name__ == "__main__":
    main()
