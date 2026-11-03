using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 061 (Sky 2026-10-25, lane mouths cross on a continuous footway; Vela's sketch briefs/061/footway-sketch.png): the export's
    /// ground.footways drawn as the table's element (the table is a flat in ground.flats, so the ground layer's count stays the export's), its
    /// parts in the frame the export gives (world.schema.ts Footway):
    ///   table: the flat at the pavement top from the lip's back edge (s = lip.d) to the ramp's top (s = depth), in the plots' paving;
    ///   lip:   the table's front edge falling `drop` over lip.d to the carriageway at the kerb line (s = 0), in the pearl kerb;
    ///   ramp:  the lane's carriageway rising `rise` over `run` to the table's back edge, in the lane's finish;
    ///   bands: the flush pearl strips at the ramp's foot (level 0) and behind the lip (the table's level), lifted as paint is.
    /// The table is drawn from lip.d, not over its whole ring: the lip lies inside the ring and a flat at the table's level would cover it
    /// (tools/geometry/visible.ts takes the higher of the two there; Splice's to settle, port-progress 2026-10-25). The posts are the export's
    /// bollards (street/footway/&lt;gate&gt;/post-a|b), placed with the street. The higher road's kerb is not laid across the mouth already
    /// (Layers.OffOtherRoads drops it on the lane's carriageway) and the lane's kerbs run along the table's sides. Geometry only; the sim and
    /// the walk read the export (the walk file), never this.</summary>
    public static class FootwayKit
    {
        /// <summary>m: a band lies this far over the floor it marks (RoadMarkings lifts its paint 2.5 mm; the bands share the kerb material).</summary>
        const float Lift = 0.003f;
        /// <summary>m: an export ring may differ from the one the frame gives by this much (it is rounded to 3 decimals).</summary>
        const float Tol = 0.01f;
        public static int Built, Bands;

        /// <summary>The footways by their table's flat id (the flats the ground layer draws here instead of as a plain flat).</summary>
        public static Dictionary<string, FootwayDto> ByTable(GroundDto g)
        {
            var d = new Dictionary<string, FootwayDto>(System.StringComparer.Ordinal);
            foreach (var f in g.Footways) if (!d.TryAdd(f.TableId, f)) throw new InvalidDataException($"World: {f.Id}: table {f.TableId} shared with another footway");
            return d;
        }

        /// <summary>The table, lip, ramp and bands of one footway, as parts of the table's element.</summary>
        public static void Build(Nodes nodes, Transform el, FootwayDto f)
        {
            float h = f.Half, d = f.LipD, D = f.Depth, run = f.RampRun, top = f.Level, foot = top - f.RampRise, kerb = top - f.LipDrop;
            Check(f, "table", f.Table, Rect(f, 0f, D)); Check(f, "lip", f.Lip, Rect(f, 0f, d)); Check(f, "ramp", f.Ramp, Rect(f, D, D + run));
            nodes.Part(el, "table", Quad(new MeshKit(), P(f, -h, d, top), P(f, h, d, top), P(f, h, D, top), P(f, -h, D, top)), nodes.Ground(f.TableSurface));
            nodes.Part(el, "lip", Quad(new MeshKit(), P(f, -h, 0f, kerb), P(f, h, 0f, kerb), P(f, h, d, top), P(f, -h, d, top)), nodes.Ground(f.LipSurface));
            nodes.Part(el, "ramp", Quad(new MeshKit(), P(f, -h, D, top), P(f, h, D, top), P(f, h, D + run, foot), P(f, -h, D + run, foot)), nodes.Ground(f.RampSurface));
            for (int i = 0; i < f.Bands.Count; i++)
            {
                var (ring, level, surface) = f.Bands[i];
                nodes.Part(el, $"band-{i}", new MeshKit().Flat(ring, level + Lift), nodes.Ground(surface));
                Bands++;
            }
            Built++;
        }

        /// <summary>True if plan point q lies on a footway's table, ramp or foot band (grown 0.2 m): the lane's centre-line paint keeps off it.</summary>
        public static bool On(List<FootwayDto> footways, Vector2 q)
        {
            foreach (var f in footways)
            {
                var r = q - f.Origin; float a = Vector2.Dot(r, f.Along), s = Vector2.Dot(r, f.Into), reach = f.Depth + f.RampRun;
                foreach (var b in f.Bands) foreach (var p in b.ring) reach = Mathf.Max(reach, Vector2.Dot(p - f.Origin, f.Into));
                if (Mathf.Abs(a) <= f.Half + 0.2f && s >= -0.2f && s <= reach + 0.2f) return true;
            }
            return false;
        }

        static Vector3 P(FootwayDto f, float a, float s, float z) { var q = f.Origin + f.Along * a + f.Into * s; return new Vector3(q.x, z, q.y); }
        static Vector2[] Rect(FootwayDto f, float s0, float s1)
        {
            Vector2 At(float a, float s) => f.Origin + f.Along * a + f.Into * s;
            return new[] { At(-f.Half, s0), At(f.Half, s0), At(f.Half, s1), At(-f.Half, s1) };
        }

        /// <summary>The export's ring is the rectangle its frame gives (each corner within Tol of one of the ring's): else the data disagree with itself (reject).</summary>
        static void Check(FootwayDto f, string part, Vector2[] ring, Vector2[] want)
        {
            if (ring == null || ring.Length != 4) throw new InvalidDataException($"World: {f.Id}: {part} ring is not four corners");
            foreach (var w in want)
            {
                bool hit = false; foreach (var p in ring) if ((p - w).sqrMagnitude <= Tol * Tol) { hit = true; break; }
                if (!hit) throw new InvalidDataException($"World: {f.Id}: {part} ring does not match its frame (corner {w} missing)");
            }
        }

        /// <summary>One quad facing up (a slope or a flat), whatever the corners' turn.</summary>
        static MeshKit Quad(MeshKit k, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            int a = k.Vert(p0), b = k.Vert(p1), c = k.Vert(p2), d = k.Vert(p3);
            if (Vector3.Cross(p1 - p0, p2 - p0).y >= 0f) { k.Tri(a, b, c); k.Tri(a, c, d); } else { k.Tri(a, c, b); k.Tri(a, d, c); }
            return k;
        }
    }
}
