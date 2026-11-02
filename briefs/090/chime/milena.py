"""Brief 090 (Chime): three Milenas. The Listen scene three times, Inez and Larthi as in round two, Milena cloned from each option's reference; game levels over the cutaway room tone. Measures each option's lines against Larthi's and Inez's references with the speaker encoder (out/scratch/chime/emb_mil.json, made by embed.py). Writes briefs/090/milena.mp4 (+ .mp3) and briefs/090/milena.json."""
import json, subprocess, sys
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE)); sys.path.insert(0, str(REPO / 'briefs/087/chime'))
import audiolib as A, reel as R, lines as LN, voice_measure as VM, b2 as B
S = REPO / 'out/scratch/chime'; SR = 48000
DESC = {'m1': ('Bright and quick', 'about 25, Inez\'s age: a bright, quick, grinning tease'), 'm2': ('Swedish lilt', 'about 25: a light Swedish accent, sing-song, relaxed and dry'), 'm3': ('Older and husky', 'early forties: low, husky, slow, a big sister\'s dry warmth')}


def main():
    P = B.pick(); asr = json.loads((S / 'asr_mil.json').read_text()); best = {}
    for x in asr: best.setdefault((x['k'], x['stem']), []).append((x['word_error'], x['seed'], x['wav']))
    best = {k: sorted(v)[0] for k, v in best.items()}; e = json.loads((S / 'emb_mil.json').read_text()); refs = {Path(w).stem.split('_')[0]: np.array(v) for w, v in e['ref_all'].items()}
    res = {'options': {}}; audio, segs = [], []; N = 0
    def seg(h, t, sub, a, foot=''):
        nonlocal N
        p = R.TMP / f'mil{N:02d}.png'; N += 1; R.card(p, h, t, sub, None, foot); segs.append((p, len(a) / SR)); audio.append(a)
    seg('Milena', 'Three Milenas', 'The Listen scene three times, with Inez and Larthi unchanged. Game level, over the cutaway room tone.', np.concatenate([R.sil(.3), R.speak('Three Milenas. The Listen scene, three times.'), R.sil(.4)]), foot='Chime cannot hear these: your ears decide.')
    for n, k in enumerate(('m1', 'm2', 'm3'), 1):
        V = [np.array(v) for w, v in e[k].items()]; cl = [float(v @ refs['larthi']) for v in V]; ci = [float(v @ refs['inez']) for v in V]
        meas = [VM.row(Path(best[(k, f'scene{i:02d}')][2])) for i, (w, _) in enumerate(LN.SCENE, 1) if w == 'milena']
        res['options'][str(n)] = {'id': k, 'name': DESC[k][0], 'what': DESC[k][1], 'cos_to_larthi_mean': round(float(np.mean(cl)), 3), 'cos_to_inez_mean': round(float(np.mean(ci)), 3), 'pitch_hz_median': round(float(np.median([m['f0_median'] for m in meas])), 0), 'pitch_spread_semitones': round(float(np.median([m['f0_spread_semitones'] for m in meas])), 1), 'speaking_s_total': round(sum(m['speaking_s'] for m in meas), 1), 'word_errors_in_picked': [best[(k, f'scene{i:02d}')][0] for i, (w, _) in enumerate(LN.SCENE, 1) if w == 'milena']}
        L = []
        for i, (w, line) in enumerate(LN.SCENE, 1):
            src = best[(k, f'scene{i:02d}')][2] if w == 'milena' else P[f'scene{i:02d}_{w}'][2]; L.append((w, line, B.clip(src, -23)))
        gap = R.sil(.35); sc = np.concatenate([np.concatenate([l, gap]) for _, _, l in L]); sc = sc + B.under(len(sc), 'scene')
        seg(f'Milena {n}', f'Milena {n}: {DESC[k][0]}', DESC[k][1], np.concatenate([R.sil(.3), R.speak(f'Milena {n}.'), R.sil(.3)]))
        t = 0
        for w, line, l in L: m = len(l) + len(gap); seg(f'Milena {n}', B.NAME[w], line, sc[t:t + m], foot=f'option {n}'); t += m
    res['old_milena_cos_to_larthi'] = round(float(refs['milena'] @ refs['larthi']), 3); res['note'] = 'Cosines from Qwen3-TTS Base\'s own speaker encoder: relative only (different people\'s references sit 0.93-0.99 apart). Lower is further.'
    a = np.concatenate(audio); a = a * min(1.0, 10 ** (-1 / 20) / np.abs(a).max()); w = R.TMP / 'mil.wav'; A.write_wav(w, a, SR)
    lst = R.TMP / 'cards-mil.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d:.3f}\n" for p, d in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(w), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / 'briefs/090/milena.mp3')], check=True)
    v = R.TMP / 'milv.mp4'; subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(w), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-movflags', '+faststart', str(v)], check=True, cwd=str(R.TMP))
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(v), '-t', f'{len(a) / SR:.3f}', '-c', 'copy', str(REPO / 'briefs/090/milena.mp4')], check=True)
    res['reel'] = {'seconds': round(len(a) / SR, 1), 'lufs_i': round(A.lufs_i(a, SR), 1)}; (REPO / 'briefs/090/milena.json').write_text(json.dumps(res, indent=1, ensure_ascii=False) + '\n'); print(json.dumps(res, indent=0))


if __name__ == '__main__': main()
