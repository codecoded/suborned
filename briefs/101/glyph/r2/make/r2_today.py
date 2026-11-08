"""Today's HUD (build 0.2.268) at the five moments, measured as tools/hud-budget reads a capture: the .hud.png layer alone, alpha >= 24.
Rest, quiet and loud are real captures as they are. Aim and the busiest moment are real captures with the pieces they lack laid on from
other real captures' HUD layers (each piece's own pixels, placed where the game puts it), because no single capture holds them:
- aim (fight-loud-checkpoint): + the hold pill as built on the fight range, ready ("Let go · Fire · 75 % · Alert ≥2", hold-fire-ready);
- busiest (fight-loud-body, 3:47.5): + that pill on the blocker; + the focus disc, pips and fire mark (hold-focus); + the reporter's plate and
  "!" disc (why-quiet-radio, the brief 100 plate). Glyph, brief 101 round 2. Mock only."""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r2_base import *

PIECES = {  # source capture, box on it, where its anchor lands on the target capture (dx, dy)
    'pill': ('hold-fire-ready', (756, 398, 1162, 463)), 'focus': ('hold-focus', (990, 478, 1085, 580)), 'report': ('why-quiet-radio', (830, 378, 906, 446))}


def compose(base, adds):
    """The capture and its HUD layer, with pieces from other captures' HUD layers laid on at (dx, dy)."""
    pic = Image.open(SRC + base + '.png').convert('RGBA'); hud = Image.open(SRC + base + '.hud.png').convert('RGBA')
    for key, (dx, dy) in adds:
        src, (x0, y0, x1, y1) = PIECES[key]; shape = Image.open(SRC + src + '.hud.png').convert('RGBA').crop((x0, y0, x1, y1))
        look = Image.open(SRC + src + '.png').convert('RGBA').crop((x0, y0, x1, y1)); look.putalpha(shape.getchannel('A'))  # the colour as seen, the shape from the layer
        pic.alpha_composite(look, (x0 + dx, y0 + dy)); hud.alpha_composite(shape, (x0 + dx, y0 + dy))
    return pic.convert('RGB'), float((np.array(hud)[:, :, 3] >= 24).mean())


def plain(base):
    return Image.open(SRC + base + '.png').convert('RGB'), float((np.array(Image.open(SRC + base + '.hud.png').convert('RGBA'))[:, :, 3] >= 24).mean())


def render():
    out = {'rest': plain('start-overhead-day'), 'quiet': plain('why-quiet-radio'), 'loud': plain('why-loud-police')}
    # aim: the pill's tail tip (960, 460 on its capture) 6 px above the guard's 'taking aim' marker, as the host stands it (Glyph N9)
    out['aim'] = compose('fight-loud-checkpoint', [('pill', (912 - 960, 205 - 460))])
    # busiest: the focus marks over the blocker (his feet 1083, 772; on hold-focus the target's feet are 1008, 590), lifted 28 px clear of
    # today's holder mark; the pill's tail above them; the reporter's plate over the man walking to report (head 1345, 168; on the quiet
    # capture the guard's head is 867, 452)
    out['busy'] = compose('fight-loud-body', [('focus', (1083 - 1008, 772 - 590 - 28)), ('pill', (1083 - 960, 622 - 460)), ('report', (1345 - 867, 168 - 452))])
    os.makedirs(OUT2, exist_ok=True)
    for k, (im, cov) in out.items(): im.save(OUT2 + f'today-{k}.png')
    return out


if __name__ == '__main__':
    for k, (im, cov) in render().items(): print(k, f'today {cov:.2%}')
