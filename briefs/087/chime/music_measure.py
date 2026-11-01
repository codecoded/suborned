"""Brief 087 (Chime): what can be measured of a music take: length, loudness, swing, peak, bands, tempo against the stated tempo (per 12 s window, so drift shows), key against the stated key (Krumhansl profiles on a chroma), and how steady the texture is. Nothing here says it sounds good. Args: table.json takes..."""
import json, sys
from pathlib import Path
import numpy as np
from scipy.signal import stft
sys.path.insert(0, str(Path(__file__).resolve().parent))
import audiolib as A
KS = {'major': [6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88], 'minor': [6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17]}
NOTES = 'C C# D D# E F F# G G# A A# B'.split()


def flux(m, sr):
    f, t, S = stft(m, sr, nperseg=1024, noverlap=768); M = np.log1p(np.abs(S) * 50); d = np.maximum(0, np.diff(M, axis=1)).sum(axis=0); return d - np.convolve(d, np.ones(40) / 40, 'same'), sr / 256


def tempo(env, rate, lo=50, hi=200):
    ac = np.correlate(env, env, 'full')[len(env) - 1:]; a, b = int(rate * 60 / hi), int(rate * 60 / lo); i = a + int(np.argmax(ac[a:b])); return 60 * rate / i


def fold(b, ref):
    """Tempo to the stated tempo's octave (half or double time count as the same pulse)."""
    while b < ref / 1.42: b *= 2
    while b > ref * 1.42: b /= 2
    return b


def key(m, sr):
    f, _, S = stft(m, sr, nperseg=8192); P = (np.abs(S) ** 2).mean(axis=1); ch = np.zeros(12); k = (f > 60) & (f < 2000); pc = np.round(12 * np.log2(f[k] / 261.626)).astype(int) % 12
    np.add.at(ch, pc, P[k]); best = []
    for mode, prof in KS.items():
        for r in range(12): best.append((float(np.corrcoef(ch, np.roll(prof, r))[0, 1]), f'{NOTES[r]} {mode}'))
    return sorted(best, reverse=True)[:2]


def row(p, stated_bpm=None):
    x, sr = A.read_wav(p); m = x.mean(axis=1); L = [A.lufs_i(x[i:i + 4 * sr], sr) for i in range(0, max(1, len(x) - 4 * sr), 2 * sr)]; e, r = flux(m, sr); w = int(12 * r); loc = [tempo(e[i:i + w], r) for i in range(0, max(1, len(e) - w), w)]
    b, cen = A.bands(x, sr); out = {'seconds': round(len(x) / sr, 1), 'rate': sr, 'channels': x.shape[1], 'lufs_i': round(A.lufs_i(x, sr), 1), 'swing_4s_lu': round(max(L) - min(L), 1), 'true_peak': round(A.true_peak(x), 1), 'bands_lt200_lt1k_lt4k_hi': b, 'centroid_hz': cen, 'tempo_whole': round(tempo(e, r), 1), 'key_best_two': [f'{k} {c:.2f}' for c, k in key(m, sr)]}
    if stated_bpm: out['tempo_12s_windows_folded'] = [round(fold(v, stated_bpm), 1) for v in loc]; out['tempo_within_2pct'] = round(float(np.mean([abs(fold(v, stated_bpm) - stated_bpm) / stated_bpm < .02 for v in loc])), 2); out['stated_bpm'] = stated_bpm
    tail = x[int(-2 * sr):]; out['last_2s_vs_whole_lu'] = round(A.lufs_i(tail, sr) - A.lufs_i(x, sr), 1); return out


if __name__ == '__main__':
    tbl = {k: v for k, v in json.loads(Path(sys.argv[1]).read_text()).items()}
    for f in sys.argv[2:]:
        n = Path(f).stem; pid = '_'.join(n.split('_')[:-2]); print(n, json.dumps(row(f, tbl.get(pid))))
