"""Brief 107 (Chime): briefs/107/chime/weather-sfx.json -> briefs/107/chime/weather-sfx.html, the listening page for Sky's pick.
Run from the repo root after build.py: python3 -I briefs/107/chime/make/page.py"""
import html, json
from pathlib import Path
HERE = Path(__file__).resolve().parent; OUT = HERE.parent
d = json.loads((OUT / 'weather-sfx.json').read_text())
E = html.escape
NICE = {'notice_1': 'Watcher notices', 'notice_2': 'Watcher wary', 'notice_3': 'Watcher reports', 'camera_click': 'Camera click', 'cafe_bell': 'Cafe bell', 'cafe_door': 'Cafe door',
        'handover_chime': 'Handover chime', 'car_warn': 'Car warning', 'police_siren': 'Police siren', 'patrol_break': 'Patrol breaks route'}
SPATIAL = {'2d': 'everywhere, not placed', '3d-near': 'placed in the street, heard close (8-45 m)'}


def player(src, loop=False, label=''):
    return f'<figure class="pl">{f"<figcaption>{E(label)}</figcaption>" if label else ""}<audio controls preload="none"{" loop" if loop else ""} src="{E(src)}"></audio></figure>'


def take_name(t):
    if t['id'] == 'night_bed_join': return 'Joined: takes 1-2 + 2-2'
    p, n = t['id'].rsplit('_t', 1); return f'Take {n[0]}-{n[1:]}'


def facts(m, kind):
    f = []
    if kind == 'loop':
        f.append(f"loop {m['seconds']} s")
        if 'swing_4s_lu' in m: f.append(f"level swing {m['swing_4s_lu']} LU")
        ev = m.get('low_events_s', []) + m.get('mid_events_s', []) + m.get('high_events_s', [])
        if kind == 'loop' and m.get('mid_events_s') and len(m['mid_events_s']) > 4: f.append('regular drops (wanted)')
        elif ev: f.append('bumps at ' + ', '.join(f'{v:g} s' for v in sorted(set(ev))[:5]))
        else: f.append('no bumps')
        s = m['seam']; f.append(('<b class="ok">seam clean</b>' if s['clean'] else '<b class="bad">seam jumps</b>') + f" ({s['level_jump_db']} dB at the join, against {s['typical_jump_db_p95']} dB typical)")
    elif kind == 'steps':
        f += [f"{m['steps_found']} steps found", f"4 used, at {', '.join(f'{v:g}' for v in m['steps_used_at_s'])} s", f"{m['over_floor_db']} dB over the noise between steps", f"spread {m['step_lufs_spread_db']} dB before levelling"]
    else: f.append(f"{m['seconds']} s")
    return ' · '.join(f)


def cue_card(c):
    rows = []
    for t in c['takes']:
        pick = t['id'] == c['pick']; m = t['measured']; fl = t['files']
        if c['kind'] == 'loop': pl = player(fl['loop'], True, 'Loop (the browser may leave a hair of a gap when it repeats; the game will not)') + player(fl['seam'], False, 'The seam: 6 s up to the loop point and 6 s after it, as one piece')
        elif c['kind'] == 'steps': pl = player(fl['walk'], False, 'Walking: the four steps in turn, two a second') + '<div class="steps">' + ''.join(player(s, False, f'Step {i + 1}') for i, s in enumerate(fl['steps'])) + '</div>'
        else: pl = player(fl['clip'], False, 'The clip')
        src = t.get('source', []); prm = src[0]['prompt'] if src else ''; cost = sum(s['cost_usd'] for s in src)
        rows.append(f'''<div class="take{' pick' if pick else ''}"><div class="th"><span class="tn">{E(take_name(t))}</span>{'<span class="badge">My pick</span>' if pick else ''}<span class="cost">${cost:.3f}</span></div>
{pl}<p class="facts">{facts(m, c['kind'])}</p>{f'<details><summary>Prompt</summary><p>{E(prm)}</p></details>' if prm else ''}</div>''')
    lvl = f"{c['level']} {c['level_unit']}"
    return f'''<section class="cue" id="{c['id']}"><h2>{E(c['label'])}</h2>
<p class="plays">{E(c['plays'])}</p><p class="meta">Level {E(lvl)} · {E(SPATIAL.get(c['spatial'], c['spatial']))}</p>
<p class="why"><b>Why my pick:</b> {E(c['why'])}</p><div class="takes">{''.join(rows)}</div></section>'''


def clear_table():
    floors = list(d['clear_of']); head = ''.join(f'<th>{E(f)}</th>' for f in floors)
    body = ''
    for k in NICE:
        cells = ''.join((lambda r: f'<td class="{"warn" if r["margin_top3_db"] < 10 else ""}">{r["margin_top3_db"]:g} dB <small>· {r["bands_clear_6db"]} bands · {r["loudness_margin_lu"]:+g} LU</small></td>')(d['clear_of'][f][k]) for f in floors)
        body += f'<tr><th>{E(NICE[k])}</th>{cells}</tr>'
    return f'<table class="clear"><thead><tr><th>Cue</th>{head}</tr></thead><tbody>{body}</tbody></table>'


s = d['sums']; mx = d['mixes']; LV = {c['id']: f"{c['level']:g} LUFS" + ('' if c['meter'] == 'i' else ' at the loudest') for c in d['cues']}; lv = LV.get

s = d['sums']; mx = d['mixes']; cues = ''.join(cue_card(c) for c in d['cues'])
picks = ''.join(f"<li><a href=\"#{c['id']}\">{E(c['label'])}</a>: {E(take_name({'id': c['pick']}))}</li>" for c in d['cues'])
page = f'''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Weather Sounds</title><style>
:root{{--bg:#f6f1e7;--card:#fffdf8;--ink:#2a2622;--soft:#6c645b;--line:#e3d9c9;--accent:#1e8c86;--pick:#e9f6f2;--warn:#b5532d;--ok:#1d7a4f}}
@media (prefers-color-scheme:dark){{:root:not([data-theme="light"]){{--bg:#1b1a18;--card:#24221f;--ink:#efe8dc;--soft:#a79e92;--line:#3a3631;--accent:#5cc6be;--pick:#1f312d;--warn:#e38a63;--ok:#6fcf9b}}}}
:root[data-theme="dark"]{{--bg:#1b1a18;--card:#24221f;--ink:#efe8dc;--soft:#a79e92;--line:#3a3631;--accent:#5cc6be;--pick:#1f312d;--warn:#e38a63;--ok:#6fcf9b}}
*{{box-sizing:border-box}}body{{margin:0;background:var(--bg);color:var(--ink);font:16px/1.5 -apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif}}
main{{max-width:1080px;margin:0 auto;padding:28px 16px 64px}}h1{{font-size:28px;margin:0 0 4px}}h2{{font-size:21px;margin:0 0 6px}}
.lede{{color:var(--soft);max-width:760px}}.box{{background:var(--card);border:1px solid var(--line);border-radius:12px;padding:18px;margin:18px 0}}
.mixes{{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:14px}}.pl{{margin:8px 0}}.pl figcaption{{font-size:13px;color:var(--soft)}}audio{{width:100%;height:36px}}
.cue{{background:var(--card);border:1px solid var(--line);border-radius:12px;padding:18px;margin:22px 0}}.plays{{margin:0}}.meta{{color:var(--soft);font-size:14px;margin:2px 0 8px}}
.takes{{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:12px}}.take{{border:1px solid var(--line);border-radius:10px;padding:12px}}.take.pick{{background:var(--pick);border-color:var(--accent)}}
.th{{display:flex;gap:8px;align-items:center}}.tn{{font-weight:600}}.badge{{background:var(--accent);color:var(--bg);border-radius:99px;padding:1px 9px;font-size:12px;font-weight:700}}.cost{{margin-left:auto;color:var(--soft);font-size:13px}}
.facts{{font-size:13px;color:var(--soft);margin:6px 0}}.ok{{color:var(--ok)}}.bad{{color:var(--warn)}}details{{font-size:13px;color:var(--soft)}}.steps{{display:grid;grid-template-columns:1fr 1fr;gap:0 10px}}
table{{border-collapse:collapse;width:100%;font-size:14px}}th,td{{text-align:left;padding:6px 8px;border-bottom:1px solid var(--line);vertical-align:top}}td.warn{{color:var(--warn);font-weight:600}}small{{color:var(--soft);font-weight:400}}
.scroll{{overflow-x:auto}}.ruled{{border-color:var(--accent);background:var(--pick)}}summary{{cursor:pointer}}ul{{margin:6px 0;padding-left:20px}}code{{font-size:13px}}
</style></head><body><main>
<h1>Weather and night sounds</h1>
<p class="lede">Brief 107, item 3: a summer shower, rain on awnings and parasols, drips from the eaves, wet footsteps, and a calm night street. Made the way the 087 pack was: Seed Audio 1.0 through Runware, 2-3 takes a cue, cut and levelled against the pack already in the game. I can't hear, so every pick below rests on measurements. Your ears decide.</p>
<div class="box ruled"><b>Sky picked these (2026-11-11): "go with your recommendations, though rain needs to be light background noise".</b> So the rain is now much quieter: the shower at {lv('rain_bed')} (was -40), the awnings at {lv('rain_awning')} (was -36), the drips at {lv('drips')} (was -36). With the street bed unchanged, rain near a cafe reads {s['rain_floor_lufs_i']:.1f} LUFS against the dry day's {s['street_bed_alone_lufs_i']:.1f}: it adds less than a LU, so you hear a city in light rain, not a rainstorm. Night ({lv('night_bed')}) and the wet steps ({lv('wet_steps')}) are unchanged. The clips are in the game's files; Latch wires playback (<a href="wiring.md">wiring.md</a>).</div>
<div class="box"><b>How to listen.</b> Every file on this page is raised by the same {d['listening_lift_db']:g} dB, so they compare as they would in the game: turn your volume up rather than down. In the three mixes a limiter takes up to {max(d['listening_limited_db'].values()):g} dB off the tings and the bell (page copies only). Loops repeat with the player's loop on; the second player under each loop is the seam itself, joined in the file, so a click or a jump there would be real.
<ul>{picks}</ul>Spent ${d['spent_usd']:.2f} of the $2 allowed (17 takes).</div>
<div class="box"><h2>In the mix</h2><p class="meta">20 s each, at game levels. The watcher's ting plays at 12 s and the cafe bell at 16 s, so you can hear whether they still cut through.</p><div class="mixes">
{player(mx['mix_day'], False, f"Day today: the street bed alone ({s['street_bed_alone_lufs_i']:g} LUFS over its whole loop)")}
{player(mx['mix_rain'], False, f"Rain, light: street bed + shower + awning and drips as from a cafe 15 m off; wet steps pass from 3 s ({s['rain_floor_lufs_i']:.1f} LUFS under the steps)")}
{player(mx['mix_night'], False, f"Night: the night bed alone ({s['night_bed_lufs_i']:g} LUFS), the far tram from 1 s")}
</div></div>
{cues}
<section class="cue"><h2>Do the cues still cut through?</h2><p class="meta">For each cue: how far its loudest tenth of a second stands over the floor in its three strongest bands, how many third-octave bands clear by 6 dB, and its loudness over the floor. Red is under 10 dB. Rain floor = street bed + shower + awning and drips 6 dB down, the busiest it gets near a cafe.</p>
<div class="scroll">{clear_table()}</div>
<p class="facts">The police siren sits lowest in the rain ({d['clear_of']['rain floor']['police_siren']['margin_top3_db']:g} dB in its bands, against {d['clear_of']['street_bed (087, today)']['police_siren']['margin_top3_db']:g} dB today) at the lighter level (it was 8.4 dB with the first, louder rain): the shower shares its 800 Hz-2.5 kHz range. Every cue now clears the rain by 16 dB or more in its own bands.</p></section>
<details class="cue"><summary><b>Files</b></summary><ul>
<li>Clips at game level: <code>{E(d['shipped']['unity'])}/</code>; their rows (when, where, level): <code>{E(d['shipped']['contract'])}</code>; how to play them: <a href="wiring.md">wiring.md</a>.</li>
<li>Rebuild: <code>out/scratch/chime/venv/bin/python -I briefs/107/chime/make/build.py</code> then <code>python3 -I briefs/107/chime/make/page.py</code>. Numbers: <code>weather-sfx.json</code>.</li></ul></details>
</main></body></html>
'''
(OUT / 'weather-sfx.html').write_text(page)
print(OUT / 'weather-sfx.html', len(page))
