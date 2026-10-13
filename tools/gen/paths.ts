// Where runs live: committed under runs/levels, regenerated under out/levels. Tests point both at a scratch folder.
import { resolve } from 'node:path';
import { readdirSync } from 'node:fs';
import { root } from '../rag/config';

export const runsRoot = () => process.env.LEVELS_RUNS ?? resolve(root, 'runs/levels');
export const outRoot = () => process.env.LEVELS_OUT ?? resolve(root, 'out/levels');
export const pad = (n: number, w = 3) => String(n).padStart(w, '0');
export const runDirs = (plan: string) => { try { return readdirSync(resolve(runsRoot(), plan)).filter(d => /^r\d{3}$/.test(d)).sort(); } catch { return []; } };
export const nextRun = (plan: string) => (runDirs(plan).length ? Number(runDirs(plan).at(-1)!.slice(1)) + 1 : 1);
export const nextMinor = (version: string) => { const [a, b] = version.split('.').map(Number); return `${a}.${b! + 1}.0`; };
export const candidateVersion = (input: string, run: number, round: number) => `${nextMinor(input)}-r${pad(run)}.${round}`;
