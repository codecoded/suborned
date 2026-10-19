import { afterEach, describe, expect, test } from 'bun:test';
import { mkdtemp, mkdir, rm, symlink, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { hash } from '../../rag/corpus';
import { selectLatestGdd } from '../../rag/gdd-sources';
import { assertLatestGdd, buildCodeIndex, compareVersions, mainWorktreeRoots, readCodeFiles, readCodeRanges, resolveLatestGdd, searchCode } from '../discovery-sources';

const dirs: string[] = [];
afterEach(async () => { await Promise.all(dirs.splice(0).map(p => rm(p, { recursive: true, force: true }))); });
async function fixture(): Promise<string> {
  const root = await mkdtemp(join(tmpdir(), 'a05-discovery-')); dirs.push(root);
  await mkdir(join(root, 'GDD')); await mkdir(join(root, 'sim/Suborned.Sim'), { recursive: true });
  return root;
}
const names = (version: string, date = '2026-10-18') => [`the-suborned-agent-gdd-v${version}-${date}.md`, `the-suborned-gdd-v${version}-${date}.html`];
const design = '# GDD\n\n## §01 GAME + CORE\nDeterministic orders.\n\n## §05 PRESSURE\nWatch teaches servicing time.\n\n## §14 ROLES\n### Keel\nOwns simulation.\n\n## §15 ART\nCream stone.\n';
async function gdd(root: string, version = '0.24.0', text = design, human = '<h1>Human design</h1>'): Promise<void> {
  const [agentName, humanName] = names(version);
  await writeFile(join(root, 'GDD', agentName!), text);
  await writeFile(join(root, 'GDD', humanName!), human);
}

describe('latest GDD sources', () => {
  test('semantic version wins over lexical filename and date, consistently for retrieval', async () => {
    const root = await fixture(); await gdd(root, '0.9.0'); await gdd(root, '0.24.0');
    expect(compareVersions('0.24.0', '0.9.0')).toBe(1);
    expect(compareVersions('1.0.0', '0.99.99')).toBe(1);
    expect(compareVersions('0.24.0', '0.24.0')).toBe(0);
    const snapshot = await resolveLatestGdd(root);
    expect(snapshot.agent.version).toBe('0.24.0');
    expect(selectLatestGdd([...names('0.9.0', '2027-01-01'), ...names('0.24.0')]).agent.path).toBe(snapshot.agent.path);
    expect(snapshot.human.version).toBe(snapshot.agent.version);
    expect(snapshot.agent.sha256).toBe(hash(design));
    expect('text' in snapshot.human).toBe(false);
  });

  test('rejects mismatched, missing, and ambiguous source pairs', async () => {
    const root = await fixture(); await gdd(root);
    await writeFile(join(root, 'GDD', names('0.25.0')[0]!), design);
    await expect(resolveLatestGdd(root)).rejects.toThrow('version mismatch');
    expect(() => selectLatestGdd([names('0.24.0')[0]!])).toThrow('No versioned human');
    expect(() => selectLatestGdd([...names('0.24.0'), names('0.24.0', '2026-10-19')[0]!])).toThrow('Ambiguous latest agent');
  });

  test('all numbered sections including roles and art retain true line boundaries', async () => {
    const root = await fixture(); await gdd(root);
    const snapshot = await resolveLatestGdd(root);
    expect(snapshot.sections.map(s => s.id)).toEqual(['01', '05', '14', '15']);
    for (const section of snapshot.sections) {
      expect(design.split('\n').slice(section.from - 1, section.to).join('\n')).toBe(section.text);
      expect(section.sha256).toBe(hash(section.text));
      expect(section.path).toBe(snapshot.agent.path);
    }
    expect(snapshot.sections[2]!.text).toContain('### Keel');
  });

  test('invalid sections and symlinked designs fail rather than escape the checkout', async () => {
    const root = await fixture(); await gdd(root, '0.24.0', '## §01 A\n## §01 Duplicate');
    await expect(resolveLatestGdd(root)).rejects.toThrow('unique numbered');
    const other = await fixture(); await gdd(other);
    await rm(join(root, 'GDD', names('0.24.0')[0]!));
    await symlink(join(other, 'GDD', names('0.24.0')[0]!), join(root, 'GDD', names('0.24.0')[0]!));
    await expect(resolveLatestGdd(root)).rejects.toThrow('Symlink refused');
  });

  test('main-checkout freshness records matching hashes and rejects newer or same-version drift', async () => {
    const root = await fixture(), main = await fixture(); await gdd(root); await gdd(main);
    const fresh = await assertLatestGdd(root, [main]);
    expect(fresh.comparisons).toEqual([{ root: await import('node:fs/promises').then(fs => fs.realpath(main)), version: '0.24.0', agentHash: hash(design), humanHash: hash('<h1>Human design</h1>'), status: 'same' }]);
    await gdd(main, '0.24.0', design, '<h1>Changed authority</h1>');
    await expect(assertLatestGdd(root, [main])).rejects.toThrow('different content at the same');
    await gdd(main, '0.25.0');
    await expect(assertLatestGdd(root, [main])).rejects.toThrow('newer v0.25.0');
  });

  test('a newer local design is allowed while an older main is explicitly recorded', async () => {
    const root = await fixture(), main = await fixture(); await gdd(root, '0.25.0'); await gdd(main);
    expect((await assertLatestGdd(root, [root, main])).comparisons.map(c => c.status)).toEqual(['older']);
    expect(mainWorktreeRoots('worktree /tmp/main with spaces\nHEAD abc\nbranch refs/heads/main\n\nworktree /tmp/assignment\nHEAD def\nbranch refs/heads/assignment-05\n')).toEqual(['/tmp/main with spaces']);
  });
});

describe('current code navigation and evidence', () => {
  const source = '// Scout knowledge\npublic partial class Game\n{\n  public bool Watch(int target) => true;\n  // literal [a-z]+ is not a search regex\n}\n';
  async function codeFixture(): Promise<string> {
    const root = await fixture();
    await writeFile(join(root, 'sim/Suborned.Sim/Game.cs'), source);
    await mkdir(join(root, 'sim/Suborned.Sim/obj'));
    await writeFile(join(root, 'sim/Suborned.Sim/obj/Generated.cs'), 'generated');
    await mkdir(join(root, 'contracts/rules'), { recursive: true });
    await writeFile(join(root, 'contracts/rules/rulebook.json'), '{\n  "watch": { "duration": 20 }\n}\n');
    return root;
  }

  test('indexes simulation C# and rule contracts, excluding build output', async () => {
    const root = await codeFixture(), index = await buildCodeIndex(root);
    expect(index.map(i => i.path)).toEqual(['contracts/rules/rulebook.json', 'sim/Suborned.Sim/Game.cs']);
    expect(index[1]!.symbols).toEqual(['Game', 'Watch']);
    expect(index[0]!.symbols).toEqual(['watch']);
    expect(index[1]!.summary).toContain('Scout knowledge');
    expect(index.every(i => i.summary.length <= 1200)).toBe(true);
    expect(await readCodeFiles(root, index, ['sim/Suborned.Sim/Game.cs'])).toEqual([{ path: 'sim/Suborned.Sim/Game.cs', sha256: hash(source), from: 1, to: 7, text: source }]);
  });

  test('literal search includes rules, reports truncation, and preserves source lines', async () => {
    const root = await codeFixture(), index = await buildCodeIndex(root);
    const literal = await searchCode(root, index, ['[a-z]+']);
    expect(literal.matches).toHaveLength(1); expect(literal.matches[0]!.line).toBe(5);
    expect(literal.searchedFiles).toBe(2); expect(literal.truncated).toBe(false);
    const bounded = await searchCode(root, index, ['watch'], { maxMatches: 1 });
    expect(bounded.matches).toHaveLength(1); expect(bounded.truncated).toBe(true);
    expect(bounded.matches[0]!.path).toBe('contracts/rules/rulebook.json');
  });

  test('bounded search exposes later tests and headless consumers despite repeated early matches', async () => {
    const root = await codeFixture();
    await writeFile(join(root, 'sim/Suborned.Sim/Game.cs'), 'public class Game {}\n' + '// Watch\n'.repeat(100));
    await mkdir(join(root, 'sim/Suborned.Sim.Tests'), { recursive: true });
    await writeFile(join(root, 'sim/Suborned.Sim.Tests/WatchTests.cs'), '// Watch behavior\n');
    await mkdir(join(root, 'tools/sim'), { recursive: true });
    await writeFile(join(root, 'tools/sim/consumer.ts'), 'export const Watch = true;\n');
    const index = await buildCodeIndex(root);
    const result = await searchCode(root, index, ['Watch'], { maxMatches: 4 });
    expect(result.truncated).toBe(true);
    expect(new Set(result.matches.map(m => m.path)).size).toBe(4);
    expect(result.matches.some(m => m.path === 'tools/sim/consumer.ts')).toBe(true);
    expect((await readCodeRanges(root, index, [{ path: 'tools/sim/consumer.ts', from: 1, to: 1 }]))[0]!.text).toContain('export const');
  });

  test('unknown paths, duplicates, oversized context, and stale source are rejected', async () => {
    const root = await codeFixture(), index = await buildCodeIndex(root), path = 'sim/Suborned.Sim/Game.cs';
    await expect(readCodeFiles(root, index, ['../escape'])).rejects.toThrow('not in discovery index');
    await expect(readCodeFiles(root, index, [path, path])).rejects.toThrow('Duplicate');
    await expect(readCodeFiles(root, index, [path], 10)).rejects.toThrow('exceed');
    await writeFile(join(root, path), source + '// changed');
    await expect(readCodeFiles(root, index, [path])).rejects.toThrow('Code changed');
    await expect(searchCode(root, index, ['watch'])).rejects.toThrow('Code changed');
  });

  test('symlinked source and rule files cannot bypass root containment', async () => {
    const root = await codeFixture();
    await symlink(join(root, 'sim/Suborned.Sim/Game.cs'), join(root, 'contracts/rules/alias.json'));
    await expect(buildCodeIndex(root)).rejects.toThrow('Symlink refused');
  });

  test('range reads are line-accurate and pinned while source outside the excerpt remains hashed', async () => {
    const root = await codeFixture(), index = await buildCodeIndex(root), path = 'sim/Suborned.Sim/Game.cs';
    expect(await readCodeRanges(root, index, [{ path, from: 2, to: 4 }])).toEqual([
      { path, sha256: hash(source), from: 2, to: 4, text: source.split('\n').slice(1, 4).join('\n') },
    ]);
    await writeFile(join(root, path), source + '// outside requested range');
    await expect(readCodeRanges(root, index, [{ path, from: 2, to: 4 }])).rejects.toThrow('Code changed');
  });

  test('range limits reject malformed, oversized, excessive and aggregate-overbudget requests', async () => {
    const root = await codeFixture(), index = await buildCodeIndex(root), path = 'sim/Suborned.Sim/Game.cs';
    for (const [from, to] of [[0, 1], [1.5, 2], [3, 2], [1, 501]]) {
      await expect(readCodeRanges(root, index, [{ path, from: from!, to: to! }])).rejects.toThrow('Invalid code line range');
    }
    await expect(readCodeRanges(root, index, [{ path, from: 1, to: 20 }])).rejects.toThrow('exceeds 7 lines');
    await expect(readCodeRanges(root, index, Array.from({ length: 11 }, () => ({ path, from: 1, to: 1 })))).rejects.toThrow('At most 10');
    await expect(readCodeRanges(root, index, [{ path, from: 1, to: 4 }], 10)).rejects.toThrow('exceed 10 characters');
    await expect(readCodeRanges(root, index, [{ path: '../Game.cs', from: 1, to: 1 }])).rejects.toThrow('not in discovery index');
  });
});
