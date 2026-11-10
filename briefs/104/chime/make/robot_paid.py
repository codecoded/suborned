"""Brief 104 (Chime): the paid pass's candidates → finished clips, measured against the 087 sounds they sit with. Sums only: Sky's ears decide.
Mastered as briefs/087/chime/build.py does: cut, high-pass, fades, to a loudness level (momentary max; loops integrated), a -1 dBTP ceiling, fight cues start on their attack.
Writes briefs/104/chime/paid/<name>.wav (git-ignored, like every brief WAV) and briefs/104/chime/paid-measures.json. Run from the repo root:
   out/scratch/chime/venv/bin/python -I briefs/104/chime/make/robot_paid.py            the clips and paid-measures.json
   out/scratch/chime/venv/bin/python -I briefs/104/chime/make/robot_paid.py preview    then the 17 s preview with the picks"""
import hashlib, json, subprocess, sys, tempfile
from pathlib import Path
import numpy as np

HERE = Path(__file__).resolve().parent; CH = HERE.parent; REPO = HERE.parents[3]
sys.path.insert(0, str(REPO / 'briefs/087/chime')); sys.path.insert(0, str(HERE))
import audiolib as A  # noqa: E402
from robot_measure import shape_dist, decay20, pitch_track, mono  # noqa: E402

SR = 48000; CEIL = -1.0
TAKES = REPO / 'paid/104-robot-sfx/takes'; PACK = REPO / 'unity/Assets/Suborned/Resources/Audio/087'; OUT = CH / 'paid'; DRAFTS = CH / 'drafts'
LEDGER = REPO / 'paid/104-robot-sfx/ledger.jsonl'

# name: (cue, take, cut (s in the take), level, hp Hz, fade in, fade out, loop xf). level = LUFS-M for one-shots, LUFS-I for loops; the drafts' targets, which were the pack's.
C = {
    'siren_yelp_t1':  ('siren', 'siren_yelp_t1.wav', (0.06, 1.90), -22, 250, 0.01, 0.30, 0),
    'siren_wooo_t1':  ('siren', 'siren_wooo_t1.wav', (0.06, 1.85), -22, 250, 0.01, 0.30, 0),
    'siren_yelp_t3':  ('siren', 'siren_yelp_t3.wav', (0.08, 1.50), -22, 250, 0.01, 0.15, 0),
    'siren_whoop_t1': ('siren', 'siren_whoop_t1.wav', (0.00, 2.95), -22, 250, 0.02, 0.50, 0),
    'siren_hilo_t1':  ('siren', 'siren_hilo_t1.wav', (0.10, 1.30), -22, 250, 0.01, 0.20, 0),
    'siren_wooo_t3':  ('siren', 'siren_wooo_t3.wav', (0.06, 1.60), -22, 250, 0.01, 0.25, 0),
    'siren_crank_t1': ('siren', 'siren_crank_t1.wav', (0.05, 1.45), -22, 250, 0.005, 0.40, 0),
    'siren_twotone_t3': ('siren', 'siren_twotone_t3.wav', (0.06, 1.50), -22, 250, 0.01, 0.20, 0),
    'shot_slide_t1':  ('shot', 'shot_slide_t1.wav', (0.05, 1.45), -18.5, 120, 0, 0.50, 0),
    'shot_slide_t1_up': ('shot', 'shot_slide_t1.wav', (0.05, 1.45), -18.5, 120, 0, 0.50, 0),
    'shot_tin_t3':    ('shot', 'shot_tin_t3.wav', (0.08, 1.35), -18.5, 120, 0, 0.50, 0),
    'shot_pack_t1':   ('shot', 'shot_pack_t1.wav', (0.05, 1.45), -18.5, 120, 0, 0.60, 0),
    'shot_slide_t3':  ('shot', 'shot_slide_t3.wav', (0.05, 1.45), -18.5, 120, 0, 0.50, 0),
    'draw_hatch_t1':  ('draw', 'draw_hatch_t1.wav', (0.06, 1.48), -20, 80, 0.005, 0.12, 0),
    'draw_hatch_t2':  ('draw', 'draw_hatch_t2.wav', (0.05, 2.15), -20, 80, 0.005, 0.15, 0),
    'draw_servo_t1':  ('draw', 'draw_servo_t1.wav', (0.05, 2.45), -20, 80, 0.005, 0.15, 0),
    'draw_video2_t1': ('draw', 'draw_video2_t1.mp4', (0.00, 2.02), -20, 80, 0.01, 0.05, 0),
    'draw_video2_t3': ('draw', 'draw_video2_t3.mp4', (0.00, 2.02), -20, 80, 0.01, 0.05, 0),
    'chirp_alert_t2': ('chirp', 'chirp_alert_t2.wav', (0.08, 1.02), -27, 300, 0.005, 0.10, 0),
    'chirp_alert_t2b': ('chirp', 'chirp_alert_t2.wav', (0.08, 0.62), -27, 300, 0.005, 0.06, 0),
    'chirp_alert_t1': ('chirp', 'chirp_alert_t1.wav', (0.08, 0.50), -27, 300, 0.005, 0.08, 0),
    'roll_ball_t2':   ('roll', 'roll_ball_t2.wav', (0.5, 16.0), -36, 45, 0, 0, 0.5),
    'hum_gyro_t1':    ('hum', 'hum_gyro_t1.wav', (0.5, 14.5), -42, 60, 0, 0, 1.0),
}
# drive: the pack's soft limit (dB into tanh), for guns that are all peak; pitch: a tape-speed shift in semitones (pitch and length together), as the pack's card_new
SHAPE = {'shot_slide_t1': {'drive': 6}, 'shot_slide_t1_up': {'drive': 6, 'pitch': 2}, 'shot_tin_t3': {'drive': 4}, 'shot_slide_t3': {'drive': 4}}
# what each cue sits with, from the pack, and the draft it would replace
REF = {'siren': ['police_siren', 'patrol_break'], 'shot': ['fire_pistol', 'fire_police', 'fire_rival', 'fire_robot', 'fire_carbine'], 'draw': ['patrol_break', 'camera_click'],
       'chirp': ['notice_1', 'notice_3'], 'roll': ['patrol_loop'], 'hum': ['patrol_loop']}
DRAFT = {'siren': 'robot_siren', 'shot': 'robot_shot', 'draw': 'robot_draw', 'chirp': 'robot_chirp_alert', 'roll': 'robot_roll', 'hum': 'robot_hum'}
BUS = {'siren': 0, 'draw': 0, 'chirp': 0, 'shot': -3, 'roll': -3, 'hum': -3}


def load(p):
    if p.suffix == '.mp4':
        with tempfile.TemporaryDirectory() as d:
            w = Path(d) / 'x.wav'; subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(p), '-vn', '-c:a', 'pcm_s16le', str(w)], check=True); return A.read_wav(w)
    return A.read_wav(p)


def rd(p): x, sr = A.read_wav(p); return A.resample(x, sr, SR)
def r1(v): return None if v is None else round(float(v), 1)


def make(name):
    cue, take, (a, b), level, hp, fin, fout, xf = C[name]
    x, sr = load(TAKES / take); x = x.mean(axis=1, keepdims=True)[int(a * sr):int(b * sr)]
    sh = SHAPE.get(name, {})
    if sh.get('pitch'): x = A.resample(x, int(round(sr * 2 ** (sh['pitch'] / 12))), sr)
    x = A.resample(x, sr, SR); x = A.hp(x, hp, SR)
    if xf: x = A.loop(x, SR, xf)
    else: x = A.fade(x, SR, fin or 0.002, fout or 0.005)
    if cue == 'shot':   # on the frame, as the pack's guns: start at the attack, 3 ms of lead kept and faded
        m = mono(x); h = int(0.002 * SR); e = np.sqrt(np.convolve(m ** 2, np.ones(h) / h, 'same')); on = int(np.argmax(e > e.max() * 0.1)); x = A.fade(x[max(0, on - int(0.003 * SR)):], SR, 0.002, 0)
    if sh.get('drive'): dr = 10 ** (sh['drive'] / 20); x = x / (np.abs(x).max() + 1e-9); x = np.tanh(x * dr) / np.tanh(dr)
    lv = A.lufs_i(x, SR) if xf else A.lufs_m(x, SR); y = x * 10 ** ((level - lv) / 20); tp = A.true_peak(y)
    if tp > CEIL: y = y * 10 ** ((CEIL - tp) / 20)   # quieter than asked rather than clipped; the shortfall shows in the measures
    return y


def ends(x):
    """Clean starts and ends: the first 5 ms and the last 20 ms against the clip's peak (dB), and the floor: the quietest tenth of its active 20 ms windows."""
    m = mono(x); pk = np.abs(m).max() + 1e-12; db = lambda s: r1(20 * np.log10(np.sqrt((s ** 2).mean()) / pk + 1e-12))
    n = int(0.02 * SR); nb = len(m) // n; e = 20 * np.log10(np.sqrt((m[:nb * n].reshape(nb, n) ** 2).mean(axis=1)) / pk + 1e-12)
    return {'head_5ms_db': db(m[:int(0.005 * SR)]), 'tail_20ms_db': db(m[-n:]), 'floor_p10_db': r1(np.percentile(e[e > -60], 10)) if (e > -60).any() else None}


def measure(x, cue, loop, bed, fire):
    bands, cen = A.bands(x, SR)
    d = {'seconds': round(len(x) / SR, 3), 'true_peak_dbtp': r1(A.true_peak(x)), 'lufs_momentary_max': r1(A.lufs_m(x, SR)), 'small_speaker_lufs_m': r1(A.lufs_m(A.hp(x, 200, SR, 4), SR)),
         'centroid_hz': cen, 'bands_db_lt200_lt1k_lt4k_hi': bands, 'clipped_samples': int((np.abs(mono(x)) >= 32767 / 32768).sum())}
    if loop: d['lufs_integrated'] = r1(A.lufs_i(x, SR)); d['seam'] = A.seam(x, SR)
    else:
        d.update(ends(x)); k = A.clear_of(x, SR, bed, SR, BUS[cue]); k['loudness_margin_lu'] = r1(A.lufs_m(x, SR) + BUS[cue] - A.lufs_i(bed, SR)); d['clear_of_bed_and_roll'] = k
        if cue != 'shot': d['clear_of_gunfire'] = A.clear_of(x, SR, fire, SR, BUS[cue])
    def sh(y): _, b = A.third_octaves(A.loudest(y, SR, 0.25), SR); return b - b.mean()
    refs = {r: rd(PACK / f'{r}.wav') for r in REF[cue]}
    d['vs_pack'] = {r: {'shape_distance_db': shape_dist(x, y) if cue == 'shot' else r1(np.sqrt(((sh(x) - sh(y)) ** 2).mean())), 'lufs': r1(A.lufs_i(y, SR) if loop else A.lufs_m(y, SR)),
                        'centroid_hz': A.bands(y, SR)[1]} for r, y in refs.items()}
    if cue == 'shot': d['decay_20db_s'] = decay20(x)
    if cue == 'siren': d['sweep'] = pitch_track(x)
    return d


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    led = {Path(r['file']).name: r for r in map(json.loads, LEDGER.read_text().splitlines()) if r.get('file')}
    bed = rd(PACK / 'street_bed_even.wav'); roll = rd(DRAFTS / 'robot_roll.wav'); hum = rd(DRAFTS / 'robot_hum.wav')
    if bed.shape[1] == 2: roll, hum = np.repeat(roll, 2, axis=1), np.repeat(hum, 2, axis=1)
    under = bed + np.resize(roll, bed.shape) * 10 ** (-3 / 20) + np.resize(hum, bed.shape) * 10 ** (-3 / 20)   # as robot_measure.py: the robot rolling near
    f = np.zeros((int(6 * SR), 1)); pist = mono(rd(PACK / 'fire_pistol.wav')).reshape(-1, 1)
    for s in np.arange(0, 4.5, 0.75): i = int(s * SR); k = min(len(pist), len(f) - i); f[i:i + k] += pist[:k] * 10 ** (-3 / 20)   # steady pistol fire on the fight bus
    res = {'note': 'Sums only, from the WAVs. Chime cannot hear: Sky\'s ears decide. Mastered as briefs/087/chime/build.py; measured as make/robot_measure.py (bed + roll = street_bed_even with the '
                   'robot rolling near; gunfire = the cell pistol every 0.75 s on the fight bus). vs_pack: third-octave shape distance (dB) of the loudest 250 ms; the pack\'s closest gun pair is 4.8.',
           'clips': {}, 'drafts': {}, 'spend': {}}
    for name, (cue, take, *_r) in C.items():
        y = make(name); p = OUT / f'{name}.wav'; A.write_wav(p, y, SR); r = led.get(take) or {}
        m = measure(y, cue, bool(C[name][7]), under, f)
        m.update({'shaping': SHAPE.get(name, {}), 'cue': cue, 'take': f'paid/104-robot-sfx/takes/{take}', 'ledger_id': r.get('id'), 'model': r.get('model'), 'cost_usd': r.get('cost_usd'), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()})
        res['clips'][name] = m
    for cue, dn in DRAFT.items():
        res['drafts'][dn] = measure(rd(DRAFTS / f'{dn}.wav'), cue, cue in ('roll', 'hum'), under, f)
    rows = list(map(json.loads, LEDGER.read_text().splitlines())); calls = [r for r in rows if r.get('task') in ('audio',)]
    by = {}
    for r in calls: by.setdefault(r['model'], [0, 0.0]); by[r['model']][0] += 1; by[r['model']][1] += r.get('cost_usd') or 0
    res['spend'] = {'calls': len(calls), 'ok': sum(r['status'] == 'ok' for r in calls), 'usd': round(sum(r.get('cost_usd') or 0 for r in calls), 4), 'budget_usd': 3.0,
                    'by_model': {k: {'calls': n, 'usd': round(u, 4)} for k, (n, u) in by.items()}}
    (CH / 'paid-measures.json').write_text(json.dumps(res, indent=1) + '\n')
    for k, v in {**res['clips'], **res['drafts']}.items():
        lv = f"I {v['lufs_integrated']:6.1f} seam {v['seam']['step_ratio']}/{v['seam']['level_jump_db']}" if 'seam' in v else f"M {v['lufs_momentary_max']:6.1f} head {v['head_5ms_db']:6.1f} tail {v['tail_20ms_db']:6.1f} floor {v['floor_p10_db']}"
        vs = ' '.join(f"{r}:{q['shape_distance_db']}" for r, q in v['vs_pack'].items()); c = v.get('clear_of_bed_and_roll') or {}; g = v.get('clear_of_gunfire') or {}
        print(f"{k:18} {v['seconds']:5.2f}s tp {v['true_peak_dbtp']:5.1f} {lv} ss {v['small_speaker_lufs_m']:6.1f} cen {v['centroid_hz']:5d} clip {v['clipped_samples']} | {vs}"
              + (f" | bed {c.get('margin_top3_db')} fire {g.get('margin_top3_db')}" if c else '') + (f" sweep {v['sweep']}" if 'sweep' in v else '') + (f" dec {v['decay_20db_s']}" if 'decay_20db_s' in v else ''))
    print(json.dumps(res['spend']))


if __name__ == '__main__' and 'preview' not in sys.argv[1:]:
    main()


# ---------------------------------------------------------------- the 17 s listening aid with the paid picks (same timeline as robot_synth.scene, so the two previews compare)
PICKS = {'siren': 'siren_yelp_t3', 'shot': 'shot_slide_t1', 'draw': 'draw_hatch_t1', 'chirp': 'chirp_alert_t1', 'roll': 'roll_ball_t2'}   # the hum and the curious chirp stay synth (no paid take passed)


def preview():
    """make/demo_motion.py's beats: roll 0-5 s, curious 5.93, alert chirp 8.6 and the siren 8.75, the draw from 10.4 (latch on the panel at 10.5, lock just before shot 1 at 11.67),
    two more shots 1.5 s apart (the rulebook's rate), the roll away from 15.6. Clip levels as mastered, no buses, no bed, -1.5 dB overall, as the synth preview."""
    def at(o, x, s): i = int(s * SR); k = min(len(x), len(o) - i); o[i:i + k] += x[:k]
    pk = lambda n: A.read_wav(OUT / f'{PICKS[n]}.wav')[0][:, 0]; dr = lambda n: A.read_wav(DRAFTS / f'{n}.wav')[0][:, 0]
    o = np.zeros(int(17.0 * SR)); o += np.resize(dr('robot_hum'), len(o))
    roll = pk('roll'); rl = roll[:int(5.4 * SR)].copy(); e = np.ones(len(rl)); k = int(0.6 * SR); e[-k:] = np.linspace(1, 0, k) ** 2; e[:int(0.3 * SR)] = np.linspace(0, 1, int(0.3 * SR)); at(o, rl * e, 0.0)
    rl2 = roll[int(6 * SR):int(7.4 * SR)] * np.linspace(0, 1, int(1.4 * SR)) ** 0.5; at(o, rl2, 15.6)
    at(o, dr('robot_chirp_curious'), 5.93); at(o, pk('chirp'), 8.6); at(o, pk('siren'), 8.75); at(o, pk('draw'), 10.4)
    for s in (11.67, 13.17, 14.67): at(o, pk('shot'), s)
    y = A.fade((o * 10 ** (-1.5 / 20)).reshape(-1, 1), SR, 0.0, 0.6); p = OUT / 'robot_beat_preview_paid.wav'; A.write_wav(p, y, SR)
    print(p.relative_to(REPO), f'{len(y) / SR:.1f} s tp {A.true_peak(y):.1f} M {A.lufs_m(y, SR):.1f}')
    return {'file': str(p.relative_to(REPO)), 'picks': PICKS, 'true_peak_dbtp': r1(A.true_peak(y)), 'lufs_momentary_max': r1(A.lufs_m(y, SR)), 'clipped_samples': int((np.abs(y) >= 32767 / 32768).sum())}


if __name__ == '__main__' and 'preview' in sys.argv[1:]:
    m = json.loads((CH / 'paid-measures.json').read_text()); m['preview'] = preview(); (CH / 'paid-measures.json').write_text(json.dumps(m, indent=1) + '\n')
