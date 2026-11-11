"""Brief 104 · patrol robot concepts (Vela, 2026-11-10): the Codex prompts.

Every prompt = the style contract's concept_prompt (era, city, avoid; verbatim) + the open brief (facts about the robot, not a design)
+ the sheet's framing + one direction line + the finish. Run from the repo root:
    python3 -I briefs/104/vela/patrol-concepts/make/prompts.py            # writes prompts.json beside this file's folder
    python3 -I briefs/104/vela/patrol-concepts/make/prompts.py A          # prints sheet A's prompt
"""
import json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
HERE = Path(__file__).resolve().parent

BRIEF = """The brief: concept designs for the city's patrol robot, the machine that keeps watch on the civic square. Facts about it in this world (the design is yours):
- Its job: it walks a fixed round, slowly and steadily, through the civic square and past the Records Office. It notices people who linger and reports them to the security desk by radio. When the city's alert rises, robots like it are sent out from street docks, booths about one metre wide inside, so it must fit standing in one.
- It is clockwork-smart and follows fixed routines: not a thinking machine, no networks. Information moves through people, paper and wires; cameras are watched by people.
- It sees only the way its face points, never all round, and its back is blind, so which way it faces must be obvious at a glance, even from above.
- Its face is a dot-matrix display: a dark panel of small round lamps showing simple glowing patterns for four stages: calm, noticed, wary, reporting. No cartoon eyes, no sculpted or painted eyes, no camera lenses as eyes.
- When it answers an alarm it shows an alarm signal that glows red, with its own distinct shape or movement; on its round the signal is dark.
- It can stand its ground in a fight and is harder to stop than a person. In daily life it is the civic face of an abundant city that fears disorder: polite and friendly on the surface, with authority underneath.
- Like all the city's robots it carries one personality prop that shows its job at a glance. A hard enamel shell and bare chrome joints only: nothing hangs from the body (no cloth, apron, skirt, cape or bag). Plain enamel panels: no lettering, numbers, labels or badges.
- Colour: cream and ivory enamel, seafoam-teal bands, chrome, coral or peach accents; brass only as a thin hairline. More colour, less shine: matte enamel and soft satin chrome, never mirror-glossy.
- It must read from a high camera looking down at 38 degrees, where a person is only about 80 pixels tall: a bold, simple silhouette and a distinctive top, clearly different from a walking person, a uniformed guard, and the city's waiter, butler and luggage-porter robots.
- Original designs only: nothing resembling any robot from a film, TV show or game. No domed astromech, no pepper-pot, no classic tin-man, no glass bubble-helmet head, no armoured police cyborg."""

SHEET = """The sheet: a wide landscape robot concept lineup. Exactly six clearly different patrol robot designs standing in one row, evenly spaced and not overlapping, full body from top to ground, all at the same scale, in the same gentle three-quarter front view seen from slightly above, with soft contact shadows. At the far left, a plain flat mid-grey human silhouette 1.75 m tall, for scale only. Vary the body plan, the height (from waist-high to about a person's height) and how each one moves."""

FINISH = """Finish: high-art gouache on matte paper with crisp, confident ink linework, flat matte enamel colour, soft satin chrome without hot highlights, warm soft light; a mid-century design illustration, not a glossy CG render or a photo. Plain warm paper-cream backdrop, clear margin all round. No text, numbers, labels or watermark anywhere on the sheet."""

DIRECTIONS = {
    'A': ('civic', 'Friendly civic', "Direction for this sheet: the friendly civic face. Approachable, rounded and courteous, like a well-loved public institution: the robot a child would wave at, still plainly on duty. Soft curves, generous enamel, a polite posture. Each robot's face shows its calm pattern."),
    'B': ('stern', 'Quietly stern', "Direction for this sheet: quietly stern. Courteous but unmistakably official: upright, composed, still and watchful, the authority under the city's polish. Restrained tailored lines, more ivory and deep teal with a little ink-black, chrome; calm rather than menacing, never military, never armoured like a soldier. Each robot's face shows its calm pattern."),
    'C': ('signal', 'Signal and silhouette', "Direction for this sheet: designed around its alarm signal and its silhouette. Each robot's outline and top must be unmistakable from high above, and each carries its alarm signal as a defining shape of the design (for example a ring, a crest, a fin, a mast or a halo, or something better), each one different. Show every robot with its alarm signal lit, glowing red, as when it answers an alarm."),
}


def head():
    cp = json.loads((ROOT / 'contracts/style/suborned.style.json').read_text())['concept_prompt']
    return cp['era'] + '\n\nThe city: ' + cp['city'] + '\n\nAvoid: ' + '; '.join(cp['avoid']) + '.'


def prompt(key):
    return '\n\n'.join([head(), BRIEF, SHEET, DIRECTIONS[key][2], FINISH])


def main(a):
    if a:
        print(prompt(a[0])); return 0
    out = {'note': 'Brief 104 patrol robot concepts: each prompt = concept_prompt (era, city, avoid) + brief + sheet + direction + finish.',
           'parts': {'brief': BRIEF, 'sheet': SHEET, 'finish': FINISH},
           'sheets': {k: {'file': f'{k}-{v[0]}.png', 'label': v[1], 'direction': v[2], 'prompt': prompt(k)} for k, v in DIRECTIONS.items()}}
    (HERE.parent / 'prompts.json').write_text(json.dumps(out, indent=1, ensure_ascii=False))
    print(f'wrote {(HERE.parent / "prompts.json").relative_to(ROOT)}'); return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
