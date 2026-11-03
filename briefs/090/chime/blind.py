"""Brief 090 (Chime): the blind comparison for Sky's phone. Each clip trimmed of its silence and set to one level (scene and names -23 LUFS, shouts -18 LUFS momentary) so loudness doesn't pick the winner; services in a fixed shuffled order as A, B, C. Writes briefs/090/voices-blind.mp4 (+ .mp3), briefs/090/blind-key.json, briefs/090/voices-measures.json. Run from the repo root with the scratch venv's python -I."""
import hashlib, json, subprocess, sys
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(HERE)); sys.path.insert(0, str(REPO / 'briefs/087/chime'))
import audiolib as A, reel as R, lines as LN, voice_measure as VM
SR = 48000; P = REPO / 'paid/090-voices'; SV = sorted(['inworld', 'qwen', 'fish'], key=lambda s: hashlib.sha256(('090|' + s).encode()).hexdigest()); KEY = dict(zip('ABC', SV))
NAME = {'milena': 'Milena', 'inez': 'Inez', 'larthi': 'Larthi', 'arthur': 'Arthur', 'arruns': 'Arruns', 'police': 'Civic Police officer', 'rival': 'A rival', 'watcher': 'Camera desk', 'hasti': 'Hasti', 'lukac': 'Lukac', 'sethre': 'Sethre', 'popovic': 'Popovic'}


def clip(svc, stem, lu, mom=False):
    x, sr = A.read_wav(P / svc / f'{stem}.wav'); x = A.resample(x.mean(axis=1, keepdims=True), sr, SR); m = np.abs(x[:, 0]); h = int(.01 * SR); e = np.sqrt(np.convolve(m ** 2, np.ones(h) / h, 'same')); on = np.where(e > e.max() * 10 ** (-40 / 20))[0]
    x = x[max(0, on[0] - int(.06 * SR)):on[-1] + int(.12 * SR)]; x = A.fade(x, SR, .005, .04); g = lu - (A.lufs_m(x, SR) if mom else A.lufs_i(x, SR)); y = x * 10 ** (g / 20); pk = np.abs(y).max()
    return np.repeat(y * min(1, 10 ** (-1 / 20) / pk), 2, axis=1), round(g, 1)


def main():
    audio, segs = [], []; N = 0; gains = {}
    def seg(h, t, sub, a, foot=''):
        nonlocal N
        p = R.TMP / f'v{N:03d}.png'; N += 1; R.card(p, h, t, sub, None, foot); segs.append((p, len(a) / SR)); audio.append(a)
    seg('Voices, blind', 'Three services: A, B, C', 'Each plays the Listen scene, ten shouts, and eight lines with names. All set to one level. The key is in a separate file.', np.concatenate([R.sil(.3), R.speak('Spoken voices, a blind test. Three services, A, B and C. Each plays the Listen scene, ten shouts, and eight lines with names.'), R.sil(.4)]), foot='Chime cannot hear these: your ears decide.')
    for L, svc in KEY.items():
        seg(f'Service {L}', f'Service {L}', 'The Listen scene: Milena and Inez, then Larthi.', np.concatenate([R.speak(f'Service {L}. The Listen scene.'), R.sil(.4)]))
        for i, (who, line) in enumerate(LN.SCENE, 1):
            y, g = clip(svc, f'scene{i:02d}_{who}', -23); gains[f'{svc}/scene{i:02d}'] = g; seg(f'Service {L} · scene', NAME[who], line, np.concatenate([y, R.sil(.35)]), foot=f'{i} of 10')
        seg(f'Service {L}', f'Service {L}: shouts', 'Ten shouts from the game.', np.concatenate([R.sil(.4), R.speak('Shouts.'), R.sil(.3)]))
        for i, (who, line, _) in enumerate(LN.SHOUTS, 1):
            y, g = clip(svc, f'shout{i:02d}_{who}', -18, True); gains[f'{svc}/shout{i:02d}'] = g; seg(f'Service {L} · shouts', NAME[who], line, np.concatenate([y, R.sil(.6)]), foot=f'{i} of 10')
        seg(f'Service {L}', f'Service {L}: names', 'Larthi, Popovic, Lukac, Nikolaou.', np.concatenate([R.sil(.4), R.speak('Names.'), R.sil(.3)]))
        for i, (who, line, src) in enumerate(LN.NAMES, 1):
            y, g = clip(svc, f'name{i:02d}_{who}', -23); gains[f'{svc}/name{i:02d}'] = g; seg(f'Service {L} · names', NAME[who], line, np.concatenate([y, R.sil(.5)]), foot=('test line' if 'TEST' in src else f'{i} of 8'))
    a = np.concatenate(audio); a = a * min(1.0, 10 ** (-1 / 20) / np.abs(a).max()); w = R.TMP / 'voices.wav'; A.write_wav(w, a, SR)
    lst = R.TMP / 'cards-v.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d:.3f}\n" for p, d in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(w), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / 'briefs/090/voices-blind.mp3')], check=True)
    v = R.TMP / 'v.mp4'; subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(w), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-movflags', '+faststart', str(v)], check=True, cwd=str(R.TMP))
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(v), '-t', f'{len(a) / SR:.3f}', '-c', 'copy', str(REPO / 'briefs/090/voices-blind.mp4')], check=True)
    (REPO / 'briefs/090/blind-key.json').write_text(json.dumps({'A': KEY['A'], 'B': KEY['B'], 'C': KEY['C'], 'models': {'inworld': 'inworld:tts@1.5-max', 'qwen': 'alibaba:qwen@3-tts-1.7b-voicedesign', 'fish': 'fishaudio:s2.1@pro'}, 'order': 'sha256 of 090|service, fixed'}, indent=1) + '\n')
    meas = {s: [VM.row(f) for f in sorted((P / s).glob('*.wav'))] for s in SV}; asr = json.loads((REPO / 'out/scratch/chime/asr090.json').read_text())
    # how the shouts would sit in play: over the even bed, and over the scripted fight (087 mix, 34-46 s), at -18 LUFS momentary on the Voices bus
    bed = A.read_wav(REPO / 'unity/Assets/Suborned/Resources/Audio/087/street_bed_even.wav'); fx, fsr = A.read_wav(REPO / 'out/scratch/chime/mix.wav'); fight = fx[34 * fsr:46 * fsr]
    fit = {}
    for s in SV:
        for i, (who, _, _) in enumerate(LN.SHOUTS, 1):
            y, _ = clip(s, f'shout{i:02d}_{who}', -18, True); fit[f'{s}/shout{i:02d}'] = {'over_bed': A.clear_of(y, SR, A.resample(bed[0], bed[1], SR), SR), 'over_fight': A.clear_of(y, SR, A.resample(fight, fsr, SR), SR), 'loudness_over_fight_lu': round(-18 - A.lufs_i(fight, fsr), 1)}
    (REPO / 'briefs/090/voices-measures.json').write_text(json.dumps({'note': 'Measures only; Chime cannot hear. Services are named here: open after listening.', 'raw': meas, 'reel_gain_db': gains, 'asr_whisper_small_en': asr, 'shouts_in_the_mix': fit, 'reel': {'seconds': round(len(a) / SR, 1), 'lufs_i': round(A.lufs_i(a, SR), 1), 'peak_db': round(A.sample_peak(a), 1)}}, indent=1, ensure_ascii=False) + '\n')
    print('reel', round(len(a) / SR, 1), 's', A.lufs_i(a, SR), 'key', KEY)
    for s in SV: print(s, 'shout margin over fight (min top3 dB):', min(v['over_fight']['margin_top3_db'] for k, v in fit.items() if k.startswith(s)), 'over bed:', min(v['over_bed']['margin_top3_db'] for k, v in fit.items() if k.startswith(s)), 'LU over fight', fit[f'{s}/shout01']['loudness_over_fight_lu'])


main()
