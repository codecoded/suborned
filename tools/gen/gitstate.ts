// Which commit a run was made from, and whether the working tree had edits on top of it (run.json `commit`, `dirty`).
// A run made from an uncommitted tree names a code state no commit holds: `dirty` says so.
import { root } from '../rag/config';

export type GitState = { commit: string | null; dirty: boolean };

export function gitState(cwd: string = root): GitState {
  const git = (...args: string[]) => Bun.spawnSync(['git', ...args], { cwd, stdout: 'pipe', stderr: 'ignore' });
  const head = git('rev-parse', 'HEAD');
  if (head.exitCode !== 0) return { commit: null, dirty: false };
  return { commit: head.stdout.toString().trim(), dirty: git('status', '--porcelain').stdout.toString().trim() !== '' };
}
