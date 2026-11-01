"""Brief 087 (Chime): calm, tension and fight from the long piece's stems, so they share every sample's position; a 4-bar loop cut at one offset for all three; each measured against the danger cues at its Alert level; the reel. Writes briefs/087/music-layers/*.mp3, music-layers.json, music-layers.mp4 + .mp3. Run from the repo root with the scratch venv's python -I."""
import json, subprocess, sys
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE))
import audiolib as A, music_measure as MM, layers_measure as LM, mix as M, reel as R, cars as C
L = REPO / 'out/scratch/chime/layers'; OUT = REPO / 'briefs/087/music-layers'; OUT.mkdir(exist_ok=True); SR = 48000
RECIPE = {'calm': {'other': 0, 'bass': -9, 'drums': None}, 'tension': {'other': 0, 'bass': 0, 'drums': -5}, 'fight': {'other': 0, 'bass': 2, 'drums': 4}}  # dB per stem; None = out
LEVEL = {'calm': -32, 'tension': -28, 'fight': -24}  # LUFS at Music 100; Alert 0, Alert 1-2, a fight
DANGER = ['notice_1', 'notice_2', 'notice_3', 'camera_click', 'patrol_break', 'police_siren', 'handover_chime', 'cafe_bell', 'car_warn_relay', 'car_warn_escape', 'car_warn_bell']


def stems():
    return {s: A.resample(A.read_wav(L / f'stems/base.{s}.wav')[0], 44100 if A.read_wav(L / f'stems/base.{s}.wav')[1] == 44100 else 48000, SR) for s in ('drums', 'bass', 'other')}


def fine_bar(d):
    """The bar period that best repeats the drum stem's onsets over the piece (stated 86 BPM; the tool reads a little high)."""
    e, r = MM.flux(d.mean(axis=1), SR); best = []
    for bpm in np.arange(84, 89, 0.02):
        lag = 4 * 4 * 60 / bpm * r; i = int(round(lag)); best.append((float((e[:-i] * e[i:]).mean()), round(float(bpm), 2)))
    return sorted(best, reverse=True)[0][1], sorted(best, reverse=True)[:3]


def phase(d, bar):
    """Where the bars start: the offset whose comb of bar lines catches most drum onset."""
    e, r = MM.flux(d.mean(axis=1), SR); n = int(bar * r); c = np.zeros(n); k = len(e) // n; c = e[:k * n].reshape(k, n).sum(axis=0); return float(np.argmax(c) / r)


def main():
    S = stems(); n = min(len(v) for v in S.values()); S = {k: v[:n] for k, v in S.items()}; bpm, top = fine_bar(S['drums']); bar = 4 * 60 / bpm; ph = phase(S['drums'], bar); res = {'note': 'Measures only; Chime cannot hear. Base: text_t2 (ACE-Step, no MiniMax audio in it). Arrangements are stem mixes: nothing new is played in them.', 'bpm_fine': bpm, 'bpm_candidates': top, 'bar_s': round(bar, 4), 'downbeat_offset_s': round(ph, 3), 'recipes_db': RECIPE, 'levels_lufs_at_music_100': LEVEL}
    arr = {}
    for name, rec in RECIPE.items():
        x = sum(S[s] * 10 ** (db / 20) for s, db in rec.items() if db is not None); g = LEVEL[name] - A.lufs_i(x, SR); arr[name] = x * 10 ** (g / 20)
    # the loop: 4 bars at one offset for all three, chosen on the tension mix: harmony at the end like the start, and no level step
    best = None; chs = []; nb = int((n / SR - ph) / bar); pos = lambda b: int((ph + b * bar) * SR)
    for b in range(nb): chs.append(LM.chroma(arr['tension'][pos(b):pos(b + 1)].mean(axis=1), SR))
    for b in range(nb - 5):
        a, e = pos(b), pos(b + 4); t = arr['tension']; jump = abs(20 * np.log10(np.sqrt((t[a:a + 4800] ** 2).mean()) / np.sqrt((t[e - 4800:e] ** 2).mean()))); cc = float(np.corrcoef(chs[b], chs[b + 4])[0, 1]); s = cc - jump * .05
        if best is None or s > best[0]: best = (s, b, cc, jump)
    b0 = best[1]; a, e = pos(b0), pos(b0 + 4); xf = 0.03; loops = {}; res['loop'] = {'start_bar': b0, 'start_s': round(a / SR, 3), 'seconds': round((e - a) / SR, 3), 'next_bar_chroma_corr': round(best[2], 2), 'crossfade_s': xf, 'by_arrangement': {}}
    bed = M.clip('street_bed_even'); res['against_danger_cues_with_even_bed'] = {}
    for name, x in arr.items():
        k = int(xf * SR); lp = A.loop(x[a:e + k], SR, xf); loops[name] = lp; res['loop']['by_arrangement'][name] = A.seam(lp, SR); A.write_wav(L / f'loop_{name}.wav', lp, SR)
        b_, cen = A.bands(x, SR); L4 = [A.lufs_i(x[i:i + 4 * SR], SR) for i in range(0, n - 4 * SR, 2 * SR)]; res.setdefault('arrangements', {})[name] = {'lufs_i': round(A.lufs_i(x, SR), 1), 'true_peak': round(A.true_peak(x), 1), 'swing_4s_lu': round(max(L4) - min(L4), 1), 'bands': b_, 'centroid_hz': cen, 'keys_per_20s': [MM.key(x[i:i + 20 * SR].mean(axis=1), SR)[0][1] for i in range(0, n - 20 * SR, 20 * SR)]}
        under = x[:len(bed)] + bed[:len(x)]; res['against_danger_cues_with_even_bed'][name] = {d: {**A.clear_of(M.clip(d), SR, under, SR, M.BUSES[M.G[d]]['db']), 'loudness_margin_lu': round(A.lufs_m(M.clip(d), SR) + M.BUSES[M.G[d]]['db'] - A.lufs_i(under, SR), 1)} for d in DANGER}
        w = L / f'arr_{name}.wav'; A.write_wav(w, x, SR); subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(w), '-c:a', 'libmp3lame', '-b:a', '160k', str(OUT / f'{name}.mp3')], check=True)
    # alignment proof: the arrangements' own onsets against each other, sample-locked by construction; report the measured lag
    res['alignment_ms_between_arrangements'] = {f'{p}/{q}': int(np.median([u for u, _ in __import__('layers_align').align(arr[p], SR, arr[q], SR)])) for p, q in (('calm', 'tension'), ('tension', 'fight'), ('calm', 'fight'))}
    res['routes'] = json.loads((L / 'routes.json').read_text()); res['cover_arrangements'] = json.loads((L / 'align.json').read_text())
    # ---- reel: crossfade on bar lines as a game would: 2-bar equal-power, tension 8 bars, fight 8 bars, back to calm
    st = pos(b0); seg_b = 8; xfb = 2; tl = [('calm', 8), ('tension', 8), ('fight', 8), ('tension', 4), ('calm', 6)]; total = sum(b for _, b in tl); out = np.zeros((int((total + xfb) * bar * SR), 2)); cur = 0; marks = []
    def env(name_b, b1, b2):
        na = int(b1 * bar * SR); nb_ = int(b2 * bar * SR); return na, nb_
    gm = np.zeros((len(out), 3)); idx = {'calm': 0, 'tension': 1, 'fight': 2}; b = 0
    for name, bars in tl:
        i0 = int(b * bar * SR); i1 = int((b + bars) * bar * SR); gm[i0:i1, idx[name]] = 1; marks.append((b * bar, name)); b += bars
    k = int(xfb * bar * SR)
    for j in range(3): gm[:, j] = np.convolve(gm[:, j], np.ones(k) / k, 'same'); 
    gm = np.sqrt(np.clip(gm, 0, 1)); xx = {name: np.resize(np.roll(arr[name], -st, axis=0), out.shape) for name in arr}
    for name, j in idx.items(): out += xx[name] * gm[:, j:j + 1]
    tailk = out.shape[0]; out = A.fade(out, SR, .3, 1.0); seq = out[:int(total * bar * SR)]
    N = 0; audio, segs = [], []
    def seg(h, t, sub, a_, foot=''):
        nonlocal N
        p = R.TMP / f'lay{N:02d}.png'; N += 1; R.card(p, h, t, sub, None, foot); segs.append((p, len(a_) / SR)); audio.append(a_)
    gain = lambda y, lu: y * 10 ** ((lu - A.lufs_i(y, SR)) / 20)
    tz, tsr = A.read_wav(L / 'teaser.wav'); tz = A.resample(tz, tsr, SR)[:35 * SR]; tz = A.fade(gain(tz, -21), SR, .05, 1.0)
    base, bsr = A.read_wav(L / 'base.wav'); base = A.resample(base, bsr, SR); ex = A.fade(gain(base[:75 * SR], -21), SR, .05, 1.5)
    seg('Layer test', 'Teaser direction', 'The original, the longer piece, then calm, tension and fight crossfading as a game would. Nothing is wired.', np.concatenate([R.sil(.3), R.speak('Layer test on the teaser music. The original, the longer piece, then calm to tension to fight, as a game would crossfade them.'), R.sil(.4)]), foot='Chime cannot hear these: your ears decide.')
    seg('1 of 4', 'The original', 'The teaser score, MiniMax, its first 35 s.', np.concatenate([R.speak('The original.'), R.sil(.3)])); seg('1 of 4', 'The original', 'The teaser score, MiniMax, its first 35 s. The file runs 140 s, and changes key at about a minute.', np.concatenate([tz, R.sil(.6)]), foot='score_t1.mp3')
    seg('2 of 4', 'The longer piece', 'ACE-Step, same words, 86 BPM, F minor. 148 s; here the first 75 s.', np.concatenate([R.speak('The longer piece.'), R.sil(.3)])); seg('2 of 4', 'The longer piece', 'ACE-Step, same words, 86 BPM, F minor. 148 s; here the first 75 s.', np.concatenate([ex, R.sil(.6)]), foot='long_piece.mp3')
    seg('3 of 4', 'Calm, tension, fight', 'Same music at every moment: remixed from its stems. Crossfades on bar lines, 2 bars each.', np.concatenate([R.speak('Calm, to tension, to fight, and back.'), R.sil(.3)]))
    lab = {'calm': 'Calm (Alert 0)', 'tension': 'Tension (Alert 1 and 2)', 'fight': 'Fight'}; marks2 = marks + [(total * bar, '')]
    for (t0, nm), (t1, _) in zip(marks2, marks2[1:]): seg('3 of 4', lab[nm], f'At their game levels: {LEVEL[nm]} LUFS.', seq[int(t0 * SR):int(t1 * SR)], foot=f'{bpm} BPM measured, F minor')
    seg('4 of 4', 'The 4-bar loop', f"Tension's {round((e - a) / SR, 1)} s loop, three times round: listen at each join.", np.concatenate([R.speak('The four bar loop, three times round.'), R.sil(.3), np.concatenate([loops['tension']] * 3), R.sil(.6)]), foot='loop_tension')
    a_ = np.concatenate(audio) * 10 ** (4 / 20); a_ = a_ * min(1.0, 10 ** (-1 / 20) / np.abs(a_).max()); w = R.TMP / 'layers.wav'; A.write_wav(w, a_, SR); res['reel'] = {'seconds': round(len(a_) / SR, 1), 'peak_db': round(A.sample_peak(a_), 1), 'lufs_i': round(A.lufs_i(a_, SR), 1), 'seq_lufs_by_part': {nm: round(A.lufs_i(seq[int(t0 * SR):int(t1 * SR)], SR), 1) for (t0, nm), (t1, _) in zip(marks2, marks2[1:])}}
    lst = R.TMP / 'cards-lay.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d:.3f}\n" for p, d in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(w), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / 'briefs/087/music-layers.mp3')], check=True)
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(w), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-shortest', '-movflags', '+faststart', str(REPO / 'briefs/087/music-layers.mp4')], check=True, cwd=str(R.TMP))
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(L / 'base.wav'), '-c:a', 'libmp3lame', '-b:a', '160k', str(OUT / 'long_piece.mp3')], check=True)
    res['stems_teaser'] = LM.stems('teaser'); res['stems_base'] = LM.stems('base')
    (REPO / 'briefs/087/music-layers.json').write_text(json.dumps(res, indent=1, ensure_ascii=False) + '\n'); print(json.dumps({k: res[k] for k in ('bpm_fine', 'bpm_candidates', 'downbeat_offset_s', 'loop', 'arrangements', 'alignment_ms_between_arrangements', 'reel')}, indent=0)[:3500])
    for nm, t in res['against_danger_cues_with_even_bed'].items(): print(nm, {d: (v['margin_top3_db'], v['loudness_margin_lu']) for d, v in t.items()})


main()
