import type { Brief, Conversation, Guide, HardIssue } from './types';

const regexEscape = (text: string) => text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
export const wordCount = (text: string) => text.trim().split(/\s+/).filter(t => /[\p{L}\p{N}]/u.test(t)).length;

/** The checks code can make for certain (Keel's side of the review). Voice, story and meaning are the evaluator's. */
export function checkConversation(brief: Brief, c: Conversation, guide: Guide): HardIssue[] {
  const bad: HardIssue[] = [];
  const flag = (rule: HardIssue['rule'], reason: string) => bad.push({ rule, reason });
  const [min, max] = brief.lines;
  if (c.lines.length < min || c.lines.length > max)
    flag('FORM', `${c.lines.length} lines; this ${brief.seconds} s act holds ${min}–${max} bubbles of ${guide.bubble.seconds} s.`);
  c.lines.forEach((l, i) => {
    const n = i + 1, words = wordCount(l.line);
    if (!brief.cast.includes(l.speaker)) flag('FORM', `Line ${n}: ${l.speaker} is not in this scene (${brief.cast.join(', ')}).`);
    if (words > guide.bubble.maxWords) flag('FORM', `Line ${n}: ${words} words; a bubble holds ${guide.bubble.maxWords}.`);
    if (l.line.length > guide.bubble.maxChars) flag('FORM', `Line ${n}: ${l.line.length} characters; a bubble holds ${guide.bubble.maxChars}.`);
    if (/[\r\n<>{}*_]|^\(|\)$/.test(l.line)) flag('FORM', `Line ${n}: one plain spoken line, no stage directions or markup.`);
    if (i > 0 && c.lines[i - 1]!.speaker === l.speaker && i > 1 && c.lines[i - 2]!.speaker === l.speaker)
      flag('FORM', `Lines ${n - 2}–${n}: ${l.speaker} speaks three bubbles running; people answer each other.`);
    for (const term of [...guide.avoid, ...guide.profanity])
      if (new RegExp(`(?<![\\p{L}\\p{N}_])${regexEscape(term)}(?![\\p{L}\\p{N}_])`, 'iu').test(l.line))
        flag(guide.profanity.includes(term) ? 'VOICE' : 'WORLD', `Line ${n}: "${term}" is on the mission's word filter.`);
  });
  for (const who of brief.cast) if (!c.lines.some(l => l.speaker === who) && brief.parts.some(p => p.with.includes(who) || who === 'waitress'))
    flag('REVEAL', `${who} never speaks, but this scene is with them.`);
  return bad;
}
