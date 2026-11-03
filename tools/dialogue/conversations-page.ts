// Brief 085: the page Sky reads, briefs/085/conversations.html. Left: what the game plays today, from text.json through the host's own rules (HostConvo.cs). Right: the proposal.
//   bun tools/dialogue/conversations-page.ts            the scenes in briefs/085/fable/scenes.json (written by Fable in session)
//   bun tools/dialogue/conversations-page.ts --run r001 the finals of a pipeline run under runs/conversations/085
import { resolve } from 'node:path';
import { lookupScene, pickLine, playScene, type TextFile } from '../../contracts/missions/handover/text.schema';
import { root } from '../rag/config';

const json = async (p: string) => JSON.parse(await Bun.file(resolve(root, p)).text());
const t: TextFile = await json('contracts/missions/handover/text.json');
const SEED = 'handover-1', ids = ['listen-inez', 'help-inez-1', 'help-inez-2', 'pressure-inez', 'pay-driver', 'call-inez'];
const runArg = Bun.argv.includes('--run') ? Bun.argv[Bun.argv.indexOf('--run') + 1] : undefined;
type S = { id: string; beats: [string, string][]; learned: string[]; opens: string[] };
const scenes: S[] = runArg
  ? await Promise.all(ids.map(async id => { const f = await json(`runs/conversations/085/${runArg}/${id}/final.json`); return { id, beats: f.beats.map((b: { speaker: string; line: string }) => [b.speaker, b.line]), learned: f.learned, opens: f.opens }; }))
  : (await json('briefs/085/fable/scenes.json')).scenes;
const scene = (id: string) => scenes.find(s => s.id === id)!;

const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const words = (s: string) => s.trim().split(/\s+/).length;
const secs = (s: S) => Math.round(s.beats.reduce((n, b) => n + words(b[1]), 0) * 0.3 + s.beats.length * 0.5);
const NAMES: Record<string, string> = { waitress: 'Inez', guard: 'Guard', driver: 'Driver', fixer: 'Fixer', scout: 'Scout' };
const ROLES: Record<string, string> = { Inez: 'waitress', Milena: 'waitress, inside', Lukac: "Popovic's driver", Arthur: 'door guard' };

type Where = 'cut' | 'bub';
type Row = [Where, string, string];
/** Today's lines, split as the host splits them: a Listen's cutaway is the Scout's arrive + the first two overheard beats; an approach's is the lines before the reply, and only the first time with that person (the default setting); a call's is two lines, and the first time only. */
function today(q: Parameters<typeof lookupScene>[1], cutaway: number, leadIn: Row[] = [], tail: string[] = []): { rows: Row[]; result: string } {
  const sc = lookupScene(t, q)!, played = playScene(t, sc, SEED, 0);
  const rows: Row[] = [...leadIn];
  const left = Math.max(0, cutaway - leadIn.length);
  played.beats.forEach((b, i) => rows.push([i < left ? 'cut' : 'bub', NAMES[b.speaker] ?? b.speaker, b.text]));
  for (const x of tail) rows.push(['bub', 'Scout', x]);
  const result = played.result.replace('{temperament}', 'Fearful').replace(/\{who\}/g, q.about === 'driver' ? 'The driver' : 'Inez');
  return { rows, result };
}
const line = (q: Parameters<typeof pickLine>[2]) => pickLine(t, SEED, q, 0) ?? '';
const arrive: Row = ['cut', 'Scout', line({ event: 'arrive', verb: 'listen', speaker: 'scout' })];
const read = line({ event: 'read', verb: 'listen', speaker: 'scout', temperament: 'fearful', about: 'waitress' });

const leftCol = (d: { rows: Row[]; result: string }, note: string) => {
  const cut = d.rows.filter(r => r[0] === 'cut'), bub = d.rows.filter(r => r[0] === 'bub');
  const list = (rows: Row[]) => rows.map(r => `<p class="l"><b>${esc(r[1])}</b> ${esc(r[2])}</p>`).join('');
  return `<div class="col old"><h3>In the game today</h3><p class="note">${note}</p>
  ${cut.length ? `<h4>Cutscene</h4>${list(cut)}` : '<h4>Cutscene</h4><p class="none">None plays.</p>'}
  <h4>Bubbles over the street</h4>${list(bub)}<p class="res">${esc(d.result)}</p></div>`;
};
const rightCol = (ss: { s: S; label?: string }[], note: string) => {
  const seen = new Set<string>();
  const one = ({ s, label }: { s: S; label?: string }) => `${label ? `<h4>${esc(label)}</h4>` : ''}${s.beats.map(([w, l]) => {
    const first = !seen.has(w); seen.add(w);
    return `<p class="l"><b>${esc(w)}</b>${first && ROLES[w] ? ` <i>${esc(ROLES[w])}</i>` : ''} ${esc(l)}</p>`;
  }).join('')}<div class="end">${s.learned.map(x => `<span>${esc(x)}</span>`).join('')}${s.opens.map(x => `<span class="open">${esc(x)}</span>`).join('')}</div>`;
  return `<div class="col new"><h3>Proposed</h3><p class="note">${note}</p>${ss.map(one).join('')}</div>`;
};

const rows = [
  { title: 'Listen to Inez', why: 'You learn something. She is fearful, in the game on every seed.',
    left: leftCol(today({ kind: 'overheard', verb: 'listen', about: 'waitress', temperament: 'fearful' }, 3, [arrive], [read]), 'About 9 s, then scraps. Nobody mentions the debt on this seed, yet the result says she\'s in debt. The guard is 75 m away, off screen, in a bubble with no name.'),
    right: rightCol([{ s: scene('listen-inez') }], `About ${secs(scene('listen-inez'))} s. She talks to the woman she works with, because the guard really is across the square.`) },
  { title: 'Help Inez', why: 'You turn her the kind way. Help takes two visits with a fearful person.',
    left: leftCol(today({ kind: 'approach', verb: 'help', about: 'waitress', outcome: 'turned', temperament: 'fearful' }, 0), 'Listen already used up her one cutscene, so there isn\'t one. Nobody says what she has agreed to.'),
    right: rightCol([{ s: scene('help-inez-1'), label: 'First visit' }, { s: scene('help-inez-2'), label: 'Second visit' }], `About ${secs(scene('help-inez-1'))} s and ${secs(scene('help-inez-2'))} s. You find out what you're asking of her when she does.`) },
  { title: 'Pressure Inez', why: 'The same woman, the other way in. It costs you something different.',
    left: leftCol(today({ kind: 'approach', verb: 'pressure', about: 'waitress', outcome: 'turned', temperament: 'fearful' }, 0), 'No cutscene, as above. She opens with the same line as in Help.'),
    right: rightCol([{ s: scene('pressure-inez') }], `About ${secs(scene('pressure-inez'))} s. Colder, and the last line is true: she can identify the Fixer.`) },
  { title: 'Pay the driver', why: 'Someone else, who prices everything. The other route.',
    left: leftCol(today({ kind: 'approach', verb: 'pay', about: 'driver', outcome: 'turned', temperament: 'mercenary' }, 3), 'The cutscene stops before the answer. Nobody says where the car goes.'),
    right: rightCol([{ s: scene('pay-driver') }], `About ${secs(scene('pay-driver'))} s. The whole job gets spelled out, because the driver insists on it.`) },
];
const call = scene('call-inez');
const callToday = today({ kind: 'call', verb: 'call', about: 'waitress', temperament: 'fearful' }, 0);
const callLeft = leftCol({ ...callToday, rows: callToday.rows.slice(0, t.less.feed.cityBeats) }, 'The moment the front door hangs on gets no cutscene, because she has already had hers. Only the first 3 lines play, so the guard never answers.');

const routeA = secs(scene('listen-inez')) + secs(scene('help-inez-1')) + secs(scene('help-inez-2')) + secs(call);
const routeP = secs(scene('listen-inez')) + secs(scene('pressure-inez')) + secs(call);

const html = `<!doctype html><html lang="en"><meta charset="utf-8"><title>Conversations, brief 085</title><meta name="viewport" content="width=device-width,initial-scale=1">
<style>
:root{--ink:#26302f;--mute:#66706c;--paper:#fbf5e9;--card:#fffdf7;--sea:#8fcfc0;--brass:#b08d4a;--coral:#d9694f}
*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.45 Georgia,'Times New Roman',serif}
main{max-width:1180px;margin:auto;padding:28px 24px 60px}header{border-bottom:5px solid var(--sea);padding-bottom:12px;margin-bottom:18px}
h1{font-size:30px;margin:0}h2{font-size:22px;margin:34px 0 2px;border-top:1px solid var(--brass);padding-top:16px}h3{font:700 12px/1.2 Helvetica,Arial,sans-serif;letter-spacing:.08em;text-transform:uppercase;margin:0 0 4px}
h4{font:700 12px/1.2 Helvetica,Arial,sans-serif;color:var(--mute);margin:14px 0 6px}.why{color:var(--mute);margin:0 0 12px}
.warn{background:#fdeee2;border-left:5px solid var(--coral);padding:10px 14px;margin:14px 0}
.grid{display:grid;grid-template-columns:minmax(0,5fr) minmax(0,7fr);gap:18px}.col{background:var(--card);border:1px solid #e6dcc6;border-radius:10px;padding:14px 16px}
.old{font-size:14.5px;color:#4e5754}.new{border-color:var(--sea);border-width:2px}.new .l{font-size:17px}
.note{font:13px/1.35 Helvetica,Arial,sans-serif;color:var(--mute);margin:0 0 8px}.l{margin:0 0 6px}.l b{font:700 12.5px Helvetica,Arial,sans-serif;margin-right:5px}.l i{font:12px Helvetica,Arial,sans-serif;color:var(--mute);margin-right:5px}
.none{font-style:italic;color:var(--coral);margin:0}.res{font:13px/1.35 Helvetica,Arial,sans-serif;border-top:1px dotted var(--brass);padding-top:6px;margin:8px 0 0}
.end{display:flex;flex-wrap:wrap;gap:6px;margin:10px 0 2px}.end span{font:13px Helvetica,Arial,sans-serif;background:#f1eadb;border-radius:999px;padding:3px 10px}.end .open{background:var(--sea);font-weight:700}
table{border-collapse:collapse;width:100%;font-size:15px;margin-top:6px}td,th{border-top:1px solid #e6dcc6;padding:8px 10px 8px 0;text-align:left;vertical-align:top}th{font:700 12px Helvetica,Arial,sans-serif}
details{margin-top:26px}summary{cursor:pointer;font-size:19px}footer{font:13px/1.45 Helvetica,Arial,sans-serif;color:var(--mute);margin-top:30px;border-top:1px solid var(--brass);padding-top:10px}
@media(max-width:820px){.grid{grid-template-columns:1fr}}
</style><main>
<header><h1>Conversations: what we have, what they could be</h1><p class="why">Brief 085, your note 7. Nothing in the game has changed. The lines to judge are on the right.</p></header>
${runArg ? '' : `<div class="warn"><b>The generator has not run these.</b> I wrote them by hand, to the same facts and checks, because a live model call from an agent needs your go-ahead (question 1). Judge the direction. The generator's own output still has to earn the same read.</div>`}
<p><b>Why it reads badly:</b> today's lines swap in any speaker, with nothing at stake and nobody saying what's being asked. <b>Why it reads short:</b> a cutscene stops at 3 lines before any answer, and each person gets just one.</p>
${rows.map(r => `<h2>${esc(r.title)}</h2><p class="why">${esc(r.why)}</p><div class="grid">${r.left}${r.right}</div>`).join('\n')}
<details><summary>One more, the call that pulls him off the door</summary><div class="grid" style="margin-top:12px">${callLeft}${rightCol([{ s: call }], `About ${secs(call)} s, after Help. The shot cuts between them, and he walks off in the same cut.`)}</div></details>
<h2>My pick</h2><p>Listen to Inez sets the bar for learning something, and Pressure for turning someone. Every conversation should end like those two: what you know, what you can do now, and something you'll have to live with.</p>
<h2>Your calls</h2><table><tr><th>Question</th><th>My pick</th><th>What it means</th></tr>
<tr><td>1. Run the generator on these six?</td><td><b>Yes</b></td><td>13 model calls, up to 19 with retries, on your subscription: my estimate is 5 to 10 minutes. You then read its versions here before anything goes in the game.</td></tr>
<tr><td>2. When does the cutscene play?</td><td><b>Once, whole, when the act finishes</b></td><td>The answer is known by then, so it can't contradict the result, and the new action opens as you read about it. The cost is a cut you didn't time yourself, but the clock is stopped and you can skip. The other ways: half at the start and half at the end (two cuts), or all at the start (you'd wait out the act knowing the answer).</td></tr>
<tr><td>3. How often?</td><td><b>First time per person and act</b></td><td>Today it's first time per person, which is why Help and the call get no cutscene after a Listen. Repeats stay as bubbles. The whole scene is kept in the history.</td></tr>
<tr><td>4. How long?</td><td><b>Up to 9 lines, about 30 s</b></td><td>The front door by Help comes to about ${routeA} s of reading in a 7:30 mission, by Pressure about ${routeP} s, all with the clock stopped. If that's too much reading, 6 lines is about 18 s, and it loses the sideways talk first.</td></tr>
<tr><td>5. Names in speech?</td><td><b>Okafor and Zhou, by surname</b></td><td>The design says the guard and driver are named in play, but the game says "the guard". Their given names in the data are Folake and Mei, while the design calls both "he". Surnames avoid that clash; say if you'd rather recast the names.</td></tr>
</table>
<footer>Today's lines: text.json at seed ${SEED}, split as HostConvo.cs splits them at the default setting. Reading times: 0.3 s a word + 0.5 s a line, an estimate. Facts: sim/data/handover.people.json. Generator: tools/dialogue/conversations.ts.</footer>
</main></html>`;
await Bun.write(resolve(root, 'briefs/085/conversations.html'), html);
console.log(`briefs/085/conversations.html ${html.length} bytes; ${runArg ? `run ${runArg}` : 'scenes.json (not a pipeline run)'}; route A ${routeA} s, Pressure ${routeP} s`);
