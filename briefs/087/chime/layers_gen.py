"""Brief 087 (Chime): the layer test's long-piece routes, job 087-music pass 2 (budget $5), ACE-Step XL Base through tools/paid/paid.py. The source goes up by Runware's mediaStorage (free). Run: python3 -I briefs/087/chime/layers_gen.py <routes|arrange>"""
import base64, json, subprocess, sys, time, uuid
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
REPO = Path(__file__).resolve().parents[3]; sys.path.insert(0, str(REPO / 'tools/paid')); import paid
L = REPO / 'out/scratch/chime/layers'; MODEL = 'runware:ace-step@v1.5-xl-base'; BPM, KEY = 86, 'F minor'
CHAR = 'Instrumental retro-future score, 86 BPM, F minor, 4/4. Warm analog synthesizers, a vibraphone and theremin motif, soft strings, a pulsing ostinato and brushed drums; tense but bright.'
TAIL = ' Instrumental only, no vocals, no speech. One fixed tempo and one key throughout, no tempo change, no key change, no big hit, no ending, no fade-out; a steady groove all the way through.'
SET = {'bpm': BPM, 'keyScale': KEY, 'timeSignature': 4, 'lyrics': '[Instrumental]', 'vocalLanguage': 'unknown'}


def upload(path):
    c = paid.RunwareClient(); kind = 'audio/mpeg' if path.suffix == '.mp3' else 'audio/wav'
    return c.call([{'taskType': 'mediaStorage', 'taskUUID': str(uuid.uuid4()), 'operation': 'upload', 'media': f'data:{kind};base64,' + base64.b64encode(path.read_bytes()).decode()}], timeout=240)[0]['mediaUUID']


def mp3(p):
    o = p.with_suffix('.mp3'); subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(p), '-c:a', 'libmp3lame', '-b:a', '192k', str(o)], check=True); return o


def routes():
    teaser = upload(REPO / 'paid/052-teaser/audio/score_t1.mp3'); groove = upload(mp3(L / 'groove.wav')); section = upload(mp3(L / 'section.wav')); print('uploaded', teaser, groove, section)
    J = []
    for t in (1, 2, 3): J.append((f'text_t{t}', {'duration': 156, 'seed': 8800 + t}, CHAR + TAIL, 0.03))
    for s in (30, 50, 70): J.append((f'cover_whole_s{s}', {'inputs': {'audio': teaser}, 'strength': s / 100, 'seed': 8810 + s}, CHAR + TAIL, 0.03))
    for s in (30, 50, 70): J.append((f'cover_groove_s{s}', {'inputs': {'audio': groove}, 'strength': s / 100, 'seed': 8820 + s}, CHAR + TAIL, 0.03))
    J.append(('extend_section', {'inputs': {'audio': section}, 'seed': 8890, 'settings': {**SET, 'repaintingStart': 39, 'repaintingEnd': 156}}, CHAR + TAIL, 0.03))
    return J


def arrange():
    base = json.loads((L / 'base.json').read_text()); uu = upload(mp3(L / base['wav']))
    P = {'calm': 'Sparse and quiet: warm analog pads and a soft vibraphone only, no drums, almost no bass.', 'tension': 'Same music with the pulsing analog bass ostinato and a soft ticking hi-hat added, a thin theremin line, restless.', 'fight': 'Same music at full force: driving drums, heavy pulsing bass, muted brass stabs, strings hammering the ostinato, urgent.'}
    J = []
    for name, text in P.items():
        for s in base['strengths']: J.append((f'arr_{name}_s{int(s * 100)}', {'inputs': {'audio': uu}, 'strength': s, 'seed': 8900 + int(s * 100) + len(name)}, 'Instrumental retro-future score, 86 BPM, F minor, 4/4. ' + text + TAIL, 0.03))
    return J


def run(J, name):
    p = paid.Pass('087-music', '2', budget=5.0, note='Layer test on the teaser direction (Sky via Aster; $5)')
    def one(j):
        id, extra, prompt, est = j; task = {'model': MODEL, 'positivePrompt': prompt, 'outputFormat': 'WAV', 'numberResults': 1, 'settings': SET, **extra}
        for a in range(4):
            try: return p.audio(task, name=f'{id}.wav', subdir='layers', est=est, prompt=prompt, refs_log=[str(extra.get('inputs', ''))], params={k: v for k, v in extra.items()}, note=id)
            except SystemExit as e:
                if 'serviceOverloaded' not in str(e) or a == 3: print(id, str(e)[:300]); return None
                time.sleep(20 * (a + 1))
    with ThreadPoolExecutor(3) as ex: list(ex.map(one, J))
    print(f'pass 2: ${p.spent():.4f} of ${p.budget:.2f}')


if __name__ == '__main__': run(routes() if sys.argv[1] == 'routes' else arrange(), sys.argv[1])
