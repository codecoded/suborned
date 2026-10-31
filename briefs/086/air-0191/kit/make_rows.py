#!/usr/bin/env python3
# Brief 086, Redline §1.3: the rows of the Air sitting on 0.2.191, one file a launch (TpLongRun's -tplong file). A row's zoom is NOT read by this player: a launch's zoom comes
# from the camera preset beside the app (run-air.sh), so each file here is one zoom. Run on the Studio: python3 -I make_rows.py; run-air.sh reads the files.
import json, os
H = os.path.dirname(os.path.abspath(__file__))
def play(name, zoom, s, w=1920, h=1080, variant=None):
    return dict(name=name, view='overhead', kind='play', agent='scout', walk=[0, 0], faceHeading=-1, yaw=30, zoom=zoom, settleSeconds=1.5, seconds=s, w=w, h=h, clockRunning=True, hud=True, vsync=False, variant=variant or {})
def third(name, walk, face, s):
    return dict(name=name, view='follow', kind='third', agent='scout', walk=walk, faceHeading=face, yaw=30, zoom=10, settleSeconds=1.5, seconds=s, w=1920, h=1080, clockRunning=True, hud=True, vsync=False, variant={})
street, worst = lambda s: third('tp-street', [0, -20], 180, s), lambda s: third('tp-worst', [-10, -25], 90, s)
N = (2880, 1808)  # the 3D pass of the shipped full screen on Sky's Air: display 3840x2410, RenderCap 0.75 (windowed here, HUD at its 1x size; the launch `shipped` is the real full screen)
OFF = {'surfaceDetail': 'off'}
files = {
    'cold-z10': [play('play-z10', 10, 15), street(10), worst(10)],
    'soak-z10': [play(f'soak-{i:02}', 10, 30) for i in range(1, 15)],
    'warm-z10': [play('play-z10-a', 10, 12), play('surface-off-a', 10, 12, variant=OFF), play('play-z10-b', 10, 12), play('surface-off-b', 10, 12, variant=OFF),
                 play('native-z10-a', 10, 12, *N), play('native-z10-b', 10, 12, *N), street(10), worst(10)],
    'warm-z8': [play('play-z8', 8, 12), play('native-z8', 8, 12, *N)],
    'warm-z14': [play('play-z14', 14, 12)],
    'warm-z18': [play('play-z18', 18.5, 12)],
    'scale': [play('native-z10', 10, 12, *N), play('play-z10', 10, 12)],
}
for k, rows in files.items():
    json.dump({'schema': 'suborned.tplong.resolved/1', 'outDir': 'set by the command line', 'rows': rows}, open(os.path.join(H, f'{k}.rows.json'), 'w'), indent=1)
