#if UNITY_EDITOR
using UnityEngine;
using G = Suborned.OldTown.Editor.DecoKit.DecoKitGroundMesh;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Orbit Gardens, west of the frontage (studies: Concepts/District aerial_circus): the circus's west gate opens on
    /// a terrazzo promenade to a raised round basin; a long raised canal runs south from it between cypress walks to
    /// the west lane's path; a pavilion with café tables looks over the canal. Basins stand proud of the lawn (coping
    /// walls), so every surface layers on the ground without cutting it.
    internal static class DecoKitGardens
    {
        const float CanalX = -78f, CanalHalf = 3f, PoolR = 12f, Coping = .6f, Rim = .45f, WaterDrop = .1f;
        static Vector2 Pool => new(CanalX, P.Circus.z);  // on the gate axis

        internal static void Build(DecoKitPlacer p)
        {
            var root = new GameObject("Orbit Gardens").transform;
            float w = L.Walk, x0 = P.GardensX.x, x1 = P.GardensX.y, z0 = P.GardensZ.x, z1 = P.GardensZ.y;
            float canal0 = P.WestLane.x - 6f, canal1 = Pool.y - PoolR - Coping - 4f;
            new G().Rect(Rect.MinMaxRect(x0, z0, x1, z1), w - .003f).Emit("Gardens lawn", G.Lawn(), true, root);

            // terrazzo: the gate promenade, the ring round the basin, the canal walks, the west lane's path
            new G().Rect(Rect.MinMaxRect(x0, Pool.y - 6f, x1, Pool.y + 6f), w + .002f)
                   .Annulus(Pool, PoolR, PoolR + 5f, w + .002f)
                   .Rect(Rect.MinMaxRect(CanalX - CanalHalf - 5f, canal0, CanalX - CanalHalf - .6f, canal1 + 4f), w + .002f)
                   .Rect(Rect.MinMaxRect(CanalX + CanalHalf + .6f, canal0, CanalX + CanalHalf + 5f, canal1 + 4f), w + .002f)
                   .Rect(Rect.MinMaxRect(CanalX + CanalHalf + 5f, P.WestLane.x - 1f, x1, P.WestLane.y + 1f), w + .002f)
                   .Emit("Gardens paths", G.Mat("DK_Terrazzo_Ivory"), false, root);

            // raised basins: granite coping, water a hand below it
            float top = w + Rim, water = top - WaterDrop;
            var canal = Rect.MinMaxRect(CanalX - CanalHalf, canal0, CanalX + CanalHalf, canal1);
            var rim = Rect.MinMaxRect(canal.xMin - Coping, canal.yMin - Coping, canal.xMax + Coping, canal.yMax + Coping);
            new G().Block(Rect.MinMaxRect(rim.xMin, rim.yMin, canal.xMin, rim.yMax), w - .01f, top)
                   .Block(Rect.MinMaxRect(canal.xMax, rim.yMin, rim.xMax, rim.yMax), w - .01f, top)
                   .Block(Rect.MinMaxRect(canal.xMin, rim.yMin, canal.xMax, canal.yMin), w - .01f, top)
                   .Block(Rect.MinMaxRect(canal.xMin, canal.yMax, canal.xMax, rim.yMax), w - .01f, top)
                   .Annulus(Pool, PoolR, PoolR + Coping, top)
                   .Wall(Pool, PoolR + Coping, w - .01f, top, true)
                   .Wall(Pool, PoolR, water - .2f, top, false)
                   .Emit("Gardens basin coping", G.Mat("DK_Granite_Kerb"), true, root);
            var still = DecoKitHarbour.Water("DK_PoolWater", new Vector4(9f, 2.6f, .05f, .25f), new Vector2(15f, 250f));
            new G().Rect(canal, water).Disc(Pool, PoolR, water).Emit("Gardens water", still, false, root);
            p.Put("PlazaFountain", new Vector3(Pool.x, water, Pool.y));

            Plant(p, canal0, canal1);
            // the pavilion west of the canal, café tables on its terrace; a coffee cart at the gate
            p.Put("DecoPavilion", new Vector3(-101f, w, 96f), 90f);
            for (int k = 0; k < 4; k++) p.Put("CafeSet", new Vector3(-93.5f, w, 88f + 5.5f * k), k * 41f);
            p.Put("CoffeeCart", new Vector3(x1 - 5f, w, Pool.y + 8.5f), 180f);
        }

        /// Cypress walks along the canal, palms round the basin, benches facing the water, lamps along the paths,
        /// shrub mounds and flower beds seeded over the open lawns (clear of paths and basins).
        static void Plant(DecoKitPlacer p, float canal0, float canal1)
        {
            float w = L.Walk;
            for (float z = canal0 + 4f; z < canal1; z += 8f)
                foreach (var s in new[] { -1f, 1f })
                {
                    p.Put("Cypress", new Vector3(CanalX + s * (CanalHalf + 7f), w, z), z * 13f);
                    if (Mathf.Repeat(z - canal0 - 4f, 24f) < .1f)
                        p.Put("Bench", new Vector3(CanalX + s * (CanalHalf + 4.2f), w, z + 4f), s < 0f ? 90f : -90f);
                    if (Mathf.Repeat(z - canal0 - 12f, 24f) < .1f)
                        p.Put("PlazaSphereLamp", new Vector3(CanalX + s * (CanalHalf + 5.6f), w, z), 0f);
                }
            for (int k = 0; k < 10; k++)
            {
                float phi = 18f + 36f * k;
                if (Mathf.Abs(Mathf.DeltaAngle(phi, 90f)) < 20f || Mathf.Abs(Mathf.DeltaAngle(phi, 270f)) < 20f
                    || Mathf.Abs(Mathf.DeltaAngle(phi, 180f)) < 20f) continue;  // gate axis and canal
                p.Put("FanPalm", new Vector3(Pool.x, w, Pool.y) + P.Dir(phi) * (PoolR + 7.5f), phi * 3f);
            }
            var rng = new System.Random(7);
            for (int placed = 0, tries = 0; placed < 60 && tries < 2000; tries++)
            {
                var at = new Vector2(Mathf.Lerp(P.GardensX.x + 3f, P.GardensX.y - 3f, (float)rng.NextDouble()),
                                     Mathf.Lerp(P.GardensZ.x + 3f, P.GardensZ.y - 3f, (float)rng.NextDouble()));
                if (Mathf.Abs(at.x - CanalX) < CanalHalf + 11f && at.y < canal1 + 6f) continue;            // canal walks
                if (Mathf.Abs(at.y - Pool.y) < 9f || Vector2.Distance(at, Pool) < PoolR + 12f) continue;  // gate, basin
                if (at.y > P.WestLane.x - 3f && at.y < P.WestLane.y + 3f && at.x > CanalX) continue;       // lane path
                if (Vector2.Distance(at, new Vector2(-98f, 96f)) < 16f) continue;                           // pavilion
                p.Put(rng.NextDouble() < .6 ? "ShrubMound" : "FlowerBed", new Vector3(at.x, w, at.y), (float)rng.NextDouble() * 360f);
                placed++;
            }
        }
    }
}
#endif
