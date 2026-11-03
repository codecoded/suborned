"""Brief 087 (Chime): the sums shared by build.py, mix.py and reel.py. numpy + scipy only; run with the scratch venv's python -I."""
import wave
import numpy as np
from scipy.signal import butter, lfilter, resample_poly, sosfilt, stft
from math import gcd


def read_wav(p):
    with wave.open(str(p)) as w:
        ch, sw, sr = w.getnchannels(), w.getsampwidth(), w.getframerate(); b = w.readframes(w.getnframes())
    if sw == 2: x = np.frombuffer(b, '<i2').astype(np.float64) / 32768
    elif sw == 3:
        a = np.frombuffer(b, np.uint8).reshape(-1, 3); x = (a[:, 0].astype(np.int32) | a[:, 1].astype(np.int32) << 8 | a[:, 2].astype(np.int32) << 16); x = np.where(x >= 2 ** 23, x - 2 ** 24, x) / 2 ** 23
    else: x = np.frombuffer(b, '<i4').astype(np.float64) / 2 ** 31
    return x.reshape(-1, ch), sr


def write_wav(p, x, sr):
    x = x.reshape(len(x), -1); y = np.clip(np.round(x * 32767), -32768, 32767).astype('<i2')
    with wave.open(str(p), 'wb') as w:
        w.setnchannels(x.shape[1]); w.setsampwidth(2); w.setframerate(sr); w.writeframes(y.tobytes())


def resample(x, a, b):
    if a == b: return x
    g = gcd(a, b); return resample_poly(x, b // g, a // g, axis=0)


def k_weight(x, sr):
    """ITU-R BS.1770 K-weighting at any rate (the two biquads, recomputed for sr)."""
    f0, g, q = 1681.974450955533, 3.999843853973347, 0.7071752369554196; k = np.tan(np.pi * f0 / sr); vh = 10 ** (g / 20); vb = vh ** 0.4996667741545416; a0 = 1 + k / q + k * k
    b1 = [(vh + vb * k / q + k * k) / a0, 2 * (k * k - vh) / a0, (vh - vb * k / q + k * k) / a0]; a1 = [1, 2 * (k * k - 1) / a0, (1 - k / q + k * k) / a0]
    f1, q1 = 38.13547087602444, 0.5003270373238773; k = np.tan(np.pi * f1 / sr); a0 = 1 + k / q1 + k * k
    b2 = [1, -2, 1]; a2 = [1, 2 * (k * k - 1) / a0, (1 - k / q1 + k * k) / a0]
    return lfilter(b2, a2, lfilter(b1, a1, x, axis=0), axis=0)


def _blocks(x, sr, win=0.4, hop=0.1):
    """Mean-square per channel summed, over sliding windows; a short clip is padded so its window exists."""
    n, h = int(win * sr), int(hop * sr)
    if len(x) < n: x = np.vstack([x, np.zeros((n - len(x), x.shape[1]))])
    y = k_weight(x, sr); e = np.concatenate([np.zeros((1, y.shape[1])), np.cumsum(y ** 2, axis=0)])
    idx = np.arange(0, len(y) - n + 1, h)
    return ((e[idx + n] - e[idx]) / n).sum(axis=1)


def lufs_m(x, sr):
    """Momentary maximum (LUFS): the loudest 400 ms."""
    return float(-0.691 + 10 * np.log10(_blocks(x.reshape(len(x), -1), sr).max() + 1e-14))


def lufs_i(x, sr):
    """Integrated (BS.1770-4 gating)."""
    b = _blocks(x.reshape(len(x), -1), sr); l = -0.691 + 10 * np.log10(b + 1e-14); b1 = b[l > -70]
    if not len(b1): return -70.0
    g = -0.691 + 10 * np.log10(b1.mean()) - 10; b2 = b1[-0.691 + 10 * np.log10(b1) > g]
    return float(-0.691 + 10 * np.log10(b2.mean()))


def true_peak(x):
    x = x.reshape(len(x), -1); return float(20 * np.log10(np.abs(resample_poly(x, 4, 1, axis=0)).max() + 1e-12))


def sample_peak(x):
    return float(20 * np.log10(np.abs(x).max() + 1e-12))


def bands(x, sr):
    """Spectral balance: share of the energy (dB) below 200 Hz, 200-1 k, 1-4 k, above 4 k; and the centroid."""
    m = x.reshape(len(x), -1).mean(axis=1); f, _, S = stft(m, sr, nperseg=min(2048, len(m)))
    P = (np.abs(S) ** 2).mean(axis=1); t = P.sum() + 1e-20
    sh = [10 * np.log10(P[(f >= a) & (f < b)].sum() / t + 1e-12) for a, b in ((0, 200), (200, 1000), (1000, 4000), (4000, 1e6))]
    return [round(float(v), 1) for v in sh], round(float((f * P).sum() / t))


def third_octaves(x, sr):
    """Band levels (dB, power per second of signal), 1/3-octave centres 63 Hz to 12.5 kHz."""
    m = x.reshape(len(x), -1).mean(axis=1); cs = 63 * 2 ** (np.arange(0, 24) / 3); F = np.abs(np.fft.rfft(m)) ** 2 / max(1, len(m)) ** 2 * 2; f = np.fft.rfftfreq(len(m), 1 / sr)
    return cs, np.array([10 * np.log10(F[(f >= c / 2 ** (1 / 6)) & (f < c * 2 ** (1 / 6))].sum() + 1e-14) for c in cs])


def loudest(x, sr, s=0.1):
    """The loudest s seconds of a clip: where a cue must clear what is under it."""
    m = x.reshape(len(x), -1); n = min(len(m), int(s * sr)); e = np.cumsum((m ** 2).sum(axis=1)); e = np.concatenate([[0], e]); i = int(np.argmax(e[n:] - e[:-n])); return m[i:i + n]


def clear_of(cue, cue_sr, bed, bed_sr, gain_db=0.0):
    """Masking test: the cue's loudest 100 ms against the bed's average, in 1/3-octave bands: margin in dB in the cue's strongest three bands, and how many bands clear by 6 dB."""
    c = loudest(cue, cue_sr); _, a = third_octaves(c, cue_sr); a = a + gain_db; _, b = third_octaves(bed, bed_sr)
    top = np.argsort(a)[-3:]; cs = 63 * 2 ** (np.arange(0, 24) / 3)
    return {'top_bands_hz': [round(float(cs[i])) for i in sorted(top)], 'margin_top3_db': round(float((a[top] - b[top]).min()), 1), 'bands_clear_6db': int(((a - b) >= 6).sum()), 'loudness_margin_lu': None}


def seam(x, sr):
    """A loop's end into its start: the sample step against the median step, and the 20 ms level jump against the clip's typical 20 ms jump."""
    m = x.reshape(len(x), -1).mean(axis=1); d = np.abs(np.diff(m)); med = float(np.median(d) + 1e-12); ratio = float(abs(m[0] - m[-1]) / med)
    n = int(0.02 * sr); nb = len(m) // n; r = 20 * np.log10(np.sqrt((m[:nb * n].reshape(nb, n) ** 2).mean(axis=1)) + 1e-9); j = np.abs(np.diff(r))
    jump = abs(r[0] - r[-1]); return {'step_percentile': round(float((d < abs(m[0] - m[-1])).mean() * 100), 1), 'step_ratio': round(ratio, 2), 'level_jump_db': round(float(jump), 2), 'typical_jump_db_p95': round(float(np.percentile(j, 95)), 2)}


def hp(x, hz, sr, order=2): return sosfilt(butter(order, hz, 'hp', fs=sr, output='sos'), x, axis=0)
def lp(x, hz, sr, order=2): return sosfilt(butter(order, hz, 'lp', fs=sr, output='sos'), x, axis=0)


def fade(x, sr, fin=0.0, out=0.0):
    x = x.copy(); n = len(x)
    if fin > 0: k = min(n, int(fin * sr)); x[:k] *= (np.sin(np.linspace(0, np.pi / 2, k)) ** 2).reshape(-1, *([1] * (x.ndim - 1)))
    if out > 0: k = min(n, int(out * sr)); x[-k:] *= (np.cos(np.linspace(0, np.pi / 2, k)) ** 2).reshape(-1, *([1] * (x.ndim - 1)))
    return x


def join(a, b, sr, xf=0.015):
    """Crossfade b onto a's end, equal power."""
    n = int(xf * sr); s = np.sin(np.linspace(0, np.pi / 2, n)).reshape(-1, *([1] * (a.ndim - 1))); c = np.cos(np.linspace(0, np.pi / 2, n)).reshape(-1, *([1] * (a.ndim - 1)))
    return np.concatenate([a[:-n], a[-n:] * c + b[:n] * s, b[n:]])


def loop(x, sr, xf):
    """Seamless: the tail crossfades, equal power, into the head; the result is xf shorter."""
    n = int(xf * sr); s = np.sin(np.linspace(0, np.pi / 2, n)).reshape(-1, *([1] * (x.ndim - 1))); c = np.cos(np.linspace(0, np.pi / 2, n)).reshape(-1, *([1] * (x.ndim - 1)))
    y = x[:-n].copy(); y[:n] = x[-n:] * c + x[:n] * s; return y


def period(x, sr, lo=0.2, hi=1.2):
    """The step period (s) of a walking loop: the highest autocorrelation of the short-term level above 1 kHz (the ticks) between lo and hi."""
    m = hp(x.reshape(len(x), -1).mean(axis=1), 1000, sr); h = int(0.005 * sr); e = np.abs(m); e = np.convolve(e, np.ones(h) / h, 'same')[::h]; e = e - e.mean()
    ac = np.correlate(e, e, 'full')[len(e) - 1:]; a, b = int(lo * sr / h), int(hi * sr / h); return float((a + int(np.argmax(ac[a:b]))) * h / sr), float(ac[a:b].max() / (ac[0] + 1e-12))
