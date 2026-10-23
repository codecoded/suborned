"""Teaser cut v3 (brief 052; v2 is edit2.py): python3 -I edit3.py [out.mp4]. Cuts the chosen windows of the shots, one grade, grain, title card composited here (never generated).
Edit the CUTS table to change the film; every window cites the shot file in shots/."""
import subprocess, sys, os
from pathlib import Path
HERE = Path(__file__).resolve().parent; S = HERE / 'shots'; K = HERE / 'keyframes'; TMP = HERE.parents[1] / 'out/scratch/forma/edit3'; TMP.mkdir(parents=True, exist_ok=True)
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else HERE / 'teaser-15s-v3.mp4'
FPS = 24
# (name, file, start s, duration s, extra filter)
CUTS = [  # shots 1-6 exactly as v2; frames at 24 fps: 34 + 67 + 4 x 38 + 47 = 300, title 60, total 360 = 15.00 s
 ('hook',   'S0_hailuo_t2.mp4',       4.30, 34, ''),
 ('reveal', 'V2S1_hailuo_t1.mp4',     0.30, 67, 'fade=t=in:st=0:d=0.25:color=white,'),
 ('scout',  'V2S2_hailuo_t1.mp4',     1.20, 38, ''),
 ('fixer',  'V2S3_hailuo_t1.mp4',     2.80, 38, ''),
 ('tech',   'V2S4_hailuo_t1.mp4',     1.70, 38, ''),
 ('enf',    'V2S5_hailuo_t2.mp4',     1.70, 38, ''),
 ('tension','V3S6_seedanceF_t5.mp4',  0.50, 47, ''),   # v3: live tension shot, car rolls in, people cross
]
TITLE_SRC = ('V3S6_seedanceF_t5.mp4', 2.54)  # the title runs over the live tail of the same clip
TITLE_FR = 60; TITLE_D = TITLE_FR / 24
GRADE = ("eq=contrast=1.05:saturation=1.07:gamma=0.97,colorbalance=rs=-0.025:gs=0.0:bs=0.045:rh=0.025:gh=0.005:bh=-0.02,"
         "curves=m='0/0.015 0.5/0.5 1/0.985',vignette=angle=PI/5.5")
GRAIN = "noise=alls=8:allf=t+u"
def run(*a):
    r = subprocess.run(['ffmpeg', '-v', 'error', '-y', *map(str, a)], capture_output=True, text=True)
    if r.returncode: raise SystemExit(r.stderr)
parts = []
for name, f, ss, d, extra in CUTS:
    o = TMP / f'{name}.mp4'
    run('-ss', ss, '-i', S / f, '-vf', f"{extra}scale=1920:1080:flags=lanczos,fps={FPS},setsar=1,format=yuv420p", '-frames:v', d, '-an', '-c:v', 'libx264', '-crf', '12', '-preset', 'fast', o)
    parts.append(o)
lst = TMP / 'list.txt'; lst.write_text(''.join(f"file '{p}'\n" for p in parts))
run('-f', 'concat', '-safe', '0', '-i', lst, '-c', 'copy', TMP / 'cut.mp4')
run('-i', TMP / 'cut.mp4', '-vf', f"{GRADE},{GRAIN},format=yuv420p", '-c:v', 'libx264', '-crf', '12', '-preset', 'fast', TMP / 'graded.mp4')
# title card: dark, blurred, slowly pushing frame of the last shot with the lockup
subprocess.run(['python3', '-I', HERE / 'title.py', TMP / 'title.png', '1920', '1080', '3.4'], check=True)
n = TITLE_FR
ts, to = TITLE_SRC
# live title plate: the graded tail of the clip, easing (0.6 s) into a blurred, darkened version so people and traffic keep moving behind the lockup
run('-ss', to, '-i', S / ts, '-frames:v', TITLE_FR, '-vf', f"scale=1920:1080:flags=lanczos,fps={FPS},setsar=1,format=yuv420p", '-an', '-c:v', 'libx264', '-crf', '12', TMP / 'plate.mp4')
run('-i', TMP / 'plate.mp4', '-i', TMP / 'title.png',
    '-filter_complex',
    f"[0:v]{GRADE},{GRAIN},format=yuv420p,split[a][b];"
    f"[b]gblur=sigma=6,eq=brightness=-0.17:saturation=0.92:contrast=1.06,colorbalance=bs=0.06:rs=-0.04,format=yuv420p[bb];"
    f"[a][bb]blend=all_expr='A*(1-min(1,T/0.6))+B*min(1,T/0.6)',format=yuva420p[bg];"
    f"[1:v]format=rgba,loop=loop=-1:size=1,fade=t=in:st=0.15:d=0.55:alpha=1[ti];[bg][ti]overlay=x='(main_w-overlay_w)/2':y='(main_h-overlay_h)/2':format=auto:shortest=1,format=yuv420p,fade=t=out:st={TITLE_D-0.3}:d=0.3[v]",
    '-map', '[v]', '-frames:v', TITLE_FR, '-c:v', 'libx264', '-crf', '12', '-preset', 'fast', TMP / 'title.mp4')
lst2 = TMP / 'list2.txt'; lst2.write_text(f"file '{TMP/'graded.mp4'}'\nfile '{TMP/'title.mp4'}'\n")
run('-f', 'concat', '-safe', '0', '-i', lst2, '-c:v', 'libx264', '-crf', '19', '-preset', 'slow', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', '-an', OUT)
print(OUT)
