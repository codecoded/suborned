// suborned.oldbuilt/1: the old-built scene's geometry, exported by Latch's swap (brief 075 remap step 2) to briefs/075/oldbuild/built.json and read by the
// plan-match check (tools/world/oldmatch, `bun run oldbuild:match`, Splice). Sky 2026-10-29: "we build a world with the old code, and then we map and take it from
// there": the old builders build the look; the plan stays the source for missions; this file is how the two are held together. Agreed in briefs/075/notes.md
// under "Plan match".
//
// Frame: every point is in the PLAN frame (x east, y north, metres), after `frame.offset`: plan (x, y) = (unity.x + offset[0], unity.z + offset[1]). The old town
// built at the old DecoKit coordinates has offset [0, -206] (plan.frame: plan (x, y) = old Unity (x, z - 206)); a scene already in our frame has [0, 0]. Heights
// (`base`, `height`, `z`, `top`, `level`) are Unity y, metres, unshifted. Rings are closed implicitly (last point != first), either winding.
//
// What goes in: the whole scene as built, the Handover zone included (our civic block, café and Records Office as kept by the swap), so the check can prove the
// cut-out is clean. Round to 1 mm. Arrays may be empty; a missing optional array means "not exported" and that part of the check is skipped with a note.
export type V = [number, number];

export type OldBuilt = {
  schema: 'suborned.oldbuilt/1';
  source: {
    scene: string; // e.g. Assets/Scenes/OldTown.unity
    builder: string; // the entry point that built it, e.g. Suborned.OldTown.Editor.DecoKit.DecoKitMenu.Street
    commit?: string; // git HEAD the scene was built at
    heights?: string; // the height rule in force (brief 075: flagships at old height, domestic ~3 storeys)
    at?: string; // ISO time of export
  };
  frame: { units: 'm'; offset: V };
  /** The walk levels the scene was built to (plan standards.levels.list: street 0 / walk 0.18, deck 9.43, concourse 24). Must match the plan exactly. */
  levels: { id: string; z: number; walk?: number }[];
  /** One entry per building mass footprint at its foot (the ground storey's outline, plinth included, canopies and eyebrows excluded). A building made of
   *  several footprints (wings, a tower on a podium standing on the street) repeats `building`. kind: what it is (flagship/landmark stay tall, Sky 2026-10-29);
   *  origin: `old` built by the old builders, `ours` kept from our pipeline (the civic block, the Skyport, stations). */
  buildings: {
    id: string; building?: string; ring: V[]; base: number; height: number;
    kind: 'domestic' | 'commercial' | 'flagship' | 'landmark' | 'civic' | 'megastructure' | 'signature' | 'station' | 'other';
    origin: 'old' | 'ours'; storeys?: number; piece?: string;
  }[];
  /** Every door a person can use at street or deck level: the shopfront/lobby door's centre on the wall line, z = the floor it opens onto. normal = outward. */
  entrances: { id: string; building: string; at: V; z: number; normal?: V; w?: number; kind?: 'lobby' | 'shop' | 'service' | 'public' | 'official' | 'station' | 'other' }[];
  /** Carriageways: centreline (dense enough that a 0.5 m kerb check holds: a vertex every <= 2 m on curves), kerb-to-kerb width, carriageway z. */
  roads: { id: string; line: V[]; width: number; level: number }[];
  /** Roundabouts: the carriageway's outer kerb radius and the island's kerb radius. */
  circuses?: { id: string; centre: V; radius: number; islandRadius: number; level: number }[];
  /** The kerb lines as built (the kerb stone's face on the carriageway side), rounded returns included. Optional: without them the kerbs are derived from roads. */
  kerbs?: { id: string; line: V[]; top: number; road?: string }[];
  /** The raised walking surfaces at their top: pavement slabs, footways, squares and plazas (not lawns or water). Under buildings may be included or not. */
  pavements: { id: string; ring: V[]; top: number }[];
  /** Raised walkways at deck and concourse level (our network, kept by the swap). z = walk top. */
  decks?: { id: string; ring: V[]; z: number; level: string }[];
};
