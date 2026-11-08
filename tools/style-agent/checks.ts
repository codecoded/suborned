import { questionCount, sentenceCount } from '../../contracts/missions/handover/conversations.schema.ts';
import { FORETELLS } from './short-natural';
import { guideVersion, type Brief, type Conversation, type Guide, type HardIssue } from './types';

const regexEscape = (text: string) => text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
export const wordCount = (text: string) => text.trim().split(/\s+/).filter(t => /[\p{L}\p{N}]/u.test(t)).length;

/** The checks code can make for certain (Keel's side of the review). Voice, story and meaning are the evaluator's. */
export function checkConversation(brief: Brief, c: Conversation, guide: Guide): HardIssue[] {
  const bad: HardIssue[] = [];
  const flag = (rule: HardIssue['rule'], reason: string) => bad.push({ rule, reason });
  const [min, max] = brief.lines, cut = guide.cutaway; // guide /3: a cutaway conversation (brief 096); /2: speech bubbles
  if (c.lines.length < min || c.lines.length > max)
    flag('FORM', cut ? `${c.lines.length} lines; a cutaway conversation holds ${min}–${max}.` : `${c.lines.length} lines; this ${brief.seconds} s act holds ${min}–${max} bubbles of ${guide.bubble.seconds} s.`);
  const most = cut ?? guide.bubble, holder = cut ? 'a cutaway line' : 'a bubble';
  c.lines.forEach((l, i) => {
    const n = i + 1, words = wordCount(l.line);
    if (!brief.cast.includes(l.speaker)) flag('FORM', `Line ${n}: ${l.speaker} is not in this scene (${brief.cast.join(', ')}).`);
    if (words > most.maxWords) flag('FORM', `Line ${n}: ${words} words; ${holder} holds ${most.maxWords}.`);
    if (l.line.length > most.maxChars) flag('FORM', `Line ${n}: ${l.line.length} characters; ${holder} holds ${most.maxChars}.`);
    if (cut && sentenceCount(l.line) > cut.maxSentences) flag('NATURAL', `Line ${n}: ${sentenceCount(l.line)} sentences; one or two full sentences a line.`);
    if (cut && questionCount(l.line) > 1) flag('NATURAL', `Line ${n}: ${questionCount(l.line)} questions; one thing at a time.`);
    const told = guideVersion(guide) >= 4 ? l.line.match(FORETELLS) : null; // guide /4 (brief 102): the family Sky named, in a conversation too
    if (told) flag('NATURAL', `Line ${n}: "${told[0]}" explains the game (brief 102); say what this person would say now.`);
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
