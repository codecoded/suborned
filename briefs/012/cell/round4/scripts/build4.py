import base64, html, io, json, shutil
from pathlib import Path
from PIL import Image
from lib import *
import p11, p12

rows_all = read_ledger(); by = {r["id"]: r for r in rows_all}; rows = list(by.values())
R = lambda pre: sorted([r for r in rows if r["id"].startswith(pre)], key=lambda r: r["id"])
SD = DEL / "scripts"; SD.mkdir(exist_ok=True); (DEL / "refs").mkdir(exist_ok=True)
for f in Path(__file__).parent.glob("*.py"): shutil.copy(f, SD / f.name)
for f in (SCR / "refs").glob("*.png"): shutil.copy(f, DEL / "refs" / f.name)
for n in ("analysis.json", "colours4.json", "hero_share.json"):
    p = SCR / n if (SCR / n).exists() else None
    if p: shutil.copy(p, DEL / n)
if (SCR / "analysis.json").exists(): pass
A = json.load(open(SCR / "analysis.json")); C = json.load(open(SCR / "colours4.json")); HS = json.load(open(SCR / "hero_share.json"))
R3B = ROOT / "briefs/012/cell/round3b"; R3 = ROOT / "briefs/012/cell/round3"
cmp = [R3 / "2_icons/lineups/R3-2-lineup-L1.png", R3B / "lineups/R3b-lineup-WB1.png", DEL / "lineups/R4-lineup-L1.png", DEL / "lineups/R4-lineup-L4.png"]
M = Image.new("RGB", (1200, 3200), "white")
for i, p in enumerate(cmp): M.paste(Image.open(p).convert("RGB").resize((1200, 800)), (0, i * 800))
M.save(DEL / "lineups/comparison-L1-WB1-R4L1-R4L4.jpg", quality=90)
def thumb(path, w):
    im = Image.open(path).convert("RGB"); im.thumbnail((w, w * 2)); b = io.BytesIO(); im.save(b, "JPEG", quality=78, optimize=True)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()
rel = lambda p: str(Path(p).relative_to(DEL))
def fig(path, w, cap=""): return f'<figure><a href="{html.escape(rel(path))}"><img loading="lazy" src="{thumb(path, w)}"></a><figcaption>{cap}</figcaption></figure>'
def rfig(r, w, extra=""): return fig(DEL / r["file"], w, f'<span class="b codex">Codex</span> <b>{html.escape(r["id"])}</b>{extra}<br><i>{html.escape(r.get("hook",""))}</i>')

NOTES = {
 "scout-1-mod-runner": ("pack (follow-through), collar-hood, earpiece cord baked; periscope mast rigid on the pack", "none: vinyl jacket is a rigid shell skinned to chest and arms, patent shorts skinned", "+2 (pack 1, collar-hood 1)", "vinyl = hard specular and normal-mapped seams, no sim; go-go boots rigid"),
 "scout-2-cut-out-fins": ("pack, two fin hinges (fold/open)", "none; cut-outs are mesh holes over an underlayer", "+3 (pack 1, fins 2)", "fins are rigid plates"),
 "scout-3-quilted-dish": ("pack, dish (spin or fold hinge)", "none; quilting is a normal map", "+2 (pack 1, dish 1)", "dish rigid, periscope rigid"),
 "scout-4-hero-block": ("pack, collar-hood, periscope rigid", "none", "+2 (pack 1, collar-hood 1)", "same rig as mod-runner, more colour"),
 "fixer-1-ring-collar-column": ("ponytail chain, coiled cable chain, case swing", "none; hip-length zip tunic skinned, ring collar rigid", "+8 (ponytail 4, cable 3, case 1)", "ring collar must not clip the ponytail: constrain the first ponytail bone"),
 "fixer-2-chevron-tunic": ("ponytail chain, coiled cable chain, case swing", "none; hip-length tunic skinned", "+8", "chevron seams are texture, not geometry"),
 "fixer-3-yoke-disc": ("ponytail chain, coiled cable chain, case swing", "none; waist cut-out is a mesh hole", "+8", "round cuffs rigid rings"),
 "fixer-4-hero-block": ("ponytail chain, coiled cable chain, case swing", "none", "+8", "same rig as yoke-disc; ring collar rigid"),
 "tech-1-quilted-tunic": ("visor discs (2 hinges), rigid quilted hip panel (1 hinge)", "low: asymmetric mid-thigh hem needs a thigh collision proxy", "+3 (visor 2, hip panel 1)", "quilting is a normal map"),
 "tech-2-quilted-belt": ("visor discs (2 hinges), hanging tools (2 jiggle bones)", "none", "+4", "belt ring skinned to hips, tools rigid props"),
 "tech-3-bell-shell": ("visor discs (2 hinges); bell shorts need hem bones", "low to medium: a rigid flare clips the thigh on stride; use 2 to 4 hem bones or split front and back panels", "+4 to +6", "the bell; keeps the hem at mid-thigh"),
 "tech-4-hero-block": ("visor discs (2 hinges); bell shorts need hem bones", "low to medium (same bell as above)", "+4 to +6", "emerald hero block; same rig as bell-shell"),
 "enforcer-1-yoke-staff": ("rifle socket; cuirass and pauldrons rigid on the chest and shoulders, optional 2 lag bones", "none", "+1 (optional +2)", "staff grip needs hand IK on the rifle"),
 "enforcer-2-yoke-across": ("rifle on the shoulders (prop bone on the chest)", "none", "+1 bone, +2 IK constraints", "arms locked over the rifle: a special carry locomotion set; swap to the hands to fight (animation event)"),
 "enforcer-3-quilted-discs": ("rifle socket, round pauldron discs rigid (optional lag bones)", "none", "+1 (optional +2)", "one-hand shoulder carry; free arm animates normally"),
 "enforcer-4-hero-block": ("rifle socket; cuirass rigid; optional 2 lag bones", "none", "+1 (optional +2)", "same rig as yoke-staff"),
}
P = [f"""<!doctype html><meta charset=utf-8><title>Cell round 4: space age made modern (brief 012)</title><style>
body{{font:14px/1.45 system-ui,sans-serif;margin:0;background:#f4f1ea;color:#222}} header,section.s{{padding:16px 28px}} header{{background:#1e2a2a;color:#eee}}
h1{{margin:0 0 4px}} h2{{border-bottom:2px solid #1e2a2a;padding-bottom:4px;margin-top:44px}} h3{{margin:26px 0 6px}} .note{{max-width:1000px;color:#555;margin:2px 0 10px}}
.grid,.wide{{display:flex;flex-wrap:wrap;gap:12px}} figure{{margin:0;background:#fff;padding:6px;border-radius:6px;box-shadow:0 1px 3px #0002;max-width:320px}} .wide figure{{max-width:640px}} .xw figure{{max-width:980px}} .tall figure{{max-width:1200px}}
figure img{{display:block;max-width:100%;height:auto}} figcaption{{font-size:12px;margin-top:4px}} .b{{font-size:10px;padding:1px 5px;border-radius:8px;color:#fff}} .codex{{background:#7a4a38}}
table{{border-collapse:collapse;font-size:12px;margin:8px 0}} td,th{{border:1px solid #bbb;padding:3px 8px;text-align:left;vertical-align:top}} .num td{{text-align:right}}
.px{{image-rendering:pixelated}} .sil{{display:flex;gap:14px;align-items:flex-start;background:#fff;padding:8px;border-radius:6px;margin:8px 0;flex-wrap:wrap}} .legend{{font-size:12px;margin:2px 0 8px;max-width:640px}} .sw{{display:inline-block;width:14px;height:14px;border:1px solid #888;vertical-align:middle}}
</style><header><h1>Cell round 4: space age, made modern</h1><div>Brief 012 round 4 · Forma · 2026-10-15 · Codex only (ceiling total ${spent():.3f} of $3.00). Faces, bodies, hair, signature items and silhouette shapes kept; animation rules kept (hems at hip or mid-thigh, no capes or tails, ponytail one chain). Thumbnails embedded; click for full files; model, prompt, refs, sha256 in PROVENANCE.json.</div></header><section class=s>"""]
P.append("<h2>Summary</h2><p class=note>Round 3b drifted into generic modern tactical (khaki, moss, chestnut, ripstop, canvas). Round 4 goes back to space-age couture pushed forward with modern construction: white and silver base, vinyl, patent, lacquer, bonded and quilted panels, moulded yokes and collars, ring pulls and round buckles, white boots, gauntlets. Each person takes a different corner of that vocabulary and one bold hero colour; the team thread is the white-and-silver base and the porthole lamp. <b>Top lineup: L4, the hero-block set</b> (each person's colour as the main garment). The first three lineups, with the hero colour only as trim, read at 50 px as one white squad; L4 reads as four people (section 5).</p>")
P.append("<h2>1. Four corners of the vocabulary</h2>")
spec = [("Scout", "mod runner: Courrèges-style cropped moulded jacket, cut-outs, hot-pant shorts, white go-go boots", "moulded vinyl jacket and collar-hood, chevron seams, cut-out windows, quilted panels, ring-pull zips, patent shorts, round buckle belt", "the pack-and-hood (tall moulded pack above the shoulder, bare legs); or twin fins; or a radar dish", "the pack shell with a porthole lamp and a chrome periscope mast", "fresh green-lime #7fd13b", "an earpiece with a lime cord"),
        ("Fixer", "Cardin-style geometric column tunic, circles, ring collar", "bonded-knit zip tunic, chrome O-ring collar, circle cut-out and disc, chevron seams, round silver cuffs, slim silver trousers", "the exclamation mark (narrow column of colour, case at the hip, tall ponytail)", "the silver case with a porthole window and coiled handset cable", "cool candy pink #f48fc0", "a chrome ring on the ponytail"),
        ("Tech", "quilted technician's tunic with disc and ring hardware (Rabanne-like), porthole visor", "quilted bonded panels, rigid moulded bell shorts, wide moulded belt with a round buckle, chrome gauntlets on bare arms", "the bell at the hip, or the jingle of tools", "the porthole visor with two flip-up chrome disc plates, lit pale aqua only when he works", "emerald #0b9a64", "round silver bezels on the gauntlets"),
        ("Enforcer", "sculpted shoulder-yoke cuirass over a knit, gauntlets", "moulded yoke and pauldron discs, chevron seams, white technical knit mock neck, big round buckle, white-and-silver heavy boots", "the door (rifle as a staff) or the yoke (rifle across the shoulders)", "the long chrome rifle with a cerulean inlay", "cerulean #12a0d8", "smoked glasses pushed up; a photograph in the collar")]
P.append("<table><tr><th>person</th><th>corner</th><th>vocabulary used</th><th>silhouette</th><th>signature item</th><th>hero colour</th><th>identity detail</th></tr>" + "".join("<tr>" + "".join(f"<td>{html.escape(c)}</td>" for c in r) + "</tr>" for r in spec) + "</table>")
P.append("<p class=note>Team thread: white (cool, not the city's cream) and brushed silver, charcoal only for small parts, plus the porthole lamp motif (a chrome ring around a lens that glows soft pale aqua; it lights when that person acts), worn on the pack shell, the ring collar, the visor and the belt buckle. At 50 px the lamp is a small emissive dot: as paint it is lost, as a game sprite with bloom it works.</p>")

P.append("<h2>2. Colour</h2><p class=note>Gate: dE2000 20 or more from cone red #e03a2f, guard amber #f0a020, rival violet #8a4fd0 (unlit). Every bright blue that clears the rivals' violet lands within 15 of the city's cobalt or turquoise enamels, and the greens and pinks sit near the city's park green and flamingo; that is flagged below, not avoidable. The base is cool white, not the city's cream (dE 9.8 from cream).</p>")
P.append("<table class=num><tr><th>colour</th><th>hex</th><th>L</th><th>vs cones</th><th>vs guard</th><th>vs rivals</th><th>nearest city colour (dE)</th></tr>")
for role, t in C["hero"].items(): P.append(f"<tr><td style='text-align:left'>{role} hero</td><td><span class=sw style='background:{t['hex']}'></span> {t['hex']}</td><td>{t['L']}</td><td>{t['cones']}</td><td>{t['guard']}</td><td>{t['rivals']}</td><td style='text-align:left'>{t['city_nearest']} ({t['city_min']})</td></tr>")
for n, t in C["base"].items(): P.append(f"<tr><td style='text-align:left'>{n}</td><td><span class=sw style='background:{t['hex']}'></span> {t['hex']}</td><td></td><td>{t['cones']}</td><td>{t['guard']}</td><td>{t['rivals']}</td><td style='text-align:left'>{t['city_nearest']} ({t['city_min']})</td></tr>")
P.append("</table><table class=num><tr><th>hero pairs, dE</th>" + "".join(f"<th>{k}</th>" for k in C["mutual"]) + "</tr><tr><td style='text-align:left'>between heroes</td>" + "".join(f"<td>{v}</td>" for v in C["mutual"].values()) + "</tr><tr><td style='text-align:left'>lamp aqua vs hero</td>" + "".join(f"<td>{k}: {v}</td>" for k, v in C["lamp_vs_hero"].items()) + "</tr></table>")
P.append("<p class=note>Tightest margins: Scout lime to guard amber (30 as targeted), Enforcer cerulean to rival violet (36 targeted, 26 to 29 as painted), Tech emerald to the lamp aqua (27.8) and Scout lime to Tech emerald (22.7: both greens, separated by hue and by shape). Lamp aqua is 10 from the city's mint: keep it a small glow.</p>")
P.append("<h3>As painted</h3><p class=note>Hero pixels of each take (hue mask), median colour and its dE. The first round of heroes (lime #a8d234, rose #f58fb0) painted yellower and redder than targeted: lime reached 22.8 from guard amber and rose 20.4 from cone red, too thin for a rendered, lit character. They were retuned (greener lime, cooler pink) and the Scout and Fixer takes redrawn; the first set is in the iteration log. Painted margins now: lime 29.7 to 32.9 from amber, pink 24.5 to 28.4 from red, cerulean 26 to 29 from violet.</p>")
P.append("<table class=num><tr><th>take</th><th>painted hero</th><th>share of image</th><th>cones</th><th>guard</th><th>rivals</th><th>city min</th></tr>")
for role, d in C["painted"].items():
    for k, x in d.items():
        if x: P.append(f"<tr><td style='text-align:left'>{k}</td><td><span class=sw style='background:{x['painted']}'></span> {x['painted']}</td><td>{x['share']*100:.1f}%</td><td>{x['cones']}</td><td>{x['guard']}</td><td>{x['rivals']}</td><td>{x['city_min']}</td></tr>")
P.append("</table>")
P.append("<h3>How much of each figure is hero colour, and does it read at 50 px</h3><table class=num><tr><th>lineup</th><th>scout</th><th>fixer</th><th>tech</th><th>enforcer</th><th>colour separation at 50 px (min / mean pair)</th></tr>")
lab = {"L1": "R4-L1", "L2": "R4-L2", "L3": "R4-L3", "L4": "R4-L4-hero-block"}
for k, v in HS.items(): P.append(f"<tr><td style='text-align:left'>{k}</td>" + "".join(f"<td>{v[r]*100:.0f}%</td>" for r in ('scout', 'fixer', 'tech', 'enforcer')) + f"<td>{A['lineups'][lab[k]]['min_colour_pair']} / {A['lineups'][lab[k]]['mean_colour_pair']}</td></tr>")
for k in ("L1-round3-uniform", "WB1-round3b-wardrobes"): P.append(f"<tr><td style='text-align:left'>{k}</td><td colspan=4></td><td>{A['lineups'][k]['min_colour_pair']} / {A['lineups'][k]['mean_colour_pair']}</td></tr>")
P.append("</table><p class=note>Hero colour as trim covers 5 to 16 percent of a figure; as the main garment (L4) 16 to 23 percent. Colour separation (average dE between two figures' colour clusters, crude) is 3 to 7 for the trim-only lineups (one white squad), 14 for L4, against 5 for the round 3 uniform and 10 for the round 3b wardrobes.</p>")

P.append("<h2>3. Takes</h2><p class=note>Four per person: three explorations with the hero colour as trim, and a hero-block take with the colour as the main garment (the one I recommend).</p>")
for role in ("scout", "fixer", "tech", "enforcer"):
    P.append(f"<h3>{role.capitalize()}</h3><div class=grid>")
    for r in R(f"R4-{role}-"):
        n = NOTES[r["id"].replace("R4-", "")]; P.append(rfig(r, 250, f"<br>Secondary: {html.escape(n[0])}<br>Cloth risk: {html.escape(n[1])}<br>Bones: {html.escape(n[2])}"))
    P.append("</div>")
P.append("<h3>Animation notes (estimates from the drawings; no rig built)</h3><p class=note>Base rig: 65-bone Mixamo humanoid. Extra bones are over those 65. Vinyl, patent, lacquer and quilting are shader and normal-map work on skinned rigid shells; no garment needs a cloth sim. The lamps are emissive materials (0 bones), driven by the 'acting' state.</p>")
P.append("<table><tr><th>design</th><th>secondary motion</th><th>cloth-sim risk</th><th>extra bones</th><th>note</th></tr>" + "".join(f"<tr><td>{k}</td>" + "".join(f"<td>{html.escape(x)}</td>" for x in v) + "</tr>" for k, v in NOTES.items()) + "</table>")
P.append("<table class=num><tr><th>lineup</th><th>extra bones</th></tr><tr><td>L1</td><td>+2 +8 +5 +1 = about +16</td></tr><tr><td>L2</td><td>+3 +8 +4 +1 = +16 (and 2 IK constraints)</td></tr><tr><td>L3</td><td>+2 +8 +3 +1 = +14</td></tr><tr><td>L4</td><td>+2 +8 +5 +1 = about +16</td></tr></table>")

P.append("<h2>4. Lamp motif and lineups</h2><p class=note>Lamp bead kept and styled as a porthole lamp: a chrome ring around a pale-aqua lens. It reads in all four lineups at close range (Scout pack, Fixer collar, Tech visor, Enforcer belt buckle and rifle).</p>")
P.append("<div class=wide>" + "".join(rfig(r, 640, f"<br>Takes: " + ", ".join(f"{k} {v.split('-')[1]}" for k, v in r['takes'].items())) for r in R("R4-lineup-")) + "</div>")
P.append("<h3>The comparison row</h3><div class='wide tall'>" + fig(DEL / "lineups/comparison-L1-WB1-R4L1-R4L4.jpg", 1200, "<b>1:</b> round 3 L1 (white on charcoal uniform, long coats). <b>2:</b> round 3b WB1 (khaki, navy, moss, chestnut). <b>3:</b> round 4 L1 (space age, hero colour as trim). <b>4:</b> round 4 L4 (space age, hero-block).") + "</div>")

P.append("<h2>5. Silhouette test at 50 px</h2><p class=note>Black fills and the whole lineup at play scale (tallest figure 56 px after the 38 degree foreshortening), native size and 5x. Profile distance: how different the width-over-height outlines are (higher = more distinct; the closest pair matters). IoU: true-scale overlap (lower = more distinct). Figures: 0 Scout, 1 Fixer, 2 Tech, 3 Enforcer.</p>")
P.append("<table class=num><tr><th>lineup</th><th>mean profile dist</th><th>closest pair (dist)</th><th>mean IoU</th><th>colour sep min / mean</th></tr>" + "".join(f"<tr><td style='text-align:left'>{html.escape(k)}</td><td>{v['mean_profile_dist']}</td><td>{v['closest_pair']} ({v['min_profile_dist']})</td><td>{v['mean_iou']}</td><td>{v['min_colour_pair']} / {v['mean_colour_pair']}</td></tr>" for k, v in A["lineups"].items()) + "</table>")
P.append("<p class=note>Reading: in outline the round 4 lineups are about as separable as round 3 L1 (closest pair 0.031 to 0.041 against 0.047), and the yoke and shoulder-rifle poses make the most nameable shadows (L2, L3). At 50 px the first three round 4 rows read as one white-and-silver squad with coloured flecks; the L4 row reads as four different people. The cut of the colour block, not the garment shape, is what makes them separable.</p>")
P.append('<div class=sil><img src="silhouettes/overview-50px-x3.png" style="height:700px"><div class=legend>Rows: round 3 L1, round 3b WB1, round 4 L1, L2, L3, L4, at 3x of the 50 px scale (colour cut-out left, black fill right).</div></div>')
for k, v in A["lineups"].items():
    O = "silhouettes/" + k
    P.append(f'<div class=sil><div><b>{html.escape(k)}</b> <span class=legend>{html.escape(v["label"])}<br>profile {v["mean_profile_dist"]}, closest {v["closest_pair"]} ({v["min_profile_dist"]}), IoU {v["mean_iou"]}</span><br><img src="{O}-black.png" style="width:560px"></div><div class=legend>50 px native<br><img class=px src="{O}-thumb50-colour.png"> <img class=px src="{O}-thumb50-black.png"><br>5x<br><img class=px src="{O}-thumb50-colour-x5.png"> <img class=px src="{O}-thumb50-black-x5.png"></div></div>')
for role in ("scout", "fixer", "tech", "enforcer"):
    P.append(f'<div class=sil><div class=legend><b>{role}</b> takes as shadows (left to right: {", ".join(x.split("-",3)[-1] for x in A["takes"][role])})</div><img src="silhouettes/takes-{role}-black.png" style="max-width:1900px;height:260px"></div>')

P.append("<h2>6. Cosplay sheets</h2><p class=note>Top take per person (hero-block), front and back and three numbered callouts each; back views are the model's invention, check before building. The earlier picks (mod runner, yoke-and-disc, moulded bell, yoke across the shoulders) are shown after them as alternates.</p><div class='wide xw'>")
for r in R("R4-sheet-"):
    pass
tops = [r for r in R("R4-sheet-") if "hero-block" in r["id"]]; alts = [r for r in R("R4-sheet-") if "hero-block" not in r["id"]]
for r in tops + alts:
    c = r["callouts"]; P.append(fig(DEL / r["file"], 980, f'<b>{html.escape(r["id"])}</b>{" (alternate)" if r in alts else ""}<div class=legend>1 = {html.escape(c["c1"])}<br>2 = {html.escape(c["c2"])}<br>3 = {html.escape(c["c3"])}</div>'))
P.append("</div>")
its = R("iter-")
P.append("<h2>7. Iteration log</h2><p class=note>First hero colours (lime #a8d234, rose #f58fb0) painted too close to guard amber and cone red (22.8 and 20.4); Scout and Fixer redrawn with a greener lime and a cooler pink, then the lineups and the two sheets redone.</p><div class=grid>")
for r in its: P.append(rfig(r, 220))
P.append("</div></section>")
(DEL / "index.html").write_text("".join(P))
def prow(r):
    d = {k: r.get(k) for k in ("id", "set", "model", "model_id", "prompt", "seed", "cost", "file", "size", "sha256", "sha256_png", "hook", "at", "secs", "refnotes")}
    d["refs"] = [("refs/" + Path(x).name if str(x).startswith(str(SCR / "refs")) else rel(x) if str(x).startswith(str(DEL)) else str(Path(x).relative_to(ROOT))) for x in r.get("refs", [])]
    for k in ("role", "outfit", "silhouette", "signature", "hero", "corner", "detail", "motif", "takes", "callouts", "take"):
        if k in r: d[k] = r[k]
    d["note"] = "Codex image tool: prompt verbatim, unseeded, no per-image cost."; return d
deliv = [r for r in rows if r["set"].startswith("R4") or r["set"] == "iteration"]
prov = dict(brief="012 round 4", owner="Forma", date="2026-10-15", spent_total_usd=round(spent(), 4), spent_round4_usd=round(spent() - PROBE, 4), ceiling_usd=3.0, model="Codex image tool only",
            counts=dict(images=len(deliv), takes=len(R("R4-scout-") + R("R4-fixer-") + R("R4-tech-") + R("R4-enforcer-")), lineups=len(R("R4-lineup-")), sheets=len(R("R4-sheet-")), iteration=len(its)),
            palettes=C, hero_share=HS, analysis=A, animation_notes=NOTES, images=[prow(r) for r in sorted(deliv, key=lambda r: r["id"])])
(DEL / "PROVENANCE.json").write_text(json.dumps(prov, indent=1)); print(prov["counts"], "spent", prov["spent_total_usd"])
