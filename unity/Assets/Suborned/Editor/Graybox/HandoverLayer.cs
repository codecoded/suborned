using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>The Handover block at exact geometry: the two block pavements (0 → pavement, lay-bys notched out as
    /// exported), buildings, structures and objects extruded from base to base + height as exported (no corrections), the
    /// two lay-by bays as surfaces at their exported level, the zebra crossing as a marking, and the mission points as
    /// person-height pegs standing on the ground under them (doors also get a pad), the crew as smaller pegs, and the
    /// Records Office entrance towers as thin anchor posts (no tower geometry: the sources give none). Boarding canopies are a roof slab on
    /// corner posts so the bay under them stays visible.</summary>
    public static class HandoverLayer
    {
        const float MarkingLevel = 0.01f, StripeLevel = 0.015f;

        public static void Build(Transform root, object world, CutAway cut)
        {
            var h = J.O(world)["handover"];
            float pavement = J.F(J.O(world)["heights"], "pavement");

            var blocks = Nodes.Group(root, "blocks");
            foreach (var b in J.A(h, "blocks"))
                Nodes.Part(blocks, J.S(b, "id"), new MeshKit().Prism(J.Poly(b, "outer"), 0, pavement), "pavement");

            var buildings = Nodes.Group(root, "buildings");
            foreach (var b in J.A(h, "buildings"))
            {
                string id = J.S(b, "id"), material = id == "cafe" ? "building/cafe" : "building";
                var go = Nodes.Part(buildings, id, Solid(b), material);
                float lo = J.F(b, "base");
                cut.Add(id, J.Poly(b, "polygon"), lo, lo + J.F(b, "height"), material, go);
            }
            Towers(root, h);

            var structures = Nodes.Group(root, "structures");
            foreach (var s in J.A(h, "structures"))
                Nodes.Part(structures, J.S(s, "id"), J.S(s, "category") == "boarding-canopy" ? Canopy(s) : Solid(s), "structure");

            var objects = Nodes.Group(root, "objects");
            foreach (var o in J.A(h, "objects"))
                Nodes.Part(objects, J.S(o, "id"), Solid(o), "object/" + J.S(o, "kind"));

            var bays = Nodes.Group(root, "bays");
            foreach (var b in J.A(h, "bays"))
            {
                string id = J.S(b, "id");
                Nodes.Part(bays, id, new MeshKit().Flat(J.Poly(b, "polygon"), J.F(b, "level") + MarkingLevel), "bay/" + id);
            }

            var crossing = Nodes.Group(root, "crossing");
            var area = J.Poly(h, "crossing");
            Nodes.Part(crossing, "crossing", new MeshKit().Flat(area, MarkingLevel), "crossing");
            Nodes.Part(crossing, "stripes", Zebra(Poly.Clean(area)), "crossing/stripe");

            Points(root, h, pavement);
            Crew(root, h, pavement);
        }

        /// <summary>Entrance towers are anchors only (a point on the building face and a crown height; the sources give no
        /// footprint), so each is a thin post from the building base to its crown, not geometry. The building itself stands
        /// at its exported height (the Records Office: wing height). Never a collider in the visibility study.</summary>
        static void Towers(Transform root, object h)
        {
            var towers = Nodes.Group(root, "towers");
            foreach (var b in J.A(h, "buildings"))
            {
                if (J.Get(b, "towers") is not List<object> list) continue;
                float lo = J.F(b, "base");
                foreach (var t in list)
                {
                    var post = Poly.Rect(J.P(t, "at"), Vector2.right, Style.TowerPost, Style.TowerPost);
                    Nodes.Part(towers, J.S(b, "id") + "-" + J.S(t, "id"), new MeshKit().Prism(post, lo, lo + J.F(t, "height")), "marker/tower");
                }
            }
        }

        /// <summary>Crew start positions as small pegs in their own colour, on the ground under them.</summary>
        static void Crew(Transform root, object h, float pavement)
        {
            var crew = Nodes.Group(root, "crew");
            foreach (var c in J.A(h, "crew"))
            {
                var at = J.P(c, "at");
                float ground = Ground(h, at, pavement);
                var peg = Poly.Rect(at, Vector2.right, Style.CrewPegSize, Style.CrewPegSize);
                Nodes.Part(crew, J.S(c, "name"), new MeshKit().Prism(peg, ground, ground + Style.CrewPegHeight), "marker/crew");
            }
        }

        /// <summary>Doors and entrances, by name (the export gives points no kind).</summary>
        public static bool IsDoor(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("door") || lower.Contains("entrance");
        }

        /// <summary>The ground under a point: a bay's level inside a lay-by, else the pavement.</summary>
        public static float Ground(object h, Vector2 at, float pavement)
        {
            foreach (var b in J.A(h, "bays")) if (Poly.Contains(J.Poly(b, "polygon"), at)) return J.F(b, "level");
            return pavement;
        }

        /// <summary>One peg per mission point (plan order), standing on its ground (a bay's level inside a lay-by, else the
        /// pavement), and a pad under each door or entrance. The export gives points as name + position + surface, with
        /// no kind, so doors are recognised by name.</summary>
        static void Points(Transform root, object h, float pavement)
        {
            var points = Nodes.Group(root, "points");
            var doors = Nodes.Group(root, "doors");
            foreach (var p in J.A(h, "points"))
            {
                string name = J.S(p, "name");
                var at = J.P(p, "at");
                float ground = Ground(h, at, pavement);
                var peg = Poly.Rect(at, Vector2.right, Style.PegSize, Style.PegSize);
                Nodes.Part(points, name, new MeshKit().Prism(peg, ground, ground + Style.PegHeight), "marker/point");
                if (!IsDoor(name)) continue;
                var pad = Poly.Rect(at, Vector2.right, Style.DoorPad, Style.DoorPad);
                Nodes.Part(doors, name, new MeshKit().Flat(pad, ground + Style.DoorPadLift), "marker/door");
            }
        }

        /// <summary>Stripes across a four-sided crossing, spaced along its long side (the walking direction).</summary>
        static MeshKit Zebra(Vector2[] q)
        {
            var kit = new MeshKit();
            if (q.Length != 4) return kit;
            Vector2 a = q[1] - q[0], b = q[2] - q[1];
            var (walk, across) = a.sqrMagnitude >= b.sqrMagnitude ? (a, b) : (b, a);
            var centre = (q[0] + q[1] + q[2] + q[3]) * 0.25f;
            float pitch = Style.StripeWidth * 2, length = walk.magnitude, span = across.magnitude - 2 * Style.StripeMargin;
            int n = Mathf.FloorToInt((length - Style.StripeWidth) / pitch) + 1;
            var dir = walk.normalized;
            for (int i = 0; i < n; i++)
            {
                var c = centre + dir * ((i - (n - 1) * 0.5f) * pitch);
                kit.Flat(Poly.Rect(c, across, span, Style.StripeWidth), StripeLevel);
            }
            return kit;
        }

        static MeshKit Solid(object o)
        {
            float b = J.F(o, "base");
            return new MeshKit().Prism(J.Poly(o, "polygon"), b, b + J.F(o, "height"));
        }

        static MeshKit Canopy(object o)
        {
            float b = J.F(o, "base"), top = b + J.F(o, "height");
            var poly = Poly.Clean(J.Poly(o, "polygon"));
            var kit = new MeshKit().Prism(poly, top - Style.CanopyRoof, top);
            var inner = Poly.Inset(poly, Style.CanopyPost) ?? poly;
            foreach (var c in inner)
                kit.Prism(Poly.Rect(c, Vector2.right, Style.CanopyPost, Style.CanopyPost), b, top - Style.CanopyRoof);
            return kit;
        }
    }
}
