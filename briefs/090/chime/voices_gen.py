"""Brief 090 (Chime): voice takes through tools/paid/paid.py, job 090-voices pass 1 ($5). One call per line.
   python3 -I briefs/090/chime/voices_gen.py audition          Inworld: one line in each candidate voice (it publishes no voice descriptions)
   python3 -I briefs/090/chime/voices_gen.py make <service>    inworld | qwen | fish: the scene, the shouts and the name lines in its cast"""
import json, re, sys, time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]; sys.path.insert(0, str(REPO / 'tools/paid')); sys.path.insert(0, str(HERE))
import paid, lines as LN
MODEL = {'inworld': 'inworld:tts@1.5-max', 'qwen': 'alibaba:qwen@3-tts-1.7b-voicedesign', 'fish': 'fishaudio:s2.1@pro'}
CAST = json.loads((HERE / 'cast.json').read_text()) if (HERE / 'cast.json').exists() else {}
AUD_F = ['Loretta', 'Darlene', 'Marlene', 'Evelyn', 'Celeste', 'Pippa', 'Tessa', 'Deborah', 'Elizabeth', 'Olivia', 'Sarah', 'Wendy', 'Julia', 'Serena', 'Claire', 'Victoria', 'Miranda', 'Hana', 'Luna', 'Ashley', 'Jessica', 'Lauren', 'Chloe', 'Abby', 'Veronica', 'Kelsey']
AUD_M = ['Mortimer', 'Theodore', 'Ronald', 'Dennis', 'Graham', 'Clive', 'Malcolm', 'Hades', 'Victor', 'Edward', 'Mark', 'Craig', 'Shaun', 'Gareth', 'Rupert', 'Hamish', 'Callum', 'Simon', 'Oliver', 'Timothy', 'Hank', 'Derek', 'Grant', 'Carter', 'Blake', 'Elliot', 'Sebastian', 'James', 'Liam', 'Brian', 'Dominus']


def say(text, svc):
    for name, (ipa, resp) in LN.SAY.items():
        text = re.sub(rf'\b{name}\b', f'/{ipa}/' if svc == 'inworld' else resp, text)
    return text


def task(svc, who, text):
    c = CAST[svc][who]
    if svc == 'inworld': return {'model': MODEL[svc], 'speech': {'text': text, 'voice': c['voice']}, 'settings': {'temperature': 0.8}}
    if svc == 'qwen': return {'model': MODEL[svc], 'positivePrompt': c['describe'], 'speech': {'text': text, 'voice': 'design', 'language': 'English'}}
    return {'model': MODEL[svc], 'speech': {'text': text, 'voice': c['voice']}}


def run(jobs):
    p = paid.Pass('090-voices', '1', budget=5.0, note='Spoken voices, first test (brief 090; Sky yes, $5)')
    def one(j):
        name, t, prompt, svc = j; t = {'outputFormat': 'WAV', 'numberResults': 1, **t}
        for a in range(4):
            try: return p.audio(t, name=name, subdir=svc, est=0.02, prompt=prompt, params={'service': svc, 'voice': t.get('speech', {}).get('voice'), 'describe': t.get('positivePrompt')}, note=name)
            except SystemExit as e:
                if 'serviceOverloaded' not in str(e) or a == 3: print(name, str(e)[:300]); return None
                time.sleep(15 * (a + 1))
    with ThreadPoolExecutor(4) as ex: list(ex.map(one, jobs))
    print(f'pass 1: ${p.spent():.4f} of ${p.budget:.2f}')


if __name__ == '__main__':
    if sys.argv[1] == 'audition':
        line = "Have you eaten? You've looked at that Office door all shift."
        run([(f'aud_{v}.wav', {'model': MODEL['inworld'], 'speech': {'text': line, 'voice': v}, 'settings': {'temperature': 0.8}}, line, 'inworld-audition') for v in AUD_F + AUD_M])
    else:
        svc = sys.argv[2]; J = []
        for i, (who, line) in enumerate(LN.SCENE, 1): J.append((f'scene{i:02d}_{who}.wav', task(svc, who, say(line, svc)), say(line, svc), svc))
        for i, (who, line, _) in enumerate(LN.SHOUTS, 1): J.append((f'shout{i:02d}_{who}.wav', task(svc, who, say(line, svc)), say(line, svc), svc))
        for i, (who, line, _) in enumerate(LN.NAMES, 1): J.append((f'name{i:02d}_{who}.wav', task(svc, who, say(line, svc)), say(line, svc), svc))
        run(J)
