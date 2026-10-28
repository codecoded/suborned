#if UNITY_EDITOR
using Suborned.OldTown.Sky;
using UnityEditor;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Everything that moves in the street's sky (studies: Concepts/Horizon/street_ab): the sky rail, three aircar
    /// lanes stacked by height (down the canyon under the arch, a rooftop circuit, a high crossing), a liner climbing
    /// from the north spaceport on its contrail and another settling onto a pad in the north-west. Lanes are kept
    /// clear of the landmarks (see DecoKitStreetSkyline for their footprints).
    internal static class DecoKitSkyTraffic
    {
        static readonly Vector3[] Canyon =  // 30 m: down the boulevard, through the arch, round the HQ
        {
            new(-6f, 30f, -420f), new(-6f, 30f, 150f), new(-6f, 31f, 360f), new(-30f, 33f, 430f), new(0f, 35f, 505f),
            new(30f, 33f, 430f), new(6f, 31f, 360f), new(6f, 30f, 150f), new(6f, 30f, -420f), new(0f, 30f, -470f),
        };
        static readonly Vector3[] Rooftop =  // 70 m: over the frontage, behind the HQ, clear of the sphere
        {
            new(-95f, 70f, -300f), new(-95f, 71f, -25f), new(-75f, 72f, 40f),  // Southside: between N4 / W2 and N1's towers new(-75f, 72f, 300f), new(-40f, 75f, 540f), new(40f, 75f, 540f), new(75f, 72f, 300f),
            new(75f, 70f, -300f), new(0f, 70f, -380f),
        };
        static readonly Vector3[] High =  // 130-140 m: a long crossing of the sky, south of the sphere station
        {
            new(-1000f, 130f, 120f), new(0f, 130f, 160f), new(1000f, 132f, 200f), new(1060f, 136f, 240f),
            new(0f, 140f, 200f), new(-1000f, 136f, 160f), new(-1060f, 132f, 130f),
        };

        static readonly Vector3[] Skyport =  // 44-54 m: round the skyport outside its discs, in over the parade and circus
        {
            new(122f, 46f, 150f), new(200f, 50f, 150f), new(225f, 54f, 206f), new(200f, 52f, 262f), new(122f, 48f, 262f),
            new(60f, 44f, 220f), new(0f, 44f, 206f), new(60f, 44f, 192f),
        };

        internal static void Build(DecoKitPlacer p)
        {
            var root = new GameObject("Deco Sky Traffic").transform;
            DecoKitSkyRail.Build(p, root);
            GameObject coral = p.Prefab("AirCar_Coral"), teal = p.Prefab("AirCar_Teal"), saucer = p.Prefab("AirCar_Saucer");
            Lane(root, "Lane canyon 30 m", Canyon, new[] { coral, teal }, 10, 24f, .6f, 11f);
            Lane(root, "Lane rooftop 70 m", Rooftop, new[] { teal, coral, saucer }, 8, 34f, 1.2f, 5f);
            Lane(root, "Lane high 130 m", High, new[] { saucer, coral }, 6, 55f, 2f, 17f);
            Lane(root, "Lane skyport 45 m", Skyport, new[] { coral, saucer, teal }, 6, 20f, .8f, 3f);
            var trail = Contrail();
            var ship = p.Prefab("RocketShip");
            // both in the open wedge of sky over the boulevard's axis (the canyon hides > ~10 deg off axis below ~20 deg)
            Rocket(root, "Liner ascent", ship, trail, new Vector3(180f, 0f, 1700f), false, 160f, 7000f, 20f, .5f, 50f);
            Rocket(root, "Liner landing", ship, trail, new Vector3(-260f, 0f, 1700f), true, 60f, 1500f, -20f, .15f, 15f);
            foreach (Transform t in root)
                Debug.Log($"[DecoKit] sky: {t.name} children {t.childCount}" +
                          (t.childCount > 0 ? $", first at {t.GetChild(0).position}" : ""));
        }

        static void Lane(Transform root, string name, Vector3[] route, GameObject[] models, int units, float speed,
                         float bob, float frozenAt)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var path = go.AddComponent<SkyPath>();
            path.Set(route, true);
            go.AddComponent<SkyTraffic>().Configure(path, models, units, 1, 0f, speed, 1f, bob, frozenAt);
        }

        static void Rocket(Transform root, string name, GameObject ship, Material trail, Vector3 pad, bool landing,
                           float period, float altitude, float heading, float downrange, float frozenAt)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = pad;
            go.AddComponent<RocketFlight>().Configure(ship, trail, landing, period, altitude, heading, downrange, 70f,
                                                      new Vector2(5f, 80f), frozenAt);
        }

        static Material Contrail()
        {
            EnsureFolder(GeneratedDir);
            var path = $"{GeneratedDir}/DK_Contrail.mat";
            var shader = Shader.Find("OldTown/DecoKit/Contrail");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) AssetDatabase.CreateAsset(mat = new Material(shader), path);
            else mat.shader = shader;
            mat.SetTexture("_NoiseTex", DecoKitSky.Tex("DK_Sky_Noise.png", srgb: false, TextureWrapMode.Repeat));
            mat.SetFloat("_Soft", 1.3f);       // broad, billowing body; still feathers at the silhouette
            mat.SetFloat("_Opacity", .85f);
            mat.SetFloat("_FogHeight", 500f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
