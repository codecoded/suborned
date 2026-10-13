// Tags for pieces the generator built to an order: fixed size (scale 1 only: regenerate rather than stretch), used at the order's site.
import type { KitTags } from '../../contracts/world/kit.schema';

/** `Name@site` = a tower for that plan site; `SpaceHQ_Portal` the signature; else the Skyport Tower's portal variant. */
export const generatedTags = (id: string): KitTags => ({ use: id.includes('@') ? ['tower', `site:${id.split('@')[1]}`] : id === 'SpaceHQ_Portal' ? ['landmark:space-hq'] : ['landmark:skyport-tower'], scale: [1, 1], squash: 1, wave: 1 });
