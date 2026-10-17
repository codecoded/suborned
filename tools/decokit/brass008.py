#!/usr/bin/env python3
"""Brass over gold (Sky, 14 Oct 2026), the kit side: what changed, what Splice re-adopts. No Blender; reads out/decokit-brass (built with
`DECOKIT_TEXDIRS_EXTRA=$PWD/out/decokit-008-src/Textures/derived bun tools/decokit/run.ts --out out/decokit-brass --only <crown pieces>`), contracts/world/kit.json
(the adopted files and their hashes) and the Unity kit's .mat files (the colours Unity draws now). Writes out/decokit-brass/brass-family.json. Exit 1 on a failed check.

What turned: DK_Gold #E3D681 and DK_Accent_Gold #D0C848 -> brass #99784D, DK_Gold17 #C9C08E -> light brass #AD866F (mats.py). Their .mat files change (same guids), so every
adopted piece that lists them re-adopts by hash; the FBX files are not rebuilt (Unity binds the kit .mat by name over whatever colour an FBX embeds).
What stayed: the crown pieces (crown.py) draw DK_Crown_Gold / DK_Crown_Gold17 at the old values, so they are rebuilt (their FBX, LODs and material list change); DK_Far_Gold
(horizon) and DK_Neon_Gold (a light) are untouched; DK_Bronze, already a brass, too.
Run: python3 tools/decokit/brass008.py"""

import hashlib
import json
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import crown  # noqa: E402
from deltae import de  # noqa: E402

REPO = HERE.parents[1]
NEW = REPO / "out/decokit-brass"
KITDIR = REPO / "unity/Assets/Suborned/Kit"
SIG = json.loads((REPO / "contracts/style/suborned.style.json").read_text())["signal_colours"]
SIG = {k: v.upper() for k, v in SIG.items() if k != "rule"}
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()  # noqa: E731
dsig = lambda h: {k: round(de(h, v), 1) for k, v in SIG.items()}  # noqa: E731
TURNED = {"DK_Gold": "brass", "DK_Gold17": "light brass", "DK_Accent_Gold": "brass"}
NEWMATS = {"DK_Crown_Gold": "what DK_Gold was", "DK_Crown_Gold17": "what DK_Gold17 was"}
KEPT = ("DK_Far_Gold", "DK_Neon_Gold", "DK_Bronze")


def base_hex(path):
    c = [float(x) for x in re.search(r"_BaseColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)", Path(path).read_text()).groups()]
    return "#%02X%02X%02X" % tuple(round(x * 255) for x in c)


def main():
    kit = json.loads((REPO / "contracts/world/kit.json").read_text())["assets"]
    man = {a["name"]: a for a in json.loads((NEW / "decokit_manifest.json").read_text())["assets"]}
    problems = []
    # ---- the materials
    fam = []
    kitfile = {f["path"]: f for a in kit for f in a["files"] if f["path"].startswith("Materials/")}
    for m, role in {**TURNED, **NEWMATS, **{k: "unchanged" for k in KEPT}}.items():
        path = f"Materials/{m}.mat"
        old = base_hex(KITDIR / path) if (KITDIR / path).exists() else None
        new = base_hex(NEW / path) if (NEW / path).exists() else None
        row = dict(material=m, role=role, old=old, new=new, dE_old=dsig(old) if old else None, dE_new=dsig(new) if new else None,
                   mat_sha256_old=kitfile.get(path, {}).get("sha256"), mat_sha256_new=sha(NEW / path) if (NEW / path).exists() else None,
                   meta_guid_kept=(sha(NEW / f"{path}.meta") == kitfile[f"{path}.meta"]["sha256"]) if f"{path}.meta" in kitfile and (NEW / f"{path}.meta").exists() else None)
        if m in KEPT and row["mat_sha256_new"] and row["mat_sha256_old"] and row["mat_sha256_new"] != row["mat_sha256_old"] and m != "DK_Neon_Gold":
            problems.append(f"{m} should be unchanged but its .mat differs")
        fam.append(row)
    # every other material the build wrote is byte-identical to the adopted file (no drift from this change)
    drift = [p for p in sorted((NEW / "Materials").glob("*.mat*")) if f"Materials/{p.name}" in kitfile and sha(p) != kitfile[f"Materials/{p.name}"]["sha256"]
             and p.name.rsplit(".mat", 1)[0] not in TURNED]
    # ---- the pieces
    users = {m: [a["id"] for a in kit if a["status"] == "adopted" and m in a["materials"]] for m in TURNED}
    adopted_touched = sorted({i for v in users.values() for i in v})
    crown_set = [i for i in adopted_touched if crown.is_crown(i, next(a["category"] for a in kit if a["id"] == i))]
    cat = {a["id"]: a for a in kit}
    pieces_crown, pieces_brass = [], []
    for i in crown_set:
        a, n = cat[i], man.get(i)
        if not n:
            problems.append(f"{i} is a crown piece but was not built")
            continue
        if n["tris"] != a["tris"] or any(abs(x - y) > 0.06 for x, y in zip(sorted(n["size"]), sorted(a["size"]))):
            problems.append(f"{i}: rebuilt geometry differs from the adopted piece ({n['tris']} vs {a['tris']} tris)")
        left = [x for x in n["materials"] if x in TURNED]
        if left:
            problems.append(f"{i} still draws {left}")
        changed = []
        for f in a["files"]:
            p = NEW / f["path"]
            if p.exists() and sha(p) != f["sha256"]:
                changed.append(dict(role=f["role"], path=f["path"], sha256_old=f["sha256"], sha256_new=sha(p)))
        for p in sorted(NEW.glob(f"Models/**/DK_{i}_L*.fbx")):
            rel = str(p.relative_to(NEW))
            if not any(f["path"] == rel for f in a["files"]):
                changed.append(dict(role="fbx-lod", path=rel, sha256_old=None, sha256_new=sha(p)))
        pieces_crown.append(dict(id=i, category=a["category"], source="generated" if any(f.get("root") == "generated" and f["role"] == "fbx" for f in a["files"]) else "legacy DecoKit root (rebuilt here from its generator: tris and size identical)",
                                 height_m=a["size"][1], tris=n["tris"], materials_old=[x for x in a["materials"] if x in TURNED], materials_new=[x for x in n["materials"] if x in NEWMATS], files_changed=changed))
    for i in adopted_touched:
        if i in crown_set:
            continue
        a = cat[i]
        pieces_brass.append(dict(id=i, category=a["category"], height_m=a["size"][1], materials=[x for x in a["materials"] if x in TURNED], files_changed=[f"Materials/{x}.mat" for x in a["materials"] if x in TURNED]))
    # ---- what Splice copies
    copy = []
    for m in [*TURNED, *NEWMATS]:
        for ext in (".mat", ".mat.meta"):
            p = NEW / f"Materials/{m}{ext}"
            copy.append(dict(path=f"Materials/{m}{ext}", root="generated", sha256=sha(p), bytes=p.stat().st_size, status="changed" if f"Materials/{m}{ext}" in kitfile and sha(p) != kitfile[f"Materials/{m}{ext}"]["sha256"] else "new" if f"Materials/{m}{ext}" not in kitfile else "same"))
    doc = {
        "schema": "suborned.brass/1", "brief": "brass over gold (Sky, 14 Oct 2026; narrowed: street level and facades brass, skyline crowns keep gold)", "date": "2026-10-14",
        "rule": crown.__doc__.split("\n\n")[1].strip(),
        "family": fam,
        "turned": {m: dict(role=r, adopted_users=len(users[m])) for m, r in TURNED.items()},
        "crown_pieces": pieces_crown, "brass_pieces": pieces_brass,
        "judgement_calls": {i: "brass: seen from the plaza or a deck at eye level although tall; move it to crown.CROWN_LANDMARKS to keep its gold and rebuild it" for i in ("CircusMonument", "ParadeGate_Block", "SkyRailStation_Twin", "SkyPad_Roof", "OceanLiner")},
        "splice": {"copy_from": "out/decokit-brass (root generated)", "materials": copy, "re_adopt_by_hash_mat_only": [p["id"] for p in pieces_brass], "re_adopt_fbx_lods_and_mat": [p["id"] for p in pieces_crown],
                   "note": "FBX of the brass-only pieces are not rebuilt: an FBX embeds the old colour (36 bytes in PlanterArc's), but Unity's importer remaps every kit material to the .mat by name (KitImport AddRemap). The crown pieces' FBX and L1/L2 FBX are rebuilt: only the material names differ (tris and size identical)."},
        "drift": [p.name for p in drift],
        "checks": {"problems": problems, "other .mat byte-identical to adopted": not drift},
    }
    (NEW / "brass-family.json").write_text(json.dumps(doc, indent=1) + "\n")
    print(f"brass-family.json: {len(pieces_crown)} crown pieces rebuilt, {len(pieces_brass)} brass-only pieces re-adopt by .mat hash; problems {problems}; other-material drift {[p.name for p in drift]}")
    for r in fam:
        print(f"  {r['material']:16s}{r['role']:18s}{r['old']} -> {r['new']}  dE old {min(r['dE_old'].values()) if r['dE_old'] else '-'} new {min(r['dE_new'].values()) if r['dE_new'] else '-'}  guid kept {r['meta_guid_kept']}")
    sys.exit(1 if problems or drift else 0)


if __name__ == "__main__":
    main()
