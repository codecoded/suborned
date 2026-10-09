// Test-only retriever: term-overlap ranking over the live corpus, so the pipeline runs with no index and no Docker.
import { hash, loadCorpus } from '../../rag/corpus';
import { terms, type Hit } from '../../rag/retrieval';

export const lexical: typeof import('../../rag/retrieval').retrieve = async (query, limit = 8, pinned = []) => {
  const corpus = await loadCorpus(), queries = [query].flat();
  const hits: Hit[] = corpus.map(c => ({ ...c, score: queries.reduce((s, q) => s + terms(q).filter(t => c.text.toLowerCase().includes(t)).length, 0), signals: ['lexical'] }))
    .sort((a, b) => b.score - a.score || a.key.localeCompare(b.key)).slice(0, limit);
  for (const c of corpus) if (pinned.includes(c.key) && !hits.some(h => h.key === c.key)) hits.push({ ...c, score: 0, signals: ['critic-cited'] });
  return { query: queries.join(' ‖ '), queries, pinned: [...new Set(pinned)], method: 'lexical term overlap (test only)', corpusHash: hash(JSON.stringify(corpus)), embeddingModel: 'none', hits };
};
