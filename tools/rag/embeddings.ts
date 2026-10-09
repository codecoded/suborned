// Local BGE embeddings (services/embeddings); refuses any model or dimension the index was not built with.
import { embeddingModel, embeddingUrl } from './config';

export async function embed(texts: string[], kind: 'query' | 'passage' = 'passage'): Promise<number[][]> {
  const response = await fetch(`${embeddingUrl}/embed`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ texts, kind }), signal: AbortSignal.timeout(120000) });
  if (!response.ok) throw new Error(`Embedding HTTP ${response.status}: ${await response.text()}`);
  const data = (await response.json()) as { model: string; dimensions: number; vectors: number[][] };
  if (data.model !== embeddingModel || data.dimensions !== 384 || data.vectors.length !== texts.length || data.vectors.some(v => v.length !== 384 || v.some(n => !Number.isFinite(n))))
    throw new Error('Embedding model or dimensions do not match the index');
  return data.vectors;
}
