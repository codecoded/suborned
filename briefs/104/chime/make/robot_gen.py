"""Brief 104 (Chime): the patrol robot's paid takes, made the way brief 087's pack was (briefs/087/chime/gen.py): Seed Audio 1.0 on Runware, one take per call,
plus Mirelo SFX 1.5 video-to-audio on a cut of the demo's draw beat. Every call goes through tools/paid/paid.py, job 104-robot-sfx.
Run: python3 -I briefs/104/chime/make/robot_gen.py <pass> <budget> [ids...]      Takes land in paid/104-robot-sfx/takes/<id>_t<n>.<wav|mp4>, never overwritten."""
import base64, json, subprocess, sys, time, uuid
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[3]
sys.path.insert(0, str(REPO / 'tools/paid'))
import paid

SEED = 'bytedance:seed-audio@1.0'
DEMO = REPO / 'briefs/104/vela/patrolbot/demo/patrolbot-demo.mp4'
CLIPS = {'draw': (9.70, 11.60), 'draw2': (9.62, 11.62)}     # the close camera's draw beat (make/demo_motion.py: turn 290, panel 315, sidearm 330, shot 1 at frame 350 = 11.67 s), cut before the shot
SCRATCH = REPO / 'out/scratch/chime/r104'

# Mirelo answers a video input with a video (MP4 with the sound on it): its result carries videoURL, not audioURL. Let paid.py's wait see it; the row stays task 'audio'.
_orig = paid._result_url
paid._result_url = lambda kind, d: _orig(kind, d) or (d.get('videoURL') if kind == 'audio' else None)


def upload(p, clip):
    """Cut the beat (cropped to the robot, 960x540), upload it once with Runware's mediaStorage (storage, not a generation: no cost), cache the mediaUUID."""
    SCRATCH.mkdir(parents=True, exist_ok=True); cache = SCRATCH / f'{clip}.media.json'
    if cache.exists(): return json.loads(cache.read_text())
    a, b = CLIPS[clip]; mp4 = SCRATCH / f'{clip}_cut.mp4'
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-ss', str(a), '-i', str(DEMO), '-t', f'{b - a:.2f}', '-vf', 'crop=1080:608:540:240,scale=960:540', '-an',
                    '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p', str(mp4)], check=True)
    d = p.client.call([{'taskType': 'mediaStorage', 'taskUUID': str(uuid.uuid4()), 'operation': 'upload',
                        'media': 'data:video/mp4;base64,' + base64.b64encode(mp4.read_bytes()).decode()}])[0]
    rec = {'mediaUUID': d['mediaUUID'], 'mediaURL': d.get('mediaURL'), 'cut': [a, b], 'file': str(mp4.relative_to(REPO))}
    cache.write_text(json.dumps(rec, indent=1)); return rec


def hash_id(s):
    h = 2166136261
    for ch in s.encode(): h = ((h ^ ch) * 16777619) & 0xffffffff
    return h


def main(a):
    name, budget = a[0], float(a[1]); want = set(a[2:])
    table = json.loads((HERE / 'prompts-paid.json').read_text())
    p = paid.Pass('104-robot-sfx', name, budget=budget, note='Patrol robot sound, paid pass (brief 104; Sky: "paid pass please"; capped at $3)')
    jobs = [(c, t) for c in table if not want or c['id'] in want for t in range(1, c.get('takes', 2) + 1)]
    media = {c['video']: upload(p, c['video']) for c in table if c.get('video') and (not want or c['id'] in want)}

    def one(j):
        c, t = j; ext = 'mp4' if c.get('video') else 'wav'; nm = f"{c['id']}_t{c.get('base', 0) + t}.{ext}"
        if c.get('video'):
            m = media[c['video']]; seed = 104000 + hash_id(c['id']) % 10000 + t * 100
            task = {'model': c['model'], 'inputs': {'video': m['mediaUUID']}, 'steps': 25, 'seed': seed, 'outputFormat': 'MP4', 'numberResults': 1}
            params, refs = {'steps': 25, 'cue': c['cue'], 'video_cut_s': m['cut']}, [m['file']]
        else:
            task = {'model': SEED, 'positivePrompt': c['prompt'], 'outputFormat': 'WAV', 'numberResults': 1}   # one clip per call, as 087
            params, refs = {'duration': c['s'], 'cue': c['cue']}, []
        for attempt in range(4):
            try:
                return p.audio(task, name=nm, subdir='takes', est=c.get('est', 0.05), prompt=c['prompt'], refs_log=refs, params=params, note=f"take {t} for {c['cue']} ({c['id']})")
            except SystemExit as e:
                if 'serviceOverloaded' not in str(e) or attempt == 3: print(e); return None
                time.sleep(15 * (attempt + 1))
    with ThreadPoolExecutor(3) as ex:
        list(ex.map(one, jobs))
    print(f'pass {name}: ${p.spent():.4f} of ${p.budget:.2f}')


if __name__ == '__main__':
    main(sys.argv[1:])
