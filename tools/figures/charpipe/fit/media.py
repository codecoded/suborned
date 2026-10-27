"""Brief 069 fit test: labelled videos (ffmpeg, H.264) and play-zoom strips from the animate frames. System python (PIL).
  python3 tools/figures/charpipe/fit/media.py                                  (the fit test: every input's AG, ext and play)
  python3 tools/figures/charpipe/fit/media.py <set dir> video:<name> ... play:<name> ...   (any set: <dir>/anim -> <dir>/media)"""
import json, os, glob, subprocess, sys
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(HERE))))
S = os.path.join(ROOT, 'out/scratch/forma/char-pipeline/fit-test'); D = json.load(open(os.path.join(HERE, 'fit.json')))
A = os.path.join(S, 'anim'); M = os.path.join(S, 'media'); os.makedirs(M, exist_ok=True)
try: F = ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc', 22); Fs = ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc', 15)
except Exception: F = Fs = ImageFont.load_default()


def video(name):
    m = json.load(open(os.path.join(A, f'{name}.metrics.json'))); clips = m['clips']; labs = m['figures']
    frames = sorted(glob.glob(os.path.join(A, name, 'close_*.png')))
    per = len(frames) // len(clips); tmp = os.path.join(A, name, 'lab'); os.makedirs(tmp, exist_ok=True)
    for k, f in enumerate(frames):
        im = Image.open(f).convert('RGB'); d = ImageDraw.Draw(im); w, h = im.size
        d.text((14, 10), clips[min(k // per, len(clips) - 1)].replace('_', ' '), fill=(20, 30, 30), font=F)
        n = len(labs)
        for j, lab in enumerate(labs): d.text((int(w * (j + 0.5) / n) - 30, h - 34), lab, fill=(20, 30, 30), font=F)
        im.save(os.path.join(tmp, f'{k:04d}.png'))
    out = os.path.join(M, f'{name}.mp4')
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(D['render']['fps'] // 2), '-i', os.path.join(tmp, '%04d.png'), '-vf', 'scale=trunc(iw*0.8/2)*2:trunc(ih*0.8/2)*2',
                    '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '28', '-preset', 'slow', out], check=True)
    # a key-frame sheet: one frame per clip (the middle)
    picks = [frames[c * per + per // 2] for c in range(len(clips))]
    ims = [Image.open(os.path.join(tmp, os.path.basename(p).replace('close_', ''))) if False else Image.open(os.path.join(tmp, f'{c * per + per // 2:04d}.png')) for c, p in enumerate(picks)]
    w, h = ims[0].size; cols = 4; rows = (len(ims) + cols - 1) // cols; sh = Image.new('RGB', (w * cols, h * rows), (235, 235, 235))
    for i, im in enumerate(ims): sh.paste(im, ((i % cols) * w, (i // cols) * h))
    sh = sh.resize((int(sh.width * 0.5), int(sh.height * 0.5))); sh.save(os.path.join(M, f'{name}_sheet.jpg'), quality=82)
    return out


def play(name):
    P = D['render']['play']; files = sorted(glob.glob(os.path.join(A, name, 'play_*.png')))
    m_clips = list(D['clips']['list'])
    rows = []
    for c in m_clips:
        fs = sorted(f for f in files if os.path.basename(f).startswith(f'play_{c}_'))
        if not fs: continue
        ims = [Image.open(f).convert('RGB') for f in fs]; w, h = ims[0].size
        row = Image.new('RGB', (w * len(ims) * P['upscale'] + 130, h * P['upscale']), (235, 235, 235))
        ImageDraw.Draw(row).text((6, 8), c.replace('_', ' '), fill=(20, 30, 30), font=Fs)
        for i, im in enumerate(ims): row.paste(im.resize((w * P['upscale'], h * P['upscale']), Image.NEAREST), (130 + i * w * P['upscale'], 0))
        rows.append(row)
    W = max(r.width for r in rows); out = Image.new('RGB', (W, sum(r.height for r in rows)), (235, 235, 235)); y = 0
    for r in rows: out.paste(r, (0, y)); y += r.height
    out.save(os.path.join(M, f'{name}_x{P["upscale"]}.png'))
    # true size, first frame of walk
    f = sorted(f for f in files if 'play_walk_' in f)[0]; Image.open(f).save(os.path.join(M, f'{name}_1x.png'))


if len(sys.argv) > 1:
    A = os.path.join(sys.argv[1], 'anim'); M = os.path.join(sys.argv[1], 'media'); os.makedirs(M, exist_ok=True)
    for job in sys.argv[2:]:
        kind, name = job.split(':', 1); print(video(name) if kind == 'video' else play(name))
else:
    for i in [r['id'] for r in D['inputs']]:
        for kind in ('AG', 'ext'): print(video(f'{i}_{kind}'))
        play(f'{i}_play')
