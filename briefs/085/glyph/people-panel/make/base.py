"""The real frames the mocks stand on, and the budget's inputs. Glyph, brief 085 note 10."""
import json, os, cv2, numpy as np
from PIL import Image
from kit import ROOT, Layer, CREAM, INK, font
OUT = ROOT + 'briefs/085/glyph/people-panel/'
SCR = ROOT + 'out/scratch/glyph/people-panel/'
B = ROOT + 'briefs/085/latch/'; S1 = ROOT + 'out/batch/shots-prev1/'; S2 = ROOT + 'out/batch/shots-prev2/'


def rgb(p): return Image.open(p).convert('RGB')


def inpaint(im, boxes, r=5):
    a = np.array(im)[:, :, ::-1].copy(); m = np.zeros(a.shape[:2], np.uint8)
    for x0, y0, x1, y1 in boxes: m[y0:y1, x0:x1] = 255
    return Image.fromarray(cv2.inpaint(a, m, r, cv2.INPAINT_TELEA)[:, :, ::-1])


def frame_open():
    """0:05, a real frame: the cell by the car, Inez at the café door. start-overhead-day, shots of the last batch."""
    return rgb(S1 + 'start-overhead-day.png')


def frame_learned():
    """0:59, Listen on Inez just landed, nothing else given: r085-door-price (its ring open), with the ring and its pills lifted out using
    real-player-overhead, the same view one second later. The two bubbles and the busy chip that frame carries are painted out; Inez's marks stay."""
    a, b = rgb(B + 'r085-door-price.png'), rgb(B + 'real-player-overhead.png')
    a.paste(b.crop((600, 372, 1320, 645)), (600, 372))
    a.paste(rgb(B + 'r085-door-price.png').crop((1048, 486, 1114, 522)), (1048, 486))
    return inpaint(a, [(1034, 486, 1050, 518), (1044, 462, 1052, 490)])  # two bubbles stay in that frame's place: spoken_learned covers them


def frame_door():
    """1:20, one Help landed: door-today (Latch's camera board). Real, as it stands."""
    return rgb(B + 'camera/door-today.png')


def frame_door_stopped():
    """door-today for the guide: the Tech, the Enforcer, the bubble and the busy strip painted out, since at 0:00 the cell is by the car."""
    return inpaint(rgb(B + 'camera/door-today.png'), [(1432, 568, 1482, 662), (1560, 905, 1604, 962), (878, 342, 1046, 364), (106, 146, 278, 178)], 7)


def frame_cafe_stopped():
    """frame_learned without the Scout and the Fixer, for the guide's ring at 0:00."""
    return inpaint(frame_learned(), [(988, 500, 1098, 642), (885, 376, 1040, 398), (1040, 440, 1150, 464)], 9)


def clock(im, t, x=1445, cash=None, trail=None):
    """Re-letter the status pill's clock (and purse and Trail, where the frame has spent) for a stopped frame."""
    l = Layer(1); body = tuple(np.median(np.array(im)[28:44, 1503:1510].reshape(-1, 3), axis=0).astype(int).tolist())
    def patch(x0, x1, txt, size, tx):
        im.paste(body, (x0, 26, x1, 47)); l.text(tx, 36.5, txt, size)
    if x == 1445: patch(1462, 1492, '00', 22, 1463)  # only the seconds: the frames here read 0:05 and 0:59
    else: patch(x - 1, x + 49, t, 22, x)
    if cash: patch(1651, 1722, cash, 14.5, 1651)
    if trail: patch(1763, 1780, trail, 14.5, 1766)
    im.paste(l.img.convert('RGB'), (0, 0), l.img)
    return im


def spoken_learned(L):
    """Two real lines in the two bubbles' place: Inez's rounds, and the Scout's Listen line for a Fearful Inez (text.json dialogue)."""
    from people import bubble
    bubble(L, 884, 373, 'Hush, and drink it before it cools.'); L.note(884, 373, 240, 24, 'speech', 'Inez: Hush, and drink it before it cools.')
    bubble(L, 950, 437, 'Debt. The guard visits her quietly. She startles.'); L.note(950, 437, 320, 26, 'speech', 'Scout: Debt. The guard visits her quietly. She startles.')


BASE_HUD = {'open': S1 + 'start-overhead-day', 'learned': S2 + 'cafe-front-day'}


def compose(world, layer):
    """1920 x 1080 frame from a world frame and a layer at 2x."""
    down = layer.img.resize((1920, 1080), Image.LANCZOS)
    out = world.convert('RGBA'); out.alpha_composite(down); return out.convert('RGB')


def save(name, world, layer, base, state='rest', drop=('known people',), keep_plate=False):
    if state == 'guide': state, drop = 'active', ('known people', 'Take Vale alive.')
    """The mock, its full-size crop source (2x layer kept) and its budget inputs: mask = the base shot's HUD layer + this layer."""
    frame = compose(world, layer); os.makedirs(SCR + 'budget', exist_ok=True)
    frame.save(OUT + name + '.png')
    hud = Image.open(BASE_HUD[base] + '.hud.png').convert('RGBA')
    man = json.load(open(BASE_HUD[base] + '.manifest.json'))
    els = [e for e in man['elements'] if keep_plate or e.get('text') not in drop]
    if not keep_plate:  # the plate's own pixels leave the mask with it
        hud.paste((0, 0, 0, 0), (1800, 62, 1910, 106))
    hud.alpha_composite(layer.img.resize((1920, 1080), Image.LANCZOS))
    hud.save(SCR + f'budget/{name}.hud.png'); frame.save(SCR + f'budget/{name}.png')
    json.dump({'state': state, 'elements': els + layer.rects}, open(SCR + f'budget/{name}.manifest.json', 'w'), indent=0)
    return frame
