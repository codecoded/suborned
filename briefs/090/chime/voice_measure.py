"""Brief 090 (Chime): what can be measured of a voice clip: length, loudness, peak, median pitch and its spread (autocorrelation, 60-400 Hz, voiced frames only), speaking time (frames above -35 dB of peak), spectral centroid and the share above 4 kHz (hiss, sibilance). Nothing here says a voice sounds right. Args: wavs."""
import sys, json
from pathlib import Path
import numpy as np
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / '087/chime'))
import audiolib as A


def f0(m, sr):
    n = int(.04 * sr); h = int(.01 * sr); out = []
    for i in range(0, len(m) - n, h):
        w = m[i:i + n] * np.hanning(n); e = (w ** 2).mean()
        if e < 1e-6: continue
        F = np.fft.rfft(w, 2 * n); ac = np.fft.irfft(np.abs(F) ** 2)[:n]; a, b = int(sr / 400), int(sr / 60); k = a + int(np.argmax(ac[a:b])); s = ac[k] / (ac[0] + 1e-12)
        if s > .5: out.append(sr / k)
    return np.array(out)


def row(p):
    x, sr = A.read_wav(p); m = x.mean(axis=1); f = f0(m, sr); h = int(.02 * sr); e = 20 * np.log10(np.sqrt(np.convolve(m ** 2, np.ones(h) / h, 'same'))[::h] + 1e-9); b, cen = A.bands(x, sr)
    return dict(file=Path(p).name, seconds=round(len(m) / sr, 2), rate=sr, speaking_s=round(float((e > e.max() - 35).sum() * .02), 2), lufs_i=round(A.lufs_i(x, sr), 1), lufs_m=round(A.lufs_m(x, sr), 1), true_peak=round(A.true_peak(x), 1),
                f0_median=round(float(np.median(f)), 1) if len(f) else None, f0_spread_semitones=round(float(12 * np.log2(np.percentile(f, 90) / np.percentile(f, 10))), 1) if len(f) > 5 else None, voiced_share=round(len(f) / max(1, len(m) / (.01 * sr)), 2), centroid_hz=cen, above_4k_db=b[3])


if __name__ == '__main__':
    for a in sys.argv[1:]: print(json.dumps(row(a)))
