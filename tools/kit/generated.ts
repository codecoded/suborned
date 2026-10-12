// Tags for pieces the generator built to an order: fixed size (scale 1 only: regenerate rather than stretch), used at the order's site.
import type { KitTags } from '../../contracts/world/kit.schema';

export const generatedTags = (id: string): KitTags => ({ use: id.includes('@') ? ['tower', `site:${id.split('@')[1]}`] : ['landmark:skyport-tower'], scale: [1, 1], squash: 1, wave: 1 });
