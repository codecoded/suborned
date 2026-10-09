// usage: bun run levels transit [--plan contracts/world/harbour-district.plan.json] [--seed station-on-plot] [--rounds 3]
// Needs `bun run up` + `bun run ingest` and a signed-in Claude Code. The final plan is never written to contracts/.
import { retrieve } from '../rag/retrieval';
import { runLevels } from './run';

const args = process.argv.slice(2), opt = (k: string) => (args.includes(k) ? args[args.indexOf(k) + 1] : undefined);
if (args[0] !== 'transit') { console.error('usage: bun run levels transit [--plan <plan.json>] [--seed station-on-plot] [--rounds 3]'); process.exit(2); }
const seed = opt('--seed');
if (seed && seed !== 'station-on-plot') { console.error(`unknown seed ${seed}; known: station-on-plot`); process.exit(2); }
const offline = process.env.LEVELS_TEST_RETRIEVER === 'lexical';
const retriever = offline ? (await import('./test/lexical')).lexical : retrieve;
const r = await runLevels({ plan: opt('--plan') ?? 'contracts/world/harbour-district.plan.json', seed: seed as 'station-on-plot' | undefined, rounds: Number.isFinite(Number(opt('--rounds') ?? 3)) ? Math.min(3, Math.max(1, Math.trunc(Number(opt('--rounds') ?? 3)))) : 3, retriever, retrieval: offline ? 'lexical (test only)' : undefined });
process.exit(r.outcome === 'ACCEPTED' ? 0 : 1);
