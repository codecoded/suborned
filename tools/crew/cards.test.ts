import { describe, expect, test } from 'bun:test';
import { readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';

const root = resolve(import.meta.dir, '../..');
const read = (path: string) => readFileSync(resolve(root, path), 'utf8');
const names = readdirSync(resolve(root, '.claude/agents')).filter(x => x.endsWith('.md')).map(x => x.slice(0, -3));
const role = (name: string) => Bun.TOML.parse(read(`.codex/agents/${name}.toml`)) as Record<string, string>;
const block = (text: string, from: string, to: string) => text.split(from)[1]?.split(to)[0]?.trim();

describe('GDD crew in both clients', () => {
  test('every Claude role has a parseable Codex counterpart with identical responsibilities and core', () => {
    expect(names).toHaveLength(17);
    for (const name of names) {
      const claude = read(`.claude/agents/${name}.md`), codex = role(name);
      expect(codex.name).toBe(name);
      expect(codex.description).toContain('Owns:');
      expect(block(codex.developer_instructions, '## Role (§14)', '## Core (§01)')).toBe(block(claude, '## Role (§14)', '## Core (§01)'));
      const end = name === 'lead-producer' ? '## Leading the crew' : '## Working here';
      expect(block(codex.developer_instructions, '## Core (§01)', end)).toBe(block(claude, '## Core (§01)', end));
      expect(codex.developer_instructions).toContain('Crew (GDD name → agent):');
      expect(codex.developer_instructions).not.toContain('subagent_type');
      expect(codex.developer_instructions).not.toContain('claude --agent');
      // A provider port must not silently replace user-selected models or permissions.
      expect(Object.keys(codex).sort()).toEqual(['description', 'developer_instructions', 'name']);
    }
  });

  test('coordinator assigns one writer per file and independent reviewers', () => {
    const lead = role('lead-producer').developer_instructions;
    expect(lead).toContain('only one writer per file');
    expect(lead).toContain('Suture or Redline independently review');
    expect(lead).toContain('Retain child task IDs');
    for (const name of names.filter(x => x !== 'lead-producer')) expect(lead).toContain(name);
    for (const name of ['code-performance-reviewer', 'playtest-qa-lead']) {
      expect(role(name).developer_instructions).toContain('Review without editing production code');
    }
  });

  test('project config enables a bounded crew without overriding session credentials or permissions', () => {
    const config = Bun.TOML.parse(read('.codex/config.toml'));
    expect(config).toEqual({ agents: { enabled: true, max_concurrent_threads_per_session: 3 } });
  });

  test('both generated sets match the current GDD', () => {
    const result = Bun.spawnSync(['bun', 'tools/crew/cards.ts', '--check'], { cwd: root });
    expect(result.stderr.toString()).toBe('');
    expect(result.exitCode).toBe(0);
    expect(result.stdout.toString()).toContain('17 Claude + 17 Codex');
  });
});
