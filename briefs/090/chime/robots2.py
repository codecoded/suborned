"""Brief 090 (Chime): steps between robot 2 (brass horn with a ring) and robot 3 (one fixed pitch). The fixed-pitch layer is the vocoder alone, put through the same horn, and mixed with the horn voice: 2 = 0 %, a = 25 %, b = 50 %, c = 75 %, 3 = robot 3 as before. `render` makes every line each way, alone and over a fight; `reel` makes briefs/090/robots2.mp4 (five phrases each). Run from the repo root with the scratch venv's python -I."""
import json, subprocess, sys
from pathlib import Path
import numpy as np
from scipy.signal import butter, sosfilt
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE)); sys.path.insert(0, str(REPO / 'briefs/087/chime'))
import audiolib as A, reel as R, b2 as B, robots as RB
S = REPO / 'out/scratch/chime'; SR = 48000; O = S / 'q/robotfx2'; O.mkdir(parents=True, exist_ok=True)
STEPS = {'2': ('Robot 2: brass horn with a ring', 0.0), 'a': ('Step a: horn, a quarter machine', 0.25), 'b': ('Step b: half horn, half machine', 0.5), 'c': ('Step c: machine through the horn, a quarter voice', 0.75), '3': ('Robot 3: mechanical, one fixed pitch', None)}
SHOW = (4, 5, 8, 11, 19)


def voder(x):
    m = x[:, 0]; edges = np.geomspace(100, 7000, 21); t = np.arange(len(m)); pulse = np.zeros(len(m)); pulse[(t % int(SR / 120)) == 0] = 1.0; pulse = sosfilt(butter(1, 3000, 'lowpass', fs=SR, output='sos'), pulse)
    noise = np.random.default_rng(87).standard_normal(len(m)) * 0.05; out = np.zeros(len(m)); sm = butter(2, 40, 'lowpass', fs=SR, output='sos')
    for lo, hi in zip(edges[:-1], edges[1:]):
        bp = butter(2, [lo, hi], 'bandpass', fs=SR, output='sos'); env = sosfilt(sm, np.abs(sosfilt(bp, m))); out += sosfilt(bp, pulse if hi < 4000 else noise) * env
    return (out / (np.abs(out).max() + 1e-9) * np.abs(m).max())[:, None]


def step(k, x):
    if k == '3': return RB.fx3(x)
    a = STEPS[k][1]; h = RB.fx2(x); v = RB.fx2(voder(x)); h = h / (np.sqrt((h ** 2).mean()) + 1e-9); v = v / (np.sqrt((v ** 2).mean()) + 1e-9); return (1 - a) * h + a * v


def render():
    L = json.loads((S / 'robot_lines.json').read_text()); asr = []
    for id, text in L:
        x = RB.mono(RB.base(id))
        for k in STEPS:
            y = step(k, x); A.write_wav(O / f'{id}_{k}.wav', RB.lev(y, -18), SR); asr.append({'wav': str(O / f'{id}_{k}.wav'), 'line': text, 'k': k, 'ctx': 'alone'})
            z = RB.lev(y, -14); pad = np.vstack([np.zeros((int(.5 * SR), 2)), z, np.zeros((int(.5 * SR), 2))]); w = pad + B.under(len(pad), 'fight'); A.write_wav(O / f'{id}_{k}_fight.wav', w, SR); asr.append({'wav': str(O / f'{id}_{k}_fight.wav'), 'line': text, 'k': k, 'ctx': 'fight'})
    (S / 'asr_robotfx2_in.json').write_text(json.dumps(asr))


def reel():
    L = json.loads((S / 'robot_lines.json').read_text()); a_ = json.loads((S / 'asr_robotfx2.json').read_text()); res = {}
    for k in STEPS:
        for ctx in ('alone', 'fight'):
            v = [x for x in a_ if x['k'] == k and x['ctx'] == ctx]; res.setdefault(k, {'name': STEPS[k][0]})[f'misses_{ctx}'] = f"{sum(1 for x in v if x['word_error'] > 0)} of {len(v)}"; res[k][f'word_error_{ctx}'] = round(float(np.mean([x['word_error'] for x in v])), 3)
    audio, segs = [], []; N = 0
    def seg(h, t, sub, a, foot=''):
        nonlocal N
        p = R.TMP / f'r2{N:03d}.png'; N += 1; R.card(p, h, t, sub, None, foot); segs.append((p, len(a) / SR)); audio.append(a)
    seg('Robots', 'Between 2 and 3', 'Five voices from the horn to the fixed pitch. Five phrases each, alone, then two over a fight.', np.concatenate([R.sil(.3), R.speak('Between robot two and robot three. Five voices, five phrases each, then two over a fight.'), R.sil(.4)]), foot='Chime cannot hear these: your ears decide.')
    for k, (name, _) in STEPS.items():
        seg(name.split(':')[0], name, '', np.concatenate([R.sil(.3), R.speak(name.split(':')[0] + '.'), R.sil(.3)]))
        for i in SHOW:
            id, text = L[i - 1]; y, _ = A.read_wav(O / f'{id}_{k}.wav'); seg(name.split(':')[0], text, name, np.concatenate([y, R.sil(.45)]))
        ys = [A.read_wav(O / f'{L[i - 1][0]}_{k}_fight.wav')[0] for i in (11, 13)]; seg(name.split(':')[0], 'Over a fight', name, np.concatenate([R.speak('Over a fight.'), R.sil(.2)] + ys))
    a = np.concatenate(audio); a = a * min(1.0, 10 ** (-1 / 20) / np.abs(a).max()); w = R.TMP / 'r2.wav'; A.write_wav(w, a, SR)
    lst = R.TMP / 'cards-r2.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d:.3f}\n" for p, d in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(w), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / 'briefs/090/robots2.mp3')], check=True)
    v = R.TMP / 'r2v.mp4'; subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(w), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-movflags', '+faststart', str(v)], check=True, cwd=str(R.TMP))
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(v), '-t', f'{len(a) / SR:.3f}', '-c', 'copy', str(REPO / 'briefs/090/robots2.mp4')], check=True)
    res['reel_seconds'] = round(len(a) / SR, 1); (REPO / 'briefs/090/robots2.json').write_text(json.dumps(res, indent=1) + '\n'); print(json.dumps(res, indent=0))


if __name__ == '__main__': render() if sys.argv[1] == 'render' else reel()
