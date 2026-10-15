using System;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The engine's frame to Unity's: the engine works in the civic block frame (integer cm, x south, y east; sim/data/handover.site.json);
    /// the plan places that frame by plan.civic.blockToPlan (metres); Unity x = plan x, Unity z = plan y. Ground heights are baked on a 1 m
    /// grid over the site window from the export's plot slabs (PlayInstall), so a capsule stands on the pavement, not in it.</summary>
    [Serializable]
    public sealed class SiteFrame
    {
        public Vector2 origin = new(-130f, -230f), axisX = new(0f, -1f), axisY = new(1f, 0f);
        public float x0 = -52f, y0 = -66f, cell = 1f; public int nx, ny;
        public float[] heights = Array.Empty<float>();

        public Vector2 ToPlan(float bx, float by) => origin + bx * axisX + by * axisY;

        public Vector2 ToBlock(Vector2 plan)
        {
            var d = plan - origin; float det = axisX.x * axisY.y - axisY.x * axisX.y;
            return new Vector2((d.x * axisY.y - axisY.x * d.y) / det, (axisX.x * d.y - d.x * axisX.y) / det);
        }

        public float Height(float bx, float by)
        {
            int i = Mathf.FloorToInt((bx - x0) / cell), j = Mathf.FloorToInt((by - y0) / cell);
            return i < 0 || j < 0 || i >= nx || j >= ny || heights.Length != nx * ny ? 0f : heights[i * ny + j];
        }

        /// <summary>An engine point (cm) on the ground, in Unity world space.</summary>
        public Vector3 World(Pt p)
        {
            float bx = p.X / 100f, by = p.Y / 100f; var plan = ToPlan(bx, by);
            return new Vector3(plan.x, Height(bx, by), plan.y);
        }

        /// <summary>A Unity world point to the engine's integer cm.</summary>
        public Pt Engine(Vector3 w) { var b = ToBlock(new Vector2(w.x, w.z)); return new Pt(Mathf.RoundToInt(b.x * 100f), Mathf.RoundToInt(b.y * 100f)); }

        /// <summary>A direction in the engine frame (any length) as a Unity ground direction (unit).</summary>
        public Vector3 Dir(Pt f) { var d = f.X / 100f * axisX + f.Y / 100f * axisY; return new Vector3(d.x, 0f, d.y).normalized; }

        /// <summary>Where a screen ray meets the ground (two passes over the baked heights).</summary>
        public bool Ground(Ray r, out Vector3 hit)
        {
            hit = default; float h = 0f;
            for (int k = 0; k < 2; k++)
            {
                if (Mathf.Abs(r.direction.y) < 1e-4f) return false;
                float t = (h - r.origin.y) / r.direction.y; if (t < 0f) return false;
                hit = r.origin + r.direction * t;
                var b = ToBlock(new Vector2(hit.x, hit.z)); h = Height(b.x, b.y);
            }
            return true;
        }
    }
}
