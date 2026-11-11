"""Brief 109 (Vela): a numeric model of the day play look, to judge a kit colour in game light from the desk (Sky: the patrol robot's green "doesn't quite look right").

game(lin) = what the play camera shows for a lit linear colour: the day grade (Scenes/HarbourDistrict-grade.asset: postExposure -0.063, contrast +10, saturation +25,
ACES), in URP's LUT order (contrast in ACEScc about mid grey, saturation about AP1 luminance in ACEScg, then ACES; ACES by Stephen Hill's RRT+ODT fit). The light on a
piece is fitted from a colour of known albedo in a capture (`fit_light`), then `solve` finds the albedo the play look shows at a target hue, lightness and saturation.
Checked on 0.2.330 (out/scratch/vela/109bot/game): the light fitted on the robot's cream predicts its sage #7E8C54 at #898001, measured #7F8106.
python3 -I tools/decokit/play_look.py   prints the patrol robot's solve (unity_mats.GAME_ALBEDO_109). No bpy; numpy only."""
import numpy as np
def s2l(c): c=np.asarray(c,float); return np.where(c<=0.04045,c/12.92,((c+0.055)/1.055)**2.4)
def l2s(c): c=np.clip(np.asarray(c,float),0,1); return np.where(c<=0.0031308,c*12.92,1.055*c**(1/2.4)-0.055)
def hexc(h): return np.array([int(h[i:i+2],16)/255 for i in (1,3,5)])
def tohex(c): return '#%02X%02X%02X'%tuple(int(round(x*255)) for x in np.clip(c,0,1))
SRGB_AP1=np.array([[0.6131,0.3395,0.0474],[0.0702,0.9164,0.0134],[0.0206,0.1096,0.8698]])
AP1_SRGB=np.linalg.inv(SRGB_AP1)
AP1_Y=np.array([0.2722287,0.6740818,0.0536895])
def acescc(x): x=np.maximum(x,1e-10); return (np.log2(x)+9.72)/17.52
def acescc_inv(y): return 2**(y*17.52-9.72)
MID=acescc(0.18)
IN=np.array([[0.59719,0.35458,0.04823],[0.07600,0.90834,0.01566],[0.02840,0.13383,0.83777]])
OUT=np.array([[1.60475,-0.53108,-0.07367],[-0.10208,1.10813,-0.00605],[-0.00327,-0.07276,1.07602]])
def rrt(v): return (v*(v+0.0245786)-0.000090537)/(v*(0.983729*v+0.4329510)+0.238081)
def aces_hill(lin_srgb): return np.clip(OUT@rrt(IN@lin_srgb),0,1)
def game(lin, exposure=-0.063, contrast=10, sat=25, tonemap=True):
    x=np.asarray(lin,float)*2**exposure
    ap1=SRGB_AP1@x
    ap1=acescc_inv((acescc(ap1)-MID)*(1+contrast/100)+MID)
    y=AP1_Y@ap1; ap1=y+(1+sat/100)*(ap1-y)
    x=AP1_SRGB@np.maximum(ap1,0)
    return l2s(aces_hill(x) if tonemap else np.clip(x,0,1))


def fit_light(albedo_hex, seen):
    """The light (linear rgb, green = level) on a surface of albedo `albedo_hex` that the play look shows as `seen` (0..1 sRGB)."""
    from itertools import product
    a, best = s2l(hexc(albedo_hex)), None
    for e in np.linspace(0.5, 3.0, 51):
        for wr, wb in product(np.linspace(0.8, 1.4, 31), np.linspace(0.4, 1.2, 41)):
            E = e * np.array([wr, 1, wb]); d = float(np.sum((game(a * E) - seen) ** 2))
            if best is None or d < best[0]: best = (d, E)
    return best[1]


def solve(E, hue, light, sat, step=2):
    """The albedo (hex) the play look shows at (hue degrees, HLS lightness, HLS saturation) under light E."""
    import colorsys
    best = None
    for r in range(30, 220, step):
        for g in range(30, 220, step):
            for b in range(20, 230, step):
                h, l, s = colorsys.rgb_to_hls(*game(s2l(np.array([r, g, b]) / 255) * E))
                dh = min(abs(h * 360 - hue), 360 - abs(h * 360 - hue))
                d = (dh / 3) ** 2 + ((l - light) / 0.02) ** 2 + ((s - sat) / 0.03) ** 2
                if best is None or d < best[0]: best = (d, (r, g, b))
    return '#%02X%02X%02X' % best[1]


if __name__ == "__main__":
    # the patrol robot (0.2.330, route-a-board tick 200): its cream #F3DDB0 seen as #ECCA7E; target = the Blender sage's hue (74) at its lightness against the cream
    # (0.51 / 0.84 of the cream's), saturation 0.25 (the play look runs about twice Blender's AgX saturation on the robot's colours)
    E = fit_light("#F3DDB0", hexc("#ECCA7E"))
    alb = solve(E, 74, 0.71 * 0.51 / 0.84, 0.25)
    print("light", E.round(3), "today #7E8C54 shows", tohex(game(s2l(hexc("#7E8C54")) * E)), "| albedo", alb, "shows", tohex(game(s2l(hexc(alb)) * E)))
