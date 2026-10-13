using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>What a walker stands on, from the export alone (the scene has no colliders): the street datum (0, or the water
    /// level over water), the flat tops the export names (plot paving, medians, quays, the circus island, tram platforms, pier
    /// platforms, decks at their level's height, sky rail platforms), the connectors that have a run (stairs, escalators, the
    /// cart ramp), which slope from foot to head, and the cores (stairs or a lift in a shaft), which are a landing at every
    /// surface they serve and at their top: inside one the walker asks for the next level up or down (Shift). Nothing blocks:
    /// walls, railings and water are walked through. Roofs and megastructure floors that are not decks are not stood on.</summary>
    public sealed class WalkSurface
    {
        /// <summary>The tallest step taken without asking (a kerb, a platform edge, the next tread).</summary>
        public const float Step = 0.6f;
        /// <summary>A gap this wide or less is stepped across (the joint between a core and its deck, a platform edge); a wider one is a drop.</summary>
        public const float Stride = 0.8f;
        /// <summary>A cylinder landmark no taller than this is a platform to stand on (the Skyport pad and the liner berth, 1.2 m), not a monument.</summary>
        const float Platform = 3f;

        public readonly struct Spot
        {
            public readonly float Height; public readonly string Id, Level;
            public Spot(float height, string id, string level) { Height = height; Id = id; Level = level; }
        }

        sealed class Flat { public string Id, Level; public Vector2[] Ring; public Rect Box; public float Top; }
        sealed class Run { public string Id; public Vector2[] Ring; public Rect Box; public Vector2 Foot, Head; public float Bottom, Top; }

        readonly List<Flat> flats = new(), water = new();
        readonly List<Run> runs = new();
        readonly List<Spot> found = new();

        public WalkSurface(WorldDto w)
        {
            var g = w.Ground;
            foreach (var x in g.Water) water.Add(Make(x.Id, "water", x.Ring, x.Level));
            foreach (var m in g.Plots) Add(m, "street");
            foreach (var m in g.Medians) Add(m, "street");
            foreach (var m in g.Quays) Add(m, "street");
            foreach (var c in g.Circuses) flats.Add(Make(c.Id, "street", Disc(c.Centre, c.IslandRadius), c.Level + c.IslandHeight));
            foreach (var m in w.Transit.Stops) Add(m, "street");
            foreach (var l in w.Landmarks)
                if (l.Shape == "cylinder" && l.Base + l.Height <= Platform) flats.Add(Make(l.Id, "street", Disc(l.Centre, l.Radius), l.Base + l.Height));
            var served = new Dictionary<string, Flat>(System.StringComparer.Ordinal); // what a core can serve: decks and sky rail platforms
            foreach (var lv in w.Levels)
                foreach (var d in lv.Decks) flats.Add(served[d.Id] = Make(d.Id, lv.Name, d.Ring, lv.Height));
            foreach (var m in w.Transit.Stations) flats.Add(served[m.Id] = Make(m.Id, "concourse", m.Ring, m.Base + m.Height));
            foreach (var c in w.Connectors)
            {
                if (c.Ring is not { Length: > 2 }) continue;
                if ((c.Head - c.Foot).sqrMagnitude > 0.01f)
                {
                    runs.Add(new Run { Id = c.Id, Ring = c.Ring, Box = PlanIndex.Box(c.Ring), Foot = c.Foot, Head = c.Head, Bottom = c.Bottom, Top = c.Top });
                    continue;
                }
                var stops = new List<float>();
                void Landing(float height, string level)
                {
                    foreach (float h in stops) if (Mathf.Abs(h - height) <= Step) return;
                    stops.Add(height); flats.Add(Make(c.Id, level, c.Ring, height));
                }
                foreach (var id in c.Serves ?? System.Array.Empty<string>()) if (served.TryGetValue(id, out var f)) Landing(f.Top, f.Level);
                if (c.Bottom > Step) Landing(c.Bottom, "core");
                Landing(c.Top, "core");
            }
        }

        void Add(MassDto m, string level) => flats.Add(Make(m.Id, level, m.Ring, m.Base + m.Height));

        static Flat Make(string id, string level, Vector2[] ring, float top) => new() { Id = id, Level = level, Ring = ring, Box = PlanIndex.Box(ring), Top = top };

        public static Vector2[] Disc(Vector2 c, float r)
        {
            var ring = new Vector2[32];
            for (int i = 0; i < ring.Length; i++) { float a = i * Mathf.PI * 2f / ring.Length; ring[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
            return ring;
        }

        /// <summary>Every surface at a plan point, lowest first.</summary>
        List<Spot> At(Vector2 p)
        {
            found.Clear();
            var datum = new Spot(0f, null, "street");
            foreach (var x in water) if (x.Box.Contains(p) && Poly.Contains(x.Ring, p)) datum = new Spot(x.Top, x.Id, x.Level);
            found.Add(datum);
            foreach (var f in flats) if (f.Box.Contains(p) && Poly.Contains(f.Ring, p)) found.Add(new Spot(f.Top, f.Id, f.Level));
            foreach (var r in runs)
                if (r.Box.Contains(p) && Poly.Contains(r.Ring, p))
                {
                    var along = r.Head - r.Foot;
                    found.Add(new Spot(Mathf.Lerp(r.Bottom, r.Top, Vector2.Dot(p - r.Foot, along) / along.sqrMagnitude), r.Id, "connector"));
                }
            found.Sort((a, b) => a.Height.CompareTo(b.Height));
            return found;
        }

        /// <summary>The surface a walker whose feet are at `feet` stands on here: the highest within a step up. Over a drop, a
        /// surface at the walker's height within a stride carries them across; failing that, the next one down.</summary>
        public Spot Stand(Vector2 p, float feet)
        {
            var here = Under(p, feet);
            if (here.Height >= feet - Step) return here;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                var near = Under(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Stride, feet);
                if (near.Height >= feet - Step) return near;
            }
            return here;
        }

        Spot Under(Vector2 p, float feet)
        {
            var all = At(p); var best = all[0];
            foreach (var s in all) if (s.Height <= feet + Step) best = s;
            return best;
        }

        /// <summary>The next surface more than a step above (dir > 0) or below here; false when there is none.</summary>
        public bool Shift(Vector2 p, float feet, int dir, out Spot to)
        {
            var all = At(p);
            if (dir > 0) { foreach (var s in all) if (s.Height > feet + Step) { to = s; return true; } }
            else for (int i = all.Count - 1; i >= 0; i--) if (all[i].Height < feet - Step) { to = all[i]; return true; }
            to = default;
            return false;
        }
    }
}
