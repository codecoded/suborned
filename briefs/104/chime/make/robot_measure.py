"""Brief 104 (Chime): measures the robot drafts against the pack they would join (brief 087). Sums only: none of this says how anything sounds.
Writes briefs/104/chime/measures.json. Run from the repo root with the scratch venv:
   out/scratch/chime/venv/bin/python -I briefs/104/chime/make/robot_measure.py
Per draft: length, sample and true peak, momentary-max and (loops) integrated loudness, small-speaker loudness, centroid and band shares,
the loop seam, and the masking margin over the street bed with the robot's own roll near. Then the questions the design asks:
is the shot its own voice among the weapons, is the siren apart from the police siren, does the draw carry over a fight."""
import hashlib, json, sys
from pathlib import Path
import numpy as np

HERE = Path(__file__).resolve().parent; REPO = HERE.parents[3]
sys.path.insert(0, str(REPO / 'briefs/087/chime'))
import audiolib as A  # noqa: E402

SR = 48000
DRAFTS = HERE.parent / 'drafts'; PACK = REPO / 'unity/Assets/Suborned/Resources/Audio/087'
# the bus each draft would sit on (briefs/087/cues.json buses): the draw, siren and chirps are danger (0 dB), the shot fight (-3), the loops vehicles (-3)
BUS_DB = {'robot_draw': 0, 'robot_siren': 0, 'robot_chirp_alert': 0, 'robot_chirp_curious': 0, 'robot_shot': -3, 'robot_roll': -3, 'robot_hum': -3}
LOOPS = {'robot_roll', 'robot_hum'}


def mono(x): return x.reshape(len(x), -1).mean(axis=1)
def rd(p):
    x, sr = A.read_wav(p); return A.resample(x, sr, SR)
def r1(v): return round(float(v), 1)


def shape(x):
    """The third-octave shape of a shot's first 250 ms, level-matched (as briefs/087/chime/mix.py)."""
    _, b = A.third_octaves(x[:int(.25 * SR)], SR); return b - b.mean()
def shape_dist(a, b): return r1(np.sqrt(((shape(a) - shape(b)) ** 2).mean()))
def decay20(x):
    n = int(.02 * SR); m = mono(x); e = np.sqrt(np.convolve(m ** 2, np.ones(n) / n, 'same')); p = e.argmax(); d = np.where(e[p:] < e[p] * 0.1)[0]
    return round(float(d[0] / SR), 3) if len(d) else None
def pitch_track(x, lo=150, hi=2500):
    """The strongest frequency every 20 ms between lo and hi (a siren's sweep): min, max and the span in octaves."""
    m = mono(x); n = 4096; h = int(.02 * SR); w = np.hanning(n); f = np.fft.rfftfreq(n, 1 / SR); sel = (f >= lo) & (f <= hi); out = []
    for i in range(0, len(m) - n, h):
        s = np.abs(np.fft.rfft(m[i:i + n] * w)); e = (m[i:i + n] ** 2).mean()
        if e > 1e-6: out.append(f[sel][np.argmax(s[sel])])
    out = np.array(out); return {'min_hz': round(float(np.percentile(out, 5))), 'max_hz': round(float(np.percentile(out, 95))), 'span_octaves': round(float(np.log2(np.percentile(out, 95) / np.percentile(out, 5))), 2)}


def main():
    bed = rd(PACK / 'street_bed_even.wav'); roll = rd(DRAFTS / 'robot_roll.wav'); hum = rd(DRAFTS / 'robot_hum.wav')
    if bed.shape[1] == 2 and roll.shape[1] == 1: roll = np.repeat(roll, 2, axis=1); hum = np.repeat(hum, 2, axis=1)
    near = np.resize(roll, bed.shape) * 10 ** (-3 / 20) + np.resize(hum, bed.shape) * 10 ** (-3 / 20)   # the robot rolling at its min distance
    under = bed + near; under_i = A.lufs_i(under, SR); bed_i = A.lufs_i(bed, SR)
    # steady pistol fire, one shot every 1.5 s at the fight bus (as mix.py's danger_over_gunfire), with the robot's own shots in it
    def volley(ids):
        f = np.zeros((int(6 * SR), 1))
        for j, s in enumerate(np.arange(0, 4.5, 0.75)):
            x = mono(rd(PACK / f'{ids[j % len(ids)]}.wav' if not ids[j % len(ids)].startswith('robot') else DRAFTS / f'{ids[j % len(ids)]}.wav')).reshape(-1, 1)
            i = int(s * SR); k = min(len(x), len(f) - i); f[i:i + k] += x[:k] * 10 ** (-3 / 20)
        return f
    fire = volley(['fire_pistol', 'robot_shot'])
    res = {'note': 'Sums only, from the WAVs. Chime cannot hear: Sky\'s ears decide. Under = street_bed_even + this robot\'s roll and hum at -3 dB (min distance). '
                   'Gunfire = the cell\'s pistol and the robot\'s shot alternating every 0.75 s on the fight bus (-3 dB).',
           'bed_lufs_i': r1(bed_i), 'under_lufs_i': r1(under_i), 'drafts': {}}
    for p in sorted(DRAFTS.glob('robot_*.wav')):
        name = p.stem; x = rd(p); m = mono(x); loop = name in LOOPS; bands, cen = A.bands(x, SR)
        d = {'seconds': round(len(x) / SR, 3), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest(), 'sample_peak_dbfs': r1(A.sample_peak(x)), 'true_peak_dbtp': r1(A.true_peak(x)),
             'lufs_momentary_max': r1(A.lufs_m(x, SR)), 'small_speaker_lufs_m': r1(A.lufs_m(A.hp(x, 200, SR, 4), SR)), 'centroid_hz': cen, 'bands_db_lt200_lt1k_lt4k_hi': bands,
             'clipped_samples': int((np.abs(m) >= 32767 / 32768).sum())}
        if name == 'robot_beat_preview': d['use'] = 'listening aid only (the demo timeline at the drafts\' levels), not a cue'; res['drafts'][name] = d; continue
        d['bus_db'] = BUS_DB[name]
        if loop: d['lufs_integrated'] = r1(A.lufs_i(x, SR)); d['seam'] = A.seam(x, SR); d['over_bed_lu'] = r1(A.lufs_i(x, SR) + BUS_DB[name] - bed_i)
        else:
            k = A.clear_of(x, SR, under, SR, BUS_DB[name]); k['loudness_margin_lu'] = r1(A.lufs_m(x, SR) + BUS_DB[name] - under_i); d['clear_of_bed_and_roll'] = k
            if name != 'robot_shot': d['clear_of_gunfire'] = A.clear_of(x, SR, fire, SR, BUS_DB[name])
        res['drafts'][name] = d
    # the draw's 'ready' tones alone (0.98-1.16 s): the part meant to survive a fight and a laptop speaker
    tones = rd(DRAFTS / 'robot_draw.wav')[int(0.98 * SR):int(1.16 * SR)]
    res['draw_ready_tones'] = {'lufs_m': r1(A.lufs_m(tones, SR)), 'over_bed': A.clear_of(tones, SR, bed, SR, 0), 'over_gunfire': A.clear_of(tones, SR, fire, SR, 0)}
    # the shot among the weapon voices
    fires = ['fire_pistol', 'fire_carbine', 'fire_police', 'fire_rival', 'fire_robot']; shot = rd(DRAFTS / 'robot_shot.wav')
    res['robot_shot_vs_weapons'] = {'shape_distance_db': {f: shape_dist(shot, rd(PACK / f'{f}.wav')) for f in fires},
                                    'decay_20db_s': {'robot_shot': decay20(shot), **{f: decay20(rd(PACK / f'{f}.wav')) for f in fires}},
                                    'lufs_m': {'robot_shot': r1(A.lufs_m(shot, SR)), **{f: r1(A.lufs_m(rd(PACK / f'{f}.wav'), SR)) for f in fires}},
                                    'note': 'The pack\'s closest existing pair is fire_pistol / fire_rival at 4.8 dB (briefs/087/mix.json).'}
    # one cue per meaning: the new cues against the old ones they could be mistaken for
    pairs = [('robot_siren', 'police_siren'), ('robot_siren', 'patrol_break'), ('robot_draw', 'patrol_break'), ('robot_draw', 'camera_click'),
             ('robot_chirp_alert', 'notice_1'), ('robot_chirp_alert', 'notice_3'), ('robot_chirp_curious', 'notice_1'), ('robot_chirp_alert', 'card_new')]
    def sh_all(x): _, b = A.third_octaves(A.loudest(x, SR, 0.25), SR); return b - b.mean()
    res['apart_from_existing'] = {f'{a} / {b}': {'shape_distance_db': r1(np.sqrt(((sh_all(rd(DRAFTS / f'{a}.wav')) - sh_all(rd(PACK / f'{b}.wav'))) ** 2).mean())),
                                                 'centroid_hz': [A.bands(rd(DRAFTS / f'{a}.wav'), SR)[1], A.bands(rd(PACK / f'{b}.wav'), SR)[1]],
                                                 'seconds': [round(len(rd(DRAFTS / f'{a}.wav')) / SR, 2), round(len(rd(PACK / f'{b}.wav')) / SR, 2)]} for a, b in pairs}
    res['siren_sweeps'] = {'robot_siren': pitch_track(rd(DRAFTS / 'robot_siren.wav')), 'police_siren': pitch_track(rd(PACK / 'police_siren.wav'))}
    # the old walking loop against the new roll: what changes if the roll replaces patrol_loop for this robot
    pl = rd(PACK / 'patrol_loop.wav'); res['roll_vs_patrol_loop'] = {'patrol_loop_lufs_i': r1(A.lufs_i(pl, SR)), 'patrol_loop_centroid_hz': A.bands(pl, SR)[1],
                                                                       'robot_roll_lufs_i': res['drafts']['robot_roll']['lufs_integrated'], 'robot_roll_centroid_hz': res['drafts']['robot_roll']['centroid_hz']}
    out = HERE.parent / 'measures.json'; out.write_text(json.dumps(res, indent=1) + '\n')
    for k, v in res['drafts'].items():
        c = v.get('clear_of_bed_and_roll'); g = v.get('clear_of_gunfire')
        print(f"{k:22} {v['seconds']:5.2f}s tp {v['true_peak_dbtp']:6.1f} M {v['lufs_momentary_max']:6.1f} ss {v['small_speaker_lufs_m']:6.1f} cen {v['centroid_hz']:5d}"
              + (f" I {v['lufs_integrated']:6.1f} seam {v['seam']['step_ratio']} jump {v['seam']['level_jump_db']}" if 'seam' in v else '')
              + (f" | bed+roll {c['margin_top3_db']:5.1f} dB {c['loudness_margin_lu']:5.1f} LU" if c else '') + (f" | fire {g['margin_top3_db']:5.1f} dB clear {g['bands_clear_6db']}" if g else ''))
    print(json.dumps({k: res[k] for k in ('draw_ready_tones', 'robot_shot_vs_weapons', 'apart_from_existing', 'siren_sweeps', 'roll_vs_patrol_loop')}, indent=1))


if __name__ == '__main__':
    main()
