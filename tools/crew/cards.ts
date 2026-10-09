// The crew: agent GDD §01 + each §14 role block → .claude/agents/<name>.md (Claude Code subagents).
// usage (repo root): bun tools/crew/cards.ts [GDD/<agent-gdd>.md]   ·   never edit a card: edit the GDD, rerun.
import { readdirSync, readFileSync, writeFileSync, mkdirSync, rmSync } from 'node:fs';
import { resolve, relative } from 'node:path';

const root = resolve(import.meta.dir, '../..');
const gddPath = process.argv[2] ?? `GDD/${readdirSync(resolve(root, 'GDD')).filter(f => f.includes('agent-gdd') && f.endsWith('.md')).sort().at(-1)}`;
const text = readFileSync(resolve(root, gddPath), 'utf8');
const lines = text.split('\n');

/** `## §NN TITLE` headings with their line numbers, for opening sections on demand. */
const sections = lines.flatMap((l, i) => (l.startsWith('## §') ? [{ id: l.slice(3, 6), title: l.slice(3), line: i + 1 }] : []));
const section = (id: string) => {
  const s = sections.findIndex(x => x.id === id), end = sections[s + 1]?.line ?? lines.length + 1;
  return lines.slice(sections[s].line, end - 1).join('\n').trim();
};
const header = lines.slice(1, sections[0].line - 1).join('\n').trim();

type Role = { name: string; job: string; model: string; fields: [string, string][] };
const roles: Role[] = section('§14').split(/^### /m).slice(1).map(block => {
  const [head, ...rest] = block.split('\n');
  const [name, job, model] = head.split(' · ').map(s => s.trim());
  return { name, job, model, fields: rest.filter(l => /^[a-z]+: /.test(l)).map(l => [l.slice(0, l.indexOf(':')), l.slice(l.indexOf(':') + 2)] as [string, string]) };
});

// Claude only for now: GDD Opus 5.5 → opus, GDD GPT 6.1 Sol → sonnet (target model kept in the card).
const MODEL: Record<string, string> = { 'Opus 5.5': 'opus', 'GPT 6.1 Sol': 'sonnet' };
const WORK = ['Read', 'Edit', 'Write', 'Bash', 'Glob', 'Grep', 'StructuredOutput'], REVIEW = ['Read', 'Bash', 'Glob', 'Grep', 'StructuredOutput'], UNITY = ['mcp__unity'];
const TOOLS: Record<string, string[]> = {
  Aster: [...WORK, 'Agent'], Latch: [...WORK, ...UNITY], Splice: [...WORK, ...UNITY], Glint: [...WORK, ...UNITY],
  Suture: [...REVIEW, 'Write', ...UNITY], Redline: [...REVIEW, 'Write'],
};

const field = (r: Role, k: string) => r.fields.find(f => f[0] === k)?.[1] ?? '';
const index = sections.map(s => `${s.id} ${s.title.slice(4).toLowerCase()} (line ${s.line})`).join(' · ');
const repo = `- Repo: \`GDD/\` design source (the human GDD html wins on conflict; ~2 MB, never load it whole) · \`contracts/\` source-of-truth data (the plan is \`contracts/world/<district>.plan.json\`) · \`tools/\` bun TypeScript (\`tools/plan\` = plan checker + CAD sheet) · \`unity/\` the Unity project (only Latch edits scenes) · \`out/\` generated, never edit by hand.
- Commands: \`bun tools/plan/cli.ts\` (plan checker; exit 1 on any failed rule; writes the sheet to out/plan/) · \`bun run typecheck\` · \`bun run crew\` (regenerates these cards from the agent GDD).`;
const rules = `- Never commit, push or deploy. Never use the browser pane or any browser tool. Never spend paid credits without Aster's go-ahead.
- Done means evidence: the command you ran and its result (checker output, typecheck, Unity compile/console). A build passing is not verification of behaviour.
- Stay inside your role's \`owns\`. Anything outside it goes back to Aster as a handoff, not a silent edit.
- Report back in this order: what changed (paths) · evidence · open issues · next owner (handoff = inputs · done-check · next owner).`;

function card(r: Role): string {
  const lead = r.name === 'Aster';
  const load = lead
    ? `At spawn, read ${gddPath} §01–§13 in full (you are the lead; the GDD is yours).`
    : `Your role block and §01 are above. Open other sections of ${gddPath} by number when the work needs them; yours: ${field(r, 'opens') || '—'}.`;
  const crew = lead
    ? `\n## Leading the crew\nYou are the main thread (\`claude --agent aster\`). Spawn crew members with the Agent tool (subagent_type = their lowercase name: ${roles.filter(x => x.name !== 'Aster').map(x => x.name.toLowerCase()).join(', ')}). One objective per owner with a definition of done and the evidence expected; run independent objectives in parallel; have Suture or Redline review; accept or send back. Escalate to Sky only for the §14 escalations.\n`
    : '';
  return `---
name: ${r.name.toLowerCase()}
description: "${r.job} (${r.name}). Owns: ${field(r, 'owns').replace(/"/g, "'")}"
model: ${MODEL[r.model] ?? 'inherit'}
tools: ${(TOOLS[r.name] ?? WORK).join(', ')}
---
# ${r.name} · ${r.job}
GDD model: ${r.model}${MODEL[r.model] === 'sonnet' ? ' (target); runs on Claude sonnet for now' : ''}. Generated from ${gddPath} by tools/crew/cards.ts; edit the GDD, never this card.

## Role (§14)
${r.fields.map(([k, v]) => `${k}: ${v}`).join('\n')}

## Core (§01)
${header}

${section('§01')}
${crew}
## Working here
- ${load}
- GDD sections: ${index}.
${repo}
${rules}
`;
}

const dir = resolve(root, '.claude/agents');
rmSync(dir, { recursive: true, force: true });
mkdirSync(dir, { recursive: true });
for (const r of roles) writeFileSync(resolve(dir, `${r.name.toLowerCase()}.md`), card(r));
console.log(`${roles.length} cards from ${gddPath} → ${relative(root, dir)}/: ${roles.map(r => `${r.name.toLowerCase()} (${MODEL[r.model]})`).join(', ')}`);
