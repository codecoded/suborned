"""Brief 101, round 3 (Glyph): the inventory of HUD pieces still in the old kit, from the real 0.2.286 captures (Latch's r4 frames, the batch's
shots and proof sets). Every manifest element that isn't one of the built street pieces is cropped at 2x from its frame and laid on a sheet by
family, with its frame and size. Writes out/scratch/glyph/101r3/inventory/*.png and inventory.json. Glyph. Mock only."""
import json, glob, os, re, collections
from PIL import Image, ImageDraw, ImageFont

R = '/Users/sky/Projects/suborned/'
OUT = R + 'out/scratch/glyph/101r3/inventory/'
DIRS = ['out/scratch/latch/r4/shots-final', 'out/scratch/latch/r4/proof-r101-final', 'out/batch/shots', 'out/batch/proofset/r059',
        'out/batch/proofset/r067', 'out/batch/proofset/r083', 'out/batch/proofset/r123', 'out/build/play/proof']
SINCE = os.path.getmtime(R + 'out/batch/shots/shots.json') - 6 * 3600  # the 0.2.286 batch (its shots at 00:41 on 9 Nov) less six hours: older proof frames are skipped
PARTS = {'window-mark': 'window marker', 'attention-clock': 'attention clock', 'pop': 'pop', 'gain-chip': 'done chip', 'plan-ghost': 'plan ghost',
         'stash': 'stash plate', 'timer': 'timer', 'goal': 'goal line', 'history': 'history', 'beat': 'beat', 'strip': 'strip', 'sheet': 'sheet',
         'response': 'response', 'intel': 'intel', 'verb-pill': 'hover label'}
BUILT = {'turn button', 'alert lamps', 'trail icon', 'cash icon', 'next icon', 'known people', 'pause button', 'roster portraits', 'book key',
         'plot scope', 'cones button', 'squad button', 'next unknown', 'card badge', 'seal lock', 'seal job', 'seal cause', 'squad button word'}
# family by (kind, text): what the piece is, in the GDD's words
FAMILY = [
    (lambda k, t: k == 'marker' and t in ('noticed', 'reported'), 'attention clock'),
    (lambda k, t: k == 'marker' and t in ('done', 'stop first', 'taking aim'), 'state word'),
    (lambda k, t: k == 'marker' and re.match(r'^\d+ s$', t or ''), 'report seconds'),
    (lambda k, t: k == 'pop', 'pop'),
    (lambda k, t: k == 'image' and t == 'attention callout', 'attention callout'),
    (lambda k, t: k == 'image' and t in ('door marker', 'door turn', 'door next step', 'car marker', 'stand disc', 'hand-off take', "stand after Popovic's car", 'body', 'rival stop'), 'step markers'),
    (lambda k, t: k == 'image' and (t or '').startswith('pip '), 'person pips'),
    (lambda k, t: k == 'image' and 'edge chip' in (t or ''), 'edge chips'),
    (lambda k, t: k == 'image' and t in ('hint reopen', 'hint portrait'), 'hints'),
    (lambda k, t: k == 'line', 'hint line'),
    (lambda k, t: k == 'chip', 'done chip'),
    (lambda k, t: k == 'card' and not re.search(r' · \d+:\d+$', t or ''), 'person card'),
    (lambda k, t: k == 'image' and t in ('stack tab', 'skip', "speaker's face") or (t or '').startswith('stash plate'), 'other'),
    (lambda k, t: k == 'altline', 'alt line'),
]


def family(k, t):
    for f, name in FAMILY:
        if f(k, t): return name
    return None


def font(n, bold=True):
    return ImageFont.truetype(R + 'unity/Assets/Suborned/UI/Resources/Kit/fonts/' + ('Jost-Bold.ttf' if bold else 'Jost-Medium.ttf'), n)


def main():
    os.makedirs(OUT, exist_ok=True); fams = collections.defaultdict(list); seen = set()
    for d in DIRS:
        for f in sorted(glob.glob(R + d + '/*.manifest.json')):
            shot = f[:-len('.manifest.json')]; name = os.path.basename(shot)
            if '-2x' in name or not os.path.exists(shot + '.png') or os.path.getmtime(f) < SINCE: continue
            pf = shot + '.parts.json'
            if os.path.exists(pf):
                for pk, v in json.load(open(pf)).items():
                    base = re.sub(r'-?\d+$', '', pk)
                    if base not in PARTS or not (isinstance(v, list) and len(v) == 4): continue
                    x, y, w, h = v
                    if not (0 <= x < 1920 and 0 <= y < 1080 and w > 0 and h > 0): continue
                    key = ('part:' + PARTS[base], pk, name)
                    if key in seen: continue
                    seen.add(key); fams['part:' + PARTS[base]].append(dict(shot=d + '/' + name, kind='part', text=pk, x=x, y=y, w=w, h=h))
            for e in json.load(open(f)).get('elements', []):
                k, t = e.get('kind'), e.get('text', '')
                if k in ('bar', 'speech') or t in BUILT: continue
                fam = family(k, t)
                if not fam: continue
                key = (fam, re.sub(r'\d', '#', t or ''), name)
                if key in seen: continue
                seen.add(key); fams[fam].append(dict(shot=d + '/' + name, kind=k, text=t, x=e['x'], y=e['y'], w=e['w'], h=e['h']))
    json.dump(fams, open(OUT + 'inventory.json', 'w'), indent=1)
    for fam, items in fams.items():
        # one row per distinct text, at most 8 examples a family
        uniq = []; texts = set()
        for it in items:
            tk = re.sub(r'\d', '#', it['text'] or '') + it['kind'] + (it['shot'] if it['kind'] == 'part' else '')
            if tk in texts and len(uniq) >= 3: continue
            texts.add(tk); uniq.append(it)
        uniq = uniq[:8]; tiles = []
        for it in uniq:
            im = Image.open(R + it['shot'] + '.png').convert('RGB'); m = 24
            box = (max(0, it['x'] - m), max(0, it['y'] - m), min(1920, it['x'] + it['w'] + m), min(1080, it['y'] + it['h'] + m))
            c = im.crop(box); c = c.resize((c.width * 2, c.height * 2), Image.LANCZOS); tiles.append((it, c))
        W = max(900, max(c.width for _, c in tiles) + 40); H = sum(c.height + 70 for _, c in tiles) + 60
        sh = Image.new('RGB', (W, H), (247, 244, 236)); d = ImageDraw.Draw(sh); d.text((20, 14), fam, font=font(28), fill=(23, 48, 47)); y = 60
        for it, c in tiles:
            d.text((20, y), f"{it['kind']} · {it['text']!r} · {it['w']}x{it['h']} px · {os.path.basename(it['shot'])}", font=font(18, False), fill=(23, 48, 47))
            sh.paste(c, (20, y + 26)); y += c.height + 70
        sh.save(OUT + re.sub(r'\W+', '-', fam) + '.png')
    for fam, items in sorted(fams.items()): print(f'{fam:18} {len(items):3}  ' + ', '.join(sorted({repr(i["text"])[:30] for i in items}))[:200])


if __name__ == '__main__':
    main()
