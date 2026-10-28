#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;
using G = Suborned.OldTown.Editor.DecoKit.DecoKitGroundMesh;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The harbour that closes the east half of the scene (studies: Concepts/District aerial_harbour, parade_eye): an
    /// upper terrace of flags in front of the blocks' sea walls, five steps down to a quay promenade along the whole
    /// coast, a granite-coped quay wall, piers on piles to the liner dock and the skyport, and the sea to the horizon
    /// (DK_Ocean: sky reflection, sun glitter, the horizon haze where it meets the far band).
    internal static class DecoKitHarbour
    {
        const float Far = 1500f, Sea = 4000f, Riser = .2f, Tread = 1.2f, SkyportApron = 22f;

        internal static void Build(DecoKitPlacer p)
        {
            var root = new GameObject("Harbour").transform;
            float w = L.Walk, lo = P.PromenadeY, z0 = P.HarbourZ.x, z1 = P.HarbourZ.y;
            var flags = G.Metric("DK_Pave_Flags_Metric", "DK_Pave_Flags", 4f);
            var stone = G.Mat("DK_Limestone_Tex");
            new G().Rect(Rect.MinMaxRect(P.EastBack, z0, P.StepsX, z1), w).Emit("Harbour terrace", flags, true, root);

            // steps inside the harbour's run, a plain retaining wall beyond it; the promenade the whole coast
            var steps = new G();
            int n = Mathf.RoundToInt((w - lo) / Riser);
            for (int k = 0; k < n; k++)
                steps.Block(Rect.MinMaxRect(P.StepsX + k * Tread, z0, P.StepsX + (k + 1) * Tread, z1), lo - .5f, w - (k + 1) * Riser);
            steps.Face(new Vector2(P.StepsX, -Far), new Vector2(P.StepsX, z0), lo, w)
                 .Face(new Vector2(P.StepsX, z1), new Vector2(P.StepsX, Far), lo, w)
                 .Rect(Rect.MinMaxRect(P.StepsX, -Far, P.PromenadeX, z0), lo)
                 .Rect(Rect.MinMaxRect(P.StepsX, z1, P.PromenadeX, Far), lo)
                 .Face(new Vector2(P.StepsX, z0), new Vector2(P.PromenadeX, z0), lo, w)   // ends of the flight
                 .Face(new Vector2(P.PromenadeX, z1), new Vector2(P.StepsX, z1), lo, w);
            steps.Emit("Harbour steps", stone, true, root);
            new G().Rect(Rect.MinMaxRect(P.PromenadeX, -Far, P.QuayX - .6f, Far), lo).Emit("Quay promenade", flags, true, root);
            new G().Rect(Rect.MinMaxRect(P.QuayX - .6f, -Far, P.QuayX, Far), lo + .05f)
                   .Face(new Vector2(P.QuayX - .6f, Far), new Vector2(P.QuayX - .6f, -Far), lo, lo + .05f)  // faces the promenade
                   .Face(new Vector2(P.QuayX, -Far), new Vector2(P.QuayX, Far), P.WaterY - 3f, lo + .05f)   // faces the sea
                   .Emit("Quay wall", G.Mat("DK_Granite_Kerb"), true, root);

            Pier(root, "Market pier", P.MarketPierZ, P.QuayX, 236f);
            Pier(root, "Skyport pier", P.SkyportPierZ, P.QuayX, P.Skyport.x - SkyportApron);
            new G().Rect(Rect.MinMaxRect(P.QuayX - 1f, -Sea, Sea, Sea), P.WaterY).Emit("Sea", Ocean(), false, root);

            p.Put("Skyport_Tower", P.Skyport, 180f);  // skyport.py: apron edge on its -X (Unity +X), turned to the pier
            Park(p);
            for (float z = z0 + 6f; z < z1; z += 16f)  // mooring bollards on the coping, clear of the piers
                if (!Within(z, P.MarketPierZ) && !Within(z, P.SkyportPierZ)) p.Put("Bollard", new Vector3(P.QuayX - .9f, lo + .05f, z));
        }

        static bool Within(float z, Vector2 span) => z > span.x - 3f && z < span.y + 3f;

        // skyport.py PADS (Blender x, y, deck top): turned 180 about the stem, Blender (x, y) lands on world (x, z)
        static readonly (Vector3 at, string car)[] Pads =
        {
            (new(10f, 38f, -17.32f), "AirCar_Coral"), (new(-15.97f, 62f, 5.81f), "AirCar_Teal"),
            (new(13.16f + .94f, 84f, 4.79f + .34f), "AirCar_Saucer"),  // pad 3 is tight: 1 m out along its azimuth
        };

        /// Aircars parked on the landing discs (their origin is the body centre: deck + 0.6).
        static void Park(DecoKitPlacer p)
        {
            if (!p.Has("Skyport_Tower")) return;
            foreach (var (at, car) in Pads)
                p.Put(car, P.Skyport + at + Vector3.up * .6f, Mathf.Atan2(at.x, at.z) * Mathf.Rad2Deg + 90f);
        }

        /// Deck at the promenade level on two rows of piles every 10 m.
        static void Pier(Transform root, string name, Vector2 z, float x0, float x1)
        {
            float top = P.PromenadeY;
            new G().Block(Rect.MinMaxRect(x0, z.x, x1, z.y), top - .7f, top).Emit(name, G.Mat("DK_Limestone_Tex"), true, root);
            var piles = new DecoKitMassMesh(1);
            for (float x = x0 + 5f; x < x1; x += 10f)
                foreach (var pz in new[] { z.x + 1f, z.y - 1f })
                    piles.Drum(new Vector3(x, 0f, pz), .55f, P.WaterY - 6f, top - .7f, 12, 0, false);
            var go = new GameObject($"{name} piles", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            var mesh = piles.Build($"{name}_piles");
            var path = $"{GeneratedDir}/{name.Replace(' ', '_')}_piles.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = G.Mat("DK_Stone_Dark");
        }

        internal static Material Ocean() => Water("DK_Ocean", new Vector4(42f, 11f, .55f, 1f), new Vector2(80f, 1600f));

        /// DecoKit/Ocean with its waves (x tile A m, y tile B m, z slope, w speed) and slope fade (start, end m):
        /// the sea, or a garden basin stilled almost to a mirror.
        internal static Material Water(string name, Vector4 waves, Vector2 fade)
        {
            EnsureFolder(GeneratedDir);
            var path = $"{GeneratedDir}/{name}.mat";
            var shader = Shader.Find("OldTown/DecoKit/Ocean");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) AssetDatabase.CreateAsset(mat = new Material(shader), path);
            else mat.shader = shader;
            mat.SetTexture("_WaveTex", DecoKitSky.Tex("DK_Sea_Normal.png", srgb: false, TextureWrapMode.Repeat));
            mat.SetColor("_Deep", new Color(.05f, .32f, .42f));     // sRGB albedo: teal-blue body, lit by sun + sky
            mat.SetColor("_Scatter", new Color(.1f, .55f, .5f));    // crests glowing toward the sun
            mat.SetVector("_Sky", new Vector4(.45f, .55f, 0f, 0f)); // reflection lift toward the zenith, Fresnel cap
            mat.SetVector("_Waves", waves);
            mat.SetVector("_Fade", fade);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
