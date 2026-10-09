// Chunks the original human GDD and ADD (self-contained HTML) by h2/h3 section.
// Tables become pipe rows so prices, timings and role cards survive as text.
import { hash, type Chunk } from "./corpus";

const decode = (s: string) => s.replace(/&nbsp;/g, " ").replace(/&amp;/g, "&").replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, '"').replace(/&#39;|&rsquo;|&#8217;/g, "’").replace(/&mdash;/g, "—").replace(/&ndash;/g, "–").replace(/&middot;/g, "·").replace(/&rarr;/g, "→").replace(/&times;/g, "×").replace(/&#(\d+);/g, (_, n) => String.fromCodePoint(+n));
const text = (html: string) => decode(html
  .replace(/<(script|style|svg|figure)[^>]*>[\s\S]*?<\/\1>/g, " ")
  .replace(/<\/(td|th)>/g, " | ").replace(/<\/(tr|p|li|div|h\d|dt|dd|blockquote)>|<br\s*\/?>/g, "\n")
  .replace(/<[^>]+>/g, " ")).replace(/[ \t]+/g, " ").replace(/ *\n */g, "\n").replace(/\n{2,}/g, "\n").trim();

export type HtmlSource = { source: string; kind: string; authority: string; version: string; skip?: RegExp };
/** One chunk per h3 (or h2 without h3), titled "h2 › h3"; long sections split at paragraph boundaries. */
export function chunkHtml(meta: HtmlSource, html: string): Chunk[] {
  const sourceHash = hash(html), lineOf = (i: number) => html.slice(0, i).split("\n").length;
  const body = meta.skip ? html.replace(meta.skip, "") : html;
  const heads = [...body.matchAll(/<(h[23])[^>]*>([\s\S]*?)<\/\1>/g)];
  const chunks: Chunk[] = [];
  let h2 = "";
  heads.forEach((m, i) => {
    const title = text(m[2]!);
    if (m[1] === "h2") h2 = title;
    const from = m.index! + m[0].length, to = heads[i + 1]?.index ?? body.length;
    const content = text(body.slice(from, to));
    if (content.length < 40) return; // an h2 that only introduces its h3s
    const section = m[1] === "h2" ? title : `${h2} › ${title}`;
    const start = lineOf(html.indexOf(m[0])), end = lineOf(html.indexOf(m[0]) + (to - m.index!));
    const parts = content.length > 1400 ? content.split("\n").reduce<string[]>((acc, line) => {
      if (!acc.length || acc.at(-1)!.length > 900) acc.push(line); else acc[acc.length - 1] += "\n" + line;
      return acc;
    }, []) : [content];
    parts.forEach((part, j) => chunks.push({
      key: hash(`${meta.source}:${section}:${j}:${part}`).slice(0, 24), source: meta.source, sourceHash, section: parts.length > 1 ? `${section} (${j + 1}/${parts.length})` : section,
      lineStart: start, lineEnd: end, text: `${section}\n${part}`, authority: meta.authority, version: meta.version, kind: meta.kind,
    }));
  });
  return chunks;
}
