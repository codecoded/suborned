using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 026 round 2: road markings, drawn procedurally as the old city drew them (the old project's DecoKitRoadMarkings: a dashed
    /// centre line, 2 m on and 4 m off, on carriageways wide enough for two running lanes and no wider; no zebra, brief 009). Read off the
    /// export's roads, not hand-placed: a road whose carriageway is 7 to 17 m wide (one without a median; the 24 and 28 m roads carry theirs
    /// as `ground.medians`) gets the dash every 6 m of its centreline, kept clear of its ends (6 m), of other roads' carriageways (junction
    /// boxes, + 1.5 m), of the circus and of every crossing strip (+ 1 m). The export has no lane or marking data (Splice: none in
    /// world.schema.ts); if it gains some, that wins and this rule goes. Paint is geometry (crisp at every zoom), a part of its road's
    /// element in look.json's `ground/paint`.</summary>
    public static class RoadMarkings
    {
        const float MinW = 7f, MaxW = 17f, On = 2f, Off = 4f, EndClear = 6f, JunctionClear = 1.5f, CrossClear = 1f, Wide = 0.12f, Lift = 0.0025f;
        public const string Surface = "ground/paint";
        public static int Dashes, Roads;

        public static void Build(Nodes nodes, Transform ground, WorldDto world)
        {
            Dashes = Roads = 0;
            if (!nodes.Has(Surface)) { Debug.Log("World: road markings off (look.json has no ground/paint)"); return; }
            var roads = world.Ground.Roads;
            var lines = new List<(Vector2[] line, float half, string id)>();
            foreach (var r in roads) lines.Add((Poly.CleanLine(r.Line), r.Width * 0.5f, r.Id));
            foreach (var r in roads)
            {
                if (r.Width < MinW || r.Width > MaxW) continue;
                var line = Poly.CleanLine(r.Line); if (line.Length < 2) continue;
                float total = 0f; for (int i = 1; i < line.Length; i++) total += Vector2.Distance(line[i - 1], line[i]);
                var kit = new MeshKit(); int n = 0;
                for (float s = 0f; s + On <= total; s += On + Off)
                {
                    if (s < EndClear || s + On > total - EndClear) continue;
                    var a = At(line, s); var b = At(line, s + On); var mid = (a + b) * 0.5f;
                    if (InOther(lines, r.Id, mid) || InCrossing(world, mid) || InCircus(world, mid)) continue;
                    var d = (b - a).normalized; var nrm = new Vector2(-d.y, d.x) * (Wide * 0.5f); float y = r.Level + Lift;
                    int i0 = kit.Vert(W(a - nrm, y)), i1 = kit.Vert(W(b - nrm, y)), i2 = kit.Vert(W(b + nrm, y)), i3 = kit.Vert(W(a + nrm, y));
                    // face up: Unity's front normal is cross(p1 - p0, p2 - p0)
                    if (Vector3.Cross(W(b - nrm, y) - W(a - nrm, y), W(b + nrm, y) - W(a - nrm, y)).y >= 0f) { kit.Tri(i0, i1, i2); kit.Tri(i0, i2, i3); }
                    else { kit.Tri(i0, i2, i1); kit.Tri(i0, i3, i2); }
                    n++;
                }
                if (n == 0) continue;
                Transform el = null; foreach (Transform c in ground) if (c.name == r.Id) { el = c; break; } // ids carry "/": Transform.Find would read a path
                if (!el) throw new System.IO.InvalidDataException($"World: road markings: road {r.Id} not built");
                nodes.Part(el, "markings", kit, Surface);
                Dashes += n; Roads++;
            }
            Debug.Log($"World: road markings {Dashes} centre-line dashes on {Roads} roads (7-17 m carriageways; the export carries no marking data)");
        }

        static Vector3 W(Vector2 p, float y) => new(p.x, y, p.y);

        static Vector2 At(Vector2[] line, float s)
        {
            for (int i = 1; i < line.Length; i++)
            {
                float l = Vector2.Distance(line[i - 1], line[i]);
                if (s <= l) return Vector2.Lerp(line[i - 1], line[i], l > 0f ? s / l : 0f);
                s -= l;
            }
            return line[^1];
        }

        static bool InOther(List<(Vector2[] line, float half, string id)> lines, string self, Vector2 p)
        {
            foreach (var (line, half, id) in lines)
            {
                if (id == self) continue;
                Poly.NearestTangent(line, p, out float d);
                if (d < half + JunctionClear) return true;
            }
            return false;
        }

        static bool InCrossing(WorldDto w, Vector2 p)
        {
            if (w.Crossings == null) return false;
            foreach (var c in w.Crossings)
            {
                if (c.Line == null || c.Line.Length < 2) continue;
                Poly.NearestTangent(c.Line, p, out float d);
                if (d < c.Width * 0.5f + CrossClear) return true;
            }
            return false;
        }

        static bool InCircus(WorldDto w, Vector2 p)
        {
            foreach (var c in w.Ground.Circuses) if (Vector2.Distance(c.Centre, p) < c.Radius + JunctionClear) return true;
            return false;
        }
    }
}
