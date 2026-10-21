using System.IO;
using System.Linq;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Play
{
    /// <summary>Brief 029, the fight range scene (Assets/Scenes/FightRange.unity), built from Keel's range site (sim/data/range.site.json,
    /// tools/sim/range.ts; never by hand): a flat, well-lit grey floor 45 x 40 m with a 1 m grid, 5 m distance rings about the mark, the cover
    /// pieces as graybox blocks by kind (low wall, high wall, pillar, car-sized box), a sun, the play camera and the host in range mode
    /// (PlayInstall.InstallRange). The engine's cm map straight onto Unity metres: x along the range, y across it as Unity z. The dense
    /// layout's pieces are drawn too, on their own root, shown when the range runs dense (RangePieces). Menu: Suborned/World/Build Fight Range.</summary>
    public static class RangeScene
    {
        public const string ScenePath = "Assets/Scenes/FightRange.unity";

        [MenuItem("Suborned/World/Build Fight Range")]
        static void Menu() => HarbourWorldBuilder.Batch(Build);

        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            made.Clear();
            var repo = HarbourWorldBuilder.RepoRoot;
            // light: a soft sun and a bright, even ambient, so the floor reads flat and every piece's form shows
            var sun = new GameObject("sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.5f; sun.shadows = LightShadows.Soft;
            sun.color = new Color(1f, 0.97f, 0.92f); sun.transform.rotation = Quaternion.Euler(52f, 30f, 0f); RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.7f); RenderSettings.ambientEquatorColor = new Color(0.5f, 0.52f, 0.54f); RenderSettings.ambientGroundColor = new Color(0.32f, 0.33f, 0.34f);
            RenderSettings.fog = false; RenderSettings.skybox = null;
            // the play camera (the host places it; its overhead pose, zoom and projection are the Handover's)
            var cams = new GameObject("cameras"); var cg = new GameObject("play"); cg.transform.SetParent(cams.transform, false); cg.tag = "MainCamera";
            var cam = cg.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.78f, 0.8f, 0.82f); cam.orthographic = true; cam.orthographicSize = 14f;
            var cd = cg.AddComponent<UniversalAdditionalCameraData>(); cd.renderPostProcessing = false; cam.allowHDR = true;
            cg.transform.SetPositionAndRotation(new Vector3(3f, 40f, -10f), Quaternion.Euler(38f, 30f, 0f));
            // floor and grid: one quad, a 1 m grid texture with every fifth line stronger
            var range = new GameObject("range");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad); floor.name = "floor"; floor.transform.SetParent(range.transform, false);
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.SetPositionAndRotation(new Vector3(22.5f, 0f, 20f), Quaternion.Euler(90f, 0f, 0f)); floor.transform.localScale = new Vector3(45f, 40f, 1f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = FloorMaterial();
            // the 5 m rings about the mark
            var site = Json.Parse(File.ReadAllText(Path.Combine(repo, "sim/data/range.site.json")));
            var markP = J.A(J.Get(J.Get(site, "places"), "range:mark"), "at") is { Count: 2 } ma ? new Vector2((float)(double)ma[0] / 100f, (float)(double)ma[1] / 100f) : new Vector2(3f, 20f);
            var mark = new Vector3(markP.x, 0.02f, markP.y);
            var ringMat = Unlit("range-ring", new Color(0.25f, 0.27f, 0.29f));
            for (int r = 5; r <= 40; r += 5)
            {
                var go = new GameObject($"ring-{r}m"); go.transform.SetParent(range.transform, false);
                var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.loop = true; lr.positionCount = 96; lr.widthMultiplier = r % 15 == 0 ? 0.1f : 0.05f; lr.sharedMaterial = ringMat;
                lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; lr.alignment = LineAlignment.TransformZ; go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                for (int i = 0; i < 96; i++) { float a = i / 96f * Mathf.PI * 2f; lr.SetPosition(i, mark + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r)); }
            }
            // the pieces, per layout (each a root; RangePieces shows the one the run uses)
            int open = Pieces(range.transform, "pieces-open", site), dense = Pieces(range.transform, "pieces-dense", Json.Parse(File.ReadAllText(Path.Combine(repo, "sim/data/range-dense.site.json"))));
            // engine 0.16 lane (tuning.md §10.4): the open layout's pieces on a floor run on to 90 m; the floor's second half and the rings past
            // 40 m (their arc on the floor ahead of the mark) are the lane's own, shown with it
            int lane = Pieces(range.transform, "pieces-lane", Json.Parse(File.ReadAllText(Path.Combine(repo, "sim/data/range-lane.site.json"))));
            var laneRoot = range.transform.Find("pieces-lane");
            var ext = GameObject.CreatePrimitive(PrimitiveType.Quad); ext.name = "floor-lane"; ext.transform.SetParent(laneRoot, false);
            Object.DestroyImmediate(ext.GetComponent<Collider>());
            ext.transform.SetPositionAndRotation(new Vector3(67.5f, 0f, 20f), Quaternion.Euler(90f, 0f, 0f)); ext.transform.localScale = new Vector3(45f, 40f, 1f);
            ext.GetComponent<MeshRenderer>().sharedMaterial = FloorMaterial();
            for (int r = 45; r <= 85; r += 5)
            {
                var go = new GameObject($"ring-{r}m"); go.transform.SetParent(laneRoot, false);
                var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.loop = false; lr.widthMultiplier = r % 15 == 0 ? 0.1f : 0.05f; lr.sharedMaterial = ringMat;
                lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; lr.alignment = LineAlignment.TransformZ; go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                float half = Mathf.Asin(Mathf.Min(1f, 19.9f / r)); const int n = 48; lr.positionCount = n + 1; // the arc ahead of the mark that lies on the 40 m wide floor
                for (int i = 0; i <= n; i++) { float a = -half + 2f * half * i / n; lr.SetPosition(i, mark + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r)); }
            }
            range.transform.Find("pieces-dense").gameObject.SetActive(false); laneRoot.gameObject.SetActive(false);
            range.AddComponent<RangePieces>();
            var host = PlayInstall.InstallRange();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Path.Combine(Application.dataPath, "..", ScenePath))));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException($"Range: could not save {ScenePath}");
            Debug.Log($"Range: built {ScenePath}: floor 45 x 40 m, 1 m grid, rings every 5 m about the mark {mark}, pieces open {open} / dense {dense} / lane {lane}");
        }

        static int Pieces(Transform parent, string name, object site)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); int n = 0;
            foreach (var o in J.A(site, "occluders"))
            {
                var id = J.S(o, "id"); if (id == null || !id.StartsWith("range/")) continue;
                var poly = J.A(o, "poly").Select(p => { var a = J.A(p); return new Vector2((float)(double)a[0] / 100f, (float)(double)a[1] / 100f); }).ToArray();
                float h = (float)J.F(o, "h") / 100f, z = (float)J.F(o, "z") / 100f;
                float x0 = poly.Min(p => p.x), x1 = poly.Max(p => p.x), y0 = poly.Min(p => p.y), y1 = poly.Max(p => p.y);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = id; go.transform.SetParent(root, false); Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.position = new Vector3((x0 + x1) / 2f, z + h / 2f, (y0 + y1) / 2f); go.transform.localScale = new Vector3(x1 - x0, h, y1 - y0);
                var kind = id.StartsWith("range/low-wall") ? 0 : id.StartsWith("range/high-wall") ? 1 : id.StartsWith("range/pillar") ? 2 : 3;
                go.GetComponent<MeshRenderer>().sharedMaterial = Lit("range-" + new[] { "low-wall", "high-wall", "pillar", "box" }[kind], new[] { new Color(0.72f, 0.7f, 0.66f), new Color(0.56f, 0.57f, 0.6f), new Color(0.64f, 0.6f, 0.56f), new Color(0.48f, 0.54f, 0.6f) }[kind]);
                n++;
            }
            return n;
        }

        const string Gen = "Assets/Suborned/Generated/Range";
        static Material Lit(string name, Color c) => Save(name, () => { var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.15f); return m; });
        static Material Unlit(string name, Color c) => Save(name, () => { var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name }; m.SetColor("_BaseColor", c); return m; });

        static Material FloorMaterial() => Save("range-floor", () =>
        {
            // a 64 px cell: a 1 px line at the edge; every fifth cell's line darker (the texture covers 5 x 5 m, tiled 9 x 8)
            const int N = 320; var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "range-grid", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            var px = new Color[N * N]; var bg = new Color(0.66f, 0.67f, 0.68f); var thin = new Color(0.55f, 0.56f, 0.57f); var thick = new Color(0.4f, 0.41f, 0.43f);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { bool five = x < 3 || y < 3; bool one = x % 64 < 1 || y % 64 < 1; px[y * N + x] = five ? thick : one ? thin : bg; }
            tex.SetPixels(px); tex.Apply(true); AssetDatabase.CreateAsset(tex, Gen + "/range-grid.asset");
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "range-floor" }; m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", new Vector2(9f, 8f)); m.SetFloat("_Smoothness", 0.05f);
            return m;
        });

        static readonly System.Collections.Generic.Dictionary<string, Material> made = new();
        static Material Save(string name, System.Func<Material> make)
        {
            if (made.TryGetValue(name, out var have) && have) return have; // one material a kind (re-made, a piece built before would lose it)
            if (!AssetDatabase.IsValidFolder("Assets/Suborned/Generated")) AssetDatabase.CreateFolder("Assets/Suborned", "Generated");
            if (!AssetDatabase.IsValidFolder(Gen)) AssetDatabase.CreateFolder("Assets/Suborned/Generated", "Range");
            var path = Gen + "/" + name + ".mat"; AssetDatabase.DeleteAsset(path); if (name == "range-floor") AssetDatabase.DeleteAsset(Gen + "/range-grid.asset");
            var m = make(); AssetDatabase.CreateAsset(m, path); made[name] = m; return m;
        }
    }
}
