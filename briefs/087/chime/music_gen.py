"""Brief 087 (Chime): soundtrack sketches, job 087-music, one call per take through tools/paid/paid.py. ACE-Step takes tempo, key and metre as settings; MiniMax only reads them in the prompt. Run: python3 -I briefs/087/chime/music_gen.py <pass> <budget>"""
import json, sys, time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
REPO = Path(__file__).resolve().parents[3]; sys.path.insert(0, str(REPO / 'tools/paid')); import paid
TAIL = ' Instrumental only, no vocals, no choir, no speech. One fixed tempo from start to finish, no tempo change, no key change, no fade-out; a steady groove all the way through, so it can loop.'
PIECES = [
    ('lounge_spy', 'Lounge-spy', 112, 'D minor', 4, '1950s atomic-age lounge jazz crossed with a cold-war spy film score, 112 BPM, D minor, 4/4. Vibraphone melody, walking upright bass, brushed drums on a ride cymbal, muted trumpet stabs, a quiet theremin line. Polished and smiling on top, with something watching underneath.'),
    ('analog_theremin', 'Analog synth and theremin', 84, 'A minor', 4, 'Retro-future instrumental, 84 BPM, A minor, 4/4. Warm analog synthesizer pads, a soft pulsing analog bass ostinato, a theremin melody, a little vibraphone, light brushed drums. Bright, shimmering, slightly uneasy; the optimism of a world fair at night.'),
    ('big_band_tension', 'Big-band tension', 132, 'C minor', 4, '1950s big band crime-film tension, 132 BPM, C minor, 4/4. Muted brass section stabs, low saxophone section riff, walking double bass, tight snare and ride swing, tense piano comping. Driving and coiled, held back rather than exploding.'),
    ('civic_clockwork', 'Civic clockwork', 96, 'F major', 4, '1950s public-information film music, 96 BPM, F major, 4/4. Celesta and orchestra bells playing a bright cheerful tune over pizzicato strings and a steady woodblock tick like clockwork, marimba, soft muted horns, and underneath a low sustained tuba and bowed bass note that never resolves. Cheerful, orderly and faintly threatening, like an official telling you everything is fine.'),
]


def main(a):
    p = paid.Pass('087-music', a[0], budget=float(a[1]), note='Four soundtrack sketches (brief 087; Sky yes, $2 budget)')
    jobs = []
    for i, (id, label, bpm, key, ts, text) in enumerate(PIECES):
        for t in (1, 2):
            jobs.append((id, 'ace', t, {'model': 'runware:ace-step@v1.5-xl-base', 'positivePrompt': text + TAIL, 'duration': 58, 'seed': 8700 + i * 10 + t, 'outputFormat': 'WAV', 'numberResults': 1, 'settings': {'bpm': bpm, 'keyScale': key, 'timeSignature': ts, 'lyrics': '[Instrumental]', 'vocalLanguage': 'unknown'}}, 0.03))
        jobs.append((id, 'minimax', 1, {'model': 'minimax:music@2.6', 'positivePrompt': (text + TAIL + ' About 55 seconds.')[:2000], 'seed': 8750 + i, 'outputFormat': 'WAV', 'numberResults': 1, 'settings': {'instrumental': True}}, 0.16))
    def one(j):
        id, eng, t, task, est = j
        for attempt in range(4):
            try:
                return p.audio(task, name=f'{id}_{eng}_t{t}.wav', subdir='takes', est=est, prompt=task['positivePrompt'], params={'engine': eng, 'piece': id, 'settings': task.get('settings')}, note=f'{id} {eng} take {t}')
            except SystemExit as e:
                if 'serviceOverloaded' not in str(e) or attempt == 3: print(e); return None
                time.sleep(20 * (attempt + 1))
    with ThreadPoolExecutor(3) as ex: list(ex.map(one, jobs))
    print(f'pass {a[0]}: ${p.spent():.4f} of ${p.budget:.2f}')


if __name__ == '__main__': main(sys.argv[1:])
