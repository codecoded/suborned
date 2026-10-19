// The crew: agent GDD §01 + each §14 role → Claude Markdown and Codex TOML agents.
// usage: bun tools/crew/cards.ts [GDD/<agent-gdd>.md] [--check]
import { readdirSync, readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { resolve, dirname } from 'node:path';

const root = resolve(import.meta.dir, '../..');
const args = process.argv.slice(2);
if (args.some(x => x.startsWith('--') && x !== '--check') || args.filter(x => !x.startsWith('--')).length > 1) throw new Error('Usage: bun run crew [GDD/<agent-gdd>.md] [--check]');
const gddPath = args.find(x => !x.startsWith('--')) ?? `GDD/${readdirSync(resolve(root, 'GDD')).filter(f => f.includes('agent-gdd') && f.endsWith('.md')).sort((a, b) => a.localeCompare(b, 'en', { numeric: true })).at(-1)}`; // numeric: v0.17.0 > v0.9.0
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

// Claude mapping stays unchanged. Codex inherits the selected session model and effort.
const MODEL: Record<string, string> = { 'Opus 5.5': 'opus', 'GPT 6.1 Sol': 'sonnet' };
// Web search and fetch for every role (Sky, 2026-10-15: the GDD doesn't cover a project this complex). runAgent pipeline calls stay tool-less.
const WEB = ['WebSearch', 'WebFetch'];
const WORK = ['Read', 'Edit', 'Write', 'Bash', 'Glob', 'Grep', 'StructuredOutput', ...WEB], REVIEW = ['Read', 'Bash', 'Glob', 'Grep', 'StructuredOutput', ...WEB], UNITY = ['mcp__unity'];
const TOOLS: Record<string, string[]> = {
  Aster: [...WORK, 'Agent'], Latch: [...WORK, ...UNITY], Splice: [...WORK, ...UNITY], Glint: [...WORK, ...UNITY],
  Suture: [...REVIEW, 'Write', ...UNITY], Redline: [...REVIEW, 'Write'],
};

/** Agent name = the job, not the GDD's code name: "Schedule and AI designer" → schedule-ai-designer. */
const agent = (r: Role) => r.job.toLowerCase().replace(/\band\b/g, ' ').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
const roster = `Crew (GDD name → agent): ${roles.map(x => `${x.name} → ${agent(x)}`).join(' · ')}. The GDD and briefs use the names; spawn and address agents by job.`;
const field = (r: Role, k: string) => r.fields.find(f => f[0] === k)?.[1] ?? '';
const index = sections.map(s => `${s.id} ${s.title.slice(4).toLowerCase()} (line ${s.line})`).join(' · ');
const repo = `- Repo: \`GDD/\` design source (the human GDD html wins on conflict; ~2 MB, never load it whole) · \`contracts/\` source-of-truth data (the plan is \`contracts/world/<district>.plan.json\`) · \`tools/\` bun TypeScript (\`tools/plan\` = plan checker + CAD sheet) · \`unity/\` the Unity project (only Latch edits scenes) · \`out/\` generated, never edit by hand.
- Commands: \`bun tools/plan/cli.ts\` (plan checker; exit 1 on any failed rule; writes the sheet to out/plan/) · \`bun run typecheck\` · \`bun run crew\` (regenerates these cards from the agent GDD).`;
const rules = `- Never commit, push or deploy. Never use the browser pane or any browser tool (WebSearch and WebFetch are fine). Never spend paid credits without Aster's go-ahead.
- Research on the web freely. When it shapes a decision, save what you used with its source URLs in \`briefs/<n>/research/\`; the GDD and briefs stay the source, never a web page.
- Done means evidence: the command you ran and its result (checker output, typecheck, Unity compile/console). A build passing is not verification of behaviour.
- Stay inside your role's \`owns\`. Anything outside it goes back to Aster as a handoff, not a silent edit.
- Report back in this order: what changed (paths) · evidence · open issues · next owner (handoff = inputs · done-check · next owner).`;

function card(r: Role, provider: 'claude' | 'codex' = 'claude'): string {
  const lead = r.name === 'Aster';
  const load = lead
    ? `At spawn, read ${gddPath} §01–§13 in full (you are the lead; the GDD is yours).`
    : `Your role block and §01 are above. Open other sections of ${gddPath} by number when the work needs them; yours: ${field(r, 'opens') || '—'}.`;
  const crew = lead
    ? provider === 'claude'
      ? `\n## Leading the crew\nYou are the main thread (\`claude --agent ${agent(r)}\`). Spawn crew members with the Agent tool (subagent_type = their agent name: ${roles.filter(x => x !== r).map(agent).join(', ')}). One objective per owner with a definition of done and the evidence expected; run independent objectives in parallel; have Suture or Redline review; accept or send back. Escalate to Sky only for the §14 escalations.\n`
      : `\n## Leading the crew\nCoordinate from the main Codex session as Aster. Delegate using the available subagent tools and these job names: ${roles.filter(x => x !== r).map(agent).join(', ')}. Give each owner one objective, allowed paths, a done-check and expected evidence. Parallelize independent tasks; assign only one writer per file. Have Suture or Redline independently review, then accept or send back. Retain child task IDs and wait for their results before reporting completion. Do not spawn another lead when the main session already owns coordination. Escalate to Sky only for the §14 escalations.\n`
    : '';
  const preamble = provider === 'claude' ? `---
name: ${agent(r)}
description: "${r.job} (${r.name}). Owns: ${field(r, 'owns').replace(/"/g, "'")}"
model: ${MODEL[r.model] ?? 'inherit'}
tools: ${(TOOLS[r.name] ?? WORK).join(', ')}
---
# ${r.name} · ${r.job}
GDD model: ${r.model}${MODEL[r.model] === 'sonnet' ? ' (target); runs on Claude sonnet for now' : ''}. Generated from ${gddPath} by tools/crew/cards.ts; edit the GDD, never this card.`
    : `# ${r.name} · ${r.job}\nGDD model preference: ${r.model}. This Codex role inherits the session's selected model and reasoning effort. Generated from ${gddPath} by tools/crew/cards.ts; edit the GDD, never this card.`;
  return `${preamble}

## Role (§14)
${r.fields.map(([k, v]) => `${k}: ${v}`).join('\n')}

## Core (§01)
${header}

${section('§01')}
${crew}
## Working here
- ${load}
- GDD sections: ${index}.
- ${roster}
${repo}
${provider === 'claude' ? rules : rules.replace('(WebSearch and WebFetch are fine)', '(web search and fetching source documents are fine)')}
${provider === 'codex' ? codexWorkingRules(r) : ''}`;
}

function codexWorkingRules(r: Role): string {
  return `
## Codex runtime
- Read AGENTS.md and CLAUDE.md before development; the shared repo rules apply to both crews. Assignment review instructions take precedence when marking.
- Respect the user's task scope. For Assignment 5 code-only work, do not open Unity, model, or generate assets even if your general role covers them.
- Share the working tree carefully: never git stash, checkout, reset or clean. Do not overwrite another owner's work; coordinate file ownership with Aster.
- Use native named subagents when available. In T3, if named roles are not exposed, pass this role's full developer_instructions to the app-owned delegated task using the current Codex provider/model from its live catalog. Keep delegated tasks under this thread; do not create top-level conversations.
- Permissions, tools and MCP servers come from the parent session. Claude tool names are not a Codex tool allowlist. Do not assume Unity MCP exists or install/start it for code-only work.
${['Suture', 'Redline'].includes(r.name) ? '- Review without editing production code. Write test reports/evidence within the assigned paths; send fixes back to the implementation owner. This is an instruction boundary, not a separate read-only sandbox.\n' : ''}`;
}

// JSON basic strings are valid TOML basic strings and safely encode quotes/newlines.
function codexCard(r: Role): string {
  return `# Generated by bun run crew; edit the GDD, never this file.
name = ${JSON.stringify(agent(r))}
description = ${JSON.stringify(`${r.job} (${r.name}). Owns: ${field(r, 'owns')}`)}
developer_instructions = ${JSON.stringify(card(r, 'codex'))}
`;
}

const outputs = roles.flatMap(r => [
  { path: `.claude/agents/${agent(r)}.md`, content: card(r) },
  { path: `.codex/agents/${agent(r)}.toml`, content: codexCard(r) },
]);
if (args.includes('--check')) {
  const stale = outputs.filter(f => !existsSync(resolve(root, f.path)) || readFileSync(resolve(root, f.path), 'utf8') !== f.content);
  if (stale.length) {
    console.error(`Crew cards need regeneration: ${stale.map(f => f.path).join(', ')}. Run bun run crew.`);
    process.exitCode = 1;
  } else console.log(`Crew current: ${roles.length} Claude + ${roles.length} Codex roles from ${gddPath}.`);
} else {
  // Leave unrelated custom agents alone; only the GDD's named outputs are managed.
  for (const f of outputs) {
    mkdirSync(dirname(resolve(root, f.path)), { recursive: true });
    writeFileSync(resolve(root, f.path), f.content);
  }
  console.log(`${roles.length} Claude + ${roles.length} Codex roles generated from ${gddPath}.`);
}
