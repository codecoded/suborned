"""Brief 087 (Chime): the layer test's measures. `stems <prefix>`: bleed of a Demucs split. `routes wavs...`: each long take against the teaser's F minor section: chroma correlation, spectrum distance, tempo and key held. Run with the scratch venv's python -I. Measures only."""
import json, sys
from pathlib import Path
import numpy as np
from scipy.signal import stft
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE))
import audiolib as A, music_measure as MM
L = REPO / 'out/scratch/chime/layers'; STEMS = ['drums', 'bass', 'other', 'vocals']


def chroma(m, sr):
    f, _, S = stft(m, sr, nperseg=8192); P = (np.abs(S) ** 2).mean(axis=1); ch = np.zeros(12); k = (f > 60) & (f < 2000); np.add.at(ch, np.round(12 * np.log2(f[k] / 261.626)).astype(int) % 12, P[k]); return ch


def stems(prefix, d=None):
    d = Path(d or L / 'stems'); X = {s: A.read_wav(d / f'{prefix}.{s}.wav') for s in STEMS}; sr = X['bass'][1]; mix = sum(X[s][0] for s in STEMS); o, osr = A.read_wav(L / f'{prefix}.wav') if (L / f'{prefix}.wav').exists() else A.read_wav(REPO / f'paid/087-music/layers/{prefix}.wav'); o = A.resample(o, osr, sr); n = min(len(o), len(mix)); res = {'residual_db_vs_mix': round(A.sample_peak(o[:n] - mix[:n]) if False else float(20 * np.log10(np.sqrt(((o[:n] - mix[:n]) ** 2).mean()) / np.sqrt((o[:n] ** 2).mean()))), 1), 'stems': {}}
    tot = sum((X[s][0] ** 2).mean() for s in STEMS)
    for s in STEMS:
        x = X[s][0]; m = x.mean(axis=1); f, _, S = stft(m, sr, nperseg=2048); P = np.abs(S) ** 2; e = P.sum() + 1e-20; lo = 10 * np.log10(P[f < 120].sum() / e + 1e-9); hi = 10 * np.log10(P[f > 500].sum() / e + 1e-9); gm = np.exp(np.mean(np.log(P + 1e-12), axis=0)); am = P.mean(axis=0) + 1e-12; flat = float(np.median(gm / am))
        act = 20 * np.log10(np.sqrt(np.convolve(m ** 2, np.ones(int(.1 * sr)) / int(.1 * sr), 'same'))[::int(.1 * sr)] + 1e-9); pk = np.percentile(act, 95)
        res['stems'][s] = {'share_of_energy_db': round(float(10 * np.log10((x ** 2).mean() / tot)), 1), 'lufs_i': round(A.lufs_i(x, sr), 1), 'below_120hz_db': round(float(lo), 1), 'above_500hz_db': round(float(hi), 1), 'flatness_median': round(flat, 3), 'silent_share_30db_below_peak': round(float((act < pk - 30).mean()), 2)}
    # bleed proxy: how alike two stems' moment-to-moment level is, band by band (a clean split moves independently); both stems in their shared band
    res['level_correlation_200_2k'] = {}
    env = {}
    for s in STEMS:
        f, t, S = stft(X[s][0].mean(axis=1), sr, nperseg=2048); env[s] = np.log((np.abs(S) ** 2)[(f > 200) & (f < 2000)].sum(axis=0) + 1e-9)
    for i, a in enumerate(STEMS):
        for b in STEMS[i + 1:]: res['level_correlation_200_2k'][f'{a}/{b}'] = round(float(np.corrcoef(env[a], env[b])[0, 1]), 2)
    # bass and drums hold their stem? the bass stem above 500 Hz and the drums stem's tonal lines
    return res


def route(p, ref_ch, ref_bands):
    x, sr = A.read_wav(p); r = MM.row(p, 86); m = x.mean(axis=1); ch = chroma(m, sr); w = 20 * sr; keys = [MM.key(x[i:i + w].mean(axis=1), sr)[0][1] for i in range(0, max(1, len(x) - w), w)]; _, b = A.third_octaves(x, sr); b = b - b.mean(); r.update(chroma_corr_to_teaser=round(float(np.corrcoef(ch, ref_ch)[0, 1]), 2), spectrum_dist_db=round(float(np.sqrt(((b - ref_bands) ** 2).mean())), 1), keys_per_20s=keys, f_minor_share=round(sum(k == 'F minor' for k in keys) / len(keys), 2)); return r


if __name__ == '__main__':
    if sys.argv[1] == 'stems': print(json.dumps(stems(sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None), indent=0))
    else:
        s, sr = A.read_wav(L / 'section.wav'); ref = chroma(s.mean(axis=1), sr); _, rb = A.third_octaves(s, sr); rb = rb - rb.mean(); out = {}
        for f in sys.argv[2:]:
            r = route(f, ref, rb); out[Path(f).stem] = r; print(Path(f).stem, r['seconds'], 'I', r['lufs_i'], 'swing', r['swing_4s_lu'], 'tp', r['true_peak'], 'chroma', r['chroma_corr_to_teaser'], 'spec', r['spectrum_dist_db'], 'tempo', r['tempo_12s_windows_folded'][:14], 'within2%', r['tempo_within_2pct'], 'keys', r['keys_per_20s'], 'end', r['last_2s_vs_whole_lu'])
        (L / 'routes.json').write_text(json.dumps(out, indent=1))
