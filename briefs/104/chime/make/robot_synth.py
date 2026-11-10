"""Brief 104 (Chime): free drafts of the ball-balancing patrol robot's key sounds, synthesised. No model, no spend, no library clip.
Deterministic: fixed seeds, no clock, so the same code writes the same bytes. Writes 48 kHz 16-bit mono WAVs into briefs/104/chime/drafts/.
Run from the repo root with the scratch venv:   out/scratch/chime/venv/bin/python -I briefs/104/chime/make/robot_synth.py
Then measure:                                   out/scratch/chime/venv/bin/python -I briefs/104/chime/make/robot_measure.py

The robot (briefs/104/vela/patrolbot/notes.md): 1.27 m, a sage and cream enamel body on a 0.40 m gunmetal ball, an LED dot face,
a red beacon on its crown, a sidearm on a rail behind a hinged chest panel. Demo beats (make/demo_motion.py): panel opens 10.50 s,
sidearm out 11.00 s, first shot 11.67 s, so the draw lasts about 1.0 s; the rulebook's react-s gives a hostile 2 s from first sight to its first shot.
"""
import sys
from pathlib import Path
import numpy as np

HERE = Path(__file__).resolve().parent; REPO = HERE.parents[3]
sys.path.insert(0, str(REPO / 'briefs/087/chime'))
import audiolib as A  # noqa: E402  (brief 087's shared sums: filters, fades, the wav writer)

SR = 48000; TAU = 2 * np.pi
OUT = HERE.parent / 'drafts'


def rng(seed): return np.random.default_rng(seed)
def t_of(s): return np.arange(int(round(s * SR))) / SR
def at(buf, x, s, g=1.0):
    """Add x into buf starting at s seconds (clipped to buf)."""
    i = int(round(s * SR)); n = min(len(x), len(buf) - i)
    if n > 0: buf[i:i + n] += g * x[:n]
def env_ad(n, a, d, sr=SR):
    """Attack (s) then an exponential decay (time constant d s), length n."""
    t = np.arange(n) / sr; e = np.exp(-np.maximum(t - a, 0) / d); k = max(1, int(a * sr)); e[:k] *= np.linspace(0, 1, k); return e
def phase(f):
    """Phase of a per-sample frequency track."""
    return np.cumsum(TAU * np.asarray(f) / SR)
def bp(x, lo, hi, order=2): return A.lp(A.hp(x, lo, SR, order), hi, SR, order)
def fft_band(x, lo, hi):
    """A circular band filter (FFT domain): what goes out of the end comes back at the start, so a loop stays seamless."""
    X = np.fft.rfft(x); f = np.fft.rfftfreq(len(x), 1 / SR); X[(f < lo) | (f > hi)] = 0; return np.fft.irfft(X, len(x))
def modes(n, parts, seed):
    """A struck metal shell: inharmonic partials (Hz, gain, decay s), each with a random start phase."""
    t = np.arange(n) / SR; r = rng(seed); ramp = np.minimum(1, t / 0.0005)  # a 0.5 ms rise, so a partial never starts on a step
    return ramp * sum(g * np.exp(-t / d) * np.sin(TAU * f * t + r.uniform(0, TAU)) for f, g, d in parts)
def click(n, seed, lo=1500, hi=9000, d=0.004):
    """A short broadband tick: band-passed noise with a fast decay."""
    return bp(rng(seed).standard_normal(n), lo, hi) * env_ad(n, 0.0004, d)
def echo(x, taps, dark=2400):
    """A street's worth of space: dark delayed copies (s, gain), with room for the tail."""
    tail = max(d for d, _ in taps) + 0.25; o = np.concatenate([x, np.zeros(int(tail * SR))]); w = A.lp(x, dark, SR)
    for d, g in taps: at(o, w, d, g)
    return o
def speaker(x, lo=550, hi=5200, peak=2300):
    """The robot's own little loudspeaker grille: a narrow band with a bump where the cone rings."""
    y = bp(x, lo, hi); return y + 0.6 * bp(y, peak / 1.25, peak * 1.25, 2)


# ---------------------------------------------------------------- the roll and the hum (two loops, so the host can scale the roll by speed)
LOOP_S = 4.0  # every tone below is a whole number of cycles in 4 s (a multiple of 0.25 Hz), and every noise is filtered circularly: the end meets the start

def hum_loop():
    """Idle: the atomic gyroscope that keeps it upright. A soft low hum, a faint beating whine, and the clockwork escapement ticking at 6 Hz."""
    n = int(LOOP_S * SR); t = np.arange(n) / SR; o = np.zeros(n)
    wob = 1 + 0.06 * np.sin(TAU * 0.5 * t)                                     # the balance correcting itself, twice in a loop
    for h, g in ((1, 1.0), (2, 0.35), (3, 0.18), (5, 0.05)): o += g * np.sin(TAU * 118.0 * h * t) * wob
    o += 0.05 * (np.sin(TAU * 1846.0 * t) + 0.7 * np.sin(TAU * 1846.5 * t + 1.1)) * (0.8 + 0.2 * np.sin(TAU * 0.25 * t))  # the gyro's whine, beating at 0.5 Hz
    o += 0.10 * fft_band(rng(31).standard_normal(n), 60, 400)                   # air moving round the body
    tick = np.zeros(n); k = int(0.02 * SR)
    for i, s in enumerate(np.arange(1 / 12, LOOP_S, 1 / 6)):                         # tick-tock, alternating pitch: a clock, not a machine gun
        part = [(4200 if i % 2 else 3100, 1.0, 0.004), (6900, 0.4, 0.002)]; at(tick, modes(k, part, 40 + i), s)
    return o + 0.22 * tick

def roll_loop():
    """Moving: the ball on stone. A low rumble and a grit band whose level rides the ball's turn (1 rev/s here = 1.26 m/s, the host
    pitch-scales it by speed), the band's seam passing the ground once a turn, and a slight scrub of the halves."""
    n = int(LOOP_S * SR); t = np.arange(n) / SR
    rev = 1.0; turn = 0.5 - 0.5 * np.cos(TAU * rev * t)
    rumble = fft_band(rng(11).standard_normal(n), 35, 190) * (1.0 + 0.25 * turn)
    grit = fft_band(rng(12).standard_normal(n), 380, 1400) * (0.55 + 0.45 * fft_band(rng(13).standard_normal(n), 0.25, 3.0) * 6)
    hiss = fft_band(rng(14).standard_normal(n), 2500, 6500)
    seam = np.zeros(n); k = int(0.09 * SR)
    for i, s in enumerate(np.arange(0.31, LOOP_S, 1 / rev)):
        q = np.arange(k) / SR; at(seam, np.sin(TAU * (92 - 30 * q / 0.09) * q) * env_ad(k, 0.002, 0.025) + 0.3 * click(k, 50 + i, 600, 3000, 0.006), s)
    o = 2.2 * rumble + 0.55 * grit + 0.08 * hiss + 0.35 * seam
    return o


def rotate_to_seam(x):
    """A circular loop can start anywhere: start it where the step from the last sample to the first is the smallest, near a quiet zero crossing."""
    d = np.abs(x - np.roll(x, 1)); lv = np.convolve(np.abs(x), np.ones(480) / 480, 'same'); i = int(np.argmin(d + 0.5 * lv)); return np.roll(x, -i)


# ---------------------------------------------------------------- the voice: face changes
def chirp(notes, seed, buzz=0.0, glide=0.025):
    """A run of short tones (Hz, length s, gap s), gliding into each, a square-ish wave rounded off, through the robot's grille.
    `buzz` adds a 60 Hz rasp: the stern voice."""
    total = sum(d + g for _, d, g in notes) + 0.08; n = int(total * SR); f = np.zeros(n); a = np.zeros(n); s = 0.0; prev = notes[0][0]
    for hz, d, g in notes:
        i, k = int(s * SR), int(d * SR); q = np.arange(k) / SR
        f[i:i + k] = hz + (prev - hz) * np.exp(-q / glide) if isinstance(hz, (int, float)) else hz(q)
        a[i:i + k] = np.minimum(1, q / 0.006) * np.minimum(1, (d - q) / 0.02); s += d + g; prev = f[i + k - 1]
    f[f == 0] = prev; t = np.arange(n) / SR; f = f * (1 + 0.006 * np.sin(TAU * 7 * t))
    ph = phase(f); w = sum(np.sin(h * ph) / h ** 1.15 for h in (1, 3, 5, 7)) + 0.25 * np.sin(2 * ph)
    w *= 1 - buzz * (0.5 - 0.5 * np.sign(np.sin(TAU * 60 * t)))
    return speaker(w * a) + 0.002 * bp(rng(seed).standard_normal(n), 2000, 6000) * a

def chirp_curious():
    """'Hm?': two notes rising, the second bending up at its end, as a question. Plays when the face goes to curious (attention: noticed)."""
    bend = lambda q: 1180 + 520 * (q / 0.17) ** 2
    return chirp([(880, 0.075, 0.035), (bend, 0.17, 0.0)], 61)

def chirp_alert():
    """'Hey!': two quick high notes and a lower held one, with a rasp: firmer, not a scream. Plays when the face goes to alert (wary, reporting)."""
    return chirp([(1568, 0.06, 0.03), (1568, 0.06, 0.03), (1175, 0.2, 0.0)], 62, buzz=0.35, glide=0.012)


# ---------------------------------------------------------------- the siren: a little 1950s motor siren, wound up twice
def siren():
    """A mechanical siren (a rotor chopping air through a stator) in miniature: higher and much shorter than the city's police siren,
    two quick whoops and a wind-down. The rotor's pitch is one track; the timbre is a pulse train with a horn's two formants, plus breath."""
    dur = 1.75; n = int(dur * SR); t = np.arange(n) / SR
    k1 = [(0.0, 380), (0.42, 1240), (0.52, 1240), (0.74, 820), (1.06, 1260), (1.16, 1260), (1.75, 260)]
    f = np.interp(t, [p for p, _ in k1], [v for _, v in k1])
    f = np.convolve(np.pad(f, (600, 600), mode='edge'), np.hanning(1201) / np.hanning(1201).sum(), 'valid')  # round the corners, 25 ms
    ph = phase(f); w = np.zeros(n)
    for h in range(1, 14): w += np.sin(h * ph) * (1.0 if h == 1 else 0.55 / h ** 0.7) / (1 + (h * f / 4200) ** 4)
    breath = bp(rng(71).standard_normal(n), 900, 5000) * (0.5 + 0.5 * np.sin(ph))
    motor = 0.18 * np.sin(phase(f / 6)) * (f / 1260)                             # the little motor under it, turning at a sixth of the rotor's chop
    a = np.minimum(1, t / 0.04) * np.minimum(1, (dur - t) / 0.35) * (0.55 + 0.45 * (f / 1260))
    y = (w + 0.12 * breath + motor) * a
    y = y + 0.8 * bp(y, 950, 1500, 2) + 0.5 * bp(y, 2300, 3100, 2)                 # the horn's two resonances
    return echo(y, [(0.11, 0.22), (0.23, 0.12), (0.37, 0.06)])


# ---------------------------------------------------------------- the draw: latch, panel up, slide out, lock, ready
def draw():
    """The moment that matters: 'it has drawn on you'. About 1.0 s, cut to the demo's beats (panel 0 to 0.45 s, slide 0.5 to 0.9 s), and ending
    on a tone, because clacks alone get lost under a bed or a fight and a tone carries on a laptop speaker.
      0.00  the panel's latch lets go: a sharp click and a spring's tink
      0.02  the hinge motor: a small geared whirr rising as the panel swings up
      0.45  the panel reaches its stop: a dull tin thunk (the shell rings a little)
      0.50  the sidearm runs out on its rail: a rising scrape with ratchet teeth getting faster
      0.90  it locks home: a hard double clack and a low ring of the body
      0.98  ready: two short rising tones from its grille (the face goes to focused here)"""
    dur = 1.45; n = int(dur * SR); o = np.zeros(n)
    k = int(0.06 * SR); at(o, click(k, 81, 2500, 11000, 0.003) + 0.35 * modes(k, [(5200, 1, 0.03), (7700, 0.5, 0.02)], 82), 0.0, 0.9)
    m = int(0.44 * SR); q = np.arange(m) / SR; f = 700 + 620 * (q / 0.44) ** 0.8
    gear = np.sign(np.sin(phase(f / 4.5))) * 0.5 + np.sin(phase(f)) + 0.4 * np.sin(phase(2 * f))
    whirr = bp(gear, 300, 4500) * np.minimum(1, q / 0.04) * np.minimum(1, (0.44 - q) / 0.05)
    at(o, whirr + 0.25 * bp(rng(83).standard_normal(m), 1500, 6000) * np.minimum(1, (0.44 - q) / 0.05), 0.02, 0.32)
    k = int(0.25 * SR); q = np.arange(k) / SR
    thunk = np.sin(TAU * (190 - 60 * q / 0.25) * q) * env_ad(k, 0.001, 0.03) + 0.5 * modes(k, [(640, 1, 0.06), (1460, 0.7, 0.05), (2380, 0.45, 0.04)], 84)
    at(o, thunk + 0.5 * click(k, 85, 800, 5000, 0.005), 0.45, 0.55)
    m = int(0.40 * SR); q = np.arange(m) / SR; scrape = bp(rng(86).standard_normal(m), 1800, 7000) * (q / 0.4) ** 0.7 * np.minimum(1, (0.4 - q) / 0.02)
    teeth = np.zeros(m); s = 0.0; rate = 18.0
    while s < 0.38:
        at(teeth, click(int(0.01 * SR), int(s * 1000) + 87, 2000, 8000, 0.0018), s); rate *= 1.07; s += 1 / rate
    at(o, 0.35 * scrape + 0.5 * teeth, 0.50)
    k = int(0.4 * SR); q = np.arange(k) / SR
    clack = click(k, 88, 1200, 12000, 0.004) + 0.8 * np.roll(click(k, 89, 1500, 10000, 0.003), int(0.008 * SR))
    body = modes(k, [(220, 1.0, 0.09), (540, 0.8, 0.08), (1290, 0.55, 0.07), (2870, 0.4, 0.05)], 90)
    at(o, 1.3 * clack + 0.55 * body + 0.6 * np.sin(TAU * (110 - 40 * q / 0.4) * q) * env_ad(k, 0.001, 0.04), 0.90)
    for s, hz in ((0.985, 1046.5), (1.075, 1396.9)):                                 # C6 then F6, a rising fourth: "ready"
        k = int(0.07 * SR); q = np.arange(k) / SR; tone = sum(np.sin(TAU * hz * h * q) / h ** 1.3 for h in (1, 2, 3))
        at(o, speaker(tone * np.minimum(1, q / 0.004) * np.minimum(1, (0.07 - q) / 0.015)), s, 0.42)
    return echo(o, [(0.12, 0.10), (0.26, 0.05)])


# ---------------------------------------------------------------- the shot
def shot():
    """Its sidearm: a small, hard, dry crack with a pneumatic push, and then what no person's gun has: the robot's own tin shell
    ringing for a fifth of a second, and the sidearm's 18 mm recoil clacking back on its rail. Short street echo."""
    dur = 0.55; n = int(dur * SR); t = np.arange(n) / SR; o = np.zeros(n)
    crack = A.hp(rng(91).standard_normal(n), 900, SR, 2) * env_ad(n, 0.0003, 0.012)
    push = A.lp(rng(92).standard_normal(n), 700, SR, 2) * env_ad(n, 0.0015, 0.045)
    drop = np.sin(phase(70 + 110 * np.exp(-t / 0.02))) * env_ad(n, 0.0008, 0.06)
    shell = modes(n, [(1480, 1.0, 0.16), (2310, 0.8, 0.13), (3570, 0.55, 0.10), (5020, 0.35, 0.07), (930, 0.45, 0.12)], 93) * np.minimum(1, t / 0.002)
    o += 1.0 * crack + 0.9 * push + 0.8 * drop + 0.11 * shell
    k = int(0.05 * SR); at(o, click(k, 94, 1500, 9000, 0.003) + 0.3 * modes(k, [(3100, 1, 0.015)], 95), 0.036, 0.5)
    o = np.tanh(3.0 * o / np.abs(o).max()) / np.tanh(3.0)                          # a soft clip, as a real report is: denser, so it can be as loud as the other guns under the ceiling
    return echo(o, [(0.09, 0.20), (0.19, 0.11), (0.31, 0.05)], dark=2000)


CUES = {  # name: (maker, LUFS-M target, loop)
    'robot_draw': (draw, -20.0, False),
    'robot_siren': (siren, -22.0, False),
    'robot_roll': (roll_loop, -36.0, True),
    'robot_hum': (hum_loop, -42.0, True),
    'robot_chirp_alert': (chirp_alert, -27.0, False),
    'robot_chirp_curious': (chirp_curious, -28.0, False),
    'robot_shot': (shot, -18.5, False),
}
CEIL_DBTP = -1.0


def finish(name, x, target, loop):
    """Loudness to its mix target (momentary max for one-shots, integrated for loops), a true-peak ceiling, short fades on one-shots."""
    x = rotate_to_seam(x) if loop else A.fade(x, SR, 0.0, 0.03)
    lv = A.lufs_i(x, SR) if loop else A.lufs_m(x, SR); x = x * 10 ** ((target - lv) / 20)
    tp = A.true_peak(x)
    if tp > CEIL_DBTP: x = x * 10 ** ((CEIL_DBTP - tp) / 20)   # quieter than asked rather than clipped; measured and reported
    return x


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, (fn, target, loop) in CUES.items():
        y = finish(name, fn(), target, loop); A.write_wav(OUT / f'{name}.wav', y, SR); print(f'{name:22} {len(y) / SR:5.2f} s')
    # a listening aid, not a cue: the robot's whole beat from the demo, roll, stop, curious, alert, siren, draw, three shots, in one file
    sc = scene(); A.write_wav(OUT / 'robot_beat_preview.wav', sc, SR); print(f'{"robot_beat_preview":22} {len(sc) / SR:5.2f} s')


def scene():
    """The demo's 20 s timeline at the drafts' own levels, no bed: patrol 0-5 s, stop, curious 5.9, alert 8.6 with the siren, draw 10.5 and the first shot 11.67 as in
    make/demo_motion.py, then two more 1.5 s apart (the rulebook's pistol rate; the demo's 0.4 s spacing is the render's), and the roll away."""
    rd = lambda n: A.read_wav(OUT / f'{n}.wav')[0][:, 0]
    o = np.zeros(int(17.0 * SR)); hum = rd('robot_hum'); roll = rd('robot_roll')
    hl = np.resize(hum, len(o)); o += hl
    rl = np.resize(roll, int(5.4 * SR)); e = np.ones(len(rl)); k = int(0.6 * SR); e[-k:] = np.linspace(1, 0, k) ** 2; e[:int(0.3 * SR)] = np.linspace(0, 1, int(0.3 * SR)); at(o, rl * e, 0.0)
    rl2 = np.resize(roll, int(1.4 * SR)) * np.linspace(0, 1, int(1.4 * SR)) ** 0.5; at(o, rl2, 15.6)
    at(o, rd('robot_chirp_curious'), 5.93); at(o, rd('robot_chirp_alert'), 8.6); at(o, rd('robot_siren'), 8.75)
    at(o, rd('robot_draw'), 10.5)
    for s in (11.67, 13.17, 14.67): at(o, rd('robot_shot'), s)
    return A.fade(o * 10 ** (-1.5 / 20), SR, 0.0, 0.6)


if __name__ == '__main__':
    main()
