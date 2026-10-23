"""Teaser cut v5, 30 s (brief 052): python3 -I edit5.py [out.mp4]. Cuts the chosen windows of the shots, one grade, grain, title card composited here (never generated).
Edit the CUTS table to change the film; every window cites the shot file in shots/."""
import subprocess, sys, os
from pathlib import Path
HERE = Path(__file__).resolve().parent; S = HERE / 'shots'; K = HERE / 'keyframes'; TMP = HERE.parents[1] / 'out/scratch/forma/edit5'; TMP.mkdir(parents=True, exist_ok=True)
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else HERE.parents[1] / 'paid/052-teaser/edits/teaser-30s-v5.mp4'
FPS = 24
# (name, file, start s, duration s, extra filter)
CUTS = [  # 30 s = 720 frames at 24 fps (v5: flyby = one H3 take; Scout, Tech, envelope on H3; Tech holds ~1 s longer with the visor down)
 ('flyby',  'V5A1_h3L_t1.mp4',        0.60, 168, ''),   # H3 1440p, one continuous take, trains running to the end
 ('reveal', 'V2S1_hailuo_t1.mp4',     0.30, 56, 'fade=t=in:st=0:d=0.2:color=white,'),
 ('scout',  'V5S2_h3F_t1.mp4',        1.00, 46, ''),    # H3: steadier face, black pack readable, backlit
 ('fixer',  'V2S3_hailuo_t1.mp4',     2.80, 46, ''),
 ('tech',   'V5S4_h3F_t1.mp4',        2.00, 74, ''),    # H3: only visor + thin piping glow; visor down from ~2.7 s, held to the end of the clip
 ('enf',    'V2S5_hailuo_t2.mp4',     1.70, 46, ''),
 ('inez',   'V4M1_hailuo_t1.mp4',     1.00, 38, ''),
 ('envelope','V5M2_h3F_t1.mp4',       1.20, 44, ''),    # H3, driver with hair and the slate-blue jacket
 ('junction','V4M3_seedanceF_t1.mp4', 0.40, 42, ''),
 ('monitors','V4M4_hailuo_t1.mp4',    1.00, 40, ''),
 ('tension','V3S6_seedanceF_t5.mp4',  0.00, 52, ''),
]
TITLE_SRC = ('V3S6_seedanceF_t5.mp4', 52 / 24)  # title over the live tail of the tension clip, as v4
TITLE_FR = 68; TITLE_D = TITLE_FR / 24
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
run('-f', 'concat', '-safe', '0', '-i', lst2, '-c:v', 'libx264', '-crf', '19', '-preset', 'slow', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', '-an', TMP / 'silent.mp4')
# scratch score: score_t1.mp3 is 140 s with its biggest hit at 93.0 s; start so that hit lands on the title cut (the tension -> title cut), fades 1.2 s in and 0.8 s out
CUT_S = sum(c[3] for c in CUTS) / FPS
START = 93.0 - CUT_S
run('-ss', START, '-t', 30, '-i', HERE / 'audio/score_t1.mp3', '-af', f"afade=t=in:st=0:d=1.2,afade=t=out:st=29.2:d=0.8,volume=0.9", '-ar', '48000', '-ac', '2', TMP / 'score30.wav')
run('-i', TMP / 'silent.mp4', '-i', TMP / 'score30.wav', '-map', '0:v', '-map', '1:a', '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k', '-shortest', '-movflags', '+faststart', OUT)
print(OUT, 'title cut at', CUT_S, 's; score from', round(START, 2), 's')
