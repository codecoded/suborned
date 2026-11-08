# Keel's scratch generator (brief 098, engine 0.50.0): briefs/098/keel/hold-and-doors.html from briefs/098/keel/hold-and-doors.json and c062.
import json, html
R = '/Users/sky/.t3/worktrees/suborned/t3-48e18061'
d = json.load(open(R + '/briefs/098/keel/hold-and-doors.json'))
c62 = json.load(open(R + '/runs/missions/handover/c062/run.json'))
E = ('0.48.0', '0.49.0', '0.50.0')
esc = html.escape

def td(v, cls='n'): return f'<td class="{cls}">{v}</td>'

# bands
bands = d['bands']['rows']
bt = '<table><tr><th>setting</th><th>wins 0.48.0</th><th>0.49.0</th><th>0.50.0</th><th>quiet-g</th><th>quiet-a</th><th>veteran</th><th>prepared-loud</th><th>the rest</th></tr>'
for r in bands:
    pw = lambda e, p: r[e]['planWins'][p]
    tri = lambda p: ' → '.join(str(pw(e, p)) for e in E)
    rest = []
    for p in r['0.50.0']['planWins']:
        if p in ('quiet-g', 'quiet-a', 'veteran', 'prepared-loud'): continue
        dv = pw('0.50.0', p) - pw('0.49.0', p)
        if dv: rest.append(f'{p} {dv:+d}')
    bt += f"<tr><td>{r['setting']}</td>" + ''.join(td(r[e]['wins']) for e in E) + td(tri('quiet-g')) + td(tri('quiet-a')) + td(tri('veteran')) + td(tri('prepared-loud')) + td(', '.join(rest) or 'the same', '') + '</tr>'
bt += '</table>'

# cover cycles by setting
ct = '<table><tr><th>setting</th><th>returns to cover left before: 0.48.0</th><th>0.49.0</th><th>0.50.0</th><th>runs with a fighter going back 3+ times</th><th>runs with a fighter taking cover 6+ times</th></tr>'
for r in bands:
    cv = lambda e, k: r[e]['cover'][k]
    ct += f"<tr><td>{r['setting']}</td>" + ''.join(td(cv(e, 'returns')) for e in E) + td(' → '.join(str(cv(e, 'runsWithAFighter3Returns')) for e in E)) + td(' → '.join(str(cv(e, 'runsWithAFighter6Takes')) for e in E)) + '</tr>'
tot = d['coverCycles']['totals']
ct += '<tr><th>all 2,100</th>' + ''.join(f'<th class="n">{tot[e]["returns"]}</th>' for e in E) + f'<th class="n">{" → ".join(str(tot[e]["runsWithAFighter3Returns"]) for e in E)}</th><th class="n">{" → ".join(str(tot[e]["runsWithAFighter6Takes"]) for e in E)}</th></tr></table>'

# holds by place
H9 = {(h['setting'], h['region']): h for h in d['holdsByPlace']['0.49.0']}
H0 = {(h['setting'], h['region']): h for h in d['holdsByPlace']['0.50.0']}
W8 = {(h['setting'], h['region']): h for h in json.load(open(R + '/briefs/098/keel/walk-law.json'))['holdsByPlace']['before']}
names = {'office-entrance': 'Office, public door', 'office-door': 'Office, official door', 'cafe-front': 'café front', 'fountain': 'fountain'}
ht = '<table><tr><th>place</th><th>setting</th><th>holds 5 s+, counted per plan<br>0.48.0 → 0.49.0 → 0.50.0</th><th>counted once each<br>0.49.0 → 0.50.0</th><th>of them 10 s+</th><th>longest<br>0.48.0 → 0.49.0 → 0.50.0</th></tr>'
for reg in ('office-entrance', 'office-door', 'cafe-front', 'fountain'):
    sets = ('unset', 'people=64', 'guards=64', 'x4')
    for i, s in enumerate(sets):
        a, b, w = H9[(s, reg)], H0[(s, reg)], W8[(s, reg)]
        cell = f'<td rowspan="4">{names[reg]}</td>' if i == 0 else ''
        good = 'good' if b['distinct5sOrMore'] < a['distinct5sOrMore'] else 'bad' if b['distinct5sOrMore'] > a['distinct5sOrMore'] else ''
        ht += f"<tr>{cell}<td>{s}</td>{td(f'{w['held5sOrMore']} → {a['perPlan5sOrMore']} → {b['perPlan5sOrMore']}')}{td(f'{a['distinct5sOrMore']} → {b['distinct5sOrMore']}', 'n ' + good)}{td(f'{a['distinct10sOrMore']} → {b['distinct10sOrMore']}')}{td(f'{w['maxS']:.1f} → {a['maxS']:.1f} → {b['maxS']:.1f} s')}</tr>"
ht += '</table>'

# bodies
def btab(key):
    t = '<table><tr><th>setting</th><th>runs</th><th>jumps</th><th>pairs inside two radii</th><th>tenet pairs</th><th>errors</th><th>held 10 s+</th><th>longest hold</th><th>changed plans</th></tr>'
    for b in d['bodies'][key]:
        t += f"<tr><td>{b['setting']}</td>{td(b['runs'])}{td(b['jumps'])}{td(b['insideTwoRadii'])}{td(b['tenetPairs'])}{td(b['errors'])}{td(b['held10sOrMore'])}{td(f'{b['heldMaxS10'] / 10:.1f} s')}{td(b['changedPlans'])}</tr>"
    return t + '</table>'
b9 = {b['setting']: b for b in d['bodies']['0.49.0']}

# tick cost
pt = '<table><tr><th>script</th><th>setting</th><th>walkers</th><th>ms a tick 0.49.0 → 0.50.0</th><th>p99 tick ms</th><th>start ms</th></tr>'
for p in d['tickCost']['rows']:
    a, b = p['0.49.0'], p['0.50.0']; dv = 100.0 * (b['msPerTick'] - a['msPerTick']) / a['msPerTick']
    pt += f"<tr><td>{'route A to 4:10' if p['script'].startswith('route-a') else 'loud-fight'}</td><td>{p['setting']}</td>{td(a['walkers'])}{td(f'{a['msPerTick']:.4f} → {b['msPerTick']:.4f} ({dv:+.0f} %)', 'n ' + ('mid' if dv > 3 else ''))}{td(f'{a['p99TickMs']} → {b['p99TickMs']}')}{td(f'{a['startMs']} → {b['startMs']}')}</tr>"
pt += '</table>'

# record
owners = {
 'R-7': ('Waymark, Pulse', "quiet-g's beside-guard window is under 30 s at a person's pace: 97 % → 82 % a step 30 s late (quiet-a's call and enter hold now)"),
 'B-2 thin-quiet': ('Motive', 'thin quiet wins 91.5 %, over its 60-90 % (92 % on c060): the band'),
 'B-8 quiet-g': ('Waymark', "the plan waits for the beside-guard window with the whole cell idle (median 57 s, p95 82 s): the plan's logic"),
 'B-9 quiet-b': ('Motive, Waymark', "on the lend the three left at the square wait in the open; the rivals' advances on them are no longer turned back, so they close and trade at short range: 1.09 down per win (c061 0.88, c060 0.46)"),
 'B-11 drive-early': ('Motive', 'no final fight on the plan (0-0 s)'), 'B-11 quiet-b-lookout-ignored': ('Motive', 'no final fight (0-0 s)'), 'B-11 quiet-b-taped': ('Motive', 'no final fight (0-0 s)'),
 'B-11 drive-round': ('Motive', "the drive round's fights span 9-17 s"),
 'B-11 quiet-a': ('Motive, Pulse', 'the law decides the safe fight: middle half 10-16 s (B-7 median 12 s, in band)'),
 'B-11 quiet-g': ('Motive, Pulse', 'the law decides the safe fight: middle half 15-22 s (B-7 median 17 s, in band)'),
 'B-11 thin-quiet': ('Motive, Pulse', 'the law decides the safe fight: middle half 12-16 s (B-7 median 13 s, in band)'),
 'B-14': ('Motive', '20 of 1,871 falls under 5 s, the Enforcer 1 of 167 under 7 s: the hit rolls'),
 'B-16 drive-early': ('Motive', 'the early drive is never seen (Alert ≥ 1 on 0 of 200)'),
 'L96 A': ('Motive', 'quiet-a wins 99 %, over its 97 % ceiling'),
 'L96 B-all': ('Motive, Waymark', "quiet-b's all four out on 32.5 % of wins, 1.5 captured per win: the same trade at the square as B-9 quiet-b"),
}
rt = '<table><tr><th>line</th><th>c062</th><th>owner</th><th>cause</th></tr>'
for c in c62['conditions']:
    if c['ok']: continue
    o, why = owners[c['id']]
    rt += f"<tr><td>{esc(c['id'])}</td><td>{esc(c['value'][:70])}</td><td>{o}</td><td>{esc(why)}</td></tr>"
rt += '</table>'

posts = d['doorPosts']
dp = '<table><tr><th>door</th><th>0.49.0 beat point</th><th>0.50.0 beat point</th><th>where it stands now</th></tr>'
where = {'office-entrance': "on the public steps, 1.6 m out from the door and 1.1 m off its line, on the side of the relief's waiting point (6520, -4125), 1.6 m from it", 'cafe-entrance': 'beside the café door, out from it and to one side, 1.6 m from Inez at her post', 'official-entrance': "beside the official door on the side of its waiting point at (7625, -2880), 1.6 m from it"}
for k in ('office-entrance', 'cafe-entrance', 'official-entrance'):
    dp += f"<tr><td>{k}</td><td>({posts['0.49.0'][k][0]}, {posts['0.49.0'][k][1]})</td><td>({posts['0.50.0'][k][0]}, {posts['0.50.0'][k][1]})</td><td>{where[k]}</td></tr>"
dp += '</table>'

q50 = d['coverCycles']['quietG50Seeds']
page = f'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>Brief 098: fighters hold cover, a door's guard stands beside the door, engine 0.50.0 (Keel)</title>
<style>
body{{font:15px/1.5 -apple-system,system-ui,sans-serif;max-width:1040px;margin:2em auto;padding:0 1em;color:#1d2330;background:#fbfaf6}}
h1{{font-size:1.5em;margin-bottom:.2em}}h2{{font-size:1.15em;margin-top:2em;border-bottom:1px solid #d9d4c7;padding-bottom:.2em}}h3{{font-size:1em;margin-top:1.4em}}
.s{{color:#5b6170}}table{{border-collapse:collapse;margin:.6em 0;font-size:13.5px}}td,th{{border:1px solid #d9d4c7;padding:3px 8px;text-align:left;vertical-align:top}}th{{background:#efece3}}
td.n{{text-align:right;font-variant-numeric:tabular-nums}}.good{{color:#1d6b3a}}.bad{{color:#9a2b1f}}.mid{{color:#8a6100}}code{{background:#efece3;padding:0 3px;border-radius:3px;font-size:12.5px}}
.box{{background:#fff;border:1px solid #d9d4c7;border-radius:6px;padding:.6em 1em;margin:.8em 0}}
</style></head><body>
<h1>Brief 098: fighters hold cover, and a door's guard stands beside the door. Engine 0.50.0</h1>
<p class="s">Keel, 2026-11-08. Branch <code>t3/walkable-gaps-listening</code>, on 0.49.0 at <code>a8ebe565</code>. Evidence: <code>briefs/098/keel/hold-and-doors.json</code>, its probes in <code>briefs/098/keel/probe/</code>. Before = 0.49.0 (<code>a8ebe565</code>) and 0.48.0 (<code>8f4b9ded</code>), each built from its own source and run on its own data; after = this branch.</p>
<p class="s">Aster's ruling on 0.49.0: the walk law stays (Sky: "We can do better"); the gaps it opened are real ground and the padding stays out. What it exposed are two laws that weren't there.</p>

<h2>What a player sees</h2>
<div class="box">
<p><b>The terrace wall fight.</b> On 0.49.0 a rival at the café terrace's west wall went round a loop. He advanced on the Tech and got him in reach at 15 m in the open. Then he stepped 5.6 m back to the wall's tip (cv-035), out of his own reach. A shot on the way turned him to the next point (cv-036). There he had nobody in reach, so he advanced again. In seed 1 he did it six times while the car stayed boxed in. Now he takes cover only where he can still hit the Tech. There is none at that range, so he fights from where he stands. Seed 1's fight is over in 28 s (5:17-5:46), and the cell drives away. Over 50 quiet-g seeds, rivals went back to cover they had left 321 times on 0.49.0 and 10 times on 0.48.0. On 0.50.0 it's 3 times. quiet-g wins 48 of 50.</p>
<p><b>The loud fight too.</b> In loud-fight seed 1, officer-1 went from cv-032 to cv-027 and back every 3 s, 29 times. He set off on his close to cover nearer the Tech, and half a second later the re-plan sent him back. A close is now walked through. Across the 2,100-run sweep, returns to cover fall from 1,810 to 118 (0.48.0: 841).</p>
<p><b>What gets harder.</b> A rival who leaves cover for a reason commits to it. An advance is never turned back by a step out of a line. In quiet-a seed 3, rival-1's advance takes him within 8 m of the car and the rivals reach Popovic, so quiet-a wins 19 of 20 under every setting. On route B, the three left at the square after the lend trade at short range: 1.09 down per win (c061 0.88).</p>
<p><b>The Office door.</b> On 0.49.0 the dial's guards posted 2 m in front of the public door, on its steps, and guards sharing the post spread across the steps. Now the post stands beside the door, on the side of the steps by the relief's waiting point. It is 1.1 m off the door's line and 1.6 m from the relief, so people pass between. A guard at a post never takes a place in the doorway: no crew spot, no step aside, no ring, no held spot. At x4, holds of 5 s or more at the public door, counted per plan as before (21 on 0.48.0, 75 on 0.49.0), are 7. Counted once each it's 32 → 7, none of them 10 s or more (4 before), and the longest is 15.1 → 8.6 s. At the café front the longest hold at x4 is 16.4 → 8.6 s. The fountain is unchanged: nobody held there over 1 s.</p>
<p><b>What isn't fixed.</b> At x4 the Office's official door still has 33 distinct holds of 5 s+ (36 on 0.49.0), 5 of them 10 s+ (13), the longest 26.9 s (32.6 s). Nearly all of them are the loud response's press at that door in one run, prepared-loud seed 2, where the Office clerks crowd out. It's the same press as 0.49.0's. At guards=64 that door's post stands on the side of its second waiting point: 5 distinct holds of 5 s+, the longest 13.7 s (0.49.0: 2, 6.4 s). For Vantage and Pulse, below.</p>
</div>

<h2>The laws</h2>
<h3>Fighters hold cover (rulebook <code>fight.cover</code>)</h3>
<p>One law for every fighter in a fight: the rival crew, police officers, the response unit and the police units. The blocker keeps his lane, as he does.</p>
<ol>
<li>A fighter who reaches cover that shields him from the threat holds it while it still does. He leaves only for a reason: flanked or exposed (unshielded from his target for <code>leave</code>, 2 s, or not shielded from a shooter he can't answer), his crew's plan (an advance, the press, the walk to a held car, the break), his target out of reach or sight for <code>lapse</code> (1 s; then he advances, and the advance closes), or the cover lost.</li>
<li>A move out of a shooter's line commits him to where he moved. On the walk and once there, he takes no new step out of that shooter's line while the place still shields him from where the shooter stands now, nor out of another's line it shields him from too (<code>Gun.Commit</code>). A walk to a blind point holds no cover point.</li>
<li>Cover is somewhere to fight from. A fighter whose target is in his reach steps only to cover within <code>reach</code> (1000 ‰ of his weapon's effective reach) of that target, with the line to him. With none, he fights from where he stands.</li>
<li>A move for a reason is walked through. An advance (on the car or an operative) and a police body's close are never turned back by a step out of a line. On the walk of a move out of a line, his target out of reach is no reason yet. A foe in reach still stops him, and he fights.</li>
</ol>
<p class="s">New setting: <code>cover-hold-reach-pct</code> 1000. <code>leave</code> and <code>lapse</code> are <code>ai.cover.leave</code> and <code>ai.engageLapse</code>, one value each. Code: <code>GameCoverHold.cs</code> (new), <code>GameAi.cs</code>, <code>GameKite.cs</code>, <code>RulesCoverHold.cs</code> (new).</p>
<h3>A doorway is a way (rulebook <code>bodies.door</code>)</h3>
<p>Every entrance has its way: the ground in front of its door, out to <code>depth</code> (300 cm) along the open side of the foot ground round it, and <code>half</code> (80 cm) either side of the door's line, two people abreast. A guard at a door takes a post beside it. The post is the nearest point to the door on its open side that stands a body's radius outside every door's way. It leaves a passage of two personal spaces (160 cm, centre to centre) from the door's own people: the cast stands, the door's waiting points and its going-in point. No beat point stands in a door's way. A guard at a post takes no place in one. The door's own people keep their places (the cast's door guard stands at his door). Code: <code>DoorWays.cs</code> (new), <code>Population.cs</code>, <code>GameBodyForms.cs</code>, <code>GameBodySides.cs</code>. <code>population.json</code>'s <code>beat.doorOutCm</code> is retired.</p>
{dp}
<p class="s">A passage of four radii (120 cm) was tried first. A man leaving the café waited 20 s between Inez and the guard: 120 cm leaves room only on the exact midline. Two personal spaces is the plainer law and holds fewer people.</p>
<h3>Two L9 fixes the probes turned up</h3>
<ul><li>One who sits keeps his place, so a squeeze he was in ends (route-a seed 6 at people=128: x-person-0173 stepped into cafe-table-7's seat squeezing past x-person-0157).</li>
<li>A rival walking to his spot when the fight takes him walks on from where he stands. Keeping the crew's walk dropped his body's lay: an 80 cm jump in route-a-pressure seed 2 at people=64, on 0.49.0 too.</li></ul>

<h2>Cover cycles across the sweep</h2>
<p class="s">{esc(d['coverCycles']['how'])}. The same 15 plans × 20 seeds × 7 settings on each engine.</p>
{ct}
<p>The 7 runs on 0.50.0 are one fighter under each setting: quiet-b seed 10's rival-2 makes four closing advances on four different operatives (fixer, scout, fixer, tech) as the cell moves to the lent car. Each one closes, so it isn't the old loop. 50 quiet-g seeds: 0.48.0 {esc(q50['0.48.0'])}; 0.49.0 {esc(q50['0.49.0'])}; 0.50.0 {esc(q50['0.50.0'])}.</p>

<h2>Bands</h2>
<p class="s">{esc(d['bands']['how'])}. No run errs and no replay differs on any engine. Wins are out of 300 a setting, a plan's out of 20.</p>
{bt}
<p>quiet-g falls where the law puts it: 18 of 20 in the default game (13 on 0.48.0, 5 on 0.49.0). At the dial's top guard counts (guards=64, x4) it wins none of 20, as on 0.48.0 and 0.49.0. Nothing here is tuned to win.</p>

<h2>The mission record, c062</h2>
<p>Recorded on this branch's engine and inputs, after composing every plan on it. The verdict is HOLDS with 15 lines red (c061 22, c060 12). All four references win: route A, route B, route G (quiet-g) and loud. These went green again: MC-0 (23/23), MC-5 (route G 1:15 early), MC-8 (route-g 6:05.1), B-1 quiet-g 97 %, L96 G 97 %, B-7 quiet-g (median 17 s) and thin-quiet (13 s), B-8 quiet-a, B-9 quiet-g 0.21 and B-13. New red: B-11 quiet-a, quiet-g and thin-quiet. The variety band reads a fight the law now decides quickly and alike. Off the known-red list: L96 veteran (43 % out).</p>
<p>19 route scripts composed; route-g-kill-board composes again. 8 don't, all at their plans' steps (Waymark's): route-b-thin and careless-thin-all at step 2 (the takedown of rival-3, never clear for 1 s); route-g at step 25 (cell go); loud, loud-fight, loud-left-downed and loud-left-standing at step 29 (scout move); careless-reporter-not-stopped at step 20 (cell go car:official).</p>
<p>The known-red list in <code>tools/missions/check/check.test.ts</code> now names each red line with its owner and cause, and "the Handover, played headless" passes:</p>
{rt}

<h2>Holds by place</h2>
<p class="s">{esc(d['holdsByPlace']['how'])}.</p>
{ht}

<h2>Bodies probe</h2>
<p class="s">{esc(d['bodies']['how'])}.</p>
{btab('0.50.0')}
<p>The adaptive plans' 315 runs: 0 jumps, 0 pairs inside two radii, 0 tenet pairs, 0 law overlaps and 0 errors, as on 0.49.0. Holds of 10 s or more at x4 are 20 → {d['bodies']['0.50.0'][-1]['held10sOrMore']}, the longest 32.6 → {d['bodies']['0.50.0'][-1]['heldMaxS10'] / 10:.1f} s. Over the route scripts, 0.49.0's 2 jumps and 2 pairs inside two radii are gone. The 27 tenet tick-pairs at people=64 are the Enforcer and the patrol robot 58 cm apart at 2:15.8 on seed 2 of three scripts. They were there on 0.49.0 too, and they're mine to look at next.</p>
{btab('routes0.50.0')}

<h2>Tick cost</h2>
<p class="s">{esc(d['tickCost']['how'])}.</p>
{pt}
<p>With few guards nothing changes. At x4 and guards=64 a tick costs 4 to 7 % more, about 4 µs, or 0.04 ms a second of game at 10 Hz. A split shows where it goes: {esc(d['tickCostSplit'])}. So the doorway rule saves work, and what's left is the dial's guards working round posts beside the doors. The start costs the same.</p>

<h2>Scenarios and tests touched</h2>
<p>Each was rewritten to its rule, never to a tick.</p>
<ul>
<li><b>F8-O10, F8-O10-LAB</b> (fire on the move): its rivals stood at the effective ring of the open range, where no cover keeps a rival in his reach, so under the law they stand and fight. Now a car stands on the first slot and they spawn near the cell. The car shields them 4 m back within reach, and the step to it is the walk the rule reads (4 shots on the move, each at the walk's chance).</li>
<li>New range scenarios <b>CH-REACH-NONE</b> (no cover in reach: no rival takes any, and they fire from where they stand) and <b>CH-REACH-CAR</b> (each cover a rival takes is within his reach of his target), in <code>ScenariosCoverHold.cs</code>.</li>
<li>New tests: <code>CoverHold.cs</code> (no fighter goes back to cover he left three or more times; returns in fewer than a third of fight runs, 19 of 96 now, each once; a step out of a line is never turned on its walk) and <code>DoorWays.cs</code> (every door post beside its door, off every way, with the passage; no dial guard at rest at a post stands in a doorway at guards=64 or at guards and people 64).</li>
<li>Bodies tests with the bar unchanged, the laws looked for on more runs. The make-room law on a second seed. The squeeze law on six seeds of its four runs. The change-plans law where it still comes up (route B at people=64, the body found at guards=64 and people=64): with posted guards out of the doorways, its three runs showed none. <code>Bodies.cs</code> reads the post points as the game does.</li>
<li><code>rulebook098b.test.ts</code>: the doorway's values and <code>fight.cover</code>. DP1 re-recorded.</li>
</ul>

<h2>What Latch must draw</h2>
<ul>
<li><b>Seated listeners</b> (0.49.0): <code>ListenSpotNow</code>'s <code>at</code> is the seat, and <code>SeatedAt(name)</code> says she's in it. She sits as a customer does and stands when her next order walks her out.</li>
<li><b>The step into a chair</b>: across the table's ground at a walk, the Scout and customers alike.</li>
<li><b>The idle step aside</b> (<code>BodyIdle</code>): at a walk and back, no snap.</li>
<li><b>The door posts</b>: the dial's guards at the Office's public door stand on the steps by the relief's side, facing out over the forecourt. At the café door and the official door they stand beside the door, each 1.6 m from the door's own people (table above). Guards sharing a post stand off the doorway, on the steps' sides or beyond its 3 m.</li>
<li>The fights need no new drawing (positions only): rivals stand in the open and fire more, and walk their advances through.</li>
</ul>

<h2>For Pulse and Motive to review</h2>
<ol>
<li>With no cover that keeps his target in reach, a rival fights in the open. The safe fight is decided in 10 to 22 s (B-7 in band), and B-11's spread falls under 10 s. Should a decided fight still owe that spread? (Motive)</li>
<li>An advance is never turned back. Route B's square trades (B-9 quiet-b 1.09, L96 B-all) and quiet-a seed 3's lost run come from it. (Motive, Pulse; Waymark for route B's plan.)</li>
<li>Cover to fight from needs the line from the point. Points behind a tall wall shield but don't count. Is that the cover generator's intent? (Pulse, <code>tools/cover</code>)</li>
<li>The police: the answering re-plan waits while the place still shields them, and a close (§7.10 rule 2) is walked through. That ended officer-1's 29 returns in loud-fight seed 1. (Pulse)</li>
<li><code>cover-hold-reach-pct</code> 1000 is a starting value. (Motive)</li>
<li><code>cover-snap-cm</code>'s reason still cites the 65 cm walk margin, stale since 0.49.0. (Motive's number)</li>
<li>GDD §06's "answering fire" and "hostiles" lines need the law at the next version. (Aster)</li>
</ol>

<h2>Handoffs</h2>
<ul>
<li><b>Waymark</b>: the 8 plans that don't compose (above); quiet-g's idle wait (B-8) and its beside-guard window (R-7). From 0.49.0: veteran's 13 m draw rule in a crowd, the loud family's Go before the rival car leaves, and careless-thin-all and route-b-thin at rival-3's post.</li>
<li><b>Pulse</b>: the static read of Inez's orders in <code>tools/routines/rounds.test.ts</code>, out of date with the seated Listen, left failing for you. The official door's press in prepared-loud seed 2 at x4. The full café at people=64 (from 0.49.0).</li>
<li><b>Vantage</b>: the official door's post stands on the side of its waiting point at (7625, −2880). Its two candidates, one past each waiting point, are the same distance from the door, so the side is the first in a fixed order. The holds there at guards=64 are 5, the longest 13.7 s. A layout reason to prefer a side would go in the door's data.</li>
<li><b>Motive</b>: the bands in the known-red list.</li>
<li><b>Keel</b> (next): the Enforcer and the patrol robot as two tenets 58 cm apart on route scripts, seed 2 at 2:15.8.</li>
<li><b>Aster</b>: the known-red list carries owners and causes. Merge, then Latch builds.</li>
</ul>

<h2>Checks</h2>
<table><tr><th>check</th><th>result</th></tr>
<tr><td><code>suborned-sim check</code></td><td>472/472</td></tr>
<tr><td><code>bun run sim:test</code></td><td>3,313/3,313 (130 s)</td></tr>
<tr><td><code>suborned-sim hashes</code></td><td>exit 0</td></tr>
<tr><td><code>suborned-sim gaps</code></td><td>161/161 both ways</td></tr>
<tr><td>bodies probe, 315 runs</td><td>0 jumps, 0 pairs inside two radii, 0 tenet pairs, 0 law overlaps, 0 errors</td></tr>
<tr><td>holds by place, bands with the cover cycles, tick cost</td><td>above, against 0.49.0 and 0.48.0</td></tr>
<tr><td>comment lint</td><td>0 hits (1,518 files)</td></tr>
<tr><td><code>bun run typecheck</code></td><td>clean</td></tr>
<tr><td><code>bun run test</code></td><td>2,630 pass, 8 fail, 58 skip. All 8 already failed at HEAD: 5 skyline (Splice, Vantage); 2 drift-auto end to end (Pulse, Splice); rounds.test.ts Inez's static read (Pulse). "The Handover, played headless" passes.</td></tr>
<tr><td>missions:check, c062</td><td>HOLDS, 15 red, each with its owner</td></tr>
<tr><td>500-line rule</td><td>no file grew past or over it; the new files are under it</td></tr>
</table>
<p class="s">Branch <code>t3/walkable-gaps-listening</code>, head: the commit that adds this report.</p>
</body></html>
'''
open(R + '/briefs/098/keel/hold-and-doors.html', 'w').write(page)
print(len(page))
