using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Parsed JSON → WorldDto. Missing optional arrays read as empty; missing required fields throw (unknown = reject).</summary>
    public static class WorldReader
    {
        public static WorldDto Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"World: export not found at {path}");
            var o = Json.Parse(File.ReadAllText(path));
            var w = new WorldDto { Schema = J.S(o, "schema"), Source = J.S(o, "source") };
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

            w.Transit.Stops = Each(t, "stops", Mass);
            w.Transit.Stations = Each(t, "stations", Mass);

            w.Buildings = Each(o, "buildings", Mass);
            w.Roofs = Each(o, "roofs", x => new RoofDto
            {
                Id = Id(x), Building = J.S(x, "building"), Programme = J.S(x, "programme"), Surface = J.S(x, "surface"), Form = J.S(x, "form"), Z = J.F(x, "z"), Parapet = J.F(x, "parapet"), Yaw = J.F(x, "yaw"),
                Centre = J.P(x, "c"), Size = J.P(x, "size"), Parts = Each(x, "parts", Prim), Kit = Kit(J.Get(x, "kit")), Crown = Kit(J.Get(x, "crown")),
            });
            w.Frontages = Each(o, "frontages", Frontage);
            w.Connectors = Each(o, "connectors", c => new ConnectorDto
            {
                Id = Id(c), Kind = J.S(c, "kind"), Host = J.S(c, "host"), From = J.S(c, "from"), To = J.S(c, "to"), Serves = Strs(c, "serves"), Foot = J.P(c, "foot"), Head = J.P(c, "head"),
                Ring = J.Poly(c, "ring"), Bottom = J.F(c, "bottom"), Top = J.F(c, "top"), Width = J.F(c, "width"), Yaw = J.F(c, "yaw"), StepFree = J.B(c, "stepFree"),
                Dress = Each(c, "dress", Prim),
            });
            w.Skins = Each(o, "skins", Skin);
            w.Street = Each(o, "street", StreetItem);
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
            w.Megastructures = Each(o, "megastructures", m => new MegaDto { Id = Id(m), Parts = Each(m, "parts", Mass), Prims = Each(m, "prims", Prim), Gateway = Kit(J.Get(m, "gateway")) });
            w.Civic = Each(o, "civic", Mass);
            w.Landmarks = Each(o, "landmarks", l => new LandmarkDto
            {
                Id = Id(l), Surface = J.S(l, "surface"), Shape = J.S(l, "shape"), Centre = J.P(l, "centre"), Radius = J.F(l, "radius"), Base = J.F(l, "base"), Height = J.F(l, "height"), Source = J.S(l, "source"), Kit = Kit(J.Get(l, "kit")), Kits = Each(l, "kits", Kit),
            });
            w.Gates = Each(o, "gates", Mass);
            w.Facades = Each(o, "facades", f => new FacadeDto
            {
                Id = Id(f), Building = J.S(f, "building"), Form = J.S(f, "form"), Storeys = (int)J.F(f, "storeys"), Edge = (int)J.F(f, "edge"),
                Kit = Kit(J.Get(f, "kit")) ?? throw new InvalidDataException($"World: facade {Id(f)} has no kit"), Base = J.F(f, "base"), A = J.P(f, "a"), B = J.P(f, "b"),
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
                    SeaSectors = Each(sea, "sectors", Flat), Land = Each(hz, "land", Flat), Banks = Each(hz, "banks", Mass),
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

        static WaterDto Flat(object x) => new() { Id = Id(x), Surface = J.S(x, "surface"), Ring = J.Poly(x, "ring"), Level = J.F(x, "level") };

        static float[] Floats(object o, string key)
        {
            var l = J.Get(o, key) as List<object>; if (l == null) return new float[0];
            var r = new float[l.Count]; for (int i = 0; i < r.Length; i++) r[i] = (float)(double)l[i]; return r;
        }

        static SkinDto Skin(object x)
        {
            var g = Req(x, "glass"); var p = Req(x, "pier"); var ph = Req(x, "porthole");
            return new SkinDto
            {
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

        static StreetItemDto StreetItem(object s)
        {
            var st = J.Get(s, "staffed"); var h = st != null ? J.Get(st, "hours") : null; var hl = h as List<object>;
            return new StreetItemDto
            {
                Id = Id(s), Kind = J.S(s, "kind"), Surface = J.S(s, "surface"), Edge = J.S(s, "edge"), Sight = J.S(s, "sight"), Variant = J.S(s, "variant"), At = J.P(s, "at"), Z = J.F(s, "z"), Yaw = J.F(s, "yaw"), Size = V3(s, "size"),
                Staffed = st != null, Trade = st != null ? J.S(st, "trade") : null, Occupation = st != null ? J.S(st, "occupation") : null, HoursService = h is string, HoursFrom = hl != null ? (float)(double)hl[0] : 0, HoursTo = hl != null ? (float)(double)hl[1] : 0,
                Source = J.S(s, "source"), Kit = Kit(J.Get(s, "kit")),
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
