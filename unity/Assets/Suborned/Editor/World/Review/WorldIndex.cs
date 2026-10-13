using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>A kit piece the export (or a generated stand-in) puts on an element: at plan `At`, standing on `Z`.</summary>
    public sealed class KitWant { public string Id, Why; public Vector2 At; public float Z; }

    /// <summary>One export element (or a part of one: a pylon, a roof item, a megastructure part, a frontage sign) with the
    /// box its geometry must occupy, computed from the export alone (Unity frame: x east, y up, z north). Source:
    /// kit (only kit pieces), procedural (batched geometry), mixed (both), covered (a stand-in elsewhere replaces it), none (a group).</summary>
    public sealed class Item
    {
        public string Id, Layer, Kind, Source;
        public Bounds Box;
        public Item Parent;
        public readonly List<KitWant> Kits = new();
        public readonly List<Item> Subs = new();
        public string CoveredBy;
        /// <summary>Slack (m) around Box when looking for built geometry.</summary>
        public float Tol = 0.35f;
        public bool Procedural => Source is "procedural" or "mixed";
        /// <summary>Plan coordinates (x, y, height of the box bottom), for reports.</summary>
        public Vector3 Plan => new(Box.center.x, Box.center.z, Box.min.y);
    }

    /// <summary>The export as review items, one per element per layer (the builder's layers), plus sub-items. Shared by the
    /// props audit (what the scene must contain) and the flyby (where things are).</summary>
    public sealed class WorldIndex
    {
        public readonly WorldDto World;
        public readonly List<Item> Items = new();
        public readonly Dictionary<string, Item> ById = new(System.StringComparer.Ordinal);
        public readonly KitPlacer Placer;
        public readonly StandIns StandIns;

        public static readonly string[] LayerOrder =
            { "ground", "transit", "buildings", "skins", "facades", "roofs", "frontages", "connectors", "street", "levels", "decks", "megastructures", "civic", "gates", "landmarks", "signatures", "horizon" };

        public WorldIndex(WorldDto w)
        {
            World = w;
            Placer = new KitPlacer(HarbourWorldBuilder.KitCataloguePath);
            StandIns = new StandIns(Placer, w);
            var g = w.Ground;
            foreach (var r in g.Roads) Add("ground", r.Id, "road", Line(r.Line, r.Width * 0.5f + r.KerbWidth, r.Level, r.Level + r.KerbHeight));
            foreach (var c in g.Circuses) Add("ground", c.Id, "circus", Disc(c.Centre, c.Radius, c.Level, c.Level + c.IslandHeight));
            foreach (var f in g.Foot) Add("ground", f.Id, "foot", Line(f.Line, f.Width * 0.5f, f.Level, f.Level));
            foreach (var m in g.Medians) Add("ground", m.Id, "median", Mass(m));
            foreach (var f in g.Flats) Add("ground", f.Id, "flat:" + f.Surface, Ring(f.Ring, f.Level, f.Level));
            foreach (var x in g.Water) Add("ground", x.Id, "water", Ring(x.Ring, x.Level, x.Level));
            foreach (var q in g.Quays) Add("ground", q.Id, "quay", Mass(q));
            foreach (var p in g.Plots) Add("ground", p.Id, "plot:" + p.Surface, Mass(p));

            var t = w.Transit;
            foreach (var x in t.Tram) Add("transit", x.Id, "tram-track", Line(x.Line, x.Spacing * 0.5f + x.Width * 0.5f, x.Level - x.Depth, x.Level));
            foreach (var m in t.Stops) Add("transit", m.Id, "tram-stop", Mass(m));
            foreach (var m in t.Stations) Add("transit", m.Id, "skyrail-station", Mass(m));
            foreach (var s in t.SkyRail)
            {
                var it = Add("transit", s.Id, "skyrail-beam", Line(s.Line, s.Spacing * 0.5f + s.Width * 0.5f, s.Height - s.Depth, s.Height));
                float half = Mathf.Max(s.PylonSize, s.Spacing + s.Width) * 0.5f;
                for (int i = 0; i < s.Pylons.Length; i++)
                    Sub(it, $"{s.Id}/pylon-{i}", "skyrail-pylon", Box(s.Pylons[i], half, s.Ground, s.Height - s.Depth));
            }

            foreach (var m in w.Buildings)
            {
                var kind = "building:" + Stem(m.Id);
                if (StandIns.Covers(m)) Add("buildings", m.Id, kind, Mass(m), "covered").CoveredBy = StandIns.CoveredBy(FacadeKit.HostOf(m.Id));
                else Add("buildings", m.Id, kind, Mass(m));
            }

            foreach (var sk in w.Skins)
            {   // the procedural facade skin of a building: its faces from the lowest window row to a storey over the highest
                var pts = new List<Vector2>(); float lo = float.MaxValue, hi = float.MinValue;
                foreach (var f in sk.Faces)
                {
                    pts.Add(f.A); pts.Add(f.B);
                    foreach (var z in f.Rows) { lo = Mathf.Min(lo, z); hi = Mathf.Max(hi, z + 3.6f); }
                }
                if (lo > hi) { lo = 0; hi = 1; }
                bool covered = sk.Faces.TrueForAll(f => w.Buildings.Exists(m => m.Id == f.Mass && StandIns.Covers(m)));
                Add("skins", sk.Id, "skin:" + sk.Family, Ring(pts.ToArray(), lo, hi), covered ? "covered" : "procedural");
            }

            foreach (var f in w.Facades)
            {
                var pivot = Placer.PivotForCentre(f.Kit, f.Kit.At);
                var it = Add("facades", f.Id, "facade:" + f.Form, KitBox(f.Kit, pivot, f.Base), "kit");
                it.Kits.Add(new KitWant { Id = f.Kit.Id, Why = $"{f.Form} on {f.Building}", At = pivot, Z = f.Base });
            }

            foreach (var r in w.Roofs)
            {
                var box = new Bounds(new Vector3(r.Centre.x, r.Z, r.Centre.y), new Vector3(r.Size.x, 0.2f, r.Size.y));
                foreach (var p in r.Parts) box.Encapsulate(Prim(p));
                if (StandIns.Covers(r)) { Add("roofs", r.Id, "roof:" + r.Programme, box, "covered").CoveredBy = StandIns.CoveredBy(r.Building); continue; }
                bool hasKit = r.Kit != null || r.Crown != null;
                var it = Add("roofs", r.Id, "roof:" + r.Programme, box, hasKit ? (r.Parts.Count > 0 ? "mixed" : "kit") : "procedural");
                if (r.Kit != null) it.Kits.Add(new KitWant { Id = r.Kit.Id, Why = "roof " + r.Programme, At = r.Kit.At, Z = r.Z });
                if (r.Crown != null) it.Kits.Add(new KitWant { Id = r.Crown.Id, Why = "crown", At = r.Crown.At, Z = r.Z });
                foreach (var p in r.Parts) Sub(it, p.Id, "roof-item:" + Stem(p.Id), Prim(p));
            }

            foreach (var f in w.Frontages)
            {
                var box = Prim(f.Front);
                foreach (var p in new[] { f.Entrance, f.Canopy, f.Sign, f.Service }) if (p != null) box.Encapsulate(Prim(p));
                var it = Add("frontages", f.Id, "frontage:" + f.Trade, box, f.SignKit != null ? "mixed" : "procedural");
                // The builder's sign grammar (ProcShop.ProcSign) places the sign itself: along the frontage span (pylons and blades
                // near an end), up to 2.45 m out from the wall (or at canopy depth), from 4.9 m above the frontage floor (above the 4.6 m shop header)
                // to 8.65 m (a pylon's top); the export prim gives only its lateral centre. So the sign zone is that span and band.
                // Signs hang at canopy depth (dd = canopy outer edge - 0.4), so the zone reaches the canopy's outer edge where that is deeper.
                var n = f.Normal.normalized;
                float reach = Mathf.Max(2.6f, f.Canopy != null ? Vector2.Dot(f.Canopy.Centre - f.A, n) + f.Canopy.Size.y * 0.5f + 0.2f : 0f);
                var zone = Ring(new[] { f.A - n * 0.2f, f.B - n * 0.2f, f.A + n * reach, f.B + n * reach }, f.Z + 4.85f, f.Z + 8.9f);
                Sub(it, f.Sign.Id ?? f.Id + "/sign", "sign:procedural", zone).Tol = 0f;
                if (f.SignKit != null)
                    it.Kits.Add(new KitWant { Id = f.SignKit.Id, Why = $"{f.SignKit.Mount ?? "sign"} for {f.Trade}", At = f.SignKit.At, Z = f.SignKit.Base });
            }

            foreach (var c in w.Connectors)
            {
                Bounds b;
                if ((c.Head - c.Foot).sqrMagnitude < 1e-4f && c.Ring != null && c.Ring.Length > 2) b = Ring(c.Ring, c.Bottom, c.Top);
                else b = Line(new[] { c.Foot, c.Head }, c.Width * 0.5f, c.Bottom - 1f, c.Top);
                Add("connectors", c.Id, "connector:" + c.Kind, b);
            }

            foreach (var s in w.Street)
            {
                float half = Mathf.Max(s.Size.x, s.Size.y) * 0.6f + 0.2f;
                var it = Add("street", s.Id, "street:" + s.Kind + (s.Variant != null ? "/" + s.Variant : ""),
                    Box(s.At, half, s.Z - 0.1f, s.Z + Mathf.Max(s.Size.z, 0.1f)), s.Kit != null ? "kit" : "procedural");
                if (s.Kit != null) it.Kits.Add(new KitWant { Id = s.Kit.Id, Why = s.Kind, At = s.At, Z = s.Z });
            }

            foreach (var lv in w.Levels)
            {
                var it = Add("levels", lv.Id, "level", new Bounds(new Vector3(0, lv.Height, 0), Vector3.zero), "none");
                foreach (var d in lv.Decks)
                {
                    var b = Ring(d.Ring, Mathf.Min(d.Ground, lv.Height - d.Thickness), lv.Height);
                    Add("decks", d.Id, "deck:" + lv.Name, b).Parent = it;
                }
            }

            foreach (var m in w.Megastructures)
            {
                bool covered = StandIns.Covers(m);
                Bounds? b = null;
                foreach (var p in m.Parts) b = U(b, Mass(p));
                foreach (var p in m.Prims) b = U(b, Prim(p));
                var it = Add("megastructures", m.Id, "megastructure", b ?? default, covered ? "kit" : (m.Gateway != null ? "mixed" : "procedural"));
                if (m.Gateway != null) it.Kits.Add(new KitWant { Id = m.Gateway.Id, Why = "gateway", At = m.Gateway.At, Z = float.NaN });
                if (covered)
                {
                    var c = b.Value;
                    it.Kits.Add(new KitWant { Id = MegaKitId(m.Id), Why = "stand-in for " + m.Id, At = new Vector2(c.center.x, c.center.z), Z = c.min.y });
                }
                foreach (var p in m.Parts) { var s = Sub(it, p.Id, "mega-part:" + p.Surface, Mass(p)); if (covered) { s.Source = "covered"; s.CoveredBy = MegaKitId(m.Id); } }
                foreach (var p in m.Prims) { var s = Sub(it, p.Id, "mega-prim:" + p.Shape, Prim(p)); if (covered) { s.Source = "covered"; s.CoveredBy = MegaKitId(m.Id); } }
            }

            foreach (var m in w.Civic)
            {
                var it = Add("civic", m.Id, "civic:" + CivicStem(m.Id), Mass(m), m.Kit != null ? "kit" : "procedural");
                if (m.Kit != null) it.Kits.Add(new KitWant { Id = m.Kit.Id, Why = "civic object", At = m.Kit.At, Z = m.Kit.Base });
            }
            foreach (var m in w.Gates) Add("gates", m.Id, "gate", Mass(m));
            foreach (var l in w.Landmarks)
            {
                float top = l.Shape == "sphere" ? l.Base + 2f * l.Radius : l.Base + l.Height;
                var lb = Disc(l.Centre, l.Radius, l.Base, top);
                if (l.Kit != null) lb.Encapsulate(KitBox(l.Kit, l.Centre, l.Base)); // the piece's own height (the flyby frames it)
                var it = Add("landmarks", l.Id, "landmark:" + l.Shape, lb, l.Kit != null ? "kit" : (l.Kits.Count > 0 ? "mixed" : "procedural"));
                if (l.Kit != null) it.Kits.Add(new KitWant { Id = l.Kit.Id, Why = "landmark", At = l.Centre, Z = l.Base });
                foreach (var k in l.Kits) it.Kits.Add(new KitWant { Id = k.Id, Why = "on landmark", At = k.At, Z = l.Base + l.Height });
            }

            foreach (var sg in w.Signatures)
            {
                if (sg.Kit == null)
                {
                    var gr = sg.Ground;
                    Add("signatures", sg.Id, "signature:" + sg.Kind, gr is { Length: 4 } ? Ring(new[] { new Vector2(gr[0], gr[2]), new Vector2(gr[1], gr[3]) }, sg.Base, sg.Tip) : default);
                    continue;
                }
                float z = SkylineKit.SignatureZ(Placer, sg);
                var it = Add("signatures", sg.Id, "signature:" + sg.Kind, KitBox(sg.Kit, sg.Kit.At, z), "kit");
                it.Kits.Add(new KitWant { Id = sg.Kit.Id, Why = sg.Kind, At = sg.Kit.At, Z = z });
            }

            if (w.Horizon != null)
                foreach (var p in w.Horizon.Pieces)
                {
                    var it = Add("horizon", p.Id, "horizon:" + p.Ring, KitBox(p.Kit, p.Kit.At, p.Base), "kit");
                    it.Kits.Add(new KitWant { Id = p.Kit.Id, Why = "horizon " + p.Ring, At = p.Kit.At, Z = p.Base });
                }
        }

        /// <summary>World box of a kit ref with its pivot at (pivot, z): the prefab's own bounds, scaled and turned (axis-aligned around the turn).</summary>
        Bounds KitBox(KitRefDto r, Vector2 pivot, float z)
        {
            var lb = Placer.LocalBounds(r.Id); var rot = KitPlacer.Rotation(float.IsNaN(r.Yaw) ? 0f : r.Yaw); Bounds? b = null;
            for (int i = 0; i < 8; i++)
            {
                var c = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = new Vector3(pivot.x, z, pivot.y) + rot * Vector3.Scale(c, r.Scale);
                b = U(b, new Bounds(p, Vector3.zero));
            }
            return b.Value;
        }

        string TowerKit(string host) => Placer.Uses.Where(u => u.use == "site:" + host.Substring("tower/".Length) && Placer.Generated.ContainsKey(u.id)).Select(u => u.id).FirstOrDefault() ?? "?";
        string MegaKitId(string mega) => Placer.Uses.Where(u => u.use == "landmark:" + mega.Substring("mega/".Length) && Placer.Generated.ContainsKey(u.id)).Select(u => u.id).FirstOrDefault() ?? "?";

        Item Add(string layer, string id, string kind, Bounds box, string source = "procedural")
        {
            var it = new Item { Id = id, Layer = layer, Kind = kind, Box = box, Source = source };
            Items.Add(it);
            ById[id] = it;
            return it;
        }

        Item Sub(Item parent, string id, string kind, Bounds box)
        {
            var it = new Item { Id = id, Layer = parent.Layer, Kind = kind, Box = box, Source = "procedural", Parent = parent };
            parent.Subs.Add(it);
            ById.TryAdd(id, it);
            return it;
        }

        /// <summary>Union of the boxes of the named elements: '*' = everything, a layer name, an id, or an id prefix ending in '*'.</summary>
        public Bounds Frame(IEnumerable<string> names)
        {
            Bounds? b = null;
            foreach (var n in names)
            {
                IEnumerable<Item> hit = n == "*" ? Items.Where(i => i.Layer != "levels" && i.Layer != "horizon")
                    : LayerOrder.Contains(n) ? Items.Where(i => i.Layer == n)
                    : n.EndsWith("*") ? Items.Where(i => i.Id.StartsWith(n.TrimEnd('*'), System.StringComparison.Ordinal))
                    : ById.TryGetValue(n, out var one) ? new[] { one } : System.Array.Empty<Item>();
                int count = 0;
                foreach (var i in hit) { b = U(b, i.Box); count++; }
                if (count == 0) throw new System.IO.InvalidDataException($"World: frame `{n}` matches nothing in the export");
            }
            return b ?? throw new System.IO.InvalidDataException("World: empty frame");
        }

        // ------------------------------------------------------------------ boxes

        static Bounds? U(Bounds? a, Bounds b) { if (a is { } h) { h.Encapsulate(b); return h; } return b; }

        public static Bounds Ring(Vector2[] ring, float lo, float hi)
        {
            Vector2 a = ring[0], z = ring[0];
            foreach (var q in ring) { a = Vector2.Min(a, q); z = Vector2.Max(z, q); }
            var b = new Bounds(); b.SetMinMax(new Vector3(a.x, lo, a.y), new Vector3(z.x, hi, z.y));
            return b;
        }

        static Bounds Mass(MassDto m) => Ring(m.Ring, m.Base, m.Base + m.Height);

        static Bounds Line(Vector2[] line, float half, float lo, float hi)
        {
            var b = Ring(line, lo, hi);
            b.Expand(new Vector3(half * 2f, 0f, half * 2f));
            return b;
        }

        static Bounds Disc(Vector2 c, float r, float lo, float hi) => Box(c, r, lo, hi);

        static Bounds Box(Vector2 c, float half, float lo, float hi)
        {
            var b = new Bounds(); b.SetMinMax(new Vector3(c.x - half, lo, c.y - half), new Vector3(c.x + half, hi, c.y + half));
            return b;
        }

        /// <summary>A prim's box: its plan half-diagonal around its centre (yaw-free), base to base + height.</summary>
        static Bounds Prim(PrimDto p) => Box(p.Centre, 0.5f * Mathf.Sqrt(p.Size.x * p.Size.x + p.Size.y * p.Size.y), p.Base, p.Base + p.Size.z);

        static Vector2 Centre(Vector2[] ring) { var b = Ring(ring, 0, 0); return new Vector2(b.center.x, b.center.z); }

        /// <summary>Last id segment without trailing digits and separators: `.../leg-3` → `leg`, `.../e0-3` → `e`.</summary>
        public static string Stem(string id)
        {
            var s = id.Substring(id.LastIndexOf('/') + 1).TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_');
            return s.Length == 0 ? id.Substring(id.LastIndexOf('/') + 1) : s;
        }

        static string CivicStem(string id) => id.StartsWith("civic/civic-office/") ? "civic-office/" + Stem(id) : Stem(id);
    }
}
