"""Forma, brief 067 W1: the pictures. python3 -I briefs/067/forma/boards.py [street|rows|crew|clerks|all]   (renders through tools/figures/proof_run.py: Blender, a few seconds a row; no model call)
Every picture is the play camera (orthographic, 38 degrees, 28 m over 1080 px: a 1.75 m figure is 53 px), the game's sun and ambient (light.json), the game's grade, over a patch of a real 0.2.122
frame. Relative reads, not Unity's pixels: the lighting is fitted to the old rivals in the real 3:20 frame (tools/figures/proof_run.py: AMBIENT, GAIN, VEIL)."""
import os, sys, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFont

R = '/Users/sky/Projects/suborned/'; B = R + 'briefs/067/forma/'
def load(name, path):
    sp = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(sp); sp.loader.exec_module(m); return m
pr = load('proof_run', R + 'tools/figures/proof_run.py'); pp = pr.pp
INSTALLED = R + 'unity/Assets/Suborned/People'      # what 0.2.122 shows (the old rival)
BUILD = R + 'out/people-models/People'               # the new build (not installed: Latch installs)
FONT = R + 'unity/Assets/Suborned/UI/Resources/Kit/fonts/'
INKDEEP = (11, 22, 21); INK = (23, 48, 47); CREAM = (247, 244, 236); DIM = (176, 205, 199); TURQL = (96, 224, 234); CHROME = (203, 198, 195)
def f(px, bold=True): return ImageFont.truetype(FONT + ('Jost-Bold.ttf' if bold else 'Barlow-Regular.ttf'), px)
def up(im, k): return im.resize((im.width * k, im.height * k), Image.NEAREST)
def paste_label(board, im, x, y, text, k=3, sub=None):
    d = ImageDraw.Draw(board); d.text((x, y), text, font=f(22), fill=CREAM)
    if sub: d.text((x, y + 28), sub, font=f(15, False), fill=DIM)
    board.paste(up(im, k) if k > 1 else im, (x, y + (58 if sub else 34)))
    return y + (58 if sub else 34) + im.height * k
def wrap(d, text, font, width):
    lines, line = [], ''
    for w_ in text.split():
        t = (line + ' ' + w_).strip()
        if d.textlength(t, font=font) > width and line: lines.append(line); line = w_
        else: line = t
    return lines + [line]
def sheet(w, h, title, sub):
    b = Image.new('RGB', (w, h), INK); d = ImageDraw.Draw(b); d.text((48, 30), title, font=f(40), fill=CREAM)
    y = 84
    for l in wrap(d, sub, f(18, False), w - 96): d.text((48, y), l, font=f(18, False), fill=DIM); y += 25
    d.rectangle((48, y + 6, 188, y + 8), fill=TURQL); return b

def feet_to_offset(fx, fy, cx, cy, ppm=pr.pp_ppm(), pitch=38.0, zlook=0.62):
    """the metres right and up the screen that put a figure's feet at pixel (fx, fy) when pixel (cx, cy) shows the world point at height zlook"""
    import math
    dx = (fx - cx) / ppm; dy = ((cy + zlook * math.cos(math.radians(pitch)) * ppm) - fy) / (math.sin(math.radians(pitch)) * ppm); return dx, dy

def street():
    """the real 3:20 frame: the old three where Sky saw them, and the new three beside them, the same light, the same size"""
    frame = Image.open(R + 'briefs/041/build/0311/proof-1080/ring-plain-rival.png').convert('RGB')
    x0, y0, x1, y1 = 880, 430, 1240, 640; crop = frame.crop((x0, y0, x1, y1)); w, h = crop.size
    cx, cy = w // 2, h // 2
    # the old trio's feet in the frame, then the new trio 150 px to their right on the same road
    old_feet = [(957, 542), (987, 527), (974, 557)]; shift = 125
    pos = [feet_to_offset(fx + shift - x0, fy - y0, cx, cy) for fx, fy in old_feet]
    figs = [('rival', 190.0, None, False, pos[0]), ('rival_b', 215.0, None, False, pos[1]), ('rival_d', 200.0, None, False, pos[2])]
    img = pr.run(figs, BUILD, 30.0, 'sun', w=w, h=h, bg=np.asarray(crop), samples=96)
    out = np.asarray(crop).copy(); new = np.asarray(Image.fromarray(img))
    # take only the pixels that changed (the figures and their shadows): the rest of the crop stays the game's own
    diff = np.abs(new.astype(int) - np.asarray(crop).astype(int)).sum(-1) > 14
    out[diff] = new[diff]
    after = Image.fromarray(out)
    board = sheet(1500, 1000, 'The rivals in the street: before and after', 'The real 0.2.122 frame at 3:20 (briefs/041/build/0311/proof-1080/ring-plain-rival.png), play camera, 1080p, the rings left off so the figures must read alone. Left, the three as Sky saw them; right, the new crew, rendered in the same light and laid in the same frame. Enlarged 4x, nearest pixel; a figure is 53 px in the game.')
    d = ImageDraw.Draw(board)
    board.paste(up(after, 4).crop((0, 0, 1404, 800)), (48, 172))
    d.text((70, 184), 'before: Worker D5, dark violet, hard hat, amber piping', font=f(20), fill=CREAM); d.text((730, 184), 'after: the Velvet Set, the three posted (rival-1, 2, 3)', font=f(20), fill=CREAM)
    board.save(B + 'rival-street-before-after.png'); after.save(B + 'rival-street-after-1x.png'); crop.save(B + 'rival-street-before-1x.png')
    print('wrote rival-street-before-after.png', after.size)

def rows():
    """true size rows, before (the installed figures) and after (the build), in sun and in shade, the play camera at yaw 30 and at yaw 210 (the Office front is played at 210)"""
    names = {'guard': 'guard', 'security': 'police', 'driver': 'driver', 'rival': 'rival', 'rival_b': 'rival b', 'rival_c': 'rival c', 'd4': 'clerk d4', 'd5c': 'clerk d5c', 'd4b': 'd4b'}
    def mk(yaw, light, rival_ids, people, extra=()):
        hd = (yaw + 180.0)
        ids = ['guard', 'security', 'driver'] + list(rival_ids) + list(extra)
        figs = [(i, hd + (-14 if k % 2 else 12) - (6 if i == 'rival' else 0), None, False, None) for k, i in enumerate(ids)]
        return pr.run(figs, people, yaw, light, spacing=1.15, h=140, samples=64), ids
    conds = [(30.0, 'sun'), (30.0, 'shade'), (210.0, 'sun'), (210.0, 'shade')]
    W = 1712; board = sheet(W, 1620, 'The rivals at true size: before and after', 'Left, the installed 0.2.122 figures; right, the new build. Beside them the guard, the police and the driver, whom the rivals must not look like. Ring off. Real road of the 0.2.122 frame, the game\'s sun and grade (tools/figures/proof_run.py). Enlarged 2x, nearest pixel; a figure is 53 px.')
    y = 190; d = ImageDraw.Draw(board)
    for yaw, lt in conds:
        d.text((48, y), f'{lt}, yaw {yaw:g}', font=f(20), fill=TURQL); y += 30
        for k, (people, rv, tag) in enumerate(((INSTALLED, ['rival', 'rival', 'rival', 'rival'], 'before'), (BUILD, ['rival', 'rival_b', 'rival_c', 'rival_d'], 'after'))):
            img, ids = mk(yaw, lt, rv, people)
            im = up(Image.fromarray(img), 2); x = 48 + k * 816
            board.paste(im, (x, y)); d.text((x + 8, y + 6), tag, font=f(18), fill=INKDEEP)
            for j, nm in enumerate(['guard', 'police', 'driver'] + (['rival', 'rival', 'rival', 'rival'] if k == 0 else ['rival', 'rival b', 'rival c', 'rival d'])):
                cxm = x + 2 * (im.width // 4 + (j - (len(ids) - 1) / 2) * 1.15 * pr.pp_ppm()); d.text((cxm, y + im.height - 34), nm, font=f(15), fill=INKDEEP, anchor='ma')
        y += 320
    board.crop((0, 0, W, y + 10)).save(B + 'rival-rows-before-after.png'); print('wrote rival-rows-before-after.png')

def cell():
    """the real 0:40 frame: the cell as the game draws it, on its road, and the new crew laid in the same frame and light beside them: the cell's ivory and black against violet and pale trousers"""
    frame = Image.open(R + 'briefs/041/build/0311/proof-1080/overhead-pickup.png').convert('RGB')
    x0, y0, x1, y1 = 840, 440, 1240, 640; crop = frame.crop((x0, y0, x1, y1)); w, h = crop.size; cx, cy = w // 2, h // 2
    feet = [(1103, 560), (1135, 548), (1160, 566)]
    pos = [feet_to_offset(fx - x0, fy - y0, cx, cy) for fx, fy in feet]
    figs = [('rival', 190.0, None, False, pos[0]), ('rival_b', 205.0, None, False, pos[1]), ('rival_d', 215.0, None, False, pos[2])]
    img = pr.run(figs, BUILD, 30.0, 'sun', w=w, h=h, bg=np.asarray(crop), samples=96)
    out = np.asarray(crop).copy(); new = np.asarray(Image.fromarray(img)); diff = np.abs(new.astype(int) - np.asarray(crop).astype(int)).sum(-1) > 14; out[diff] = new[diff]
    after = Image.fromarray(out)
    board = sheet(1296, 820, 'The crew beside the cell', 'The real 0.2.122 frame at 0:40 (briefs/041/build/0311/proof-1080/overhead-pickup.png): the cell as the game draws it (Tech, Fixer, Scout, Enforcer) and the new crew laid in beside them, the same light, the rings off. 3x, nearest pixel. The cell is ivory and black with a cyan edge; the crew is violet over pale trousers.')
    board.paste(up(after, 3).crop((0, 0, 1200, 600)), (48, 190)); board.save(B + 'rival-beside-the-cell.png'); print('wrote rival-beside-the-cell.png', after.size)


def clerks():
    """the d4 question: the door guard and the clerks, true size, in the sun and in shade, at yaw 210 (the Office front) and 30; and the colour blocks in numbers"""
    ids = ['guard', 'd4', 'guard', 'd4', 'd5c', 'd4b', 'security', 'd5c']
    heads = [30, 30, 75, 75, 30, 30, 30, 75]
    board = sheet(1500, 1260, 'The door guard and the clerks, true size', 'Guard (white duty suit, blue panels, cap), clerk d4 (grey bomber over navy), d5c (mint dress, pale hat), d4b (peach bomber over charcoal), police (olive). The play camera at yaw 210, the Office front, in the sun and in the shade; the rings off. 3x, nearest pixel. Each row stands the same figures at three headings.')
    y = 200; d = ImageDraw.Draw(board)
    for lt in ('sun', 'shade'):
        figs = [(i, 210.0 + 180.0 - 210.0 + hd - 30.0 if False else float(hd), None, False, None) for i, hd in zip(ids, heads)]
        img = pr.run(figs, BUILD, 210.0, lt, spacing=1.2, h=140, samples=64)
        im = up(Image.fromarray(img), 3); d.text((48, y), f'{lt}, yaw 210', font=f(20), fill=TURQL)
        board.paste(im, (48, y + 30)); y += 30 + im.height + 20
    board.crop((0, 0, 1500, y)).save(B + 'clerks-vs-guard.png'); print('wrote clerks-vs-guard.png')


def crew():
    """concept to figure: the Codex lineup (paid/067-rival-look/codex/rival_lineup_1.png, $0) over the three built colourways at full detail (LOD0, 8000 triangles, 2048 albedo)"""
    import subprocess
    T = R + 'out/scratch/forma/067/turn'
    for n in ('rival', 'rival_b', 'rival_c', 'rival_d'):
        subprocess.run(['/Applications/Blender.app/Contents/MacOS/Blender', '-b', '-P', R + 'tools/figures/turn.py', '--', BUILD + '/Figures', n, '0', T], capture_output=True)
    concept = Image.open(R + 'paid/067-rival-look/codex/rival_lineup_1.png').convert('RGB')
    W = 1560; board = sheet(W, 1800, 'The crew: the concept and the figures', 'Above, the open-brief concept (Codex, $0, no date in the prompt): a street crew in velvet-violet bomber jackets, pale trousers, chrome belts, bare heads. Below, the four looks as built: three colourways on Commuter D4\'s body and the crew\'s woman on Commuter C6\'s, front, back and the head, at full detail (8000 triangles, 2048 albedo), studio light, not the game\'s. Violet jackets, pale trousers or dress, pearl boots; no cap, no gold, no amber. The concept\'s five are five bodies: the stand-in has two.')
    c = concept.resize((W - 96, int((W - 96) * concept.height / concept.width)), Image.LANCZOS); y = 220; board.paste(c, (48, y)); y += c.height + 24
    d = ImageDraw.Draw(board); cols = [('rival', 'rival: mid violet, ivory'), ('rival_b', 'rival_b: deep violet, pale grey'), ('rival_c', 'rival_c: lavender, pearl'), ('rival_d', 'rival_d: the woman, hood and dress')]
    for k, (n, t) in enumerate(cols):
        x = 48 + k * 366; d.text((x, y), t, font=f(16), fill=CREAM)
        fr = Image.open(f'{T}/{n}_front.png').convert('RGB').crop((70, 90, 350, 650)).resize((100, 200)); bk = Image.open(f'{T}/{n}_back.png').convert('RGB').crop((70, 90, 350, 650)).resize((100, 200)); hd = Image.open(f'{T}/{n}_head.png').convert('RGB').crop((60, 150, 360, 640)).resize((122, 200))
        board.paste(fr, (x, y + 28)); board.paste(bk, (x + 104, y + 28)); board.paste(hd, (x + 208, y + 28))
    board.crop((0, 0, W, y + 250)).save(B + 'rival-concept-and-figures.png'); print('wrote rival-concept-and-figures.png')


if __name__ == '__main__':
    what = sys.argv[1] if len(sys.argv) > 1 else 'all'
    if what in ('street', 'all'): street()
    if what in ('rows', 'all'): rows()
    if what in ('cell', 'all'): cell()
    if what in ('clerks', 'all'): clerks()
    if what in ('crew', 'all'): crew()
