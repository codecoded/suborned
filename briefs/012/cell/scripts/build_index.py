import base64, html, io, json, shutil, hashlib
from pathlib import Path
from PIL import Image
from lib import *

rows = read_ledger()
by = {r["id"]: r for r in rows}
# --- refs copied into the deliverable (picks, 008 sheet) so provenance paths resolve
RD = DEL / "refs"; RD.mkdir(exist_ok=True)
for p in list(PICK.values()):
    shutil.copy(p, RD / p.name)
shutil.copy(REFS / "b008_sheet.png", RD / "b008_sheet.png")
# --- scripts + analysis copied for replay
SD = DEL / "scripts"; SD.mkdir(exist_ok=True)
for f in Path(__file__).parent.glob("*.py"):
    if f.name.startswith("t_") or f.name in ("t2.py",): continue
    shutil.copy(f, SD / f.name)
shutil.copy(SCR / "de2000.json", DEL / "de2000.json")

def relref(r):
    out = []
    for x in r.get("refs", []):
        x = Path(x)
        if x.parent == REFS: out.append("refs/" + x.name)
        elif x.parent == RAW and x.stem.startswith("A-codex-lineup"): out.append(by[x.stem]["file"])
        else: out.append(str(x))
    return out

AH = {"scout": "Reads a street like a page: first one up, last one down.",
      "fixer": "Composed and unhurried; has never needed to raise her voice.",
      "tech": "Easygoing, hands never idle; more amused by machines than people.",
      "enforcer": "Carries the weight so no one else has to; patient until he isn't."}
ACCN = {"teal": "deep teal-black", "sienna": "deep sienna-brown", "white": "white-on-charcoal"}

def hook(r):
    if r["set"] == "A":
        i = r["id"]
        for role, h in AH.items():
            if f"-{role}-" in i: return h
        if "lineup" in i: return "Scout, Fixer, Tech, Enforcer: the four Sky chose, one finish, one accent."
        if "visor" in i: return "Visor dark when idle, a soft pale glow only while he works."
    return r.get("hook", "")

def thumb(path, w):
    im = Image.open(path).convert("RGB"); im.thumbnail((w, w * 2))
    b = io.BytesIO(); im.save(b, "JPEG", quality=78, optimize=True)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()

def fig(r, w=300, extra=""):
    p = DEL / r["file"]
    badge = "Qwen" if r["model"] == "qwen" else "Codex"
    return (f'<figure><a href="{html.escape(r["file"])}"><img loading="lazy" src="{thumb(p, w)}" alt="{html.escape(r["id"])}"></a>'
            f'<figcaption><span class="b {badge.lower()}">{badge}</span> <b>{html.escape(r["id"])}</b>{extra}<br><i>{html.escape(hook(r))}</i></figcaption></figure>')

def section(title, items, w=300, cls="grid", note=""):
    s = f'<h3>{title}</h3>' + (f'<p class="note">{note}</p>' if note else "") + f'<div class="{cls}">' + "".join(fig(r, w) for r in items) + "</div>"
    return s

deliv = [r for r in rows if r["set"] in "ABCD" and r["set"] != "test"]
S = lambda s: [r for r in deliv if r["set"] == s]
A, B, C, D = S("A"), S("B"), S("C"), S("D")
ids = lambda pre: sorted([r for r in A if r["id"].startswith(pre)], key=lambda r: r["id"])
order = ["scout", "fixer", "tech", "enforcer"]
def portrait_sorted(model):
    return [by[f"A-{model}-portrait-{ro}-{ac}"] for ac in ("white", "teal", "sienna") for ro in order]
def full_sorted():
    return [by[f"A-qwen-full-{ro}-{ac}"] for ac in ("white", "teal", "sienna") for ro in order]

TOP = [("A-codex-lineup-white", "The unified picks: one finish, bone on charcoal. The team answer to the four-finish problem."),
       ("D-codex-D5-lineup-seafoam-and-bone", "Best chemistry and silhouette variety: four different cuts, one palette; the 008 cohesion with an outsider's edge."),
       ("B-fixer-01", "A Fixer to follow: ex-records clerk, tired and intelligent, the Story's backstory in one look."),
       ("B-enforcer-03", "A big silhouette with warmth: laughing demolition expert, plant in her pocket; strong against the usual bruiser."),
       ("B-tech-08", "Bold two-tone apron shape, gentle face, wrench: the Tech reads at a glance."),
       ("B-scout-02", "Youth and movement: a cocky runner, pack and mirror, the lightest silhouette in the set."),
       ("C2-found-family", "Four generations, four shapes, one palette: the team you would want to follow."),
       ("A-codex-portrait-enforcer-white", "HUD-ready face with weight and history; the picks' Enforcer, made calmer.")]

DE = json.load(open(SCR / "de2000.json"))["candidates"]
de_rows = "".join(f"<tr><td>{html.escape(k)}</td><td>{v['cones']}</td><td>{v['guard']}</td><td>{v['rivals']}</td></tr>" for k, v in DE.items())

parts = []
parts.append(f"""<!doctype html><meta charset=utf-8><title>Cell characters, round 2 (brief 012)</title>
<style>
body{{font:14px/1.4 system-ui,sans-serif;margin:0;background:#f4f1ea;color:#222}}
header,section.s{{padding:16px 28px}} header{{background:#1e2a2a;color:#eee}} h1{{margin:0 0 4px}} h2{{border-bottom:2px solid #1e2a2a;padding-bottom:4px;margin-top:40px}}
h3{{margin:26px 0 6px}} .note{{max-width:980px;color:#555;margin:2px 0 10px}}
.grid{{display:flex;flex-wrap:wrap;gap:12px}} .wide{{display:flex;flex-wrap:wrap;gap:12px}}
figure{{margin:0;background:#fff;padding:6px;border-radius:6px;box-shadow:0 1px 3px #0002;max-width:310px}} .wide figure{{max-width:640px}}
figure img{{display:block;max-width:100%;height:auto}} figcaption{{font-size:12px;margin-top:4px}}
.b{{font-size:10px;padding:1px 5px;border-radius:8px;color:#fff}} .qwen{{background:#2a6f6a}} .codex{{background:#7a4a38}}
table{{border-collapse:collapse}} td,th{{border:1px solid #bbb;padding:3px 8px;text-align:right}} td:first-child{{text-align:left}}
.pair{{display:flex;gap:10px;margin:10px 0;background:#fff;padding:8px;border-radius:6px;flex-wrap:wrap}} details{{max-width:1000px;font-size:12px;color:#444}}
</style>
<header><h1>Cell characters, round 2</h1><div>Brief 012 · Forma · 2026-10-15 · {len(deliv)} images (A {len(A)}, B {len(B)}, C {len(C)}, D {len(D)}) · Qwen Image 2.1 Turbo spend ${spent():.3f} of the $3.00 ceiling (probe included) · Codex image tool free.
Thumbnails are embedded; click any image for the full file. Per-image model, prompt, refs, seed, cost, sha256: PROVENANCE.json.</div></header>
<section class=s>
<h2>Top 8</h2><div class=grid>""")
for i, why in TOP:
    r = by[i]; parts.append(fig(r, 300, f"<br>{html.escape(why)}"))
parts.append("</div>")

# A
parts.append("<h2>A. Unify the picks</h2><p class=note>Sky's four locked picks (Scout S1-2, Fixer P4-6, Tech F2-1, Enforcer G3), same faces, bodies, hair and silhouettes, one finish. Gold becomes chrome or matte metal; violet becomes the cell accent; the Tech's glow is calmed to a dark unlit visor; the Enforcer's amber glasses go neutral smoke grey (amber is the guard signal). Three accents: white-on-charcoal, deep teal-black, deep sienna-brown (a mid burnt sienna measured only ΔE 14 from the cones, so the accent is the dark one).</p>")
parts.append("<div class=wide>" + "".join(fig(by[f"A-codex-lineup-{a}"], 640) for a in ("white", "teal", "sienna")) + "</div>")
parts.append(section("A. HUD portraits, Codex (rows: white-on-charcoal, teal-black, sienna; Scout, Fixer, Tech, Enforcer)", portrait_sorted("codex"), 250))
parts.append("<h3>A. Same brief on Qwen Turbo with the picks as reference</h3><p class=note>Qwen takes reference images (inputs.referenceImages, prompt extend off) and keeps identity well, but follows the edits less strictly: the Tech's strips still glow faintly, the Enforcer's glasses stay amber in places, some gold survives on shoes.</p>")
parts.append("<div class=wide>" + "".join(fig(by[f"A-qwen-lineup-{a}"], 640) for a in ("white", "teal", "sienna")) + "</div>")
parts.append(section("A. Qwen full body (white, teal, sienna)", full_sorted(), 250))
parts.append(section("A. Qwen portraits (white, teal, sienna)", portrait_sorted("qwen"), 250))
parts.append("<div class=wide>" + "".join(fig(by[f"A-qwen-tech-visor-states-{a}"], 640) for a in ("white", "teal", "sienna")) + "</div>")

# B
parts.append("<h2>B. Fresh takes per role (Qwen Image 2.1 Turbo)</h2><p class=note>14 per role: 11 open briefs plus 3 blends (the 008 sheet's cohesion, bone and dark seafoam-teal with a coral lining flash, but cut sharper, no brass, faces open). Each prompt starts from a one-line personality hook. Open takes use a charcoal base, one off-white panel and one accent from: deep teal-black, burnt sienna, bone, slate.</p>")
for role in order:
    parts.append(section(f"B. {role.capitalize()}", sorted([r for r in B if r.get("role") == role], key=lambda r: r["id"]), 240))

# C
c2 = sorted([r for r in C if r["id"].startswith("C2-")], key=lambda r: r["id"]); c1 = sorted([r for r in C if r["id"].startswith("C-")], key=lambda r: r["id"])
parts.append("<h2>C. Team lineups (Qwen Image 2.1 Turbo)</h2>")
parts.append("<div class=wide>" + "".join(fig(r, 640) for r in c2) + "</div>")
parts.append("<h3>C. Round 1 (kept as evidence: the four drifted into a matching squad uniform with cargo trousers and belts, i.e. tactical costume)</h3><div class=wide>" + "".join(fig(r, 640) for r in c1) + "</div>")
parts[-1] = parts[-1].replace("</div>", "</div>")
parts.append("<p class=note>Round 2 adds a silhouette recipe per role (Scout: short jacket and pack; Fixer: one long slim line; Tech: loose vest or apron; Enforcer: biggest, long heavy coat) and bans cargo trousers, belts and matching tops.</p>")

# D
parts.append("<h2>D. Model comparison: same prompt, Qwen Turbo (left) and Codex (right)</h2><p class=note>Codex prompts are the same text plus the runner's standard margin sentence (D1-D5). Qwen: 17 s and $0.014 per image, seeded and repeatable. Codex: about 1 to 2 minutes, free, unseeded.</p>")
dd = sorted(set(r["meta_pair"] if "meta_pair" in r else r["pair"] for r in D))
for pid in dd:
    q = by[f"D-qwen-{pid}"]; c = by[f"D-codex-{pid}"]
    w = 520 if "lineup" not in pid else 760
    parts.append(f'<div class=pair>{fig(q, w)}{fig(c, w)}<details><summary>prompt</summary>{html.escape(q["prompt"])}</details></div>')

# E: iteration log
tests = [r for r in rows if r["set"] == "test" and (DEL / r["file"]).exists()]
parts.append("<h2>E. Iteration log (not counted)</h2><p class=note>First attempts that changed the prompt: v1 B prompt (photo-flat, catalogue pose), v1 C lineup (uniform drift), v1 sienna (burnt sienna too bright, ΔE 14 to cones), v1 teal lineup (amber glasses), v1 D6 (asked for head and shoulders, got full body).</p><div class=grid>" + "".join(fig(r, 220) for r in tests) + "</div>")
parts.append(f"<h2>Accent colours against the signals (ΔE2000, unlit)</h2><table><tr><th>colour</th><th>cones #e03a2f</th><th>guard #f0a020</th><th>rivals #8a4fd0</th></tr>{de_rows}</table><p class=note>Gate: every world colour at ΔE 20 or more from each signal. Measured painted accents are in de2000.json.</p></section>")
(DEL / "index.html").write_text("".join(parts))

# PROVENANCE
def prow(r):
    d = {k: r.get(k) for k in ("id", "set", "model", "model_id", "prompt", "seed", "cost", "file", "size", "sha256", "sha256_png", "hook", "at", "secs")}
    d["hook"] = hook(r); d["refs"] = relref(r)
    if r.get("refnotes"): d["refnotes"] = r["refnotes"]
    for k in ("role", "accent", "team", "pair", "round", "blend"):
        if k in r: d[k] = r[k]
    if r["model"] == "codex": d["note"] = "Codex image tool runs the prompt verbatim plus a standard margin sentence unless frame disabled; unseeded."
    return d
prov = dict(brief="012", owner="Forma", date="2026-10-15", ceiling_usd=3.00, stop_usd=STOP, qwen_probe_usd=PROBE, qwen_spent_usd=round(spent(), 4),
            qwen_model="alibaba:qwen-image@2.1-turbo (settings.promptExtend=false, seed = sha256(id) mod 2^31, 1024x1536 singles, 1536x1024 lineups, 1024x1280 portraits)",
            codex="codex exec image tool, ChatGPT plan, no per-image cost; references via -i",
            counts=dict(A=len(A), B=len(B), C=len(C), D=len(D), total=len(deliv)),
            inputs=dict(picks={p.name: sha(p.read_bytes()) for p in PICK.values()}, sheet_008=sha((REFS / "b008_sheet.png").read_bytes())),
            images=[prow(r) for r in deliv], iteration_tests=[prow(r) for r in rows if r["set"] == "test"])
(DEL / "PROVENANCE.json").write_text(json.dumps(prov, indent=1))
print(len(deliv), "delivered; spent", round(spent(), 4))
