// The page: one self-contained html file (inline css, js and data; no network). The structure of the five views (the overview, the map's layers and one dot per person,
// the tie graph's nodes and edges, the roster's rows, the compare) is written here as html, so the page reads and can be counted without running a script; client.js adds
// what moves (tabs, filters, sorting, the day scrubber, path finding, the character sheet) and reads the same data from the `data` element.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { VPerson, VRole, View } from './data';
import { usdText } from './fit';
import { GRAPH_H, GRAPH_W } from './layout';

export const esc = (x: unknown) => String(x).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!);
const hm = (m: number) => `${String(Math.floor(m / 60) % 24).padStart(2, '0')}:${String(Math.round(m) % 60).padStart(2, '0')}`;
const cap = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);
export const UNIT_PALETTE = ['#12908e', '#35508f', '#67a74b', '#7b8794', '#b8895a', '#4aa3c7', '#9aa64b'];
export const TEM_COLOUR: Record<string, string> = { idealist: '#2f80c8', mercenary: '#8c6a43', fearful: '#4fb3a0', loyal: '#24324d' };
export const KIND_COLOUR: Record<string, string> = { workmate: '#17807e', family: '#25418c', regular: '#4b9a3f', debtor: '#8d6e4c', rival: '#2b2b2b' };
const KIND_DASH: Record<string, string> = { workmate: '', family: '', regular: '6 3', debtor: '2 3 7 3', rival: '1.5 3.5' };
const KIND_W: Record<string, number> = { workmate: 1.2, family: 1.8, regular: 1.2, debtor: 1.2, rival: 1.6 };
const TEM_SHAPE = (t: string) => ({ idealist: 'circle', mercenary: 'rect', fearful: 'tri', loyal: 'dia' }[t] ?? 'circle');

const asset = (f: string) => readFileSync(resolve(import.meta.dir, f), 'utf8');
/** JSON safe inside a script element: no `</script`, no `<!--`, no U+2028/9. */
const scriptJson = (x: unknown) => JSON.stringify(x).replace(/</g, '\\u003c').replace(new RegExp('\\u2028', 'g'), '\\u2028').replace(new RegExp('\\u2029', 'g'), '\\u2029');

export function render(v: View): string {
  const groups = ['cafe', 'records-office', ...v.units.map(u => u.id).filter(u => u !== 'cafe' && u !== 'records-office'), 'resident'];
  const uCol = (u: string) => UNIT_PALETTE[Math.max(0, groups.indexOf(u)) % UNIT_PALETTE.length]!;
  const uName = (u: string) => (u === 'resident' ? 'Residents' : v.units.find(x => x.id === u)?.name ?? u);
  const P = new Map(v.persons.map(p => [p.id, p])), nm = (id: string) => P.get(id)?.name ?? id;
  const link = (id: string) => `<button class="lnk" data-open="${esc(id)}">${esc(nm(id))}</button>`;
  const pal = `${groups.map(u => `.u-${u}{--c:${uCol(u)}}`).join('')}${Object.entries(TEM_COLOUR).map(([t, c]) => `.t-${t}{--tc:${c}}`).join('')}${Object.entries(KIND_COLOUR).map(([k, c]) => `.kind.${k}{background:${c}}`).join('')}`;
  const roleName = new Map(v.persons.filter(p => p.role).map(p => [p.id, p.role!]));
  const css = asset('style.css'), js = asset('client.js');

  // ---------- overview ----------
  const run = v.run;
  const chk = v.checks.map(c => `<span class="chip ${c.ok ? 'ok' : 'fail'}" title="${esc(`${c.id}: ${c.rule}\n${c.value} (limit ${c.limit})${c.detail ? `\n${c.detail}` : ''}${c.recorded !== undefined && c.recorded !== c.ok ? `\nrecorded in the run: ${c.recorded ? 'ok' : 'fail'}` : ''}`)}">${c.ok ? '✓' : '✗'} ${esc(c.id)}${c.recorded !== undefined && c.recorded !== c.ok ? ' ≠ run' : ''}</span>`).join(' ');
  const runCard = run ? `<div class="card"><div class="row spread"><h2>Run ${esc(run.id)} <span class="pill ${run.outcome === 'ACCEPTED' ? 'ok' : 'fail'}">${esc(run.outcome)}</span></h2><span class="muted">${esc(run.date.slice(0, 10))}</span></div>
<dl class="kv"><dt>Cost</dt><dd>$${run.cost.usd.toFixed(3)} in ${run.cost.calls} calls <small>(ceiling $${run.maxUsd}; ${esc(run.approvedBy)})</small></dd>
<dt>Rounds</dt><dd>${run.rounds.length}; the accepted cast is round ${run.final.round}, <span class="mono">${esc(run.final.version)}</span></dd>
${run.continues ? `<dt>Continues</dt><dd>${esc(run.continues.run)} (${esc(run.continues.outcome)}, round ${run.continues.round})</dd>` : ''}
<dt>Model</dt><dd>${esc(run.model)}</dd></dl>
<h4>Rounds</h4><div class="rounds">${run.rounds.map(r => `<div class="round ${r.keelVerdict === 'pass' ? 'pass' : 'fail'}"><b>Round ${r.round}</b> <span class="pill ${r.keelVerdict === 'pass' ? 'ok' : 'fail'}">${esc(r.keelVerdict)}</span><br><small>${r.persons} people · checker ${r.checkerOk ? 'ok' : 'fail'} · ${r.faults} fault${r.faults === 1 ? '' : 's'}</small>${r.faultTexts.map(f => `<div class="muted" style="font-size:12px;margin-top:3px">${esc(f.owner)} · ${esc(f.rule)}: ${esc(f.fault.slice(0, 150))}${f.fault.length > 150 ? '…' : ''}</div>`).join('')}</div>`).join('')}</div>
<h4>Keel's verdict <span class="pill ${run.keel.verdict === 'pass' ? 'ok' : 'fail'}">${esc(run.keel.verdict)}</span></h4><p style="margin:0 0 6px">${esc(run.keel.summary)}</p>
${run.keel.questions.length ? `<h4>Open questions (not faults)</h4>${run.keel.questions.map((q, i) => `<div class="q"><b>${i + 1}.</b> ${esc(q)}</div>`).join('')}` : ''}
${v.drift.length ? `<details><summary>Inputs the run was pinned to against the ones now (${v.drift.filter(d => !d.same).length} moved)</summary><table><tr><th>input</th><th>pinned</th><th>now</th><th></th></tr>${v.drift.map(d => `<tr><td>${esc(d.input)}</td><td class="mono">${esc(d.pinned.slice(0, 12))}</td><td class="mono">${esc(d.current.slice(0, 12))}</td><td>${d.same ? '<span class="chip ok">same</span>' : '<span class="chip warn">moved</span>'}</td></tr>`).join('')}</table></details>` : ''}</div>`
    : `<div class="card"><h2>${esc(v.source.kind === 'promoted' ? 'The promoted cast' : 'A cast file')}</h2><dl class="kv"><dt>Source</dt><dd>${esc(v.source.label)}</dd>${v.source.promotedFrom ? `<dt>From run</dt><dd>${esc(v.source.promotedFrom)}</dd>` : ''}<dt>Cast sha256</dt><dd class="mono">${esc(v.source.castSha.slice(0, 16))}</dd></dl></div>`;
  const checkCard = `<div class="card"><h2>Checks <small>(${v.checks.filter(c => c.ok).length}/${v.checks.length} ok, on the inputs now)</small></h2><div class="row" style="gap:5px">${chk}</div>
<details><summary>What each one holds</summary><table>${v.checks.map(c => `<tr><td><span class="chip ${c.ok ? 'ok' : 'fail'}">${esc(c.id)}</span></td><td>${esc(c.rule)}</td><td class="mono">${esc(c.value)}</td></tr>`).join('')}</table></details>
<h4>The cast</h4><dl class="kv"><dt>People</dt><dd><b>${v.persons.length}</b> simulated in ${v.units.length} units${v.persons.filter(p => p.unit === 'resident').length ? ` and ${v.persons.filter(p => p.unit === 'resident').length} residents` : ''}</dd><dt>Plan</dt><dd>${esc(v.plan.id)} ${esc(v.plan.version)} · scope ${esc(v.scope)}</dd><dt>Cast</dt><dd class="mono">${esc(v.source.castSha.slice(0, 16))}</dd><dt>Source</dt><dd>${esc(v.source.label)}</dd>${v.against ? `<dt>Against</dt><dd>${esc(v.against.label)}</dd>` : ''}</dl></div>`;
  const level = { fail: '✗', warn: '!', info: 'i' };
  const notices = v.notices.map(n => `<div class="notice ${n.level}"><div class="ic">${level[n.level]}</div><div><h3>${esc(n.title)}</h3><p>${esc(n.detail)}</p>${n.people?.length ? n.people.slice(0, 12).map(id => `<span class="who">${link(id)}</span>`).join('') : ''}${n.src ? `<p><small>${esc(n.src)}</small></p>` : ''}</div></div>`).join('');

  const roleCard = (r: VRole) => {
    const p = r.pinned ? P.get(r.pinned) : undefined, fit = p ? v.fits[p.fit]! : [];
    const fr = r.approach ? fit.find(x => x.a === r.approach) : undefined;
    const T = p ? v.rules.temperaments[p.tem] : undefined;
    const ok = (b: boolean) => (b ? '<span class="chip ok">✓</span>' : '<span class="chip fail">✗</span>');
    return `<div class="card role ${p ? '' : ''}" data-role="${esc(r.id)}"><div class="row spread"><h3>${esc(cap(r.id))} ${ok(r.ok)}</h3><small>${esc(r.employer)} · ${esc(r.position)}</small></div>
${p ? `<div class="nm"><button class="lnk" data-open="${esc(p.id)}">${esc(p.name)}</button> <small class="mono">${esc(p.id)}</small></div>` : '<div class="nm">nobody cast</div>'}
<div class="why">${esc(r.what)}</div>
${p ? `<dl class="need"><dt>Need</dt><dd><b>${esc(p.need)}</b> ${r.need ? (r.need === p.need ? '<small>· the role needs it</small>' : `<span class="chip fail">role needs ${esc(r.need)}</span>`) : ''}</dd>
<dt>Temperament</dt><dd><b>${esc(T?.name ?? p.tem)}</b> <small>· responds to ${esc(T?.respondsTo.join('/') ?? '')}${T && T.refuses.length ? `, refuses ${esc(T.refuses.join('/'))}` : ''}</small></dd>
${fr ? `<dt>Plan</dt><dd>${esc(cap(fr.a))}: <span class="chip ${fr.fit === 'suits' ? 'suits' : fr.fit === 'worse' ? 'worse' : 'refused'}">${fr.fit === 'suits' ? 'suits them' : fr.fit === 'worse' ? 'one step worse' : 'refused'}</span> <small>${esc(fr.a === 'help' ? `${fr.orders} Help${fr.orders === 1 ? '' : 's'}, ${usdText(fr.total.usd)}` : `${usdText(fr.total.usd)} · +${fr.total.trail} T`)}</small></dd>` : ''}
<dt>Works</dt><dd><div class="appr">${fit.map(x => `<span class="chip ${x.works ? (x.fit === 'suits' ? 'suits' : 'worse') : 'refused'}" title="${esc(x.say)}">${esc(cap(x.a))}${x.works ? (x.fit === 'suits' ? '' : ' (dearer)') : ''}</span>`).join('')}</div></dd>
<dt>Window</dt><dd>${esc(r.window.missionFrom)}–${esc(r.window.missionTo)} <small>mission · ${esc(hm(r.window.dayFrom))}–${esc(hm(r.window.dayTo))}${r.window.place ? ` at ${esc(r.window.place)}` : ''}${r.leavesTo ? ` · leaves for ${esc(r.leavesTo.at)} by ${esc(r.leavesTo.by)}` : ''}${r.alone ? ' · alone' : ''}${r.waitsFor ? ` · waits for the ${esc(r.waitsFor)}` : ''}</small></dd>
<dt>Ties</dt><dd>${r.tied.length ? r.tied.map(t => `<div>${t.ok ? '✓' : '✗'} ${esc(t.text)}</div>`).join('') : '<small>none asked of this role</small>'}<small>${p.ties.length} held in all${p.knownBy.length ? ` · need known to ${p.knownBy.map(k => esc(nm(k))).join(', ')}` : ''}</small></dd></dl>` : ''}
<div class="why"><small>${esc(r.why)}</small></div>
${r.reqs.map(q => `<div class="why"><span class="chip ${q.ok ? 'ok' : 'fail'}">${q.ok ? 'met' : 'NOT MET'}</span> <small>${esc(q.need)} · ${esc(q.detail)}</small></div>`).join('')}
<details><summary>Candidates (${r.candidates.length})</summary>${r.candidates.map(id => `<span class="who">${link(id)}</span>`).join('') || '<small>none</small>'}</details></div>`;
  };

  const bar = (dim: string, xs: { k: string; n: number }[]) => { const max = Math.max(1, ...xs.map(x => x.n)); return `<div class="bars">${xs.map(x => `<span class="lab" data-filter="${dim}:${esc(x.k)}" title="filter the roster">${esc(dim === 'unit' ? uName(x.k) : x.k)}</span><span class="bar"><b style="width:${(100 * x.n / max).toFixed(1)}%${dim === 'temperament' ? `;background:${TEM_COLOUR[x.k] ?? ''}` : dim === 'unit' ? `;background:${uCol(x.k)}` : ''}"></b></span><span class="n">${x.n}</span>`).join('')}</div>`; };
  const countsCard = (title: string, dim: string, xs: { k: string; n: number }[]) => `<div class="card flat"><h3 style="margin-bottom:6px">${title}</h3>${bar(dim, xs)}</div>`;

  const overview = `<section id="tab-overview" class="tab" data-tab="overview">
<div class="row spread" style="margin-bottom:6px"><h1>${esc(v.title)}</h1></div>
<p class="lede">${v.persons.length} people made from the cast tables by the same expansion as <span class="mono">bun run people</span>, on the inputs in the repository now. Click a name anywhere for the sheet; the numbers on a sheet are the person's truth, the cell starts knowing none of it.</p>
<div class="grid g2">${runCard}${checkCard}</div>
<div class="sect"><h2>What the viewer noticed</h2><small>read from the people and the rules, not from the generator's own notes</small></div>${notices}
<div class="sect"><h2>The Handover's four roles</h2><small>cast from the city's people by the same casting as M-2; pinned by role, never by name</small></div>
<div class="grid g4">${v.roles.map(roleCard).join('')}</div>
<div class="sect"><h2>Who is here</h2><small>click a label to filter the roster</small></div>
<div class="grid g3">${countsCard('By unit', 'unit', v.counts.unit)}${countsCard('By temperament', 'temperament', v.counts.temperament)}${countsCard('By need', 'need', v.counts.need)}${countsCard('By position', 'position', v.counts.position)}${countsCard('By occupation', 'occupation', v.counts.occupation)}${countsCard('By shift', 'shift', v.counts.shift)}</div>
<div class="sect"><h2>Units</h2></div>
<div class="card flat"><table><tr><th>Unit</th><th>Open</th><th>Staff a shift</th><th>Shifts</th><th>Watch shifts</th><th>People</th></tr>${v.units.map(u => `<tr><td><i class="sw" style="background:${uCol(u.id)}"></i> ${esc(u.name)} <small class="mono">${esc(u.id)}</small></td><td>${u.hours[0]}:00-${u.hours[1]}:00</td><td>${u.staff}</td><td>${u.shifts.map(s => `${hm(s[0])}-${hm(s[1])}`).join(', ')}</td><td>${u.watch.map(s => `${hm(s[0])}-${hm(s[1])}`).join(', ') || '·'}</td><td>${u.people}</td></tr>`).join('')}</table></div>
<div class="sect"><h2>Cast tables</h2><small>what the model proposed; the people are drawn from these</small></div>
<details><summary>${v.rows.length} rows</summary><div class="grid g2">${v.rows.map(r => `<div class="card flat"><h3>${esc(r.key)} <small>· ${r.people} people</small></h3><p style="margin:4px 0">${esc(r.why)}</p><small>positions: ${esc(r.positions)}</small>${wbar(r.temperaments, 'temperament')}${wbar(r.needs, 'need')}</div>`).join('')}</div></details>
${v.castNotes.length ? `<details><summary>Pulse's notes with the cast (${v.castNotes.length})</summary><ul class="plain">${v.castNotes.map(n => `<li>${esc(n)}</li>`).join('')}</ul></details>` : ''}
${v.genNotes.length ? `<details><summary>The expander's notes (${v.genNotes.length})</summary><ul class="plain">${v.genNotes.map(n => `<li>${esc(n)}</li>`).join('')}</ul></details>` : '<p class="muted">The expander raised no notes (no stop dropped for want of a seat).</p>'}
<p class="foot">Keys: <kbd>/</kbd> search · <kbd>1</kbd>–<kbd>5</kbd> views · <kbd>Esc</kbd> close · map: <kbd>Space</kbd> play, <kbd>←</kbd><kbd>→</kbd> scrub · roster: <kbd>↑</kbd><kbd>↓</kbd> + <kbd>Enter</kbd>. Same inputs, same bytes: <span class="mono">${esc(v.inputs.cast.slice(0, 10))}/${esc(v.inputs.mission.slice(0, 10))}/${esc(v.inputs.rules.slice(0, 10))}</span>.</p>
</section>`;
  function wbar(xs: { k: string; w: number }[], dim: string) {
    const tot = xs.reduce((n, x) => n + x.w, 0) || 1;
    return `<div style="display:flex;height:9px;border-radius:5px;overflow:hidden;margin-top:6px" title="${esc(dim)} weights">${xs.map(x => `<span title="${esc(x.k)} ${x.w}" style="width:${(100 * x.w / tot).toFixed(1)}%;background:${dim === 'temperament' ? TEM_COLOUR[x.k] ?? '#999' : '#9ab8c4'};border-right:1px solid #fff"></span>`).join('')}</div><small>${xs.map(x => `${esc(x.k)} ${Math.round(100 * x.w / tot)}%`).join(' · ')}</small>`;
  }

  // ---------- map ----------
  const m = v.map, [bx0, by0, bx1, by1] = m.bounds, pts = (ps: number[][]) => ps.map(p => `${p[0]},${p[1]}`).join(' ');
  const layerSvg = (layer: string, cls = (k: string) => k) => m.polys.filter(p => p.layer === layer).map(p => `<polygon class="${esc(cls(p.kind))}" points="${pts(p.pts)}"><title>${esc(p.name)}</title></polygon>`).join('');
  const mapSvg = `<svg id="map" viewBox="${bx0} ${by0} ${(bx1 - bx0).toFixed(2)} ${(by1 - by0).toFixed(2)}" data-vb="${bx0} ${by0} ${(bx1 - bx0).toFixed(2)} ${(by1 - by0).toFixed(2)}" role="img" aria-label="The civic block with every person at the chosen time">
<g id="L-ground">${layerSvg('ground', () => 'ground')}</g>
<g id="L-bays">${layerSvg('bays', () => 'bay')}</g>
<g id="L-crossing">${layerSvg('crossing', k => (k === 'landing' ? 'landing' : 'crossing'))}</g>
<g id="L-keepclear" hidden>${layerSvg('keepclear', () => 'zone')}${m.circles.filter(c => c.layer === 'keepclear').map(c => `<circle class="zone" cx="${c.c[0]}" cy="${c.c[1]}" r="${c.r}"${c.dashed ? ' fill="#f2ecdc" fill-opacity=".9"' : ''}><title>${esc(c.name)}</title></circle>`).join('')}</g>
<g id="L-buildings">${layerSvg('buildings', k => (k === 'tower' ? 'tower' : 'building'))}${m.labels.map(l => `<text class="lbl" x="${l.at[0]}" y="${l.at[1]}">${esc(l.text)}</text>`).join('')}</g>
<g id="L-structures">${m.polys.filter(p => p.layer === 'structures').map(p => `<polygon class="structure ${esc(p.kind)}" points="${pts(p.pts)}"><title>${esc(p.name)}</title></polygon>`).join('')}</g>
<g id="L-objects">${m.polys.filter(p => p.layer === 'objects').map(p => `<polygon class="object ${esc(p.kind)}" points="${pts(p.pts)}"><title>${esc(p.name)} (${esc(p.kind)})</title></polygon>`).join('')}</g>
<g id="L-seats">${layerSvg('tables', () => 'table')}${m.chairs.map(c => `<circle class="chair" cx="${c.at[0]}" cy="${c.at[1]}" r=".32"><title>${esc(c.id)}</title></circle>`).join('')}${m.stands.map(s => `<circle class="stand" cx="${s.at[0]}" cy="${s.at[1]}" r=".4"><title>${esc(s.id)}</title></circle>`).join('')}</g>
<g id="L-cones" hidden>${m.cones.map(c => `<polygon class="cone" points="${pts([c.apex, c.a, c.b])}"><title>${esc(c.name)}</title></polygon>`).join('')}</g>
<g id="L-doors">${m.marks.filter(k => k.kind === 'door').map(k => `<circle class="mk door" cx="${k.at[0]}" cy="${k.at[1]}" r=".55"><title>${esc(k.name)}</title></circle>`).join('')}</g>
<g id="L-points" hidden>${m.marks.filter(k => k.kind === 'dest' || k.kind === 'point' || k.kind === 'camera').map(k => `<g><rect class="mk ${k.kind}" x="${k.at[0] - .45}" y="${k.at[1] - .45}" width=".9" height=".9" transform="rotate(45 ${k.at[0]} ${k.at[1]})"><title>${esc(k.name)}${k.src ? ` (${esc(k.src)})` : ''}</title></rect><text class="mktxt" x="${k.at[0] + .8}" y="${k.at[1] + .5}">${esc(k.name)}</text></g>`).join('')}</g>
<g id="L-cell">${m.marks.filter(k => k.kind === 'cell').map(k => `<g><rect class="mk cell" x="${k.at[0] - .6}" y="${k.at[1] - .6}" width="1.2" height="1.2"><title>The cell: ${esc(k.name)} (mission start)</title></rect><text class="mktxt" x="${k.at[0]}" y="${k.at[1] + .45}" text-anchor="middle" style="font-weight:700">${esc(k.name.charAt(0))}</text></g>`).join('')}</g>
<g id="L-labels">${''}</g>
<polyline id="selpath" class="selpath" points=""/>
<g id="L-dots">${v.persons.map(p => `<circle class="dot u-${esc(p.unit)} t-${esc(p.tem)}${p.role ? ' role' : ''}${p.diff?.status === 'added' ? ' chg' : ''}" data-p="${esc(p.id)}" r="1.05" cx="0" cy="0" hidden></circle>`).join('')}${v.persons.filter(p => p.role).map(p => `<text class="rolelbl" data-lbl="${esc(p.id)}" text-anchor="middle" hidden>${esc(cap(p.role!))}</text>`).join('')}</g>
</svg>`;
  const unitLegend = groups.map(u => `<span><i class="sw" style="background:${uCol(u)}"></i> ${esc(uName(u))} <small>${v.persons.filter(p => p.unit === u).length}</small></span>`).join('');
  const temLegend = Object.keys(v.rules.temperaments).map(t => `<span><i class="sw" style="background:${TEM_COLOUR[t] ?? '#999'}"></i> ${esc(v.rules.temperaments[t]!.name)}</span>`).join('');
  const mission = `${v.mission.startsAt}`;
  const mapTab = `<section id="tab-map" class="tab" data-tab="map" hidden>
<div class="maprow"><div class="mapwrap">${mapSvg}</div>
<div class="mapctl">
<div class="card"><div class="clock" id="clk">00:00:00</div><div class="mclock" id="mclk">mission</div>
<input type="range" id="scrub" min="0" max="86400" step="1" value="0" aria-label="time of day">
<div class="btns" style="margin-top:6px"><button class="btn primary" id="play" title="Space">▶ Play</button><select id="speed" aria-label="speed"><option value="1">1× (mission pace)</option><option value="10">10×</option><option value="60" selected>60× (1 min/s)</option><option value="300">300×</option><option value="900">900×</option></select></div>
<div class="btns" style="margin-top:6px"><button class="btn" data-jump="${esc(mission)}">Mission start ${esc(mission)}</button><button class="btn" data-jump="17:30:00">Chime 17:30</button><button class="btn" data-jump="17:00:00">17:00</button><button class="btn" data-jump="08:00:00">08:00</button><button class="btn" data-jump="01:30:00">01:30</button></div></div>
<div class="card"><h3 style="margin-bottom:6px">Right now</h3><div class="live" id="live"></div></div>
<div class="card"><h3 style="margin-bottom:6px">Colour</h3><div class="row"><label><input type="radio" name="colour" value="unit" checked> unit</label><label><input type="radio" name="colour" value="tem"> temperament</label></div>
<div class="legend" id="legend-unit" style="margin-top:6px">${unitLegend}</div><div class="legend" id="legend-tem" style="margin-top:6px" hidden>${temLegend}</div>
${v.compare ? `<label style="display:block;margin-top:6px"><input type="checkbox" id="mark-chg" checked> ring people added against ${esc(v.against!.label.split(' (')[0]!)}</label>` : ''}</div>
<div class="card"><h3 style="margin-bottom:6px">Layers</h3><div class="legend">${[['buildings', 'buildings', true], ['structures', 'structures', true], ['objects', 'planters, fountain', true], ['seats', 'café tables, chairs and standing points', true], ['bays', 'kerb bays', true], ['crossing', 'crossing and landings', true], ['doors', 'doors', true], ['points', 'the mission\'s points', false], ['cones', 'camera cones', false], ['cell', 'the cell\'s start line', true], ['keepclear', 'keep-clear zones', false]].map(([id, lab, on]) => `<label><input type="checkbox" data-layer="${id}"${on ? ' checked' : ''}> ${esc(lab as string)}</label>`).join('')}<label><input type="checkbox" id="names"> names on every dot</label></div></div>
<p class="muted" style="margin:0;font-size:12px">Plan north is to the left; the block's long axis runs across the page. Walkers move in straight lines between stops (the walk graph gives distances, not paths). People inside a building (a clerk at the desk) are counted, not drawn. Drag to pan, wheel to zoom, double-click to reset.</p>
</div></div></section>`;

  // ---------- graph ----------
  const nodeById = new Map(v.graph.nodes.map(n => [n.id, n]));
  const edgeSvg = v.graph.edges.map((e, i) => { const a = nodeById.get(e.a)!, b = nodeById.get(e.b)!; return `<line class="e ${esc(e.kind)}" data-i="${i}" data-a="${esc(e.a)}" data-b="${esc(e.b)}" data-k="${esc(e.kind)}" x1="${a.x}" y1="${a.y}" x2="${b.x}" y2="${b.y}"${e.dir ? ' marker-end="url(#arr)"' : ''}><title>${esc(nm(e.a))} ${e.dir ? '→' : '—'} ${esc(nm(e.b))}: ${esc(e.kind)}${e.at ? ` (${esc(e.at)})` : ''}</title></line>`; }).join('');
  const nodeSvg = v.persons.map(p => { const n = nodeById.get(p.id)!; return `<g class="n u-${esc(p.unit)} t-${esc(p.tem)}${p.role ? ' role' : ''}${p.diff?.status === 'added' ? ' chgd' : ''}" data-p="${esc(p.id)}" data-u="${esc(p.unit)}" transform="translate(${n.x} ${n.y})"><title>${esc(p.name)} · ${esc(p.position)} · ${n.deg} ties</title><circle class="chgc" r="${n.r + 4}"/>${p.role ? `<circle class="ringc" r="${n.r + 3}"/>` : ''}<circle class="body" r="${n.r}"/><text y="${-n.r - 4}" data-short="${esc(p.name.split(' ').slice(-1)[0]!)}" ${p.role ? '' : 'hidden'}>${p.role ? esc(`${cap(p.role)}: ${p.name.split(' ')[0]}`) : esc(p.name.split(' ').slice(-1)[0]!)}</text></g>`; }).join('');
  const kinds = Object.keys(v.rules.ties);
  const graphTab = `<section id="tab-graph" class="tab" data-tab="graph" hidden>
<div class="graphrow"><div><svg id="graph" viewBox="0 0 ${GRAPH_W} ${GRAPH_H}" data-vb="0 0 ${GRAPH_W} ${GRAPH_H}" role="img" aria-label="The tie graph: people joined by ties">
<defs><marker id="arr" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M0,0 L10,5 L0,10 z" fill="#667580"/></marker></defs>
<g id="edges">${edgeSvg}</g><g id="nodes">${nodeSvg}</g></svg></div>
<div class="mapctl">
<div class="card"><h3 style="margin-bottom:6px">Units</h3><div class="chipset" id="g-units">${groups.map(u => `<span class="chip btn" data-gu="${esc(u)}" aria-pressed="true" tabindex="0"><i style="background:${uCol(u)}"></i>${esc(uName(u))}</span>`).join('')}</div>
<h3 style="margin:10px 0 6px">Tie kinds</h3><div class="legend" id="g-kinds">${kinds.map(k => `<label><input type="checkbox" data-gk="${esc(k)}" checked><span class="keysw" style="border-top-color:${KIND_COLOUR[k] ?? '#999'};border-top-style:${k === 'regular' ? 'dashed' : k === 'debtor' ? 'dashed' : k === 'rival' ? 'dotted' : 'solid'};border-top-width:${KIND_W[k] ? Math.round(KIND_W[k]! * 2) : 2}px"></span>${esc(k)} <small>${v.graph.kinds[k] ?? 0}${v.rules.ties[k]!.direction !== 'both' ? ' · one way →' : ''}${v.rules.ties[k]!.introduces ? ' · introduces' : ' · leverage'}</small></label>`).join('')}</div>
<label style="display:block;margin-top:8px"><input type="checkbox" id="g-labels"> a name on every node</label></div>
<div class="card"><h3 style="margin-bottom:6px">Path between two people</h3><div style="display:grid;gap:6px"><input list="people-list" id="pa" placeholder="from (name or id)" class="mono" style="padding:6px;border:1px solid var(--line);border-radius:8px"><input list="people-list" id="pb" placeholder="to (name or id)" class="mono" style="padding:6px;border:1px solid var(--line);border-radius:8px"><div class="btns"><button class="btn" id="pswap">⇄ swap</button><button class="btn" id="pclear">clear</button></div><label><input type="checkbox" id="pany"> any tie, either way</label></div><div id="pathout" class="path" hidden></div>
<p class="muted" style="font-size:12px;margin:8px 0 0">By default a path follows ties that introduce (${esc(kinds.filter(k => v.rules.ties[k]!.introduces).join(', '))}), each from the person who holds it. <kbd>Shift</kbd>+click a node to set the end points.</p></div>
<div class="card"><h3 style="margin-bottom:6px">The graph</h3><dl class="kv"><dt>People</dt><dd>${v.persons.length}</dd><dt>Ties</dt><dd>${v.graph.edges.length}</dd><dt>Groups</dt><dd>${v.graph.components.length} <small>(${v.graph.components.map(c => c.length).join(' + ')})</small></dd><dt>Busiest</dt><dd>${[...v.graph.nodes].sort((a, b) => b.deg - a.deg)[0] ? `${link([...v.graph.nodes].sort((a, b) => b.deg - a.deg)[0]!.id)} <small>${[...v.graph.nodes].sort((a, b) => b.deg - a.deg)[0]!.deg} ties</small>` : ''}</dd></dl><p class="muted" style="font-size:12px;margin:6px 0 0">${esc(v.graph.cell)} Ringed: the Handover's roles. Node size: ties held and received. The layout is seeded by person id, so the same id starts in the same place in every cast.</p></div>
</div></div><datalist id="people-list">${v.persons.map(p => `<option value="${esc(p.id)}">${esc(p.name)} · ${esc(p.position)}</option>`).join('')}</datalist></section>`;

  // ---------- roster ----------
  const stepCell = (p: VPerson, s: 'bravery' | 'resentment' | 'fear' | 'loyalty') => `<td data-n="${p.start[s]}" class="stepw${p.start[s] ? ' hi' : ''}" title="${s} ${p.start[s]}">${esc(p.steps[s])}</td>`;
  const chips = (dim: string, label: string, xs: { k: string; n: number }[], fmt: (k: string) => string = k => k) => `<div class="chipset"><span class="lab">${label}</span>${xs.map(x => `<span class="chip btn" data-f="${dim}:${esc(x.k)}" aria-pressed="false" tabindex="0">${dim === 'tem' ? `<i style="background:${TEM_COLOUR[x.k] ?? '#999'}"></i>` : dim === 'unit' ? `<i style="background:${uCol(x.k)}"></i>` : ''}${esc(fmt(x.k))} <small>${x.n}</small></span>`).join('')}</div>`;
  const roleChips = v.roles.filter(r => r.pinned).map(r => ({ k: r.id, n: 1 }));
  const rosterTab = `<section id="tab-roster" class="tab" data-tab="roster" hidden>
<div class="rost-ctl card flat"><div class="row spread"><div class="row"><b id="rcount">${v.persons.length} of ${v.persons.length}</b><span class="muted">people shown</span><button class="btn" id="rclear">Clear filters</button></div><small>Click a header to sort · <kbd>/</kbd> search · <kbd>↑</kbd><kbd>↓</kbd> then <kbd>Enter</kbd> opens a sheet</small></div>
${chips('unit', 'Unit', v.counts.unit, uName)}${chips('tem', 'Temperament', v.counts.temperament)}${chips('need', 'Need', v.counts.need)}${chips('pos', 'Position', v.counts.position)}${chips('shift', 'Shift', v.counts.shift)}
<div class="chipset"><span class="lab">Handover role</span>${roleChips.map(x => `<span class="chip btn" data-f="role:${esc(x.k)}" aria-pressed="false" tabindex="0">${esc(cap(x.k))}</span>`).join('')}<span class="chip btn" data-f="role:any" aria-pressed="false" tabindex="0">any role</span><span class="chip btn" data-f="talker:yes" aria-pressed="false" tabindex="0">Talker at start</span></div>
${v.compare ? `<div class="chipset"><span class="lab">Against ${esc(v.against!.id)}</span>${['added', 'changed', 'same'].map(s => `<span class="chip btn" data-f="diff:${s}" aria-pressed="false" tabindex="0">${s} <small>${v.persons.filter(p => p.diff?.status === s).length}</small></span>`).join('')}</div>` : ''}</div>
<table id="roster"><thead><tr><th data-col="name">Name</th><th data-col="id">Id</th><th data-col="unit">Unit</th><th data-col="pos">Position</th><th data-col="shift">Shift</th><th data-col="tem">Temperament</th><th data-col="need">Need</th><th data-col="b" title="bravery">Bravery</th><th data-col="r" title="resentment">Resent.</th><th data-col="f" title="fear">Fear</th><th data-col="l" title="loyalty">Loyalty</th><th data-col="ties" data-num>Ties</th><th data-col="role">Handover</th>${v.compare ? '<th data-col="diff">Change</th>' : ''}</tr></thead>
<tbody>${v.persons.map(p => `<tr tabindex="0" data-p="${esc(p.id)}" data-unit="${esc(p.unit)}" data-tem="${esc(p.tem)}" data-need="${esc(p.need)}" data-pos="${esc(p.position)}" data-shift="${esc(p.shift ? `${hm(p.shift[0])}-${hm(p.shift[1])}` : 'no shift (resident)')}" data-role="${esc(p.role ?? '')}" data-talker="${p.talker ? 'yes' : 'no'}" data-diff="${esc(p.diff?.status ?? '')}" data-q="${esc([p.name, p.id, p.position, p.unit, p.tem, p.need, p.role ?? '', p.trade, p.home.plot, p.variant, p.workPlace ?? ''].join(' ').toLowerCase())}">
<td data-s="${esc(p.name.toLowerCase())}"><b>${esc(p.name)}</b></td><td class="mono" data-s="${esc(p.id)}">${esc(p.id)}</td><td data-s="${esc(p.unit)}"><i class="t-dot" style="background:${uCol(p.unit)}"></i>${esc(uName(p.unit))}</td><td data-s="${esc(p.position)}">${esc(p.position)}</td><td data-s="${p.shift ? String(p.shift[0]).padStart(4, '0') : '9999'}" class="mono">${p.shift ? `${hm(p.shift[0])}-${hm(p.shift[1])}` : '·'}</td><td data-s="${esc(p.tem)}"><i class="t-dot" style="background:${TEM_COLOUR[p.tem] ?? '#999'}"></i>${esc(v.rules.temperaments[p.tem]?.name ?? p.tem)}${p.talker ? ' <small title="a Talker at the start">talks</small>' : ''}</td><td data-s="${esc(p.need)}">${esc(p.need)}</td>${stepCell(p, 'bravery')}${stepCell(p, 'resentment')}${stepCell(p, 'fear')}${stepCell(p, 'loyalty')}<td data-n="${p.ties.length}">${p.ties.length}</td><td data-s="${esc(p.role ?? 'zzz')}">${p.role ? `<span class="badge-role">${esc(cap(p.role))}</span>` : ''}</td>${v.compare ? `<td data-s="${esc(p.diff?.status ?? '')}">${p.diff ? (p.diff.status === 'added' ? '<span class="chip ok">added</span>' : p.diff.status === 'changed' ? `<span class="chip">${p.diff.fields.length} fields</span>` : '<span class="muted">same</span>') : ''}</td>` : ''}</tr>`).join('')}</tbody></table></section>`;

  // ---------- compare ----------
  const c = v.compare;
  const dimTable = (name: string, xs: { k: string; a: number; b: number }[]) => `<div class="card flat"><h3 style="margin-bottom:6px">${name}</h3><table class="cmpgrid"><tr><th></th><th>${esc(v.against?.id ?? '')}</th><th>${esc(v.id)}</th><th></th></tr>${xs.map(x => `<tr><td>${esc(x.k)}</td><td>${x.a}</td><td>${x.b}</td><td class="${x.b > x.a ? 'up' : x.b < x.a ? 'down' : ''}">${x.b === x.a ? '' : (x.b > x.a ? '+' : '−') + Math.abs(x.b - x.a)}</td></tr>`).join('')}</table></div>`;
  const compareTab = !c ? '' : `<section id="tab-compare" class="tab" data-tab="compare" hidden>${c ? `
<h1>${esc(v.id)} against ${esc(v.against!.id)}</h1><p class="lede">${esc(v.against!.label)}. People are matched by id, which is the expander's own key (<span class="mono">p-&lt;unit&gt;-NN</span>: a seat in a unit, drawn from the cast tables), not a stable person.</p>
<div class="grid g4"><div class="card flat"><h2>${c.added.length}</h2>added</div><div class="card flat"><h2>${c.removed.length}</h2>removed</div><div class="card flat"><h2>${c.changed.length}</h2>changed <small>of ${c.shared} ids in both</small></div><div class="card flat"><h2>${c.renamed}</h2>carry another name <small>${c.renamed === c.shared ? 'every shared id is a different person in the same seat' : ''}</small></div></div>
<div class="card flat" style="margin-top:12px"><h3 style="margin-bottom:6px">Checks on each cast</h3><div class="row" style="gap:5px"><b style="min-width:90px">${esc(v.against!.id)}</b>${c.otherChecks.map(k => `<span class="chip ${k.ok ? 'ok' : 'fail'}">${k.ok ? '✓' : '✗'} ${esc(k.id)}</span>`).join(' ')}</div><div class="row" style="gap:5px;margin-top:6px"><b style="min-width:90px">${esc(v.id)}</b>${v.checks.map(k => `<span class="chip ${k.ok ? 'ok' : 'fail'}">${k.ok ? '✓' : '✗'} ${esc(k.id)}</span>`).join(' ')}</div></div>
${c.renamed === c.shared && c.shared ? `<div class="notice warn" style="margin-top:12px"><div class="ic">!</div><div><h3>Read this as a new population, not an edit</h3><p>${c.renamed} of the ${c.shared} ids in both casts hold a different name. The name pool differs (given names ${c.names.given[0]} → ${c.names.given[1]}, family names ${c.names.family[0]} → ${c.names.family[1]}; ${c.names.kept} names are in both pools), so every draw moves. The comparison that carries meaning is the cast tables and the mix below.</p></div></div>` : ''}
<div class="sect"><h2>The cast tables</h2></div><div class="grid g2">${c.rows.map(r => `<div class="card flat"><h3>${esc(r.key)} <span class="chip ${r.status === 'same' ? '' : r.status === 'changed' ? 'warn' : 'ok'}">${esc(r.status)}</span></h3>${r.lines.length ? `<ul class="plain rowdiff">${r.lines.map(l => `<li>${esc(l)}</li>`).join('')}</ul>` : '<small>identical</small>'}</div>`).join('')}</div>
<div class="sect"><h2>The mix</h2></div><div class="grid g3">${Object.entries(c.dims).map(([k, xs]) => dimTable(cap(k), xs)).join('')}</div>
<div class="sect"><h2>Added</h2></div><div class="card flat">${c.added.length ? c.added.map(id => `<span class="who">${link(id)} <small>${esc(P.get(id)!.position)}, ${esc(P.get(id)!.tem)}</small></span>`).join(' ') : '<small>nobody</small>'}</div>
<div class="sect"><h2>Removed</h2></div><div class="card flat">${c.gone.length ? c.gone.map(g => `<span class="who">${esc(g.name)} <small class="mono">${esc(g.id)}</small> <small>${esc(g.position)}, ${esc(g.tem)}, ${esc(g.need)}</small></span>`).join(' ') : '<small>nobody</small>'}</div>
<div class="sect"><h2>Changed</h2><small>${c.changed.length} people: open one for its field diff</small></div>
<div class="card flat"><table><tr><th>Person</th><th>Fields that differ</th></tr>${c.changed.slice(0, 200).map(id => `<tr><td>${link(id)} <small class="mono">${esc(id)}</small></td><td><small>${esc(P.get(id)!.diff!.fields.filter(f => f[0] !== 'name').map(f => f[0]).join(', ') || 'name only')}</small></td></tr>`).join('')}</table></div>` : '<p class="lede">No second cast to compare with. Use <span class="mono">--against &lt;run|promoted|file&gt;</span>.</p>'}</section>`;

  const tabs: [string, string][] = [['overview', 'Overview'], ['map', 'Map'], ['graph', 'Ties'], ['roster', 'Roster'], ...(v.compare ? [['compare', 'Compare'] as [string, string]] : [])];
  const data = { ...v, map: { bounds: v.map.bounds }, ui: { groups, uColours: Object.fromEntries(groups.map(u => [u, uCol(u)])), temColours: TEM_COLOUR, kindColours: KIND_COLOUR, kindDash: KIND_DASH, shape: Object.fromEntries(Object.keys(TEM_COLOUR).map(t => [t, TEM_SHAPE(t)])) } };
  return `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${esc(v.title)}</title>
<style>${css}${pal}</style></head>
<body>
<header class="top"><div class="brand"><b>The Suborned</b> · people</div><nav class="tabs" role="tablist">${tabs.map(([id, lab], i) => `<button class="tab-btn" role="tab" data-go="${id}" aria-selected="${i === 0}">${lab}<kbd>${i + 1}</kbd></button>`).join('')}</nav><div class="search"><input id="q" type="search" placeholder="Search people  ( / )" aria-label="search people" autocomplete="off"></div></header>
<main>${overview}${mapTab}${graphTab}${rosterTab}${compareTab}</main>
<aside id="sheet" hidden aria-label="character sheet"></aside><div id="tip" hidden></div>
<script type="application/json" id="data">${scriptJson(data)}</script>
<script>${js}</script>
</body></html>
`;
}
