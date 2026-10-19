"""Contact sheets for the stand-in people: close-ups (front, 38 degrees) and the play-zoom tile at true size and enlarged.
python3 -I tools/figures/sheet.py <shotsA dir> <shotsB dir> <manifest.json> <out dir>
Shots come from the scratch Unity project check (FigCheck), not from the game: Standard shader, one sun, no grade."""
import sys, json, os
from PIL import Image, ImageDraw
A, B, man, out = sys.argv[1:5]
m = json.load(open(man)); figs = {f['id']: f for f in m['figures']}
groups = {
    'cell': ['scout', 'fixer', 'tech', 'enforcer'],
    'cast': ['waitress', 'guard', 'driver', 'councillor', 'security', 'rival', 'cafe_w2', 'cafe_w3', 'cafe_w5'],
    'crowd': ['c6', 'd6', 'c1', 'd5c', 'd2m', 'workers_d5', 'd4', 'el1', 'sh2'],
}
LW, TW, TH = 230, 200, 266; PW, PH = 72, 96; Z = 3
os.makedirs(out, exist_ok=True)
def play(fid, pose):
    im = Image.open(f'{B}/{fid}_{pose}_play.png').convert('RGB')
    return im.crop((960 - PW // 2, 545 - PH // 2, 960 + PW // 2, 545 + PH // 2))
for g, ids in groups.items():
    W = LW + TW * 3 + PW * Z * 2 + PW * 2 + 40; H = (TH + 6) * len(ids) + 28
    img = Image.new('RGB', (W, H), (214, 214, 204)); d = ImageDraw.Draw(img)
    heads = ['', 'front, idle (LOD0)', '38 deg yaw 30, idle (LOD0)', '38 deg yaw 30, walk (LOD0)', 'play zoom x3, idle', 'play zoom x3, walk', 'true size']
    xs = [0, LW, LW + TW, LW + TW * 2, LW + TW * 3, LW + TW * 3 + PW * Z, LW + TW * 3 + PW * Z * 2]
    for x, t in zip(xs, heads): d.text((x + 4, 6), t, fill=(30, 30, 30))
    for r, fid in enumerate(ids):
        y = 28 + r * (TH + 6); f = figs[fid]
        d.text((6, y + 4), f"{fid}  [{f['role']}]", fill=(0, 0, 0))
        words = f['label']; line = ''; yy = y + 22
        for w in words.split():
            if len(line + ' ' + w) > 34: d.text((6, yy), line, fill=(40, 40, 40)); yy += 12; line = w
            else: line = (line + ' ' + w).strip()
        d.text((6, yy), line, fill=(40, 40, 40)); yy += 16
        d.text((6, yy), 'LODs ' + ' / '.join(str(l['tris']) for l in f['lods']), fill=(40, 40, 40)); yy += 12
        if f.get('variantOf'): d.text((6, yy), f"recolour of {f['variantOf']}", fill=(90, 40, 40)); yy += 12
        for c, (pose, view) in enumerate([('idle_0.10', 'front'), ('idle_0.10', 'close'), ('walk_0.30', 'close')]):
            t = Image.open(f'{A}/{fid}_{pose}_{view}.png').convert('RGB').resize((TW, TH)); img.paste(t, (LW + c * TW, y))
        for c, pose in enumerate(['idle_0.10', 'walk_0.30']):
            p = play(fid, pose); img.paste(p.resize((PW * Z, PH * Z), Image.NEAREST), (xs[4] + c * PW * Z, y))
            img.paste(p, (xs[6] + c * PW, y))
    img.save(f'{out}/{g}.png'); print(g, img.size)
