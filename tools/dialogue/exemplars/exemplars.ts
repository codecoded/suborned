// Brief 085: the classic conversations a dialogue writer studies for their moves. Verbatim excerpts from public domain texts, each with notes on what a writer can do; never lines to reuse.
// The per-job JSON files and speech-rules.json hold the content; verify.ts proves each excerpt against the file it was fetched into. Outside the RAG corpus: tools/rag/corpus.ts reads no tools/ path.
import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { z } from 'zod';

export const JOBS = ['overheard-at-work', 'pressure', 'debt', 'bargain', 'dodge', 'lie-to-loved', 'yes-or-no'] as const;
export type Job = (typeof JOBS)[number];

export const Move = z.strictObject({ at: z.string().min(1), move: z.string().min(1) });
export const Excerpt = z.strictObject({ id: z.string(), source: z.string(), where: z.string(), text: z.string().min(1), moves: z.array(Move).min(1) });
export const JobFile = z.strictObject({ job: z.enum(JOBS), what: z.string(), excerpts: z.array(Excerpt).min(2).max(5) });
export const Source = z.strictObject({
  slug: z.string(), work: z.string(), author: z.string(), translator: z.string().nullable(), published: z.number().int(), translationPublished: z.number().int().nullable(),
  url: z.string(), format: z.enum(['text', 'html']), datesFrom: z.string(),
});
export const Rules = z.strictObject({ speaker: z.string(), noticesFirst: z.string(), refusesToSay: z.string(), talksAbout: z.string(), dodges: z.string(), length: z.string(), pays: z.string().optional(), movesFrom: z.array(z.string()).min(1) });
export const SpeechRules = z.strictObject({ note: z.string(), speakers: z.array(Rules) });
export type Excerpt = z.infer<typeof Excerpt>; export type JobFile = z.infer<typeof JobFile>; export type Source = z.infer<typeof Source>; export type Rules = z.infer<typeof Rules>;

const here = import.meta.dir;
const json = (f: string): unknown => JSON.parse(readFileSync(join(here, f), 'utf8'));
export const loadJobs = (): JobFile[] => JOBS.map(j => JobFile.parse(json(`${j}.json`)));
export const loadSources = (): Source[] => z.array(Source).parse(json('sources.json'));
export const loadRules = (): z.infer<typeof SpeechRules> => SpeechRules.parse(json('speech-rules.json'));
export const exemplarFiles = () => readdirSync(here);

/** Public domain in the US on the day of this study, 2026-10-31: published 1930 or earlier, the translation too. */
export const LAST_PD_YEAR = 1930;

/** An HTML source as readable text: tags off, the common entities decoded. Standard Ebooks' single page only. */
export const stripHtml = (h: string) => h.replace(/<\/(p|h\d|section|blockquote|li)>/g, '\n').replace(/<[^>]+>/g, '').replace(/&amp;/g, '&').replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&#160;|&nbsp;/g, ' ').replace(/\n{2,}/g, '\n\n');
/** What "verbatim" allows for: line wraps and runs of space, Gutenberg's `_` italic marks, Standard Ebooks' word joiners. Every word and every mark of punctuation must match. */
export const normalise = (s: string) => s.replace(/_/g, '').replace(/⁠/g, '').replace(/\s+/g, ' ').trim();
/** Is the excerpt in the source, word for word? */
export const inSource = (excerpt: string, source: string) => normalise(source).includes(normalise(excerpt));

/** The words of a line for comparing: lower case, straight apostrophes, punctuation gone. */
export const tokens = (s: string) => s.toLowerCase().replace(/[‘’]/g, "'").replace(/[^a-z0-9' ]+/g, ' ').replace(/(^| )'+|'+( |$)/g, ' ').trim().split(/\s+/).filter(Boolean);
/** Every run of `n` consecutive words in the text, as a set. */
export const grams = (s: string, n = 5) => { const t = tokens(s), out = new Set<string>(); for (let i = 0; i + n <= t.length; i++) out.add(t.slice(i, i + n).join(' ')); return out; };
/** The runs of `n` or more consecutive words a scene shares with any excerpt, reading the beats in order so a run across two bubbles counts too. Empty = nothing lifted. */
export function lifted(lines: string[], excerpts: string[], n = 5): string[] {
  const known = new Set(excerpts.flatMap(e => [...grams(e, n)]));
  return [...grams(lines.join(' '), n)].filter(g => known.has(g));
}
