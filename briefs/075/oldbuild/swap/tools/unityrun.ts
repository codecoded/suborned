// Brief 075 swap (Latch): one offscreen Unity batch step through the playing guard, with ProjectSettings/QualitySettings.asset and GraphicsSettings.asset
// held: the old render-settings swap (OldTownProof.WithOldSettings) restores the quality level's pipeline from GetRenderPipelineAssetAt, which reads null in
// batch mode, and Unity then saves `customRenderPipeline: {fileID: 0}`. The two files' bytes are read before the step and written back after it if Unity
// changed them (logged). usage: bun briefs/075/oldbuild/swap/tools/unityrun.ts <log> -- <Unity args after -projectPath>
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
const root = resolve(import.meta.dir, '../../../../..');
const UNITY = '/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity';
const args = process.argv.slice(2), sep = args.indexOf('--');
if (sep < 1) { console.error('usage: unityrun.ts <log> -- <unity args>'); process.exit(2); }
const files = ['unity/ProjectSettings/QualitySettings.asset', 'unity/ProjectSettings/GraphicsSettings.asset'].map(f => resolve(root, f));
const held = files.map(f => readFileSync(f));
const r = spawnSync('bun', ['tools/game/playing.ts', UNITY, '-batchmode', '-quit', '-projectPath', resolve(root, 'unity'), '-logFile', resolve(root, args[0]!), ...args.slice(sep + 1)], { cwd: root, stdio: 'inherit' });
files.forEach((f, i) => { if (!readFileSync(f).equals(held[i]!)) { writeFileSync(f, held[i]!); console.log(`unityrun: ${f.replace(root + '/', '')} put back as it was before the step`); } });
console.log(`unityrun: ${args[0]} exit ${r.status}`);
process.exit(r.status ?? 1);
