#!/usr/bin/env python3
"""Brief 008 follow-up (14 Oct): write the corrected colour values into contracts/world/civic-look.json (Vela's edit, colour values only).

Replaces `roles` (decision, roles, parts, pool_water, facade_patch) from out/decokit-008/civic-colour-roles.json and brings `palette.materials` to the
new pieces' unlit colours (textured materials as their linear-light mean). Structure, pieces, swaps, scales, place, ground, overhead, fade, budget, slab,
bands and sources are not touched. Materials the ADOPTED pieces still draw (kit.json lists DK_Accent_Gold, DK_Fabric_Banner, DK_Banner_Comet until Splice
re-adopts) stay in the table with their old value, so CD-4 holds before and after the re-adoption; Splice drops them on the re-import.
Run: python3 tools/decokit/lookcolours008.py   (idempotent)"""

import json
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
LOOK = REPO / "contracts/world/civic-look.json"
ROLES = REPO / "out/decokit-008/civic-colour-roles.json"


def main():
    look, new = json.loads(LOOK.read_text()), json.loads(ROLES.read_text())
    look["roles"] = {k: new[k] for k in ("decision", "roles", "parts", "pool_water", "facade_patch")}
    old = {m["material"]: m for m in look["palette"]["materials"]}
    fresh = {m["material"]: dict(material=m["material"], unlit=(m.get("unlit") or m["unlit_mean"]).upper(), used_by=m["used_by"]) for m in new["dE2000_unlit"]["materials"]}
    kept = {k: v for k, v in old.items() if k not in fresh and k in {"DK_Accent_Gold", "DK_Fabric_Banner", "DK_Banner_Comet"}}
    gone = sorted(set(old) - set(fresh) - set(kept))
    look["palette"]["materials"] = [({**fresh, **kept})[k] for k in sorted({**fresh, **kept})]
    LOOK.write_text(json.dumps(look, indent=1) + "\n")
    print(f"civic-look.json: roles replaced; palette.materials {len(old)} -> {len(look['palette']['materials'])} (kept for the adopted pieces: {sorted(kept)}; dropped, unused: {gone})")
    for k in sorted(fresh):
        o = old.get(k)
        if o is None or o["unlit"] != fresh[k]["unlit"]:
            print(f"  {k}: {o['unlit'] if o else 'new'} -> {fresh[k]['unlit']}")


if __name__ == "__main__":
    main()
