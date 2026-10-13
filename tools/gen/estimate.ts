// A rough price for a call, before it is made. The rates are what experiment D's usage records imply (opus, 1 h
// cache): inferred, not published figures, which is why a preview prints them. Output length is an assumption per
// kind of member; the real figures come from the tokens that run.json records.
export const MODEL_PRICE = { input: 4, cacheWrite: 8, cacheRead: 0.4, output: 20 }; // dollars per million tokens
export const CHARS_PER_TOKEN = 3.2; // JSON-heavy text; D: a 7.6 KB prompt plus schema came to about 3.4 thousand tokens
export const IMAGE_TOKENS = 3300; // per contact sheet, from D (two sheets came to about 6.6 thousand)
export const OUTPUT_TOKENS = { builder: 8000, critic: 3000 };

export type Estimate = { chars: number; tokensIn: number; tokensOut: number; usd: number };

/** `chars`: everything sent as text (prompt, system card, output schema). Round 1 is a cold cache: input is written. */
export function estimateCost(o: { chars: number; images: number; builder: boolean }): Estimate {
  const tokensIn = Math.round(o.chars / CHARS_PER_TOKEN + o.images * IMAGE_TOKENS);
  const tokensOut = o.builder ? OUTPUT_TOKENS.builder : OUTPUT_TOKENS.critic;
  const usd = (tokensIn * MODEL_PRICE.cacheWrite + tokensOut * MODEL_PRICE.output) / 1e6;
  return { chars: o.chars, tokensIn, tokensOut, usd };
}
