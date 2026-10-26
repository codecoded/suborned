"""Intake stage, step 4 (brief 069 v2): build the intake prompt for a person from intake.json's template and slots, and (run) make the still with the
Codex CLI at $0 through the paid helper, the grey mannequin sheet of the person's sex attached as the pose reference.
  python3 -I tools/figures/charpipe/intake_prompt.py show <test id>
  python3 -I tools/figures/charpipe/intake_prompt.py run <pass> <mannequin dir> [test id ...]      (writes paid/069-intake/codex/<pass>_<id>.png)"""
import json, os, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
IN = json.load(open(os.path.join(HERE, 'intake.json'))); TESTS = {p['id']: p for p in json.load(open(os.path.join(HERE, 'intake_tests.json')))['people']}


def prompt(p):
    t, s = IN['prompt'], IN['sheet']
    return t['template'].format(views=t['views_words'], scale=s['scale'][0].upper() + s['scale'][1:], projection=s['projection'][0].upper() + s['projection'][1:],
                                pose_words=t['pose_words'], person=p['person'][0].upper() + p['person'][1:] + '.', outfit=p['outfit'][0].upper() + p['outfit'][1:] + '.',
                                palette=p['palette'], rules=t['rules_words'], background=s['background'][0].upper() + s['background'][1:],
                                light=s['light'][0].upper() + s['light'][1:], style=s['style'][0].upper() + s['style'][1:])


if __name__ == '__main__':
    a = sys.argv[1:]
    if a[0] == 'show': print(prompt(TESTS[a[1]]))
    elif a[0] == 'run':
        pas, mdir, ids = a[1], a[2], a[3:] or list(TESTS)
        for i in ids:
            p = TESTS[i]; ref = os.path.join(mdir, f"mannequin_{p['sex']}.png")
            subprocess.run(['python3', '-I', os.path.join(ROOT, 'tools/paid/paid.py'), 'codex', '069-intake', pas, '--name', f'{pas}_{i}.png', '--prompt', prompt(p), '--ref', ref,
                            '--note', f'brief 069 v2 intake test {i} (pose ref {os.path.basename(ref)})'], check=False)
