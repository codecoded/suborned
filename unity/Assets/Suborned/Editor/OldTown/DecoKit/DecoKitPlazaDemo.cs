#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;
using B = Suborned.OldTown.Editor.DecoKit.DecoKitPlazaBackdrop;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitPlazaLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Walkable recreation of ref/plaza.png: sunny civic plaza, tram street, hall frontage. Blue procedural
    /// sky (chrome reflects sky + plaza, as in the reference), baked box-projected probes, light haze.
    internal static class DecoKitPlazaDemo
    {
        const string SceneName = "DecoKitPlaza";
        const float Far = 1500f;  // ground extent: well past fog end
        static readonly Vector3 HeroPos = new(10f, 22f, 58f), HeroTarget = new(-2f, 9f, 0f);  // plaza.png framing: street left, skyline in

        // Tunables (batch: -dkExposure -dkSun -dkFill -dkSaturation -dkOut); defaults match ref/plaza.png.
        static float Exposure => DecoKitStage.Arg("dkExposure", -.8f);
        static float SunIntensity => DecoKitStage.Arg("dkSun", 3f);
        static float Fill => DecoKitStage.Arg("dkFill", 1f);  // ambient multiplier

        internal static void Build()
        {
            DecoKitStage.NewScene(SceneName);
            Sky();
            DecoKitStage.Directional("Sun", new Vector3(.55f, -.72f, -.42f), new Color(1f, .93f, .8f), SunIntensity, true);
            DecoKitStage.Grade("DK_PlazaGrade", TonemappingMode.ACES, Exposure, 10f, DecoKitStage.Arg("dkSaturation", 10f));
            var root = new GameObject("Deco Plaza").transform;
            var placer = new DecoKitPlacer(root);
            L.Build(placer);
            DecoKitPlazaBackdrop.Build(placer);
            Ground();
            placer.ReportTop("Tile_Plain", L.Walk);
            Probes();
            DecoKitStage.BakeEnvironment(SceneName);
            var hero = Hero();
            var shot = Path.Combine(CaptureDir, DecoKitStage.Arg("dkOut", "unity_plaza") + ".png");
            DecoKitStage.WithShadows(160f, () => DecoKitStage.Capture(hero, 1920, 1080, shot));
            hero.enabled = false;  // Play uses the walker; the hero stays for re-captures
            var walker = DecoKitWalker.Make(new Vector3(6f, L.Walk, 44f), new Vector3(-2f, 6f, 0f));
            var eye = walker.GetComponentInChildren<Camera>();  // eye-level proof shot: what Play mode opens on
            DecoKitStage.WithShadows(160f, () => DecoKitStage.Capture(eye, 1920, 1080, Path.Combine(CaptureDir, "unity_plaza_walk.png")));
            EditorSceneManager.SaveOpenScenes();
            var missing = placer.Missing.Count == 0 ? "none" : string.Join(", ", placer.Missing.OrderBy(n => n));
            Debug.Log($"[DecoKit] plaza built; missing prefabs: {missing}");
        }

        /// Painted gradient sky at the skyline card's gain; fog light enough that the card (300 m) keeps its painting.
        static void Sky() => DecoKitAtmosphere.PaintedSky("Plaza", "Backdrop/DK_Sky_Gradient.png",
            DecoKitAtmosphere.PlazaAmbient, B.SkyGain, Fill, 150f, 900f);

        /// Road deck between the kerbs and limestone either side, all stopping at the quay; the bay beyond is a
        /// mirror-smooth plane that reflects the sky out to the painted shoreline.
        static void Ground()
        {
            var asphalt = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/DK_Asphalt.mat");
            var stone = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/DK_Limestone_Tex.mat");
            DecoKitStage.Slab("Road deck", Rect.MinMaxRect(L.KerbX, B.QuayZ, L.StreetMaxX, Far), -.002f, asphalt);
            DecoKitStage.Slab("Apron", Rect.MinMaxRect(-Far, B.QuayZ, L.KerbX, Far), L.Walk - .004f, stone);
            DecoKitStage.Slab("Far verge", Rect.MinMaxRect(L.StreetMaxX, B.QuayZ, Far, Far), L.Walk - .004f, stone);
            DecoKitStage.Slab("Bay", Rect.MinMaxRect(-Far, -Far, Far, B.QuayZ), B.WaterY, Water());
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);  // quay face, plain stone (no tiling needed)
            wall.name = "Quay wall";
            wall.transform.position = new Vector3(0f, (B.WaterY + L.Walk) * .5f, B.QuayZ - .5f);
            wall.transform.localScale = new Vector3(Far * 2f, L.Walk - B.WaterY, 1f);
            wall.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/DK_Limestone.mat");
        }

        static Material Water()
        {
            var path = $"{GeneratedDir}/DK_Bay_Water.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) AssetDatabase.CreateAsset(mat = new Material(Shader.Find("Universal Render Pipeline/Lit")), path);
            mat.SetColor("_BaseColor", new Color(.1f, .27f, .38f));  // plaza.png bay reads (112, 156, 182)
            mat.SetFloat("_Smoothness", .94f);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Probes()
        {
            DecoKitAtmosphere.Probe("Plaza probe", new Vector3(-5f, 4f, 22f), new Vector3(70f, 30f, 56f));
            DecoKitAtmosphere.Probe("Street probe", new Vector3(30f, 4f, 20f), new Vector3(20f, 24f, 200f));
            DecoKitAtmosphere.Probe("Hall probe", new Vector3(0f, 9f, -6f), new Vector3(70f, 26f, 14f));
        }

        static Camera Hero()
        {
            var cam = DecoKitStage.Camera("DK plaza hero camera", 34f);
            cam.transform.position = HeroPos;
            cam.transform.LookAt(HeroTarget, Vector3.up);
            cam.nearClipPlane = .5f;
            cam.farClipPlane = 900f;
            cam.fieldOfView = DecoKitStage.Arg("dkFov", 42f);  // wide enough for the skyline over the crown
            return cam;
        }
    }
}
#endif
