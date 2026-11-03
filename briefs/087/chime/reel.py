"""Brief 087 (Chime): the reel for Sky's phone: each cue named aloud and on a card, twice over (loops for 5 s), then the scripted minute (mix.py). Writes briefs/087/reel.mp3 and reel.mp4. macOS `say` speaks the labels. Run from the repo root with the scratch venv's python -I."""
import subprocess, sys, tempfile, json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]
sys.path.insert(0, str(HERE))
import audiolib as A, mix as M
from pack import CUES, BUSES
SR = 48000; W, H = 1080, 1080; TMP = REPO / 'out/scratch/chime/reel'; TMP.mkdir(parents=True, exist_ok=True)
GROUPS = {'clock': 'Timing', 'danger': 'Danger', 'fight': 'Fight', 'convo': 'Conversation', 'ambience': 'City'}
INK, CREAM, SEA, BRASS = (28, 30, 34), (244, 234, 214), (121, 197, 180), (196, 158, 92)
F = lambda s: ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf', s)


def speak(text):
    p = TMP / f'say_{abs(hash_s(text))}.wav'
    if not p.exists():
        a = TMP / 'tmp.aiff'; subprocess.run(['say', '-v', 'Daniel', '-r', '185', '-o', str(a), text], check=True)
        subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(a), '-ar', str(SR), '-ac', '1', '-c:a', 'pcm_s16le', str(p)], check=True)
    x, _ = A.read_wav(p); x = x * 10 ** ((-23 - A.lufs_m(x, SR)) / 20); return np.repeat(x, 2, axis=1)


def hash_s(s):
    h = 2166136261
    for ch in s.encode(): h = ((h ^ ch) * 16777619) & 0xffffffff
    return h


def wrap(d, text, font, width):
    out, line = [], ''
    for w in text.split():
        t = (line + ' ' + w).strip()
        if d.textlength(t, font=font) > width and line: out.append(line); line = w
        else: line = t
    return out + ([line] if line else [])


def card(path, head, title, sub='', wave=None, foot=''):
    im = Image.new('RGB', (W, H), CREAM); d = ImageDraw.Draw(im); d.rectangle([0, 0, W, 150], fill=SEA); d.text((54, 48), head, font=F(56), fill=INK); y = 230
    for l in wrap(d, title, F(84), W - 108): d.text((54, y), l, font=F(84), fill=INK); y += 100
    y += 30; d.line([(54, y), (W - 54, y)], fill=BRASS, width=3); y += 30
    for l in wrap(d, sub, F(46), W - 108): d.text((54, y), l, font=F(46), fill=INK); y += 60
    if wave is not None:
        m = wave.reshape(len(wave), -1).mean(axis=1); m = m / (np.abs(m).max() + 1e-9); idx = np.linspace(0, len(m), W - 108 + 1).astype(int); cy = 900
        for i in range(W - 108):
            s = m[idx[i]:max(idx[i] + 1, idx[i + 1])]; d.line([(54 + i, cy - s.max() * 80), (54 + i, cy - s.min() * 80)], fill=(60, 110, 100))
    if foot: d.text((54, H - 60), foot, font=F(30), fill=INK)
    im.save(path)


def sil(s): return np.zeros((int(s * SR), 2))


def st(x): return x if x.shape[1] == 2 else np.repeat(x, 2, axis=1)


def main():
    segs = []; n = 0; audio = []
    def seg(head, title, sub, a, wave=None, foot=''):
        nonlocal n
        p = TMP / f'c{n:03d}.png'; n += 1; card(p, head, title, sub, wave, foot); segs.append((p, len(a) / SR)); audio.append(a)
    t = np.concatenate([sil(.4), speak('The Suborned. First sound pack. Each sound is named, then played twice. A minute of them together comes at the end. Headphones show the low end, which a phone speaker loses.'), sil(.5)])
    seg('Brief 087', 'First sound pack', 'Each sound named, played twice. A minute together at the end. Nothing is wired into the game yet.', t, foot='Chime cannot hear these: your ears decide.')
    grp = None; i = 0; total = len([c for c in CUES])
    for c in CUES:
        i += 1; x, sr = A.read_wav(M.AUD / f"{c['id']}.wav"); x = st(A.resample(x, sr, SR)) * M.gain(c['id'])
        if c['group'] != grp:
            grp = c['group']; g = np.concatenate([speak(GROUPS[grp]), sil(.35)]); seg(GROUPS[grp], GROUPS[grp], '', g)
        if c['loop']:
            k = min(8.0, len(x) / SR) if c['id'] != 'street_bed' else 10.0; b = np.resize(x, (int(k * SR), 2)); b = A.fade(b, SR, .15, .4); body = np.concatenate([sil(.25), b, sil(.4)])
        else:
            gap = sil(.75); body = np.concatenate([sil(.25), x, gap, x, sil(.6)])
        a = np.concatenate([speak(c['label']), body]); seg(f"{GROUPS[grp]} · {i} of {total}", c['label'], c['trigger'], a, wave=x, foot=c['id'])
    # the scripted minute, one card a beat
    t = np.concatenate([speak('Now all together, as a fight might go. Sixty six seconds.'), sil(.4)]); seg('The mix', 'A scripted minute', 'One made-up run at the mix levels. Timing is mine, not the game\'s.', t)
    m = np.clip(M.scene(), -1, 1); sc = M.SCENES + [(66, '')]
    for (a, txt), (b, _) in zip(sc, sc[1:]):
        seg('The mix', txt, f'{int(a) // 60}:{int(a) % 60:02d}', m[int(a * SR):int(b * SR)])
    a = np.concatenate(audio); pk = np.abs(a).max(); a = a * min(1.0, 10 ** (-1 / 20) / pk)
    wav = TMP / 'reel.wav'; A.write_wav(wav, a, SR); print(f'reel {len(a) / SR:.1f} s, peak {20 * np.log10(np.abs(a).max()):.1f} dBFS, LUFS-I {A.lufs_i(a, SR):.1f}')
    lst = TMP / 'cards.txt'; lst.write_text(''.join(f"file '{p.name}'\nduration {d:.3f}\n" for p, d in segs) + f"file '{segs[-1][0].name}'\n")
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(wav), '-c:a', 'libmp3lame', '-b:a', '128k', str(REPO / 'briefs/087/reel.mp3')], check=True)
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(lst), '-i', str(wav), '-vf', 'fps=5,format=yuv420p', '-c:v', 'libx264', '-preset', 'medium', '-crf', '30', '-c:a', 'aac', '-b:a', '128k', '-shortest', '-movflags', '+faststart', str(REPO / 'briefs/087/reel.mp4')], check=True, cwd=str(TMP))


if __name__ == '__main__': main()
