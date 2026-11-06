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
            if (wet && fountain) Ground(wet, new Vector3(centre.x, water - 0.3f, centre.z), new Vector3(9.4f, 9.4f, 1.2f), 0f, "fountain wet halo");
            if (petal) { int i = 0; foreach (var b in beds) Ground(petal, new Vector3(b.x, b.y - 0.4f, b.z) + new Vector3(R(-0.6f, 0.6f), 0f, R(-0.6f, 0.6f)), new Vector3(2.6f, 2.6f, 1.6f), R(0f, 360f), "petals " + i++); }
        }

        DecalProjector Ground(Material m, Vector3 at, Vector3 size, float yaw, string n)
        {
            var go = new GameObject("Fx097 decal " + n); go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(at + Vector3.up * (size.z * 0.5f), Quaternion.Euler(90f, yaw, 0f));
            var p = go.AddComponent<DecalProjector>(); p.material = m; p.size = size; p.pivot = Vector3.zero; p.drawDistance = 120f; p.fadeScale = 0.9f;
            p.startAngleFade = 60f; p.endAngleFade = 80f; // only on ground-facing surfaces, never smeared down a wall
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
            var p = f.Ground(f.pockMat, end + Vector3.down * 0.3f, new Vector3(0.42f, 0.42f, 0.8f), f.R(0f, 360f), "pock");
            f.pocks.Enqueue((p, Time.time));
            while (f.pocks.Count > MaxPocks) { var o = f.pocks.Dequeue(); if (o.p) Destroy(o.p.gameObject); }
        }

        void PockFade()
        {
            foreach (var (p, at) in pocks) if (p) { float age = Time.time - at; p.fadeFactor = Mathf.Clamp01(1f - (age - PockLife * 0.75f) / (PockLife * 0.25f)); }
            while (pocks.Count > 0 && Time.time - pocks.Peek().at > PockLife) { var o = pocks.Dequeue(); if (o.p) Destroy(o.p.gameObject); }
        }
    }
}
