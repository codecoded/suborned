// bun tools/audio/cli.ts          make the clips (tools/audio/synth.ts) and write contracts/audio/clips.json
// bun tools/audio/cli.ts --check  check the clips on disk against clips.json and the cue mapping against both; exit 1 on any failure
import { build } from './synth.ts';
import { check } from './check.ts';

if (process.argv.includes('--check')) {
  const errs = check();
  if (errs.length) { for (const e of errs) console.error('FAIL', e); process.exit(1); }
  console.log('audio: ok');
} else {
  for (const [k, v] of Object.entries(build())) console.log(k, JSON.stringify(v));
}
