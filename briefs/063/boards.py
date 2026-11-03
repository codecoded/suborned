# Forma, brief 063: the pick boards. python3 -I briefs/063/boards.py   (reads briefs/063/{concepts,cameos,models,patrol}; no model call)
import os
from PIL import Image, ImageDraw, ImageFont
R = '/Users/sky/Projects/suborned/'; B = R + 'briefs/063/'
FONT = R + 'unity/Assets/Suborned/UI/Resources/Kit/fonts/'
INKDEEP = (11, 22, 21); INK = (23, 48, 47); CREAM = (247, 244, 236); DIM = (176, 205, 199); TURQL = (96, 224, 234); CHROME = (203, 198, 195)
def f(px, bold=True): return ImageFont.truetype(FONT + ('Jost-Bold.ttf' if bold else 'Barlow-Regular.ttf'), px)
def rgba(p): return Image.open(p).convert('RGBA')
def text(d, xy, s, px, fill=CREAM, bold=True, anchor='la'): d.text(xy, s, font=f(px, bold), fill=fill, anchor=anchor)
def wrap(d, xy, s, px, width, fill=DIM, bold=False, lh=1.32):
    x, y = xy; line = ''
    for w in s.split():
        t = (line + ' ' + w).strip()
        if d.textlength(t, font=f(px, bold)) > width and line: text(d, (x, y), line, px, fill, bold); y += int(px * lh); line = w
        else: line = t
    if line: text(d, (x, y), line, px, fill, bold); y += int(px * lh)
    return y
def head(im, d, title, sub):
    text(d, (60, 44), title, 44); text(d, (60, 100), sub, 21, DIM, False)
    d.rectangle((60, 142, 60 + 140, 144), fill=TURQL)
def cameos_row(im, src, x, y, tag=None):
    """the cameo at 256, 128 (the roster @2x) and 64 px (the roster's size), real pixels on the kit's deep teal"""
    c = rgba(src)
    for s in (256, 128, 64):
        im.alpha_composite(c.resize((s, s), Image.LANCZOS), (x, y + (256 - s))); x += s + 24
    return x
def option_board(name, title, sub, opts, notes, out):
    W, H = 2480, 1500; im = Image.new('RGBA', (W, H), INK + (255,)); d = ImageDraw.Draw(im)
    head(im, d, title, sub)
    colw = (W - 120 - 2 * 40) // 3
    for i, (k, label, line) in enumerate(opts):
        x = 60 + i * (colw + 40); y = 180
        text(d, (x, y), label, 30); y += 48
        pl = rgba(B + f'concepts/{k}.png'); pl = pl.resize((colw, int(colw * 2 / 3)), Image.LANCZOS)
        d.rectangle((x - 3, y - 3, x + colw + 2, y + pl.height + 2), outline=CHROME, width=2); im.alpha_composite(pl, (x, y)); y += pl.height + 28
        end = cameos_row(im, B + f'cameos/{k}.png', x, y); y += 256 + 24
        text(d, (x, y), 'cameo 256 / 128 / 64 px (real pixels)', 15, DIM, False); y += 36
        y = wrap(d, (x, y), line, 20, colw, CREAM, False)
        text(d, (x, y + 8), f'concepts/{k}.png   cameos/{k}.png', 14, DIM, False)
    y = H - 150
    wrap(d, (60, y), notes, 19, W - 120, DIM, False)
    im.convert('RGB').save(out)
def board_vale():
    option_board('vale', 'Vale: three councillors', 'Concept plates (Runware gpt-image@2; Codex was down), each a head-and-shoulders and a full-body front, with the roster-sized cameo cut from it.',
      [('vale_A', 'A  Silver, ivory stand-collar', 'Silver swept-back hair, lined face, no glasses. Ivory jacket with a tall ribbed stand collar and thin gold piping over ink black. The plainest, the most "institution": the silhouette is a tall white collar over a dark column.'),
       ('vale_B', 'B  Chain of office', 'White hair and beard, deep brown skin. Ivory tabard under a broad ink-black yoke and a chunky chrome-and-gold collar of office. The most readable at 50 px: the black yoke and the bright collar make a bib shape no one else in the cast has.'),
       ('vale_C', 'C  Spectacles, shoulder disc', 'Cropped grey hair, round chrome spectacles, a grey moustache. Ivory tunic-jacket, ink collar band, one big black disc cut-out at the shoulder, black gloves. The coldest face; the shoulder disc is the one-off silhouette cue.')],
      'All three: ivory is the dominant block (the carried Vale reads against the Enforcer\'s black), ink and thin gold only as trim, hem at the hip, nothing flowing, no date in the prompt. Prompts: paid/063-portraits/ledger.jsonl and tools in briefs/063/gen.py. My lean: B for readability, A if Sky wants understatement.', B + 'board-01-vale.png')
def board_inez():
    option_board('inez', 'Inez: three waitresses', 'Café waitress, nervous, in debt, in love with the door guard. Each plate has her tell: the hand at a locket (the affair), a tray held as a shield (the debt), the anxious smile.',
      [('inez_A', 'A  Seafoam, locket, glasses (closest to the model)', 'Chestnut bob with bangs, tortoiseshell glasses, a sidelong glance, one hand on a chrome locket. Seafoam shift, white ribbed collar and cuffs, small white apron. Same person as the in-game waitress, in couture: the model\'s green becomes seafoam.'),
       ('inez_B', 'B  Coral vinyl tabard, tray', 'High ponytail, tired eyes, biting her lip, tray clutched to her chest. Coral glossy tabard over a cream tunic and slim trousers. The strongest colour block in the café; reads as "serves" from the tray and the tabard.'),
       ('inez_C', 'C  Mint pillbox, curls', 'Curly dark hair under a mint pillbox cap, freckles, worried eyes, a smile that does not hold. Mint zip tunic with a round chest cut-out. The most expressive face and the cap is the silhouette.')],
      'The cameo is where the face shows at 64 px, so expression matters more than outfit: C is the readable nervous face, A the safest match to the model already in the game (so the cameo and the figure agree), B the best silhouette from above.', B + 'board-02-inez.png')
def board_driver():
    option_board('driver', 'The driver: three chauffeurs', 'Vale\'s official driver, needs cash. Institution dress (sleeker), job readable by silhouette plus one colour block.',
      [('driver_A', 'A  Mint tunic, visor band (the model today)', 'Close-cropped hair, tired kind eyes, heavy jaw. Mint sleeveless stand-collar tunic with two big chrome buttons over black, chrome visor band. The in-game driver, repainted as a person.'),
       ('driver_B', 'B  Ink black with gold, peaked visor', 'Receding grey hair, moustache, worry lines. Black fitted stand-collar jacket with gold piping, ivory gloves, chrome driving visor pushed up. Reads as the councillor\'s man; but black is the Enforcer\'s block and the security\'s.'),
       ('driver_C', 'C  Deep teal, white band, pillbox', 'A woman of forty, hair in a low bun, alert glance. Deep teal short jacket with a white chest band and a peaked chrome-badged pillbox cap. Distinct from every other block in the scene; changes the cast (the figure is male today).')],
      'The driver\'s block should not be ivory (Vale) or black (Enforcer, security). A keeps the mint the game already shows; C takes deep teal. B collides with black.', B + 'board-03-driver.png')
def board_models():
    W, H = 2480, 1520; im = Image.new('RGBA', (W, H), INK + (255,)); d = ImageDraw.Draw(im)
    head(im, d, 'Portrait renders of the in-game models', 'Blender, the real FBX and its 2048 albedo, 85 mm, framed like the roster cameos (round, head at 6 % to 70 % of the disc, neutral grey ground, thin dark ring). Not the concepts.')
    items = [('model_waitress', 'Inez (waitress W6)', 'ready'), ('model_driver', 'Driver, tint fixed', 'figures.json keep mask: skin stays skin'), ('model_driver_as_shipped', 'Driver before the fix', 'teal lips, nose, cheeks, hands'), ('model_councillor', 'Councillor stand-in', 'young commuter D4; not Vale'), ('model_security', 'Security officer', 'posted and response officers; not the patrol')]
    cw = (W - 120) // 5
    for i, (k, lab, st) in enumerate(items):
        x = 60 + i * cw; y = 190; c = rgba(B + f'cameos/{k}.png').resize((cw - 30, cw - 30), Image.LANCZOS); im.alpha_composite(c, (x, y))
        text(d, (x, y + cw - 5), lab, 24); text(d, (x, y + cw + 30), st, 17, DIM, False)
        cx = x; cy = y + cw + 78
        cc = rgba(B + f'cameos/{k}.png')
        for s in (128, 64):
            im.alpha_composite(cc.resize((s, s), Image.LANCZOS), (cx, cy + 128 - s)); cx += s + 20
    y0 = 190 + cw + 78 + 170
    for i, k in enumerate(['councillor', 'el1', 'waitress', 'driver', 'driver_fixed']):
        fg = rgba(B + f'models/fig_{k}.png'); fg = fg.resize((int(fg.width * 0.5), int(fg.height * 0.5)), Image.LANCZOS)
        bg = Image.new('RGBA', fg.size, (178, 174, 171, 255)); bg.alpha_composite(fg)
        im.alpha_composite(bg, (60 + i * (bg.width + 30), y0))
    xx = 60 + 5 * (bg.width + 30)
    text(d, (xx, y0 + 6), 'In the game today (full figure, same renderer)', 22)
    yy = wrap(d, (xx, y0 + 46), 'Left to right: councillor stand-in (young, blond, camel trousers), el1 (the only elder in the library: a woman in orange, not usable for Vale), the waitress, the driver before and after the tint fix.', 18, W - xx - 60, DIM, False)
    yy = wrap(d, (xx, yy + 14), 'The driver\'s teal skin was a build defect: figures.json tinted hue 2 to 20 deg (the coral uniform) and his lips, nose, cheeks and hands sit in that window. Fixed in the tint (tools/figures/build.py keep mask), not in the render.', 18, W - xx - 60, DIM, False)
    im.convert('RGB').save(B + 'board-04-models.png')
def board_patrol():
    W, H = 2480, 1000; im = Image.new('RGBA', (W, H), INK + (255,)); d = ImageDraw.Draw(im)
    head(im, d, 'Patrol cameo: three faces for badges/npc_patrol', 'Ink on the kit\'s cream enamel disc, the same treatment as the other npc_* badges. Built with Glyph\'s briefs/057/patrol/disc.py (preview only, nothing installed).')
    opts = [('A', 'A  Stern screen', 'Ink head, mast lamp, side ear caps, two dot-bar eyes, a short flat mouth. A machine that watches and does not smile (the stand-in smiles).'), ('B', 'B  Scan line', 'Same head; one row of dots with a larger middle dot, like a scan sweep. Reads as "watching", not as a face. The least cartoon.'), ('C', 'C  Cap and brim', 'A peaked brim over the screen, eyes lifted a little, mouth flat. The brim is the one cue that still shows at 24 px and sets it apart from the other npc_* discs.')]
    kit = rgba(R + 'unity/Assets/Suborned/UI/Resources/Kit/badges/npc_driver@2x.png')
    colw = (W - 120) // 3
    for i, (k, lab, line) in enumerate(opts):
        x = 60 + i * colw; y = 190; text(d, (x, y), lab, 30); y += 52
        b = rgba(B + f'patrol/npc_patrol_{k}@2x.png'); fb = rgba(B + f'patrol/npc_patrol_face_{k}@2x.png')
        im.alpha_composite(b.resize((288, 288), Image.NEAREST), (x, y)); im.alpha_composite(b.resize((96, 96), Image.NEAREST), (x + 320, y)); im.alpha_composite(b, (x + 320, y + 120)); im.alpha_composite(b.resize((24, 24), Image.LANCZOS), (x + 380, y + 120))
        im.alpha_composite(kit.resize((96, 96), Image.NEAREST), (x + 440, y)); im.alpha_composite(kit, (x + 440, y + 120))
        im.paste(fb.convert('RGB'), (x + 320, y + 190)); text(d, (x + 380, y + 240), '(face-only, for Cameo())', 13, DIM, False)
        wrap(d, (x, y + 310), line, 20, colw - 50, CREAM, False)
        text(d, (x, y + 420), f'face art: patrol/face_{k}.png (disc.py build)', 14, DIM, False)
    text(d, (60, H - 60), 'Right of each: the kit\'s npc_driver at 96 and 48 px, for scale. Face PNGs are 464 px ink-on-transparent for disc.py build; previews of the two outputs are patrol/npc_patrol_*@2x.png.', 16, DIM, False)
    im.convert('RGB').save(B + 'board-05-patrol.png')
if __name__ == '__main__':
    board_vale(); board_inez(); board_driver(); board_models(); board_patrol()
