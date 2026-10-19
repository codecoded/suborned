import { lstat, mkdir, readFile, readdir, realpath, writeFile } from 'node:fs/promises';
import { dirname, isAbsolute, relative, resolve } from 'node:path';
import { hash } from '../rag/corpus';
import { ScopeSchema, type Scope } from './schema';

export const json = async (path: string, value: unknown) => { await mkdir(dirname(path), { recursive: true }); await writeFile(path, JSON.stringify(value, null, 2) + '\n'); };

/** Paths come from a reviewed scope or a model response; neither may escape through .. or a symlink. */
export async function localPath(root: string, path: string): Promise<string> {
  if (isAbsolute(path) || path.includes('\\') || path.split('/').some(p => !p || p === '.' || p === '..')) throw new Error(`Unsafe relative path: ${path}`);
  const base = await realpath(root), target = resolve(base, path);
  let parent = target;
  while (parent !== base) {
    const stat = await lstat(parent).catch(e => { if (e.code === 'ENOENT') return undefined; throw e; });
    if (stat?.isSymbolicLink()) throw new Error(`Symlink refused: ${path}`);
    parent = dirname(parent);
  }
  return target;
}

export async function textOrNull(path: string): Promise<string | null> {
  return readFile(path, 'utf8').catch(e => { if (e.code === 'ENOENT') return null; throw e; });
}

export async function loadScope(root: string, path: string): Promise<Scope> {
  const scope = ScopeSchema.parse(JSON.parse(await readFile(await localPath(root, path), 'utf8')));
  const unique = (xs: string[], name: string) => { if (new Set(xs).size !== xs.length) throw new Error(`Duplicate ${name}`); };
  unique(scope.requirements.map(r => r.id), 'requirement'); unique(scope.checks.map(c => c.id), 'check'); unique(scope.writable, 'writable path');
  const ids = new Set(scope.requirements.map(r => r.id)), checks = new Set(scope.checks.map(c => c.id));
  if (!checks.has(scope.regression)) throw new Error('Missing regression check');
  for (const r of scope.requirements) {
    if (!scope.sources[r.source] || r.dependsOn.some(d => !ids.has(d) || d === r.id) || r.checks.some(c => !checks.has(c))) throw new Error(`Invalid references for ${r.id}`);
  }
  const visit = (id: string, ancestors: Set<string>) => {
    if (ancestors.has(id)) throw new Error(`Dependency cycle at ${id}`);
    for (const d of scope.requirements.find(r => r.id === id)!.dependsOn) visit(d, new Set([...ancestors, id]));
  };
  for (const r of scope.requirements) visit(r.id, new Set());
  for (const path of scope.writable) {
    if (!/^sim\/Suborned\.Sim\/[A-Za-z][A-Za-z0-9.]*\.cs$/.test(path)) throw new Error(`Version 1 writes shared simulation C# only: ${path}`);
    await localPath(root, path);
  }
  for (const source of scope.sources) {
    const text = await readFile(await localPath(root, source.path), 'utf8');
    if (!text.includes(source.quote)) throw new Error(`GDD source changed or quote absent: ${source.path}`);
  }
  for (const c of scope.context) {
    await localPath(root, c.path);
    if (c.from && c.to && c.to < c.from) throw new Error(`Invalid context range: ${c.path}`);
  }
  await localPath(root, scope.testProject);
  return scope;
}

export async function sourceFiles(root: string, directories: string[]): Promise<string[]> {
  const paths: string[] = [];
  const walk = async (rel: string) => {
    for (const entry of await readdir(await localPath(root, rel), { withFileTypes: true })) {
      if (['bin', 'obj', 'node_modules', '.git', 'TestResults'].includes(entry.name)) continue;
      const path = `${rel}/${entry.name}`;
      if (entry.isSymbolicLink()) throw new Error(`Source symlink refused: ${path}`);
      if (entry.isDirectory()) await walk(path);
      else if (/\.(cs|csproj|json)$/.test(entry.name)) paths.push(path);
    }
  };
  for (const d of directories) await walk(d);
  return paths.sort();
}

export async function fileHashes(root: string, paths: string[]): Promise<Record<string, string>> {
  return Object.fromEntries(await Promise.all(paths.map(async path => [path, hash(await readFile(await localPath(root, path), 'utf8'))])));
}

export type FileState = Record<string, string | null>;
export async function fileState(root: string, paths: string[]): Promise<FileState> {
  return Object.fromEntries(await Promise.all(paths.map(async path => [path, await textOrNull(await localPath(root, path))])));
}

export const portable = (root: string, path: string) => relative(root, path).split('\\').join('/');
