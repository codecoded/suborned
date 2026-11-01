"""Brief 087 (Chime): the four picked sketches: cut to whole bars, measured, with a labelled reel for Sky's phone. Writes briefs/087/music/*.mp3, briefs/087/music-sketches.mp3 + .mp4, briefs/087/music-measures.json. Run from the repo root with the scratch venv's python -I."""
import json, subprocess, sys
from pathlib import Path
import numpy as np
from scipy.signal import stft
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE))
import audiolib as A, music_measure as MM, reel as R
T = REPO / 'paid/087-music/takes'; OUT = REPO / 'briefs/087/music'; OUT.mkdir(exist_ok=True); SR = 48000
PICKS = [  # id, take, style, stated bpm, key, what it would carry
    ('lounge_spy', 'lounge_spy_ace_t4', 'Lounge-spy', 112, 'D minor', 'Vibraphone, walking bass, brushes, muted trumpet, theremin'),
    ('analog_theremin', 'analog_theremin_ace_t1', 'Analog synth and theremin', 84, 'A minor', 'Warm pads, pulsing bass, theremin; the teaser\'s family'),
    ('big_band_tension', 'big_band_tension_ace_t2', 'Big-band tension', 132, 'C minor', 'Muted brass stabs, low saxes, walking bass, snare and ride'),
    ('civic_clockwork', 'civic_clockwork_ace_t2', 'Civic clockwork (my pick)', 96, 'F major', 'A public-information film: celesta, bells, woodblock tick, pizzicato, and a low tuba note that never resolves'),
]


def bar_repeat(x, sr, bpm):
    """How alike the bars are, by band (0-1, mean correlation of each bar's level contour with the next one's): loops and layers want this high."""
    m = x.mean(axis=1); f, t, S = stft(m, sr, nperseg=2048, noverlap=1536); P = np.abs(S) ** 2; bar = 4 * 60 / bpm; n = int(bar / (t[1] - t[0])); out = {}
    for name, lo, hi in (('low_lt200', 0, 200), ('mid_200_1k', 200, 1000), ('high_gt1k', 1000, 20000)):
        e = np.log(P[(f >= lo) & (f < hi)].sum(axis=0) + 1e-9); nb = len(e) // n; b = e[:nb * n].reshape(nb, n); c = [np.corrcoef(b[i], b[i + 1])[0, 1] for i in range(nb - 1)]
        out[name] = {'repeat': round(float(np.nanmean(c)), 2), 'level_steadiness_db': round(float(np.std(b.mean(axis=1)) * 4.34), 1)}
    return out


def main():
    res = {'note': 'Measures only; Chime cannot hear. Tempo reads run about 1 % high against ACE-Step takes with an exact stated tempo, so treat small gaps as noise.', 'pieces': []}; audio = []; segs = []; N = 0
    def seg(head, title, sub, a, foot=''):
        nonlocal N
        p = R.TMP / f'mus{N:03d}.png'; N += 1; R.card(p, head, title, sub, None, foot); segs.append((p, len(a) / SR)); audio.append(a)
    seg('Brief 087', 'Four soundtrack sketches', 'Style and tempo named, then about a minute each. Nothing is wired. Instrumental, made to be split into Alert layers later.', np.concatenate([R.sil(.3), R.speak('Four soundtrack sketches. Each one is named, then plays for about a minute.'), R.sil(.5)]), foot='Chime cannot hear these: your ears decide.')
    for i, (id, take, style, bpm, key, what) in enumerate(PICKS, 1):
        x, sr = A.read_wav(T / f'{take}.wav'); x = A.resample(x, sr, SR); m = np.abs(x).max(axis=1); h = int(.05 * SR); e = np.sqrt(np.convolve(m ** 2, np.ones(h) / h, 'same')); on = np.where(e > e.max() * .03)[0]; a, b = on[0], on[-1]; bar = 4 * 60 / bpm
        bars = int((b - a) / SR / bar); bars = min(bars, int(60 / bar)); y = x[a:a + int(bars * bar * SR)]; y = A.fade(y, SR, .01, .6); g = -23 - A.lufs_i(y, SR); y = y * 10 ** (g / 20); wav = R.TMP / f'mus_{id}.wav'; A.write_wav(wav, y, SR)
        mp = OUT / f'{id}.mp3'; subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(wav), '-c:a', 'libmp3lame', '-b:a', '160k', str(mp)], check=True)
        r = MM.row(wav, bpm); r.update({'id': id, 'style': style, 'stated_key': key, 'bars': bars, 'source_take': take + '.wav', 'bar_repeat': bar_repeat(y, SR, bpm), 'mp3': str(mp.relative_to(REPO))}); res['pieces'].append(r); print(id, json.dumps(r)[:620])
        tempo_say = f'{bpm} beats a minute, {key}'
        seg(f'{i} of 4', style, f'{bpm} BPM stated, {key}. {what}.', np.concatenate([R.speak(f'{i}. {style.replace(" (my pick)", "")}. {tempo_say}.'), R.sil(.4)]), foot=id)
        seg(f'{i} of 4', style, f'{bpm} BPM stated, {key}. {what}.', np.concatenate([y, R.sil(.8)]), foot=f'{id}, measures about {round(r["tempo_12s_windows_folded"][0])} BPM')
    a = np.concatenate(audio); a = a * 10 ** (4 / 20); a = a * min(1.0, 10 ** (-1 / 20) / np.abs(a).max()); wav = R.TMP / 'music.wav'; A.write_wav(wav, a, SR); print(f'reel {len(a) / SR:.1f} s, peak {A.sample_peak(a):.1f} dBFS, LUFS-I {A.lufs_i(a, SR):.1f}')
    lst = R.TMP / 'cards-mus.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d:.3f}\n" for p, d in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(wav), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / 'briefs/087/music-sketches.mp3')], check=True)
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(wav), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-shortest', '-movflags', '+faststart', str(REPO / 'briefs/087/music-sketches.mp4')], check=True, cwd=str(R.TMP))
    (REPO / 'briefs/087/music-measures.json').write_text(json.dumps(res, indent=1, ensure_ascii=False) + '\n')


main()
