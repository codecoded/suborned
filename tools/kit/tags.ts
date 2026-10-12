// Generator tags per asset: what it is for, its scale range, and (wave 2) form and trade. Wave 1 = first adopted set (brief 006 A).
import type { KitReach, KitTags } from '../../contracts/world/kit.schema';

export const WAVE1 = new Set(['SphereStation', 'Skyport_Tower', 'CircusMonument', 'ParabolicArch', 'SkyPad_Roof', 'Streetlamp', 'Bollard', 'Bench', 'Hydrant', 'RecyclingBins', 'PhoneBooth', 'TicketMachine', 'PlazaSphereLamp', 'CCTVPole', 'DomeCamera', 'CoconutPalm', 'FanPalm', 'Cypress', 'BladeSign_Cafe', 'BladeSign_Hotel', 'BladeSign_Cinema', 'NeonStar_Wall', 'AdColumn']);

const USE: Record<string, string[]> = {
  Streetlamp: ['lamp'], PlazaSphereLamp: ['lamp'], Bollard: ['bollard'], Bench: ['bench'], Hydrant: ['hydrant'], RecyclingBins: ['bin'], PhoneBooth: ['phone-booth'], TicketMachine: ['ticket-machine'],
  CCTVPole: ['pole-camera'], DomeCamera: ['canopy-camera'], CoconutPalm: ['tree', 'tub-palm'], FanPalm: ['tub-palm'], Cypress: ['tree'], AdColumn: ['advertising-column'],
  BladeSign_Cafe: ['blade-sign'], BladeSign_Hotel: ['blade-sign'], BladeSign_Cinema: ['blade-sign'], NeonStar_Wall: ['wall-sign'],
  SphereStation: ['landmark:sphere'], SkyPad_Roof: ['landmark:skyport'], CircusMonument: ['landmark:monument'],
};
const TRADE: Record<string, string[]> = {
  BladeSign_Cafe: ['diner', 'soda-fountain', 'automat', 'grocer', 'tiki-bar', 'cocktail-lounge'], BladeSign_Hotel: ['hotel-lobby', 'travel-bureau'],
  BladeSign_Cinema: ['cinema', 'dance-hall', 'jazz-club', 'bowling'],
  NeonStar_Wall: ['diner', 'tiki-bar', 'cocktail-lounge', 'jazz-club', 'dance-hall', 'bowling', 'record-shop', 'cinema', 'soda-fountain', 'hotel-lobby'],
};
// [min, max] uniform scale on height; palms may be squeezed to `squash` of their height scale (tall slim crowns: Washingtonia).
const SCALE: Record<string, [number, number]> = {
  Streetlamp: [0.7, 1.6], PlazaSphereLamp: [0.7, 1.6], Bollard: [0.8, 1.4], Bench: [0.9, 1.25], Hydrant: [0.9, 1.3], RecyclingBins: [0.8, 1.3], PhoneBooth: [0.9, 1.1], TicketMachine: [0.9, 1.15],
  CCTVPole: [0.8, 1.8], DomeCamera: [0.6, 1.2], CoconutPalm: [0.5, 2.2], FanPalm: [0.5, 2.2], Cypress: [0.6, 2.2], AdColumn: [0.9, 1.2],
  BladeSign_Cafe: [0.5, 1], BladeSign_Hotel: [0.5, 1], BladeSign_Cinema: [0.5, 1], NeonStar_Wall: [0.4, 1.2],
  SphereStation: [0.4, 1.5], Skyport_Tower: [0.4, 1.5], CircusMonument: [0.4, 1.5],
};
const SQUASH: Record<string, number> = { Streetlamp: 0.7, CCTVPole: 0.5 }; // trees and palms scale uniformly (squash 1)
/** Where the working end reaches from the pivot: the streetlamp's pole is at the pivot, its head 1.25 m out along local -X. */
const REACH: Record<string, KitReach> = { Streetlamp: { axis: '-x', m: 1.25 } };
const USE_BY_CATEGORY: Record<string, string> = { Street: 'street', Signals: 'street', Booths: 'street', Furnish: 'street', Plants: 'tree', Rooftop: 'roof', Signage: 'sign', Facades: 'facade', Skyline: 'skyline', Highrise: 'tower', Landmarks: 'landmark', Skyport: 'landmark', Ground: 'ground', Alleys: 'alley', Plaza: 'plaza', Structures: 'structure', Sky: 'sky', Vehicles: 'vehicle', Robots: 'cast', Calibration: 'calibration' };

export function tagsOf(name: string, category: string): KitTags {
  const form = /Corner/.test(name) ? 'corner' : /End/.test(name) ? 'end' : /Bay/.test(name) ? 'bay' : undefined;
  return {
    use: USE[name] ?? [USE_BY_CATEGORY[category] ?? category.toLowerCase()],
    ...(TRADE[name] ? { trade: TRADE[name] } : {}), ...(form && ['Facades', 'Skyline'].includes(category) ? { form } : {}),
    ...(REACH[name] ? { reach: REACH[name] } : {}), scale: SCALE[name] ?? [0.9, 1.1], squash: SQUASH[name] ?? 1, wave: WAVE1.has(name) ? 1 : 2,
  };
}
