using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Parsed JSON → WorldDto. Missing optional arrays read as empty; missing required fields throw (unknown = reject).</summary>
    public static class WorldReader
    {
        public static WorldDto Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"World: export not found at {path}");
            var bytes = File.ReadAllBytes(path); // read once: the hash is of the bytes that are parsed
            var o = Json.Parse(new UTF8Encoding(false).GetString(bytes).TrimStart('\uFEFF'));
            var w = new WorldDto { Schema = J.S(o, "schema"), Source = J.S(o, "source"), Sha256 = Hex(bytes) };
            if (string.IsNullOrEmpty(w.Schema)) throw new InvalidDataException("World: export has no schema");
            var v = Req(o, "view");
            w.View = new ViewDto { Yaw = J.F(v, "yaw"), Pitch = J.F(v, "pitch"), Zoom = J.F(v, "zoom"), Focus = J.P(v, "focus") };

            var g = J.Get(o, "ground");
            w.Ground.Roads = Each(g, "roads", r => new RoadDto
            {
                Id = Id(r), Surface = J.S(r, "surface"), KerbSurface = J.S(r, "kerbSurface"), Line = J.Poly(r, "line"),
                Width = J.F(r, "width"), Level = J.F(r, "level"), KerbWidth = J.F(r, "kerbWidth"), KerbHeight = J.F(r, "kerbHeight"),
            });
            w.Ground.Circuses = Each(g, "circuses", c => new CircusDto
            {
                Id = Id(c), Surface = J.S(c, "surface"), IslandSurface = J.S(c, "islandSurface"), Centre = J.P(c, "centre"),
                Radius = J.F(c, "radius"), IslandRadius = J.F(c, "islandRadius"), Level = J.F(c, "level"), IslandHeight = J.F(c, "islandHeight"),
            });
            w.Ground.Water = Each(g, "water", x => new WaterDto { Id = Id(x), Surface = J.S(x, "surface"), Ring = J.Poly(x, "ring"), Level = J.F(x, "level") });
            w.Ground.Quays = Each(g, "quays", Mass);
            w.Ground.Foot = Each(g, "foot", r => new RoadDto { Id = Id(r), Surface = J.S(r, "surface"), Line = J.Poly(r, "line"), Width = J.F(r, "width"), Level = J.F(r, "level") });
            w.Ground.Medians = Each(g, "medians", Mass);
            w.Ground.Plots = Each(g, "plots", Mass);
            w.Ground.Flats = Each(g, "flats", x => new WaterDto { Id = Id(x), Surface = J.S(x, "surface"), Ring = J.Poly(x, "ring"), Level = J.F(x, "level") });
            w.Ground.Drives = Each(g, "drives", x => new DriveDto
            {
                Id = Id(x), Surface = J.S(x, "surface"), Ring = J.Poly(x, "ring"), Level = J.F(x, "level"), Road = J.S(x, "road"), To = J.S(x, "to"),
                Kerb = J.Poly(x, "kerb"), KerbWidth = J.F(x, "kerbWidth"), KerbHeight = J.F(x, "kerbHeight"),
            }); // optional (present when the civic block states drives)
            w.Ground.Pieces = Each(g, "pieces", StreetItem); // brief 026: optional
            w.Ground.Footways = Each(g, "footways", Footway); // brief 061: optional (present when the plan carries streetscape.continuousFootway)
            w.Ground.Kerbside = Each(g, "kerbside", x => new KerbBayDto
            {
                Id = Id(x), Kind = J.S(x, "kind") is "parallel" or "angle" or "loading" ? J.S(x, "kind") : throw new InvalidDataException($"World: kerb bay {Id(x)}: kind `{J.S(x, "kind")}` (parallel | angle | loading)"),
                Road = J.S(x, "road") ?? throw new InvalidDataException($"World: kerb bay {Id(x)} has no road"), Ring = J.Poly(x, "ring"), Yaw = J.F(x, "yaw"), Level = J.F(x, "level"),
            }); // brief 075: optional
            w.Ground.Islands = Each(g, "islands", Mass); // brief 075: optional

            var t = J.Get(o, "transit");
            w.Transit.Tram = Each(t, "tram", x => new TrackDto
            {
                Id = Id(x), Surface = J.S(x, "surface"), Line = J.Poly(x, "line"), Width = J.F(x, "width"), Level = J.F(x, "level"), Depth = J.F(x, "depth"), Spacing = J.F(x, "spacing"),
            });
            w.Transit.SkyRail = Each(t, "skyRail", x => new SkyRailDto
            {
                Id = Id(x), Surface = J.S(x, "surface"), PylonSurface = J.S(x, "pylonSurface"), Line = J.Poly(x, "line"), Pylons = J.Poly(x, "pylons"),
                Height = J.F(x, "height"), Width = J.F(x, "width"), Depth = J.F(x, "depth"), PylonSize = J.F(x, "pylonSize"), Ground = J.F(x, "ground"), Spacing = J.F(x, "spacing"), CapDepth = J.F(x, "capDepth"),
            });

            if (J.Get(t, "skyTrack") is { } st) // brief 075 roll-out: the old sky track (the sweep and the pylon stacks)
            {
                var bm = J.Get(st, "beam") ?? throw new InvalidDataException("World: transit.skyTrack has no beam");
                var stripe = J.A(bm, "stripe");
                w.Transit.SkyTrack = new SkyTrackDto
                {
                    Piece = J.S(bm, "piece"), Width = J.F(bm, "width"), Depth = J.F(bm, "depth"), Fillet = J.F(bm, "fillet"),
                    StripeLo = (float)(double)stripe[0], StripeHi = (float)(double)stripe[1], StripeProud = J.F(bm, "stripeProud"), UPerMetre = J.F(bm, "uPerMetre"),
                    Beams = Each(bm, "beams", x => new SkyBeamDto { Id = Id(x), Line = J.Poly(x, "line"), Top = J.F(x, "top") }),
                    Pylons = Each(st, "pylons", x =>
                    {
                        var p = new SkyPylonDto { Id = Id(x), At = J.P(x, "at"), Yaw = J.F(x, "yaw"), Ground = J.F(x, "ground") };
                        if (J.Get(x, "foot") is { } f) { p.Foot = Kit(f); p.FootZ = J.F(f, "z"); }
                        var sh = J.Get(x, "shaft") ?? throw new InvalidDataException($"World: sky pylon {p.Id} has no shaft"); p.Shaft = Kit(sh); p.ShaftZ = J.F(sh, "z");
                        var hd = J.Get(x, "head") ?? throw new InvalidDataException($"World: sky pylon {p.Id} has no head"); p.Head = Kit(hd); p.HeadZ = J.F(hd, "z");
                        return p;
                    }),
                };
            }
            w.Transit.Stops = Each(t, "stops", Mass);
            w.Transit.Stations = Each(t, "stations", Mass);
            if (J.Get(t, "wires") is { } wires) // brief 075: the overhead; the contact and span lines are data (their pieces are what draws)
                w.Transit.Wires = Each(wires, "pieces", x => new WirePieceDto
                {
                    Id = Id(x), Kind = J.S(x, "kind") is "contact" or "span" or "hanger" or "rosette" ? J.S(x, "kind") : throw new InvalidDataException($"World: wire piece {Id(x)}: kind `{J.S(x, "kind")}` (contact | span | hanger | rosette)"),
                    Kit = Kit(J.Get(x, "kit")) ?? throw new InvalidDataException($"World: wire piece {Id(x)} has no kit"), Z = J.F(x, "z"),
                });
            if (J.Get(o, "dressing") is { } dressing) // brief 075: presentation only
                w.Trams = Each(dressing, "trams", x => new ParkedTramDto
                {
                    Id = Id(x), Line = J.S(x, "line"), Stop = J.S(x, "stop"), Kit = Kit(J.Get(x, "kit")) ?? throw new InvalidDataException($"World: parked tram {Id(x)} has no kit"), Z = J.F(x, "z"),
                });

            w.Buildings = Each(o, "buildings", Mass);
            w.Roofs = Each(o, "roofs", x => new RoofDto
            {
                Id = Id(x), Building = J.S(x, "building"), Programme = J.S(x, "programme"), Surface = J.S(x, "surface"), Form = J.S(x, "form"), Z = J.F(x, "z"), Parapet = J.F(x, "parapet"), Yaw = J.F(x, "yaw"),
                Centre = J.P(x, "c"), Size = J.P(x, "size"), Parts = Each(x, "parts", Prim), Kit = Kit(J.Get(x, "kit")), Crown = Kit(J.Get(x, "crown")), Kits = Each(x, "kits", Kit), // brief 075 remap: the roofline pieces
            });
            w.Frontages = Each(o, "frontages", Frontage);
            w.Connectors = Each(o, "connectors", c => new ConnectorDto
            {
                Id = Id(c), Kind = J.S(c, "kind"), Host = J.S(c, "host"), From = J.S(c, "from"), To = J.S(c, "to"), Serves = Strs(c, "serves"), Foot = J.P(c, "foot"), Head = J.P(c, "head"),
                Ring = J.Poly(c, "ring"), Bottom = J.F(c, "bottom"), Top = J.F(c, "top"), Width = J.F(c, "width"), Yaw = J.F(c, "yaw"), StepFree = J.B(c, "stepFree"),
                Dress = Each(c, "dress", Prim),
            });
            w.Skins = Each(o, "skins", Skin);
            if (J.Get(o, "fabric") is { } fab)
            {
                w.Fabric.Awnings = Each(fab, "awnings", x => new AwningDto
                {
                    Id = Id(x), Building = J.S(x, "building"), Face = J.S(x, "face"), At = J.P(x, "at"), Normal = J.P(x, "normal"), Yaw = J.F(x, "yaw"), Z = J.F(x, "z"),
                    W = J.F(x, "w"), D = J.F(x, "d"), Fall = J.F(x, "fall"), Valance = J.F(x, "valance"), Stripe = J.F(x, "stripe"), Stripes = Strs(x, "stripes"),
                });
                w.Fabric.Banners = Each(fab, "banners", x => new BannerDto
                {
                    Id = Id(x), Building = J.S(x, "building"), Kind = J.S(x, "kind"), At = J.P(x, "at"), Normal = J.P(x, "normal"), Yaw = J.F(x, "yaw"), Z0 = J.F(x, "z0"),
                    W = J.F(x, "w"), H = J.F(x, "h"), Arm = J.F(x, "arm"), Surface = J.S(x, "surface"), BorderSurface = J.S(Req(x, "border"), "surface"), BorderW = J.F(Req(x, "border"), "w"),
                });
                w.Fabric.Flags = Each(fab, "flags", x => new FlagDto
                {
                    Id = Id(x), Kind = J.S(x, "kind"), Host = J.S(x, "host"), At = J.P(x, "at"), Z = J.F(x, "z"), H = J.F(x, "h"), Mast = J.S(x, "mast"),
                    PennantW = J.A(x, "pennant") is { Count: 2 } pn ? (float)(double)pn[0] : throw new InvalidDataException($"World: flag {Id(x)} pennant"), PennantH = (float)(double)J.A(x, "pennant")[1],
                    Surface = J.S(x, "surface"), Yaw = J.F(x, "yaw"),
                });
                foreach (var a in w.Fabric.Awnings) if (a.Stripes.Length != 2) throw new InvalidDataException($"World: awning {a.Id} needs two stripe surfaces");
            }
            w.Street = Each(o, "street", StreetItem);
            if (J.Get(o, "crossings") is List<object>)
                w.Crossings = Each(o, "crossings", c => new CrossingDto
                {
                    Id = Id(c), Kind = J.S(c, "kind"), Road = J.S(c, "road"), Control = J.S(c, "control"), Items = Strs(c, "items"),
                    Line = J.Poly(c, "line"), Width = J.F(c, "width"), Refuges = Each(c, "refuges", J.Poly),
                    Cycle = J.Get(c, "cycle") is { } cy ? new SignalCycleDto { Walk = (int)J.F(cy, "walk"), Clearing = (int)J.F(cy, "clearing"), Stop = (int)J.F(cy, "stop"), Arrive = (int)J.F(cy, "arrive"), Offset = (int)J.F(cy, "offset") } : null, // brief 061: absent = the export's state stands
                });
            w.Levels = Each(o, "levels", l => new LevelDto
            {
                Id = Id(l), Name = J.S(l, "name"), Height = J.F(l, "height"),
                Decks = Each(l, "decks", d => new DeckDto
                {
                    Group = J.S(d, "group"),
                    Id = Id(d), Surface = J.S(d, "surface"), ColumnSurface = J.S(d, "columnSurface"), Ring = J.Poly(d, "ring"),
                    Columns = J.Get(d, "columns") != null ? J.Poly(d, "columns") : new UnityEngine.Vector2[0],
                    Thickness = J.F(d, "thickness"), ColumnSize = J.F(d, "columnSize"), Ground = J.F(d, "ground"),
                }),
                Stairs = Each(l, "stairs", s => new StairDto
                {
                    Id = Id(s), Surface = J.S(s, "surface"), Foot = J.P(s, "foot"), Head = J.P(s, "head"),
                    Bottom = J.F(s, "bottom"), Top = J.F(s, "top"), Width = J.F(s, "width"), Riser = J.F(s, "riser"),
                }),
            });
            if (J.Get(o, "takedown") is { } td)
                w.Entrances = Each(td, "entrances", x => new EntranceDto { Id = Id(x), Building = J.S(x, "building"), Name = J.S(x, "name"), Kind = J.S(x, "kind"), At = J.P(x, "at"), Z = J.F(x, "z") });
            w.Megastructures = Each(o, "megastructures", m => new MegaDto { Id = Id(m), Kind = J.S(m, "kind"), Parts = Each(m, "parts", Mass), Prims = Each(m, "prims", Prim), Gateway = Kit(J.Get(m, "gateway")) });
            // Splice (18:0x export): a mass with `hidden: true` is data only (the two PavilionPier_Square supports the walk grid keeps); nothing draws it
            var civic = Each(o, "civic", Mass); w.HiddenCivic = civic.Count(m => J.Get(m.Raw, "hidden") is true); w.Civic = civic.Where(m => !(J.Get(m.Raw, "hidden") is true)).ToList();
            w.Landmarks = Each(o, "landmarks", l => new LandmarkDto
            {
                Id = Id(l), Surface = J.S(l, "surface"), Shape = J.S(l, "shape"), Centre = J.P(l, "centre"), Radius = J.F(l, "radius"), Base = J.F(l, "base"), Height = J.F(l, "height"), Source = J.S(l, "source"), Kit = Kit(J.Get(l, "kit")), Kits = Each(l, "kits", Kit), Gateway = Kit(J.Get(l, "gateway")), // brief 075: the landmark's gateway arch
            });
            w.Gates = Each(o, "gates", Mass);
            w.Liveries = J.Get(o, "liveries"); // brief 075-C
            w.Posters = J.Get(o, "posters"); // brief 075-F1
            w.Facades = Each(o, "facades", f => new FacadeDto
            {
                Id = Id(f), Building = J.S(f, "building"), Form = J.S(f, "form"), Storeys = (int)J.F(f, "storeys"), Edge = (int)J.F(f, "edge"),
                Kit = Kit(J.Get(f, "kit")) ?? throw new InvalidDataException($"World: facade {Id(f)} has no kit"), Base = J.F(f, "base"), A = J.P(f, "a"), B = J.P(f, "b"),
                Family = J.S(f, "family"), Casts = J.S(f, "casts"), Colourway = J.S(f, "colourway"),
                Column = J.Get(f, "column") is { } col ? J.S(col, "id") : null, ColumnStoreys = J.Get(f, "column") is { } col2 ? (int)J.F(col2, "storeys") : 0,
                LodClass = J.Get(f, "lod") is { } lod ? J.S(lod, "class") : null,
            });
            w.Signatures = Each(o, "signatures", x => new SignatureDto
            {
                Id = Id(x), Kind = J.S(x, "kind"), Source = J.S(x, "source"), Pending = J.S(x, "pending"), Why = J.S(x, "why"), Kit = Kit(J.Get(x, "kit")),
                Ground = J.Get(x, "ground") is List<object> gr ? gr.ConvertAll(v => (float)(double)v).ToArray() : null, Base = J.F(x, "base"), Tip = J.F(x, "tip"),
            });
            if (J.Get(o, "horizon") is { } hz)
            {
                var sea = Req(hz, "sea");
                w.Horizon = new HorizonDto
                {
                    Centre = J.P(hz, "centre"), Haze = J.S(hz, "haze"), SeaSurface = J.S(sea, "surface"), SeaCentre = J.P(sea, "centre"),
                    SeaSize = J.F(sea, "size"), SeaLevel = J.F(sea, "level"), SeaFadeFrom = J.F(sea, "fadeFrom"),
                    SeaSectors = Each(sea, "sectors", Flat), Land = Each(hz, "land", Flat), Patches = Each(hz, "patches", Flat), Hills = Each(hz, "hills", Mass), Banks = Each(hz, "banks", Mass),
                    Pieces = Each(hz, "pieces", x => new HorizonPieceDto
                    {
                        Id = Id(x), Ring = J.S(x, "ring"), Kit = Kit(J.Get(x, "kit")) ?? throw new InvalidDataException($"World: horizon piece {Id(x)} has no kit"), Base = J.F(x, "base"), Haze = J.F(x, "haze"),
                        TiltDeg = J.Get(x, "tilt") is { } t ? J.F(t, "deg") : 0f, TiltYaw = J.Get(x, "tilt") is { } t2 ? J.F(t2, "yaw") : 0f,
                    }),
                };
            }
            if (J.Get(o, "counts") is Dictionary<string, object> counts)
            {
                w.Counts = new Dictionary<string, int>();
                foreach (var kv in counts) w.Counts[kv.Key] = (int)J.F(counts, kv.Key);
            }
            return w;
        }

        static string Hex(byte[] bytes)
        {
            using var sha = SHA256.Create();
            var sb = new StringBuilder();
            foreach (var b in sha.ComputeHash(bytes)) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        static WaterDto Flat(object x) => new() { Id = Id(x), Surface = J.S(x, "surface"), Ring = J.Poly(x, "ring"), Level = J.F(x, "level") };

        static float[] Floats(object o, string key)
        {
            var l = J.Get(o, key) as List<object>; if (l == null) return new float[0];
            var r = new float[l.Count]; for (int i = 0; i < r.Length; i++) r[i] = (float)(double)l[i]; return r;
        }

        static SkinDto Skin(object x)
        {
            var g = Req(x, "glass"); var p = Req(x, "pier"); var ph = Req(x, "porthole"); var fa = Req(x, "faience"); var panel = Req(ph, "panel"); var sp = J.Get(x, "spandrel");
            return new SkinDto
            {
                Accent2 = J.S(x, "accent2"), Trim = J.S(x, "trim") ?? throw new InvalidDataException($"World: skin {Id(x)} has no trim"),
                FaiencePlinth = J.F(fa, "plinth"), FaienceBand = J.F(fa, "band"), FaienceBulkhead = J.F(fa, "bulkhead"), FaiencePiers = J.B(fa, "piers"), FaiencePier = J.F(fa, "pier"),
                Spandrel = sp == null ? null : new SpandrelDto { H = J.F(sp, "h"), Surface = J.S(sp, "surface"), BalconyW = J.F(Req(sp, "balcony"), "w"), BalconyH = J.F(Req(sp, "balcony"), "h") },
                PanelW = J.F(panel, "w"), PanelH = J.F(panel, "h"), PanelSurface = J.S(panel, "surface"),
                Fins = Each(x, "fins", f => new FinDto
                {
                    Id = Id(f), Kind = J.S(f, "kind"), At = J.P(f, "at"), Normal = J.P(f, "normal"), W = J.F(f, "w"), D = J.F(f, "d"), Z0 = J.F(f, "z0"), Z1 = J.F(f, "z1"),
                    Surface = J.S(f, "surface"), BeadSurface = J.S(Req(f, "bead"), "surface"), BeadW = J.F(Req(f, "bead"), "w"),
                }),
                Id = Id(x), Building = J.S(x, "building"), Family = J.S(x, "family"), Accent = J.S(x, "accent"),
                GlassSurface = J.S(g, "surface"), GlassShare = J.F(g, "share"), MaxPane = J.F(g, "maxPane"), Sill = J.F(g, "sill"), Head = J.F(g, "head"), Reveal = J.F(g, "reveal"), Mullion = J.F(g, "mullion"),
                PierSurface = J.S(p, "surface"), Frame = J.F(p, "frame"), FrameSurface = J.S(p, "frameSurface"),
                PortholeD = J.F(ph, "d"), PortholeRing = J.F(ph, "ring"), RingSurface = J.S(ph, "ringSurface"), PerBay = (int)J.F(ph, "perBay"),
                Faces = Each(x, "faces", f => new SkinFaceDto
                {
                    Mass = J.S(f, "mass"), Edge = (int)J.F(f, "edge"), A = J.P(f, "a"), B = J.P(f, "b"), Bays = J.S(f, "bays") ?? "", Rows = Floats(f, "rows"), Portholes = Floats(f, "portholes"), Glass = J.F(f, "glass"),
                }),
            };
        }

        static MassDto Mass(object m) => new()
        {
            Id = Id(m), Surface = J.S(m, "surface"), Ring = J.Poly(m, "ring"), Base = J.F(m, "base"), Height = J.F(m, "height"), Kit = Kit(J.Get(m, "kit")),
            Fade = J.Get(m, "fade") is true, GroundBy = J.Get(m, "groundBy") as string, Colourway = J.S(m, "colourway"), Raw = m,
            Materials = J.Get(m, "materials") is Dictionary<string, object> slots ? slots.ToDictionary(kv => kv.Key, kv => (string)kv.Value) : null,
        };

        static Vector3 V3(object o, string key) { var a = J.A(o, key); return new Vector3((float)(double)a[0], (float)(double)a[1], (float)(double)a[2]); }

        static string[] Strs(object o, string key)
        {
            var l = J.Get(o, key) as List<object>; if (l == null) return new string[0];
            var r = new string[l.Count]; for (int i = 0; i < r.Length; i++) r[i] = (string)l[i]; return r;
        }

        static PrimDto Prim(object p) => new()
        {
            Id = Id(p), Shape = J.S(p, "shape"), Surface = J.S(p, "surface"), Centre = J.P(p, "c"), Base = J.F(p, "base"), Size = V3(p, "size"), Yaw = J.F(p, "yaw"),
        };

        static FrontageDto Frontage(object f)
        {
            var sign = J.Get(f, "sign"); var svc = J.Get(f, "service"); var hours = J.Get(f, "hours"); var spill = J.Get(f, "spill");
            var h = hours as List<object>;
            return new FrontageDto
            {
                Id = Id(f), Host = J.S(f, "host"), Level = J.S(f, "level"), Trade = J.S(f, "trade"), Area = J.S(f, "area"), Street = J.S(f, "street"), Z = J.F(f, "z"), Width = J.F(f, "w"), Yaw = J.F(f, "yaw"),
                A = J.P(f, "a"), B = J.P(f, "b"), Normal = J.P(f, "normal"), Lobby = J.B(f, "lobby"), Anchor = J.B(f, "anchor"), Staffed = J.B(f, "staffed"),
                SpillKind = J.S(spill, "kind"), SpillDepth = J.F(spill, "depth"), HoursService = hours is string, HoursFrom = h != null ? (float)(double)h[0] : 0, HoursTo = h != null ? (float)(double)h[1] : 0,
                Motif = J.S(sign, "motif"), Icon = J.S(sign, "icon"), Palette = J.S(sign, "palette"),
                Source = J.S(f, "source"), Facade = J.S(f, "facade"), SignKit = Kit(J.Get(f, "signKit")),
                Front = Prim(J.Get(f, "front")), Entrance = Prim(J.Get(f, "entrance")), Canopy = Prim(J.Get(f, "canopy")), Sign = Prim(sign), Service = svc != null ? Prim(svc) : null,
            };
        }

        /// <summary>A continuous footway (brief 061, world.schema.ts Footway); every part required (unknown = reject).</summary>
        static FootwayDto Footway(object x)
        {
            var fr = Req(x, "frame"); var table = Req(x, "table"); var ramp = Req(x, "ramp"); var lip = Req(x, "lip");
            var f = new FootwayDto
            {
                Id = Id(x), Gate = J.S(x, "gate"), Lane = J.S(x, "lane"), Road = J.S(x, "road"), Level = J.F(x, "level"), Half = J.F(x, "half"), Depth = J.F(x, "depth"),
                Origin = J.P(fr, "origin"), Along = J.P(fr, "along"), Into = J.P(fr, "into"),
                TableId = J.S(table, "id") ?? throw new InvalidDataException($"World: {Id(x)}: table without its flat id"), Table = J.Poly(table, "ring"), TableSurface = J.S(table, "surface"),
                Ramp = J.Poly(ramp, "ring"), RampRun = J.F(ramp, "run"), RampRise = J.F(ramp, "rise"), RampSurface = J.S(ramp, "surface"),
                Lip = J.Poly(lip, "ring"), LipD = J.F(lip, "d"), LipDrop = J.F(lip, "drop"), LipSurface = J.S(lip, "surface"),
                Posts = J.Poly(x, "posts"),
            };
            foreach (var b in Each(x, "bands", b => (J.Poly(b, "ring"), J.F(b, "level"), J.S(b, "surface")))) f.Bands.Add(b);
            if (f.Half <= 0f || f.Depth <= 0f || f.LipD <= 0f || f.RampRun <= 0f || f.LipD >= f.Depth) throw new InvalidDataException($"World: {f.Id}: footway sizes out of order (half {f.Half}, lip {f.LipD}, depth {f.Depth}, run {f.RampRun})");
            return f;
        }

        static StreetItemDto StreetItem(object s)
        {
            var st = J.Get(s, "staffed"); var h = st != null ? J.Get(st, "hours") : null; var hl = h as List<object>;
            var halo = J.Get(s, "signal") is { } sgh ? J.Get(sgh, "halo") : null;
            return new StreetItemDto
            {
                HasHalo = halo != null, HaloAt = halo != null ? J.P(halo, "at") : default, HaloZ = halo != null ? J.F(halo, "z") : 0f, HaloR = halo != null ? J.F(halo, "r") : 0f,
                Id = Id(s), Kind = J.S(s, "kind"), Surface = J.S(s, "surface"), Edge = J.S(s, "edge"), Sight = J.S(s, "sight"), Variant = J.S(s, "variant"), At = J.P(s, "at"), Z = J.F(s, "z"), Yaw = J.F(s, "yaw"), Size = V3(s, "size"),
                Staffed = st != null, Trade = st != null ? J.S(st, "trade") : null, Occupation = st != null ? J.S(st, "occupation") : null, HoursService = h is string, HoursFrom = hl != null ? (float)(double)hl[0] : 0, HoursTo = hl != null ? (float)(double)hl[1] : 0,
                Source = J.S(s, "source"), Kit = Kit(J.Get(s, "kit")),
                Crossing = J.S(s, "crossing"), CastShadows = J.B(s, "castShadows"), Signal = J.Get(s, "signal") is { } sg ? J.S(sg, "state") ?? throw new InvalidDataException($"World: {Id(s)}: signal without a state") : null,
            };
        }

        /// <summary>Kit reference or null. An id or scale missing = reject.</summary>
        static KitRefDto Kit(object k)
        {
            if (k == null) return null;
            var r = new KitRefDto { Id = J.S(k, "id") ?? throw new InvalidDataException("World: kit ref without id"), Scale = V3(k, "scale"), Mount = J.S(k, "mount") };
            if (J.Get(k, "yaw") is double y) r.Yaw = (float)y;
            if (J.Get(k, "at") != null) r.At = J.P(k, "at");
            if (J.Get(k, "base") is double b) r.Base = (float)b; // signs only; other placed refs stand on their element
            if (J.Get(k, "z") is double z) r.Z = (float)z; // brief 075 remap: roofs[].kits[] carry their own pivot height
            return r;
        }

        static string Id(object o) => J.S(o, "id") ?? throw new InvalidDataException("World: element without id");

        static object Req(object o, string key) => J.Get(o, key) ?? throw new InvalidDataException($"World: export has no `{key}`");

        static List<T> Each<T>(object parent, string key, System.Func<object, T> read)
        {
            var r = new List<T>();
            if (parent == null || J.Get(parent, key) is not List<object> list) return r;
            foreach (var x in list) r.Add(read(x));
            return r;
        }
    }
}
