"""Brief 087 (Chime): the car cues measured as a mix, through a copy of the game's master (AudioMaster: -6 dB, 4:1, 5 ms, 100 ms, hard stop at 0 dBFS), and the short reel for Sky's phone. Adds `cars` and `in_game_0_2_211` to briefs/087/mix.json; writes briefs/087/reel-cars.mp3 and .mp4. Run from the repo root with the scratch venv's python -I."""
import json, subprocess, sys
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]
sys.path.insert(0, str(HERE))
import audiolib as A, mix as M, reel as R
from pack import CUES, BUSES
SR = 48000; LEN = 20.0; T_HIT, T_DIS, T_FIRE, T_BLAST = 0.5, 2.0, 4.5, 10.5
START = 5  # the burn loop starts here (s): a steady stretch, so the ramp reads as a rise
WARN = [(3.0, 0), (2.0, 2), (1.0, 4), (0.5, 6)]  # seconds before the blast, semitones up


def st(x, pan=0.0): return x if x.shape[1] == 2 else np.repeat(x, 2, axis=1) * np.array([np.cos((pan + 1) * np.pi / 4), np.sin((pan + 1) * np.pi / 4)]) * 2 ** .5


def burn_ramp(s=6.0, up=3.0, gain_db=6.0):
    """The loop as the host plays it: pitch rising `up` semitones and level `gain_db` over the burn, cut off at the blast (the shape, not a bake: the host does this with AudioSource.pitch and volume)."""
    x = np.roll(M.clip('car_burn')[:, 0], -START * SR); n = int(s * SR); rate = 2 ** (np.linspace(0, 1, n) ** 1.5 * up / 12); pos = np.cumsum(rate); y = np.interp(pos, np.arange(len(x)), x) * 10 ** (np.linspace(0, 1, n) ** 1.5 * gain_db / 20) * 10 ** (BUSES['danger']['db'] / 20); return y.reshape(-1, 1)


def warn(semis):
    x = M.clip('car_warn'); return A.resample(x, int(round(SR * 2 ** (semis / 12))), SR)


def lay(buses, bus, x, at, pan=0.0, db=0.0):
    i = int(at * SR); y = st(x, pan) * 10 ** ((BUSES[bus]['db'] + db) / 20); n = min(len(y), len(buses[bus]) - i)
    if n > 0: buses[bus][i:i + n] += y[:n]


def scene(fight=True):
    """A car shot out in a firefight: bed, shots, hits on the car, disabled, burning with its ticks, the blast, the wreck. Buses kept apart so the duck and Options can act on them."""
    N = int(LEN * SR); b = {k: np.zeros((N, 2)) for k in ('ambience', 'fight', 'danger', 'blast')}; b['ambience'] += np.resize(M.clip('street_bed_even'), (N, 2)) * 10 ** (BUSES['ambience']['db'] / 20)
    if fight:
        for t in np.arange(0.2, 15, 1.5): lay(b, 'fight', M.clip('fire_pistol'), t, -.3, -3)
        for t in np.arange(0.6, 15, 2.0): lay(b, 'fight', M.clip('fire_carbine'), t, .2, -2)
    for t in (T_HIT, 1.3, 2.0, 2.9, 3.7): lay(b, 'fight', M.clip('hit_metal'), t, .3, -3)
    lay(b, 'fight', M.clip('car_disabled'), T_DIS, .3); lay(b, 'fight', M.clip('car_ignite'), T_FIRE, .3); lay(b, 'danger', burn_ramp(T_BLAST - T_FIRE), T_FIRE, .3)
    for before, semi in WARN: lay(b, 'danger', warn(semi), T_BLAST - before, .3)
    lay(b, 'blast', M.clip('car_explode'), T_BLAST, .3); lay(b, 'fight', M.clip('car_burnout'), T_BLAST + 0.8, .3)
    return b


def duck_env(N, cut_db, at=T_BLAST, attack=0.005, hold=0.35, rel=0.9):
    e = np.zeros(N); a, h, r = int(at * SR), int(attack * SR), int(hold * SR); k = int(rel * SR); e[a:a + h] = np.linspace(0, cut_db, h); e[a + h:a + h + r] = cut_db; e[a + h + r:a + h + r + k] = np.linspace(cut_db, 0, k); return 10 ** (e / 20)


def master(x):
    """Sample for sample as AudioMaster: peak detector over the channels, attack 5 ms, release 100 ms, 4:1 over -6 dB, then a hard stop at 1."""
    att, rel = np.exp(-1 / (0.005 * SR)), np.exp(-1 / (0.1 * SR)); env, most, hard = 0.0, 0.0, 0; y = x.copy(); pk = np.abs(x).max(axis=1)
    for i, m in enumerate(pk):
        env = att * env + (1 - att) * m if m > env else rel * env + (1 - rel) * m
        if env > 0.501:
            db = 20 * np.log10(env); cut = (db + 6) * 0.75; most = max(most, cut); y[i] *= 10 ** (-cut / 20)
    hard = int((np.abs(y) > 1).sum()); return np.clip(y, -1, 1), {'peak_in_db': round(A.sample_peak(x), 1), 'peak_out_db': round(A.sample_peak(np.clip(y, -1, 1)), 1), 'most_taken_db': round(float(most), 1), 'hard_stops': hard}


def run(settings, duck, fight=True):
    ms, ef, am = settings; b = scene(fight); N = len(b['fight'])
    fd, dd, ad = (duck_env(N, -6).reshape(-1, 1), duck_env(N, -3).reshape(-1, 1), duck_env(N, -4).reshape(-1, 1)) if duck else (1, 1, 1)
    s = (b['fight'] * fd + b['danger'] * dd + b['blast']) * ef + b['ambience'] * ad * am; s = s * ms; y, stats = master(s); return s, y, stats


def main():
    res = {'note': 'Sums through a copy of AudioMaster, not the running game. Scene: a car shot out in a firefight (two guns, five hits), burn 6 s, blast, wreck.', 'settings': {}}
    for name, st_ in (('defaults_today_master80_effects80_ambience60', (.8, .8, .6)), ('proposed_master80_effects100_ambience100', (.8, 1, 1)), ('all_100', (1, 1, 1))):
        for duck in (False, True):
            s, y, m = run(st_, duck); a = int(T_BLAST * SR); m.update({'fight_before_blast_lufs': round(A.lufs_i(y[int(4.5 * SR):a], SR), 1), 'blast_lufs_m': round(A.lufs_m(y[a:a + 2 * SR], SR), 1), 'wreck_13_17s_lufs': round(A.lufs_i(y[int(16 * SR):int(19.5 * SR)], SR), 1)}); res['settings'][name + ('_duck' if duck else '_no_duck')] = m
    # the blast alone, and the burn's readability under fire
    res['blast_alone_at_defaults_peak_db'] = round(A.sample_peak(M.clip('car_explode') * .8 * .8), 1)
    pistol = np.zeros((int(6 * SR), 2)); [pistol.__setitem__(slice(int(t * SR), int(t * SR) + len(M.clip('fire_pistol'))), pistol[int(t * SR):int(t * SR) + len(M.clip('fire_pistol'))] + st(M.clip('fire_pistol')) * M.gain('fire_pistol')) for t in np.arange(0, 4.5, 1.5)]
    bed = M.clip('street_bed_even')
    res['over_bed'] = {k: A.clear_of(M.clip(k), SR, bed, SR, BUSES[M.G[k]]['db']) for k in ('car_disabled', 'car_ignite', 'car_warn', 'car_explode')}
    res['over_steady_pistols'] = {k: A.clear_of(M.clip(k), SR, pistol, SR, BUSES[M.G[k]]['db']) for k in ('car_disabled', 'car_ignite', 'car_warn', 'car_explode')}
    res['burn_loop_end_vs_start_lufs'] = {'first_2s': round(A.lufs_i(burn_ramp()[:2 * SR], SR), 1), 'last_2s': round(A.lufs_i(burn_ramp()[-2 * SR:], SR), 1), 'pitch_up_semitones': 3}
    # the beds' swing: why the game's bed reads low
    out = {}
    for id in ('street_bed', 'street_bed_even'):
        x = M.clip(id); L = [A.lufs_i(x[i:i + 4 * SR], SR) for i in range(0, len(x) - 4 * SR, SR)]; out[id] = {'integrated': round(A.lufs_i(x, SR), 1), 'loudest_4s': round(max(L), 1), 'quietest_4s': round(min(L), 1), 'swing_lu': round(max(L) - min(L), 1)}
    res['bed_swing'] = out
    d = json.loads((REPO / 'briefs/087/mix.json').read_text()); d['cars'] = res
    db = lambda v: round(20 * np.log10(v), 1)
    d['in_game_0_2_211'] = {'measured_by_latch': {'bed_lufs': -48, 'help_scene_lufs': -49, 'fight_lufs': -29, 'ambience_default': 60},
        'capture_checked_by_chime': 'briefs/087/latch/r087-seam-street_bed.wav reads -47.8 LUFS over 4 s at the loop point, where the clip itself reads -35.1. -12.7 dB in all.',
        'that_gap_adds_up_as': {'master_80_db': db(.8), 'ambience_60_db': db(.6), 'paused_half_db': db(.5), 'sum_db': db(.8 * .6 * .5)},
        'inference': 'The capture ran paused (PackLoops halves the bed when paused and sets its volume every frame, over PackSoloLoop\'s 1.0). If -48 came from this capture, the bed in play is about -42 at the loop point, about -44.3 overall at defaults, and about -50 in the quiet half of street_bed.',
        'seam_note': 'r087-seam-room_tone.wav has the bed\'s spectrum, not room tone\'s (share below 200 Hz -4.4 dB against the clip\'s -2.2; 1-4 kHz -10.9 against -18.6): PackLoops seems to have restarted the bed during the solo. So room_tone\'s seam has not been checked after Vorbis.'}
    (REPO / 'briefs/087/mix.json').write_text(json.dumps(d, indent=1) + '\n'); print(json.dumps(res, indent=0)[:5000])
    # ---- the reel
    N = 0; audio = []; segs = []
    def seg(head, title, sub, a, wave=None, foot=''):
        nonlocal N
        p = R.TMP / f'car{N:03d}.png'; N += 1; R.card(p, head, title, sub, wave, foot); segs.append((p, len(a) / SR)); audio.append(a)
    seg('Brief 087', 'Cars: burning and blowing', 'Each sound named, played twice, then a car shot out in a firefight.', np.concatenate([R.sil(.3), R.speak('Cars. Each sound named and played twice. Then a car shot out in a firefight.'), R.sil(.4)]), foot='Chime cannot hear these: your ears decide.')
    rows = {c['id']: c for c in CUES}
    for id, twice in (('car_disabled', 1), ('car_ignite', 1), ('car_warn', 1), ('car_explode', 1), ('car_burnout', 0)):
        c = rows[id]; x = st(M.clip(id)) * M.gain(id); body = np.concatenate([R.sil(.25), x, R.sil(.7), x, R.sil(.6)]) if twice else np.concatenate([R.sil(.25), x, R.sil(.5)])
        seg('Cars', c['label'], c['trigger'], np.concatenate([R.speak(c['label']), body]), wave=x, foot=id)
    b = st(burn_ramp()); w = np.zeros_like(b)
    for before, semi in WARN:
        i = int((6 - before) * SR); y = st(warn(semi)); n = min(len(y), len(w) - i); w[i:i + n] += y[:n]
    seg('Cars', 'Burning, the 6 s countdown', 'The loop rises 3 semitones and 6 dB; ticks at 3, 2, 1 and a half second. Cut at the blast.', np.concatenate([R.speak('Burning, the six second countdown. It rises, and ticks at the end.'), R.sil(.3), b + w, R.sil(.8)]), wave=b, foot='car_burn + car_warn')
    _, y, _ = run((.8, 1, 1), True); beats = [(0, 'A firefight: shots hit the car'), (T_DIS, 'Its engine dies'), (T_FIRE, 'It catches fire, and burns'), (T_BLAST - 3, 'Ticks: 3, 2, 1'), (T_BLAST, 'The blast: everything else dips'), (T_BLAST + 1.5, 'The wreck dies down'), (LEN, '')]
    seg('Together', 'A car shot out in a firefight', 'At the mix levels, Master 80, Effects and Ambience 100, through the game\'s compressor. Timing is mine.', np.concatenate([R.speak('Now all together.'), R.sil(.3)]))
    for (a, txt), (e, _) in zip(beats, beats[1:]): seg('Together', txt, f'{a:.1f} s', y[int(a * SR):int(e * SR)])
    a = np.concatenate(audio); a = a * min(1.0, 10 ** (-1 / 20) / np.abs(a).max()); wav = R.TMP / 'reel-cars.wav'; A.write_wav(wav, a, SR); print(f'reel {len(a) / SR:.1f} s, peak {A.sample_peak(a):.1f} dBFS, LUFS-I {A.lufs_i(a, SR):.1f}')
    lst = R.TMP / 'cards-cars.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d:.3f}\n" for p, d in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(wav), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / 'briefs/087/reel-cars.mp3')], check=True)
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(wav), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-shortest', '-movflags', '+faststart', str(REPO / 'briefs/087/reel-cars.mp4')], check=True, cwd=str(R.TMP))


main()
