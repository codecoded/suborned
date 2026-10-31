#!/usr/bin/env python3
# Brief 086, Redline §1.3: the rows of the Air sitting, one file a launch (TpLongRun's -tplong file). From Latch's next build a play row's zoom and yaw are read, and a row's variant may set
# softShadows, bloom, leavesAlpha, shadows, oldFar, planting (each "off"), upscale (auto, linear, point, fsr, stp), renderScalePct and gpuSplit ("on": gpu-<row>.json beside tplong.json). run-air.sh's presets still agree with these zooms. Run on the Studio: python3 -I make_rows.py; run-air.sh reads the files.
import json, os
H = os.path.dirname(os.path.abspath(__file__))
def play(name, zoom, s, w=1920, h=1080, variant=None):
    return dict(name=name, view='overhead', kind='play', agent='scout', walk=[0, 0], faceHeading=-1, yaw=30, zoom=zoom, settleSeconds=1.5, seconds=s, w=w, h=h, clockRunning=True, hud=True, vsync=False, variant=variant or {})
def third(name, walk, face, s, variant=None):
    return dict(name=name, view='follow', kind='third', agent='scout', walk=walk, faceHeading=face, yaw=30, zoom=10, settleSeconds=1.5, seconds=s, w=1920, h=1080, clockRunning=True, hud=True, vsync=False, variant=variant or {})
street, worst = lambda s, v=None, n='tp-street': third(n, [0, -20], 180, s, v), lambda s, v=None, n='tp-worst': third(n, [-10, -25], 90, s, v)
def ab(name, key, s=12):  # A B A B: the switch against as built, interleaved
    v = {key: 'off'}; return [play(f'{name}-base-a', 10, s), play(f'{name}-off-a', 10, s, variant=v), play(f'{name}-base-b', 10, s), play(f'{name}-off-b', 10, s, variant=v)]
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
    # the ceiling variants and switches (Redline §1.3 item 4; Splice's report)
    'ceiling': ab('shadows', 'shadows') + ab('planting', 'planting') + ab('softshadows', 'softShadows') + ab('bloom', 'bloom') + ab('leaves', 'leavesAlpha'),
    'oldfar': [worst(12, None, 'worst-base-a'), worst(12, {'oldFar': 'off'}, 'worst-oldfar-a'), worst(12, None, 'worst-base-b'), worst(12, {'oldFar': 'off'}, 'worst-oldfar-b')],
    'gpu': [play('gpu-play-z10', 10, 8, variant={'gpuSplit': 'on'}), worst(8, {'gpuSplit': 'on'}, 'gpu-tp-worst')],
    'fsr': [play('fsr-base', 10, 12, *N), play('fsr-65-linear', 10, 12, *N, variant={'upscale': 'linear', 'renderScalePct': 65}), play('fsr-65', 10, 12, *N, variant={'upscale': 'fsr', 'renderScalePct': 65}), play('fsr-65-linear-b', 10, 12, *N, variant={'upscale': 'linear', 'renderScalePct': 65}), play('fsr-65-b', 10, 12, *N, variant={'upscale': 'fsr', 'renderScalePct': 65})],
}
for k, rows in files.items():
    json.dump({'schema': 'suborned.tplong.resolved/1', 'outDir': 'set by the command line', 'rows': rows}, open(os.path.join(H, f'{k}.rows.json'), 'w'), indent=1)
