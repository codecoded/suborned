"""Brief 090 (Chime): the robot voice, three ideas from one designed base voice (calm, even, polite; cloned per line):
  1 valve radio: band-limited 300 Hz-3.4 kHz, a small cabinet's resonance and soft valve saturation;
  2 brass horn with a ring: a small horn's two resonances, a 0.9 ms comb for metal, a light 90 Hz ring blended in, the clockwork;
  3 mechanical (Voder): a channel vocoder, the words' envelopes played on a fixed 120 Hz pulse, so it speaks on one pitch, with a quarter of the voice left in so it stays clear.
`render`: every line three ways, plus each over the street (-18 LUFS momentary over the bed) and over a fight (-14, the fight 4 dB down), for the machine listener; `reel`: briefs/090/robots.mp4. Run from the repo root with the scratch venv's python -I."""
import json, subprocess, sys
from pathlib import Path
import numpy as np
from scipy.signal import butter, sosfilt, iirpeak, lfilter
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE)); sys.path.insert(0, str(REPO / 'briefs/087/chime'))
import audiolib as A, reel as R, b2 as B
S = REPO / 'out/scratch/chime'; SR = 48000; O = S / 'q/robotfx'; O.mkdir(parents=True, exist_ok=True)
DESC = {1: ('Valve radio', 'a calm announcer heard through a small valve set: narrow, warm, a little fuzzy'), 2: ('Brass horn with a ring', 'the same voice from a small brass horn, with a light metallic ring: a clockwork machine'), 3: ('Mechanical voice', 'the words played on one fixed pitch, like an early talking machine, a quarter of the real voice kept for clarity')}


def peak(x, f, q, db):
    b, a = iirpeak(f, q, fs=SR); y = lfilter(b, a, x, axis=0); return x + y * (10 ** (db / 20) - 1)


def fx1(x):
    y = sosfilt(butter(4, [300, 3400], 'bandpass', fs=SR, output='sos'), x); y = peak(y, 1600, 2.0, 5); d = 10 ** (6 / 20); y = y / (np.abs(y).max() + 1e-9); y = np.tanh(d * (y + 0.08 * y ** 2)) / np.tanh(d)
    k = int(.0012 * SR); z = y.copy(); z[k:] += 0.3 * y[:-k]; return z


def fx2(x):
    y = sosfilt(butter(2, 450, 'highpass', fs=SR, output='sos'), x); y = peak(peak(y, 1100, 4, 6), 2600, 4, 5); k = int(.0009 * SR); z = y.copy(); z[k:] += 0.5 * y[:-k]
    t = np.arange(len(z)) / SR; ring = z * np.sin(2 * np.pi * 90 * t)[:, None]; return 0.8 * z + 0.2 * ring


def fx3(x):
    m = x[:, 0]; edges = np.geomspace(100, 7000, 21); t = np.arange(len(m)); f0 = 120; pulse = np.zeros(len(m)); pulse[(t % int(SR / f0)) == 0] = 1.0; pulse = sosfilt(butter(1, 3000, 'lowpass', fs=SR, output='sos'), pulse)
    rng = np.random.default_rng(87); noise = rng.standard_normal(len(m)) * 0.05; out = np.zeros(len(m)); sm = butter(2, 40, 'lowpass', fs=SR, output='sos')
    for lo, hi in zip(edges[:-1], edges[1:]):
        bp = butter(2, [lo, hi], 'bandpass', fs=SR, output='sos'); env = sosfilt(sm, np.abs(sosfilt(bp, m))); car = pulse if hi < 4000 else noise; out += sosfilt(bp, car) * env
    out = out / (np.abs(out).max() + 1e-9) * np.abs(m).max(); return (0.75 * out + 0.25 * m)[:, None]


FX = {1: fx1, 2: fx2, 3: fx3}


def base(stem):
    a = json.loads((S / 'asr_robot.json').read_text()); c = sorted((x['word_error'], x['seed'], x['wav']) for x in a if x['stem'] == stem)[0]; return c[2]


def mono(wav):
    x, sr = A.read_wav(wav); x = A.resample(x.mean(axis=1, keepdims=True), sr, SR); m = np.abs(x[:, 0]); h = int(.01 * SR); e = np.sqrt(np.convolve(m ** 2, np.ones(h) / h, 'same')); on = np.where(e > e.max() * 10 ** (-40 / 20))[0]
    return A.fade(x[max(0, on[0] - int(.06 * SR)):on[-1] + int(.12 * SR)], SR, .005, .04)


def lev(x, lu): y = x * 10 ** ((lu - A.lufs_m(x, SR)) / 20); return np.repeat(y * min(1, 10 ** (-1 / 20) / np.abs(y).max()), 2, axis=1)


def render():
    L = json.loads((S / 'robot_lines.json').read_text()); asr_in = []
    for id, text in L:
        x = mono(base(id))
        for n, f in FX.items():
            y = lev(f(x), -18); A.write_wav(O / f'{id}_o{n}.wav', y, SR); asr_in.append({'wav': str(O / f'{id}_o{n}.wav'), 'line': text, 'opt': n, 'ctx': 'alone', 'id': id})
            for ctx, lu in (('street', -18), ('fight', -14)):
                z = lev(f(x), lu); pad = np.vstack([np.zeros((int(.5 * SR), 2)), z, np.zeros((int(.5 * SR), 2))]); w = pad + B.under(len(pad), ctx); A.write_wav(O / f'{id}_o{n}_{ctx}.wav', w, SR); asr_in.append({'wav': str(O / f'{id}_o{n}_{ctx}.wav'), 'line': text, 'opt': n, 'ctx': ctx, 'id': id})
    (S / 'asr_robotfx_in.json').write_text(json.dumps(asr_in))


def reel():
    L = json.loads((S / 'robot_lines.json').read_text()); a_ = json.loads((S / 'asr_robotfx.json').read_text()); res = {'options': {}}
    for n in FX:
        for ctx in ('alone', 'street', 'fight'):
            v = [x['word_error'] for x in a_ if x['opt'] == n and x['ctx'] == ctx]; res['options'].setdefault(str(n), {'name': DESC[n][0], 'what': DESC[n][1]})[f'word_error_{ctx}'] = {'mean': round(float(np.mean(v)), 3), 'lines_with_errors': int(sum(1 for e in v if e > 0)), 'of': len(v)}
    audio, segs = [], []; N = 0
    def seg(h, t, sub, a, foot=''):
        nonlocal N
        p = R.TMP / f'rob{N:03d}.png'; N += 1; R.card(p, h, t, sub, None, foot); segs.append((p, len(a) / SR)); audio.append(a)
    seg('Robots', 'The patrol robot, three ways', 'One calm, polite base voice, three treatments. Every line at street level, -18 LUFS. Then three lines over the street and three over a fight.', np.concatenate([R.sil(.3), R.speak('The patrol robot, three ways. Every line, then some over the street and a fight.'), R.sil(.4)]), foot='Chime cannot hear these: your ears decide.')
    for n in FX:
        seg(f'Robot {n}', f'Robot {n}: {DESC[n][0]}', DESC[n][1], np.concatenate([R.sil(.3), R.speak(f'Robot {n}.'), R.sil(.3)]))
        for i, (id, text) in enumerate(L, 1):
            y, _ = A.read_wav(O / f'{id}_o{n}.wav'); seg(f'Robot {n}', text, '', np.concatenate([y, R.sil(.45)]), foot=f'{i} of {len(L)}')
        for ctx, ids in (('street', (4, 5, 13)), ('fight', (8, 11, 22))):
            ys = [A.read_wav(O / f'{L[i - 1][0]}_o{n}_{ctx}.wav')[0] for i in ids]; seg(f'Robot {n}', f'Over the {ctx}', ' · '.join(L[i - 1][1] for i in ids), np.concatenate([R.speak(f'Over the {ctx}.'), R.sil(.2)] + ys))
    a = np.concatenate(audio); a = a * min(1.0, 10 ** (-1 / 20) / np.abs(a).max()); w = R.TMP / 'rob.wav'; A.write_wav(w, a, SR)
    lst = R.TMP / 'cards-rob.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d:.3f}\n" for p, d in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(w), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / 'briefs/090/robots.mp3')], check=True)
    v = R.TMP / 'robv.mp4'; subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(w), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-movflags', '+faststart', str(v)], check=True, cwd=str(R.TMP))
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(v), '-t', f'{len(a) / SR:.3f}', '-c', 'copy', str(REPO / 'briefs/090/robots.mp4')], check=True)
    res['reel'] = {'seconds': round(len(a) / SR, 1), 'lufs_i': round(A.lufs_i(a, SR), 1)}; res['base_voice'] = {'reference': 'q/rref/robot_s122.wav', 'pitch_hz': 161, 'pitch_spread_semitones': 5.0, 'why': 'heard word for word, in band, the evenest of six takes'}
    (REPO / 'briefs/090/robots.json').write_text(json.dumps(res, indent=1) + '\n'); print(json.dumps(res, indent=0))


if __name__ == '__main__': render() if sys.argv[1] == 'render' else reel()
