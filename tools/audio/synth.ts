// Chime's build-time synth (brief 059, 2026-10-26): makes the rival car's two sounds, deterministically, with no model and no spend.
//   rivals_coming.wav     the timing cue at 2:35 (log code rivals.coming): a swish of a car turning in, then a low two-tone horn falling a minor third.
//   rival_car_engine.wav  the rival car's own loop while it drives: an atomic-turbine whine pair (beating), a road rumble and tyre hiss.
// Same code -> same bytes (a fixed-seed generator, no clock). Writes 48 kHz 16-bit mono WAVs into unity/Assets/Suborned/Resources/Audio/ with
// their AudioImporter .meta (a fixed guid per path), and contracts/audio/clips.json (sha256, length, peak, RMS, loop seam) for the checker.
// Run: bun tools/audio/cli.ts
import { createHash } from 'node:crypto';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

export const SR = 48000;
export const root = join(import.meta.dir, '..', '..');
export const audioDir = join(root, 'unity/Assets/Suborned/Resources/Audio');

/** mulberry32: a small seeded generator, so the noise is the same every run. */
function rng(seed: number) { let a = seed >>> 0; return () => { a = (a + 0x6d2b79f5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; }
const noise = (n: number, seed: number) => { const r = rng(seed); const o = new Float64Array(n); for (let i = 0; i < n; i++) o[i] = r() * 2 - 1; return o; };
const TAU = Math.PI * 2;
const smooth = (x: number) => x <= 0 ? 0 : x >= 1 ? 1 : x * x * (3 - 2 * x);

/** One-pole low-pass. `circular` runs the buffer twice and keeps the second pass, so the end flows into the start (a seamless loop). */
function lowpass(x: Float64Array, hz: number, circular = false): Float64Array {
  const a = 1 - Math.exp(-TAU * hz / SR), o = new Float64Array(x.length); let y = 0;
  for (let p = 0; p < (circular ? 2 : 1); p++) for (let i = 0; i < x.length; i++) { y += a * (x[i] - y); o[i] = y; }
  return o;
}
const highpass = (x: Float64Array, hz: number, circular = false) => { const l = lowpass(x, hz, circular); return x.map((v, i) => v - l[i]); };
const bandpass = (x: Float64Array, lo: number, hi: number, circular = false) => lowpass(highpass(x, lo, circular), hi, circular);

/** Band-pass noise whose centre sweeps: a state-variable filter (Chamberlin) so the centre can move each sample. */
function sweepNoise(n: number, seed: number, fc: (t: number) => number, q: number): Float64Array {
  const x = noise(n, seed), o = new Float64Array(n); let low = 0, band = 0;
  for (let i = 0; i < n; i++) {
    const f = 2 * Math.sin(Math.PI * Math.min(fc(i / SR), SR / 6) / SR), high = x[i] - low - band / q;
    band += f * high; low += f * band; o[i] = band;
  }
  return o;
}

/** Brass-ish horn: harmonics of f0, 1/h weights, a low-pass whose cutoff opens on the attack (the blat) then settles; a second voice 7 cents up; tremolo. */
function horn(n: number, f0: number, opts: { attack: number; hold: number; release: number; fcOpen: number; fcSettle: number; trem: number; tremHz: number; vib: number }): Float64Array {
  const o = new Float64Array(n), len = opts.attack + opts.hold + opts.release;
  for (const [det, gain, ph0] of [[1, 1, 0], [1.004, 0.7, 1.3]] as const) {
    const f = f0 * det;
    for (let h = 1; h * f < 9000; h++) {
      const w = TAU * f * h / SR, ph = ph0 * h; let phase = ph;
      for (let i = 0; i < n; i++) {
        const t = i / SR; if (t > len) break;
        const env = t < opts.attack ? smooth(t / opts.attack) : t < opts.attack + opts.hold ? 1 : Math.pow(1 - smooth((t - opts.attack - opts.hold) / opts.release), 1.5);
        const fc = opts.fcSettle + (opts.fcOpen - opts.fcSettle) * Math.exp(-t / 0.09);
        const g = 1 / Math.sqrt(1 + Math.pow(h * f / fc, 4)) / h;
        const trem = 1 - opts.trem * (0.5 - 0.5 * Math.cos(TAU * opts.tremHz * t));
        phase += w * (1 + opts.vib * Math.sin(TAU * 5.2 * t));
        o[i] += gain * g * env * trem * Math.sin(phase);
      }
    }
  }
  return o;
}

/** A feedback echo with a dark tail, for a street's worth of space. */
function echo(x: Float64Array, tailS: number, taps: [number, number][]): Float64Array {
  const o = new Float64Array(x.length + Math.round(tailS * SR)); o.set(x);
  for (const [delayS, gain] of taps) {
    const d = Math.round(delayS * SR), t = lowpass(x, 2200);
    for (let i = 0; i < t.length && i + d < o.length; i++) o[i + d] += t[i] * gain;
  }
  return o;
}

export function ridingStinger(): Float64Array {
  const n = Math.round(1.25 * SR), mix = new Float64Array(n);
  // the turn-in: a rising then falling swish of road noise, 0 to 0.45 s
  const sw = sweepNoise(n, 7, t => 500 + 2600 * smooth(t / 0.28) - 1500 * smooth((t - 0.28) / 0.2), 2.2);
  for (let i = 0; i < n; i++) { const t = i / SR; mix[i] += sw[i] * 0.55 * Math.sin(Math.PI * Math.min(1, t / 0.46)) ** 1.6 * (t < 0.46 ? 1 : 0); }
  // the horn: D3 then B2 (a falling minor third); the second note swells and shakes
  const h1 = horn(n, 146.83, { attack: 0.018, hold: 0.2, release: 0.07, fcOpen: 2600, fcSettle: 900, trem: 0, tremHz: 0, vib: 0 });
  const h2 = horn(n, 123.47, { attack: 0.02, hold: 0.55, release: 0.24, fcOpen: 3000, fcSettle: 1000, trem: 0.18, tremHz: 6.5, vib: 0.0015 });
  const off1 = Math.round(0.12 * SR), off2 = Math.round(0.42 * SR);
  for (let i = 0; i + off1 < n; i++) mix[i + off1] += h1[i] * 0.5;
  for (let i = 0; i + off2 < n; i++) mix[i + off2] += h2[i] * 0.55;
  // a short low thump under the second note, for weight where the speakers can carry it
  for (let i = 0; i < Math.round(0.4 * SR); i++) { const t = i / SR; if (i + off2 < n) mix[i + off2] += 0.35 * Math.sin(TAU * (61.7 + 18 * Math.exp(-t / 0.06)) * t) * Math.exp(-t / 0.12); }
  return echo(mix, 0.35, [[0.17, 0.26], [0.34, 0.12]]);
}

/** Four seconds, every tone an exact multiple of 0.25 Hz and the noise filtered round the loop, so the end meets the start. */
export function turbineLoop(): Float64Array {
  const n = 4 * SR, o = new Float64Array(n);
  const rumble = lowpass(noise(n, 11), 130, true), hiss = bandpass(noise(n, 23), 1200, 3800, true);
  for (let i = 0; i < n; i++) {
    const t = i / SR;
    const lfo = 1 + 0.07 * Math.sin(TAU * 0.5 * t) + 0.03 * Math.sin(TAU * 1.75 * t);
    let whine = 0;
    for (let h = 1; h <= 6; h++) whine += (Math.sin(TAU * 220 * h * t) + 0.55 * Math.sin(TAU * 220.5 * h * t + 0.7)) / Math.pow(h, 1.6);
    o[i] = lfo * (0.34 * whine + 2.0 * rumble[i]) + 0.35 * hiss[i] * (0.8 + 0.2 * Math.sin(TAU * 0.75 * t));
  }
  return o;
}

export const measure = (x: Float64Array) => {
  let pk = 0, ss = 0; for (const v of x) { pk = Math.max(pk, Math.abs(v)); ss += v * v; }
  return { peak: pk, rms: Math.sqrt(ss / x.length) };
};
const db = (v: number) => 20 * Math.log10(Math.max(v, 1e-9));
const normalize = (x: Float64Array, peakDb: number) => { const g = Math.pow(10, peakDb / 20) / measure(x).peak; return x.map(v => v * g); };

export function wav16(x: Float64Array): Buffer {
  const b = Buffer.alloc(44 + x.length * 2);
  b.write('RIFF', 0); b.writeUInt32LE(36 + x.length * 2, 4); b.write('WAVEfmt ', 8); b.writeUInt32LE(16, 16); b.writeUInt16LE(1, 20); b.writeUInt16LE(1, 22);
  b.writeUInt32LE(SR, 24); b.writeUInt32LE(SR * 2, 28); b.writeUInt16LE(2, 32); b.writeUInt16LE(16, 34); b.write('data', 36); b.writeUInt32LE(x.length * 2, 40);
  for (let i = 0; i < x.length; i++) b.writeInt16LE(Math.max(-32768, Math.min(32767, Math.round(x[i] * 32767))), 44 + i * 2);
  return b;
}
const guid = (path: string) => createHash('md5').update('suborned/audio/' + path).digest('hex');

/** The AudioImporter settings: PCM (no compression), mono, kept at 48 kHz, no normalising (the mix is the data's), loaded with the scene. */
export function metaFor(path: string, o: { threeD: boolean }): string {
  return `fileFormatVersion: 2
guid: ${guid(path)}
AudioImporter:
  externalObjects: {}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 48000
    compressionFormat: 0
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {}
  forceToMono: 1
  normalize: 0
  loadInBackground: 0
  ambisonic: 0
  3D: ${o.threeD ? 1 : 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
}

export function build() {
  mkdirSync(audioDir, { recursive: true });
  writeFileSync(audioDir + '.meta', `fileFormatVersion: 2\nguid: ${guid('Resources/Audio')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
  const clips: Record<string, unknown> = {};
  const make = (name: string, x: Float64Array, peakDb: number, o: { loop: boolean; threeD: boolean }) => {
    const y = normalize(x, peakDb), buf = wav16(y), m = measure(y);
    // the loop seam: the jump from the last sample to the first, against the typical step between neighbours (a smooth join is a ratio near 1 or less)
    let step = 0; for (let i = 1; i < y.length; i++) step += Math.abs(y[i] - y[i - 1]); step /= y.length - 1;
    const seam = Math.abs(y[0] - y[y.length - 1]) / step;
    const path = `unity/Assets/Suborned/Resources/Audio/${name}.wav`;
    writeFileSync(join(root, path), buf); writeFileSync(join(root, path + '.meta'), metaFor(path, o));
    clips[name] = { file: `Audio/${name}`, path, sampleRate: SR, channels: 1, bits: 16, seconds: +(y.length / SR).toFixed(3), peakDb: +db(m.peak).toFixed(1), rmsDb: +db(m.rms).toFixed(1),
      loop: o.loop, ...(o.loop ? { seamRatio: +seam.toFixed(2) } : {}), threeD: o.threeD, sha256: createHash('sha256').update(buf).digest('hex'), guid: guid(path) };
  };
  make('rivals_coming', ridingStinger(), -3, { loop: false, threeD: false });
  make('rival_car_engine', turbineLoop(), -9, { loop: true, threeD: true });
  writeFileSync(join(root, 'contracts/audio/clips.json'), JSON.stringify({ schema: 'suborned.audio-clips/1', note: 'Generated by tools/audio/synth.ts (bun tools/audio/cli.ts). Edit the synth, never this file.', clips }, null, 1) + '\n');
  return clips;
}
