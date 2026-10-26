# Glyph, brief 067, board 02 (E): locked ways show. python3 -I briefs/067/board02.py -> briefs/067/board-02-locked-ways.png
import sys; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/067")
from g67 import *
OUTD = R + "briefs/067/"
SH = R + "out/scratch/suture/shots0122/out/"
INEZ_SHOT = SH + "05-inez-fixer-4680-call-locked.png"; INEZ_HUD = SH + "05-inez-fixer-4680-call-locked.hud.png"
GUARD_SHOT = SH + "01-guard-scout-3300-listen.png"; GUARD_HUD = SH + "01-guard-scout-3300-listen.hud.png"
INEZ = (958, 527); GUARD = (958, 514)

def inez():
    return Frame(INEZ_SHOT, blot=[(880, 222, 1040, 312)])
def guard():
    return Frame(GUARD_SHOT, blot=[(738, 380, 1182, 440)])

# A: hover Inez with the Fixer selected: the locked pill (Call is locked: Alarm up)
def pa():
    f = inez(); pill(f, (INEZ[0], INEZ[1] - 68), None, "Call Inez", "Alarm up", None, faces=[FIXER], lock=True, dim=0.8); return f
# B: click her: the shut ring (never a walk)
def pb():
    f = inez(); C = (INEZ[0], INEZ[1] - 30)
    ring67(f, C, [dict(icon="call", word="Call", lock="Alarm up", shut=True, cams=[FIXER]), dict(more=True)], lit=-1); return f
# C: the Scout on the door guard after he was heard: the locked pill, and the shut ring with Fire last and dashed
def pc1():
    f = guard(); pill(f, (GUARD[0], GUARD[1] - 62), None, "Listen to the guard", "Already heard", None, faces=[SCOUT], lock=True, dim=0.8); return f
def pc2():
    f = guard(); C = (GUARD[0], GUARD[1] - 36)
    ring67(f, C, [dict(icon="listen", word="Listen to the guard", lock="Already heard", shut=True, cams=[SCOUT]),
                  dict(icon="fire", word="Fire", violent=True, fields=[("draws", "draws 0.6 s"), ("trail", "+2/hit"), ("alert", "≥2")], cams=[SCOUT]),
                  dict(more=True)], lit=-1); return f
# D: the Tech clicks the guard: no row at all: the faces pop, no walk
def pd():
    f = guard(); pop(f, GUARD[0] + 4, GUARD[1] - 40, [("camx", TECH), ("gap", 2), ("icon", "pointer"), ("gap", 2), ("cam", SCOUT), ("cam", FIXER)]); return f

if __name__ == "__main__":
    a, b, c1, c2, d = pa(), pb(), pc1(), pc2(), pd()
    B = {"locked-pill": budget(a, INEZ_HUD, False), "shut-ring": budget(b, INEZ_HUD, False), "guard-ring": budget(c2, GUARD_HUD, False), "pop": budget(d, GUARD_HUD, False)}
    cropI = (700, 330, 1220, 640); cropG = (700, 330, 1220, 650); cropR = (820, 330, 1340, 650)
    items = [
        (a.crop(cropI).convert("RGB"), 0, 0, "Hover Inez, the Fixer selected: the locked pill", "pill_status 38 px at 80 %: the lock disc, Call Inez in cream, a dim dot and Alarm up in --text-dim (Fable's short), the Fixer's face. No Hold, no sweep. The cursor takes the ring's three dots."),
        (b.crop(cropI).convert("RGB"), 1100, 0, "Click her: the shut ring, nothing taken, no walk", "Every locked way is a wedge, struck and never lit: the verb's icon at 45 %, the pill at 75 % with a lock and the short lock; the Fixer's cameo under the icon. A press pops the lock and closes the ring."),
        (c1.crop(cropG).convert("RGB"), 0, 760, "The Scout on the guard once he is heard", "the locked pill replaces Fire: Listen to the guard · Already heard. Before, the pill said Right hold · Fire and the click walked."),
        (c2.crop(cropR).convert("RGB"), 1100, 760, "Click him: the shut Listen first, Fire last and dashed", "a shut calm way takes its slot before any violent one, so Fire is never the whole ring while a calm way exists but is locked."),
        (d.crop(cropG).convert("RGB"), 0, 1540, "The Tech clicks the guard: nobody selected has a way", "plate_dark 26 px, 2 s: the Tech's cameo struck, a pointer, then the cameos of who can (the Scout, the Fixer). No words; Alt shows the long line. It replaces the 8-word sentence and the silent walk."),
    ]
    print(compose(items, OUTD + "board-02-locked-ways.png", "067 · E · locked ways show",
          "Suture's 0.2.122 frames (briefs/060-bot-playtests.md, shots 01 and 05), the old pills taken out and the new marks drawn from the kit at 2 px per game px (placement indicative)."))
    import json; print(json.dumps(B))
