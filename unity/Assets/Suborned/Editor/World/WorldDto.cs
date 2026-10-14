using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    // C# side of the world export. Plan frame: x east, y north, metres; Unity = (x, height, y).
    // Splice owns the real schema (contracts/world/); these DTOs and WorldReader are the only code that changes with it.
    // Every height and size is data: the builder holds no height constants.

    public sealed class WorldDto
    {
        public string Schema, Source;
        public ViewDto View;
        public GroundDto Ground = new();
        public TransitDto Transit = new();
        public List<MassDto> Buildings = new();
        public List<RoofDto> Roofs = new();
        public List<FrontageDto> Frontages = new();
        public List<ConnectorDto> Connectors = new();
        public List<StreetItemDto> Street = new();
        public List<LevelDto> Levels = new();
        public List<MegaDto> Megastructures = new();
        public List<MassDto> Civic = new();
        public List<LandmarkDto> Landmarks = new();
        public List<MassDto> Gates = new();
        public List<FacadeDto> Facades = new();
        public List<SignatureDto> Signatures = new();
        public List<SkinDto> Skins = new(); // the procedural facade of every plot building (window family, accent, per-face bays)
        public FabricDto Fabric = new(); // grammar §2a colour life: balcony awnings, banners, flag masts
        public HorizonDto Horizon; // null when the export has none (the sample)
        public List<CrossingDto> Crossings; // null when the export has none (a plan without streetscape.crossings.strip)
        /// <summary>SHA-256 (lower-case hex) of the export file's bytes, the ones this was parsed from.</summary>
        public string Sha256;
        /// <summary>Elements per layer as the exporter declares them (`counts`); null when the export has none (the sample).</summary>
        public Dictionary<string, int> Counts;
    }

    /// <summary>Play camera: yaw (compass degrees, 90 = looking east), pitch above horizontal, zoom = orthographic size,
    /// focus in plan.</summary>
    public sealed class ViewDto { public float Yaw, Pitch, Zoom; public Vector2 Focus; }

    public sealed class GroundDto
    {
        public List<RoadDto> Roads = new();
        public List<CircusDto> Circuses = new();
        public List<WaterDto> Water = new();
        public List<MassDto> Quays = new();
        public List<RoadDto> Foot = new();   // flat ribbons, no kerbs
        public List<MassDto> Medians = new();
        public List<WaterDto> Flats = new(); // flat land layers (surface at Level)
        public List<MassDto> Plots = new();  // one 0.18 m pavement slab per plot
    }

    /// <summary>Carriageway ribbon at Level, kerb strips either side (KerbWidth wide, KerbHeight proud).</summary>
    public sealed class RoadDto { public string Id, Surface, KerbSurface; public Vector2[] Line; public float Width, Level, KerbWidth, KerbHeight; }

    /// <summary>Round carriageway with a raised island.</summary>
    public sealed class CircusDto { public string Id, Surface, IslandSurface; public Vector2 Centre; public float Radius, IslandRadius, Level, IslandHeight; }

    public sealed class WaterDto { public string Id, Surface; public Vector2[] Ring; public float Level; }

    /// <summary>Extruded footprint from Base to Base + Height (buildings, civic, quays, megastructure parts).</summary>
    /// <summary>Kit (civic objects): the piece standing in for the extrusion at its At, on its Base. Fade (civic kit masses): the
    /// play camera's architecture fade may ghost the piece (a parasol over a table someone stands at). GroundBy: the id of the
    /// kit mass that is this building's ground storey (the café's bay), so the facade draws none.
    /// Materials: this mass's piece draws the slot (a material name on the piece) in the named kit material.</summary>
    public sealed class MassDto
    {
        public string Id, Surface, GroundBy; public Vector2[] Ring; public float Base, Height; public KitRefDto Kit; public bool Fade; public Dictionary<string, string> Materials;

        /// <summary>This mass on another ring (its own ring is left alone).</summary>
        public MassDto With(Vector2[] ring) { var c = (MassDto)MemberwiseClone(); c.Ring = ring; return c; }
    }

    /// <summary>A kit facade piece (bay, end or corner) on a generated building's street face A→B; Kit.At = the piece's footprint centre, Base = pavement top.</summary>
    public sealed class FacadeDto { public string Id, Building, Form; public int Storeys, Edge; public KitRefDto Kit; public float Base; public Vector2 A, B; }

    /// <summary>A plan signature structure (SpaceHQ, a tower site's regenerated piece, the SkyBridge, a sky rail station, the Parade gate block).
    /// Ground (signature only) = [xmin, xmax, ymin, ymax], where no plot geometry stands. Kit null = procedural (Why says why).</summary>
    public sealed class SignatureDto { public string Id, Kind, Source, Pending, Why; public KitRefDto Kit; public float[] Ground; public float Base, Tip; }

    /// <summary>The horizon band: the sea plane and distant pieces, all hazed toward Haze.</summary>
    /// <summary>Sea = the bay sector(s) at SeaLevel (never a square plane), fading into the haze from SeaFadeFrom about SeaCentre; Land = flats
    /// beyond the district (surface `beyond`); Banks = thin quay-coloured walls on the land side of every land/sea edge.</summary>
    public sealed class HorizonDto
    {
        public Vector2 Centre; public string Haze; public string SeaSurface; public Vector2 SeaCentre; public float SeaSize, SeaLevel, SeaFadeFrom;
        public List<WaterDto> SeaSectors = new(), Land = new(), Patches = new(); public List<MassDto> Banks = new(), Hills = new(); // Patches: flats over the land (beyond_green parks and tree clumps, quay promenades); Hills: terraced prisms (beyond_hill)
        public List<HorizonPieceDto> Pieces = new();
    }

    /// <summary>A procedural facade skin (tools/world/gen/skins.ts): family ribbon | porthole | balcony | shed; Accent = the look surface its
    /// speed bands carry; glazing, piers and portholes as data; one face per mass wall (Mass, Edge, A→B).</summary>
    public sealed class SkinDto
    {
        public string Id, Building, Family, Accent;
        public string GlassSurface; public float GlassShare, MaxPane, Sill, Head, Reveal, Mullion;
        public string PierSurface, FrameSurface; public float Frame;
        public float PortholeD, PortholeRing; public string RingSurface; public int PerBay;
        public List<SkinFaceDto> Faces = new();
        // grammar §2a colour life (brief 007 round 3)
        public string Accent2, Trim; // Accent2: banners only; Trim: gold (coping bead, marquee edge, fin beads)
        public float FaiencePlinth, FaienceBand, FaienceBulkhead, FaiencePier; public bool FaiencePiers; // in the family's plinth faience (`<family>_p`); piers in the accent
        public SpandrelDto Spandrel; // null = plain stone under the ribbons
        public float PanelW, PanelH; public string PanelSurface; // the accent panel behind every porthole, flush, ring proud of it
        public List<FinDto> Fins = new();
    }

    /// <summary>Accent panels under every ribbon window (H m high, ending at the sill, the ribbon's width) and the solid front of every balcony (BalconyW x BalconyH at the floor).</summary>
    public sealed class SpandrelDto { public float H, BalconyW, BalconyH; public string Surface; }

    /// <summary>A vertical fin: W wide along the wall, D proud, its back on the wall at At facing Normal, from Z0 to Z1, its outer edge beaded BeadW in BeadSurface.</summary>
    public sealed class FinDto { public string Id, Kind, Surface, BeadSurface; public Vector2 At, Normal; public float W, D, Z0, Z1, BeadW; }

    /// <summary>Striped fabric, banners and flags (grammar §2a); every surface an accent role or `canvas`.</summary>
    public sealed class FabricDto { public List<AwningDto> Awnings = new(); public List<BannerDto> Banners = new(); public List<FlagDto> Flags = new(); }

    /// <summary>A balcony awning: back edge on the wall at At (Z high, W wide), sloping D out and Fall down, a Valance drop at the front; stripes
    /// Stripe wide down the slope alternating Stripes[0] / Stripes[1].</summary>
    public sealed class AwningDto { public string Id, Building, Face; public Vector2 At, Normal; public float Yaw, Z, W, D, Fall, Valance, Stripe; public string[] Stripes; }

    /// <summary>A banner on an arm Arm m out from the wall at At facing Normal: W x H from Z0 in Surface with a BorderW border in BorderSurface.</summary>
    public sealed class BannerDto { public string Id, Building, Kind, Surface, BorderSurface; public Vector2 At, Normal; public float Yaw, Z0, W, H, Arm, BorderW; }

    /// <summary>A flag mast H high in Mast at At (base Z) with a PennantW x PennantH pennant in Surface at the top flying toward Yaw (plan radians).</summary>
    public sealed class FlagDto { public string Id, Kind, Host, Mast, Surface; public Vector2 At; public float Z, H, PennantW, PennantH, Yaw; }

    /// <summary>One wall of a building mass: Bays = one letter per equal bay along A→B (R ribbon, P porthole bay, B balcony bay, - plain stone);
    /// Rows = floor z of each storey with windows; Portholes = positions along A→B of vertical porthole columns.</summary>
    public sealed class SkinFaceDto { public string Mass, Bays; public int Edge; public Vector2 A, B; public float[] Rows, Portholes; public float Glass; }

    /// <summary>Ring A | B | C | shore | rocket-pad | rocket-ascent | planet | moon. Sky bodies and ascents: Base = centre height. Tilt leans the piece TiltDeg toward TiltYaw (plan radians).</summary>
    public sealed class HorizonPieceDto { public string Id, Ring; public KitRefDto Kit; public float Base, Haze, TiltDeg, TiltYaw; }

    public sealed class TransitDto
    {
        public List<TrackDto> Tram = new();
        public List<SkyRailDto> SkyRail = new();
        public List<MassDto> Stops = new(), Stations = new();
    }

    public sealed class TrackDto { public string Id, Surface; public Vector2[] Line; public float Width, Level, Depth, Spacing; } // Spacing > 0: two ribbons that far apart

    /// <summary>Beam with its top at Height; pylons from the ground to the beam soffit.</summary>
    public sealed class SkyRailDto
    {
        public string Id, Surface, PylonSurface;
        public Vector2[] Line, Pylons;
        public float Height, Width, Depth, PylonSize, Ground, Spacing, CapDepth; // Spacing > 0: two beams, pylon carries a cross-head CapDepth deep
    }

    /// <summary>A named walkable level (street, deck, sky concourse) with its decks and the stairs that reach it.</summary>
    public sealed class LevelDto
    {
        public string Id, Name;
        public float Height;
        public List<DeckDto> Decks = new();
        public List<StairDto> Stairs = new();
    }

    /// <summary>Slab with its top at the level height, on square columns down to Ground.</summary>
    public sealed class DeckDto
    {
        public string Id, Surface, ColumnSurface;
        public Vector2[] Ring, Columns;
        public float Thickness, ColumnSize, Ground;
        public string Group; // walkable piece: decks that touch at one level share it
    }

    /// <summary>Straight flight from Foot at Bottom to Head at Top; steps no taller than Riser.</summary>
    public sealed class StairDto { public string Id, Surface; public Vector2 Foot, Head; public float Bottom, Top, Width, Riser; }

    /// <summary>Sphere (radius standing on Base) or cylinder (Radius, Base to Base + Height). Kit = the piece that is the landmark;
    /// Kits = pieces standing on its top (Base + Height) at their At, the landmark itself staying procedural (the skyport's pads).</summary>
    public sealed class LandmarkDto { public string Id, Surface, Shape, Source; public Vector2 Centre; public float Radius, Base, Height; public KitRefDto Kit; public List<KitRefDto> Kits = new(); }

    /// <summary>Gateway: a kit arch standing on the quay under its At (its Yaw turns the piece's +X).</summary>
    public sealed class MegaDto { public string Id; public List<MassDto> Parts = new(); public List<PrimDto> Prims = new(); public KitRefDto Gateway; }

    /// <summary>Primitive solid. Shape: box | cyl | sphere | vault | saucer | wedge. Size = (a along Yaw, b across, h); Base = lowest z; Yaw radians from +x toward +y in plan.</summary>
    public sealed class PrimDto { public string Id, Shape, Surface; public Vector2 Centre; public float Base, Yaw; public Vector3 Size; }

    /// <summary>One programme on a roof at Z (top of the roof slab); Parts are in world frame.</summary>
    /// <summary>Kit = the programme's piece (hover-pad: SkyPad_Roof) standing on the slab at its At; its procedural stand-in is then not in Parts.</summary>
    public sealed class RoofDto { public string Id, Building, Programme, Surface, Form; public float Z, Parapet, Yaw; public Vector2 Centre, Size; public List<PrimDto> Parts = new(); public KitRefDto Kit, Crown; } // Crown: the Crown_* piece at the roof centre

    /// <summary>A tenancy a to b on a facade at Z facing Normal; Sign carries Motif, Icon and a palette key (no text).</summary>
    public sealed class FrontageDto
    {
        public string Id, Host, Level, Trade, Area, Street, SpillKind, Motif, Icon, Palette;
        public float Z, Width, Yaw, SpillDepth; public bool Lobby, Anchor, Staffed; public float HoursFrom, HoursTo; public bool HoursService;
        public Vector2 A, B, Normal;
        public PrimDto Front, Entrance, Canopy, Sign, Service; // Service null where the trade has no bay here
        public string Source, Facade; public KitRefDto SignKit; // Facade = the kit facade piece over this tenancy (its shopfront is in the piece; the sign stays). SignKit adds a blade or wall sign at its At, lowest point at its Base; the fascia stays procedural
    }

    /// <summary>stair | ramp | cartRamp | escalator | lift | goodsLift | stairTower from level From (Bottom) to level To (Top); Serves lists the deck ids it lands on.</summary>
    public sealed class ConnectorDto { public string Id, Kind, Host, From, To; public string[] Serves; public Vector2 Foot, Head; public Vector2[] Ring; public float Bottom, Top, Width, Yaw; public bool StepFree; public List<PrimDto> Dress = new(); } // Dress: the dressed form, drawn instead of the ring prism

    /// <summary>Tree, lamp, furniture, kit item or watcher at At, standing on Z. Kind names the catalogue entry; Variant the species or lamp type.</summary>
    public sealed class StreetItemDto { public string Id, Kind, Surface, Edge, Sight, Variant, Trade, Occupation; public Vector2 At; public float Z, Yaw; public Vector3 Size; public bool Staffed; public float HoursFrom, HoursTo; public bool HoursService; public string Source; public KitRefDto Kit;
        /// <summary>A crossing's part (brief 009): the crossing it belongs to; whether it casts (the signal orb's body only); a signal orb's state (walk | stop).</summary>
        public string Crossing, Signal; public bool CastShadows; }

    /// <summary>A crossing. Kind kerb = two landings (street items, one on each kerb of Road) and the way across the carriageway
    /// between them; stop | bank = one patch, its one street item (the way from a tram platform, or from a station bank). Line = the
    /// way's centre line, end to end; Width across it; Control signal (it carries signal orbs), null = none (a zebra no longer exists, brief 009); Refuges = the rings of the median the way
    /// crosses, where people wait.</summary>
    public sealed class CrossingDto { public string Id, Kind, Road, Control; public string[] Items; public Vector2[] Line; public float Width; public List<Vector2[]> Refuges = new(); }

    /// <summary>A DecoKit piece (contracts/world/kit.json id) standing in for an element. Scale = (across, up, depth) on the piece's own
    /// size = Unity local (x, y, z). Yaw (plan radians from +x toward +y) turns the piece's +X; its front (+Z) faces Yaw + 90°; NaN = the
    /// element's own yaw. At, Base, Mount only on signs.</summary>
    public sealed class KitRefDto { public string Id, Mount; public Vector3 Scale; public float Yaw = float.NaN, Base; public Vector2 At; }
}
