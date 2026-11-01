"""Brief 087 (Chime): is a short cue voice-like? A duck's quack is a buzz: a pulse train at a voice's rate, shaped by nasal mid-band formants, falling in pitch. Measures the three: pulse strength and rate (autocorrelation of the waveform, 60-500 Hz), mid-band share and its narrow peaks (500-3000 Hz), and pitch glide (the pulse rate's trend through the clip). Also length, decay. Args: wavs."""
import sys
from pathlib import Path
import numpy as np
sys.path.insert(0, str(Path(__file__).resolve().parent))
import audiolib as A


def pulse(m, sr):
    """(strength 0-1, rate Hz): the highest normalised autocorrelation between 2 ms and 16 ms."""
    m = m - m.mean(); n = len(m)
    if n < int(.02 * sr): return 0.0, 0.0
    F = np.fft.rfft(m * np.hanning(n), 2 * n); ac = np.fft.irfft(np.abs(F) ** 2)[:n]; a, b = int(sr / 500), min(int(sr / 60), n - 1); i = a + int(np.argmax(ac[a:b])); return float(ac[i] / (ac[0] + 1e-12)), sr / i


def row(p):
    x, sr = A.read_wav(p); m = x.mean(axis=1); h = int(.002 * sr); e = np.sqrt(np.convolve(m ** 2, np.ones(h) / h, 'same')); pk = int(e.argmax()); on = np.where(e > e.max() * .1)[0]; dur = (on[-1] - on[0]) / sr
    w = m[on[0]:on[-1] + 1]; st, rate = pulse(w, sr); seg = int(.03 * sr); rates = []
    for i in range(0, max(1, len(w) - seg), seg // 2):
        s, r = pulse(w[i:i + seg], sr)
        if s > .3: rates.append(r)
    glide = 12 * np.log2(np.median(rates[-3:]) / np.median(rates[:3])) if len(rates) >= 6 else 0.0
    F = np.abs(np.fft.rfft(w * np.hanning(len(w)), 1 << 15)) ** 2; f = np.fft.rfftfreq(1 << 15, 1 / sr); t = F.sum() + 1e-20; mid = 10 * np.log10(F[(f >= 500) & (f < 3000)].sum() / t + 1e-12); lo2 = F[(f >= 500) & (f < 3000)]; fm = f[(f >= 500) & (f < 3000)]; top = fm[np.argmax(lo2)]
    dec = np.where(e[pk:] < e[pk] * 10 ** (-20 / 20))[0]; b, cen = A.bands(x, sr)
    return dict(name=Path(p).stem, active_s=round(dur, 3), decay20_ms=round(float(dec[0] / sr * 1000), 1) if len(dec) else None, attack_ms=round((pk - on[0]) / sr * 1000, 1), pulse_strength=round(st, 2), pulse_hz=round(rate), voiced_windows=len(rates), glide_semitones=round(float(glide), 1), mid_500_3k_share_db=round(float(mid), 1), mid_top_hz=round(float(top)), centroid_hz=cen, bands=b, peak_db=round(A.sample_peak(x), 1))


if __name__ == '__main__':
    for a in sys.argv[1:]: print(row(a))
