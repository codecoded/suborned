"""Brief 087 (Chime): measures the pack as a mix, without Unity: renders a scripted minute at the bus gains (the reel's last section), the worst pile-up the voice caps allow, how far apart the weapon voices are, and each danger cue over the bed + patrol and over gunfire.
Writes briefs/087/mix.json and out/scratch/chime/mix.wav. Run from the repo root with the scratch venv's python -I."""
import json, sys
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]
sys.path.insert(0, str(HERE))
import audiolib as A
from pack import CUES, BUSES
AUD = REPO / 'unity/Assets/Suborned/Resources/Audio/087'; SR = 48000; G = {c['id']: c['group'] for c in CUES}


def clip(id):
    x, sr = A.read_wav(AUD / f'{id}.wav'); return A.resample(x, sr, SR)


def gain(id, duck=0.0): return 10 ** ((BUSES[G[id]]['db'] + duck) / 20)


def put(mix, id, at, pan=0.0, db=0.0):
    x = clip(id); x = x if x.shape[1] == 2 else np.repeat(x, 2, axis=1) * np.array([np.cos((pan + 1) * np.pi / 4), np.sin((pan + 1) * np.pi / 4)]) * 2 ** .5
    i = int(at * SR); n = min(len(x), len(mix) - i)
    if n > 0: mix[i:i + n] += x[:n] * gain(id) * 10 ** (db / 20)


def lay_loop(mix, id, a, b, pan=0.0, db=0.0, env=None):
    x = clip(id); x = x if x.shape[1] == 2 else np.repeat(x, 2, axis=1) * np.array([np.cos((pan + 1) * np.pi / 4), np.sin((pan + 1) * np.pi / 4)]) * 2 ** .5
    n = int((b - a) * SR); y = np.resize(x, (n, 2)) * gain(id) * 10 ** (db / 20)
    k = min(n // 2, int(0.15 * SR)); r = np.linspace(0, 1, k).reshape(-1, 1); y[:k] *= r; y[-k:] *= r[::-1]
    mix[int(a * SR):int(a * SR) + n] += y


SCENES = [
    # (start s, on-screen line)
    (0, 'Calm street: bed, a patrol passing at 20 m'), (6, 'Guard reaches the cafe: bell'), (9, 'Guard leaves: door'), (13, 'Surveillance click, near'),
    (16, 'A watcher notices'), (19, 'It stays wary'), (22, 'It calls it in'), (25, 'Patrol breaks route, its sound changes'), (29, 'The desk sends the police, from the left'),
    (34, 'Fight: Fixer, Enforcer, a rival, a police officer, the robot'), (46, 'Two go down; firing stops'), (49, 'Conversation: cut in, the street ducks, room tone'), (55, 'Cut out; New card'), (58, 'Handover chime')]


def scene():
    mix = np.zeros((int(66 * SR), 2))
    # the bed runs on, ducked by 14 dB in the cutaway
    x = clip('street_bed'); y = np.resize(x, (len(mix), 2)) * gain('street_bed'); e = np.ones(len(mix)); a, b = int(49.12 * SR), int(55.0 * SR); e[a:b] = 10 ** (-14 / 20); k = int(.12 * SR); e[a - k:a] = np.linspace(1, 10 ** (-14 / 20), k); kk = int(.4 * SR); e[b:b + kk] = np.linspace(10 ** (-14 / 20), 1, kk); mix += y * e.reshape(-1, 1)
    lay_loop(mix, 'patrol_loop', 0, 25, pan=-.3, db=-4); lay_loop(mix, 'patrol_chase', 25, 49, pan=-.25, db=-2)
    lay_loop(mix, 'room_tone', 49.1, 55.0)
    put(mix, 'cafe_bell', 6.0, .3); put(mix, 'cafe_door', 9.0, .3); put(mix, 'camera_click', 13.0, .4); put(mix, 'camera_click', 14.5, .4)
    put(mix, 'notice_1', 16); put(mix, 'notice_2', 19); put(mix, 'notice_3', 22); put(mix, 'patrol_break', 25, -.25); put(mix, 'police_siren', 29, -.8)
    t = 34.0
    while t < 46:  # weapons at their rates (pistol 1.5 s, carbine 2 s), offset by the view's small stagger
        put(mix, 'fire_pistol', t, -.2, -3); t += 1.5
    for t in np.arange(34.3, 46, 2.0): put(mix, 'fire_carbine', t, .1, -2)
    for t in np.arange(34.8, 44, 1.5): put(mix, 'fire_rival', t, .5, -5)
    for t in np.arange(35.1, 44, 1.5): put(mix, 'fire_police', t, -.6, -6)
    for t in np.arange(35.6, 44, 3.0): put(mix, 'fire_robot', t, -.3, -5)
    for t, h in [(34.4, 'hit_body'), (36.0, 'miss'), (37.2, 'hit_body'), (38.4, 'miss'), (39.3, 'hit_metal'), (40.5, 'miss'), (41.2, 'hit_body'), (42.7, 'miss')]: put(mix, h, t, .2)
    put(mix, 'down_body', 45.0, .4); put(mix, 'down_robot', 46.2, -.3)
    put(mix, 'cut_in', 49.0); put(mix, 'cut_out', 54.95); put(mix, 'card_new', 55.6); put(mix, 'handover_chime', 58.0)
    return mix


def main():
    res = {'note': 'Sums only. The running game is measured by Latch and Suture when it is wired (brief 087 done-check). None of this says how anything sounds.'}
    # ---- the weapon voices: a third-octave shape distance (level-matched, dB RMS across 63 Hz-12.5 kHz) between each pair's first 250 ms
    fires = ['fire_pistol', 'fire_carbine', 'fire_police', 'fire_rival', 'fire_robot']; sh = {}; res['weapon_voices'] = {}
    for f in fires:
        x = clip(f); _, b = A.third_octaves(x[:int(.25 * SR)], SR); sh[f] = b - b.mean()
        n = int(.02 * SR); m = x.mean(axis=1); e = np.sqrt(np.convolve(m ** 2, np.ones(n) / n, 'same')); p = e.argmax(); dec = np.where(e[p:] < e[p] * 10 ** (-20 / 20))[0]
        res['weapon_voices'][f] = {'decay_20db_s': round(float(dec[0] / SR), 3) if len(dec) else None, 'lufs_m': round(A.lufs_m(x, SR), 1)}
    res['weapon_pairs_shape_distance_db'] = {f'{a} / {b}': round(float(np.sqrt(((sh[a] - sh[b]) ** 2).mean())), 1) for i, a in enumerate(fires) for b in fires[i + 1:]}
    # ---- danger over what plays under it
    bed = clip('street_bed'); pat = np.resize(clip('patrol_loop'), (len(bed), 2)) * 10 ** (-3 / 20); under = bed + pat; ful = under.copy()
    dang = ['notice_1', 'notice_2', 'notice_3', 'camera_click', 'patrol_break', 'police_siren', 'cafe_bell', 'cafe_door', 'handover_chime', 'card_new']
    res['danger_over_bed_and_near_patrol'] = {}; base_i = A.lufs_i(under, SR)
    for d in dang:
        x = clip(d); k = A.clear_of(x, SR, under, SR, BUSES[G[d]]['db']); k['loudness_margin_lu'] = round(A.lufs_m(x, SR) + BUSES[G[d]]['db'] - base_i, 1); res['danger_over_bed_and_near_patrol'][d] = k
    fire = np.zeros((int(6 * SR), 2)); [fire.__setitem__(slice(int(t * SR), int(t * SR) + len(clip('fire_pistol'))), fire[int(t * SR):int(t * SR) + len(clip('fire_pistol'))] + np.repeat(clip('fire_pistol'), 2, axis=1) * gain('fire_pistol')) for t in np.arange(0, 4.5, 1.5)]
    res['danger_over_gunfire'] = {d: A.clear_of(clip(d), SR, fire, SR, BUSES[G[d]]['db']) for d in ['notice_1', 'notice_3', 'police_siren', 'handover_chime']}
    # ---- the worst pile-up the voice caps allow, every peak on one sample at full gain, nearest distance
    caps = {c['id']: c['voices'] for c in CUES}; worst = sum(caps[f] * 10 ** (A.sample_peak(clip(f)) / 20) * gain(f) for f in fires + ['hit_body', 'hit_metal', 'miss', 'down_body', 'down_robot', 'notice_3'])
    res['worst_stack_linear_peak'] = round(worst, 2); res['worst_stack_dbfs'] = round(20 * np.log10(worst), 1)
    # ---- the scripted minute
    m = scene(); A.write_wav(REPO / 'out/scratch/chime/mix.wav', np.clip(m, -1, 1), SR)
    res['scripted_minute'] = {'seconds': len(m) / SR, 'lufs_integrated': round(A.lufs_i(m, SR), 1), 'true_peak_db': round(A.true_peak(m), 1), 'clipped_samples_before_limit': int((np.abs(m) >= 1).sum()),
                              'calm_0_15s_lufs': round(A.lufs_i(m[:15 * SR], SR), 1), 'fight_34_46s_lufs': round(A.lufs_i(m[34 * SR:46 * SR], SR), 1), 'cutaway_50_54s_lufs': round(A.lufs_i(m[50 * SR:54 * SR], SR), 1), 'chime_58_63s_lufs_m': round(A.lufs_m(m[58 * SR:63 * SR], SR), 1)}
    (REPO / 'briefs/087/mix.json').write_text(json.dumps(res, indent=1) + '\n'); print(json.dumps(res, indent=0)[:6000])


if __name__ == '__main__': main()
