"""Reference implementation of loading.json for Latch (brief 047). Renders a mockup:
   python3 -I mock.py loading.json ../fix/B_after.png out.png W H [progress 0..1]
Maths here = the maths Latch builds: cover mapping, image-space title rect fitted and clamped, ref-px scale = H/1080."""
import json, sys, os
from PIL import Image, ImageDraw, ImageFont
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../..'))
FONTS = {
  'Jost-Regular': ROOT + '/unity/Assets/Suborned/UI/Resources/Kit/fonts/Jost-Regular.ttf',
  'Jost-Medium': ROOT + '/unity/Assets/Suborned/UI/Resources/Kit/fonts/Jost-Medium.ttf',
  'Jost-Regular-Tab': ROOT + '/unity/Assets/Suborned/UI/Resources/Kit/fonts/Jost-Regular-Tab.ttf',
}
if not os.path.exists(FONTS['Jost-Regular']): FONTS['Jost-Regular'] = ROOT + '/testbed/hud-style/fonts/Jost-Regular-Tab.ttf'

def hexrgb(h): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))
def font(name, px): return ImageFont.truetype(FONTS[name], max(1, round(px)))

def tracked(d, xy, text, f, fill, track, anchor='la'):
    x, y = xy
    for i, ch in enumerate(text):
        d.text((x, y), ch, font=f, fill=fill, anchor='ls' if anchor == 'ls' else 'la')
        nxt = text[i+1] if i + 1 < len(text) else ''
        adv = f.getlength(ch + nxt) - f.getlength(nxt) if nxt else f.getlength(ch)
        x += adv + track
    return x

def tracked_w(text, f, track):
    w = 0
    for i, ch in enumerate(text):
        nxt = text[i+1] if i + 1 < len(text) else ''
        w += (f.getlength(ch + nxt) - f.getlength(nxt) if nxt else f.getlength(ch)) + (track if nxt else 0)
    return w

def smooth(t): return t * t * (3 - 2 * t)

def scrim(W, H, s):
    a = np.zeros((H, W), np.float32)
    if s['edge'] == 'full': a[:] = s['alphaAtEdge']; return a
    horiz = s['edge'] in ('left', 'right'); n = round(s['heightFrac'] * (W if horiz else H))
    hold = s.get('holdFrac', 0) * n   # full alpha for the first holdFrac of the band, then the ramp
    ramp = np.array([smooth(1 - max(0, i - hold) / (n - hold)) for i in range(n)], np.float32) * s['alphaAtEdge']   # 0 = at the edge
    if s['edge'] == 'bottom': a[H-n:, :] = ramp[::-1][:, None]
    elif s['edge'] == 'top': a[:n, :] = ramp[:, None]
    elif s['edge'] == 'left': a[:, :n] = ramp[None, :]
    else: a[:, W-n:] = ramp[::-1][None, :]
    return a

def base_canvas(cfg, pic, W, H, scrims=None):
    """The picture, covered and cropped to W x H, with scrims; returns (RGBA image, geometry)."""
    im = Image.open(pic).convert('RGB'); iw, ih = im.size
    s = max(W / iw, H / ih); dw, dh = iw * s, ih * s
    fx, fy = cfg['image']['focus']; ox, oy = (W - dw) * fx, (H - dh) * fy
    canvas = im.resize((round(dw), round(dh)), Image.LANCZOS).crop((round(-ox), round(-oy), round(-ox) + W, round(-oy) + H))
    arr = np.asarray(canvas).astype(np.float32)
    for sc in (cfg['scrims'] if scrims is None else scrims):
        a = scrim(W, H, sc)[..., None]; arr = arr * (1 - a) + np.array(hexrgb(sc['color']), np.float32) * a
    return Image.fromarray(arr.clip(0, 255).astype(np.uint8)).convert('RGBA'), dict(k=H / cfg['refHeight'], dw=dw, dh=dh, ox=ox, oy=oy)

def title(cfg, ov, W, H, g):
    k, dw, dh, ox, oy = g['k'], g['dw'], g['dh'], g['ox'], g['oy']; R = lambda v: v * k
    m = R(cfg['frame']['safeMarginRef'])

    # lockup measured at scale 1 (ref px), fitted into the image-space rect, clamped to the safe frame
    t = cfg['title']; lines = t['lines']; ls = []
    for ln in lines:
        f = font(ln['font'], ln['sizeRef'] * 4)  # measure large, scale down
        ls.append((ln, f, tracked_w(ln['text'], f, ln['trackingEm'] * ln['sizeRef'] * 4) / 4))
    wsub = [w for ln, f, w in ls if ln['text'] == t['rule']['widthOf']][0]
    lw = max(w for _, _, w in ls)
    lh = sum(ln['sizeRef'] * 0.72 + ln.get('gapAboveRef', 0) for ln, _, _ in ls) + t['rule']['gapAboveRef'] + t['rule']['heightRef']
    r = t['rect']; rx0, ry0 = r['x'] * dw + ox, r['y'] * dh + oy; rx1, ry1 = rx0 + r['w'] * dw, ry0 + r['h'] * dh
    rx0, ry0, rx1, ry1 = max(rx0, m), max(ry0, m), min(rx1, W - m), min(ry1, H - m)
    fit = min((rx1 - rx0) / lw, (ry1 - ry0) / lh, t['maxScale'] * k)
    x0 = rx0 if t['align'][0] == 'left' else rx1 - lw * fit
    y = ry0 if t['align'][1] == 'top' else ry1 - lh * fit
    sh = t['shadow']; shl = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sd = ImageDraw.Draw(shl)
    layers = []
    yy = y
    for ln, _, w in ls:
        yy += ln.get('gapAboveRef', 0) * fit
        px = ln['sizeRef'] * fit; f = font(ln['font'], px)
        base = yy + px * 0.72   # cap-height baseline
        col = hexrgb(ln['color']) + (round(255 * ln['opacity']),)
        layers.append((x0, base, ln['text'], f, ln['trackingEm'] * px, col))
        yy += px * 0.72
    for x, base, text, f, tr, col in layers:
        tracked(sd, (x + sh['offsetRef'][0] * fit, base + sh['offsetRef'][1] * fit), text, f, hexrgb(sh['color']) + (round(255 * sh['alpha']),), tr, 'ls')
    from PIL import ImageFilter
    ov.alpha_composite(shl.filter(ImageFilter.GaussianBlur(sh['blurRef'] * fit / 2)))
    d = ImageDraw.Draw(ov)
    for x, base, text, f, tr, col in layers: tracked(d, (x, base), text, f, col, tr, 'ls')
    ry = yy + t['rule']['gapAboveRef'] * fit
    d.rectangle([x0, ry, x0 + wsub * fit * 0.999, ry + max(1, t['rule']['heightRef'] * k)], fill=hexrgb(t['rule']['color']) + (255,))

def render(cfg, pic, W, H, prog, stage='Harbour District'):
    out, g = base_canvas(cfg, pic, W, H); k = g['k']; R = lambda v: v * k
    ov = Image.new('RGBA', (W, H), (0, 0, 0, 0)); title(cfg, ov, W, H, g); d = ImageDraw.Draw(ov)

    # bar
    b = cfg['bar']; bx0, bx1 = R(b['leftRef']), W - R(b['rightRef']); bh = R(b['heightRef']); by1 = H - R(b['bottomRef']); by0 = by1 - bh
    rad = R(b['radiusRef'])
    d.rounded_rectangle([bx0, by0, bx1, by1], rad, fill=hexrgb(b['track']['color']) + (round(255 * b['track']['alpha']),),
                        outline=hexrgb(b['rim']['color']) + (round(255 * b['rim']['alpha']),), width=max(1, round(R(b['rim']['widthRef']))))
    fx1 = bx0 + (bx1 - bx0) * prog
    if prog > 0:
        d.rounded_rectangle([bx0, by0, fx1, by1], rad, fill=hexrgb(b['fill']['color']) + (255,))
        cw = R(b['cap']['widthRef']); d.rounded_rectangle([max(bx0, fx1 - cw), by0, fx1, by1], rad, fill=hexrgb(b['cap']['color']) + (255,))
    # loading line
    L = cfg['loadingLine']; f = font(L['font'], R(L['sizeRef'])); tr = L['trackingEm'] * R(L['sizeRef'])
    base = by0 - R(L['gapAboveBarRef'])
    head = (L['head'] + ' ' + L['separator'] + ' ').upper() if L['uppercase'] else L['head']
    x = tracked(d, (bx0, base), head, f, hexrgb(L['headColor']) + (255,), tr, 'ls')
    tracked(d, (x, base), stage.upper(), f, hexrgb(L['textColor']) + (255,), tr, 'ls')
    if L['percent']['enabled']:
        pf = font(L['percent']['font'], R(L['percent']['sizeRef'])); txt = f'{round(prog*100)}%'
        d.text((bx1, base), txt, font=pf, fill=hexrgb(L['percent']['color']) + (255,), anchor='rs')
    # build
    B = cfg['build']
    if B['enabled']:
        bf = font(B['font'], R(B['sizeRef']))
        d.text((W - R(B['rightRef']), H - R(B['bottomRef'] - 2) - R(30)*0 + R(0)), '', font=bf)
        d.text((W - R(B['rightRef']), H - R(B['bottomRef']) + R(B['sizeRef']) + R(2) - R(14)), 'Build 0.2.80 · c023 · sim 0.8.2 · 8bf8ce6', font=bf,
               fill=hexrgb(B['color']) + (round(255 * B['alpha']),), anchor='rs')
    return Image.alpha_composite(out, ov).convert('RGB')

if __name__ == '__main__':
    cfg = json.load(open(sys.argv[1])); render(cfg, sys.argv[2], int(sys.argv[4]), int(sys.argv[5]), float(sys.argv[6]) if len(sys.argv) > 6 else 0.62).save(sys.argv[3])
