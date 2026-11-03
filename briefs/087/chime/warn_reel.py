"""Brief 087 (Chime): car_warn's three replacements: each alone, then in the burning loop's 6 s countdown over the bed. Measures each as voice-like or not, and clear of bed, fight and the burn's loud end; adds `car_warn_options` to mix.json; writes briefs/087/reel-cars-warn.mp4 + .mp3. Run from the repo root with the scratch venv's python -I."""
import json, subprocess, sys
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE))
import audiolib as A, mix as M, reel as R, cars as C, warn_measure as W
from pack import BUSES
SR = 48000; OPTS = ['car_warn_relay', 'car_warn_escape', 'car_warn_bell']; WARN = C.WARN
LAB = {'car_warn_relay': 'Option 1: metal relay', 'car_warn_escape': 'Option 2: clock escapement', 'car_warn_bell': 'Option 3: brass ping'}
def num(o): return {k: (v.item() if hasattr(v, 'item') else v) for k, v in o.items()}


def ticks(id):
    n = int(6 * SR); w = np.zeros((n, 2))
    for before, semi in WARN:
        x = A.resample(M.clip(id), int(round(SR * 2 ** (semi / 12))), SR); i = int((6 - before) * SR); y = C.st(x); k = min(len(y), n - i); w[i:i + k] += y[:k]
    return w


def main():
    bed = M.clip('street_bed_even'); pis = np.zeros((6 * SR, 2)); f = C.st(M.clip('fire_pistol')) * M.gain('fire_pistol')
    for t in np.arange(0, 4.5, 1.5): pis[int(t * SR):int(t * SR) + len(f)] += f
    burn = C.burn_ramp(); end = burn[-2 * SR:]; res = {'old_car_warn': num(W.row(M.AUD / 'car_warn.wav')), 'options': {}}
    res['diagnosis'] = 'car_warn is about ten sharp pulses 8 to 12 ms apart (a weak 118 Hz buzz, pulse strength 0.16), rising over 37 ms, its energy a broad hump from 500 Hz to 3 kHz (-1.0 dB of the clip). A buzz through a mid-band hump is how a voice or a quack is built. No downward pitch measured: glide 0.'
    audio, segs = [], []; N = 0
    def seg(h, t, sub, a, wave=None, foot=''):
        nonlocal N
        p = R.TMP / f'warn{N:02d}.png'; N += 1; R.card(p, h, t, sub, wave, foot); segs.append((p, len(a) / SR)); audio.append(a)
    seg('Car warning', 'Three replacements', 'Each alone, then in the 6 s burn countdown.', np.concatenate([R.sil(.2), R.speak('Three replacements for the car warning. Each alone, then in the six second countdown.'), R.sil(.3)]), foot='Chime cannot hear these: your ears decide.')
    for id in OPTS:
        x = M.clip(id); s = C.st(x) * M.gain(id); r = num(W.row(M.AUD / f'{id}.wav')); r.update(over_bed=A.clear_of(x, SR, bed, SR), over_steady_pistols=A.clear_of(x, SR, pis, SR), over_burn_last_2s=A.clear_of(x, SR, end, SR), lufs_m=round(A.lufs_m(x, SR), 1), true_peak=round(A.true_peak(x), 1), seconds=round(len(x) / SR, 2)); res['options'][id] = r
        print(id, {k: r[k] for k in ('active_s', 'attack_ms', 'pulse_strength', 'voiced_windows', 'glide_semitones', 'centroid_hz', 'lufs_m', 'true_peak')}, 'bed', r['over_bed']['margin_top3_db'], 'pistols', r['over_steady_pistols']['margin_top3_db'], 'burn', r['over_burn_last_2s']['margin_top3_db'])
        alone = np.concatenate([R.speak(LAB[id] + '. Alone.'), R.sil(.4), s, R.sil(.7), s, R.sil(.7), s, R.sil(.7)]); seg(LAB[id], LAB[id], 'Alone, three times.', alone, wave=s, foot=id)
        ctx = C.st(burn) + ticks(id) * M.gain(id) + np.resize(bed, (6 * SR, 2)); ctx = A.fade(ctx, SR, 0.1, 0.02); seg(LAB[id], LAB[id], 'In the countdown: ticks at 3, 2, 1 and a half second, over the burn and the bed. Then the blast.', np.concatenate([R.speak('In the countdown.'), R.sil(.3), ctx, C.st(M.clip('car_explode')[:int(1.5 * SR)]) * 10 ** (-2 / 20), R.sil(.6)]), foot=id)
    d = json.loads((REPO / 'briefs/087/mix.json').read_text()); d['car_warn_options'] = res; (REPO / 'briefs/087/mix.json').write_text(json.dumps(d, indent=1) + '\n')
    a = np.concatenate(audio); a = a * min(1.0, 10 ** (-1 / 20) / np.abs(a).max()); wav = R.TMP / 'warn.wav'; A.write_wav(wav, a, SR); print(f'reel {len(a) / SR:.1f} s, peak {A.sample_peak(a):.1f}, LUFS-I {A.lufs_i(a, SR):.1f}')
    lst = R.TMP / 'cards-warn.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d_:.3f}\n" for p, d_ in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(wav), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / 'briefs/087/reel-cars-warn.mp3')], check=True)
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(wav), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-shortest', '-movflags', '+faststart', str(REPO / 'briefs/087/reel-cars-warn.mp4')], check=True, cwd=str(R.TMP))


main()
