"""Teaser cut (brief 052): python3 -I edit.py [out.mp4]. Cuts the chosen windows of the shots, one grade, grain, title card composited here (never generated).
Edit the CUTS table to change the film; every window cites the shot file in shots/."""
import subprocess, sys, os
from pathlib import Path
HERE = Path(__file__).resolve().parent; S = HERE / 'shots'; K = HERE / 'keyframes'; TMP = HERE.parents[1] / 'out/scratch/forma/edit'; TMP.mkdir(parents=True, exist_ok=True)
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else HERE / 'teaser-15s.mp4'
FPS = 24
# (name, file, start s, duration s, extra filter)
CUTS = [  # frames at 24 fps: 34 + 67 + 4 x 38 + 48 = 301, title 59, total 360 = 15.00 s
 ('hook',   'S0_hailuo_t2.mp4',      4.30, 34, ''),
 ('reveal', 'S1_hailuo_t3.mp4',      0.30, 67, 'fade=t=in:st=0:d=0.25:color=white,'),
 ('scout',  'S2_seedanceF_t2.mp4',   0.70, 38, ''),
 ('fixer',  'S3_hailuo_t2.mp4',      1.40, 38, ''),
 ('tech',   'S4_seedanceF_t2.mp4',   0.40, 38, ''),
 ('enf',    'S5_hailuo_t2.mp4',      3.60, 38, ''),
 ('tension','S6_hailuo_t2.mp4',      0.00, 48, ''),
]
TITLE_FR = 59; TITLE_D = TITLE_FR / 24
GRADE = ("eq=contrast=1.05:saturation=1.07:gamma=0.97,colorbalance=rs=-0.025:gs=0.0:bs=0.045:rh=0.035:gh=0.005:bh=-0.035,"
         "curves=m='0/0.015 0.5/0.5 1/0.985',vignette=angle=PI/4")
GRAIN = "noise=alls=6:allf=t+u"
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
subprocess.run(['python3', '-I', HERE / 'title.py', TMP / 'title.png', '1920', '1080', '2.2'], check=True)
n = TITLE_FR
run('-loop', '1', '-framerate', FPS, '-t', TITLE_D + 0.05, '-i', K / 'S6_t2.png', '-loop', '1', '-framerate', FPS, '-t', TITLE_D + 0.05, '-i', TMP / 'title.png',
    '-filter_complex',
    f"[0:v]scale=2880:1620:flags=lanczos,zoompan=z='1+0.045*on/{n}':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d=1:s=1920x1080:fps={FPS},gblur=sigma=22,"
    f"eq=brightness=-0.2:saturation=0.9:contrast=1.08,colorbalance=bs=0.06:rs=-0.04,vignette=angle=PI/4,{GRAIN},format=yuva420p[bg];"
    f"[1:v]format=rgba,fade=t=in:st=0.35:d=0.8:alpha=1[ti];[bg][ti]overlay=0:0:format=auto,format=yuv420p,fade=t=out:st={TITLE_D-0.35}:d=0.35[v]",
    '-map', '[v]', '-frames:v', TITLE_FR, '-c:v', 'libx264', '-crf', '12', '-preset', 'fast', TMP / 'title.mp4')
lst2 = TMP / 'list2.txt'; lst2.write_text(f"file '{TMP/'graded.mp4'}'\nfile '{TMP/'title.mp4'}'\n")
run('-f', 'concat', '-safe', '0', '-i', lst2, '-c:v', 'libx264', '-crf', '19', '-preset', 'slow', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', '-an', OUT)
print(OUT)
