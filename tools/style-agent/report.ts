import { corrected, defaults } from './loop';
import type { Brief, Conversation, Guide, Line, Result, Round } from './types';

/** Stress tests for the evaluator: three violation classes, each a real chain brief with a deliberate wrong push for the first draft. */
export const DEMOS = [
  { id: 'tone', title: 'Tone and voice', brief: 'inez/listen/idealist',
    steer: 'Make it a breezy modern romantic comedy: pet names, gushing compliments, jokes and lots of exclamation marks; everyone upbeat and chatty.' },
  { id: 'lore', title: 'Lore and vocabulary', brief: 'inez/listen/mercenary',
    steer: 'Make it a hardboiled 1950s picture: thick period slang (dame, swell, gee, copacetic, gumshoe), and give everyone names: the guard is Frank, the customer is Mrs Pell, and the lender is Mr Duggan.' },
  { id: 'length', title: 'Length and format', brief: 'inez/help/fearful',
    steer: 'Write exactly two lines: the Fixer sets out the whole offer in one paragraph of 60 to 90 words, and Inez answers in one paragraph of 60 to 90 words.' },
] as const;
export const PIPELINE = 'This Style Guide Agent runs straight after Fable generates a mission\'s conversations: Motive scores each one against the GDD\'s reveal, voice, world and form rules, and Fable revises it until it scores 10/10, before Keel\'s text checks and Latch\'s build.';
export const WHO: Record<string, string> = { waitress: 'Inez', guard: 'Guard', customer: 'Customer', keeper: 'Keeper', fixer: 'Fixer' };

const esc = (v: unknown) => String(v).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&#39;');
const tokens = (r: Result) => r.calls.reduce((n, c) => n + (c.tokens ?? 0), 0);
const shell = (title: string, body: string) => `<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${esc(title)}</title><style>
:root{--ink:#17302f;--cream:#f7f4ec;--sea:#d9ebe5;--chrome:#cbc6c3;--brass:#99784d;--dim:#46645e}
body{font:16px/1.55 system-ui,sans-serif;color:var(--ink);background:var(--cream);margin:0}main{max-width:1180px;margin:0 auto;padding:40px 32px 80px}
h1{font-size:34px;line-height:1.15;margin:0 0 8px}h2{margin-top:48px;border-bottom:1px solid var(--brass);padding-bottom:6px}h3{margin:0 0 8px}small,.dim{color:var(--dim)}
.grid{display:grid;grid-template-columns:1fr 1fr;gap:20px}.card{background:#fff;border:1px solid var(--chrome);border-radius:6px;padding:18px}
.score{display:inline-block;font-weight:700;padding:2px 10px;border-radius:12px;background:var(--ink);color:var(--cream)}.score.low{background:#a2402b}
ol.talk{list-style:none;padding:0;margin:8px 0}ol.talk li{margin:4px 0;padding:5px 10px;border-radius:12px;background:var(--sea);max-width:92%}ol.talk li.r{margin-left:auto;background:#eee6d6}
ol.talk b{font-size:12px;letter-spacing:.04em;text-transform:uppercase;color:var(--dim);margin-right:6px}ul.find li{margin-bottom:6px}code{font-size:13px;overflow-wrap:anywhere}
table{width:100%;border-collapse:collapse}td,th{text-align:left;vertical-align:top;padding:8px 10px;border-bottom:1px solid var(--chrome)}th{background:var(--sea)}
.pill{display:inline-block;font-size:12px;padding:1px 8px;border:1px solid var(--chrome);border-radius:10px;margin-right:4px}blockquote{margin:12px 0;border-left:3px solid var(--brass);padding-left:14px}
@media (max-width:820px){.grid{grid-template-columns:1fr}}</style><main>${body}</main></html>`;

export function talk(c: Conversation): string {
  const first = c.lines[0]?.speaker;
  return `<ol class="talk">${c.lines.map(l => `<li class="${l.speaker === first ? '' : 'r'}"><b>${esc(WHO[l.speaker] ?? l.speaker)}</b>${esc(l.line)}</li>`).join('')}</ol>`;
}
const score = (r: Round) => `<span class="score${r.evaluation.score < 10 ? ' low' : ''}">SCORE ${r.evaluation.score}/10</span>`;
const findings = (r: Round) => r.evaluation.findings.length + r.hardIssues.length === 0 ? '' : `<ul class="find">${r.evaluation.findings.map(f =>
  `<li><b>${esc(f.rule)}</b>${f.quote ? ` “${esc(f.quote)}”` : ''}: ${esc(f.reason)}<br><small>Fix: ${esc(f.fix)}</small></li>`).join('')}${r.hardIssues.map(h =>
  `<li><b>${esc(h.rule)}</b> (code check): ${esc(h.reason)}</li>`).join('')}</ul>`;
const briefLine = (b: Brief) => `<p class="dim"><span class="pill">${esc(b.verb)} · ${b.seconds} s</span><span class="pill">${b.lines[0]}–${b.lines[1]} lines</span>${b.temperament ? `<span class="pill">${esc(b.temperament)}</span>` : ''}${b.outcome ? `<span class="pill">${esc(b.outcome)}</span>` : ''}<span class="pill">scene ${b.scene}</span></p>`;

export function runHtml(r: Result): string {
  return shell(`The Suborned · ${r.brief.id}`, `<h1>${esc(r.brief.id)}</h1>${briefLine(r.brief)}
    <p><b>${esc(r.status)}</b> · ${esc(r.stopReason)} · ${esc(r.mode)}${r.authorization ? ` · ${esc(r.authorization.model)}` : ''} · ${r.calls.length} calls · ${tokens(r)} tokens</p>
    ${r.steer ? `<blockquote><b>Wrong push for the first draft only:</b> ${esc(r.steer)}</blockquote>` : ''}
    <p><b>Must reveal:</b> ${r.brief.reveals.map(esc).join(' · ')}<br><b>Must not reveal:</b> ${r.brief.withholds.map(esc).join(' · ')}<br><b>Sets up:</b> ${esc(r.brief.leadsTo)}</p>
    ${r.rounds.map((round, i) => `<div class="card"><h3>${i === 0 ? 'First draft' : `Revision ${i}`} ${score(round)}</h3>${talk(round.conversation)}
      <p><b>REASON:</b> ${esc(round.evaluation.reason)}</p>${findings(round)}</div>`).join('<br>')}
    <h2>Today's version in the game</h2>${talk({ lines: r.brief.current })}
    <p class="dim"><code>run.json.zst</code> (zstd-compressed JSON) holds every prompt, Codex transcript, score and revision. Play it with <code>bun run style:replay</code>; check it with <code>bun run style:verify</code>.</p>`);
}

/** What the runs show, counted from the storyline runs (no push): first-draft scores, revisions needed, what the evaluator flagged. */
export function learned(runs: Result[]) {
  const chain = runs.filter(r => r.steer === null), done = chain.filter(r => r.status === 'ACCEPTED');
  const count = <T extends string | number>(xs: T[]) => Object.fromEntries([...new Set(xs)].sort().map(x => [x, xs.filter(y => y === x).length]));
  return {
    runs: chain.length, accepted: done.length, firstTime: done.filter(r => r.rounds.length === 1).length,
    firstScores: count(chain.map(r => r.rounds[0]?.evaluation.score ?? 0)),
    revisions: count(done.map(r => r.rounds.length - 1)),
    findings: count(chain.flatMap(r => r.rounds.flatMap(x => x.evaluation.findings.map(f => f.rule)))),
    codeChecks: chain.reduce((n, r) => n + r.rounds.reduce((m, x) => m + x.hardIssues.length, 0), 0),
    dips: chain.reduce((n, r) => n + r.rounds.filter((x, i) => i > 0 && x.evaluation.score < r.rounds[i - 1]!.evaluation.score).length, 0),
  };
}
const list = (o: Record<string, number>, unit: (k: string) => string) => Object.entries(o).map(([k, n]) => `${n} × ${unit(k)}`).join(' · ');

export function submissionHtml(o: { guide: Guide; demos: { demo: (typeof DEMOS)[number]; run: Result; path: string }[];
  chain: { brief: Brief; run: Result | null; path: string | null }[]; today: Record<string, Line[]>;
  all: { run: Result; path: string }[] }): string {
  const { guide } = o, source = new Map(guide.sources.map(s => [s.id, s]));
  const accepted = o.chain.filter(c => c.run?.status === 'ACCEPTED').length, l = learned(o.all.map(x => x.run));
  const incomplete = o.all.filter(x => x.run.status !== 'ACCEPTED');
  return shell('The Suborned · Style Guide Agent', `
<p class="dim">The Suborned (GDD v${esc(guide.gdd)}) · style agent report</p>
<h1>Style Guide Agent: conversations that reveal</h1>
<p>The Suborned is real-time tactics in a 1950s-styled city that found atomic power centuries ago and never invented networks, so information moves through people and people are the weak point. In the Handover, you turn Inez, the café waitress, so she calls the door guard (her secret lover) off his post. The player learns how to turn her by watching and listening. The game's conversations were too short to teach that (4–6 lines; the Fixer's whole Help offer was one line). This agent writes the conversations that reveal what is going on and set up the next move, and enforces the game's style on them.</p>

<h2>1 · The style guide</h2>
<p>Built only from the approved GDD v${esc(guide.gdd)}, the Handover's voice sheet and text contract, and Sky's recorded decision (brief 054). Every source is fingerprinted (sha256) and archived with each run; <code>bun run style:guide</code> rebuilds it and fails if a source sentence moves.</p>
${guide.rules.map(r => `<div class="card"><h3>${esc(r.id)} · ${esc(r.kind)}</h3><p>${esc(r.instruction)}</p><small>From: ${r.sources.map(id => esc(source.get(id)?.locator ?? id)).join(' · ')}</small></div>`).join('<br>')}
<p class="dim">Code checks (no model): ${guide.bubble.maxWords} words and ${guide.bubble.maxChars} characters per bubble; line count fits the act (${guide.bubble.seconds} s per bubble); only the brief's speakers; no monologues; the mission's word filter (${guide.avoid.length} words).</p>

<h2>2 · The loop</h2>
<p>Each stage runs as one of the game's own crew cards (generated from GDD §14), in a tool-less structured call through the Codex CLI on <code>gpt-6.1-sol</code>:</p>
<table><tr><th>Stage</th><th>Crew member</th><th>Does</th></tr>
<tr><td>Generator</td><td>Fable, dialogue designer</td><td>Writes the conversation from its brief: the act and its seconds, the cast, what it must and must not reveal, what it sets up.</td></tr>
<tr><td>Evaluator</td><td>Motive, systems designer (GDD §14 Fable: “Motive reviews”)</td><td>Returns <b>SCORE</b> 1–10, a <b>REASON</b>, and findings: rule, exact quote, why, fix. Code checks run alongside.</td></tr>
<tr><td>Refiner</td><td>Fable</td><td>Gets the score, reason, findings and code checks automatically and rewrites the whole conversation. The new draft is scored again.</td></tr></table>
<p>It stops at 10/10 with no findings and clean code checks (accepted), or at its limits: ${defaults.maxRevisions} revisions, ${defaults.maxCalls} calls, ${defaults.maxTokens.toLocaleString('en-GB')} tokens, ${defaults.maxMs / 60000} minutes (incomplete). Nobody steps in. Every prompt, transcript, score and revision is kept; <code>bun run style:replay</code> plays a run step by step and <code>bun run style:verify</code> rebuilds it from its archived sources, both without calling a model.</p>

<h2>3 · Before and after</h2>
<p>Three real conversations from Inez's storyline, each with a deliberate wrong push on the first draft only; the evaluator and refiner never see the push.</p>
${o.demos.map(({ demo, run, path }) => {
  const first = run.rounds[0]!, last = run.rounds.at(-1)!;
  return `<h3>${esc(demo.title)} · ${esc(run.brief.id)}</h3>${briefLine(run.brief)}<blockquote>Push: ${esc(demo.steer)}</blockquote>
  <div class="grid"><div class="card"><h3>Before ${score(first)}</h3>${talk(first.conversation)}<p><b>REASON:</b> ${esc(first.evaluation.reason)}</p>${findings(first)}</div>
  <div class="card"><h3>After ${score(last)}</h3>${talk(last.conversation)}<p><b>REASON:</b> ${esc(last.evaluation.reason)}</p></div></div>
  <p class="dim">Scores ${run.rounds.map(x => x.evaluation.score).join(' → ')} · ${run.calls.length} calls · ${tokens(run).toLocaleString('en-GB')} tokens · <a href="${esc(path)}/index.html">full run</a></p>`;
}).join('')}
${o.demos.length < DEMOS.length ? `<p><b>${DEMOS.length - o.demos.length} demonstration(s) not yet recorded.</b></p>` : ''}

<h2>4 · Pipeline connection</h2>
<blockquote>${esc(PIPELINE)}</blockquote>

<h2>What we found</h2>
<ul>
<li><b>First drafts are close, rarely ready.</b> ${l.firstTime} of ${l.runs} storyline runs passed first time; first-draft scores ${list(l.firstScores, k => `${k}/10`)}. Accepted runs needed ${list(l.revisions, k => `${k} revision${k === '1' ? '' : 's'}`)}.</li>
<li><b>The problem is exposition, not lore.</b> Findings by rule: ${list(l.findings, k => k)}; the code checks fired ${l.codeChecks} times. Characters announce secrets and debts for the player's benefit (“The Office mustn't know about us”); Fable's card guards the world well enough that it resisted even deliberate pushes towards apps and networks.</li>
<li><b>Revealing without saying is the hard part.</b> The player must be sure of the affair and the debt; a customer a table away must not. That tension caused the ${l.dips} score dips and the runs that needed a second go.</li>
<li><b>The writer invents; the evaluator catches it.</b> Side deals, a smuggling customer, split payments, and a customer offering to clear her debt (the cell's lever) were all flagged under REVEAL.</li>
<li><b>Temperament is the weakest reveal, and that points at the GDD.</b> The runs that stalled were mercenary and fearful Inez; loyal only settled once the GDD's definition (loyalty to an employer or the civic order) was in the brief. §04 says what each temperament responds to, not how it sounds: one line per temperament would help every future conversation.</li>
<li><b>The judge is noisy.</b> Motive sometimes marked down a fix it asked for, which is why the loop has limits and records a run as incomplete instead of looping.</li>
</ul>
${incomplete.length ? `<p>Kept on record, not used: ${incomplete.length} run(s) that stopped at their limits before a 10/10 (the conversation was then rerun and accepted): ${incomplete.map(x => `<a href="${esc(x.path)}/index.html">${esc(x.run.brief.id)}${x.run.steer ? ' (demo)' : ''}</a> ${x.run.rounds.map(r => r.evaluation.score).join('→')}`).join(' · ')}.</p>` : ''}

<h2>The output: Inez's conversations</h2>
<p>${accepted} of ${o.chain.length} conversations accepted at 10/10. These are proposals for the game (brief 054): the mission text and its contract are unchanged until after submission. Each is written without any push.</p>
${o.chain.map(({ brief, run, path }) => `<div class="card"><h3>${esc(brief.id)}${run ? ` ${score(run.rounds.at(-1)!)}` : ''}</h3>${briefLine(brief)}<p class="dim">Sets up: ${esc(brief.leadsTo)}</p>
  ${run && run.status === 'ACCEPTED' ? `${talk(run.rounds.at(-1)!.conversation)}<p class="dim">${run.rounds.length === 1 ? 'Accepted first time' : `Scores ${run.rounds.map(x => x.evaluation.score).join(' → ')}`} · <a href="${esc(path)}/index.html">run</a></p>`
    : `<p><b>${run ? esc(`${run.status}: ${run.stopReason}`) : 'Not yet run'}</b></p>`}
  <details><summary>Today's version in the game</summary>${talk({ lines: o.today[brief.id] ?? brief.current })}</details></div>`).join('<br>')}

<h2>Limits</h2>
<ul><li>The conversations are proposals; the game's text, its 3–6 line scene limit and the build change after review.</li>
<li>A 10/10 means Motive found nothing to fix, not that the writing is perfect.</li>
<li>Both roles ran on <code>gpt-6.1-sol</code> through Codex. The GDD names that model for Fable but Opus 5.5 for Motive; one provider kept the submission simple.</li></ul>`);
}
