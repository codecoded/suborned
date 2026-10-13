using UnityEditor;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>The built district as a grid of roof heights (2 m cells: the highest geometry over each cell, 0 = open ground),
    /// read from the scene's meshes as Rig.Clearance reads them: generated batches per triangle, kit pieces by their renderer
    /// bounds; the horizon band and the sea are left out. The scene has no colliders, so the review camera asks this instead:
    /// StandOff is where a play camera that moves stands so that nothing inside its frame is nearer than its near plane, Hit is
    /// what a view ray lands on. A cell counts as solid from the street to its highest point, so a deck or a skybridge clears
    /// like a block of its height. One difference from the builder's fit (Rig.StandOff): the builder stands clear of a kit
    /// piece's whole bounds once they touch the frame column (206 m under SpaceHQ); this clears only what is inside the frame,
    /// so under a tower it stands nearer. The orthographic picture is the same, and the street band's shadow cascade covers
    /// the same ground: TacticalShadows is fitted from whichever stand-off the camera has.</summary>
    public sealed class HeightField
    {
        public const float Cell = 2f;
        /// <summary>Geometry no higher than this is ground (kerbs 0.18 m, tram platforms 0.3 m).</summary>
        const float Flat = 0.5f;
        /// <summary>Clearance in front of the nearest roof (Rig's StandMargin) plus one cell of grid slack.</summary>
        const float Margin = 5f + Cell;
        /// <summary>Stand-offs are rounded up to this, so the shadow fit steps as roofs enter the frame instead of changing every frame.</summary>
        const float Quantum = 8f;

        /// <summary>Plan extent covered (x east, y = Unity z north).</summary>
        public readonly Rect Area;
        readonly int nx, nz;
        readonly float[] top;

        public float Highest { get; private set; }
        /// <summary>Meshes read per triangle, and renderers read by their bounds (kit pieces; or batches Unity has already combined).</summary>
        public int Meshes { get; private set; }
        public int Boxes { get; private set; }

        public HeightField(Rect area, Transform horizon, Transform sea)
        {
            Area = area;
            nx = Mathf.CeilToInt(area.width / Cell); nz = Mathf.CeilToInt(area.height / Cell);
            top = new float[nx * nz];
            foreach (var mf in Object.FindObjectsByType<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (!mesh || !mf.TryGetComponent<MeshRenderer>(out var r) || !r.enabled || !mf.gameObject.activeInHierarchy) continue;
                if ((horizon && mf.transform.IsChildOf(horizon)) || (sea && mf.transform.IsChildOf(sea))) continue;
                var b = r.bounds;
                if (b.max.y <= Flat || b.min.x > area.xMax || b.max.x < area.xMin || b.min.z > area.yMax || b.max.z < area.yMin) continue;
                if (AssetDatabase.Contains(mesh) || !mesh.isReadable) { Box(b); Boxes++; continue; }
                var v = mesh.vertices; var m = mf.transform.localToWorldMatrix;
                if (!m.isIdentity) for (int i = 0; i < v.Length; i++) v[i] = m.MultiplyPoint3x4(v[i]);
                var t = mesh.triangles;
                for (int i = 0; i < t.Length; i += 3) Triangle(v[t[i]], v[t[i + 1]], v[t[i + 2]]);
                Meshes++;
            }
            foreach (var h in top) if (h > Highest) Highest = h;
        }

        /// <summary>Roof height over a plan point (0 on open ground and outside the area).</summary>
        public float At(float x, float z)
        {
            int c = Mathf.FloorToInt((x - Area.xMin) / Cell), r = Mathf.FloorToInt((z - Area.yMin) / Cell);
            return c < 0 || r < 0 || c >= nx || r >= nz ? 0f : top[r * nx + c];
        }

        /// <summary>Distance from the focus back along the view at which an orthographic camera of half-size halfW × halfH clears every
        /// roof in its frame column (the frame rectangle about the focus, extended toward the camera), with Rig's margin. With u across
        /// the frame, v along the view on the ground and t the height over the focus, a point is in frame while |u| ≤ halfW and
        /// |v·sin + t·cos| ≤ halfH, and lies t·sin − v·cos in front of the focus; each cell counts up to its roof or the frame's top edge.
        /// Open ground is not in the grid (a cell at 0 is no roof), so the street itself is counted first: at the frame's lower
        /// edge (t = 0, v = −halfH / sin) it lies halfH·cos / sin in front of the focus, 17.9 m at play zoom, more than the least
        /// stand-off (halfH); without it the near plane cut the foot of the frame over open ground.</summary>
        public float StandOff(Quaternion rot, Vector3 focus, float halfW, float halfH)
        {
            var fwd = rot * Vector3.forward;
            float sin = -fwd.y, cos = Mathf.Sqrt(fwd.x * fwd.x + fwd.z * fwd.z);
            if (sin < 0.05f || cos < 0.05f) throw new System.ArgumentException("HeightField.StandOff: needs a view pitched between level and straight down");
            var g = new Vector2(fwd.x, fwd.z) / cos; var s = new Vector2(g.y, -g.x);
            float reach = Highest - focus.y, step = Cell * 0.5f;
            float need = halfH * cos / sin; // the street itself, where it meets the frame's lower edge
            for (float v = -(halfH + Mathf.Max(reach, 0f) * cos) / sin; ; v += step)
            {
                float cap = (halfH - v * sin) / cos, floor = Mathf.Max(0f, (-halfH - v * sin) / cos);
                if (cap < 0f) break; // past the frame's far ground edge: everything further is above the frame
                for (float u = -halfW - Cell; u <= halfW + Cell; u += step)
                {
                    float t = At(focus.x + s.x * u + g.x * v, focus.z + s.y * u + g.y * v) - focus.y;
                    if (t > floor) need = Mathf.Max(need, Mathf.Min(t, cap) * sin - v * cos);
                }
            }
            return Mathf.Max(Mathf.Ceil((need + Margin) / Quantum) * Quantum, halfH);
        }

        /// <summary>Where a view ray first meets a roof, a wall or the street datum, marching in half-cell steps. An eye that starts
        /// under a roof (a deck, a canopy, inside a mass) first walks out from under it.</summary>
        public bool Hit(Ray ray, float reach, out Vector3 point)
        {
            bool clear = false;
            for (float d = 0f; d <= reach; d += Cell * 0.5f)
            {
                var p = ray.GetPoint(d);
                if (p.y <= 0f) { point = p; return true; }
                if (p.y > At(p.x, p.z)) { clear = true; if (p.y > Highest && ray.direction.y >= 0f) break; continue; }
                if (clear) { point = p; return true; }
            }
            point = default;
            return false;
        }

        void Box(Bounds b)
        {
            int c0 = Mathf.Max(0, Col(b.min.x)), c1 = Mathf.Min(nx - 1, Col(b.max.x)), r0 = Mathf.Max(0, Row(b.min.z)), r1 = Mathf.Min(nz - 1, Row(b.max.z));
            for (int r = r0; r <= r1; r++) Fill(r, c0, c1, b.max.y);
        }

        /// <summary>Marks every cell the triangle's plan projection touches (row by row: its extent across each row of cells) with its highest point.</summary>
        void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            float h = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            if (h <= Flat) return;
            int r0 = Mathf.Max(0, Row(Mathf.Min(a.z, Mathf.Min(b.z, c.z)))), r1 = Mathf.Min(nz - 1, Row(Mathf.Max(a.z, Mathf.Max(b.z, c.z))));
            for (int r = r0; r <= r1; r++)
            {
                float lo = Area.yMin + r * Cell, hi = lo + Cell, x0 = float.MaxValue, x1 = float.MinValue;
                Span(a, b, lo, hi, ref x0, ref x1); Span(b, c, lo, hi, ref x0, ref x1); Span(c, a, lo, hi, ref x0, ref x1);
                if (x0 <= x1) Fill(r, Mathf.Max(0, Col(x0)), Mathf.Min(nx - 1, Col(x1)), h);
            }
        }

        /// <summary>Widens [x0, x1] by the part of edge pq that lies in the row lo ≤ z ≤ hi.</summary>
        static void Span(Vector3 p, Vector3 q, float lo, float hi, ref float x0, ref float x1)
        {
            if (p.z > q.z) (p, q) = (q, p);
            if (q.z < lo || p.z > hi) return;
            float dz = q.z - p.z;
            float xa = dz > 1e-6f ? Mathf.Lerp(p.x, q.x, (lo - p.z) / dz) : p.x, xb = dz > 1e-6f ? Mathf.Lerp(p.x, q.x, (hi - p.z) / dz) : q.x; // Lerp clamps to the edge
            x0 = Mathf.Min(x0, Mathf.Min(xa, xb)); x1 = Mathf.Max(x1, Mathf.Max(xa, xb));
        }

        void Fill(int r, int c0, int c1, float h)
        {
            for (int i = r * nx + c0, end = r * nx + c1; i <= end; i++) if (top[i] < h) top[i] = h;
        }

        int Col(float x) => Mathf.FloorToInt((x - Area.xMin) / Cell);
        int Row(float z) => Mathf.FloorToInt((z - Area.yMin) / Cell);
    }
}
