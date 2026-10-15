import base64, html, io, json, shutil
from pathlib import Path
import numpy as np
from PIL import Image
from lib import *
import p7, p9

rows_all = read_ledger(); by = {}
for r in rows_all: by[r["id"]] = r
rows = list(by.values()); R = lambda pre: sorted([r for r in rows if r["id"].startswith(pre)], key=lambda r: r["id"])
SD = DEL / "scripts"; SD.mkdir(exist_ok=True)
(DEL / "refs").mkdir(exist_ok=True)
for f in (SCR / "refs").glob("*.png"): shutil.copy(f, DEL / "refs" / f.name)
for f in Path(__file__).parent.glob("*.py"):
    if not f.name.startswith("t_"): shutil.copy(f, SD / f.name)
for n in ("analysis.json", "colours.json"): shutil.copy(SCR / n, DEL / n)
A = json.load(open(SCR / "analysis.json")); C = json.load(open(SCR / "colours.json"))
L1 = ROOT / "briefs/012/cell/round3/2_icons/lineups/R3-2-lineup-L1.png"

# comparison composite: L1 (round 3) / animatable uniform / wardrobes + lamp
cmp_files = [L1, DEL / "lineups/R3b-lineup-U-uniform-animatable.png", DEL / "lineups/R3b-lineup-WB1.png"]
ims = [Image.open(p).convert("RGB").resize((1200, 800)) for p in cmp_files]
M = Image.new("RGB", (1200, 2400), "white")
for i, im in enumerate(ims): M.paste(im, (0, i * 800))
M.save(DEL / "lineups/comparison-L1-U-WB1.jpg", quality=90)

def thumb(path, w):
    im = Image.open(path).convert("RGB"); im.thumbnail((w, w * 2)); b = io.BytesIO(); im.save(b, "JPEG", quality=78, optimize=True)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()
rel = lambda p: str(Path(p).relative_to(DEL))
def fig(path, w, cap="", href=None):
    return f'<figure><a href="{html.escape(href or rel(path))}"><img loading="lazy" src="{thumb(path, w)}"></a><figcaption>{cap}</figcaption></figure>'
def rfig(r, w, extra=""):
    return fig(DEL / r["file"], w, f'<span class="b codex">Codex</span> <b>{html.escape(r["id"])}</b>{extra}<br><i>{html.escape(r.get("hook",""))}</i>')

# animation notes: (design key) -> (secondary motion, cloth-sim risk, extra bones, note). Base rig = 65-bone Mixamo humanoid. ESTIMATES from the drawings; no rig built.
NOTES = {
 "scout-1-trail-runner": ("pack (follow-through), hood, earpiece cord baked", "none (cropped windshell and shorts are skinned)", "+2 (pack 1, hood 1; mast rigid on pack)", "bare legs: nothing to sim; cleanest rig of the four"),
 "scout-2-fin-glider": ("pack, two fin hinges (open/close), hood", "none", "+3 (pack 1, fins 2)", "fins are rigid plates; the open/closed pose is an animation, not cloth"),
 "fixer-1-knit-tunic": ("ponytail chain, coiled cable chain, tunic hem front/back, case swing", "low (knit hem at mid-thigh, slim legs; no slit)", "+10 (ponytail 4, cable 3, hem 2, case 1)", "the only mid-thigh hem in the set; if cut it falls to +8"),
 "fixer-2-leather-jacket": ("ponytail chain, coiled cable chain, case swing", "none (hip-length leather jacket skinned)", "+8 (ponytail 4, cable 3, case 1)", "cheapest Fixer that keeps a garment over the hips"),
 "fixer-3-knit-jumpsuit": ("ponytail chain, coiled cable chain, case swing", "none", "+8 (ponytail 4, cable 3, case 1)", "pure skinned column: easiest to animate and to cosplay"),
 "tech-1-canvas-wide": ("visor plates (2 hinges); trousers should not need secondary motion", "MEDIUM, flagged: wide structured trousers (ankle-length); stiff box-pleat panels skinned to thigh and shin with correctives; check stride, crouch, sit", "+6 (visor 2, trouser correctives 4)", "the bell; fallback is the apron or the tool belt"),
 "tech-2-canvas-apron": ("visor plates, apron hinge, bandolier", "low to medium: stiff apron panel hinged at the belt, needs a thigh collision proxy when running or crouching", "+5 (visor 2, apron 1, bandolier 2)", "drop the hinge (skin to hips and thighs) and it is +4"),
 "tech-3-tool-belt": ("visor plates, hanging tools (2 jiggle bones)", "none (belt ring skinned; tools are rigid props)", "+4 (visor 2, tools 2)", "silhouette from bulk at the hips, no cloth"),
 "enforcer-1-lacquer-staff": ("rifle (socket), pauldrons as rigid children of the shoulders, optional 2 lag bones", "none (hip-length jacket skinned)", "+1 (rifle socket; optional +2)", "staff pose is a two-hand grip: needs hand IK on the rifle"),
 "enforcer-2-lacquer-yoke": ("rifle held on the shoulders (prop bone on the chest), no secondary", "none", "+1 bone, +2 IK constraints on the wrists", "arms are locked over the rifle: a special carry locomotion set; swap the rifle to the hands for fighting (animation event)"),
 "enforcer-3-ring-collar": ("rifle on the shoulder (socket), collar ring (optional 1 lag bone)", "none", "+2 (rifle 1, collar 1)", "one-hand shoulder carry; the free arm animates normally"),
}
UNIF_NOTES = {"enforcer-1-heavy-jacket-staff": "same cut family as lacquer-staff", "enforcer-2-yoke": "same as lacquer-yoke", "enforcer-3-marshal-short": "slung rifle: +1 socket; epaulettes rigid", "enforcer-4-hood-ring": "same as ring-collar",
               "fixer-1-jumpsuit-column": "same as knit-jumpsuit (+8)", "fixer-2-thigh-tunic": "same as knit-tunic (+10)", "fixer-3-cropped-suit": "hip flick: +2 hem bones, total +10", "fixer-4-overlay": "asymmetric overlay at hip: +2 flap bones, total +10, low cloth risk",
               "tech-1-structured-wide": "MEDIUM: asymmetric ankle-length front panel (trailing; rejected)", "tech-2-stiff-apron": "same as canvas-apron (+5)", "tech-3-tool-belt-hips": "same as tool-belt (+4)", "tech-4-bell-shorts": "rigid bell shorts at mid-thigh: +2 hem bones, thigh clipping on stride, low-medium"}

P = []
P.append(f"""<!doctype html><meta charset=utf-8><title>Cell round 3b: L1 made animatable, four wardrobes (brief 012)</title><style>
body{{font:14px/1.45 system-ui,sans-serif;margin:0;background:#f4f1ea;color:#222}} header,section.s{{padding:16px 28px}} header{{background:#1e2a2a;color:#eee}}
h1{{margin:0 0 4px}} h2{{border-bottom:2px solid #1e2a2a;padding-bottom:4px;margin-top:44px}} h3{{margin:26px 0 6px}} .note{{max-width:1000px;color:#555;margin:2px 0 10px}}
.grid,.wide{{display:flex;flex-wrap:wrap;gap:12px}} figure{{margin:0;background:#fff;padding:6px;border-radius:6px;box-shadow:0 1px 3px #0002;max-width:320px}} .wide figure{{max-width:640px}} .xw figure{{max-width:980px}} .tall figure{{max-width:1200px}}
figure img{{display:block;max-width:100%;height:auto}} figcaption{{font-size:12px;margin-top:4px}} .b{{font-size:10px;padding:1px 5px;border-radius:8px;color:#fff}} .codex{{background:#7a4a38}}
table{{border-collapse:collapse;font-size:12px;margin:8px 0}} td,th{{border:1px solid #bbb;padding:3px 8px;text-align:left;vertical-align:top}} .num td{{text-align:right}}
.px{{image-rendering:pixelated}} .sil{{display:flex;gap:14px;align-items:flex-start;background:#fff;padding:8px;border-radius:6px;margin:8px 0;flex-wrap:wrap}} .legend{{font-size:12px;margin:2px 0 8px;max-width:640px}}
.sw{{display:inline-block;width:14px;height:14px;border:1px solid #888;vertical-align:middle}}
</style><header><h1>Cell round 3b: animation-ready, and four wardrobes</h1><div>Brief 012 round 3b · Forma · 2026-10-15 · Codex only (no Qwen this round; ceiling total ${spent():.3f} of $3.00 after round 3). Faces, bodies, hair from the A portraits; signature items and silhouette shapes kept; animation rules kept (hems at hip or mid-thigh, no capes, structured trousers flagged). Thumbnails embedded; click for full files; model, prompt, refs, sha256 in PROVENANCE.json.</div></header><section class=s>""")

P.append("""<h2>Answer to Sky: is each character dressed very differently?</h2><p class=note>L1 was not: four silhouettes and four signature items, but one palette, one material language and one fashion lineage, a squad uniform. Round 3b stage 1 (white on charcoal, hip-length) fixed the animation problem only. Stage 2 gives each person their own palette, materials and fashion lineage and ties the team together with one small motif. Compare the three rows in section 3.</p>""")

# 1 wardrobes
P.append("<h2>1. Four wardrobes</h2><p class=note>Each person has a dominant colour plus one accent, their own materials, and a fashion lineage from a different subculture of their home timeline. Rules held: hems at the hip or mid-thigh at most, no capes or tails, structured trousers only (flagged), ponytail as one secondary chain.</p>")
spec = [("Scout", "trail-running and rooftop-parkour subculture", "ripstop nylon, mesh, rubberised zips, reflective tape", "khaki coyote-tan #8a7f5a", "graphite #3a3f45 panels, zips, straps", "the pack-and-hood (tall pack above the shoulder, bare legs); or twin fins", "the pack with a chrome periscope mast", "earpiece with a graphite cord"),
        ("Fixer", "editorial minimalism and embassy-aide tailoring", "fine merino rib-knit, soft leather, lacquered chrome", "deep navy #1f2f4f", "dusty rose #b98a8f trim and inlay", "the exclamation mark (narrow column, case at the hip, tall ponytail)", "silver lacquered case with a coiled handset cable", "one long chrome collar pin"),
        ("Tech", "clockmaker and machinist workshop culture", "waxed canvas, leather, enamel, chrome", "dark moss green #3f4a2a", "pale aqua #a9d9e0 webbing and stitching", "a stiff apron, or the jingle of tools at the hips, or the structured bell (flagged)", "visor with two flip-up chrome plates, lit pale aqua only when working", "chrome bracers on bare arms"),
        ("Enforcer", "dockside guild-marshal and duelist-guard culture", "lacquered armour plates, waxed wool, heavy leather", "chestnut brown #6b4226", "bone white #e8e4da inlay", "the door (pauldrons on a straight torso, rifle as a staff) or the yoke (rifle across the shoulders)", "long chrome rifle with bone inlay", "smoked glasses pushed up; a photograph in the collar")]
P.append("<table><tr><th>person</th><th>fashion lineage</th><th>materials</th><th>dominant</th><th>accent</th><th>silhouette</th><th>signature item</th><th>identity detail</th></tr>" + "".join("<tr>" + "".join(f"<td>{html.escape(c)}</td>" for c in r) + "</tr>" for r in spec) + "</table>")
SEL = {"scout": R("R3b-wB-scout-"), "fixer": R("R3b-w-fixer-"), "tech": R("R3b-wB-tech-"), "enforcer": R("R3b-wB-enforcer-")}
for role, rs in SEL.items():
    P.append(f"<h3>{role.capitalize()}: takes</h3><div class=grid>")
    for r in rs:
        key = r["id"].replace("R3b-wB-", "").replace("R3b-w-", ""); n = NOTES[key]
        P.append(rfig(r, 250, f"<br>Secondary: {html.escape(n[0])}<br>Cloth risk: {html.escape(n[1])}<br>Bones: {html.escape(n[2])}"))
    P.append("</div>")

# animation notes table
P.append("<h3>Animation notes (estimates from the drawings; no rig built)</h3><p class=note>Base rig: the 65-bone Mixamo humanoid of the pipeline. 'Extra bones' are over those 65. Cloth-sim risk is whether the garment needs sim or heavy corrective skinning; the set is built so none needs a cloth sim. One lineup adds about 16 to 18 bones over 4 x 65, roughly 7 percent.</p>")
P.append("<table><tr><th>design</th><th>secondary motion</th><th>cloth-sim risk</th><th>extra bones</th><th>note</th></tr>" + "".join(f"<tr><td>{k}</td>" + "".join(f"<td>{html.escape(x)}</td>" for x in v) + "</tr>" for k, v in NOTES.items()) + "</table>")
P.append("<table class=num><tr><th>lineup</th><th>takes</th><th>extra bones</th></tr><tr><td>WB1</td><td style='text-align:left'>trail runner, knit tunic, canvas apron, lacquer yoke</td><td>+2 +10 +5 +1 = +18 (and 2 IK constraints)</td></tr><tr><td>WB2</td><td style='text-align:left'>fin glider, leather jacket, tool belt, lacquer staff</td><td>+3 +8 +4 +1 = +16</td></tr><tr><td>WB3</td><td style='text-align:left'>trail runner, knit jumpsuit, canvas bell (flagged), ring collar</td><td>+2 +8 +6 +2 = +18</td></tr></table>")

# 2 palettes
P.append("<h2>2. Palettes and the signals</h2><p class=note>Gate: dE2000 20 or more from cone red #e03a2f, guard amber #f0a020, rival violet #8a4fd0 (unlit). Also checked against the city's own palette (cream, peach, mint, seafoam, deep teal, turquoise, cobalt, flamingo, sunshine, asphalt, park, lagoon): none of the four is cream or seafoam. The search space is narrow: of 31 candidate colours only 17 clear both the signals and the city; the best four-set is separated by dE 23 at most.</p>")
P.append("<table class=num><tr><th>person</th><th>role</th><th>hex</th><th>L</th><th>vs cones</th><th>vs guard</th><th>vs rivals</th><th>nearest city colour (dE)</th></tr>")
for role, v in C["targets"].items():
    for k in ("dom", "acc"):
        t = v[k]; P.append(f"<tr><td style='text-align:left'>{role}</td><td style='text-align:left'>{'dominant' if k=='dom' else 'accent'}</td><td><span class=sw style='background:{t['hex']}'></span> {t['hex']}</td><td>{t['L']}</td><td>{t['cones']}</td><td>{t['guard']}</td><td>{t['rivals']}</td><td style='text-align:left'>{t['city_nearest']} ({t['city_min']})</td></tr>")
P.append("</table><p class=note>Tightest margins: Scout graphite accent and Fixer navy against the rivals' violet (24.9 and 27.3), Enforcer chestnut and the Fixer's dusty rose accent against cone red (25.3 and 23.1), Scout khaki against guard amber (25.1; it is a yellowish tan: the one to watch in play). The Tech's pale aqua is dE 10 to 13 from the city's mint and turquoise: fine as a small accent and a lamp, not as a dominant.</p>")
P.append("<table class=num><tr><th>dominant colours, pairwise dE</th>" + "".join(f"<th>{k}</th>" for k in C["mutual"]) + "</tr><tr><td style='text-align:left'>palette B (final)</td>" + "".join(f"<td>{v}</td>" for v in C["mutual"].values()) + "</tr><tr><td style='text-align:left'>palette A (first try, graphite, navy, olive, cocoa)</td>" + "".join(f"<td>{v}</td>" for v in C["mutual_A"].values()) + "</tr></table>")
P.append("<p class=note>Palette A was drawn first and rejected: its dominants were 12.6 to 20.5 apart and every painted garment came out near black. Palette B swapped graphite for khaki, olive for dark moss and cocoa for chestnut.</p>")
P.append("<h3>As painted, at 50 px</h3><p class=note>Per figure: the largest colour clusters of the painting after removing skin and near-black outlines (crude), then the average dE between two figures' cluster sets (each cluster to the nearest in the other, weighted). Larger = the figures differ more in colour.</p>")
P.append("<table class=num><tr><th>lineup</th><th>min pair</th><th>mean pair</th><th>reading</th></tr>")
for k, v in A["lineups"].items():
    P.append(f"<tr><td style='text-align:left'>{html.escape(k)}: {html.escape(v['label'])}</td><td>{v['min_colour_pair']}</td><td>{v['mean_colour_pair']}</td><td style='text-align:left'>{'one uniform palette' if v['mean_colour_pair']<5 else 'four palettes'}</td></tr>")
P.append("</table><p class=note>The wardrobes are about three times further apart in colour than the uniform rows, but the absolute figures (about 7 to 13) are modest: the painted garments render darker and duller than their target hexes (matte leather, canvas and knit in soft light). To read apart at 50 px by colour alone, lift the base values (khaki, moss and chestnut each about 10 L higher) or give the accents bigger areas. The 50 px views in section 4 already read as four different people.</p>")

# 3 motifs and lineups
P.append("<h2>3. Team thread and lineups</h2><p class=note>Three motifs proposed, one per lineup, same four people each time: <b>M1 Visitor's pin</b> (a chrome ring-with-a-bar pin, worn on the pack strap, collar, visor edge, shoulder), <b>M2 Chrome chevron</b> (a small chrome chevron badge on the left upper arm or shoulder, in each person's own material), <b>M3 Lamp bead</b> (a small chrome-rimmed bead that glows pale aqua, on the earpiece, case latch, visor, rifle; it lights when that person acts). All three read in portraits and close-ups; at 50 px none survives as paint (see the x5 thumbnails). M3 is the only one that can survive at 50 px as an emissive sprite with bloom, and it carries a gameplay meaning (who is acting), so it is my pick; its pale aqua is dE 10 from the city's mint, so keep it small and glowing, not a surface. M1 is the fallback for HUD portraits.</p>")
P.append("<div class=wide>" + "".join(rfig(r, 640, f"<br>Takes: " + ", ".join(f"{k} {v.split('-')[1]}" for k, v in r['takes'].items())) for r in R("R3b-lineup-WB")) + "</div>")
P.append("<h3>The comparison: L1 uniform / animatable uniform / four wardrobes</h3><div class='wide tall'>" + fig(DEL / "lineups/comparison-L1-U-WB1.jpg", 1200, "<b>Top:</b> round 3 L1 (white on charcoal, long coats). <b>Middle:</b> the same look made animatable (hip-length). <b>Bottom:</b> WB1, four wardrobes with the Visitor's pin.") + "</div>")
P.append("<div class=wide>" + rfig(by["R3b-lineup-U-uniform-animatable"], 640) + "</div>")

# 4 silhouettes
P.append("<h2>4. Silhouette test at 50 px</h2><p class=note>Black fills of the five lineups, and the whole lineup at play scale: tallest figure 56 px after the 38 degree camera's foreshortening (height x cos 38), shown at native size and at 5x. Profile distance = how different the width-over-height outlines are (higher = more distinct; the closest pair matters). IoU = true-scale silhouette overlap (lower = more distinct). Figures: 0 Scout, 1 Fixer, 2 Tech, 3 Enforcer.</p>")
P.append("<table class=num><tr><th>lineup</th><th>mean profile dist</th><th>closest pair (dist)</th><th>mean IoU</th><th>max IoU</th></tr>" + "".join(f"<tr><td style='text-align:left'>{html.escape(k)}</td><td>{v['mean_profile_dist']}</td><td>{v['closest_pair']} ({v['min_profile_dist']})</td><td>{v['mean_iou']}</td><td>{v['max_iou']}</td></tr>" for k, v in A["lineups"].items()) + "</table>")
P.append("<p class=note>Reading: the animatable uniform is about the same as L1 in outline (hems shorter, shape similar). Wardrobes change outlines little in numbers except WB3 (rifle on the shoulder, canvas bell), which has the highest separation; WB2 (staff upright, apron-and-belt) the lowest. The 50 px view shows the real gain: the wardrobes read as four different people by colour and the yoke and shoulder-rifle poses make nameable shadows; the uniform rows read as one squad. The Enforcer stays the nameable 'door' only in the staff pose.</p>")
P.append(f'<div class=sil><img src="silhouettes/overview-50px-x3.png" style="height:640px"><div class=legend>Rows: L1 (round 3), animatable uniform, WB1, WB2, WB3 at 3x of the 50 px scale (colour cut-out left, black fill right).</div></div>')
for k, v in A["lineups"].items():
    O = "silhouettes/" + k
    P.append(f'<div class=sil><div><b>{html.escape(k)}</b> <span class=legend>{html.escape(v["label"])}<br>profile {v["mean_profile_dist"]}, closest {v["closest_pair"]} ({v["min_profile_dist"]}), IoU {v["mean_iou"]}</span><br><img src="{O}-black.png" style="width:560px"></div><div class=legend>50 px native<br><img class=px src="{O}-thumb50-colour.png"> <img class=px src="{O}-thumb50-black.png"><br>5x<br><img class=px src="{O}-thumb50-colour-x5.png"> <img class=px src="{O}-thumb50-black-x5.png"></div></div>')
for role in ("scout", "fixer", "tech", "enforcer"):
    P.append(f'<div class=sil><div class=legend><b>{role}</b> takes as shadows (left to right: {", ".join(x.split("-",3)[-1] for x in A["takes"][role])})</div><img src="silhouettes/takes-{role}-black.png" style="max-width:1900px;height:260px"></div>')

# 5 sheets
P.append("<h2>5. Cosplay sheets (top take per person)</h2><p class=note>Front and back turnaround and three numbered callouts each. Back views are the model's invention from the front: check before building. Scout, Fixer, Tech and Enforcer tops are the trail runner, knit tunic, canvas apron and lacquer yoke (the Enforcer's across-the-shoulders pose Sky liked).</p><div class='wide xw'>")
for r in R("R3b-sheetB-"):
    c = r["callouts"]; P.append(fig(DEL / r["file"], 980, f'<b>{html.escape(r["id"])}</b><div class=legend>1 = {html.escape(c["c1"])}<br>2 = {html.escape(c["c2"])}<br>3 = {html.escape(c["c3"])}</div>'))
P.append("</div>")

# 6 earlier stage + iteration
P.append("<h2>6. Stage 1: the L1 look made animatable (white on charcoal), superseded by section 1</h2><p class=note>Twelve takes plus the carried-over Scout. All meet the hem rules; kept for the comparison row and because they are the cheapest rig variants.</p><div class=grid>")
for r in R("R3b-enforcer-") + R("R3b-fixer-") + R("R3b-tech-"):
    k = r["id"].replace("R3b-", ""); P.append(rfig(r, 220, f"<br>{html.escape(UNIF_NOTES.get(k, ''))}"))
for f in sorted((DEL / "takes").glob("R3-2-scout-*.png")): P.append(fig(f, 220, f"<b>{f.stem}</b><br><i>carried over from round 3 unchanged</i>"))
P.append("</div>")
its = R("iter-")
P.append("<h2>7. Iteration log</h2><p class=note>Palette A wardrobes (graphite, navy, olive, cocoa: too dark, too close), a first set of lineups that Codex drew as portrait pictures with feet cropped (fixed by checking the aspect and retrying), and palette A cosplay sheets.</p><div class=grid>")
for r in its: P.append(rfig(r, 220))
for r in R("R3b-w-scout-") + R("R3b-w-tech-") + R("R3b-w-enforcer-"): P.append(rfig(r, 220, "<br>palette A"))
P.append("</div></section>")
(DEL / "index.html").write_text("".join(P))

def prow(r):
    d = {k: r.get(k) for k in ("id", "set", "model", "model_id", "prompt", "seed", "cost", "file", "size", "sha256", "sha256_png", "hook", "at", "secs", "refnotes")}
    d["refs"] = [("refs/" + Path(x).name if str(x).startswith(str(SCR / "refs")) else rel(x) if str(x).startswith(str(DEL)) else str(Path(x).relative_to(ROOT)) if str(x).startswith(str(ROOT)) else str(x)) for x in r.get("refs", [])]
    for k in ("role", "outfit", "silhouette", "signature", "accent", "palette", "materials", "lineage", "detail", "motif", "motif_name", "takes", "callouts", "palette_set", "take"): 
        if k in r: d[k] = r[k]
    d["note"] = "Codex image tool: prompt verbatim, unseeded, no per-image cost."; return d
deliv = [r for r in rows if r["set"].startswith("R3b") or r["set"] == "iteration"]
carried = [dict(id=f.stem, file=rel(f), sha256=sha(f.read_bytes()), note="copied unchanged from round 3 (briefs/012/cell/round3/2_icons), see round 3 PROVENANCE.json for its prompt and refs") for f in sorted((DEL / "takes").glob("R3-2-scout-*.png"))]
prov = dict(brief="012 round 3b", owner="Forma", date="2026-10-15", spent_total_usd=round(spent(), 4), spent_round3b_usd=round(spent() - PROBE, 4), ceiling_usd=3.0, model="Codex image tool only",
            counts=dict(images=len(deliv), stage1_takes=len(R("R3b-enforcer-") + R("R3b-fixer-") + R("R3b-tech-")), wardrobes_B=len(R("R3b-wB-")), wardrobes_fixer=len(R("R3b-w-fixer-")), lineups=len(R("R3b-lineup-")), sheets=len(R("R3b-sheetB-")), iteration=len(its) + len(R("R3b-w-scout-")) + len(R("R3b-w-tech-")) + len(R("R3b-w-enforcer-"))),
            motifs={k: dict(name=v[0], text=v[1]) for k, v in p7.MOTIFS.items()}, palettes=C, analysis=A, animation_notes=NOTES, carried_over=carried, images=[prow(r) for r in sorted(deliv, key=lambda r: r["id"])])
(DEL / "PROVENANCE.json").write_text(json.dumps(prov, indent=1))
print(prov["counts"], "spent", prov["spent_total_usd"])
