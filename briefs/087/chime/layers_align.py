"""Brief 087 (Chime): do the arrangements stay on the base's beat? For each take: per-4-bar lag of the onset envelope against the base (best within ±250 ms, with its correlation), key held, chroma against the base, tempo, and bands. Run from the repo root with the scratch venv's python -I; args: wavs."""
import json, sys
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE))
import audiolib as A, music_measure as MM, layers_measure as LM
L = REPO / 'out/scratch/chime/layers'; BAR = 4 * 60 / 86


def onset(x, sr): e, r = MM.flux(x.mean(axis=1), sr); return e, r


def align(x, sr, b, bsr):
    if sr != bsr: x = A.resample(x, sr, bsr); sr = bsr
    n = min(len(x), len(b)); e1, r = onset(x[:n], sr); e2, _ = onset(b[:n], sr); w = int(4 * BAR * r); out = []; mx = int(.25 * r)
    for i in range(mx, len(e1) - w - mx, w):
        a = e2[i:i + w]; a = (a - a.mean()) / (a.std() + 1e-9); best = (-2, 0)
        for lag in range(-mx, mx + 1):
            c = e1[i + lag:i + lag + w]; c = (c - c.mean()) / (c.std() + 1e-9); v = float((a * c).mean())
            if v > best[0]: best = (v, lag)
        out.append((round(best[1] / r * 1000), round(best[0], 2)))
    return out


def row(p, b, bsr, bch):
    x, sr = A.read_wav(p); al = align(x, sr, b, bsr); lags = np.array([a for a, _ in al]); cs = np.array([c for _, c in al]); w = 20 * sr; keys = [MM.key(x[i:i + w].mean(axis=1), sr)[0][1] for i in range(0, max(1, len(x) - w), w)]; r = MM.row(p, 86)
    return dict(seconds=r['seconds'], lufs_i=r['lufs_i'], swing=r['swing_4s_lu'], true_peak=r['true_peak'], bands=r['bands_lt200_lt1k_lt4k_hi'], centroid=r['centroid_hz'], tempo_within_2pct=r['tempo_within_2pct'], keys=keys, f_minor_share=round(sum(k == 'F minor' for k in keys) / len(keys), 2), chroma_to_base=round(float(np.corrcoef(LM.chroma(x.mean(axis=1), sr), bch)[0, 1]), 2), lag_ms_median=int(np.median(lags)), lag_ms_abs_p90=int(np.percentile(np.abs(lags), 90)), within_15ms=round(float((np.abs(lags) <= 15).mean()), 2), onset_corr_median=round(float(np.median(cs)), 2))


if __name__ == '__main__':
    b, bsr = A.read_wav(L / 'base.wav'); bch = LM.chroma(b.mean(axis=1), bsr); out = {}
    for f in sys.argv[1:]:
        out[Path(f).stem] = r = row(f, b, bsr, bch); print(Path(f).stem, {k: r[k] for k in ('seconds', 'lufs_i', 'swing', 'centroid', 'tempo_within_2pct', 'f_minor_share', 'chroma_to_base', 'lag_ms_median', 'lag_ms_abs_p90', 'within_15ms', 'onset_corr_median')}, r['keys'])
    (L / 'align.json').write_text(json.dumps(out, indent=1))
