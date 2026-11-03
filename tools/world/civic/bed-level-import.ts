// Brief 077 (Splice): imports the bed-level planting table into contracts/world/civic-look.json `beds.level` (Sky 2026-10-30: the civic beds are planted as one
// bed-level composition each, the testbed's own planting, not BedTuft units every 0.55 m).
//   bun tools/world/civic/bed-level-import.ts           prints the table it would write
//   bun tools/world/civic/bed-level-import.ts --write   writes it into civic-look.json (and its source sha256 into `sources`)
// Reads, never writes: the testbed art store's plaza bake (plaza-garden.bin.gz: its key holds each civic bed's outline in the testbed's plaza plan frame, the bed
// Vela's tools/decokit/domes077.ts composes each piece from), and Vela's 077 output: domes077.manifest.json `civicBeds` (the plaza -> world motion each piece's
// planting was built with: world = R(angle) a + t) and `finals` (each piece's pivot `at`, yaw 0), and out/decokit-077/decokit_manifest.json (the piece rows).
// It writes each outline in its piece's own plan frame (R(angle) a + t - at): where that piece's bed is, as built. The generator (beds.ts) fits that outline,
// vertex i to vertex i, onto the bed's ring to place the piece, so a moved bed moves its piece and a reshaped one fails the fit (CD-5); a piece whose planting
// was built half-turned about its pivot (a half-symmetric bed fitted at the other start vertex) is placed at yaw pi, so it lands as the testbed's bed moved whole.
// The table is keyed by the bed's export id (the civic mass of surface `park` the block gives no piece), one piece each.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { gunzipSync } from 'node:zlib';
import { LOOK_FILE } from './look';

const ART = process.env.TESTBED_ART ?? `${process.env.HOME}/Projects/the-suborned-store/testbed-art/cafe-plants`;
const BAKE = 'plaza-garden.bin.gz';
const repo = resolve(import.meta.dir, '../../..');
const DOMES = resolve(repo, 'out/decokit/troughs077/domes077.manifest.json'), PIECES = resolve(repo, 'out/decokit-077/decokit_manifest.json');
/** The testbed beds our five civic beds are (domes077.manifest.json civicBeds: each moved rigidly, < 1 mm), and the piece that plants each. */
const BEDS = ['civic-facade-southwest', 'civic-facade-southeast', 'civic-office-west', 'civic-office-north', 'civic-dropoff-west'];
const pieceOf = (bed: string) => `CivicBedPlanting_${bed.replace(/^civic-/, '')}`; // Vela's final ids (briefs/077/READY-pieces)

const raw = readFileSync(`${ART}/${BAKE}`), buf = gunzipSync(raw), ab = buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.byteLength);
const size = new DataView(ab).getUint32(0, true), key = JSON.parse(JSON.parse(new TextDecoder().decode(new Uint8Array(ab, 4, size))).key);
const domesText = readFileSync(DOMES), domes = JSON.parse(domesText.toString()), piecesText = readFileSync(PIECES), rows = JSON.parse(piecesText.toString()).assets as { name: string }[];
const beds: Record<string, { piece: string; outline: [number, number][] }> = {};
for (const id of BEDS) {
  const b = key.beds.find((x: { id: string }) => x.id === id), piece = pieceOf(id), m = domes.civicBeds?.[id], fin = domes.finals?.[piece]?.place;
  if (!b) throw new Error(`${BAKE} has no bed ${id}`);
  if (!m || !fin) throw new Error(`${DOMES} has no civicBeds.${id} or finals.${piece}.place`);
  if (!rows.some(r => r.name === piece)) throw new Error(`${PIECES} has no row ${piece}`);
  if (fin.yaw !== 0) throw new Error(`${piece} is placed at yaw ${fin.yaw}: this import takes the piece frame as the world's moved to its pivot (yaw 0)`);
  const c = Math.cos(m.angle), sn = Math.sin(m.angle);
  beds[`civic/${id}`] = { piece, outline: b.vertices.map((v: { x: number; y: number }) => [+(c * v.x - sn * v.y + m.t[0] - fin.at[0]).toFixed(4), +(sn * v.x + c * v.y + m.t[1] - fin.at[1]).toFixed(4)] as [number, number]) };
}
const sha = createHash('sha256').update(raw).digest('hex'), shaOf = (b: Buffer) => createHash('sha256').update(b).digest('hex');
const level = {
  soilProud: 0.003,
  tol: 0.001,
  beds,
  source: `testbed-art/cafe-plants/${BAKE} (sha256 ${sha}): the key's civic flowerBedRun beds, each moved into its piece's plan frame by the motion Vela built it with (out/decokit/troughs077/domes077.manifest.json sha256 ${shaOf(domesText)}: civicBeds, finals.place; pieces out/decokit-077/decokit_manifest.json sha256 ${shaOf(piecesText)}); bun tools/world/civic/bed-level-import.ts --write; brief 077, Sky 2026-10-30. soilProud: the soil sits 3 mm above the kerb top (Vela, briefs/077/domes/metrics.json bedtuft.proposal_for_splice); tol: the rigid fit's largest vertex error, m`,
};

if (!process.argv.includes('--write')) { console.log(JSON.stringify(level, null, 1)); process.exit(0); }
const look = JSON.parse(readFileSync(LOOK_FILE, 'utf8'));
look.sources[`testbed-art/cafe-plants/${BAKE}`] = sha;
look.beds.level = level;
// as the file is written (Python json.dump, indent 1, ASCII): non-ASCII as \u escapes
const text = JSON.stringify(look, null, 1).replace(/[\u0080-\uffff]/g, c => `\\u${c.charCodeAt(0).toString(16).padStart(4, '0')}`) + '\n';
writeFileSync(LOOK_FILE, text);
console.log(`wrote beds.level (${BEDS.length} beds) into ${LOOK_FILE}`);
