import base64, html, io, json, shutil, hashlib
from pathlib import Path
import numpy as np, cv2
from PIL import Image
from lib import *
import p1, p2, p3
from sil import mask_of, split4

rows_all = read_ledger(); by = {}
for r in rows_all: by[r["id"]] = r        # last write wins (one duplicate Codex run for the tech-2 sheet)
rows = list(by.values())
R = lambda pre: sorted([r for r in rows if r["id"].startswith(pre)], key=lambda r: r["id"])
SD = DEL / "scripts"; SD.mkdir(exist_ok=True)
for f in Path(__file__).parent.glob("*.py"):
    if f.name.startswith("t_"): continue
    shutil.copy(f, SD / f.name)
for n in ("silhouettes.json", "accent_dE.json"): shutil.copy(SCR / n, DEL / n)

# Tech crops from each lineup (third figure) for side-by-side outfit comparison
CR = DEL / "1_tech_in_line" / "tech-crops"; CR.mkdir(exist_ok=True)
crops = []
for key, path in [("A-white-baseline (original)", LINEUP)] + [(r["id"], DEL / r["file"]) for r in R("R3-1-")]:
    im, m = mask_of(path); fig = split4(m)[2]; ys, xs = np.nonzero(fig)
    c = im[max(0, ys.min() - 12):ys.max() + 12, max(0, xs.min() - 20):xs.max() + 20].copy()
    cm = cv2.dilate(fig[max(0, ys.min() - 12):ys.max() + 12, max(0, xs.min() - 20):xs.max() + 20], np.ones((3, 3), np.uint8))
    out = DEL / "1_tech_in_line" / "tech-crops" / (key.split(" ")[0] + ".jpg")
    cv2.imwrite(str(out), cv2.resize(c, (int(c.shape[1] * 700 / c.shape[0]), 700)), [cv2.IMWRITE_JPEG_QUALITY, 90]); crops.append((key, out))

def thumb(path, w):
    im = Image.open(path).convert("RGB"); im.thumbnail((w, w * 2)); b = io.BytesIO(); im.save(b, "JPEG", quality=78, optimize=True)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()
rel = lambda p: str(Path(p).relative_to(DEL))
def fig(path, w, cap="", href=None):
    h = href or rel(path)
    return f'<figure><a href="{html.escape(h)}"><img loading="lazy" src="{thumb(path, w)}"></a><figcaption>{cap}</figcaption></figure>'
def rfig(r, w, extra=""):
    b = "Qwen" if r["model"] == "qwen" else "Codex"
    return fig(DEL / r["file"], w, f'<span class="b {b.lower()}">{b}</span> <b>{html.escape(r["id"])}</b>{extra}<br><i>{html.escape(r.get("hook",""))}</i>')

SIL = json.load(open(SCR / "silhouettes.json"))["lineups"]; ACC = json.load(open(SCR / "accent_dE.json"))
TECHD = {f"R3-1-tech-{i+1}-{s}": (n, d) for i, (s, n, d) in enumerate(p1.TECH)}
P = []
P.append(f"""<!doctype html><meta charset=utf-8><title>Cell round 3: Tech in line vs four icons (brief 012)</title><style>
body{{font:14px/1.45 system-ui,sans-serif;margin:0;background:#f4f1ea;color:#222}} header,section.s{{padding:16px 28px}} header{{background:#1e2a2a;color:#eee}}
h1{{margin:0 0 4px}} h2{{border-bottom:2px solid #1e2a2a;padding-bottom:4px;margin-top:44px}} h3{{margin:26px 0 6px}} .note{{max-width:1000px;color:#555;margin:2px 0 10px}}
.grid,.wide{{display:flex;flex-wrap:wrap;gap:12px}} figure{{margin:0;background:#fff;padding:6px;border-radius:6px;box-shadow:0 1px 3px #0002;max-width:320px}} .wide figure{{max-width:640px}} .xw figure{{max-width:980px}}
figure img{{display:block;max-width:100%;height:auto}} figcaption{{font-size:12px;margin-top:4px}} .b{{font-size:10px;padding:1px 5px;border-radius:8px;color:#fff}} .qwen{{background:#2a6f6a}} .codex{{background:#7a4a38}}
table{{border-collapse:collapse;font-size:12px;margin:8px 0}} td,th{{border:1px solid #bbb;padding:3px 8px;text-align:left;vertical-align:top}} .num td{{text-align:right}}
.px{{image-rendering:pixelated}} .sil{{display:flex;gap:14px;align-items:flex-start;background:#fff;padding:8px;border-radius:6px;margin:8px 0;flex-wrap:wrap}} .sil img{{display:block}}
.legend{{font-size:12px;margin:2px 0 8px;max-width:640px}}
</style><header><h1>Cell round 3</h1><div>Brief 012 round 3 · Forma · 2026-10-15 · Option 1: the Tech redrawn in line · Option 2: four icons · cosplay sheets · silhouette test. Faces, bodies and hair come from the A white-on-charcoal set (Codex, A portrait + lineup as references). Qwen Turbo spend this round ${spent()-PROBE:.3f}; ceiling total ${spent():.3f} of $3.00. Thumbnails embedded; click for full files; per-image model, prompt, refs, seed, cost, sha256 in PROVENANCE.json.</div></header><section class=s>""")

# 1
P.append("<h2>1. Tech in line (option 1)</h2><p class=note>Eight crafted outfits for the Tech in the others' white-on-charcoal language, bare arms and relaxed stance kept, visor dark and unlit (it lights only when he works). Each lineup redraws only the third figure; the Scout, Fixer and Enforcer are unchanged (compare with the baseline).</p>")
P.append("<h3>The Tech alone, cropped from each lineup</h3><div class=grid>" + "".join(fig(p, 230, f"<b>{html.escape(k)}</b><br><i>{html.escape(TECHD.get(k, ('the original tank, cargo shorts and sandals',))[0])}</i>") for k, p in crops) + "</div>")
P.append("<h3>Lineups</h3><div class='wide'>" + fig(LINEUP, 640, "<b>Baseline: A white-on-charcoal (unchanged)</b>", href="../A/A-codex-lineup-white.png") + "".join(rfig(r, 640, f"<br>{html.escape(TECHD[r['id']][1])}") for r in R("R3-1-")) + "</div>")

# 2
P.append("<h2>2. Four icons (option 2)</h2><p class=note>One team thread (white on charcoal, chrome, matte technical fabric, visitors from a nearby timeline). Each person pushed into a distinct style with one signature item that is theirs alone, a silhouette you could name from a shadow, a personal accent (checked below against the signals) and one detail. Five Codex takes per person plus one Qwen wildcard.</p>")
spec = [("Scout", "Rooftop runner / sky-rail lookout", "the pack with a telescoping chrome periscope mast, or the pack-mounted folding spyglass arm", "'the pack-and-hood' (tall rectangular pack above the shoulder, bare legs); or the long coat tail with a spyglass", "fern green #4f7a4a", "an earpiece on a fern-green cord; a route map on the forearm"),
        ("Fixer", "Diplomat / negotiator", "the silver case with a coiled handset cable (a case that is also a telephone)", "'the exclamation mark' (a narrow column, a flat block at the hip, a tall ponytail); or 'the bell' (wide-leg trouser suit)", "deep sienna #74483a", "one long chrome pin at the collar"),
        ("Tech", "Mender-smith / clockwork mender", "the visor, pushed further: a loupe arm or flip-up lens plates, lit pale aqua only when working", "'the bell and the bar' (wide hakama trousers under a narrow torso, a visor bar); or 'the bar and the apron'", "pale aqua #a9d9e0 (visor glow and thin seam light only)", "chrome bracers on bare forearms; a tool-roll bandolier"),
        ("Enforcer", "Marshal of the greatcoat", "the long chrome rifle with ink-blue inlay, and the coat that goes with it", "'the door with a staff' (wide shoulders, long coat, rifle upright); or 'the rifle across the shoulders' (hooded heavy coat)", "ink blue #1f3a5f", "smoked glasses pushed up; a folded photograph in the collar")]
P.append("<table><tr><th>person</th><th>style lineage</th><th>signature item (theirs alone)</th><th>silhouette you can name from a shadow</th><th>personal accent</th><th>identity detail</th></tr>" + "".join("<tr>" + "".join(f"<td>{html.escape(c)}</td>" for c in r) + "</tr>" for r in spec) + "</table>")
for role in ("scout", "fixer", "tech", "enforcer"):
    P.append(f"<h3>{role.capitalize()}: takes</h3><div class=grid>" + "".join(rfig(r, 250, f"<br>Silhouette: {html.escape(r.get('silhouette',''))}<br>Signature: {html.escape(r.get('signature',''))}") for r in R(f"R3-2-{role}-")) + "</div>")
P.append("<h3>Lineups combining the strongest takes</h3><div class='wide'>" + "".join(rfig(r, 640, f"<br>Takes: " + ", ".join(f"{k} {v.split('-')[1]}" for k, v in r['takes'].items())) for r in R("R3-2-lineup-")) + "</div>")

# 3
P.append("<h2>3. Cosplay sheets</h2><p class=note>Front and back turnaround with three numbered callouts per sheet (signature item, two key pieces). Would someone build this? The back views are invented by the model from the front: check them before anyone cuts cloth.</p><div class='wide xw'>")
for r in R("R3-3-"):
    c = r["callouts"]
    P.append(fig(DEL / r["file"], 980, f'<b>{html.escape(r["id"])}</b><div class=legend>1 = {html.escape(c["c1"])}<br>2 = {html.escape(c["c2"])}<br>3 = {html.escape(c["c3"])}</div>'))
P.append("</div>")

# 4
P.append("<h2>4. Silhouette test</h2><p class=note>Black fills of every lineup, plus the whole lineup at play scale: the tallest figure 56 px high after the 38 degree camera's foreshortening (height x cos 38 = 0.788), shown at native size (the real size) and at 5x. Two numbers per lineup: true-scale silhouette overlap (IoU after aligning feet and centre; lower = more distinct) and the distance between width profiles (width over height, row by row; higher = more distinct). The worst (closest) pair matters most. Figures: 0 Scout, 1 Fixer, 2 Tech, 3 Enforcer.</p>")
P.append("<table class=num><tr><th>lineup</th><th>mean IoU</th><th>worst IoU (pair)</th><th>mean profile distance</th><th>closest pair by profile (distance)</th></tr>" +
         "".join(f"<tr><td style='text-align:left'>{html.escape(k)}</td><td>{v['mean_iou']}</td><td>{v['max_iou']} ({v['worst_pair']})</td><td>{v['mean_profile_dist']}</td><td>{v['min_profile_dist']} ({v['closest_pair']})</td></tr>" for k, v in SIL.items()) + "</table>")
P.append("<p class=note>Reading: the eight Tech-in-line lineups are numerically and visually the baseline (a different Tech outfit moves the lineup's silhouettes very little). L1 has the best worst-case separation; L3 is the weakest because three of the four wear long coats. IoU rises for the icon lineups because wider coats fill shared space, so use the profile distance and the 50 px view together, not IoU alone.</p>")
P.append(f'<div class=sil><img src="4_silhouettes/overview-50px-x3.png" style="height:640px"><div class=legend>All 13 lineups at 3x of the 50 px scale (colour cut-out left, black fill right). Rows: baseline, 8 Tech-in-line, L1 to L4.</div></div>')
for k, v in SIL.items():
    O = "4_silhouettes/" + k
    P.append(f'<div class=sil><div><b>{html.escape(k)}</b> <span class=legend>{html.escape(v["label"])}<br>mean profile distance {v["mean_profile_dist"]}, closest pair {v["closest_pair"]} ({v["min_profile_dist"]}), mean IoU {v["mean_iou"]}</span><br>'
             f'<img src="{O}-black.png" style="width:560px"></div><div class=legend>50 px, native<br><img class=px src="{O}-thumb50-colour.png"> <img class=px src="{O}-thumb50-black.png"><br>5x<br><img class=px src="{O}-thumb50-colour-x5.png"> <img class=px src="{O}-thumb50-black-x5.png"></div></div>')
P.append("<h3>Each person's takes as shadows</h3><p class=note>Which takes can you name from the shadow alone?</p>")
for role in ("scout", "fixer", "tech", "enforcer"):
    P.append(f'<div class=sil><div class=legend><b>{role}</b> takes 1 to 5, left to right</div><img src="4_silhouettes/takes-{role}-black.png" style="max-width:1900px;height:300px"></div>')

# accents
P.append("<h3>Personal accents against the signals (dE2000, unlit)</h3><table class=num><tr><th>person</th><th>target</th><th>vs cones</th><th>vs guard</th><th>vs rivals</th><th>painted in takes (share of image, vs cones / guard / rivals)</th></tr>")
for role, v in ACC.items():
    t = v["target_dE"]; tk = [f"{k.split('-')[3]}: {x['painted']} {x['share']*100:.2f}% ({x['cones']}/{x['guard']}/{x['rivals']})" for k, x in v["takes"].items() if x]
    P.append(f"<tr><td style='text-align:left'>{role}</td><td>{v['target']}</td><td>{t['cones']}</td><td>{t['guard']}</td><td>{t['rivals']}</td><td style='text-align:left'>{html.escape('; '.join(tk))}</td></tr>")
P.append("</table><p class=note>All clear dE 20. Tightest: the Enforcer's ink blue against the rivals' violet (24 to 25 painted), the Fixer's sienna against cone red (27 to 32 painted). The accents cover 0.1 to 2 percent of each figure, so they are a third read at best; at 50 px silhouette and the bone-on-charcoal blocking do the work. If colour is to be an identity read it needs an area (a lining, a panel), and the Enforcer's blue should move away from violet.</p></section>")
(DEL / "index.html").write_text("".join(P))

# PROVENANCE
def refmap(x):
    x = Path(x)
    if x == LINEUP: return "../A/A-codex-lineup-white.png"
    for r_, p_ in PORT.items():
        if x == p_: return f"../A/A-codex-portrait-{r_}-white.png"
    return rel(x) if str(x).startswith(str(DEL)) else str(x)
def prow(r):
    d = {k: r.get(k) for k in ("id", "set", "model", "model_id", "prompt", "seed", "cost", "file", "size", "sha256", "sha256_png", "hook", "at", "secs", "refnotes")}
    d["refs"] = [refmap(x) for x in r.get("refs", [])]
    for k in ("option", "role", "outfit", "lineage", "silhouette", "signature", "accent", "accent_name", "detail", "takes", "callouts", "tech_index", "take"):
        if k in r: d[k] = r[k]
    if r["model"] == "codex": d["note"] = "Codex image tool: prompt verbatim, unseeded, no per-image cost (frame sentence not appended in round 3)."
    return d
deliv = [r for r in rows if r["set"].startswith("R3")]
prov = dict(brief="012 round 3", owner="Forma", date="2026-10-15", ceiling_usd=3.00, stop_usd=STOP, spent_total_usd=round(spent(), 4), spent_round3_usd=round(spent() - PROBE, 4),
            round2_total_usd=PROBE, counts=dict(option1=len(R("R3-1-")), option2_takes=len([r for r in R("R3-2-") if "lineup" not in r["id"]]), option2_lineups=len(R("R3-2-lineup-")), cosplay_sheets=len(R("R3-3-")),
                                               derived_silhouette_files=len(list((DEL / "4_silhouettes").glob("*.png")))),
            inputs=dict(A_white_lineup_sha256=sha(LINEUP.read_bytes()), portraits={k: sha(v.read_bytes()) for k, v in PORT.items()}),
            personal_accents=ACC, silhouette_metrics=SIL, images=[prow(r) for r in sorted(deliv, key=lambda r: r["id"])])
(DEL / "PROVENANCE.json").write_text(json.dumps(prov, indent=1))
print(len(deliv), "generated;", prov["counts"], "spent", prov["spent_total_usd"])
