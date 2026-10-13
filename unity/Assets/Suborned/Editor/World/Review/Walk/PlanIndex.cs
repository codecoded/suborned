using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>The plan ids at one point, so a comment can be routed to its owner. Null = none there.</summary>
    public struct PlanIds
    {
        /// <summary>Area: plan area id. Plot: plot/…. Road: road/…, foot/… or circus/…. On: the structure stood on or looked at (bld/…, civic/…, mega/…, a deck, a stair or lift, a landmark, a quay, water).</summary>
        public string Area, Plot, Road, On;

        public override string ToString()
        {
            var parts = new List<string>(4);
            foreach (var s in new[] { Area != null ? "area " + Area : null, Plot, Road, On }) if (s != null) parts.Add(s);
            return parts.Count == 0 ? "off the plan" : string.Join(" · ", parts);
        }
    }

    /// <summary>Which plan elements a point lies in: the plan's areas (contracts/world/&lt;district&gt;.plan.json; the export carries
    /// none), and from the export the plots, the road or foot line under it, and the structure it stands on or in. Also the
    /// plan's display names by export id.</summary>
    public sealed class PlanIndex
    {
        public sealed class AreaInfo { public string Id, Name; public Rect Rect; public Vector2 Label; }
        /// <summary>A structure's footprint and the heights it fills. Run (a stair, an escalator, a ramp): its tread instead, rising from Lo at Foot to Hi at Foot + Run.</summary>
        sealed class Zone { public string Id; public Vector2[] Ring; public Rect Box; public float Lo, Hi; public Vector2 Foot, Run; }
        sealed class Line { public string Id; public Vector2[] Pts; public Rect Box; public float Half; }

        /// <summary>How far above or below a structure a point may be and still count as on it (a walker's feet, a ray's half-cell step).</summary>
        const float Near = 3f;

        public readonly List<AreaInfo> Areas = new();
        readonly List<Zone> plots = new(), solids = new();
        readonly List<Line> lines = new();
        readonly Dictionary<string, string> names = new(System.StringComparer.Ordinal);

        public PlanIndex(WorldDto w, WorldIndex index, string planPath)
        {
            var plan = Json.Parse(File.ReadAllText(planPath));
            foreach (var a in J.A(plan, "areas"))
            {
                var r = J.A(a, "rect"); // [x0, x1, y0, y1]
                Areas.Add(new AreaInfo { Id = J.S(a, "id"), Name = J.S(a, "name"), Rect = Rect.MinMaxRect(N(r[0]), N(r[2]), N(r[1]), N(r[3])), Label = J.P(a, "label") });
            }
            foreach (var (key, prefix) in new[] { ("landmarks", "landmark/"), ("signatures", "signature/"), ("megastructures", "mega/"), ("roads", "road/"), ("foot", "foot/"), ("plots", "plot/") })
                foreach (var e in J.A(plan, key))
                    if (J.S(e, "name") is { } name) names[prefix + J.S(e, "id")] = name;

            var g = w.Ground;
            foreach (var m in g.Plots) plots.Add(Mass(m));
            foreach (var r in g.Roads) lines.Add(new Line { Id = r.Id, Pts = r.Line, Box = Grow(Box(r.Line), r.Width * 0.5f + r.KerbWidth), Half = r.Width * 0.5f + r.KerbWidth });
            foreach (var r in g.Foot) lines.Add(new Line { Id = r.Id, Pts = r.Line, Box = Grow(Box(r.Line), r.Width * 0.5f), Half = r.Width * 0.5f });
            foreach (var c in g.Circuses) lines.Add(new Line { Id = c.Id, Pts = new[] { c.Centre }, Box = Grow(Box(new[] { c.Centre }), c.Radius), Half = c.Radius });

            foreach (var x in g.Water) solids.Add(new Zone { Id = x.Id, Ring = x.Ring, Box = Box(x.Ring), Lo = x.Level, Hi = x.Level });
            foreach (var list in new[] { g.Quays, g.Medians, w.Transit.Stops, w.Transit.Stations, w.Buildings, w.Civic, w.Gates })
                foreach (var m in list) solids.Add(Mass(m));
            foreach (var mega in w.Megastructures)
                foreach (var m in mega.Parts) solids.Add(Mass(m));
            foreach (var lv in w.Levels)
                foreach (var d in lv.Decks) solids.Add(new Zone { Id = d.Id, Ring = d.Ring, Box = Box(d.Ring), Lo = lv.Height - d.Thickness, Hi = lv.Height });
            foreach (var c in w.Connectors)
                if (c.Ring is { Length: > 2 }) solids.Add(new Zone { Id = c.Id, Ring = c.Ring, Box = Box(c.Ring), Lo = c.Bottom, Hi = c.Top, Foot = c.Foot, Run = c.Head - c.Foot });
            foreach (var l in w.Landmarks) Boxed(index, l.Id, WalkSurface.Disc(l.Centre, l.Radius));
            foreach (var s in w.Signatures) Boxed(index, s.Id, null);
        }

        /// <summary>A zone from the element's review box (its kit piece's size): over `ring`, or over the box's own footprint.</summary>
        void Boxed(WorldIndex index, string id, Vector2[] ring)
        {
            if (!index.ById.TryGetValue(id, out var item) || item.Box.size == Vector3.zero) return;
            var b = item.Box;
            ring ??= new[] { new Vector2(b.min.x, b.min.z), new Vector2(b.max.x, b.min.z), new Vector2(b.max.x, b.max.z), new Vector2(b.min.x, b.max.z) };
            solids.Add(new Zone { Id = id, Ring = ring, Box = Box(ring), Lo = b.min.y, Hi = b.max.y });
        }

        /// <summary>The plan's display name for an export id ("SpaceHQ · Crown Terminal"), or null.</summary>
        public string Name(string id) => names.TryGetValue(id, out var n) ? n : null;

        public PlanIds At(Vector3 point)
        {
            var p = new Vector2(point.x, point.z);
            var ids = new PlanIds();
            foreach (var a in Areas) if (a.Rect.Contains(p)) { ids.Area = $"{a.Id} ({a.Name})"; break; }
            foreach (var z in plots) if (z.Box.Contains(p) && Poly.Contains(z.Ring, p)) { ids.Plot = z.Id; break; }

            float nearest = 1f; // distance to the centre line as a share of the half width
            foreach (var l in lines)
            {
                if (!l.Box.Contains(p)) continue;
                float d = Distance(l.Pts, p) / l.Half;
                if (d <= nearest) { nearest = d; ids.Road = l.Id; }
            }

            float off = Near, lo = float.MinValue;
            foreach (var z in solids)
            {
                if (!z.Box.Contains(p) || !Poly.Contains(z.Ring, p)) continue;
                float zlo = z.Lo, zhi = z.Hi;
                if (z.Run.sqrMagnitude > 0.01f) zlo = zhi = Mathf.Lerp(z.Lo, z.Hi, Vector2.Dot(p - z.Foot, z.Run) / z.Run.sqrMagnitude);
                float d = point.y < zlo ? zlo - point.y : point.y > zhi ? point.y - zhi : 0f;
                if (d < off || (d == off && zlo > lo)) { off = d; lo = zlo; ids.On = z.Id; } // among the masses at this height, the upper one (a body over its podium)
            }
            return ids;
        }

        static Zone Mass(MassDto m) => new() { Id = m.Id, Ring = m.Ring, Box = Box(m.Ring), Lo = m.Base, Hi = m.Base + m.Height };

        static float N(object o) => (float)(double)o;

        public static Rect Box(Vector2[] ring)
        {
            Vector2 lo = ring[0], hi = ring[0];
            foreach (var q in ring) { lo = Vector2.Min(lo, q); hi = Vector2.Max(hi, q); }
            return Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
        }

        static Rect Grow(Rect r, float by) => Rect.MinMaxRect(r.xMin - by, r.yMin - by, r.xMax + by, r.yMax + by);

        static float Distance(Vector2[] line, Vector2 p)
        {
            float best = Vector2.Distance(line[0], p);
            for (int i = 0; i + 1 < line.Length; i++)
            {
                Vector2 a = line[i], ab = line[i + 1] - a;
                float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                best = Mathf.Min(best, Vector2.Distance(a + ab * t, p));
            }
            return best;
        }
    }
}
