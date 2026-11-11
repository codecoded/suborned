"""Brief 107 (Chime): takes -> finished clips, listening files and briefs/107/chime/weather-sfx.json. Deterministic: same takes, same numbers.
Run from the repo root with the scratch venv (numpy + scipy; ffmpeg for the mp3s):
   out/scratch/chime/venv/bin/python -I briefs/107/chime/make/build.py [--clips DIR]
--clips: where the finished picks go as WAV at their game level (default out/scratch/chime/107/clips; Latch points it at Unity after Sky picks).
The listening mp3s go to briefs/107/chime/audio/, all raised by the same LIFT so their levels compare as in the game."""
import hashlib, json, subprocess, sys, tempfile
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[3]
sys.path.insert(0, str(HERE))
import measure as M
from table import CUES, T
A = M.A
AUDIO = HERE.parent / 'audio'; PACK = REPO / 'unity/Assets/Suborned/Resources/Audio/087'
CEIL = -1.0; MIXSR = 48000; LIFT = 8.0  # page copies only: beds at -38..-40 play at -30..-32 LUFS; turn the volume up rather than squash the peaks
DANGER = ['notice_1', 'notice_2', 'notice_3', 'camera_click', 'cafe_bell', 'cafe_door', 'handover_chime', 'car_warn', 'police_siren', 'patrol_break']


def take(name):
    return A.read_wav(REPO / T / f'{name}.wav')


def shape(c, name):
    """Cut, channels, rate, high-pass; ride the level flat if the cue asks."""
    x, sr = take(name); a, b = c['cut'].get(name, (0.1, len(x) / sr - 0.1)); x = x[int(a * sr):int(b * sr)]
    x = x if c['stereo'] else x.mean(axis=1, keepdims=True)
    x = A.resample(x, sr, c['rate']); sr = c['rate']
    if c['hp']: x = A.hp(x, c['hp'], sr)
    if c['ride']: x = ride(x, sr)
    return x, sr


def ride(x, sr, win=2.0, hop=0.25, most=6.0):
    """Slow level riding: 2 s loudness, steered to its median, never more than 6 dB, gain eased over 2 s, so a drifting bed holds level without pumping."""
    n, h = int(win * sr), int(hop * sr); idx = np.arange(0, max(1, len(x) - n), h); cur = np.array([A.lufs_i(x[i:i + n], sr) for i in idx])
    g = np.clip(np.median(cur) - cur, -most, most); k = max(1, int(win / hop)); g = np.convolve(np.pad(g, k, mode='edge'), np.ones(k) / k, 'same')[k:-k]
    gs = np.interp(np.arange(len(x)), idx + n // 2, g); return x * (10 ** (gs / 20)).reshape(-1, 1)


def master(x, sr, level, meter):
    """To the level, then down to the ceiling if the level would pass it; returns the clip and the shortfall (dB)."""
    g = level - (A.lufs_i if meter == 'i' else A.lufs_m)(x, sr); y = x * 10 ** (g / 20); tp = A.true_peak(y); short = 0.0
    if tp > CEIL: short = tp - CEIL; y = y * 10 ** (-short / 20)
    return y, round(short, 1)


def loop_of(c, names):
    """One take (or several, evened and crossfaded) made into a seamless loop at the cue's level."""
    parts = [shape(c, n) for n in names]; sr = parts[0][1]; xs = [p[0] for p in parts]
    if len(xs) > 1:
        tgt = np.median([A.lufs_i(x, sr) for x in xs]); xs = [x * 10 ** ((tgt - A.lufs_i(x, sr)) / 20) for x in xs]; y = xs[0]
        for z in xs[1:]: y = A.join(y, z, sr, c['xf'])
    else: y = xs[0]
    pre = y; y = A.loop(y, sr, c['xf']); y, short = master(y, sr, c['level'], c['meter'])
    if c.get('peak') is not None:  # tame the odd loud drop: limit to the peak, bring the level back, limit again
        for _ in range(2): y, _t = limit(y, sr, c['peak'], wrap=True); y, short = master(y, sr, c['level'], c['meter'])
    return y, sr, pre, short


def stats(c, y, sr, pre=None):
    b, cen = A.bands(y, sr); m = {'seconds': round(len(y) / sr, 2), 'sample_rate': sr, 'channels': y.shape[1], 'lufs_integrated': round(A.lufs_i(y, sr), 1), 'lufs_momentary_max': round(A.lufs_m(y, sr), 1),
                                  'true_peak_dbtp': round(A.true_peak(y), 1), 'bands_db_lt200_lt1k_lt4k_hi': b, 'centroid_hz': cen, 'small_speaker_lufs_m': round(A.lufs_m(A.hp(y, 200, sr, 4), sr), 1)}
    if pre is not None:
        w = M.window_lufs(pre, sr); tn = M.tonal(pre, sr)
        m.update({'swing_4s_lu': round(float(w.max() - w.min()), 1), 'low_events_s': [float(v) for v in M.band_events(pre, sr, 20, 150, 8)], 'mid_events_s': [float(v) for v in M.band_events(pre, sr, 150, 2000, 8)],
                  'high_events_s': [float(v) for v in M.band_events(pre, sr, 2000, 12000, 8)], 'tonal_share': tn[0]})
    if c['kind'] == 'loop':
        s = A.seam(y, sr); s['clean'] = bool(s['step_percentile'] < 99 and s['level_jump_db'] <= s['typical_jump_db_p95']); m['seam'] = s
    return m


def step_set(c, name):
    """The take's footsteps cut apart; the four that stand clearest over the noise between steps, at a level close to the median step."""
    x, sr = take(name); x = A.hp(x.mean(axis=1, keepdims=True), c['hp'], sr); on, floor = M.steps(x, sr, c['min_gap']); rows = []
    for i, o in enumerate(on):
        end = min(on[i + 1] - int(0.03 * sr) if i + 1 < len(on) else len(x), o + int(0.42 * sr)); a = max(0, o - int(0.01 * sr)); s = x[a:end]
        if len(s) < int(0.12 * sr): continue
        h = int(0.005 * sr); e = 20 * np.log10(np.sqrt(np.convolve(s[:, 0] ** 2, np.ones(h) / h, 'same')) + 1e-9)
        rows.append({'at_s': round(a / sr, 3), 'len_s': round(len(s) / sr, 3), 'lufs_m': A.lufs_m(s, sr), 'over_floor_db': float(e.max() - floor), 'tail_db': float(e[-int(0.04 * sr):].mean() - e.max()), 'x': s})
    med = np.median([r['lufs_m'] for r in rows]); ok = [r for r in rows if abs(r['lufs_m'] - med) <= 4 and r['tail_db'] < -18] or rows
    best = sorted(sorted(ok, key=lambda r: -r['over_floor_db'])[:c['steps']], key=lambda r: r['at_s']); clips = []
    for r in best:
        y = A.fade(r['x'], sr, 0.002, 0.08); y, _ = master(y, sr, c['level'], 'm'); clips.append(y)
    score = round(float(np.mean([r['over_floor_db'] for r in best])), 1)
    return clips, sr, {'steps_found': len(rows), 'steps_used_at_s': [r['at_s'] for r in best], 'over_floor_db': score, 'step_lufs_spread_db': round(float(np.ptp([r['lufs_m'] for r in best])), 1)}


def walk(clips, sr, n=12, gap=0.55, pan=0.0):
    out = np.zeros((int((n * gap + 0.6) * sr), 1)); hs = [0.0, 1.0, -0.6, 0.4, -1.0, 0.7]
    for i in range(n):
        y = clips[i % len(clips)]; at = int((i * gap + hs[i % len(hs)] * 0.012) * sr); out[at:at + len(y)] += y[:len(out) - at]
    return out


def pack(cue):
    x, sr = A.read_wav(PACK / f'{cue}.wav'); return x, sr


def at48(x, sr):
    x = A.resample(x, sr, MIXSR); return x if x.shape[1] == 2 else np.repeat(x, 2, axis=1)


def lay(bed, clip, at_s, db=0.0):
    i = int(at_s * MIXSR); n = min(len(clip), len(bed) - i)
    if n > 0: bed[i:i + n] += clip[:n] * 10 ** (db / 20)
    return bed


def tile(x, n):
    return np.concatenate([x] * (n // len(x) + 1))[:n]


def clear_rows(bed, bsr):
    out = {}
    for d in DANGER:
        y, sr = pack(d); k = A.clear_of(y, sr, bed, bsr); k['loudness_margin_lu'] = round(A.lufs_m(y, sr) - A.lufs_i(bed, bsr), 1); out[d] = k
    return out


def limit(x, sr, ceil_db=-1.5, rel=0.08, wrap=False):
    """Peak limiter: 3 ms look-ahead, 80 ms release. Returns the clip and the most it took (dB). wrap: a loop, so the gain is worked out over the seam too."""
    if wrap:
        k = int(0.5 * sr); y, t = limit(np.concatenate([x[-k:], x, x[:k]]), sr, ceil_db, rel); return y[k:-k], t
    from scipy.ndimage import minimum_filter1d
    from scipy.signal import lfilter
    c = 10 ** (ceil_db / 20); g = np.minimum(1.0, c / (np.abs(x).max(axis=1) + 1e-12)); g = minimum_filter1d(g, int(0.003 * sr) * 2 + 1)
    a = np.exp(-1 / (rel * sr)); s = 1 - g
    env = lfilter([1 - a], [1, -a], s); env = np.maximum(env, s); gg = 1 - env; gg = minimum_filter1d(gg, int(0.003 * sr) * 2 + 1)
    return x * gg.reshape(-1, 1), float(-20 * np.log10(gg.min() + 1e-12))


def mp3(path, x, sr):
    with tempfile.TemporaryDirectory() as d:
        w = Path(d) / 'x.wav'; A.write_wav(w, x, sr); subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(w), '-codec:a', 'libmp3lame', '-b:a', '192k', str(path)], check=True)


def main(a):
    clips_dir = Path(a[a.index('--clips') + 1]) if '--clips' in a else REPO / 'out/scratch/chime/107/clips'
    clips_dir.mkdir(parents=True, exist_ok=True); AUDIO.mkdir(parents=True, exist_ok=True)
    led = {}
    for l in (REPO / 'paid/107-weather-sound/ledger.jsonl').read_text().splitlines():
        r = json.loads(l)
        if r.get('file'): led[Path(r['file']).stem] = r
    listen = {}; doc_cues = []; picks = {}
    for c in CUES:
        rows = []
        if c['kind'] == 'loop':
            for n in c['takes']:
                y, sr, pre, short = loop_of(c, [n]); m = stats(c, y, sr, pre); m['ceiling_shortfall_db'] = short
                s = int(min(6, len(y) / sr / 2 - 0.5) * sr); listen[f'{n}'] = (y, sr); listen[f'{n}_seam'] = (np.concatenate([y[-s:], y[:s]]), sr)
                rows.append({'id': n, 'files': {'loop': f'audio/{n}.mp3', 'seam': f'audio/{n}_seam.mp3'}, 'measured': m})
                if n == c['pick']: picks[c['id']] = (y, sr)
            if c['join']:
                y, sr, pre, short = loop_of(c, c['join']); m = stats(c, y, sr, pre); m['ceiling_shortfall_db'] = short; n = c['pick']
                s = int(6 * sr); listen[n] = (y, sr); listen[f'{n}_seam'] = (np.concatenate([y[-s:], y[:s]]), sr); picks[c['id']] = (y, sr)
                rows.insert(0, {'id': n, 'joined': c['join'], 'files': {'loop': f'audio/{n}.mp3', 'seam': f'audio/{n}_seam.mp3'}, 'measured': m})
        elif c['kind'] == 'steps':
            best = None
            for n in c['takes']:
                cl, sr, info = step_set(c, n); w = walk(cl, sr); listen[f'{n}_walk'] = (w, sr)
                for i, y in enumerate(cl): listen[f'{n}_step{i + 1}'] = (y, sr)
                info['step_seconds'] = [round(len(y) / sr, 3) for y in cl]; info['true_peak_dbtp'] = round(max(A.true_peak(y) for y in cl), 1)
                rows.append({'id': n, 'files': {'walk': f'audio/{n}_walk.mp3', 'steps': [f'audio/{n}_step{i + 1}.mp3' for i in range(len(cl))]}, 'measured': info})
                info['score'] = round(info['over_floor_db'] - 2 * info['step_lufs_spread_db'], 1)  # clear of the noise, and even from step to step
                if best is None or info['score'] > best[1]['score']: best = (n, info, cl, sr)
            c['pick'] = best[0]; c['why'] = (f"Its steps stand {best[1]['over_floor_db']} dB over the noise between them and come out within {best[1]['step_lufs_spread_db']} dB of each other, at a steady pace. "
                                            + '; '.join(f"{r['id'].split('_')[-1]}: {r['measured']['over_floor_db']} dB over, {r['measured']['step_lufs_spread_db']} dB spread" for r in rows if r['id'] != best[0]) + '.')
            picks[c['id']] = (best[2], best[3])
        else:
            n = c['takes'][0]; x, sr = shape(c, n); x = A.fade(x, sr, 3.0, 2.5); y, short = master(x, sr, c['level'], c['meter']); m = stats(c, y, sr); m['ceiling_shortfall_db'] = short
            listen[n + '_oneshot'] = (y, sr); rows.append({'id': n, 'files': {'clip': f'audio/{n}_oneshot.mp3'}, 'measured': m}); picks[c['id']] = (y, sr)
        for r in rows:
            src = r.get('joined', [r['id']]); r['source'] = [{'take': f'{s}.wav', 'ledger_id': led[s]['id'], 'cost_usd': led[s]['cost_usd'], 'prompt': led[s]['prompt']} for s in src if s in led]
        doc_cues.append({k: c[k] for k in ('id', 'label', 'kind', 'level', 'meter', 'plays', 'spatial', 'pick', 'why', 'hp', 'xf', 'rate', 'optional')} | {'level_unit': 'LUFS integrated' if c['meter'] == 'i' else 'LUFS momentary max', 'takes': rows})
    # finished picks at game level (for Latch after Sky picks)
    for cid, v in picks.items():
        if cid == 'wet_steps':
            for i, y in enumerate(v[0]): A.write_wav(clips_dir / f'wet_step_{i + 1}.wav', y, v[1])
        else: A.write_wav(clips_dir / f'{cid}.wav', *v)
    # the mix: what is under the cues in rain and at night
    street, ssr = pack('street_bed'); street48 = at48(street, ssr); N = len(street48)  # the whole day bed (35.6 s): its first half is the quiet one
    rain = tile(at48(*picks['rain_bed']), N); awn = tile(at48(*picks['rain_awning']), N); drp = tile(at48(*picks['drips']), N)
    night = tile(at48(*picks['night_bed']), N)
    rain_floor = street48 + rain + awn * 10 ** (-6 / 20) + drp * 10 ** (-6 / 20)
    sums = {'street_bed_alone_lufs_i': round(A.lufs_i(street48, MIXSR), 1), 'street_plus_rain_lufs_i': round(A.lufs_i(street48 + rain, MIXSR), 1),
            'rain_floor_lufs_i': round(A.lufs_i(rain_floor, MIXSR), 1), 'rain_floor_note': 'street bed + rain bed + awning and drips each 6 dB down (a cafe about 15 m off)',
            'night_bed_lufs_i': round(A.lufs_i(night, MIXSR), 1)}
    clear = {'street_bed (087, today)': clear_rows(street48, MIXSR), 'rain floor': clear_rows(rain_floor, MIXSR), 'night bed': clear_rows(night, MIXSR)}
    wk = at48(walk(picks['wet_steps'][0], picks['wet_steps'][1], n=10), picks['wet_steps'][1])
    mixes = {}; L = 20 * MIXSR; w = M.window_lufs(street48, MIXSR, 20.0, 1.0); o = int(np.argmin(np.abs(w - A.lufs_i(street48, MIXSR)))) * MIXSR  # 20 s where the day bed sits at its average
    street48, rain_floor, night = street48[o:o + L], rain_floor[o:o + L], night[o:o + L]
    m1 = street48.copy(); mixes['mix_day'] = lay(lay(m1, at48(*pack('notice_1')), 12.0), at48(*pack('cafe_bell')), 16.0)
    m2 = rain_floor.copy(); m2 = lay(m2, wk, 3.0, -2.0); m2 = lay(m2, at48(*pack('notice_1')), 12.0); mixes['mix_rain'] = lay(m2, at48(*pack('cafe_bell')), 16.0)
    m3 = night.copy(); m3 = lay(m3, at48(*picks['night_tram']), 1.0); m3 = lay(m3, at48(*pack('notice_1')), 12.0); mixes['mix_night'] = lay(m3, at48(*pack('cafe_bell')), 16.0)
    for k, v in mixes.items(): listen[k] = (v, MIXSR)
    sums.update({f'{k}_lufs_i': round(A.lufs_i(v, MIXSR), 1) for k, v in mixes.items()})
    # one lift for every listening file, so they compare as in the game; as loud as the loudest file allows under -1 dBTP, at most 18 dB
    # one lift for every listening file, so they compare as in the game; a peak limiter (page copies only) catches the few tings and steps it pushes past -1 dBTP
    lift = LIFT; limited = {}
    for k, (x, sr) in listen.items():
        y, took = limit(x * 10 ** (lift / 20), sr)
        if took > 0.05: limited[k] = round(took, 1)
        mp3(AUDIO / f'{k}.mp3', y, sr)
    spent = sum(json.loads(l).get('cost_usd') or 0 for l in (REPO / 'paid/107-weather-sound/ledger.jsonl').read_text().splitlines())
    doc = {'schema': 'suborned.audio-takes/1', 'brief': '107', 'made_by': 'briefs/107/chime/make/build.py from paid/107-weather-sound/takes (Seed Audio 1.0 via Runware, bytedance:seed-audio@1.0)',
           'note': 'Measured numbers are not a judgement of how anything sounds: Chime cannot hear. Levels are per clip at gain 1, against the 087 pack in unity/Assets/Suborned/Resources/Audio/087.',
           'listening_lift_db': lift, 'listening_limited_db': limited, 'spent_usd': round(spent, 4), 'sums': sums, 'clear_of': clear, 'mixes': {k: f'audio/{k}.mp3' for k in mixes}, 'cues': doc_cues,
           'clips_dir': str(clips_dir.relative_to(REPO)) if clips_dir.is_relative_to(REPO) else str(clips_dir)}
    (HERE.parent / 'weather-sfx.json').write_text(json.dumps(doc, indent=1, ensure_ascii=False) + '\n')
    for c in doc_cues:
        for r in c['takes']:
            m = r['measured']; print(f"{r['id']:18}", {k: m.get(k) for k in ('seconds', 'lufs_integrated', 'lufs_momentary_max', 'true_peak_dbtp', 'swing_4s_lu', 'low_events_s', 'tonal_share', 'over_floor_db', 'steps_found', 'step_lufs_spread_db', 'ceiling_shortfall_db') if k in m}, m.get('seam', ''))
    print('sums', sums); print('lift', lift, 'spent', round(spent, 4))


main(sys.argv[1:])
