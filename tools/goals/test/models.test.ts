import { expect, test } from 'bun:test';
import type { retrieve } from '../../rag/retrieval';
import { LimitReached, pinnedRetriever, remainingBudget, validateLimits } from '../models';

test('Codex requires recorded approval, an explicit model and honest subscription usage bounds', () => {
  const limits = { provider: 'codex' as const, model: 'gpt-6-astra', maxTokens: 500000, maxCalls: 20, maxIterations: 3 };
  expect(() => validateLimits(limits, 'Sky')).not.toThrow();
  expect(() => validateLimits(limits, '')).toThrow('authorized');
  expect(() => validateLimits({ ...limits, model: undefined }, 'Sky')).toThrow('model');
  expect(() => validateLimits({ ...limits, maxUsd: 10 }, 'Sky')).toThrow('dollar cost');
  for (const maxTokens of [NaN, Infinity, 0, 2000001]) expect(() => validateLimits({ ...limits, maxTokens }, 'Sky')).toThrow('max-tokens');
  expect(() => validateLimits({ ...limits, goal: ' ' }, 'Sky')).toThrow('goal');
});

test('unknown, negative and non-finite prior costs stop retries instead of increasing the budget', () => {
  for (const costUsd of [null, -0.01, NaN, Infinity, -Infinity])
    expect(() => remainingBudget([{ costUsd }], { maxUsd: 10, maxCalls: 4 })).toThrow(LimitReached);
  expect(remainingBudget([{ costUsd: 1.25 }, { costUsd: 0.25 }], { maxUsd: 10, maxCalls: 4 })).toBe(8.5);
});

test('spent budgets and call limits prevent another invocation', () => {
  expect(() => remainingBudget([{ costUsd: 10 }], { maxUsd: 10, maxCalls: 4 })).toThrow('spend ceiling');
  expect(() => remainingBudget([{ costUsd: 11 }], { maxUsd: 10, maxCalls: 4 })).toThrow('spend ceiling');
  expect(() => remainingBudget([{ costUsd: 0 }], { maxUsd: 10, maxCalls: 1 })).toThrow('call limit');
});

test('retrieval requires bootstrap and always passes its pinned hash', async () => {
  let current: string | undefined;
  const calls: unknown[][] = [];
  const retriever: typeof retrieve = async (...args) => {
    calls.push(args);
    return { query: 'test', queries: ['test'], pinned: [], method: 'fixture', corpusHash: args[3]!, embeddingModel: 'fixture', hits: [] };
  };
  const retrievePinned = pinnedRetriever(() => current, retriever);
  await expect(retrievePinned('test')).rejects.toThrow('bootstrap');
  expect(calls).toHaveLength(0);
  current = 'a'.repeat(64);
  await retrievePinned('test', 5, ['passage'], 'b'.repeat(64));
  expect(calls[0]).toEqual(['test', 5, ['passage'], current]);
  await retrievePinned('second');
  expect(calls[1]?.[3]).toBe(current);
  current = 'c'.repeat(64);
  await expect(retrievePinned('third')).rejects.toThrow('corpus changed');
  expect(calls).toHaveLength(2);
});
