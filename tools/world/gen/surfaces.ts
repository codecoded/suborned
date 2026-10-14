// The look keys of large parts. `trim` is brass (14 Oct: "brass is hairline, frame and bead only"), so a part that is not a hairline, frame or bead
// draws its own key and its colour is one look.json row (SF-21 holds the line). Latch maps each key; today every row can equal `trim`.
// Each key is named for the kind of part, not for a colour: Vela folds kinds onto one role in look.json where they are one material.

/** The Parade mall canopy slab (121.4 x 2.5 x 0.3 m; plan megastructures[0].canopy, "continuous chrome canopy over the mall frontage zone"). */
export const CANOPY = 'canopy';
/** The large chrome plates of a dressed connector (dress.ts): a stair tower's band slabs, cap and coping (7.3 / 7.4 / 6.2 m stadia), a lift's cap and dome (2.8-3.4 m). */
export const PLATE = 'plate';
/** A phone booth's body (street item `phone-booth`, 1 x 1 x 2.5 m; the roof cap above it is `enamel` in kit.json). */
export const BOOTH = 'booth';
/** A palm tub's base (street item `tub-palm`, 1.5 x 1.5 x 4.5 m with its palm; kit.json draws the tub from the item surface, the trunk `column`, the crown `park`). */
export const TUB = 'tub';
/** The market-deck gantry cranes' masts (2.4 x 2.4 x 32.6 m) and booms (40 x 1.6 x 1.6 m); the cab is `enamel` (harbourDeck.ts "gantry cranes on the crane walk"). */
export const CRANE = 'crane';
/** The Skyport Tower's structural members (pier.ts): the cross-girder carrying the stem (6 x 34 x 4 m), the disc arms ("cantilever from the stem"), the crown's lattice mast, the tubes' spines ("a chrome spine ties the tube to the stem"). */
export const GIRDER = 'girder';
/** A hoop round a lift's glass tube (dress.ts, "the kit's lift_tube: hoops every 5 m", "chrome hoops"): 2.6 / 3.2 m across, 0.4 m high, as wide as a lift dome, so only its key tells it from a cap. */
export const HOOP = 'hoop';
/** A crossing's light (brief 009): the flush light dot rows (or bars) along the walk, kerb to kerb, and the halo disc under a signal orb. Emissive seafoam in a brass frame line: the glow is its key, the frame is a hairline of the piece. */
export const GLOW = 'glow';
/** A crossing's seafoam inlay arc in the paving where it lands (brief 009): a band 0.8 m wide, so a colour of its own, not brass. */
export const INLAY = 'inlay';
/** A signal orb's body (brief 009): chrome, 0.84 m across. Its displays are the piece's emissive materials (walk and stop pictograms); this key is the stand-in's body. */
export const ORB = 'orb';

/** Every key above: the surfaces the export carries that are not in look.json until Latch adds them. */
export const OWN_KEYS = [CANOPY, PLATE, BOOTH, TUB, CRANE, GIRDER, HOOP, GLOW, INLAY, ORB] as const;
