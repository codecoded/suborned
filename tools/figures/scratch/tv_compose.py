"""Frame strips and mp4s from tv_render.py output (python3 -I tools/figures/scratch/tv_compose.py <render dir root> <out dir>). Scratch."""
import sys, os, json, subprocess, shutil
from PIL import Image, ImageDraw
root, out = sys.argv[1:3]
os.makedirs(out, exist_ok=True)
NPH = 36
FIGS = {'scout': ('walk_f', 'run'), 'tech': ('walk', 'run'), 'enforcer': ('walk', 'run')}
IMPL = (('testbed', 'testbed (own rig, own clip)'), ('now', 'today (Mixamo clip, Unity library)'))
def img(fig, impl, clip, view, i): return Image.open(f'{root}/out_{fig}/{fig}_{impl}_{clip}_{view}_{i % NPH:02d}.png').convert('RGB')
def play_crop(im, up): return im.crop((50, 50, 150, 150)).resize((100 * up, 100 * up), Image.NEAREST)
summary = {}
for fig, nowclips in FIGS.items():
    M = json.load(open(f'{root}/out_{fig}/{fig}_metrics.json')); summary[fig] = M
    for k, clip in enumerate(('walk', 'run')):
        cols = [0, 4, 8, 12, 16, 20, 24, 28, 32]
        cw = 200; sections = [('play zoom (figure about 55 px tall at 1080p, shown x2)', 'play'), ('close, 3/4 from the front', 'close'), ('side-on', 'side')]
        hdr = 34; rowh = {'play': 200, 'close': 200, 'side': 200}
        H = hdr + sum(2 * rowh[v] + 22 for _, v in sections); Wd = 150 + cw * len(cols)
        S = Image.new('RGB', (Wd, H), (240, 236, 226)); d = ImageDraw.Draw(S)
        mt, mn = M['testbed'][clip], M['now'][clip]
        d.text((6, 4), f'{fig} {clip}: testbed {mt["frames"]}f @ {mt["fps"]} = {mt["cycleS"]} s  |  today {NOWC if False else nowclips[k]} {mn["frames"]}f @ {mn["fps"]} = {mn["cycleS"]} s.  Nine phases of one cycle, left to right; walking right.', fill=(0, 0, 0))
        d.text((6, 18), 'Both sides drawn here in Blender with the same clay, light and cameras. "Today" is the Mixamo clip retargeted by a stand-in (not Unity itself).', fill=(90, 90, 90))
        y = hdr
        for title, v in sections:
            d.text((6, y + 4), title, fill=(120, 0, 0)); y += 22
            for impl, lab in IMPL:
                d.text((6, y + rowh[v] // 2 - 10), impl, fill=(0, 0, 0)); d.text((6, y + rowh[v] // 2 + 4), '(testbed)' if impl == 'testbed' else '(today)', fill=(90, 90, 90))
                for c, i in enumerate(cols):
                    im = img(fig, impl, clip, v, i)
                    t = play_crop(im, 2) if v == 'play' else im.resize((200, 200), Image.LANCZOS)
                    if v == 'close' or v == 'side': pass
                    S.paste(t, (150 + c * cw, y + (0 if v != 'play' else 0)))
                y += rowh[v]
        S.save(f'{out}/{fig}_{clip}_strips.png')
        # mp4 : 2 cols x 3 rows, real time
        reps = 3 if clip == 'walk' else 5
        dur = max(mt['cycleS'], mn['cycleS']) * reps; fps = 30; n = int(dur * fps)
        tmp = f'{out}/_f_{fig}_{clip}'; shutil.rmtree(tmp, ignore_errors=True); os.makedirs(tmp)
        for f in range(n):
            t = f / fps; V = Image.new('RGB', (800, 1230), (240, 236, 226)); dd = ImageDraw.Draw(V)
            for c, (impl, lab) in enumerate(IMPL):
                m = M[impl][clip]; ph = (t / m['cycleS']) % 1.0; i = int(round(ph * NPH)) % NPH
                dd.text((c * 400 + 6, 4), lab, fill=(0, 0, 0)); dd.text((c * 400 + 6, 18), f'{m["cycleS"]} s cycle, {m["stepsPerS"]} steps/s', fill=(90, 90, 90))
                V.paste(play_crop(img(fig, impl, clip, 'play', i), 4), (c * 400, 34))  # 2x of crop to 400 (x4 of source px => nearest)
                V.paste(img(fig, impl, clip, 'close', i), (c * 400, 434)); V.paste(img(fig, impl, clip, 'side', i), (c * 400, 834))
            V.save(f'{tmp}/{f:04d}.png')
        subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(fps), '-i', f'{tmp}/%04d.png', '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '18', f'{out}/{fig}_{clip}.mp4'], check=True)
        shutil.rmtree(tmp)
json.dump(summary, open(f'{out}/metrics.json', 'w'), indent=1)
print('ok')
