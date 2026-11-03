#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.World;
using Suborned.World.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 077, the Unity capture for Sky (Latch). Two batch steps; neither saves OldTown.unity or HarbourDistrict.unity.
    ///   Mats: the four kit materials (DK_Soil_TB on DecoKit/Lit Cutaway; DK_Leaf_TB and DK_Petal_TB on DecoKit/Lit Cutaway Leaf with _DK_VERTEX_COLOR,
    ///         _DK_TWO_SIDED and the derived mask; DK_Opal_TB, the globe lamp's opal) and the six textures (the testbed's atlas, normal, soil base and
    ///         normal as they are; the botanical and soil masks derived from the ORMs) with their .meta, written into out/decokit-077/Materials and
    ///         Textures for `bun run kit --catalogue --extra out/decokit-077`. Kit GUIDs are md5("suborned.kit077/" + file): deterministic. The
    ///         probe works on its own copies (Assets/Scenes/probe-planting077/Kit, other GUIDs), so the kit copy never meets a duplicate GUID.
    ///   Capture: the swap (OldTown.unity) opened read-only; before captures and counts; then every dome-family placement of the Handover zone
    ///         swapped to its testbed replacement (the _TB rebuilds and their companions, the five CivicBedPlanting pieces at their beds with
    ///         the hedge and the tufts gone, a GlobeLamp_TB per PlanterArc), each as a LODGroup from its catalogue row (its switch heights, its
    ///         lodSize, the soil objects casting no shadow); after captures and counts; saved as Assets/Scenes/probe-planting077.unity
    ///         (git-ignored). The baked dome triangles leave the civic batches by exact world-triangle match; the stonework's batch materials
    ///         (the restyle's) carry over to the rebuild's stone slots by slot name.</summary>
    public static class PlantingSwap077
    {
        const string Dir = "Assets/Scenes/probe-planting077";
        const string ScenePath = "Assets/Scenes/probe-planting077.unity";
        const string SwapScene = "Assets/Scenes/OldTown.unity";
        const string Root077 = "out/decokit-077";
        const string World = "out/scratch/latch/075swap2/harbour.world.json"; // the export OldTown.unity was built from (its WorldStamp)
        const string LeafShader = "DecoKit/Lit Cutaway Leaf", LitShader = "DecoKit/Lit Cutaway";
        static readonly string[] LeafKeywords = { "_DK_VERTEX_COLOR", "_DK_TWO_SIDED" };
        static string Repo => HarbourWorldBuilder.RepoRoot;
        static string Arg(string k, string d) => DecoKitStage.Arg(k, d);
        static string Evidence => Path.Combine(Repo, Arg("plantOut", "briefs/077/unity"));
        static string Scratch => Path.Combine(Repo, "out/scratch/latch/077unity");

        // ================================================================ materials and textures for the kit
        const string Art = "the-suborned-store/testbed-art/cafe-plants";
        // kit name <- (source, sha256, kind); kind: colour | normal | mask (derived from the ORM with k by atlas row)
        static readonly (string name, string src, string sha, string kind, bool repeat)[] Maps =
        {
            ("botanical-atlas-v2.png", "botanical-atlas-v2.png", "da2573c3f98e3e502ad1a6acd9e786ae43ae0961f0b8dd7a80dc1a75fa2f9bbe", "colour", false),
            ("botanical-normal-v2.png", "botanical-normal-v2.png", "9c95555b3906cf72a04fe28927dad2a6e6b0b97d84e770261f55819c65a29b79", "normal", false),
            ("botanical-mask-v2.png", "botanical-orm-v2.png", "c3574a250b1ea075f1b80b9c8fe51b805ab3e0dcb819ce5644d97c681b8d3130", "mask", false),
            ("soil-base.png", "soil-base.png", "1e90af1a2f00040f49999e3b4b72b9043ae19aecd39bdabb0aae4ad4d1ce8f55", "colour", true),
            ("soil-normal.png", "soil-normal.png", "aed2d8f06c5fd2bc4d4ae7c93b309e4710446669507951941f07bcf2be03bc73", "normal", true),
            ("soil-mask.png", "soil-orm.png", "ff8db1f5b19b743c707284aedfb8536baff21f9df499675e0cfc422308e8c8cb", "mask", true),
        };
        static string KitGuid(string file) => Md5("suborned.kit077/" + file);
        static string ProbeGuid(string file) => Md5("suborned.probe077/" + file);

        public static void Mats() => HarbourWorldBuilder.Batch(() =>
        {
            var log = new Dictionary<string, object> { ["root"] = Root077 };
            var stage = Dir + "/Kit";
            foreach (var d in new[] { Dir, stage, stage + "/Textures", stage + "/Materials" }) Directory.CreateDirectory(Path.Combine(Repo, "unity", d));
            string artRoot = Path.Combine(Path.GetDirectoryName(Repo), Art);
            var texRows = new List<object>();
            foreach (var m in Maps)
            {
                var src = Path.Combine(artRoot, m.src); var got = Sha(File.ReadAllBytes(src));
                if (got != m.sha) throw new InvalidDataException($"077: {m.src} sha256 {got}, expected {m.sha}");
                var dst = Path.Combine(Repo, "unity", stage, "Textures", m.name);
                if (m.kind == "mask") File.WriteAllBytes(dst, Mask(src, m.name.StartsWith("botanical") ? (v => v >= .5f ? .80f : .80f) : (_ => 1f)));
                else File.Copy(src, dst, true);
                File.WriteAllText(dst + ".meta", $"fileFormatVersion: 2\nguid: {ProbeGuid(m.name)}\nTextureImporter:\n  serializedVersion: 13\n");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var m in Maps) Tex($"{stage}/Textures/{m.name}", m.kind, m.repeat);
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{stage}/Textures/{n}") ?? throw new FileNotFoundException(n);
            var leaf = Plant("DK_Leaf_TB", T("botanical-atlas-v2.png"), Hex("#afbea1"), T("botanical-normal-v2.png"), .16f, T("botanical-mask-v2.png"), true);
            var petal = Plant("DK_Petal_TB", T("botanical-atlas-v2.png"), Color.white, T("botanical-normal-v2.png"), .16f, T("botanical-mask-v2.png"), true);
            var soil = Plant("DK_Soil_TB", T("soil-base.png"), Color.white, T("soil-normal.png"), .4f, T("soil-mask.png"), false);
            var opal = Opal();
            AssetDatabase.SaveAssets();
            // the kit's copies: the same bytes with the probe GUIDs rewritten to the kit GUIDs
            var outTex = Path.Combine(Repo, Root077, "Textures"); var outMat = Path.Combine(Repo, Root077, "Materials");
            Directory.CreateDirectory(outTex); Directory.CreateDirectory(outMat);
            var swap = Maps.ToDictionary(m => ProbeGuid(m.name), m => KitGuid(m.name));
            string Rewrite(string text) { foreach (var kv in swap) text = text.Replace(kv.Key, kv.Value); return text; }
            foreach (var m in Maps)
            {
                var p = Path.Combine(Repo, "unity", stage, "Textures", m.name);
                File.Copy(p, Path.Combine(outTex, m.name), true);
                File.WriteAllText(Path.Combine(outTex, m.name + ".meta"), Rewrite(File.ReadAllText(p + ".meta")));
                texRows.Add(new Dictionary<string, object> { ["file"] = "Textures/" + m.name, ["guid"] = KitGuid(m.name), ["from"] = m.src, ["fromSha256"] = m.sha, ["kind"] = m.kind, ["sha256"] = Sha(File.ReadAllBytes(p)) });
            }
            var matRows = new List<object>();
            foreach (var mat in new[] { leaf, petal, soil, opal })
            {
                var p = Path.Combine(Repo, "unity", AssetDatabase.GetAssetPath(mat));
                var text = Rewrite(File.ReadAllText(p));
                if (mat.name == "DK_Soil_TB" && !text.Contains("- SHADOWCASTER")) text = text.Replace("  disabledShaderPasses: []", "  disabledShaderPasses:\n  - SHADOWCASTER");
                File.WriteAllText(Path.Combine(outMat, mat.name + ".mat"), text);
                File.WriteAllText(Path.Combine(outMat, mat.name + ".mat.meta"), $"fileFormatVersion: 2\nguid: {KitGuid(mat.name + ".mat")}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n");
                matRows.Add(new Dictionary<string, object>
                {
                    ["file"] = "Materials/" + mat.name + ".mat", ["guid"] = KitGuid(mat.name + ".mat"), ["shader"] = mat.shader.name, ["shaderGuid"] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mat.shader)),
                    ["keywords"] = string.Join(" ", mat.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal)), ["cull"] = R(mat.GetFloat("_Cull")), ["shadowCasterInMat"] = text.Contains("- SHADOWCASTER") ? "disabled" : "enabled",
                    ["textures"] = Regex.Matches(text, @"guid: ([0-9a-f]{32})").Select(g => g.Groups[1].Value).Where(g => swap.ContainsValue(g)).Distinct().Select(g => (object)Maps.First(x => KitGuid(x.name) == g).name).ToList(),
                    ["sha256"] = Sha(Encoding.UTF8.GetBytes(text)),
                });
            }
            log["textures"] = texRows; log["materials"] = matRows;
            Directory.CreateDirectory(Scratch); File.WriteAllText(Path.Combine(Scratch, "mats.json"), Json(log));
            Debug.Log($"[077 unity] kit materials and textures -> {Root077}: {string.Join(", ", matRows.Cast<Dictionary<string, object>>().Select(r => r["file"]))}");
        });

        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : throw new InvalidDataException(h);

        static byte[] Mask(string ormPath, Func<float, float> k)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!src.LoadImage(File.ReadAllBytes(ormPath))) throw new InvalidDataException(ormPath);
            int w = src.width, h = src.height; var px = src.GetPixels32(); var o = new Color32[px.Length];
            for (int y = 0; y < h; y++) { float kk = k((y + .5f) / h); for (int x = 0; x < w; x++) { int i = y * w + x; o[i] = new Color32(0, 0, 0, (byte)Math.Round(255.0 - kk * px[i].g, MidpointRounding.AwayFromZero)); } }
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false, true); dst.SetPixels32(o); dst.Apply();
            var bytes = dst.EncodeToPNG(); Object.DestroyImmediate(src); Object.DestroyImmediate(dst); return bytes;
        }

        static void Tex(string path, string kind, bool repeat)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = kind == "normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = kind == "colour"; ti.mipmapEnabled = true; ti.npotScale = TextureImporterNPOTScale.ToNearest;
            ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; ti.filterMode = FilterMode.Trilinear; ti.anisoLevel = 8;
            ti.alphaSource = kind == "mask" ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None; ti.alphaIsTransparency = false;
            ti.isReadable = false; ti.maxTextureSize = 4096; ti.textureCompression = TextureImporterCompression.CompressedHQ;
            var ps = ti.GetPlatformTextureSettings("Standalone"); ps.overridden = true; ps.maxTextureSize = 4096; ps.format = kind == "normal" ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7;
            ti.SetPlatformTextureSettings(ps); ti.SaveAndReimport();
        }

        static Material NewMat(string name, string shader)
        {
            var path = $"{Dir}/Kit/Materials/{name}.mat"; var sh = Shader.Find(shader) ?? throw new InvalidDataException("077: no shader " + shader);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = sh; return m;
        }

        /// <summary>The testbed's cafe-planting.js materials: leaf #afbea1 / petal white, normal 0.16, roughness 0.49 / 0.64 x ORM.g (the mask), cull off,
        /// the two leaf keywords; soil: its maps, normal 0.4, roughness 1 x ORM.g, cull back, no shadow caster, on today's shader.</summary>
        /// <summary>Glint, brief 079: x the mask's gloss on leaves and petals. At 1 the leaves were the largest single source of white points in the café square views (a third to a half). 0.5 cuts them as far as 0.4 or 0 would for points that read, and keeps the leaf's dark sheen.</summary>
        const float LeafGloss = .5f;

        static Material Plant(string name, Texture2D albedo, Color colour, Texture2D nrm, float bump, Texture2D mask, bool leaf)
        {
            var m = NewMat(name, leaf ? LeafShader : LitShader);
            m.SetFloat("_WorkflowMode", 1f); m.SetTexture("_BaseMap", albedo); m.SetColor("_BaseColor", colour);
            m.SetTexture("_BumpMap", nrm); m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", mask); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", leaf ? LeafGloss : 1f); m.SetFloat("_SmoothnessTextureChannel", 0f);
            m.SetFloat("_Cull", leaf ? 0f : 2f); m.doubleSidedGI = leaf; m.SetFloat("_ReceiveShadows", 1f);
            foreach (var k in LeafKeywords) if (leaf) m.EnableKeyword(k); else m.DisableKeyword(k);
            m.SetShaderPassEnabled("ShadowCaster", leaf);
            m.renderQueue = -1; EditorUtility.SetDirty(m); return m;
        }

        /// <summary>fountain-garden-lights.js "Garden lamp warm opal glass": #fffdf0, emissive #fff1d2 x 0.06 by day, roughness 0.25.</summary>
        static Material Opal()
        {
            var m = NewMat("DK_Opal_TB", LitShader);
            m.SetFloat("_WorkflowMode", 1f); m.SetColor("_BaseColor", Hex("#fffdf0")); m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", .75f);
            m.SetColor("_EmissionColor", Hex("#fff1d2").linear * .06f); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; m.EnableKeyword("_EMISSION");
            m.SetFloat("_Cull", 2f); m.renderQueue = -1; EditorUtility.SetDirty(m); return m;
        }

        // ================================================================ the swap
        sealed class Row { public string Name; public string Fbx; public string Sha; public List<(string file, string sha, float screen)> Lods = new(); public float LodSize; public object Raw; }
        sealed class Place { public string Mass, OldId, NewId, Why; public Matrix4x4 Trs; public bool Baked; public Transform OldInstance; public string Layer = "civic"; }

        static Dictionary<string, Row> Rows()
        {
            var man = Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(Repo, Root077, "decokit_manifest.json")));
            var o = new Dictionary<string, Row>(StringComparer.Ordinal);
            foreach (var a in J.A(man, "assets"))
            {
                var r = new Row { Name = J.S(a, "name"), Fbx = J.S(a, "fbx"), Raw = a, LodSize = J.Get(a, "lodSize") is { } ls0 ? Convert.ToSingle(ls0, CultureInfo.InvariantCulture) : 0f };
                r.Sha = J.S(J.A(a, "files").Cast<object>().First(f => J.S(f, "file") == r.Fbx), "sha256");
                if (J.Get(a, "lods") is List<object> ls) foreach (var l in ls) r.Lods.Add((J.S(l, "file"), J.S(l, "sha256"), J.F(l, "screen")));
                o[r.Name] = r;
            }
            return o;
        }

        // the dome family and what replaces each (the catalogue's `<id>_TB` rebuild; its companion planting at the same transform)
        static readonly Dictionary<string, string> Companion = new(StringComparer.Ordinal) { ["CafeTerrace"] = "CafeTerrace_Planting", ["RecordsTower"] = "RecordsTower_Planting", ["CafeUpper_3"] = "CafeUpper_3_Planting", ["CafeUpper_4"] = "CafeUpper_4_Planting" };
        static bool Family(string id) => id.StartsWith("PlanterRun_", StringComparison.Ordinal) || id == "PlanterArc" || Companion.ContainsKey(id) || id.StartsWith("BedTuft_", StringComparison.Ordinal);
        static readonly string[] Beds = { "facade-southwest", "facade-southeast", "office-west", "office-north", "dropoff-west" };

        /// <summary>RETIRED at adoption (brief 077 READY-adopt): it removes the dome pieces' baked triangles, and those pieces are now rejected and pruned
        /// from the kit. The adopted build places the testbed planting itself; PlantingBuild077 shoots and checks it. Kept for the record of the probe.</summary>
        public static void Capture() => HarbourWorldBuilder.Batch(() =>
        {
            if (Arg("plantProbe", "off") != "on") throw new InvalidOperationException("077: PlantingSwap077.Capture is retired (the dome pieces are rejected and pruned); use PlantingBuild077");
            var t0 = DateTime.UtcNow;
            var rows = Rows();
            var numbers = new Dictionary<string, object> { ["schema"] = "suborned.unity077.numbers/1", ["brief"] = "077", ["author"] = "Latch", ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"), ["unity"] = Application.unityVersion, ["world"] = World, ["worldSha256"] = Sha(File.ReadAllBytes(Path.Combine(Repo, World))) };
            // 1. the models: Vela's FBXs (sha256 against the manifest) into the probe, the materials remapped by slot name
            var models = ImportModels(rows, numbers);
            // 2. the swap scene, read only
            var scene = EditorSceneManager.OpenScene(SwapScene, OpenSceneMode.Single);
            DecoKitHarbour.Ocean();
            var play = SwapPlayLook.Find() ?? throw new InvalidDataException("077: no play camera in the swap");
            var ts = play.GetComponent<TacticalShadows>();
            var world = WorldReader.Load(Path.Combine(Repo, World));
            var kit = new KitPlacer(HarbourWorldBuilder.KitCataloguePath);
            var views = Views();
            var shots = new List<object>(); var dir = Path.Combine(Evidence, "captures"); Directory.CreateDirectory(dir);
            // the frame: our plan to the swap's world (the old frame: plan y + DZ), checked by the triangle match below
            bool old = Arg("plantFrame", "old") == "old"; numbers["frame"] = old ? "old (plan y + DZ)" : "plan";
            Vector3 W(Vector2 plan, float y) => old ? OldTownSwap.Old3(plan, y) : new Vector3(plan.x, y, plan.y);
            using (var session = new RenderSession())
            {
                Object.DestroyImmediate(DecoKitStage.Render(play, 640, 360));
                Shots(play, views, dir, "before", shots);
                numbers["countsBefore"] = Counts(play, ts, views);
                // 3. the swap
                numbers["swap"] = Swap(scene, world, kit, rows, models, W);
                Object.DestroyImmediate(DecoKitStage.Render(play, 640, 360));
                Shots(play, views, dir, "after", shots);
                numbers["countsAfter"] = Counts(play, ts, views);
            }
            numbers["captures"] = shots;
            numbers["views"] = views.Select(v => (object)new Dictionary<string, object> { ["id"] = v.id, ["focusPlan"] = new List<object> { R(v.focus.x), R(v.focus.y) }, ["yaw"] = v.yaw, ["ortho"] = v.size }).ToList();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException(ScenePath);
            AssetDatabase.SaveAssets();
            numbers["scene"] = ScenePath; numbers["seconds"] = Math.Round((DateTime.UtcNow - t0).TotalSeconds, 1);
            Directory.CreateDirectory(Evidence); File.WriteAllText(Path.Combine(Evidence, "numbers.json"), Json(numbers));
            Debug.Log($"[077 unity] capture: {shots.Count} shots in {numbers["seconds"]} s -> {Evidence}; probe scene {ScenePath}");
        });

        static List<(string id, Vector2 focus, float yaw, float size)> Views() => new()
        {
            ("cafe-square-y030", new(-170.5f, -227.5f), 30f, 14f), ("cafe-square-y210", new(-170.5f, -227.5f), 210f, 14f),
            ("office-front-y030", new(-160f, -281f), 30f, 14f), ("office-front-y210", new(-160f, -281f), 210f, 14f),
            ("close-troughs-y030", new(-179.5f, -220.5f), 30f, 4.5f), ("close-troughs-y210", new(-179.5f, -220.5f), 210f, 4.5f),
            ("close-island-y030", new(-164.3f, -232.2f), 30f, 5f), ("close-island-y210", new(-155.7f, -240.8f), 210f, 5f),
            ("close-bed-y030", new(-141.1f, -294.9f), 30f, 6f), ("close-bed-y210", new(-141.1f, -294.9f), 210f, 6f),
        };

        static void Shots(Camera cam, List<(string id, Vector2 focus, float yaw, float size)> views, string dir, string tag, List<object> shots)
        {
            // a freshly opened or changed scene's first frames blow out (bloom and exposure history): three full frames at the first pose first
            SwapShots.Play(cam, views[0].focus, views[0].yaw, views[0].size);
            for (int i = 0; i < 3; i++) DecoKitStage.WithShadows(200f, () => Object.DestroyImmediate(DecoKitStage.Render(cam, 1920, 1080, 2)));
            foreach (var v in views)
            {
                SwapShots.Play(cam, v.focus, v.yaw, v.size);
                Texture2D px = null; DecoKitStage.WithShadows(200f, () => px = DecoKitStage.Render(cam, 1920, 1080, 2));
                var name = $"{v.id}-{tag}.png"; File.WriteAllBytes(Path.Combine(dir, name), px.EncodeToPNG()); Object.DestroyImmediate(px);
                shots.Add(new Dictionary<string, object> { ["file"] = "captures/" + name, ["view"] = v.id, ["state"] = tag });
            }
        }

        static List<object> Counts(Camera cam, TacticalShadows ts, List<(string id, Vector2 focus, float yaw, float size)> views)
        {
            var spec = new After072.AbSpec { cam = cam, shadows = ts, fade = null, standOff = 160f };
            foreach (var v in views.Where(x => x.size >= 14f)) { var vv = v; spec.views.Add(new After072.AbView { id = vv.id, kind = "play", pose = c => SwapShots.Play(c, vv.focus, vv.yaw, vv.size), focusPlan = vv.focus, yaw = vv.yaw }); }
            return After072.AbCapture(spec);
        }

        /// <summary>Every row the swap uses, its L0 and LOD files copied into the probe (sha256 checked) and imported with Vela's object names, the
        /// testbed materials and the kit's stone materials remapped by slot name.</summary>
        static Dictionary<string, List<GameObject>> ImportModels(Dictionary<string, Row> rows, Dictionary<string, object> numbers)
        {
            var need = new[] { "PlanterRun_4_A", "PlanterRun_4_B", "PlanterRun_4_C", "PlanterArc", "CafeTerrace", "CafeTerrace_Planting", "RecordsTower", "RecordsTower_Planting", "CafeUpper_3", "CafeUpper_3_Planting", "GlobeLamp_TB" }
                .Concat(Beds.Select(b => "CivicBedPlanting_" + b)).ToArray();
            Directory.CreateDirectory(Path.Combine(Repo, "unity", Dir, "Models")); Directory.CreateDirectory(Path.Combine(Repo, "unity", Dir, "Meshes"));
            var files = new List<(string row, string rel, string sha)>();
            foreach (var n in need)
            {
                var r = rows.TryGetValue(n, out var x) ? x : throw new InvalidDataException("077: manifest has no row " + n);
                files.Add((n, r.Fbx, r.Sha)); foreach (var l in r.Lods) files.Add((n, l.file, l.sha));
            }
            var copied = new List<object>();
            foreach (var (row, rel, sha) in files)
            {
                var src = Path.Combine(Repo, Root077, rel); var got = Sha(File.ReadAllBytes(src));
                if (got != sha) throw new InvalidDataException($"077: {rel} sha256 {got}, the manifest says {sha}");
                var dst = Path.Combine(Repo, "unity", Dir, "Models", Path.GetFileName(rel));
                if (!File.Exists(dst) || Sha(File.ReadAllBytes(dst)) != sha) File.Copy(src, dst, true);
                copied.Add(new Dictionary<string, object> { ["row"] = row, ["file"] = rel, ["sha256"] = sha });
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var probeMats = new[] { "DK_Leaf_TB", "DK_Petal_TB", "DK_Soil_TB", "DK_Opal_TB" }.ToDictionary(n => n, n => AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/Kit/Materials/{n}.mat") ?? throw new FileNotFoundException("077: run Mats first: " + n));
            // Glint 077: the probe's opal had lost _EMISSION on re-import (the kit copy, written before, kept it): assert it, with realtime-emissive flags
            var opal = probeMats["DK_Opal_TB"];
            if (!opal.IsKeywordEnabled("_EMISSION") || opal.globalIlluminationFlags != MaterialGlobalIlluminationFlags.RealtimeEmissive)
            { opal.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; opal.EnableKeyword("_EMISSION"); EditorUtility.SetDirty(opal); AssetDatabase.SaveAssets(); }
            numbers["probeOpalEmission"] = opal.IsKeywordEnabled("_EMISSION");
            var o = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);
            foreach (var (row, rel, _) in files)
            {
                var path = $"{Dir}/Models/{Path.GetFileName(rel)}";
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                mi.globalScale = 1f; mi.useFileScale = true; mi.importCameras = false; mi.importLights = false; mi.importVisibility = false;
                mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.importBlendShapes = false; mi.importConstraints = false;
                mi.meshCompression = ModelImporterMeshCompression.Off; mi.isReadable = true; mi.weldVertices = true;
                mi.importNormals = ModelImporterNormals.Import; mi.importTangents = ModelImporterTangents.CalculateMikk; mi.generateSecondaryUV = false; mi.addCollider = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
                foreach (var mn in J.A(rows[row].Raw, "materials").Cast<string>())
                {
                    var mat = probeMats.TryGetValue(mn, out var pm) ? pm : AssetDatabase.LoadAssetAtPath<Material>($"Assets/Suborned/Kit/Materials/{mn}.mat") ?? throw new FileNotFoundException("077: kit material " + mn);
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mn), mat);
                }
                mi.SaveAndReimport();
                if (!o.TryGetValue(row, out var l)) o[row] = l = new List<GameObject>();
                l.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            }
            numbers["models"] = copied;
            return o;
        }

        static string Key(Vector3 a, Vector3 b, Vector3 c, float shift = 0f)
        {
            string P(Vector3 p) => $"{Mathf.RoundToInt(p.x * 1000f + shift)},{Mathf.RoundToInt(p.y * 1000f + shift)},{Mathf.RoundToInt(p.z * 1000f + shift)}";
            var s = new[] { P(a), P(b), P(c) }; Array.Sort(s, StringComparer.Ordinal); return string.Join(";", s);
        }
        static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;

        static Dictionary<string, object> Swap(UnityEngine.SceneManagement.Scene scene, WorldDto world, KitPlacer kit, Dictionary<string, Row> rows, Dictionary<string, List<GameObject>> models, Func<Vector2, float, Vector3> W)
        {
            var rep = new Dictionary<string, object>();
            var zone = OldTownSwap.ZonePlan;
            var masses = world.Civic.Where(m => m.Kit != null && Family(m.Kit.Id) && zone.Contains(m.Kit.At)).ToList();
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
            var byName = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);
            foreach (var t in all) { if (!byName.TryGetValue(t.name, out var l)) byName[t.name] = l = new List<Transform>(); l.Add(t); }
            // 1. the expected baked triangles of every target (its prefab's LOD0 as KitPlacer.Bake placed it), by world triangle
            var expected = new Dictionary<string, List<(string mass, string slot)>>(StringComparer.Ordinal);
            var expTris = new List<(Vector3 a, Vector3 b, Vector3 c, string mass, string slot)>(); var degenerate = new Dictionary<string, int>(StringComparer.Ordinal);
            var places = new List<Place>(); var perMass = new Dictionary<string, int>(StringComparer.Ordinal); var found = new Dictionary<string, int>(StringComparer.Ordinal);
            var slotMat = new Dictionary<string, Dictionary<string, Material>>(StringComparer.Ordinal); // mass -> old slot name -> the batch's material
            var slotLayer = new Dictionary<string, (int layer, uint mask)>(StringComparer.Ordinal);
            foreach (var m in masses)
            {
                float z = kit.PivotForLowest(m.Kit, m.Kit.Base);
                var trs = Matrix4x4.TRS(W(m.Kit.At, z), Quaternion.Euler(0f, -m.Kit.Yaw * Mathf.Rad2Deg, 0f), m.Kit.Scale);
                var el = byName.TryGetValue(m.Id, out var els) ? els.FirstOrDefault(t => PathOf(t).Contains("civic")) : null;
                var inst = el ? el.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != el && t.parent == el && t.GetComponentsInChildren<MeshRenderer>(true).Length > 0) : null;
                var p = new Place { Mass = m.Id, OldId = m.Kit.Id, Trs = trs, Baked = !inst, OldInstance = inst };
                p.NewId = m.Kit.Id.StartsWith("BedTuft_", StringComparison.Ordinal) ? null : m.Kit.Id;
                places.Add(p);
                if (!p.Baked) continue;
                var prefab = kit.Prefab(m.Kit.Id); var root = prefab.transform;
                int n = 0;
                foreach (var mr in KitLods.Lod0(prefab))
                {
                    var mesh = mr.GetComponent<MeshFilter>().sharedMesh; var mats = mr.sharedMaterials; var mx = trs * root.worldToLocalMatrix * mr.transform.localToWorldMatrix;
                    var vs = mesh.vertices;
                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        var tri = mesh.GetTriangles(s); string slot = mats[Mathf.Min(s, mats.Length - 1)].name;
                        for (int k = 0; k < tri.Length; k += 3)
                        {
                            Vector3 a = mx.MultiplyPoint3x4(vs[tri[k]]), b = mx.MultiplyPoint3x4(vs[tri[k + 1]]), c = mx.MultiplyPoint3x4(vs[tri[k + 2]]);
                            if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-12f) { degenerate[m.Id] = degenerate.TryGetValue(m.Id, out var dg) ? dg + 1 : 1; continue; }
                            var key = Key(a, b, c);
                            if (!expected.TryGetValue(key, out var l)) expected[key] = l = new List<(string, string)>();
                            l.Add((m.Id, slot)); expTris.Add((a, b, c, m.Id, slot)); n++;
                        }
                    }
                }
                perMass[m.Id] = n; found[m.Id] = 0;
            }
            // the beds: the hedge prisms (centroid inside the hedge ring, above the kerb top) and the tufts (above) leave the batches
            var beds = Beds.Select(b => (bed: b, kerb: world.Civic.Single(m => m.Id == "civic/civic-" + b), hedge: world.Civic.Single(m => m.Id == $"civic/civic-{b}/hedge"))).ToList();
            var hedgeRings = beds.Select(b => (b.bed, ring: b.hedge.Ring.Select(v => { var q = W(v, 0f); return new Vector2(q.x, q.z); }).ToArray(), top: b.kerb.Base + b.kerb.Height)).ToList();
            var hedgeRemoved = beds.ToDictionary(b => b.bed, _ => 0); var hedgeMats = new SortedDictionary<string, int>(StringComparer.Ordinal);
            // 2. the sweep: every renderer whose bounds meet a target, its matching triangles out (a copy of its mesh, saved in the probe)
            var targetBox = places.Where(p => p.Baked).Select(p => new Bounds(p.Trs.GetColumn(3), Vector3.one * 40f)).ToList();
            var touched = new List<object>(); int copies = 0;
            var mine = new HashSet<Mesh>();
            for (int pass = 0; pass < 2; pass++)
            {
            float shift = pass == 0 ? 0f : .5f;
            if (pass == 1)
            {   // what the 1 mm grid missed by a rounding edge: re-keyed on a grid shifted half a cell
                var left = new Dictionary<string, List<(string mass, string slot)>>(StringComparer.Ordinal);
                foreach (var e in expTris)
                {
                    var key0 = Key(e.a, e.b, e.c);
                    if (!expected.TryGetValue(key0, out var l0) || !l0.Contains((e.mass, e.slot))) continue;
                    var key = Key(e.a, e.b, e.c, shift); if (!left.TryGetValue(key, out var ll)) left[key] = ll = new List<(string, string)>(); ll.Add((e.mass, e.slot));
                    l0.Remove((e.mass, e.slot));
                }
                expected = left;
            }
            foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).ToArray())
            {
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                if (!PathOf(r.transform).Contains("civic")) continue;
                if (!targetBox.Any(b => b.Intersects(r.bounds))) continue;
                var mesh = mf.sharedMesh; if (!mesh.isReadable) continue;
                var vs = mesh.vertices; var mx = r.transform.localToWorldMatrix; Mesh copy = null; int out0 = 0;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var tri = mesh.GetTriangles(s); var kept = new List<int>(tri.Length);
                    var mat = s < r.sharedMaterials.Length ? r.sharedMaterials[s] : null;
                    for (int k = 0; k < tri.Length; k += 3)
                    {
                        Vector3 a = mx.MultiplyPoint3x4(vs[tri[k]]), b = mx.MultiplyPoint3x4(vs[tri[k + 1]]), c = mx.MultiplyPoint3x4(vs[tri[k + 2]]);
                        var key = Key(a, b, c, shift);
                        if (expected.TryGetValue(key, out var owners) && owners.Count > 0)
                        {
                            var (mass, slot) = owners[^1]; owners.RemoveAt(owners.Count - 1); found[mass]++; out0++;
                            if (!slotMat.TryGetValue(mass, out var sm)) slotMat[mass] = sm = new Dictionary<string, Material>(StringComparer.Ordinal);
                            if (mat) sm.TryAdd(slot, mat);
                            slotLayer.TryAdd(mass, (r.gameObject.layer, r.renderingLayerMask));
                            continue;
                        }
                        if (pass == 1) { kept.Add(tri[k]); kept.Add(tri[k + 1]); kept.Add(tri[k + 2]); continue; }
                        var cen = (a + b + c) / 3f; var hb = hedgeRings.FirstOrDefault(h => cen.y > h.top + .005f && Inside(h.ring, new Vector2(cen.x, cen.z)));
                        if (hb.ring != null) { hedgeRemoved[hb.bed]++; out0++; var mn = mat ? mat.name : "-"; hedgeMats[mn] = hedgeMats.TryGetValue(mn, out var hn) ? hn + 1 : 1; continue; }
                        kept.Add(tri[k]); kept.Add(tri[k + 1]); kept.Add(tri[k + 2]);
                    }
                    if (kept.Count != tri.Length) { if (!copy) copy = Object.Instantiate(mesh); copy.SetTriangles(kept, s, false); }
                }
                if (!copy) continue;
                if (mine.Contains(mesh)) { for (int s2 = 0; s2 < copy.subMeshCount; s2++) mesh.SetTriangles(copy.GetTriangles(s2), s2, false); Object.DestroyImmediate(copy); touched.Add(new Dictionary<string, object> { ["renderer"] = PathOf(r.transform), ["trianglesOut"] = out0, ["pass"] = pass }); continue; }
                copy.name = mesh.name + " (077 swap)";
                var ap = $"{Dir}/Meshes/batch_{Md5(PathOf(r.transform))}.asset";
                AssetDatabase.DeleteAsset(ap); AssetDatabase.CreateAsset(copy, ap); mf.sharedMesh = copy; mine.Add(copy); copies++;
                touched.Add(new Dictionary<string, object> { ["renderer"] = PathOf(r.transform), ["trianglesOut"] = out0, ["pass"] = pass });
            }
            }
            var residual = expected.Values.SelectMany(v => v).GroupBy(v => v.mass).ToDictionary(g => g.Key, g => (object)g.GroupBy(x => x.slot).ToDictionary(h => h.Key, h => (object)h.Count()));
            rep["bakedMatch"] = places.Where(p => p.Baked).ToDictionary(p => p.Mass, p => (object)$"{found[p.Mass]}/{perMass[p.Mass]}" + (degenerate.TryGetValue(p.Mass, out var dg) ? $" (+{dg} degenerate, never baked)" : ""));
            rep["unmatchedBySlot"] = residual;
            int exp = places.Where(p => p.Baked).Sum(p => perMass[p.Mass]), got = places.Where(p => p.Baked).Sum(p => found[p.Mass]);
            rep["bakedMatchTotal"] = $"{got}/{exp}";
            var bad = places.Where(p => p.Baked && p.NewId != null && found[p.Mass] < .97f * perMass[p.Mass]).Select(p => $"{p.Mass} {found[p.Mass]}/{perMass[p.Mass]}").ToList();
            if (bad.Count > 0) throw new InvalidDataException("077: baked pieces under 97 % matched (refusing the swap): " + string.Join("; ", bad));
            rep["batchesChanged"] = touched; rep["hedgeTrianglesOut"] = hedgeRemoved.ToDictionary(k => k.Key, k => (object)k.Value); rep["hedgeMaterials"] = hedgeMats.ToDictionary(k => k.Key, k => (object)k.Value);
            if (hedgeRemoved.Values.Any(v => v == 0)) throw new InvalidDataException("077: a bed's hedge was not found: " + string.Join(", ", hedgeRemoved.Select(k => $"{k.Key} {k.Value}")));
            // 3. the fade instances (CafeUpper_3): their renderers off, their render settings carried over
            foreach (var p in places.Where(p => !p.Baked))
            {
                var rs = p.OldInstance.GetComponentsInChildren<MeshRenderer>(true);
                var sm = new Dictionary<string, Material>(StringComparer.Ordinal);
                var src = KitLods.Lod0(kit.Prefab(p.OldId)).ToArray();
                foreach (var r in rs.Where(r => KitLods.IsLod0(r.transform, p.OldInstance)))
                {
                    var srcR = src.FirstOrDefault(x => x.name == r.name); if (!srcR) continue;
                    for (int i = 0; i < srcR.sharedMaterials.Length && i < r.sharedMaterials.Length; i++) if (srcR.sharedMaterials[i] && r.sharedMaterials[i]) sm.TryAdd(srcR.sharedMaterials[i].name, r.sharedMaterials[i]);
                    slotLayer.TryAdd(p.Mass, (r.gameObject.layer, r.renderingLayerMask));
                }
                slotMat[p.Mass] = sm;
                foreach (var r in rs) r.enabled = false;
                p.Trs = p.OldInstance.localToWorldMatrix;
            }
            // 4. the replacements
            var holder = new GameObject("planting077 (testbed swap, review probe)").transform;
            var made = new List<object>(); var stoneCheck = new Dictionary<string, object>();
            foreach (var p in places)
            {
                var sm = slotMat.TryGetValue(p.Mass, out var x) ? x : new Dictionary<string, Material>();
                var lay = slotLayer.TryGetValue(p.Mass, out var ly) ? ly : (0, 1u);
                if (p.NewId != null)
                {
                    var g = Group(holder, p.Mass + " -> " + p.NewId + "_TB", rows[p.NewId], models[p.NewId], p.Trs, sm, lay);
                    made.Add(Made(p.Mass, p.NewId + "_TB", g, p.Baked ? "baked dome out (exact triangle match), rebuild in" : "fade instance's renderers off, rebuild in"));
                    if (p.Baked) stoneCheck[p.Mass] = StoneMatch(g, p, kit);
                    if (Companion.TryGetValue(p.NewId, out var comp)) made.Add(Made(p.Mass, comp, Group(holder, p.Mass + " -> " + comp, rows[comp], models[comp], p.Trs, sm, lay), "companion at its parent's transform"));
                    if (p.NewId == "PlanterArc")
                    {   // GlobeLamp_TB in the arc's frame (manifest placements.at: plan x, y and z above the arc's pivot)
                        var at = J.Get(J.Get(rows["GlobeLamp_TB"].Raw, "placements"), "at"); var xy = J.P(J.Get(at, "at")); float lz = J.F(at, "z");
                        var lt = p.Trs * Matrix4x4.Translate(new Vector3(xy.x, lz, xy.y));
                        made.Add(Made(p.Mass, "GlobeLamp_TB", Group(holder, p.Mass + " -> GlobeLamp_TB", rows["GlobeLamp_TB"], models["GlobeLamp_TB"], lt, sm, lay), "one per PlanterArc, in the arc's frame"));
                    }
                }
            }
            foreach (var (bed, kerb, _) in beds)
            {
                var id = "CivicBedPlanting_" + bed; var pl = J.Get(rows[id].Raw, "placement");
                var at = J.P(J.Get(pl, "at")); float yaw = J.F(pl, "yaw"), top = kerb.Base + kerb.Height + .003f; // beds.ts: the lowest point (the soil) at the kerb top + 0.003
                float low = models[id][0].GetComponentsInChildren<MeshFilter>(true).SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v).y)).Min();
                var trs = Matrix4x4.TRS(W(at, top - low), Quaternion.Euler(0f, -yaw * Mathf.Rad2Deg, 0f), Vector3.one);
                var lay = slotLayer.Where(k => k.Key.StartsWith($"civic/civic-{bed}/", StringComparison.Ordinal)).Select(k => k.Value).DefaultIfEmpty((0, 1u)).First();
                made.Add(Made(kerb.Id, id, Group(holder, kerb.Id + " -> " + id, rows[id], models[id], trs, new Dictionary<string, Material>(), lay), $"bed level at the kerb top {top:0.###} m, hedge and tufts out"));
            }
            rep["targets"] = places.GroupBy(p => p.OldId).ToDictionary(g => g.Key, g => (object)g.Count());
            rep["placed"] = made; rep["stoneMatch"] = stoneCheck; rep["batchCopies"] = copies;
            return rep;
        }

        static bool Inside(Vector2[] ring, Vector2 p)
        {
            bool c = false;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
                if ((ring[i].y > p.y) != (ring[j].y > p.y) && p.x < (ring[j].x - ring[i].x) * (p.y - ring[i].y) / (ring[j].y - ring[i].y) + ring[i].x) c = !c;
            return c;
        }

        /// <summary>One placed LODGroup from a catalogue row: L0 then each LOD file (its models[i]), switching at the row's own lods[].screen, the last
        /// level culled by KitLods' rule (1 % under 8 m, 0.4 % above, never within half the widest play zoom); size = lodSize where the row sets it.
        /// Slots keep the probe's testbed materials; a stone slot takes the material the dome's baked stone drew in (the restyle's), by slot name.
        /// The soil objects (every material DK_Soil_TB) cast no shadow (the row's objects[].shadows off).</summary>
        static GameObject Group(Transform parent, string name, Row row, List<GameObject> files, Matrix4x4 trs, Dictionary<string, Material> slotMat, (int layer, uint mask) lay)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(trs.GetColumn(3), trs.rotation); go.transform.localScale = trs.lossyScale;
            var levels = new List<Renderer[]>();
            foreach (var f in files)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(f, go.scene); inst.transform.SetParent(go.transform, false);
                var rs = inst.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in rs)
                {
                    r.sharedMaterials = r.sharedMaterials.Select(m => m && slotMat.TryGetValue(m.name, out var bm) ? bm : m).ToArray();
                    if (r.sharedMaterials.All(m => m && m.name == "DK_Soil_TB")) r.shadowCastingMode = ShadowCastingMode.Off;
                    r.gameObject.layer = lay.layer; r.renderingLayerMask = lay.mask;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }
                levels.Add(rs);
            }
            var lg = go.AddComponent<LODGroup>(); lg.fadeMode = LODFadeMode.None;
            lg.SetLODs(levels.Select((rs, i) => new LOD(.9f - .1f * i, rs)).ToArray()); lg.RecalculateBounds();
            if (row.LodSize > 0f) lg.size = row.LodSize;
            float size = lg.size, cull = Mathf.Min(size < 8f ? .01f : .004f, .5f * size / (2f * 18.5f));
            // KitLods: a level that would only switch in below the cull never shows, so it goes (its renderers off)
            int keep = 1; while (keep < levels.Count && keep - 1 < row.Lods.Count && row.Lods[keep - 1].screen > cull) keep++;
            for (int i = keep; i < levels.Count; i++) foreach (var r in levels[i]) r.gameObject.SetActive(false);
            var lods = new List<LOD>();
            for (int i = 0; i < keep; i++) lods.Add(new LOD(i < keep - 1 ? row.Lods[i].screen : cull, levels[i]));
            if (lods.Count > 1 && lods[^1].screenRelativeTransitionHeight >= lods[^2].screenRelativeTransitionHeight) throw new InvalidDataException($"077: {name} heights not decreasing");
            lg.SetLODs(lods.ToArray());
            if (row.LodSize > 0f) lg.size = row.LodSize; // after SetLODs, which recomputes the size from the renderers
            if (row.LodSize > 0f && Mathf.Abs(lg.size - row.LodSize) > 1e-4f) throw new InvalidDataException($"077: {name} LODGroup.size {lg.size} did not take lodSize {row.LodSize}");
            return go;
        }

        static Dictionary<string, object> Made(string mass, string id, GameObject g, string how)
        {
            var lg = g.GetComponent<LODGroup>(); var lods = lg.GetLODs();
            return new Dictionary<string, object>
            {
                ["mass"] = mass, ["piece"] = id, ["how"] = how, ["size"] = R(lg.size), ["heights"] = lods.Select(l => (object)R(l.screenRelativeTransitionHeight)).ToList(),
                ["trisPerLod"] = lods.Select(l => (object)l.renderers.Sum(r => { var m = r.GetComponent<MeshFilter>().sharedMesh; return Enumerable.Range(0, m.subMeshCount).Sum(s => (long)m.GetIndexCount(s) / 3); })).ToList(),
                ["renderersPerLod"] = lods.Select(l => (object)l.renderers.Length).ToList(),
                ["shadowOff"] = lods.SelectMany(l => l.renderers).Count(r => r.shadowCastingMode == ShadowCastingMode.Off),
                ["playZoomLod"] = Pick(lg, 14f), ["position"] = new List<object> { R(g.transform.position.x), R(g.transform.position.y), R(g.transform.position.z) },
            };
        }

        static string Pick(LODGroup lg, float ortho)
        {
            float rel = lg.size / (2f * ortho) * QualitySettings.lodBias; var lods = lg.GetLODs();
            for (int i = 0; i < lods.Length; i++) if (rel >= lods[i].screenRelativeTransitionHeight) return $"L{i} ({rel:0.###})";
            return $"culled ({rel:0.###})";
        }

        /// <summary>The rebuild's L0 stonework against the dome's baked stone it replaced: the share of its stone triangles that land on a removed one (1 mm).</summary>
        static string StoneMatch(GameObject g, Place p, KitPlacer kit)
        {
            var prefab = kit.Prefab(p.OldId); var root = prefab.transform; var old = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mr in KitLods.Lod0(prefab))
            {
                var mesh = mr.GetComponent<MeshFilter>().sharedMesh; var mats = mr.sharedMaterials; var mx = p.Trs * root.worldToLocalMatrix * mr.transform.localToWorldMatrix; var vs = mesh.vertices;
                for (int s = 0; s < mesh.subMeshCount; s++) { if (Plantish(mats[Mathf.Min(s, mats.Length - 1)].name)) continue; var t = mesh.GetTriangles(s); for (int k = 0; k < t.Length; k += 3) old.Add(Key(mx.MultiplyPoint3x4(vs[t[k]]), mx.MultiplyPoint3x4(vs[t[k + 1]]), mx.MultiplyPoint3x4(vs[t[k + 2]]))); }
            }
            int n = 0, hit = 0;
            var l0 = g.GetComponent<LODGroup>().GetLODs()[0].renderers;
            foreach (var r in l0)
            {
                var mesh = r.GetComponent<MeshFilter>().sharedMesh; var mats = r.sharedMaterials; var mx = r.transform.localToWorldMatrix; var vs = mesh.vertices;
                for (int s = 0; s < mesh.subMeshCount; s++) { var mn = mats[Mathf.Min(s, mats.Length - 1)].name; if (mn.EndsWith("_TB", StringComparison.Ordinal)) continue; var t = mesh.GetTriangles(s); for (int k = 0; k < t.Length; k += 3) { n++; if (old.Contains(Key(mx.MultiplyPoint3x4(vs[t[k]]), mx.MultiplyPoint3x4(vs[t[k + 1]]), mx.MultiplyPoint3x4(vs[t[k + 2]])))) hit++; } }
            }
            return n == 0 ? "no stone" : $"{hit}/{n} rebuild stone triangles on the dome's ({old.Count} dome stone)";
        }
        static bool Plantish(string n) => n.StartsWith("DK_Leaf_", StringComparison.Ordinal) || n.StartsWith("DK_Bloom_", StringComparison.Ordinal) || n.StartsWith("DK_Flower_", StringComparison.Ordinal) || n == "DK_GroundCover";

        // ================================================================ helpers
        static object R(float f) => Math.Round(f, 5);
        static string Sha(byte[] b) { using var s = SHA256.Create(); return string.Concat(s.ComputeHash(b).Select(x => x.ToString("x2"))); }
        static string Md5(string s) { using var m = MD5.Create(); return string.Concat(m.ComputeHash(Encoding.UTF8.GetBytes(s)).Select(b => b.ToString("x2"))); }
        static string Json(object o) { var sb = new StringBuilder(); W(sb, o, 0); return sb + "\n"; }
        static void W(StringBuilder sb, object o, int d)
        {
            string pad = new(' ', d + 1), end = new(' ', d);
            switch (o)
            {
                case null: sb.Append("null"); break;
                case string s: sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")).Append('"'); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case float f: sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); break;
                case double x: sb.Append(x.ToString("R", CultureInfo.InvariantCulture)); break;
                case IDictionary dict: { sb.Append("{\n"); int i = 0; foreach (DictionaryEntry kv in dict) { if (i++ > 0) sb.Append(",\n"); sb.Append(pad); W(sb, kv.Key.ToString(), d + 1); sb.Append(": "); W(sb, kv.Value, d + 1); } sb.Append('\n').Append(end).Append('}'); break; }
                case IEnumerable list:
                    {
                        var items = list.Cast<object>().ToList();
                        if (items.All(x => x is not IDictionary && (x is string || x is not IEnumerable))) { sb.Append('['); for (int k = 0; k < items.Count; k++) { if (k > 0) sb.Append(", "); W(sb, items[k], d + 1); } sb.Append(']'); }
                        else { sb.Append("[\n"); for (int k = 0; k < items.Count; k++) { if (k > 0) sb.Append(",\n"); sb.Append(pad); W(sb, items[k], d + 1); } sb.Append('\n').Append(end).Append(']'); }
                        break;
                    }
                default: sb.Append(Convert.ToString(o, CultureInfo.InvariantCulture)); break;
            }
        }
    }
}
#endif
