"""Brief 101 round 3 (Glyph's r3, Sky's yes): the report, briefs/101/latch/r5.html, from this round's evidence: the side-by-sides and 2x crops (r5.py), the per-piece
crops (r5_parts.py), coverage-r5.json, the proof's rows (out/build/play/proof/proof.json), the budget lines and the frame times given on the command line.
Usage: python3 -I r5_report.py briefs/101/latch/make/r5-facts.json   (facts: build, path, draw check, crops, budget, frame time, batch, open items)
Latch."""
import html, json, os, sys

R = '/Users/sky/Projects/suborned/'
OUT = R + 'briefs/101/latch/r5.html'
E = html.escape

PIECES = [
    ('1', "Inez's card as a slip on her", "the book's cream page on a fine stem to her head, her face in the cream ring breaking its corner, her name small; Debt and Secret as their kit pictograms before Fable's lines; the book's hairline; \"New\" in the book's deep teal beside Help and Pressure as capsules (a click opens her ring on that way; no order is given). Off screen it never docks: it waits, its read time held, and her key face (top right) carries the Book's new dot. With no room over her head it hangs under her feet on its stem.", 'HudToolkit.Card.cs; HostConvo.Cards.cs (PersonCardHold); HostBook.cs (WayClick); HudToolkit.Clock.cs (the dot)'),
    ('2', 'The next-event row tagged next', "the row's manifest kind is Splice's `next` (a name of at most 3 words and 16 characters, one m:ss)", 'HudToolkit.Clock.cs'),
    ('3', 'Timed tabs', "window markers: the window's pictogram on a 26 px tab whose cream ring drains, the time beside it (\"55 s\", m:ss past a minute), orange on ink in the last 10 s; the holder's face while the Tech holds a loop. Attention clocks: the open eye on a 24 px tab whose ring fills toward the report, the stage word beside it, orange from \"wary\"", 'HudToolkit.Timed.cs (moved out of Done.cs and Hold.cs); StepAside.cs'),
    ('4', 'Pops', "icon and words as a street label, no plate; a refusal wears mark_no on an ink disc; \"Report stopped\" is the report's own pictogram on an ink disc, struck through; \"Click again to stop\" leads with the pointer; who-can and stance pops keep their faces and marks", 'HudToolkit.Pops.cs (moved out of Less.cs); HostWhy.Reports.cs, HostActivity.cs (their art)'),
    ('5', 'Done chips', "the doer's face in the cream ring with the tone's badge, one keyed pictogram and word a fact, then › and the new ways as capsules (each a click: the thing's ring opens on that way); a shut way struck", 'HudToolkit.Gains.cs (the look; placement stays in Done.cs)'),
    ('6', 'The act ring', "the way's word and its price as street labels (the lit way's word in the book's deep teal; a shut way's soft, with mark_no), the price's pictograms keyed, the eyes soft (open: the cones' red), the outcome badge on a cream disc; the wheel's rim cream", 'HudToolkit.RingLabels.cs; HudToolkit.Ring.cs'),
    ('7', 'The callout', "the watcher's close-up in the cream ring (a camera: its red eye on the book's body), the agent's face at its lower right with 1-3 orange stage ticks, \"+n\" as a street label, a fine cream stem to the watcher's head; docked, the ink chevron", 'HudToolkit.Callout.cs (moved out of Attention.cs)'),
    ('8', 'The sweep', "step markers (door, car, turn, the next step as a capsule, bodies, stands, Popovic's take, the rivals' stop) as round tabs; pips as the badge in the cream ring; the hint disc and the reopen disc as round tabs; the hint line as a street label; edge chips (reroute, the rival car, a lock) as round tabs with the ink chevron; plan ghosts and stash names as street labels (a stash name under its point, clear of the clock stack); every remaining face in the cream ring (the aim row's shooters among them)", 'HudToolkit.StepMarks.cs, Takedown.cs, Rivals.cs, RivalsForCar.cs, Pips.cs (moved out of Less.cs), Mood.cs, HintLine.cs (moved out of Less.cs), Reroute.cs, Lock.cs, Hold.cs, Plate.cs (Cameo), Pill.cs'),
    ('+', "The lozenge's hover", "hovering an Alert or Police card shows its cause's full line, filled, as a street label over the news (wrapping at 460 px); leaving hides it. The proof checks it through the real pointer (r101 lozengeHover)", 'HudToolkit.Crucial.cs; Proof101.cs'),
]


def figure(src, cap):
    return f'<figure><img src="{E(src)}" alt="{E(cap)}"><figcaption>{E(cap)}</figcaption></figure>'


def main():
    facts = json.load(open(sys.argv[1]))
    cov = json.load(open(R + 'briefs/101/latch/coverage-r5.json'))['rows']
    h = ['<!doctype html><meta charset="utf-8"><title>101 round 3 built: the rest of the old kit (Latch, ' + E(facts['build']) + ')</title><style>body{font:15px/1.45 -apple-system,system-ui,sans-serif;margin:0;padding:24px;max-width:1180px;color:#17302f;background:#f7f4ec}h1{font-size:22px;margin:0 0 4px}h2{font-size:17px;margin:28px 0 8px;border-bottom:1px solid #d2e6e2;padding-bottom:4px}p.sub{color:#4a6461;margin:0 0 16px}table{border-collapse:collapse;width:100%;font-size:13.5px}td,th{border-bottom:1px solid #d2e6e2;padding:5px 6px;text-align:left;vertical-align:top}th{background:#e8f1ee}code{font-size:12.5px;background:#e8f1ee;padding:0 3px;border-radius:3px}figure{margin:0 0 18px}figure img{width:100%;border:1px solid #d2e6e2}figcaption{font-size:12.5px;color:#4a6461}li{margin:3px 0}</style>']
    h.append(f'<h1>101 round 3 built: the rest of the old kit (Latch, {E(facts["build"])})</h1>')
    h.append(f'<p class="sub">Latch, {E(facts["date"])} · build {E(facts["build"])} ({E(facts["path"])}; {E(facts["drawCheck"])}) · Glyph\'s round 3 (briefs/101/glyph/r3/, Sky: "yes"), built in his order. Each moment below is this build\'s frame beside Glyph\'s r3 frame, with how much of the screen the HUD takes. Pictures: briefs/101/latch/r5/ (git-ignored).</p>')
    h.append('<h2>The captures beside the mocks</h2>')
    for key, v in cov.items():
        h.append(figure(f'r5/side-{key}.png', f'{v["what"]}. This build {v["now"]} % of the screen; Glyph\'s mock {v["mock"]} %; today ({"0.2.287" if key == "ring" else "0.2.286"}) {v["today"]} %.'))
    h.append('<h2>Coverage</h2><table><tr><th>moment</th><th>today</th><th>Glyph\'s mock</th><th>this build</th><th>picture</th></tr>')
    for key, v in cov.items(): h.append(f'<tr><td>{E(key)}</td><td>{v["today"]} %</td><td>{v["mock"]} %</td><td><b>{v["now"]} %</b></td><td><code>{E(v["picture"])}</code></td></tr>')
    h.append('</table><p>The HUD layer alone (each capture\'s .hud.png), alpha 24 or more, at 1920 x 1080: how tools/hud-budget reads a capture. Keylines and shadows count; ground marks are world meshes and do not.</p>')
    h.append('<h2>The pieces at 2x</h2>')
    for f in facts.get('crops', []): h.append(figure(f[0], f[1]))
    h.append("<h2>What's built</h2><table><tr><th>#</th><th>piece</th><th>as built</th><th>files</th></tr>")
    for n, name, what, files in PIECES: h.append(f'<tr><td>{n}</td><td>{E(name)}</td><td>{E(what)}</td><td><code>{E(files)}</code></td></tr>')
    h.append('</table>')
    h.append('<h2>Also this round</h2><ul>' + ''.join(f'<li>{E(x)}</li>' for x in facts.get('also', [])) + '</ul>')
    h.append('<h2>The HUD budget</h2><ul>' + ''.join(f'<li>{E(x)}</li>' for x in facts['budget']) + '</ul>')
    h.append('<h2>Frame time (the Studio)</h2><ul>' + ''.join(f'<li>{E(x)}</li>' for x in facts['frames']) + '</ul>')
    h.append('<h2>The batch</h2><ul>' + ''.join(f'<li>{E(x)}</li>' for x in facts['batch']) + '</ul>')
    h.append('<h2>Open, and next owners</h2><ul>' + ''.join(f'<li>{E(x)}</li>' for x in facts['open']) + '</ul>')
    open(OUT, 'w').write('\n'.join(h) + '\n'); print(OUT)


if __name__ == '__main__':
    main()
