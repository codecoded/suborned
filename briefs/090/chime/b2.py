"""Brief 090 (Chime): round two's B-only recording. Picks one take per line (the first seed the machine listener hears word for word, names mapped from their respellings), trims it, sets it to game level (scene and names -23 LUFS, shouts -14 LUFS momentary), and plays the scene over room tone and the ducked bed, the shouts alone and over the street and a fight. Writes briefs/090/voices-b2.mp4 (+ .mp3), briefs/090/b2.json. Run from the repo root with the scratch venv's python -I."""
import json, re, subprocess, sys
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE)); sys.path.insert(0, str(REPO / 'briefs/087/chime'))
import audiolib as A, reel as R, lines as LN, voice_measure as VM
SR = 48000; S = REPO / 'out/scratch/chime'; AUD = REPO / 'unity/Assets/Suborned/Resources/Audio/087'
NAME = {'milena': 'Milena', 'inez': 'Inez', 'larthi': 'Larthi', 'arthur': 'Arthur', 'arruns': 'Arruns', 'police': 'Civic Police officer', 'rival': 'A rival', 'watcher': 'Camera desk', 'hasti': 'Hasti', 'lukac': 'Lukac', 'sethre': 'Sethre', 'popovic': 'Popovic'}
PAT = {'Larthi': r"\bl'?[ae]h?r[- ]?t(y|ee|i|ie|ey)\b|\blardy\b|\blarthey\b", 'Popovic': r"\bpop+[oa]-?vi?t?ch\b", 'Lukac': r"\blu-?ka?h?-?t?ch\b|\blukac\b", 'Nikolaou': r"\bni[ck]h?o-?la[ouw]+\b|\bnicolaou\b"}
norm = lambda s: re.sub(r"[^a-z' ]", ' ', s.lower().replace("'s", '')).split()


def wer(ref, hyp):
    for n, p in PAT.items(): hyp = re.sub(p, n, hyp, flags=re.I)
    r, h = norm(ref), norm(hyp); d = list(range(len(h) + 1))
    for i in range(1, len(r) + 1):
        q, d[0] = d[0], i
        for j in range(1, len(h) + 1): q, d[j] = d[j], min(d[j] + 1, d[j - 1] + 1, q + (r[i - 1] != h[j - 1]))
    return d[len(h)] / max(1, len(r))


def pick():
    takes = {}
    for f in ('asr_prod.json', 'asr_sh.json'):
        for x in json.loads((S / f).read_text()):
            if x['stem'].startswith('con_') or x['mode'] == 'design' or (x['stem'].startswith('shout') and x['mode'] == 'clone'): continue
            takes.setdefault(x['stem'], []).append((wer(x['line'], x['heard']), x['seed'], x['wav'], x['heard']))
    return {k: sorted(v)[0] for k, v in takes.items()}


def clip(wav, lu, mom=False):
    x, sr = A.read_wav(wav); x = A.resample(x.mean(axis=1, keepdims=True), sr, SR); m = np.abs(x[:, 0]); h = int(.01 * SR); e = np.sqrt(np.convolve(m ** 2, np.ones(h) / h, 'same')); on = np.where(e > e.max() * 10 ** (-40 / 20))[0]
    x = A.fade(x[max(0, on[0] - int(.06 * SR)):on[-1] + int(.12 * SR)], SR, .005, .04); y = x * 10 ** ((lu - (A.lufs_m(x, SR) if mom else A.lufs_i(x, SR))) / 20); return np.repeat(y * min(1, 10 ** (-1 / 20) / np.abs(y).max()), 2, axis=1)


def under(n, what):
    bed = A.read_wav(AUD / 'street_bed_even.wav')[0]; bed = A.resample(bed, 32000, SR)
    if what == 'scene': room = A.resample(A.read_wav(AUD / 'room_tone.wav')[0], 32000, SR); return np.resize(np.repeat(room, 2, axis=1), (n, 2)) + np.resize(bed, (n, 2)) * 10 ** (-14 / 20)
    if what == 'street': return np.resize(bed, (n, 2))
    fx, fsr = A.read_wav(S / 'mix.wav'); f = A.resample(fx[34 * fsr:46 * fsr], fsr, SR); return np.resize(f, (n, 2)) * 10 ** (-4 / 20)


def main():
    P = pick(); audio, segs = [], []; N = 0; res = {'picked': {k: {'seed': v[1], 'word_error': round(v[0], 3), 'heard': v[3], 'wav': Path(v[2]).name} for k, v in P.items()}}
    def seg(h, t, sub, a, foot=''):
        nonlocal N
        p = R.TMP / f'b{N:03d}.png'; N += 1; R.card(p, h, t, sub, None, foot); segs.append((p, len(a) / SR)); audio.append(a)
    seg('Voices, round two', 'Service B, fixed', 'Run on this Mac. Each voice designed once, then every line cloned from it. Game levels: scene -23, shouts -14 LUFS.', np.concatenate([R.sil(.3), R.speak('Service B, round two, at game levels. The Listen scene, ten shouts, then the names.'), R.sil(.4)]), foot='Chime cannot hear these: your ears decide.')
    lines = [clip(P[f'scene{i:02d}_{w}'][2], -23) for i, (w, _) in enumerate(LN.SCENE, 1)]; gap = R.sil(.35); sc = np.concatenate([np.concatenate([l, gap]) for l in lines]); sc = sc + under(len(sc), 'scene')
    seg('The Listen scene', 'Milena, Inez, then Larthi', 'Over the cutaway room tone and the ducked street, as the game plays it.', np.concatenate([R.speak('The Listen scene.'), R.sil(.3)]))
    t = 0
    for i, ((w, line), l) in enumerate(zip(LN.SCENE, lines), 1): n = len(l) + len(gap); seg('The Listen scene', NAME[w], line, sc[t:t + n], foot=f'{i} of 10'); t += n
    seg('Shouts', 'Ten shouts, alone', 'At -14 LUFS momentary, the level for a fight.', np.concatenate([R.sil(.3), R.speak('Ten shouts, alone.'), R.sil(.3)]))
    sh = {}
    for i, (w, line, _) in enumerate(LN.SHOUTS, 1): sh[i] = clip(P[f'shout{i:02d}_{w}'][2], -14, True); seg('Shouts', NAME[w], line, np.concatenate([sh[i], R.sil(.6)]), foot=f'{i} of 10')
    for what, ids, txt in (('street', (1, 5, 9), 'over the street'), ('fight', (2, 7, 8), 'over a fight, the fight dipped 4 dB under each shout')):
        a = np.concatenate([np.concatenate([R.sil(.8), sh[i]]) for i in ids] + [R.sil(.8)]); a = a + under(len(a), what); seg('Shouts in context', f'Shouts {txt}', ', '.join(LN.SHOUTS[i - 1][1] for i in ids), np.concatenate([R.speak(f'Shouts {txt.split(",")[0]}.'), R.sil(.2), A.fade(a, SR, .2, .3)]))
    seg('Names', 'Lines that say a name', 'Larthi, Popovic, Lukac, Nikolaou, as respelled for the model.', np.concatenate([R.sil(.3), R.speak('Names.'), R.sil(.3)]))
    for i, (w, line, src) in enumerate(LN.NAMES, 1): seg('Names', NAME[w], line, np.concatenate([clip(P[f'name{i:02d}_{w}'][2], -23), R.sil(.5)]), foot='test line' if 'TEST' in src else f'{i} of 8')
    a = np.concatenate(audio); a = a * min(1.0, 10 ** (-1 / 20) / np.abs(a).max()); w = R.TMP / 'b2.wav'; A.write_wav(w, a, SR)
    lst = R.TMP / 'cards-b2.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d:.3f}\n" for p, d in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(w), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / 'briefs/090/voices-b2.mp3')], check=True)
    v = R.TMP / 'b2v.mp4'; subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(w), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-movflags', '+faststart', str(v)], check=True, cwd=str(R.TMP))
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(v), '-t', f'{len(a) / SR:.3f}', '-c', 'copy', str(REPO / 'briefs/090/voices-b2.mp4')], check=True)
    meas = {k: VM.row(Path(v[2])) for k, v in P.items()}; res['measured'] = meas
    by = {}
    for k, m in meas.items():
        if not k.startswith('shout') and m['f0_median']: by.setdefault(k.split('_')[1], []).append(m['f0_median'])
    res['speech_pitch_by_person_hz'] = {k: round(float(np.median(v)), 0) for k, v in by.items()}; res['reel'] = {'seconds': round(len(a) / SR, 1), 'lufs_i': round(A.lufs_i(a, SR), 1), 'peak_db': round(A.sample_peak(a), 1)}
    (REPO / 'briefs/090/b2.json').write_text(json.dumps(res, indent=1, ensure_ascii=False) + '\n'); print(json.dumps({k: res[k] for k in ('speech_pitch_by_person_hz', 'reel')})); print({k: (v['word_error'], v['heard']) for k, v in res['picked'].items() if v['word_error'] > 0})


if __name__ == "__main__": main()
