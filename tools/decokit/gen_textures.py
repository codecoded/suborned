"""Generate Deco Kit source images with codex's built-in image generation (textures.json spec).

python3 Tools/Blender/decokit/gen_textures.py [--jobs 3] [--only file1,file2]
Skips files that already exist. Writes SourceArt/DecoKit/Textures/gen/<file> and appends
PROVENANCE.json (prompt, codex source path). Cutouts are generated on white; derive_maps.py keys them.
"""

import json
import re
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = json.loads((Path(__file__).parent / "textures.json").read_text())
GEN = ROOT / "SourceArt/DecoKit/Textures/gen"
LOGS = ROOT / "SourceArt/DecoKit/Textures/logs"

FRAME = {
    "tile": "a seamless tileable square texture, 1024x1024, top-down orthographic, evenly lit flat albedo "
            "with no cast shadows, no perspective, no text, no border, no vignette. After generating, check a "
            "2x2 repeat and fix any visible seams so it tiles perfectly",
    "sheet": "a square 1024x1024 flat decal sprite sheet, orthographic, evenly lit, crisp edges",
    "art": "a portrait 1024x1536 flat printed artwork, orthographic, evenly lit",
    "cutout": "a square 1024x1024 image of a single isolated botanical element for an alpha-cutout game "
              "texture, orthographic, evenly lit, on a pure flat #FFFFFF white background, nothing touching "
              "the image edges",
}


def job(t):
    dst = GEN / t["file"]
    prompt = (f"Use your built-in image generation tool (do not write code to draw the image). Generate ONE image: "
              f"{FRAME[t['kind']]}. Subject: {t['prompt']}. World style: {SPEC['style']}. "
              f"Copy the final PNG to {dst.relative_to(ROOT)} and print the line 'Original generated PNG: <path>'.")
    LOGS.mkdir(parents=True, exist_ok=True)
    r = subprocess.run(["codex", "exec", "--skip-git-repo-check", "-s", "workspace-write", prompt],
                       cwd=ROOT, capture_output=True, text=True, timeout=1500)
    (LOGS / f"{dst.stem}.log").write_text(r.stdout + r.stderr)
    m = re.search(r"Original generated PNG:\**\s*`?([^`\s]+\.png)", r.stdout)
    ok = dst.exists()
    print(f"[gen] {t['file']}: {'OK' if ok else 'FAILED'}", flush=True)
    return dict(file=t["file"], prompt=prompt, source_path=m.group(1) if m else None, ok=ok)


def main():
    jobs = int(sys.argv[sys.argv.index("--jobs") + 1]) if "--jobs" in sys.argv else 3
    only = sys.argv[sys.argv.index("--only") + 1].split(",") if "--only" in sys.argv else None
    GEN.mkdir(parents=True, exist_ok=True)
    todo = [t for t in SPEC["textures"] if (only is None or t["file"] in only) and not (GEN / t["file"]).exists()]
    print(f"[gen] {len(todo)} to generate", flush=True)
    with ThreadPoolExecutor(jobs) as ex:
        rows = list(ex.map(job, todo))
    prov = GEN / "PROVENANCE.json"
    old = json.loads(prov.read_text()) if prov.exists() else []
    prov.write_text(json.dumps(old + [r for r in rows if r["ok"]], indent=1))
    print(f"[gen] done: {sum(r['ok'] for r in rows)}/{len(rows)}", flush=True)


main()
