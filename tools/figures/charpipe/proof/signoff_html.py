"""Sign-off page (brief 069): signoff/signoff.html from signoff_plan.py's jobs.json, signoff_render.py's pictures and the pipeline's data files.
  python3 -I tools/figures/charpipe/proof/signoff_html.py <signoff dir>
Every picture is a strip of renders with a line under each; the tables (how each part is made and fitted, what layers with what, counts, colour) are
read from wardrobe.json, roles.json and palettes.json."""
import base64, html, io, json, os, sys
from PIL import Image, ImageDraw

D = sys.argv[1]; IMG = os.path.join(D, 'img')
HERE = os.path.dirname(os.path.abspath(__file__)); CP = os.path.dirname(HERE)
PLAN = json.load(open(os.path.join(D, 'jobs.json'))); JOBS = {j['key']: j for j in PLAN['jobs']}
WARD = json.load(open(os.path.join(CP, 'wardrobe.json'))); ROLES = json.load(open(os.path.join(CP, 'roles.json')))['roles']; PAL = json.load(open(os.path.join(CP, 'palettes.json')))
E = html.escape


def meta(key):
    p = os.path.join(IMG, key + '.json'); return json.load(open(p)) if os.path.exists(p) else {}


def strip(tiles, scale=1, label_h=34, wrap=24):
    """tiles: [(png path, caption)] -> one image, each tile with its line under it."""
    ims = [(Image.open(p).convert('RGB'), c) for p, c in tiles if os.path.exists(p)]
    if not ims: return ''
    w, h = ims[0][0].size; w, h = w * scale, h * scale
    out = Image.new('RGB', (w * len(ims), h + label_h), (247, 244, 236)); d = ImageDraw.Draw(out)
    for i, (im, c) in enumerate(ims):
        out.paste(im.resize((w, h), Image.NEAREST) if scale != 1 else im, (i * w, 0))
        lines = [c[k:k + wrap] for k in range(0, len(c), wrap)][:3]
        for li, t in enumerate(lines): d.text((i * w + 4, h + 2 + li * 11), t, fill=(23, 48, 47))
    b = io.BytesIO(); out.save(b, 'PNG', optimize=True)
    return f'<img src="data:image/png;base64,{base64.b64encode(b.getvalue()).decode()}">'


def made(part, spec):
    k = spec['kind']; extra = []
    if spec.get('tubes'): extra.append('hung rings (' + ', '.join(t['name'] for t in spec['tubes']) + ')')
    if spec.get('collar'): extra.append('a collar ring')
    if spec.get('curtain'): extra.append('a hair curtain')
    if spec.get('hood'): extra.append('a hood shell')
    if spec.get('bag'): extra.append('a strap and case' if not spec['bag'].get('noStrap') else 'a holster case')
    if spec.get('shoulders'): extra.append('shoulders pushed out')
    if k == 'paint':
        how = 'body faces taken by rule (cut on straight lines) and pushed out ' + str(spec.get('offsetMm', 0)) + ' mm' + ('; plus ' + ', '.join(extra) if extra else '')
    elif k == 'dome': how = 'a dome built over the head and hair' + (' with a visor' if spec.get('visor') else '') + ('; hair paint under it' if spec.get('paint') else '')
    elif k == 'peaked': how = 'a band, a wider crown and a visor built over the head and hair'
    elif k == 'pillbox': how = 'a small cylinder set on the crown'
    else: how = k
    fit = ('recomputed on each body after its sliders (same rule, any body), weights = the body\'s own on the same faces' if k == 'paint' and not extra else
           'recomputed on each body after its sliders; new geometry takes the body\'s weights by Data Transfer (nearest face), hems blended to hips and thighs' if k == 'paint' else
           'built on each body\'s head after its sliders, clearing hair; rigid to the head')
    return how, fit


def main():
    R = PLAN['ranges']; S = ['height', 'weight', 'muscle', 'age', 'proportions']
    sec = []
    # 1 bodies
    b = ['<h2>1 · The bodies: two base bodies, each slider across the range the generator uses</h2>',
         '<p>Ranges are the union over every role in <code>roles.json</code> (per sex where split): ' + E('; '.join(f'{sx}: ' + ', '.join(f'{s} {R[sx][s][0]}-{R[sx][s][1]}' for s in S) for sx in ('m', 'f'))) +
         '. Each column moves one slider (low, middle, high), the others at the middle. MPFB macros, 0..1; age 0.5 is about 25, 1.0 about 90. Skin is one tone here; the skin range is a colour (section 4).</p>']
    for sx in ('m', 'f'):
        b.append(strip([(os.path.join(IMG, f'body_{sx}_{s}_{t}__front.png'), f'{s} {t} · {JOBS[f"body_{sx}_{s}_{t}"]["macros"][s]} · {meta(f"body_{sx}_{s}_{t}").get("heightM", "")} m')
                        for s in S for t in ('low', 'mid', 'high')]))
    b.append('<p class="flaw">Flaws: weight and muscle change the outline only a little at these ranges (MPFB is realistic; the ranges were kept inside ordinary builds); proportions barely shows; the women\'s height range sits low (1.52-1.68 m with hair). The low proxy (1.6k quads) keeps faces simple.</p>')
    b.append('<h3>Mixed bodies, as the rules draw them</h3>')
    b.append(strip([(os.path.join(IMG, f'{k}__{v}.png'), JOBS[k]['label'][:60]) for k in sorted(j for j in JOBS if j.startswith('mix_')) for v in ('front', 'q3')], wrap=30))
    sec.append('\n'.join(b))
    # 2 wardrobe
    w = ['<h2>2 · The wardrobe: every part, both bodies, the reference and the slider ends</h2>',
         '<p>Each part is shown over the smallest outfit it layers on (a coat over a jacket and trousers, a hat over a crop), all in one neutral colourway (main turquoise, base cream, dark ink, line chrome) so the shapes compare. Columns: reference front, reference three-quarter, height low, height high, weight low, weight high, muscle high; man then woman.</p>',
         '<table><tr><th>slot</th><th>part</th><th>how it is made</th><th>how it is fitted</th><th>triangles on LOD0 (whole figure, man ref)</th></tr>']
    for part, spec in WARD['parts'].items():
        how, fit = made(part, spec)
        w.append(f'<tr><td>{E(spec["slot"])}</td><td><b>{E(part)}</b><br>{E(spec.get("label", ""))}</td><td>{E(how)}</td><td>{E(fit)}</td><td>{meta(f"part_{part}_m_ref").get("tris", "")}</td></tr>')
    w.append('<tr><td>hair</td><td><b>buzz</b></td><td>hair-coloured paint on the scalp, no geometry</td><td>the body\'s own faces</td><td></td></tr>')
    w.append('<tr><td>base</td><td><b>body, eyes, boots, gloves</b></td><td>paint layers on the body (eyes: the two faces nearest each eye joint)</td><td>the body\'s own faces</td><td></td></tr></table>')
    w.append('<p class="flaw"><b>None of these uses MPFB\'s own clothes fitting (MHCLO).</b> They are recipes on the body\'s own topology: a paint part is the body\'s faces, so it follows any slider and can\'t poke through; hung rings, caps and hoods are rebuilt from each body after its sliders. MPFB\'s system assets have clothes and hair as MHCLO (suits, casual and work wear, fedoras, ponytails, braids), fitted by MPFB to any body with its weights; none of them is used, because they are present-day or 1950s cuts. Authoring our parts as MHCLO (MPFB\'s MakeClothes) is the by-construction alternative for anything with its own volume (a full coat, a cape collar, a long ponytail); it is not done.</p>')
    for part in list(WARD['parts']) + ['buzz', 'gloves']:
        tiles = []
        for sx in ('m', 'f'):
            for tag, v in (('ref', 'front'), ('ref', 'q3'), ('height_low', 'front'), ('height_high', 'front'), ('weight_low', 'front'), ('weight_high', 'front'), ('muscle_high', 'front')):
                k = f'part_{part}_{sx}_{tag}'
                if k in JOBS and v in JOBS[k]['views']: tiles.append((os.path.join(IMG, f'{k}__{v}.png'), f'{sx} {tag.replace("_", " ")}{" 3/4" if v == "q3" else ""}'))
        if part in ('buzz', 'gloves'):
            tiles = [(os.path.join(IMG, f'{JOBS[k]["key"]}__{v}.png'), JOBS[k]['label']) for k in JOBS if k.startswith(f'part_{part}_') for v in JOBS[k]['views']]
        w.append(f'<h3>{E(part)}</h3>' + strip(tiles, wrap=22))
    w.append('<p class="flaw">Flaws seen in these pictures: the tunic\'s paint runs to the crotch, so over bare legs it reads like briefs (it is meant to stop at the hip line); on the tallest man a few thigh faces show under the coat and shift hems; painted lines (zip, sleeve and hem lines) are blocky on the low proxy and LOD1 leaves the finest out; the apron is a flat front panel; the pillbox is small at play zoom; caps and hoods are smooth shells with no brim detail; hands and faces are the proxy\'s (fingers at LOD0, mittens at LOD1). Each part follows every body without a per-body adjustment.</p>')
    sec.append('\n'.join(w))
    # 3 combinations
    c = ['<h2>3 · What layers with what, and how many outfits per role before colour</h2>', '<table><tr><th>layer (low to high)</th><th>part</th><th>slot</th><th>rules from the data</th></tr>']
    for part, spec in sorted(WARD['parts'].items(), key=lambda kv: kv[1]['layer']):
        rules = []
        for t in spec.get('tubes', []):
            if t.get('skipIf'): rules.append(f'its {t["name"]} ring is left out under ' + ', '.join(t['skipIf']))
        if spec.get('replaces'): rules.append('replaces ' + ', '.join(spec['replaces']) + ' (picked out)')
        if part == 'dress_shift': rules.append('takes the legs slot (no trousers or skirt)')
        rs = [r for r in spec.get('regions', []) if r.get('skipIf') or r.get('onlyIf')]
        for r in rs: rules.append(('drops' if r.get('skipIf') else 'adds') + f' its {r["seg"]} {r["role"]} paint ' + ('under ' if r.get('skipIf') else 'with ') + ', '.join(r.get('skipIf', []) + r.get('onlyIf', [])))
        rules.append('paints over every lower layer it covers; a hung ring takes the colour of whatever hangs under it')
        c.append(f'<tr><td>{spec["layer"]}</td><td>{E(part)}</td><td>{E(spec["slot"])}</td><td>{E("; ".join(rules))}</td></tr>')
    c.append('</table><p>Every slot holds one part; any part in a slot layers with any part in another (one rule set, no pairs listed by hand), except the rules above. So a coat goes over any top and either legs (over a skirt it replaces the skirt\'s ring), any hat over any hair (a hood replaces a puff or a bob), the apron over the tunic, the yoke and duty belt over the jacket or coat.</p>')
    c.append('<table><tr><th>role</th><th>slots and the parts each may hold (weight &gt; 0)</th><th>outfits, man</th><th>outfits, woman</th></tr>')
    for rn, role in ROLES.items():
        sl = '<br>'.join(E(f'{s}: ' + ', '.join(k for k, v in o.items() if (max(v.values()) if isinstance(v, dict) else v) > 0)) for s, o in role['slots'].items())
        c.append(f'<tr><td><b>{E(rn)}</b></td><td>{sl}</td><td>{PLAN["combos"][rn]["m"]}</td><td>{PLAN["combos"][rn]["f"]}</td></tr>')
    c.append('</table><p class="cap">Counted by signoff_plan.py from roles.json: every combination of the parts a role allows for that sex, after the rules that remove parts. A "none" counts as an option. Body and colour multiply these.</p>')
    sec.append('\n'.join(c))
    # 4 colour
    col = ['<h2>4 · Colour: the palettes per role, and one person from each role\'s rules in every main colour it allows</h2>']
    def chip(n):
        h = PAL['colours'][n]['hex']; return f'<span class="chip" style="background:{h}" title="{E(n)} {h}"></span><span class="cn">{E(n)}</span>'
    col.append('<table><tr><th>role</th><th>colour role</th><th>colours (from palettes.json)</th></tr>')
    for rn in ('commuter', 'guard', 'rival', 'cafe', 'clerk'):
        for k, src in ROLES[rn]['colours'].items():
            if k == 'E': continue
            if isinstance(src, str): names = PAL['sets'].get(src, PAL['sets'].get('hair_young', []) + PAL['sets'].get('hair_old', []) if src == 'hair' else [])
            elif isinstance(src, dict): names = [x for x in src if not x.startswith('same:')] + [f'({x})' for x in src if x.startswith('same:')]
            else: names = src
            cells = ' '.join(chip(n) if n in PAL['colours'] else E(n) for n in dict.fromkeys(names))
            col.append(f'<tr><td>{E(rn)}</td><td>{E(k)}</td><td>{cells}</td></tr>')
    col.append('</table><p class="cap">S skin, HR hair, M main block, B base, T trim, TL thin line (brass only here), BT boots, H hat, L legs, LT legs under a skirt, G gloves, D the dark step, AP apron, BG bag. (same:X) = the colour picked for X.</p>')
    for rn in ('commuter', 'guard', 'rival', 'cafe'):
        ks = sorted(k for k in JOBS if k.startswith(f'colour_{rn}_'))
        col.append(f'<h3>{E(rn)}</h3>' + strip([(os.path.join(IMG, f'{k}__q3.png'), k.split(f"colour_{rn}_")[1]) for k in ks]))
    col.append('<p class="flaw">Flaws: colour is flat per face (no fabric texture); the guard set is two ambers, the cafe set three seafoams: uniform by design, but little variety; commuters\' main colours are all cool or red (the guard and rival hue windows are kept out, as Glyph asked).</p>')
    sec.append('\n'.join(col))
    # play zoom
    pz = ['<h2>At play zoom (40-60 px)</h2>', '<p>Real people from the rules, two per role, at the play camera\'s scale (1080 px over 28 m, 38 degrees down, yaw 30), EEVEE light, not the game\'s. Shown 1:1 and at 4x.</p>']
    ks = sorted(k for k in JOBS if k.startswith('play_'))
    pz.append(strip([(os.path.join(IMG, f'{k}__play.png'), JOBS[k]['label']) for k in ks], wrap=10))
    pz.append(strip([(os.path.join(IMG, f'{k}__play.png'), JOBS[k]['label']) for k in ks], scale=4, wrap=40))
    sec.append('\n'.join(pz))
    # 5 gaps
    g = ['<h2>5 · Gaps: what the city\'s roles need that isn\'t here (not built)</h2><ul>']
    for t in ['Roles without rules yet: the driver family; market, trade and service workers (job/market, job/service, job/trade); cinema, nightlife and lobby staff; posted security and response officers as their own look (they share the guard now); residents beyond the commuter look.',
              'Work clothes: a coverall, a work smock, a tool belt, short sleeves for trades.',
              'Evening and civic wear: a long-sleeved dress, an evening coat or cape collar (the cape collar was dropped: built on the T-pose rest it went through the arms; it needs MHCLO fitting or an arms-down fit).',
              'Hair: a bun, a ponytail, braids, long hair (only crop, bob, puff and buzz exist); facial hair (Vale\'s moustache is a hero part).',
              'Heads: helmets and visors from the board (bubble, dome with a visor shield), a headband, glasses and goggles.',
              'Accessories: a case in the hand, a transparent case, an umbrella (props, not garments), jewellery and badges.',
              'Shoes: one boot shape for everyone (colours only).',
              'Bodies: children; very heavy or very thin builds (the ranges stop inside ordinary builds); the skin set is six tones.',
              'Detail: fabric textures and a mask shader (the channel is already in UV2); faces beyond the proxy\'s.',
              'Heroes (the cell, Vale, Inez, the driver): their own garments on the same base, out of this proof.']:
        g.append(f'<li>{E(t)}</li>')
    g.append('</ul>')
    sec.append('\n'.join(g))
    nparts = len(WARD['parts']) + 1
    slots = {}
    for p, s in WARD['parts'].items(): slots.setdefault(s['slot'], []).append(p)
    slots.setdefault('hair', []).append('buzz')
    summary = (f'<p><b>{len([j for j in JOBS if j.startswith("body_")])} body pictures</b> (two base bodies x 5 sliders x low/mid/high) and {len([j for j in JOBS if j.startswith("mix_")])} mixed. '
               f'<b>{nparts} parts</b> in {len(slots)} slots: ' + E('; '.join(f'{s} {len(v)} ({", ".join(v)})' for s, v in slots.items())) + '. Base layers: body, eyes, boots, gloves.</p>')
    page = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Brief 069 · sign-off: bodies and clothes</title><style>
body{{font:14px/1.5 -apple-system,Helvetica,Arial,sans-serif;background:#f7f4ec;color:#17302f;margin:0;padding:20px 32px}}
h1{{font-size:24px;margin:0}} h2{{font-size:19px;margin:30px 0 8px;border-bottom:2px solid #d2e6e2}} h3{{font-size:15px;margin:16px 0 4px}}
img{{display:block;max-width:100%;margin:6px 0;border:1px solid #d2e6e2;image-rendering:pixelated}}
table{{border-collapse:collapse;margin:8px 0;font-size:13px}} td,th{{border:1px solid #cbc6c3;padding:3px 7px;vertical-align:top;text-align:left}} th{{background:#e9efe9}}
.flaw{{background:#fff;border-left:4px solid #f0a020;padding:6px 12px}} .cap{{font-size:12px;color:#4a5f5d}} code{{background:#eee8dc;padding:0 3px}}
.chip{{display:inline-block;width:14px;height:14px;border:1px solid #888;vertical-align:middle;margin-left:6px}} .cn{{font-size:11px;margin-left:2px}}
</style></head><body><h1>Brief 069 · sign-off: the bodies and the clothes</h1>
<p class="cap">Every picture is the pipeline's own output: <code>tools/figures/charpipe/proof/signoff_plan.py</code> reads roles.json, wardrobe.json and palettes.json; <code>signoff_render.py</code> makes each body and outfit with the pipeline's build code (MPFB body, the reference T-pose rest, the 52-bone rig, the wardrobe on LOD0) and draws it with the arms lowered for the picture; this page is written by <code>signoff_html.py</code>. Static, no animation, no Unity. Flaws are named under the pictures.</p>
{summary}
{''.join(sec)}</body></html>"""
    open(os.path.join(D, 'signoff.html'), 'w').write(page); print('wrote', os.path.join(D, 'signoff.html'), round(len(page) / 1e6, 1), 'MB')


main()
