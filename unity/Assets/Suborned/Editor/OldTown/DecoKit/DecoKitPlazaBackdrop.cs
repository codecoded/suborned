#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitPlazaLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// City mass around the plaza, from the same hall modules: a frontage across the tram street (with a second
    /// entrance tower as its landmark) and a block behind the hall, screened by palms and cypress; beyond them a
    /// painted bay skyline (Concepts/AtomicAge/skyline_panorama.png) on a cylindrical card.
    internal static class DecoKitPlazaBackdrop
    {
        const float FarKerbX = L.StreetMaxX, FrontX = 46f;  // far kerb line, far frontage facade line
        const float RearZ = -36f, RowZ = -29f;              // rear block facade line, tree row
        const float WestX = L.PlazaMinX - 2f;               // west frontage facade line
        internal const float QuayZ = -52f, WaterY = -1.5f;  // bay behind the rear block, as plaza.png's upper right
        // Matte (skyline_matte.py): 5.46:1 band, shoreline at v 0.073. Span sets angular size (height = span / 5.46):
        // 75 deg puts the spires ~12 deg over the horizon, i.e. across the bay, not next door.
        const float SkyR = 300f, SkySpan = 75f, SkyAspect = 5.46f, SkyShoreV = .073f;
        static readonly Vector2 SkyFacing = new(-.25f, -1f);
        /// Painting -> screen gain, shared by the card and the gradient sky so both take the same transform.
        internal static float SkyGain => DecoKitStage.Arg("dkSkyGain", 1.5f);

        internal static void Build(DecoKitPlacer p)
        {
            FarSide(p);
            Rear(p);
            West(p);
            Skyline();
        }

        static void FarSide(DecoKitPlacer p)
        {
            for (float z = L.StreetZ.x + 1f; z < L.StreetZ.y; z += 2f)
                p.Put("Kerb_Straight", new Vector3(FarKerbX, 0f, z), -90f);
            p.Put("HallEntranceTower", new Vector3(FrontX, L.Walk, 20f), -90f);  // tower spans z 14..26
            for (float z = QuayZ + 7f; z < 110f; z += 6f)
                if (z < 11f || z > 29f) p.Put("HallBay", new Vector3(FrontX, L.Walk, z), -90f);
            for (float z = QuayZ + 6f; z < 110f; z += 12f) p.Put("CoconutPalm", new Vector3(FarKerbX + 2f, L.Walk, z), z * 11f);
        }

        /// Bays facing the plaza across its west edge, closing the square as plaza.png's right-hand frontage.
        static void West(DecoKitPlacer p)
        {
            for (float z = -31f; z < L.PlazaZ.y + 4f; z += 6f) p.Put("HallBay", new Vector3(WestX, L.Walk, z), 90f);
            for (float z = 2f; z < L.PlazaZ.y; z += 10f) p.Put("Cypress", new Vector3(WestX + 1.4f, L.Walk, z));
        }

        static void Rear(DecoKitPlacer p)
        {
            for (float x = -45f; x <= L.KerbX - 6f; x += 6f) p.Put("HallBay", new Vector3(x, L.Walk, RearZ));
            for (float x = -40f; x <= L.KerbX - 7f; x += 7f)  // stays on the hall side of the tram street
            {
                p.Put("CoconutPalm", new Vector3(x, L.Walk, RowZ), x * 23f);
                p.Put("Cypress", new Vector3(x + 3.5f, L.Walk, RowZ + 1f));
            }
        }

        /// Inward-facing cylinder arc carrying the matte, painted shoreline on the water line. Unlit alpha-blended (the
        /// painting carries its own light; its faded edges dissolve into the sky); gain lifts it back through ACES.
        static void Skyline()
        {
            const int seg = 64;
            float span = SkySpan * Mathf.Deg2Rad, h = SkyR * span / SkyAspect, y0 = WaterY + .2f - SkyShoreV * h;
            float t0 = Mathf.Atan2(SkyFacing.x, SkyFacing.y) - span * .5f;  // azimuth from +Z toward +X = viewer's right
            var v = new Vector3[(seg + 1) * 2];
            var uv = new Vector2[v.Length];
            var tri = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float u = i / (float)seg, t = t0 + span * u;
                var d = new Vector3(Mathf.Sin(t), 0f, Mathf.Cos(t)) * SkyR;
                v[i * 2] = d + Vector3.up * y0;
                v[i * 2 + 1] = d + Vector3.up * (y0 + h);
                uv[i * 2] = new Vector2(u, 0f);
                uv[i * 2 + 1] = new Vector2(u, 1f);
                if (i == seg) continue;
                int b = i * 2;  // clockwise seen from the centre: TL, BR, BL / TL, TR, BR
                System.Array.Copy(new[] { b + 1, b + 2, b, b + 1, b + 3, b + 2 }, 0, tri, i * 6, 6);
            }
            var mesh = new Mesh { name = "DK_Skyline_Card", vertices = v, uv = uv, triangles = tri };
            mesh.RecalculateNormals();
            var path = $"{GeneratedDir}/DK_Skyline_Card.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            // rgb only: alpha > 1 would subtract the sky behind the card
            var mat = DecoKitStage.Unlit("DK_Skyline_Bay", new Color(SkyGain, SkyGain, SkyGain, 1f), twoSided: false);
            mat.SetTexture("_BaseMap", DecoKitTextures.Backdrop("Backdrop/DK_Skyline_Bay.png"));
            mat.SetFloat("_Surface", 1f);  // transparent, alpha blend
            mat.SetFloat("_Blend", 0f);
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(mat);
            var go = new GameObject("Skyline card", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ReflectionProbeStatic);  // chrome reflects the city
        }
    }
}
#endif
