using UnityEngine;

namespace Suborned
{
    /// <summary>Brief 096, Redline's render audit item 2 option B (Sky 2026-11-05: "looks worth it"): the tallest geometry on a plan grid, so the
    /// orthographic play camera can stand back only as far as the roofs in its frame column need. Each cell keeps the highest top (world y) of what
    /// stands on it: a renderer's bounds, or a large mesh's triangles one by one (a merged tile or a skyline band would otherwise raise a whole
    /// footprint). Need() answers, for a frame (focus on the ground, yaw, pitch, half width and height in metres), how far back along the view from
    /// the focus the nearest geometry inside the frame's column lies: an orthographic camera standing that far back (plus the near plane and a margin)
    /// clips nothing, and the picture is the same at any stand-off. Pure geometry: no Unity objects, no allocation per query.</summary>
    public sealed class StandOffField
    {
        public readonly float Cell;
        readonly float x0, z0;
        readonly int nx, nz;
        readonly float[] top;
        /// <summary>The highest top anywhere (m, world y; 0 when empty).</summary>
        public float Max { get; private set; }
        /// <summary>Cells the last query visited.</summary>
        public int Visited { get; private set; }

        public StandOffField(float cell, float minX, float minZ, float maxX, float maxZ)
        {
            Cell = Mathf.Max(1f, cell); x0 = minX; z0 = minZ;
            nx = Mathf.Max(1, Mathf.CeilToInt((maxX - minX) / Cell)); nz = Mathf.Max(1, Mathf.CeilToInt((maxZ - minZ) / Cell));
            top = new float[nx * nz];
        }

        int Ix(float x) => Mathf.Clamp(Mathf.FloorToInt((x - x0) / Cell), 0, nx - 1);
        int Iz(float z) => Mathf.Clamp(Mathf.FloorToInt((z - z0) / Cell), 0, nz - 1);
        bool Outside(float minX, float minZ, float maxX, float maxZ) => maxX < x0 || maxZ < z0 || minX > x0 + nx * Cell || minZ > z0 + nz * Cell;

        /// <summary>Raises every cell under the box's plan footprint to its top.</summary>
        public void AddBox(Bounds b) => Raise(b.min.x, b.min.z, b.max.x, b.max.z, b.max.y);

        /// <summary>Raises the cells under the triangle's plan box to its highest corner.</summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c) =>
            Raise(Mathf.Min(a.x, Mathf.Min(b.x, c.x)), Mathf.Min(a.z, Mathf.Min(b.z, c.z)), Mathf.Max(a.x, Mathf.Max(b.x, c.x)), Mathf.Max(a.z, Mathf.Max(b.z, c.z)), Mathf.Max(a.y, Mathf.Max(b.y, c.y)));

        void Raise(float minX, float minZ, float maxX, float maxZ, float y)
        {
            if (y <= 0f || Outside(minX, minZ, maxX, maxZ)) return;
            int ax = Ix(minX), bx = Ix(maxX), az = Iz(minZ), bz = Iz(maxZ);
            for (int z = az; z <= bz; z++) for (int x = ax; x <= bx; x++) { int i = z * nx + x; if (y > top[i]) top[i] = y; }
            if (y > Max) Max = y;
        }

        /// <summary>The top (m) of the cell under a plan point; 0 off the grid.</summary>
        public float TopAt(float x, float z) => Outside(x, z, x, z) ? 0f : top[Iz(z) * nx + Ix(x)];

        /// <summary>The highest cell: its centre (x, top, z).</summary>
        public Vector3 Highest()
        {
            int best = 0; for (int i = 1; i < top.Length; i++) if (top[i] > top[best]) best = i;
            return new Vector3(x0 + (best % nx + 0.5f) * Cell, top[best], z0 + (best / nx + 0.5f) * Cell);
        }

        /// <summary>How far back along the view (m, from the focus) the nearest geometry in the frame's column lies, the column grown by `margin` m on
        /// every side (a look-ahead for panning, turning and zooming). The camera looks down `pitch` degrees at compass `yaw` (Unity's Euler(pitch, yaw, 0)).
        /// A cell is a vertical prism from the ground to its top; its nearest point that projects inside the frame is the highest one still in frame,
        /// taken at the cell's edge nearest the camera. The ground itself (the frame's lower edge) is the least it returns.</summary>
        public float Need(Vector3 focus, float yaw, float pitch, float halfW, float halfH, float margin)
        {
            float sp = Mathf.Sin(pitch * Mathf.Deg2Rad), cp = Mathf.Cos(pitch * Mathf.Deg2Rad);
            if (sp < 0.05f) sp = 0.05f;
            float sy = Mathf.Sin(yaw * Mathf.Deg2Rad), cy = Mathf.Cos(yaw * Mathf.Deg2Rad);
            float fx = sy, fz = cy, rx = cy, rz = -sy; // the view and the screen's right, on the plan
            float rho = Cell * 0.70711f, w = halfW + margin + rho, hh = halfH + margin;
            float need = hh * cp / sp; // the ground at the frame's lower edge
            float vMax = hh / sp + rho, vMin = -(Max * cp + hh) / sp - rho;
            // the plan box round the column (u across the screen, v along the view)
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity, minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
            for (int k = 0; k < 4; k++)
            {
                float u = (k & 1) == 0 ? -w : w, v = (k & 2) == 0 ? vMin : vMax;
                float x = focus.x + u * rx + v * fx, z = focus.z + u * rz + v * fz;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
            }
            Visited = 0;
            if (Outside(minX, minZ, maxX, maxZ)) return need;
            int ax = Ix(minX), bx = Ix(maxX), az = Iz(minZ), bz = Iz(maxZ);
            for (int iz = az; iz <= bz; iz++)
            {
                float qz = z0 + (iz + 0.5f) * Cell - focus.z;
                for (int ix = ax; ix <= bx; ix++)
                {
                    float h = top[iz * nx + ix]; Visited++;
                    if (h <= 0f) continue;
                    float qx = x0 + (ix + 0.5f) * Cell - focus.x;
                    float u = qx * rx + qz * rz; if (u > w || u < -w) continue;
                    float v = qx * fx + qz * fz, vNear = v - rho; // the cell's edge nearest the camera
                    if (vNear * sp > hh || (v + rho) * sp + h * cp < -hh) continue; // wholly above or below the frame
                    float hs = Mathf.Min(h, (hh - vNear * sp) / cp); // the highest point still in frame
                    float d = hs * sp - vNear * cp;
                    if (d > need) need = d;
                }
            }
            return need;
        }
    }
}
