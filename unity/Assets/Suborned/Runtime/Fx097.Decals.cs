using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Suborned
{
    /// <summary>Brief 097: URP decal projectors in the zone (Screen Space technique, Editor Decals097). Looks only; nothing in the sim reads them.
    ///  - the fountain's wet halo on the paving round the basin, and fallen petals under each flower bed, placed once;
    ///  - a shot that misses leaves a chip mark where it lands on the ground (HostFight.Shot), and kicks up dust and grit; a shot that cover takes throws
    ///    chips off the prop. The marks are a pool of 32 that fade out after a minute, oldest first, so a long fight never adds draws without end.</summary>
    public sealed partial class Fx097
    {
        Material pockMat; readonly Queue<(DecalProjector p, float at)> pocks = new(); const int MaxPocks = 32; const float PockLife = 60f;

        void Decals()
        {
            pockMat = Resources.Load<Material>("Fx097/Fx097-Decal-pock");
            var wet = Resources.Load<Material>("Fx097/Fx097-Decal-wet"); var petal = Resources.Load<Material>("Fx097/Fx097-Decal-petals");
            // both on the square's paving (groundY, measured under the square in Start): the halo's ring round the basin; fallen petals in 2.6 m patches
            // along each bed's edge (every ~2.4 m of its rim, at most 8 a bed; the bed hides its own share, so they show on the paving at its foot)
            if (wet && fountain) Ground(wet, new Vector3(centre.x, groundY - 0.12f, centre.z), new Vector3(9.4f, 9.4f, 0.32f), 0f, "fountain wet halo");
            if (petal) for (int i = 0; i < beds.Count; i++)
            {
                float hx = bedSize[i].x * 0.5f, hz = bedSize[i].y * 0.5f, rim = 4f * (hx + hz); int n = Mathf.Clamp(Mathf.RoundToInt(rim / 2.4f), 1, 8);
                for (int k = 0; k < n; k++)
                {
                    float d = (k + 0.5f) / n * rim, e = d < 2 * hx ? d : d < 2 * hx + 2 * hz ? d - 2 * hx : d < 4 * hx + 2 * hz ? d - 2 * hx - 2 * hz : d - 4 * hx - 2 * hz;
                    var off = d < 2 * hx ? new Vector3(-hx + e, 0f, -hz) : d < 2 * hx + 2 * hz ? new Vector3(hx, 0f, -hz + e) : d < 4 * hx + 2 * hz ? new Vector3(hx - e, 0f, hz) : new Vector3(-hx, 0f, hz - e);
                    Ground(petal, new Vector3(beds[i].x, groundY - 0.12f, beds[i].z) + off * 1.05f, new Vector3(2.6f, 2.6f, 0.3f), R(0f, 360f), $"petals {i}.{k}");
                }
            }
        }

        DecalProjector Ground(Material m, Vector3 at, Vector3 size, float yaw, string n)
        {
            var go = new GameObject("Fx097 decal " + n); go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(at + Vector3.up * (size.z * 0.5f), Quaternion.Euler(90f, yaw, 0f));
            var p = go.AddComponent<DecalProjector>(); p.material = m; p.size = size; p.pivot = Vector3.zero; p.drawDistance = 120f; p.fadeScale = 0.9f;
            // URP fades by angle only with rendering layers on (off here: turning them on cost the zone its lit variants), so every box is shallow
            // (about 0.3 m round the ground it marks): a planter wall or a kerb barely enters it and nothing smears down a wall
            return p;
        }

        /// <summary>A shot the engine logged, where the player sees it end (HostFight.Shot): a miss on the ground marks it and throws dust; cover takes it with chips.</summary>
        public static void Shot(Vector3 end, bool hit, bool covered)
        {
            var f = Live; if (!f || !f.sparks || hit) return;
            var grit = new Color(0.9f, 0.82f, 0.7f, 1f);
            f.Burst(end, covered ? 12 : 8, grit, covered ? 2.4f : 1.6f);
            f.Puff(end + Vector3.up * 0.05f, covered ? 3 : 4);
            if (covered || !f.pockMat) return;
            var p = f.Ground(f.pockMat, end + Vector3.down * 0.17f, new Vector3(0.62f, 0.62f, 0.3f), f.R(0f, 360f), "pock");
            f.pocks.Enqueue((p, Time.time)); if (f.pocks.Count == 1) Debug.Log($"Fx097: chip mark at {end}");
            while (f.pocks.Count > MaxPocks) { var o = f.pocks.Dequeue(); if (o.p) Destroy(o.p.gameObject); }
        }

        void PockFade()
        {
            foreach (var (p, at) in pocks) if (p) { float age = Time.time - at; p.fadeFactor = Mathf.Clamp01(1f - (age - PockLife * 0.75f) / (PockLife * 0.25f)); }
            while (pocks.Count > 0 && Time.time - pocks.Peek().at > PockLife) { var o = pocks.Dequeue(); if (o.p) Destroy(o.p.gameObject); }
        }
    }
}
