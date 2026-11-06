"""Brief 097: the review page. Self-contained HTML (images inlined as JPEG, 1600 px wide max) comparing capture rounds view by view, with each round's
frame counters (shots.json) and free-text notes. Usage:
  python3 -I briefs/097/page.py OUT.html TITLE ROUND_DIR[=label] ROUND_DIR[=label] ... [--notes notes.md] [--only name,name]
ROUND_DIR is a capture folder from out/scratch/main097/run.sh (shots.json + the frames)."""
import base64
import html
import io
import json
import sys
from pathlib import Path

from PIL import Image


def img(path, width=1600):
    im = Image.open(path).convert("RGB")
    if im.width > width:
        im = im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)
    b = io.BytesIO(); im.save(b, "JPEG", quality=84)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()


def main(argv):
    out, title, rest = Path(argv[0]), argv[1], argv[2:]
    notes, only, rounds = "", None, []
    i = 0
    while i < len(rest):
        a = rest[i]
        if a == "--notes": notes = Path(rest[i + 1]).read_text(); i += 2; continue
        if a == "--only": only = set(rest[i + 1].split(",")); i += 2; continue
        d, _, label = a.partition("="); rounds.append((Path(d), label or Path(d).name)); i += 1
    shots = {}
    for d, label in rounds:
        rows = json.loads((d / "shots.json").read_text()).get("shots", [])
        shots[label] = {r["name"]: r for r in rows}
    names = []
    for _, label in rounds:
        names += [n for n in shots[label] if n not in names and (only is None or n in only)]
    css = "body{font:14px/1.45 -apple-system,Helvetica,sans-serif;background:#f6f1e7;color:#2a2622;margin:24px}h1{font-size:22px}h2{font-size:16px;margin:28px 0 8px}" \
          ".row{display:grid;grid-template-columns:repeat(%d,1fr);gap:10px}.row figure{margin:0}.row img{width:100%%;border-radius:4px;cursor:zoom-in}" \
          "figcaption{font-size:12px;color:#6b6258;margin-top:4px}table{border-collapse:collapse;margin:8px 0}td,th{border:1px solid #d8cfc2;padding:3px 8px;font-size:12px;text-align:right}" \
          "th{background:#ece4d6}.notes{max-width:980px;white-space:pre-wrap;background:#fff;border:1px solid #e2d9ca;padding:12px 16px;border-radius:6px}" % len(rounds)
    parts = [f"<!doctype html><meta charset=utf-8><title>{html.escape(title)}</title><style>{css}</style><h1>{html.escape(title)}</h1>"]
    if notes:
        parts.append(f"<div class=notes>{html.escape(notes)}</div>")
    perf = [(label, n, shots[label][n].get("frames")) for label in shots for n in names if shots[label].get(n, {}).get("frames")]
    if perf:
        parts.append("<h2>Frame counters (Studio, 1920x1080; noisy while other work runs)</h2><table><tr><th>round</th><th>view</th><th>mean ms</th><th>p99 ms</th><th>GPU ms</th></tr>")
        for label, n, f in perf:
            parts.append(f"<tr><td>{html.escape(label)}</td><td>{n}</td><td>{f['meanMs']:.2f}</td><td>{f['p99Ms']:.2f}</td><td>{f.get('gpuMs', 0):.2f}</td></tr>")
        parts.append("</table>")
    for n in names:
        figs = []
        for d, label in rounds:
            r = shots[label].get(n)
            if not r or r.get("status") != "ok": continue
            f = next((x for x in r.get("files", []) if x.endswith((".jpg", ".png"))), None)
            if not f or not (d / f).exists(): continue
            figs.append(f"<figure><a href='#' onclick='this.firstChild.style.width=this.firstChild.style.width?\"\":\"200%\";return false'><img src='{img(d / f)}'></a><figcaption>{html.escape(label)}</figcaption></figure>")
        if figs:
            parts.append(f"<h2>{n}</h2><div class=row>{''.join(figs)}</div>")
    out.write_text("".join(parts))
    print(out, out.stat().st_size // 1024, "KB")


if __name__ == "__main__":
    main(sys.argv[1:])
