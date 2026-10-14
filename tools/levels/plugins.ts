// The objectives `bun run levels <objective>` can run, and replay can read (a run names its objective in run.json).
// Missions plug in here (brief 003).
import type { Plugin } from '../gen/plugin';
import { peoplePlugin } from './people/plugin';
import { skyline } from './skyline/plugin';
import { transit } from './transit';

export const plugins: Record<string, Plugin> = { transit, skyline, people: peoplePlugin };
