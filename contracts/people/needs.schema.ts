// suborned.needs.rules/1 and suborned.needs/1: open needs (brief 017, Motive's design, Sky 2026-10-18 "I agree with your views").
// A need is a kind (a short closed list of mechanics: which approaches act on it, what it requires, how Help pays) and a record (the open part:
// "Bureau loan", "Clearance order"). Rules: contracts/people/needs.rules.json. Catalogue: contracts/people/<plan id>.needs.json, written by
// the model at build time (phase 2), checked by code (tools/people/needs.ts, N-1..N-8), promoted with Sky's yes. Phase 1 (this file): structure
// only, every number unchanged; sizes are all `standard`, holders unset.
//
// Why a sidecar to people.rules.json and not a section of it: the C# engine reads people.rules.json strictly (Rules.cs `Only(...)` at the
// root and in `needs`) and hashes its text into every replay's start hash, so a new key there would stop the engine and move every
// recorded run. The kinds below repeat `needs.list` (minus `secret`) field for field; N-1 holds them equal. When Keel's vocabulary change lands
// (briefs/017 phase 3), `kinds` moves into people.rules.json and `needs.list` goes.
import { z } from 'zod';
import { APPROACHES, NEED_REQUIRES } from './people.rules.schema';

const id = z.string().regex(/^[a-z][a-z0-9-]*$/);
const text = z.string().min(1);
const approach = z.enum(APPROACHES);

export const BINDINGS = ['trade', 'role', 'tie'] as const; // a holder entry: the keeper of a trade's unit, a person in a civic role, the person at the other end of a tie
export const SECRET_REQUIRES = ['nothing', 'lover-tie'] as const;

const grounded = { src: text, proposed: z.literal(true).optional(), reason: text.optional() };
const ground = <T extends z.ZodRawShape>(shape: T) => z.strictObject({ ...shape, ...grounded }).refine((o) => !(o as { proposed?: true }).proposed || !!(o as { reason?: string }).reason, { message: 'a proposed rule needs a reason' });

const NeedKind = ground({
  metBy: z.array(approach).min(1), help: z.enum(['cash', 'favour']), requires: z.enum(NEED_REQUIRES),
  holders: z.array(z.string().regex(/^(trade|role|tie):[a-z][a-z0-9-]*$/)).optional().describe('the binding entries a record of this kind may name as holder (phase 2); absent: only `beyond`'),
});
const SecretKind = ground({
  requires: z.enum(SECRET_REQUIRES), learnedBy: z.array(z.string().regex(/^[a-z]+\.[a-z-]+$/)).min(1),
});

export const NeedsRules = z.strictObject({
  schema: z.literal('suborned.needs.rules/1'),
  author: text, gdd: text, rule: text, src: text,
  kinds: z.record(id, NeedKind).refine((k) => Object.keys(k).length > 0, { message: 'no need kinds' }),
  secretKinds: z.record(id, SecretKind),
  sizes: z.record(id, z.union([z.number().int(), z.string().regex(/^v:[a-z][a-z0-9-]*$/)])).describe('a size sets Help\'s price line: a number is usd added to the approach base; "v:<id>" is a rulebook value, which must exist before a record may use the size (N-2)'),
  bindings: z.array(z.enum(BINDINGS)).min(1),
  label: ground({
    maxWords: z.number().int().positive(), maxChars: z.number().int().positive(),
    era: z.array(text), gendered: z.array(text),
  }),
  tone: ground({
    never: z.array(text).describe('words no label may carry (GDD §06 content line: civilians are never the payload; Fable\'s tone filters)'),
    children: z.array(text).describe('words that name a child: a child is never a subject or a label'),
    neverPressable: z.array(id).describe('need kinds whose records Pressure never acts on (care, grief)'),
  }),
  sameAs: z.strictObject({ file: text, list: z.literal('needs.list'), minus: z.array(id) }).describe('the engine\'s own table the kinds are held to: people.rules.json needs.list less the retired kinds'),
}).superRefine((r, ctx) => {
  const bad = (path: (string | number)[], message: string) => ctx.addIssue({ code: 'custom', path, message });
  if (!('standard' in r.sizes)) bad(['sizes'], 'a size `standard` (no price line) must exist');
  else if (r.sizes.standard !== 0) bad(['sizes', 'standard'], 'standard adds nothing to Help\'s price');
  for (const [k, v] of Object.entries(r.kinds)) if (!v.metBy.includes('help') && v.help === 'cash') bad(['kinds', k, 'help'], 'help is priced but not an approach for this kind');
  for (const k of r.tone.neverPressable) if (!r.kinds[k]) bad(['tone', 'neverPressable'], `${k} is not a kind`);
});
export type NeedsRules = z.infer<typeof NeedsRules>;

const holder = z.string().regex(/^(beyond|(trade|role|tie):[a-z][a-z0-9-]*)$/).describe('"beyond": nobody on the sheet holds it; else a binding entry. Unset in phase 1 (read as beyond)');
const common = {
  id, kind: id, label: z.string().min(1).describe('1-2 words, shown in Intel after Listen (the kind\'s word while only told)'), area: id.describe('the cast area whose rows draw this record'),
  holder: holder.optional(), subject: z.string().regex(/^(tie):[a-z][a-z0-9-]*$/).optional().describe('the person the need is about, bound to a tie (never a child)'),
  metBy: z.array(approach).min(1).optional().describe('narrows the kind\'s approaches, never widens them (N-5)'), src: text,
};
export const NeedRecord = z.strictObject({ ...common, size: id });
export const SecretRecord = z.strictObject({ ...common });
export const Catalogue = z.strictObject({
  schema: z.literal('suborned.needs/1'), plan: id, scope: id, author: text,
  needs: z.array(NeedRecord).min(1), secrets: z.array(SecretRecord).min(1),
});
export type NeedRecord = z.infer<typeof NeedRecord>;
export type SecretRecord = z.infer<typeof SecretRecord>;
export type Catalogue = z.infer<typeof Catalogue>;

export const parseNeedsRules = (t: string): NeedsRules => NeedsRules.parse(JSON.parse(t));
export const parseCatalogue = (t: string): Catalogue => Catalogue.parse(JSON.parse(t));
