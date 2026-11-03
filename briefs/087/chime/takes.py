"""Brief 087 (Chime): numbers for raw takes (run with the scratch venv's python -I; numpy + scipy). Args: wav files. Prints one row each: length, peak, RMS, attack, onsets, centroid, band shares, active span."""
import sys, wave, numpy as np
from pathlib import Path
from scipy.signal import stft, find_peaks


def read(p):
    with wave.open(str(p)) as w:
        n, ch, sw, sr = w.getnframes(), w.getnchannels(), w.getsampwidth(), w.getframerate()
        b = w.readframes(n)
    x = np.frombuffer(b, {2: '<i2', 4: '<i4'}.get(sw, '<i2')).astype(np.float64) / (2 ** (8 * sw - 1))
    return x.reshape(-1, ch), sr


def row(p):
    x, sr = read(p); m = x.mean(axis=1)
    pk = 20 * np.log10(np.abs(x).max() + 1e-9); rms = 20 * np.log10(np.sqrt((m ** 2).mean()) + 1e-9)
    h = int(0.005 * sr); env = np.sqrt(np.convolve(m ** 2, np.ones(h) / h, 'same') + 1e-12); ed = 20 * np.log10(env)
    top = ed.max(); on = np.where(ed > top - 30)[0]; start, end = on[0] / sr, on[-1] / sr
    tpk = ed.argmax() / sr; pks, _ = find_peaks(ed, height=top - 12, distance=int(0.08 * sr), prominence=8)
    f, _, S = stft(m, sr, nperseg=2048); P = (np.abs(S) ** 2).mean(axis=1); c = (f * P).sum() / P.sum()
    bands = [(0, 200), (200, 1000), (1000, 4000), (4000, 24000)]; sh = [10 * np.log10(P[(f >= a) & (f < b)].sum() / P.sum() + 1e-12) for a, b in bands]
    print(f"{p.name:26} {len(m)/sr:5.2f}s {sr} {x.shape[1]}ch pk {pk:6.1f} rms {rms:6.1f} act {start:4.2f}-{end:4.2f} peak@{tpk:4.2f} on {len(pks):2d} cen {c:5.0f} bands {' '.join(f'{v:5.0f}' for v in sh)}")


for a in sys.argv[1:]:
    row(Path(a))
