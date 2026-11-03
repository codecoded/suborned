"""The crossing signal, pick A (brief 061, Vela, 2026-10-25; Sky, 2026-10-25: "your recommendation seem good"): the floating lens with a lit rim, a face atlas with numerals, and the clock ring in the paving.
Replaces SignalEye (crossing_eye.py, retired). Look only: trams and people do not obey it, the sim reads nothing from it.

SignalLens   Signals   Frame as SignalEye's: metres, Z up, pivot = the ground point under the head (the beam's foot). The two faces are local -Y (A, looks ACROSS the road at the people waiting on the far kerb)
                       and +Y (B, looks over the pavement at the people waiting here); both always show the same aspect. The export places it at yaw walk -/+ pi/2 on the near / far landing, as SignalEye;
                       the heads are never turned at runtime (the builder's `Dress` turn goes). Parts, all sharing the origin, each its own renderer and material slot(s):
  DK_SignalLens          the body: a chrome coin on edge (36-sided lathe, 4 bands, 0.88 m across at the equator, 0.30 m through the faces), the charcoal plate on each face, the nozzle and finial. DK_Chrome, DK_Enamel_Charcoal.
  DK_SignalLens_Rim      the lit rim all the way round the equator: DK_Signal_Rim (emission only, the state colour; Latch blinks it).
  DK_SignalLens_Figure   two quads (one per face, 0.50 m) on DK_Signal_Atlas: the pictogram.        The three face parts each hold one quad on face A and one on face B, so one MaterialPropertyBlock
  DK_SignalLens_Tens     two quads (0.146 x 0.234 m): the tens digit.                                  per part drives both faces: _EmissionColor for the state and _BaseMap_ST for the glyph
  DK_SignalLens_Units    two quads: the units digit.                                                   (URP Lit reads the emission map through _BaseMap_ST). UVs are cut to the WALK frame (the
                         walking figure, blank digits): a head nobody drives still reads WALK. _BaseMap_ST for another glyph = (1, 1, du, dv), tabulated in Data/signal-lens.json.
  DK_SignalLens_Beam     open 8-sided cone from the nozzle to the ground: DK_Cross_Beam (glow); UV u round, v 0 ground to 1 nozzle.
  DK_SignalLens_Halo     translucent disc on the ground (r 0.47 m): DK_Cross_Halo (glow); UV = x, y in metres from the beam centre. The clock ring is its own piece (HaloRing).
HaloRing     Ground    The 24-segment clock ring in the paving: ONE quad 1.24 m square, z 5 mm, on DK_Signal_Ring (signal_tex.py's ring map), its UV the planar radial map (x, y over the square, 0..1) turned 180 degrees
                       so segment 0 is at local -Y, the way across the road: place it with the head's own transform and the clock starts at 12 o'clock as the person waiting here sees the road. The map's alpha is the
                       segment index (segment k clockwise from the top holds 1 - (k + 0.5) / 24), so alpha clip does the fill: _Cutoff = 1 - lit / 24 lights exactly `lit` segments (ring and gaps are 0). One property.

The state data (colours, cues, blink, the cycle, the atlas and ring tables) is written beside the manifest as Data/signal-lens.json by export_kit when SignalLens is built; Latch's SignalHead and Glint read it.
Colours: emission only, base black (Glint: a lit colour past the Neutral shoulder flattens to cream, so each state has its own multiplier, Glint trims). Red on a small emissive display is allowed (GDD s10, s15)."""

import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import signal_cells as st  # noqa: E402
from geo import circle  # noqa: E402
from kit import asset  # noqa: E402
from mats import TEXTURES, pal, register  # noqa: E402

# ----------------------------------------------------------------------------- the look data (one source: the Blender build and Data/signal-lens.json read these)
STATE_COLOURS = {"walk": "#55E6B0", "clearing": "#FFB624", "stop": "#F2432E", "arriving": "#F2432E"}   # the brief 061 board's: mint, amber, red, red (ARRIVING is told from STOP by its cues, not its colour)
# Glint (2026-10-25, briefs/061 'Glint'; source of truth unity/Assets/Suborned/Resources/Atmosphere/signals.json `crossing`): per state and phase the trimmed pre-grade colour and the linear level
# (_EmissionColor = linear(colour) x level, HDR). Glint's, not Vela's to retune. ARRIVING takes STOP's.
EMISSION = {
    "walk":     dict(day=1.30, evening=1.15, night=1.15, colour=dict(day="#53FFB3", evening="#39FFC6", night="#00FFA9")),
    "clearing": dict(day=1.90, evening=1.90, night=1.75, colour=dict(day="#FF8C00", evening="#FF8B06", night="#FF8800")),
    "stop":     dict(day=1.15, evening=0.90, night=1.20, colour=dict(day="#FF5A59", evening="#FF6766", night="#FF4B46")),
}
EMISSION["arriving"] = EMISSION["stop"]
EMISSION_DAY = {k: v["day"] for k, v in EMISSION.items()}                                                   # the day levels (the .mat holds the walk's day trim)
UNTRIMMED_DAY = {"walk": 2.0, "clearing": 2.0, "stop": 1.8, "arriving": 1.8}                                # Vela's first levels on the board colours, kept as `dayAsBuilt`
EMISSION_NOTE = ("Glint 2026-10-25: _EmissionColor = linear(colour[phase]) x emission[phase], HDR. `colour` is the trim (pre-grade); the board colour (`board`, = this state's `colour` above) is what the grade then shows. "
                 "Source of truth: unity/Assets/Suborned/Resources/Atmosphere/signals.json (crossing); fit and model in briefs/061/glint/.")
BLINK = dict(hz=2.0, offLevel=0.15, clearingLastSeconds=3.0, arriving="whole state")                     # the rim; 2 Hz is 0.25 s on, 0.25 s off at the off level; the figure and numerals do not blink
CROSSFADE_S = 0.2                                                                                          # the colour crossfades; the pictogram and the digits cut
RING_N = st.RING_N
# What each state shows besides colour. The state never rests on colour alone: CLEARING and ARRIVING differ by the figure; STOP and ARRIVING by the numerals, the rim and the ring; WALK and STOP by the figure.
CUES = {
    "walk":     dict(figure="walk_full",  numerals="none",                rim="steady",                   ring="full",                  dots="all lit",
                     note="the figure walks; nothing counts"),
    "clearing": dict(figure="walk_head",  numerals="seconds left",        rim="steady, blinks in the last 3 s", ring="drains: lit = ceil(24 x left / clearing)", dots="go dark from both kerbs toward the middle, in step with the seconds left",
                     note="the window is closing: the figure still walks, the numerals count down"),
    "stop":     dict(figure="stand_full", numerals="none",                rim="steady",                   ring="full",                  dots="all lit",
                     note="the figure stands; nothing counts"),
    "arriving": dict(figure="stand_head", numerals="3, 2, 1",             rim="blinks",                   ring="fills: lit = floor(24 x elapsed / 3)", dots="all lit",
                     note="the last 3 s of STOP: the window is opening, counted down"),
}
CYCLE = dict(walkSeconds=6, clearingSeconds="ceil(W / 1.4), W = kerb-to-kerb carriageway width crossed (m)", stopBaseSeconds=14, arrivingSeconds=3,
             stopSeconds="14 + clearing, its last 3 s are ARRIVING", phase="(missionSeconds + offset(id)) mod cycle, cycle = walk + clearing + stop",
             offset="FNV-1a 32-bit of the crossing id's UTF-8 bytes, mod cycle (deterministic: the same on every replay; mission clock, never frame time)",
             order="walk, clearing, stop (the last 3 s arriving)", examples={"13 m (Orbit Walk)": [6, 10, 24], "28 m (the Boulevard)": [6, 20, 34]}, status="[S] look only; the sim reads nothing from it")
assert len({tuple(v[k] for k in ("figure", "numerals", "rim", "ring", "dots")) for v in CUES.values()}) == len(CUES), "two states differ by colour alone"
assert all(CUES[a]["figure"] != CUES[b]["figure"] or CUES[a]["numerals"] != CUES[b]["numerals"] for a in CUES for b in CUES if a < b), "two states share figure and numerals"

register("DK_Signal_Rim", "emitmap", EMISSION["walk"]["colour"]["day"], EMISSION_DAY["walk"])
register("DK_Signal_Atlas", "emitmap", EMISSION["walk"]["colour"]["day"], EMISSION_DAY["walk"], 0.5)
register("DK_Signal_Ring", "emitmap", EMISSION["walk"]["colour"]["day"], EMISSION_DAY["walk"], 0.01)
TEXTURES.setdefault("DK_Signal_Atlas", ("signal_atlas_v1.png", 1.0))
TEXTURES.setdefault("DK_Signal_Ring", ("signal_ring_v1.png", 1.0))
import crossing_common  # noqa: E402,F401  (registers DK_Cross_Beam and DK_Cross_Halo, the glow parts the lens keeps)

# ----------------------------------------------------------------------------- the lens
ZC, RB, HT, PR = 2.55, 0.44, 0.15, 0.31      # hover height of the centre, equator radius, half thickness at the faces, plate radius
FACES = ((90, 0, 0), (90, 0, 180))           # local +Z out of the face: -Y (A) and +Y (B)
FIG_Q = st.FACE_M                            # figure quad side
DIG_H = round(0.19 * st.CELL / st.DIGIT_PX_H, 4)                  # digit quad height: the 0.19 m digit is DIGIT_PX_H of the 64 px cell
DIG_W = DIG_H * st.DIGIT_RECT_W / st.CELL
DIG_X, DIG_Y = 0.085, -0.105                 # digit centres (tens at -DIG_X), plate-relative metres
QZ = 0.003                                   # the quads stand 3 mm off the plate
BEAM_TIP = ZC - (RB + 0.075) - 0.004         # the beam reaches the nozzle


def _uv(name, part):
    """The quad UVs (BL, BR, TR, TL) of a glyph cell rect."""
    _, (u0, v0, w, h) = st.cell_rect(name)
    return ((u0, v0), (u0 + w, v0), (u0 + w, v0 + h), (u0, v0 + h))


def _quad(p, cx, cy, w, h, z, mat, name):
    p.decal([(cx - w / 2, cy - h / 2, z), (cx + w / 2, cy - h / 2, z), (cx + w / 2, cy + h / 2, z), (cx - w / 2, cy + h / 2, z)], mat, _uv(name, p), lift=0.0)


def lens(s, lod):
    hi = lod == 0
    seg = 36 if hi else 18
    body = [(PR, -HT), (0.385, -0.115), (RB, 0.0), (0.385, 0.115), (PR, HT)] if hi else [(PR, -HT), (RB, 0.0), (PR, HT)]
    rim = [(RB - 0.02, -0.055), (RB + 0.035, 0.0), (RB - 0.02, 0.055)]
    with s.at((0, 0, ZC), FACES[0]):   # the body, revolved about the display axis (local Z = -Y)
        s.lathe(body, pal.Chrome, seg=seg, cap_bottom=False, cap_top=False)
    with s.part("Rim") as r, s.at((0, 0, ZC), FACES[0]):
        r.lathe(rim, pal.Signal_Rim, seg=seg, cap_bottom=False, cap_top=False)
    with s.at((0, 0, ZC)):
        s.lathe([(0.07, RB - 0.02), (0.04, RB + 0.035), (0, RB + 0.1)] if hi else [(0.07, RB - 0.02), (0, RB + 0.1)], pal.Chrome, seg=12 if hi else 8, cap_bottom=False)      # finial
        s.lathe([(0.045, -(RB + 0.075)), (0.05, -(RB + 0.045)), (0.075, -RB), (0.12, -(RB - 0.10))] if hi else [(0.045, -(RB + 0.075)), (0.12, -(RB - 0.10))],
                pal.Chrome, seg=12 if hi else 8, cap_bottom=False, cap_top=False)                                                                                            # nozzle: the lens and the sphere close it
    for rot in FACES:
        with s.at((0, 0, ZC), rot):
            s.lathe([(PR, HT), (0, HT)], pal.Enamel_Charcoal, seg=seg, cap_bottom=False)                                                                                       # the display plate
    z = HT + QZ
    for part, mat_name in (("Figure", "walk_full"), ("Tens", "blank"), ("Units", "blank")):
        for rot in FACES:
            with s.at((0, 0, ZC), rot), s.part(part) as p:
                if part == "Figure":
                    _quad(p, 0.0, 0.0, FIG_Q, FIG_Q, z, pal.Signal_Atlas, mat_name)
                else:
                    _quad(p, -DIG_X if part == "Tens" else DIG_X, DIG_Y, DIG_W, DIG_H, z + 0.001, pal.Signal_Atlas, mat_name)   # 1 mm over the figure quad's plane: their boxes overlap, their glyphs never do
    zb = 0.012
    with s.part("Beam") as b:
        b.sheet(lambda u, v: ((0.17 - 0.125 * v) * math.cos(2 * math.pi * u), (0.17 - 0.125 * v) * math.sin(2 * math.pi * u), zb + (BEAM_TIP - zb) * v), 8 if hi else 6, 1, pal.Cross_Beam)
    with s.part("Halo") as h:
        h.face([(x, y, 0.005) for x, y in circle(0.47, 12 if hi else 8)], pal.Cross_Halo)


@asset("SignalLens", "Signals", bevel=0.0, near=lambda s: lens(s, 1))
def signal_lens(s):
    lens(s, 0)


# ----------------------------------------------------------------------------- the clock ring
RING_Q = 2 * st.RING_HALF


@asset("HaloRing", "Ground", bevel=0.0)
def halo_ring(s):
    """One quad, 1.24 x 1.24 m, 5 mm proud, ring map turned 180 degrees (segment 0 at local -Y). 2 triangles."""
    h, z = st.RING_HALF, 0.005
    s.decal([(-h, -h, z), (h, -h, z), (h, h, z), (-h, h, z)], pal.Signal_Ring, ((1, 1), (0, 1), (0, 0), (1, 0)), lift=0.0)


# ----------------------------------------------------------------------------- Data/signal-lens.json
def data():
    """What Latch's SignalHead and Glint read. Glyph tables: `rect` = (u0, v0, w, h) of a glyph's cell in the atlas (v up); `st` per face part = the _BaseMap_ST that turns the part's WALK-frame UVs
    to that glyph: (1, 1, du, dv)."""
    cells = {n: st.cell_rect(n)[1] for n in st.CELLS}
    base = {"Figure": cells["walk_full"], "Tens": cells["blank"], "Units": cells["blank"]}
    tab = lambda part, names: {n: [1, 1, round(cells[n][0] - base[part][0], 6), round(cells[n][1] - base[part][1], 6)] for n in names}  # noqa: E731
    digits = [str(i) for i in range(10)]
    return dict(
        schema="suborned.signallens/1", brief="061", source="tools/decokit/crossing_lens.py, tools/decokit/signal_tex.py",
        note="Look only (Sky 2026-10-25): the cycle is runtime look, the sim reads nothing from it. SignalHead reads the mission clock and writes MaterialPropertyBlocks; the heads keep the export's `faces` yaw and are never turned.",
        states={k: dict(colour=STATE_COLOURS[k], emission=dict(day=EMISSION[k]["day"], evening=EMISSION[k]["evening"], night=EMISSION[k]["night"], colour=EMISSION[k]["colour"], board=STATE_COLOURS[k], owner="Glint", dayAsBuilt=UNTRIMMED_DAY[k], note=EMISSION_NOTE), **CUES[k]) for k in CUES},
        rim=dict(part="DK_SignalLens_Rim", material="DK_Signal_Rim", property="_EmissionColor", blink=BLINK, crossfadeSeconds=CROSSFADE_S),
        faces=dict(
            material="DK_Signal_Atlas", property="_BaseMap_ST", emissionProperty="_EmissionColor",
            note="One block per part drives both faces. Colour crossfades CROSSFADE_S, glyphs cut. Digits: clearing = ceil(seconds left), arriving = 3, 2, 1; under ten the tens part takes `blank`; walk and stop take `blank` on both digit parts.",
            parts={"Figure": dict(quads=2, size=[FIG_Q, FIG_Q], default="walk_full", st=tab("Figure", st.FIGURES)),
                   "Tens": dict(quads=2, size=[round(DIG_W, 4), DIG_H], default="blank", st=tab("Tens", digits + ["blank", "dash"])),
                   "Units": dict(quads=2, size=[round(DIG_W, 4), DIG_H], default="blank", st=tab("Units", digits + ["blank", "dash"]))},
            figureByState={k: CUES[k]["figure"] for k in CUES}),
        atlas=dict(file="Textures/signal_atlas_v1.png", size=[st.ATLAS, st.ATLAS], cell=st.CELL, alphaClip=0.5, cells={n: [round(v, 6) for v in r] for n, r in cells.items()}),
        beam=dict(part="DK_SignalLens_Beam", material="DK_Cross_Beam", alpha=0.26, colour="state"),
        halo=dict(part="DK_SignalLens_Halo", material="DK_Cross_Halo", alpha=0.40, colour="state"),
        ring=dict(piece="HaloRing", material="DK_Signal_Ring", texture="Textures/signal_ring_v1.png", segments=RING_N, fillProperty="_Cutoff",
                  lit="_Cutoff = 1 - lit / 24 lights exactly `lit` segments clockwise from local -Y (the way across the road)",
                  placement="the SignalLens's own transform: same pivot (the beam's foot) and yaw", size=[RING_Q, RING_Q], radii=[st.RING_R0, st.RING_R1],
                  walk="lit 24, state colour", clearing=CUES["clearing"]["ring"], stop="lit 24, red", arriving=CUES["arriving"]["ring"]),
        dots=dict(pieces=["LightDotRow_2", "LightDotRow_1"], material="DK_Cross_Light", property="_LitFrom / _LitTo", moduleLengths=[2.0, 1.0], pitch=0.5,
                  uv="u = metres from the module's -X end to the dot's centre (the same for every vertex of a dot, so a dot is lit or dark, never cut); v = metres across the row",
                  gate="a dot is lit when _LitFrom <= u <= _LitTo; per module, from the module's own u; a dark dot shows its charcoal enamel seat",
                  walk="all lit", clearing=CUES["clearing"]["dots"], stop="all lit, red", arriving="all lit, red"),
        cycle=CYCLE,
        stateNeverOnColourAlone=dict(
            rule="every pair of states differs by the figure, the numerals, the rim motion or the ring motion, not only by colour",
            pairs={f"{a}/{b}": [c for c in ("figure", "numerals", "rim", "ring") if CUES[a][c] != CUES[b][c]] for a in CUES for b in CUES if a < b}),
    )


def write_data(path):
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    Path(path).write_text(json.dumps(data(), indent=1) + "\n")
