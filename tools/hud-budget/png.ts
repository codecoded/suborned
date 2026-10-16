// Minimal PNG reader/writer for the HUD budget check (brief 014 A, Glyph). 8-bit, non-interlaced, grey / grey+alpha / RGB / RGBA / palette in; RGBA out.
// No dependencies: node:zlib only. Enough for Unity's screenshots and for the test fixtures; anything else throws.
import { deflateSync, inflateSync } from 'node:zlib';

export interface Image { w: number; h: number; data: Uint8Array } // RGBA, row-major

const SIG = [137, 80, 78, 71, 13, 10, 26, 10];
const crcTable = (() => { const t = new Uint32Array(256); for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; t[n] = c >>> 0; } return t; })();
const crc32 = (b: Uint8Array): number => { let c = 0xffffffff; for (const x of b) c = crcTable[(c ^ x) & 255]! ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
const u32 = (b: Uint8Array, o: number) => ((b[o]! << 24) | (b[o + 1]! << 16) | (b[o + 2]! << 8) | b[o + 3]!) >>> 0;

export function decodePng(buf: Uint8Array): Image {
  for (let i = 0; i < 8; i++) if (buf[i] !== SIG[i]) throw new Error('not a PNG');
  let o = 8, w = 0, h = 0, depth = 0, ctype = 0, interlace = 0, plte: Uint8Array | undefined, trns: Uint8Array | undefined;
  const idat: Uint8Array[] = [];
  while (o < buf.length) {
    const len = u32(buf, o), type = String.fromCharCode(...buf.slice(o + 4, o + 8)), body = buf.subarray(o + 8, o + 8 + len);
    if (type === 'IHDR') { w = u32(body, 0); h = u32(body, 4); depth = body[8]!; ctype = body[9]!; interlace = body[12]!; }
    else if (type === 'PLTE') plte = body; else if (type === 'tRNS') trns = body; else if (type === 'IDAT') idat.push(body); else if (type === 'IEND') break;
    o += 12 + len;
  }
  if (depth !== 8 || interlace !== 0) throw new Error(`unsupported PNG (depth ${depth}, interlace ${interlace})`);
  const ch = ({ 0: 1, 2: 3, 3: 1, 4: 2, 6: 4 } as Record<number, number>)[ctype];
  if (!ch) throw new Error(`unsupported PNG colour type ${ctype}`);
  const raw = inflateSync(Buffer.concat(idat)), stride = w * ch, out = new Uint8Array(w * h * 4), prev = new Uint8Array(stride), cur = new Uint8Array(stride);
  for (let y = 0; y < h; y++) {
    const f = raw[y * (stride + 1)]!, src = raw.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1));
    for (let i = 0; i < stride; i++) {
      const a = i >= ch ? cur[i - ch]! : 0, b = prev[i]!, c = i >= ch ? prev[i - ch]! : 0, x = src[i]!;
      let v: number;
      if (f === 0) v = x; else if (f === 1) v = x + a; else if (f === 2) v = x + b; else if (f === 3) v = x + ((a + b) >> 1);
      else { const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c); v = x + (pa <= pb && pa <= pc ? a : pb <= pc ? b : c); }
      cur[i] = v & 255;
    }
    for (let x = 0; x < w; x++) {
      const d = (y * w + x) * 4, s = x * ch;
      if (ctype === 6) { out[d] = cur[s]!; out[d + 1] = cur[s + 1]!; out[d + 2] = cur[s + 2]!; out[d + 3] = cur[s + 3]!; }
      else if (ctype === 2) { out[d] = cur[s]!; out[d + 1] = cur[s + 1]!; out[d + 2] = cur[s + 2]!; out[d + 3] = 255; }
      else if (ctype === 0) { out[d] = out[d + 1] = out[d + 2] = cur[s]!; out[d + 3] = 255; }
      else if (ctype === 4) { out[d] = out[d + 1] = out[d + 2] = cur[s]!; out[d + 3] = cur[s + 1]!; }
      else { const p = cur[s]!; out[d] = plte![p * 3]!; out[d + 1] = plte![p * 3 + 1]!; out[d + 2] = plte![p * 3 + 2]!; out[d + 3] = trns && p < trns.length ? trns[p]! : 255; }
    }
    prev.set(cur);
  }
  return { w, h, data: out };
}

export function encodePng(im: Image): Uint8Array {
  const { w, h, data } = im, raw = new Uint8Array(h * (w * 4 + 1));
  for (let y = 0; y < h; y++) { raw[y * (w * 4 + 1)] = 0; raw.set(data.subarray(y * w * 4, (y + 1) * w * 4), y * (w * 4 + 1) + 1); }
  const chunk = (type: string, body: Uint8Array): Uint8Array => {
    const o = new Uint8Array(12 + body.length), dv = new DataView(o.buffer);
    dv.setUint32(0, body.length); for (let i = 0; i < 4; i++) o[4 + i] = type.charCodeAt(i); o.set(body, 8);
    dv.setUint32(8 + body.length, crc32(o.subarray(4, 8 + body.length))); return o;
  };
  const ihdr = new Uint8Array(13), dv = new DataView(ihdr.buffer); dv.setUint32(0, w); dv.setUint32(4, h); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([Buffer.from(SIG), chunk('IHDR', ihdr), chunk('IDAT', deflateSync(raw)), chunk('IEND', new Uint8Array(0))]);
}
