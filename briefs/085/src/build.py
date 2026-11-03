#!/usr/bin/env python3
# Glyph, brief 085: builds briefs/085/hud.html, self-contained: kit sprites and real frames inlined.
# Run from anywhere: python3 -I briefs/085/src/build.py   (frames come from out/scratch/glyph/085/frames, patch2.py there)
import base64, os
R = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..')) + '/'
K = R + 'unity/Assets/Suborned/UI/Resources/Kit/'
F = R + 'out/scratch/glyph/085/frames/'
def d(p, mime):
    return f'data:{mime};base64,' + base64.b64encode(open(p, 'rb').read()).decode()
icons = '''watch listen ask pay help pressure call loop reroute turn_person check lock nextevent s_moving run eye_unk eye_open eye_shut
debt cash mood_uneasy mood_friendly fearful loyal mercenary lovers secret_none routine p_cash p_trail p_time car take sawyou chev_up chev_down radio unknown'''.split()
css = []
for n in icons:
    css.append(f'.i-{n}{{--m:url({d(K + f"icons/singles/{n}@2x.png", "image/png")})}}')
sprites = {
    'panel': 'panels/panel_dark@2x.png', 'pill': 'panels/pill_status@2x.png', 'plate': 'markers/plate_dark@2x.png',
    'disc': 'markers/disc_dark_24@2x.png', 'discl': 'markers/disc_light_24@2x.png', 'stripes': 'overlays/stripes_warn@2x.png',
    'mk_ok': 'badges/mark_ok@2x.png', 'mk_half': 'badges/mark_half@2x.png', 'mk_no': 'badges/mark_no@2x.png', 'mk_unk': 'badges/mark_unk@2x.png',
    'c_scout': 'portraits/cameo_scout_normal@2x.png', 'c_fixer': 'portraits/cameo_fixer_normal@2x.png', 'c_tech': 'portraits/cameo_tech_normal@2x.png',
    'c_enforcer': 'portraits/cameo_enforcer_normal@2x.png', 'c_waitress': 'portraits/cameo_waitress_normal@2x.png', 'c_driver': 'portraits/cameo_driver_normal@2x.png',
    'c_vale': 'portraits/cameo_councillor_normal@2x.png', 'c_guard': 'badges/signal_guard@2x.png',
}
for n, p in sprites.items():
    css.append(f':root{{--{n}:url({d(K + p, "image/png")})}}')
frames = {}
for f in sorted(os.listdir(F)):
    if f.endswith('.jpg'):
        frames[f[:-4]] = d(F + f, 'image/jpeg')
import json
fonts = f'''@font-face{{font-family:Jost;font-weight:700;src:url({d(K + "fonts/Jost-Bold.ttf", "font/ttf")})}}
@font-face{{font-family:Barlow;font-weight:500;src:url({d(K + "fonts/Barlow-Medium.ttf", "font/ttf")})}}'''
here = os.path.dirname(os.path.abspath(__file__))
tpl = open(here + '/tpl.html', encoding='utf-8').read()
js = open(here + '/app.js', encoding='utf-8').read()
out = tpl.replace('/*FONTS*/', fonts).replace('/*KIT*/', '\n'.join(css)).replace('/*FRAMES*/', 'const FRAMES=' + json.dumps(frames) + ';').replace('/*APP*/', js)
open(R + 'briefs/085/hud.html', 'w', encoding='utf-8').write(out)
print('hud.html', round(len(out) / 1e6, 2), 'MB')
