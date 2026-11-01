"""Brief 087 (Chime): a picture of takes, since I can't hear them: waveform over log-frequency spectrogram (30 Hz-20 kHz, grid at 100 Hz, 1 kHz, 10 kHz), 1 s ticks. Args: out.png wavs..."""
import sys, wave, numpy as np
from pathlib import Path
from scipy.signal import stft
from PIL import Image, ImageDraw, ImageFont
W, HW, HS = 460, 60, 150


def read(p):
    with wave.open(str(p)) as w:
        b = w.readframes(w.getnframes()); ch, sr = w.getnchannels(), w.getframerate()
    x = np.frombuffer(b, '<i2').astype(float) / 32768
    return x.reshape(-1, ch).mean(axis=1), sr


def tile(p):
    x, sr = read(p); im = Image.new('RGB', (W, HW + HS + 16), (15, 15, 20)); d = ImageDraw.Draw(im)
    idx = np.linspace(0, len(x), W + 1).astype(int)
    for i in range(W):
        s = x[idx[i]:max(idx[i] + 1, idx[i + 1])]; a, b = s.min(), s.max(); d.line([(i, 16 + HW / 2 - b * HW / 2), (i, 16 + HW / 2 - a * HW / 2)], fill=(220, 220, 220))
    f, t, S = stft(x, sr, nperseg=1024, noverlap=768); P = 10 * np.log10(np.abs(S) ** 2 + 1e-10)
    fy = np.geomspace(30, 20000, HS); fi = np.clip(np.searchsorted(f, fy), 0, len(f) - 1); ti = np.clip((np.arange(W) / W * P.shape[1]).astype(int), 0, P.shape[1] - 1)
    G = P[fi][:, ti]; G = np.clip((G + 90) / 70, 0, 1)[::-1]
    rgb = np.stack([G ** .7 * 255, G ** 1.6 * 255, (1 - G) * G * 200], axis=2).astype(np.uint8); im.paste(Image.fromarray(rgb), (0, 16 + HW))
    for hz in (100, 1000, 10000):
        y = 16 + HW + HS - int(np.log(hz / 30) / np.log(20000 / 30) * HS); d.line([(0, y), (W, y)], fill=(90, 160, 200)); d.text((W - 26, y - 11), f'{hz // 1000}k' if hz >= 1000 else '100', fill=(120, 190, 230))
    dur = len(x) / sr
    for s in np.arange(0, dur, 1.0 if dur > 1.5 else 0.25):
        px = int(s / dur * W); d.line([(px, 16), (px, 16 + HW)], fill=(80, 80, 110))
    d.text((3, 2), f'{Path(p).stem}  {dur:.2f}s', fill=(255, 230, 90))
    return im


def main(a):
    tiles = [tile(p) for p in a[1:]]; cols = 4; rows = (len(tiles) + cols - 1) // cols; h = tiles[0].height
    o = Image.new('RGB', (cols * (W + 6), rows * (h + 6)), (0, 0, 0))
    for i, t in enumerate(tiles): o.paste(t, ((i % cols) * (W + 6), (i // cols) * (h + 6)))
    o.save(a[0])


main(sys.argv[1:])
