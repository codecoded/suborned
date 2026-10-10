// Which GDD files the corpus and cards were built from: parsed from the files the corpus loads, never hardcoded.
import { sources } from '../rag/corpus';

export type Gdd = { version: string; agent: string; human: string };
export const versionOf = (file: string) => file.match(/v\d+\.\d+\.\d+/)?.[0] ?? 'unversioned';
export const currentGdd = (): Gdd => { const agent = sources.agentGdd(), human = sources.humanGdd(); return { version: versionOf(agent), agent, human }; };

/** Runs recorded before `gdd` existed: the GDD path is in the source of every retrieved passage. */
export function inferGdd(hitSources: Iterable<string>): Gdd | undefined {
  const s = [...new Set(hitSources)], agent = s.find(x => /^GDD\/.*agent-gdd.*\.md$/.test(x)), human = s.find(x => /^GDD\/.*\.html$/.test(x));
  return agent ? { version: versionOf(agent), agent, human: human ?? '' } : undefined;
}
