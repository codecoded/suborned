"""Brief 090 (Chime): two short recordings. larthi.mp4: three Larthis on her current lines (her words are about to change), at game levels. milena2.mp4: five Milena auditions of one line, alone, then after Inez's line. Takes picked by the machine listener (names mapped). Writes briefs/090/larthi.* and milena2.*, and r3.json."""
import json, subprocess, sys
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE)); sys.path.insert(0, str(REPO / 'briefs/087/chime'))
import audiolib as A, reel as R, lines as LN, b2 as B, voice_measure as VM
S = REPO / 'out/scratch/chime'; SR = 48000; C = json.loads((HERE / 'cast2.json').read_text())
LL = [('scene10', LN.SCENE[9][1], -23, False), ('shout05', LN.SHOUTS[4][1], -14, True), ('shout06', LN.SHOUTS[5][1], -14, True), ('name05', LN.NAMES[4][1], -23, False)]


def best():
    b = {}
    for x in json.loads((S / 'asr_r3c.json').read_text()): b.setdefault((x['k'], x['stem']), []).append((B.wer(x['line'], x['heard']), x['seed'], x['wav']))
    return {k: sorted(v)[0] for k, v in b.items()}


def write(name, segs, audio):
    a = np.concatenate(audio); a = a * min(1.0, 10 ** (-1 / 20) / np.abs(a).max()); w = R.TMP / f'{name}.wav'; A.write_wav(w, a, SR)
    lst = R.TMP / f'cards-{name}.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d:.3f}\n" for p, d in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(w), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / f'briefs/090/{name}.mp3')], check=True)
    v = R.TMP / f'{name}v.mp4'; subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(w), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-movflags', '+faststart', str(v)], check=True, cwd=str(R.TMP))
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(v), '-t', f'{len(a) / SR:.3f}', '-c', 'copy', str(REPO / f'briefs/090/{name}.mp4')], check=True); return round(len(a) / SR, 1)


def main():
    P = best(); res = {'larthi': {}, 'milena': {}}; N = [0]
    def mk(prefix, segs, audio):
        def seg(h, t, sub, a, foot=''):
            p = R.TMP / f'{prefix}{N[0]:03d}.png'; N[0] += 1; R.card(p, h, t, sub, None, foot); segs.append((p, len(a) / SR)); audio.append(a)
        return seg
    segs, audio = [], []; seg = mk('lar', segs, audio)
    seg('Larthi', 'Three Larthis', 'Young, energetic, chatty, clear. Her current lines: Fable is rewriting them, so the words will change. Game levels.', np.concatenate([R.sil(.3), R.speak('Three Larthis, on her current lines. Her words are about to change.'), R.sil(.4)]), foot='Chime cannot hear these: your ears decide.')
    for n, k in enumerate(('l1', 'l2', 'l4'), 1):
        lab = C['larthi_options'][k]['label']; seg(f'Larthi {n}', f'Larthi {n}: {lab}', C['larthi_options'][k]['describe'], np.concatenate([R.sil(.3), R.speak(f'Larthi {n}.'), R.sil(.3)]))
        res['larthi'][str(n)] = {'id': k, 'label': lab, 'describe': C['larthi_options'][k]['describe'], 'lines': {}}
        for stem, line, lu, mom in LL:
            wer, seed, wav = P[(k, stem)]; seg(f'Larthi {n}', line, lab, np.concatenate([B.clip(wav, lu, mom), R.sil(.5)])); m = VM.row(Path(wav)); res['larthi'][str(n)]['lines'][stem] = {'word_error': round(wer, 3), 'pitch_hz': m['f0_median'], 'speaking_s': m['speaking_s']}
    res['larthi_seconds'] = write('larthi', segs, audio)
    segs, audio = [], []; seg = mk('mi2', segs, audio); inez = B.clip(B.pick()['scene08_inez'][2], -23)
    seg('Milena', 'Five Milenas', 'One line each, alone, then after Inez. Most in their twenties, like her figure. Game level.', np.concatenate([R.sil(.3), R.speak('Five Milena auditions. One line each, alone, then after Inez.'), R.sil(.4)]), foot='Chime cannot hear these: your ears decide.')
    for n, k in enumerate(('a1', 'a2', 'a3', 'a4', 'a5'), 1):
        lab = C['milena_auditions'][k]['label']; wer, seed, wav = P[(k, 'aud')]; y = B.clip(wav, -23); m = VM.row(Path(wav))
        res['milena'][str(n)] = {'id': k, 'label': lab, 'describe': C['milena_auditions'][k]['describe'], 'word_error': round(wer, 3), 'pitch_hz': m['f0_median']}
        seg(f'Milena {n}', f'Milena {n}: {lab}', C['milena_auditions'][k]['describe'], np.concatenate([R.sil(.3), R.speak(f'Milena {n}.'), R.sil(.3), y, R.sil(.8)]))
        seg(f'Milena {n}', 'After Inez', LN.SCENE[7][1] + ' / ' + LN.SCENE[8][1], np.concatenate([inez, R.sil(.35), y, R.sil(.8)]))
    res['milena_seconds'] = write('milena2', segs, audio); res['picks'] = json.loads((S / 'r3_pick.json').read_text())
    (REPO / 'briefs/090/r3.json').write_text(json.dumps(res, indent=1, ensure_ascii=False) + '\n'); print(json.dumps(res, indent=0)[:3000])


if __name__ == '__main__': main()
