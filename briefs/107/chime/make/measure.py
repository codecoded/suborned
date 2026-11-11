"""Brief 107 (Chime): what I can check without ears. numpy + scipy; the shared sums come from briefs/087/chime/audiolib.py.
Run with the scratch venv: out/scratch/chime/venv/bin/python -I briefs/107/chime/make/measure.py <wav>...  (prints one row per take)"""
import sys
from pathlib import Path
import numpy as np
from scipy.signal import stft, find_peaks
REPO = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(REPO / 'briefs/087/chime'))
import audiolib as A


def window_lufs(x, sr, win=4.0, hop=1.0):
    """Loudness (LUFS) of each 4 s window, 1 s apart: the bed's swing is max - min (087's street_bed swung 9.5 LU; the evened one 2.1)."""
    n, h = int(win * sr), int(hop * sr); out = []
    for i in range(0, max(1, len(x) - n + 1), h): out.append(A.lufs_i(x[i:i + n], sr))
    return np.array(out)


def band_events(x, sr, lo, hi, over_db, win=0.5):
    """Half-second windows where the band lo-hi Hz rises over_db above its median: a rumble, a gust, a clink. Returns their start times (s)."""
    m = x.reshape(len(x), -1).mean(axis=1); f, t, S = stft(m, sr, nperseg=2048, noverlap=1024); P = (np.abs(S) ** 2)[(f >= lo) & (f < hi)].sum(axis=0)
    k = max(1, int(win / (t[1] - t[0]))); nb = len(P) // k; e = 10 * np.log10(P[:nb * k].reshape(nb, k).mean(axis=1) + 1e-20)
    return [round(i * win, 1) for i in np.where(e > np.median(e) + over_db)[0]]


def tonal(x, sr, lo=250, hi=3000):
    """Share of the time a narrow line stands 12 dB over its neighbours between lo and hi Hz: a hum is steady, a tune or a voice moves. Returns (share, distinct pitches seen)."""
    m = x.reshape(len(x), -1).mean(axis=1); f, t, S = stft(m, sr, nperseg=4096, noverlap=2048); P = 10 * np.log10(np.abs(S) ** 2 + 1e-14)
    sel = np.where((f >= lo) & (f < hi))[0]; hits = 0; pitches = set()
    for j in range(P.shape[1]):
        col = P[:, j]
        for i in sel:
            nb = np.r_[col[max(0, i - 15):i - 2], col[i + 3:i + 16]]
            if col[i] > np.median(nb) + 12 and col[i] >= col[i - 1] and col[i] >= col[i + 1]: hits += 1; pitches.add(int(round(12 * np.log2(f[i] / 440))))  # semitone bins
    return round(hits / P.shape[1], 2), len(pitches)


def steps(x, sr, min_gap=0.25):
    """Footstep onsets: peaks of the 5 ms envelope above 1 kHz-weighted level, at least min_gap apart and 15 dB over the floor."""
    m = A.hp(x.reshape(len(x), -1).mean(axis=1), 150, sr); h = int(0.005 * sr); e = 20 * np.log10(np.sqrt(np.convolve(m ** 2, np.ones(h) / h, 'same')) + 1e-9)
    floor = np.percentile(e, 30); pk, _ = find_peaks(e, height=floor + 15, distance=int(min_gap * sr), prominence=10)
    on = []
    for p in pk:  # walk back to where the rise starts (10 % of the peak's amplitude)
        i = p
        while i > 0 and e[i] > e[p] - 20 and p - i < int(0.05 * sr): i -= 1
        on.append(i)
    return np.array(on), floor


def row(p):
    x, sr = A.read_wav(p); w = window_lufs(x, sr); tn = tonal(x, sr)
    r = {'take': Path(p).name, 'seconds': round(len(x) / sr, 2), 'lufs_i': round(A.lufs_i(x, sr), 1), 'swing_4s_lu': round(float(w.max() - w.min()), 1),
         'low_events_s': band_events(x, sr, 20, 150, 8), 'mid_events_s': band_events(x, sr, 150, 2000, 8), 'high_events_s': band_events(x, sr, 2000, 12000, 8),
         'tonal_share': tn[0], 'tonal_pitches': tn[1], 'bands': A.bands(x, sr)[0], 'centroid_hz': A.bands(x, sr)[1]}
    return r


if __name__ == '__main__':
    for a in sys.argv[1:]: print(row(a))
