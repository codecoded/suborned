"""Brief 069 fit test: the self-contained report (fit-test/fit-test.html) from the stage outputs. System python (PIL); media embedded as base64.
  python3 tools/figures/charpipe/fit/report.py"""
import json, os, base64, io, html
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(HERE))))
S = os.path.join(ROOT, 'out/scratch/forma/char-pipeline/fit-test'); D = json.load(open(os.path.join(HERE, 'fit.json'))); V = json.load(open(os.path.join(HERE, 'verdicts.json')))
IDS = [r['id'] for r in D['inputs']]; TH = D['thresholds']
J = lambda p: json.load(open(os.path.join(S, p)))
esc = html.escape


def img(path, w=None, q=80, maxw=1800):
    im = Image.open(path).convert('RGB')
    if im.width > maxw: im = im.resize((maxw, int(im.height * maxw / im.width)))
    b = io.BytesIO(); im.save(b, 'JPEG', quality=q)
    return f'<img src="data:image/jpeg;base64,{base64.b64encode(b.getvalue()).decode()}" style="max-width:100%{";width:" + str(w) + "px" if w else ""}">'


def png(path):
    return f'<img src="data:image/png;base64,{base64.b64encode(open(path, "rb").read()).decode()}" style="image-rendering:pixelated">'


def video(path):
    return f'<video controls loop muted playsinline style="max-width:100%" src="data:video/mp4;base64,{base64.b64encode(open(path, "rb").read()).decode()}"></video>'


def agg(S_, key, sub='max'):
    vals = [(a[key][sub], c) for c, a in S_.items() if c != 'rest']
    return max(vals) if sub == 'max' else min(vals)


def pen(S_):
    out = {}
    for c, a in S_.items():
        if c == 'rest': continue
        for k, v in a['penetrationPct'].items():
            if v['max'] > out.get(k, (0, ''))[0]: out[k] = (v['max'], c)
    return out


def cell(v, lim, hi=True, fmt='{:.2f}'):
    bad = (v > lim) if hi else (v < lim)
    return f'<td class="{"bad" if bad else "ok"}">{fmt.format(v)}</td>'


def clip_table(S_, label):
    rows = [f'<tr><th>{esc(label)}</th><th>stretch p99</th><th>stretch max</th><th>compress p99</th><th>min tri area</th><th>flipped</th><th>penetration %</th><th>hem self</th><th>arm-torso</th><th>thigh-thigh</th><th>prop-body</th><th>flags</th></tr>']
    rest = S_.get('rest', {})
    rows.append(f'<tr class="rest"><td>rest (baseline)</td><td></td><td></td><td></td><td></td><td>{rest.get("flipped", "")}</td><td>{esc(str(rest.get("penetrationPct", "")))}</td><td>{rest.get("hemSelfPairs", "")}</td><td>{rest.get("armTorsoPairs", "")}</td><td>{rest.get("thighPairs", "")}</td><td>{rest.get("propBodyPairs", "")}</td><td></td></tr>')
    for c, a in S_.items():
        if c == 'rest': continue
        p = ', '.join(f'{k} {v["max"]:.1f}' for k, v in a['penetrationPct'].items())
        rows.append('<tr><td>' + c.replace('_', ' ') + '</td>' + cell(a['stretchP99']['max'], TH['edgeStretchP99']) + cell(a['stretchMax']['max'], TH['edgeStretchMax'], fmt='{:.1f}') +
                    cell(a['compressP99']['max'], TH['edgeCompressP99']) + cell(a['triAreaMin']['min'], TH['triAreaMin'], hi=False, fmt='{:.3f}') +
                    cell(a['flipped']['max'], TH['triFlipped'], fmt='{:.0f}') + f'<td class="{"bad" if any(v["max"] > TH["penetrationPct"] for v in a["penetrationPct"].values()) else "ok"}">{p}</td>' +
                    cell(a['hemSelfPairs']['max'], TH['hemSelfPairs'], fmt='{:.0f}') + cell(a['armTorsoPairs']['max'], TH['armTorsoPairs'], fmt='{:.0f}') +
                    cell(a['thighPairs']['max'], TH['thighPairs'], fmt='{:.0f}') + cell(a['propBodyPairs']['max'], TH['propBodyPairs'], fmt='{:.0f}') + f'<td class="fl">{len(a["flags"])}</td></tr>')
    return '<table class="m">' + ''.join(rows) + '</table>'


def main():
    H = []
    H.append(f'''<!doctype html><html><head><meta charset="utf-8"><title>Brief 069 fit test: A (keep the generated mesh) against G (garments on the base)</title>
<style>body{{font:15px/1.45 -apple-system,Helvetica,Arial,sans-serif;color:#17302f;background:#f7f4ec;max-width:1500px;margin:24px auto;padding:0 20px}}
h1{{font-size:26px}}h2{{margin-top:42px;border-bottom:2px solid #cbc6c3;padding-bottom:4px}}h3{{margin-top:26px}}
table{{border-collapse:collapse;margin:10px 0;font-size:13px}}td,th{{border:1px solid #d2e6e2;padding:4px 7px;text-align:left;vertical-align:top}}th{{background:#e9efe9}}
td.bad{{background:#f6d6cf}}td.ok{{background:#e6f2ea}}tr.rest td{{color:#667;font-style:italic}}.v-pass{{background:#d8eedd}}.v-part{{background:#f5ead0}}.v-fail{{background:#f3d3cc}}
.note{{color:#556;font-size:13px}}code{{background:#ece8de;padding:1px 4px;border-radius:3px}}.grid{{display:grid;grid-template-columns:1fr 1fr;gap:14px}}</style></head><body>
<h1>Brief 069 fit test: getting generated people onto one skeleton (A against G)</h1>
<p>Forma, {esc(V["date"])}. $0: no paid calls; the inputs are five already-paid image-to-3D meshes from <code>Citizens3D</code>. Everything here is made by
<code>tools/figures/charpipe/fit/</code> from <code>fit.json</code> (all numbers and rules; no per-figure case). Scratch only; nothing in <code>unity/</code> touched.</p>
<p><b>Sky's challenge:</b> "Option A ... might sound fine on paper but in practice it makes a horrible mess", and how hats, accessories and body sizes work. Both approaches were built on the same five people, played through seven stress clips, measured every sampled frame, and rendered side by side. Failures are in the tables in red and in the videos.</p>''')
    # verdict table
    H.append('<h2>Verdicts</h2><table><tr><th>category</th><th>input</th><th>A: keep the generated mesh</th><th>G: garments bound to the base</th><th>read</th></tr>')
    for row in V['verdicts']:
        H.append(f'<tr><td><b>{esc(row["category"])}</b></td><td>{esc(row["input"])}</td><td class="v-{row["A"][0]}">{esc(row["A"][1])}</td><td class="v-{row["G"][0]}">{esc(row["G"][1])}</td><td>{esc(row["read"])}</td></tr>')
    H.append('</table><p>' + ''.join(f'<p>{esc(p)}</p>' for p in V['summary']) + '</p>')
    # method
    H.append('<h2>Method</h2>')
    for sec in V['method']:
        H.append(f'<h3>{esc(sec["head"])}</h3><ul>' + ''.join(f'<li>{esc(x)}</li>' for x in sec['items']) + '</ul>')
    # fit table
    H.append('<h2>The fit: the base inside each mesh</h2><table><tr><th>input</th><th>what they wear</th><th>sex (row)</th><th>tighter sex (check)</th><th>weight</th><th>muscle</th><th>proportions</th><th>proxy verts &gt;5 mm outside</th><th>median gap mm</th><th>fit seconds</th></tr>')
    for i in IDS:
        f = J(f'fit/{i}.fit.json'); r = {x['id']: x for x in D['inputs']}[i]
        H.append(f'<tr><td>{i}<br><span class="note">{esc(r["label"])}</span></td><td>{", ".join(r["wears"])}</td><td>{f["sex"]}</td><td>{f["tighterSex"]}</td><td>{f["macros"]["weight"]}</td><td>{f["macros"]["muscle"]}</td><td>{f["macros"]["proportions"]}</td><td>{f["quality"]["pctOutside5mm"]} %</td><td>{f["quality"]["gapMedianMm"]}</td><td>{round(sum(f["seconds"].values()), 1)}</td></tr>')
    H.append('</table><p class="note">' + esc(V['fitNote']) + '</p>')
    # per input
    for i in IDS:
        r = {x['id']: x for x in D['inputs']}[i]; m = J(f'anim/{i}_AG.metrics.json'); a = J(f'a/{i}_A.json'); g = J(f'g/{i}/{i}_G.json')
        H.append(f'<h2>{esc(r["category"])}: {i} ({esc(r["label"])})</h2>')
        H.append('<h3>Rest: A, G on the fitted body, then G on the heaviest, slightest, tallest and shortest bodies</h3>' + img(os.path.join(S, f'lineup/{i}_front.png')) + img(os.path.join(S, f'lineup/{i}_back.png')))
        H.append('<h3>A (left) beside G (right), seven clips, the camera orbiting</h3>' + video(os.path.join(S, f'media/{i}_AG.mp4')) + img(os.path.join(S, f'media/{i}_AG_sheet.jpg')))
        H.append('<h3>Play zoom (orthographic, 38 degrees down, 38.6 px/m, last LOD): A left, G right, enlarged 3x</h3>' + img(os.path.join(S, f'media/{i}_play_play_x3.png' if False else f'media/{i}_play_x3.png'), maxw=1400) + '<p class="note">true size, walk:</p>' + png(os.path.join(S, f'media/{i}_play_1x.png')))
        H.append(f'<p><b>A</b>: LODs {a["lodTris"]} tris, {a["bones"]} bones, 1 material (the source texture, {a["texture"]}), weights matched {a["transfer"]["matched"]}, inpainted {a["transfer"]["inpainted"]} ({a["transfer"]["pctInpainted"]} %), unanchored given closest-point weights {a["transfer"]["unanchored"]}, specks dropped {a["crumbsDropped"]} verts of {a["sourceParts"]} parts; loose rules {a["looseRules"]}; build {round(sum(a["seconds"].values()), 1)} s.</p>')
        gp = g['pieces']; gv = g['variants']['fit']
        H.append('<p><b>G</b>: pieces ' + ', '.join(f'{k} {v.get("tris")} tris' for k, v in gp.items()) + f'; {gv["bones"]} deforming bones (+ attachment bones {", ".join(gv["attach"])}), {gv["materials"]} materials, props {gv["trisProps"]} tris; suit wrapped {gp["suit"].get("pctWrapped")} % of its vertices; hair pushed under the hat: {gv.get("hairUnderHatMoved", 0)} vertices.</p>')
        H.append(clip_table(m['summary']['A'], 'A') + clip_table(m['summary']['G'], 'G'))
        if i in V['notes']: H.append('<p>' + esc(V['notes'][i]) + '</p>')
    # extremes
    H.append('<h2>Body extremes (G)</h2><p>' + esc(D['extremes']['note']) + '</p><table><tr><th>body</th><th>macros</th></tr>' +
             ''.join(f'<tr><td>{k}</td><td>{esc(json.dumps(v))}</td></tr>' for k, v in D['extremes']['bodies'].items()) + '</table>')
    for i in IDS:
        e = J(f'anim/{i}_ext.metrics.json'); g = J(f'g/{i}/{i}_G.json')
        H.append(f'<h3>{i}: fit, heaviest, slightest, tallest, shortest</h3>' + video(os.path.join(S, f'media/{i}_ext.mp4')))
        H.append('<table><tr><th>body</th><th>height m</th><th>hat scale</th><th>hair pushed</th><th>stretch p99 (worst clip)</th><th>flipped</th><th>penetration % (worst)</th><th>arm-torso (rest / worst)</th><th>thigh-thigh (rest / worst)</th><th>prop-body (rest / worst)</th></tr>')
        for vn, S_ in e['summary'].items():
            gv = g['variants'][vn]; pe = pen(S_)
            H.append(f'<tr><td>{vn}</td><td>{gv["heightM"]}</td><td>{esc(str(gv.get("hatScale", "")))}</td><td>{gv.get("hairUnderHatMoved", "")}</td><td>{agg(S_, "stretchP99")[0]:.2f} ({agg(S_, "stretchP99")[1]})</td><td>{agg(S_, "flipped")[0]:.0f}</td><td>{", ".join(f"{k} {v[0]:.1f} ({v[1]})" for k, v in pe.items())}</td><td>{S_["rest"]["armTorsoPairs"]} / {agg(S_, "armTorsoPairs")[0]:.0f}</td><td>{S_["rest"]["thighPairs"]} / {agg(S_, "thighPairs")[0]:.0f}</td><td>{S_["rest"]["propBodyPairs"]} / {agg(S_, "propBodyPairs")[0]:.0f}</td></tr>')
        H.append('</table>')
    # layer
    H.append('<h2>The arm-out / leg-out layer on the heaviest bodies</h2><p>' + esc(D['layers']['note']) + '</p><table><tr><th>input</th><th>setting</th><th>arm-torso pairs, summed over the 7 clips: off → on</th><th>thigh-thigh: off → on</th></tr>')
    for i in IDS:
        for tag, f in (('12° arm / 4° leg', f'anim/{i}_layer24.metrics.json'), ('28° arm / 8° leg', f'anim/{i}_layer.metrics.json')):
            if not os.path.exists(os.path.join(S, f)): continue
            L_ = J(f); vals = list(L_['summary'].values())
            at = [sum(s[c]['armTorsoPairs']['max'] for c in s if c != 'rest') for s in vals]; th = [sum(s[c]['thighPairs']['max'] for c in s if c != 'rest') for s in vals]
            H.append(f'<tr><td>{i}</td><td>{tag}</td><td>{at[0]:.0f} → {at[1]:.0f} ({100 * (at[1] - at[0]) / max(at[0], 1):+.0f} %)</td><td>{th[0]:.0f} → {th[1]:.0f} ({100 * (th[1] - th[0]) / max(th[0], 1):+.0f} %)</td></tr>')
    H.append('</table><p>' + esc(V['layerRead']) + '</p>')
    # time
    H.append('<h2>Time per garment and per person</h2><table><tr><th>stage</th><th>seconds</th><th>what</th></tr>' + ''.join(f'<tr><td>{esc(t[0])}</td><td>{esc(t[1])}</td><td>{esc(t[2])}</td></tr>' for t in V['time']) + '</table>')
    for sec in ('failures', 'fixes', 'unity', 'reproduce', 'notdone'):
        H.append(f'<h2>{esc(V[sec]["head"])}</h2><ul>' + ''.join(f'<li>{esc(x)}</li>' for x in V[sec]['items']) + '</ul>')
    H.append('<h2>Thresholds (fit.json)</h2><pre>' + esc(json.dumps(TH, indent=1)) + '</pre></body></html>')
    out = os.path.join(S, 'fit-test.html'); open(out, 'w').write('\n'.join(H)); print(out, os.path.getsize(out) // 1024, 'KB')


main()
