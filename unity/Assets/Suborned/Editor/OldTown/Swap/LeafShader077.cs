#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.World;
using Suborned.World.Review;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 077 leaf shader (Latch). A sibling of DecoKit/Lit Cutaway, DecoKit/Lit Cutaway Leaf (DK_LitCutawayLeaf.shader, generated from it
    /// by briefs/077/shader/make_leaf_hlsl.py; the original is never edited), with two opt-in material keywords for the testbed's leaves and petals
    /// (DK_LeafForward/GBuffer/DepthNormals/Meta.hlsl): _DK_VERTEX_COLOR (albedo x vertex colour) and _DK_TWO_SIDED (back faces flip the normal
    /// and tangent frame); the gloss is a mask map derived here at build time from the testbed's ORM (A = 1 - k x ORM.g, k = the testbed's
    /// roughness factor). Three batch steps, none of which saves OldTown.unity or HarbourDistrict.unity (both are opened read-only):
    ///   Proof  -leafTag &lt;tag&gt; [-leafCompare &lt;tag&gt;]: the compiled-variant manifest of DecoKit/Lit Cutaway for every keyword set an existing
    ///          material uses, and captures of the swap (OldTown.unity: play look and presentation look) and our district (HarbourDistrict.unity,
    ///          day and night), with a pixel diff against an earlier tag. Out: out/scratch/latch/077shader/proof/&lt;tag&gt;/.
    ///   Build: the isolated review scene Assets/Scenes/probe-leafshader077.unity (git-ignored): the swap scene's cameras, sun, volumes and
    ///          nearby reflection probes only, a paving quad, Vela's PlanterRun_4_A_TB and PlanterArc_TB (imported as LODGroups) on their kit
    ///          stonework at the café square, our night light read from HarbourDistrict's LightPhase into night.json.
    ///   Review: captures (play look / presentation look x day / night x shader on / off x play zoom yaw 30 and 210 and close) into
    ///          briefs/077/shader/captures, and the numbers (SRP Batcher, SetPass estimate, draws, LODs, imported vertex colours, compile
    ///          messages) into briefs/077/shader/numbers.json.</summary>
    public static class LeafShader077
    {
        const string ShaderPath = "Assets/Suborned/Shaders/DecoKit/DK_LitCutaway.shader";
        const string ShaderName = "DecoKit/Lit Cutaway";
        const string LeafShaderPath = "Assets/Suborned/Shaders/DecoKit/DK_LitCutawayLeaf.shader";
        const string LeafShaderName = "DecoKit/Lit Cutaway Leaf";
        const string Dir = "Assets/Scenes/probe-leafshader077";
        const string ScenePath = "Assets/Scenes/probe-leafshader077.unity";
        const string SwapScene = "Assets/Scenes/OldTown.unity";
        const string StudyName = "DK street study camera";
        static readonly string[] LeafKeywords = { "_DK_VERTEX_COLOR", "_DK_TWO_SIDED" };
        static readonly Vector2 CafePlan = new(-170.5f, -227.5f), OfficePlan = new(-160f, -281f);
        static string Repo => HarbourWorldBuilder.RepoRoot;
        static string Arg(string k, string d) => DecoKitStage.Arg(k, d);
        static string Scratch => Path.Combine(Repo, "out/scratch/latch/077shader");
        static string Evidence => Path.Combine(Repo, Arg("leafOut", "briefs/077/shader"));

        // ================================================================ proof: existing materials unchanged
        public static void Proof() => HarbourWorldBuilder.Batch(() =>
        {
            var t0 = DateTime.UtcNow;
            string tag = Arg("leafTag", "before-a"), dir = Path.Combine(Scratch, "proof", tag);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ?? throw new FileNotFoundException(ShaderPath);
            var leafShader = AssetDatabase.LoadAssetAtPath<Shader>(LeafShaderPath);
            var report = new Dictionary<string, object> { ["tag"] = tag, ["shader"] = ShaderFacts(shader), ["unity"] = Application.unityVersion };
            if (leafShader) report["leafShader"] = ShaderFacts(leafShader, LeafShaderPath);
            bool sibling = Arg("leafSibling", "off") == "on"; report["existingMaterialsOnTheSibling"] = sibling;
            var inv = MaterialInventory(shader, out var sets);
            report["materials"] = inv;
            if (Arg("leafCompile", "on") == "on") report["compiled"] = CompileManifest(shader, sets);
            if (leafShader && Arg("leafCompile", "on") == "on")
            {
                report["siblingCompiled"] = CompileManifest(leafShader, sets);
                report["leafCompiled"] = CompileManifest(leafShader, new List<string[]> { new[] { "_DK_TWO_SIDED", "_DK_VERTEX_COLOR", "_METALLICSPECGLOSSMAP", "_NORMALMAP" }, new[] { "_DK_VERTEX_COLOR", "_METALLICSPECGLOSSMAP", "_NORMALMAP" }, new[] { "_DK_TWO_SIDED", "_METALLICSPECGLOSSMAP", "_NORMALMAP" } });
            }
            var caps = new List<object>();
            using (var session = new RenderSession())
            {
                if (Arg("leafSwap", "on") == "on") caps.AddRange(SwapCaptures(dir, sibling ? leafShader : null));
                if (Arg("leafHarbour", "on") == "on") caps.AddRange(HarbourCaptures(session, dir, sibling ? leafShader : null));
            }
            report["captures"] = caps;
            var cmp = Arg("leafCompare", "");
            if (cmp.Length > 0) report["compare"] = Compare(dir, Path.Combine(Scratch, "proof", cmp), cmp, report);
            report["seconds"] = Math.Round((DateTime.UtcNow - t0).TotalSeconds, 1);
            File.WriteAllText(Path.Combine(dir, "proof.json"), Json(report));
            Debug.Log($"[077 leaf] proof {tag}: {caps.Count} captures, {(cmp.Length > 0 ? "compared with " + cmp : "no compare")} in {report["seconds"]} s -> {dir}");
        });

        static Dictionary<string, object> ShaderFacts(Shader sh, string path = ShaderPath)
        {
            var srp = SrpBatcher(sh);
            return new Dictionary<string, object>
            {
                ["name"] = sh.name, ["path"] = path, ["sha256"] = FileSha(path), ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["includes"] = new[] { "DK_Cutaway.hlsl", "DK_SurfaceDetail.hlsl", "DK_Leaf.hlsl", "DK_LeafForward.hlsl", "DK_LeafGBuffer.hlsl", "DK_LeafDepthNormals.hlsl", "DK_LeafMeta.hlsl" }
                    .Where(f => File.Exists(Path.Combine(Repo, "unity/Assets/Suborned/Shaders/DecoKit", f))).ToDictionary(f => f, f => (object)FileSha("Assets/Suborned/Shaders/DecoKit/" + f)),
                ["hasError"] = ShaderUtil.ShaderHasError(sh), ["supported"] = sh.isSupported,
                ["messages"] = ShaderUtil.GetShaderMessages(sh).Select(m => (object)$"{m.severity}: {m.message} ({m.file}:{m.line}) [{m.platform}]").ToList(),
                ["srpBatcher"] = srp, ["passes"] = sh.passCount,
                ["keywordSpace"] = sh.keywordSpace.keywords.Select(k => (object)$"{k.name}{(k.type != ShaderKeywordType.UserDefined ? " (" + k.type + ")" : "")}{(k.isOverridable ? " overridable" : "")}").ToList(),
            };
        }

        static string SrpBatcher(Shader sh)
        {
            var code = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var why = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            try { var c = Convert.ToInt32(code.Invoke(null, new object[] { sh, 0 })); return $"code {c} ({why?.Invoke(null, new object[] { sh, 0, c })})"; }
            catch (Exception e) { return "unavailable: " + e.GetBaseException().Message; }
        }

        /// <summary>Every material asset on the shader, its keyword set; the distinct sets (the variants existing materials can draw).</summary>
        static Dictionary<string, object> MaterialInventory(Shader sh, out List<string[]> sets)
        {
            var bySet = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
            int n = 0, leaf = 0; var cull = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(Dir + "/", StringComparison.Ordinal)) continue; // the review's own materials are not existing ones
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m || m.shader != sh) continue;
                n++;
                var kw = m.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal).ToArray();
                if (kw.Any(k => LeafKeywords.Contains(k))) leaf++;
                var key = string.Join(" ", kw);
                if (!bySet.TryGetValue(key, out var l)) bySet[key] = l = new List<string>();
                l.Add(path);
                var c = m.HasProperty("_Cull") ? $"cull {m.GetFloat("_Cull"):0}" : "cull -";
                cull[c] = cull.TryGetValue(c, out var cc) ? cc + 1 : 1;
            }
            sets = bySet.Keys.Select(k => k.Length == 0 ? new string[0] : k.Split(' ')).ToList();
            return new Dictionary<string, object>
            {
                ["count"] = n, ["withLeafKeywords"] = leaf, ["byCull"] = cull.ToDictionary(k => k.Key, k => (object)k.Value),
                ["keywordSets"] = bySet.Select(kv => (object)new Dictionary<string, object> { ["keywords"] = kv.Key, ["materials"] = kv.Value.Count, ["example"] = kv.Value[0] }).ToList(),
            };
        }

        /// <summary>Compiles, for Metal / StandaloneOSX, each pass's vertex and fragment program for every existing keyword set, with no global
        /// keywords and with a play set, and hashes the bytes Unity would ship.</summary>
        static Dictionary<string, object> CompileManifest(Shader sh, List<string[]> sets)
        {
            var t0 = DateTime.UtcNow;
            var globals = new Dictionary<string, string[]>
            {
                ["ForwardLit"] = new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_CLUSTER_LIGHT_LOOP", "_SHADOWS_SOFT", "_REFLECTION_PROBE_BLENDING", "_REFLECTION_PROBE_BOX_PROJECTION", "_DK_CUT" },
                ["ShadowCaster"] = new[] { "_DK_CUT" }, ["GBuffer"] = new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT", "_CLUSTER_LIGHT_LOOP" },
                ["DepthOnly"] = new[] { "_DK_CUT" }, ["DepthNormals"] = new[] { "_DK_CUT" }, ["Meta"] = new string[0],
            };
            var data = ShaderUtil.GetShaderData(sh);
            var sub = data.GetSerializedSubshader(0);
            var rows = new List<object>(); int ok = 0, fail = 0; var all = new StringBuilder();
            for (int p = 0; p < sub.PassCount; p++)
            {
                var pass = sub.GetPass(p);
                if (!globals.TryGetValue(pass.Name, out var g)) continue;
                foreach (var set in sets)
                    foreach (var gl in g.Length == 0 ? new[] { g } : new[] { new string[0], g })
                        foreach (var st in new[] { ShaderType.Vertex, ShaderType.Fragment })
                        {
                            var kws = set.Concat(gl).Distinct().ToArray();
                            var r = pass.CompileVariant(st, kws, ShaderCompilerPlatform.Metal, BuildTarget.StandaloneOSX);
                            string h = r.Success ? Sha(r.ShaderData) : "FAILED";
                            if (r.Success) ok++; else fail++;
                            var id = $"{pass.Name}|{st}|{string.Join(" ", set)}|{string.Join(" ", gl)}";
                            all.Append(id).Append('=').Append(h).Append('\n');
                            rows.Add(new Dictionary<string, object> { ["pass"] = pass.Name, ["stage"] = st.ToString(), ["material"] = string.Join(" ", set), ["global"] = string.Join(" ", gl), ["bytes"] = r.Success ? r.ShaderData.Length : 0, ["sha256"] = h,
                                ["errors"] = r.Messages.Count(m => m.severity == ShaderCompilerMessageSeverity.Error), ["firstError"] = r.Messages.Where(m => m.severity == ShaderCompilerMessageSeverity.Error).Select(m => $"{m.message} ({m.file}:{m.line})").FirstOrDefault() ?? "" });
                        }
            }
            Debug.Log($"[077 leaf] compiled {ok} programs ({fail} failed) in {(DateTime.UtcNow - t0).TotalSeconds:0} s");
            return new Dictionary<string, object> { ["platform"] = "Metal / StandaloneOSX", ["programs"] = ok, ["failed"] = fail, ["manifestSha256"] = Sha(Encoding.UTF8.GetBytes(all.ToString())), ["rows"] = rows, ["seconds"] = Math.Round((DateTime.UtcNow - t0).TotalSeconds, 1) };
        }

        /// <summary>The swap at the café square and the Office front: the play camera (our play look) at yaw 30 and 210, the study camera (the
        /// presentation look) at the same play poses and at street level.</summary>
        static IEnumerable<object> SwapCaptures(string dir, Shader sibling)
        {
            EditorSceneManager.OpenScene(SwapScene, OpenSceneMode.Single);
            if (sibling) Sibling(sibling);
            DecoKitHarbour.Ocean();
            var study = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).First(c => c.name == StudyName);
            study.enabled = true; study.farClipPlane = 2500f; study.nearClipPlane = .05f; study.useOcclusionCulling = false;
            var play = SwapPlayLook.Find() ?? throw new InvalidDataException("077: the swap scene has no play camera (second build)");
            Object.DestroyImmediate(DecoKitStage.Render(study, 640, 360));
            var rows = new List<object>();
            foreach (var (id, f) in new[] { ("cafe", CafePlan), ("office", OfficePlan) })
                foreach (var yaw in new[] { 30f, 210f })
                {
                    SwapShots.Play(play, f, yaw); rows.Add(Cap(play, dir, $"swap-play-{id}-y{yaw:000}", 200f));
                    SwapShots.Play(study, f, yaw); rows.Add(Cap(study, dir, $"swap-presentation-{id}-y{yaw:000}", 200f));
                }
            study.orthographic = false; study.fieldOfView = 55f;
            study.transform.position = OldTownSwap.Old3(new Vector2(-176f, -244f), 1.7f); study.transform.LookAt(OldTownSwap.Old3(new Vector2(-166f, -232f), .6f));
            rows.Add(Cap(study, dir, "swap-presentation-cafe-street", 120f));
            return rows;
        }

        static object Cap(Camera cam, string dir, string name, float shadow)
        {
            Texture2D px = null;
            DecoKitStage.WithShadows(shadow, () => px = DecoKitStage.Render(cam, 1920, 1080, 2));
            return Keep(px, dir, name, true);
        }

        static object Keep(Texture2D px, string dir, string name, bool destroy)
        {
            var raw = px.GetRawTextureData<byte>().ToArray();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), px.EncodeToPNG());
            var row = new Dictionary<string, object> { ["name"] = name, ["size"] = $"{px.width}x{px.height}", ["format"] = px.format.ToString(), ["pixelSha256"] = Sha(raw) };
            if (destroy) Object.DestroyImmediate(px);
            return row;
        }

        /// <summary>Our district through its own play camera (Baseline072's pose): the Office front and the cell's opening at yaw 30 and 210, and
        /// the Office front at night (LightPhase.Set(true)), people off.</summary>
        static IEnumerable<object> HarbourCaptures(RenderSession session, string dir, Shader sibling)
        {
            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            if (sibling) Sibling(sibling);
            var cam = (GameObject.Find("cameras/play") ?? throw new InvalidDataException("077: no cameras/play")).GetComponent<Camera>();
            var ts = cam.GetComponent<TacticalShadows>(); var fade = cam.GetComponent<ArchitectureFade>();
            var people = GameObject.Find("people"); if (people) people.SetActive(false);
            After072.AbPlayPose(cam, ts, fade, OfficePlan, 30f, 160f);
            session.Grab(cam, 64, 64);
            var rows = new List<object>();
            foreach (var (id, f) in new[] { ("office", OfficePlan), ("cafe", CafePlan) })
                foreach (var yaw in new[] { 30f, 210f })
                { After072.AbPlayPose(cam, ts, fade, f, yaw, 160f); rows.Add(Keep(session.Grab(cam, 1920, 1080), dir, $"harbour-play-{id}-y{yaw:000}", false)); }
            var lp = Object.FindAnyObjectByType<Suborned.Play.LightPhase>(FindObjectsInactive.Include);
            if (lp)
            {
                lp.Set(true); After072.AbPlayPose(cam, ts, fade, OfficePlan, 210f, 160f); session.Grab(cam, 64, 64);
                rows.Add(Keep(session.Grab(cam, 1920, 1080), dir, "harbour-play-office-y210-night", false));
                lp.Set(false);
            }
            return rows;
        }

        /// <summary>The sibling test: every renderer's DecoKit/Lit Cutaway material swapped (in memory, never saved) for a copy on DecoKit/Lit Cutaway Leaf
        /// with the same keywords, so the frames show whether the sibling without the leaf keywords draws exactly as the original.</summary>
        static void Sibling(Shader leaf)
        {
            var copies = new Dictionary<Material, Material>(); int n = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                var ms = r.sharedMaterials; bool any = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (!ms[i] || ms[i].shader.name != ShaderName) continue;
                    if (!copies.TryGetValue(ms[i], out var c)) { c = new Material(ms[i]) { name = ms[i].name }; c.shader = leaf; foreach (var k in ms[i].shaderKeywords) c.EnableKeyword(k); c.renderQueue = ms[i].renderQueue; copies[ms[i]] = c; }
                    ms[i] = c; any = true;
                }
                if (any) { r.sharedMaterials = ms; n++; }
            }
            Debug.Log($"[077 leaf] sibling test: {copies.Count} materials on {n} renderers now draw with {leaf.name}");
        }

        static Dictionary<string, object> Compare(string dir, string other, string otherTag, Dictionary<string, object> report)
        {
            var o = new Dictionary<string, object> { ["against"] = otherTag };
            var rows = new List<object>(); int same = 0, differ = 0;
            foreach (var png in Directory.GetFiles(dir, "*.png").OrderBy(p => p, StringComparer.Ordinal))
            {
                var name = Path.GetFileName(png); var op = Path.Combine(other, name);
                if (!File.Exists(op)) { rows.Add(new Dictionary<string, object> { ["name"] = name, ["missing"] = true }); continue; }
                var a = new Texture2D(2, 2); a.LoadImage(File.ReadAllBytes(png)); var b = new Texture2D(2, 2); b.LoadImage(File.ReadAllBytes(op));
                var pa = a.GetPixels32(); var pb = b.GetPixels32(); int n = 0, max = 0;
                int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
                if (pa.Length == pb.Length)
                    for (int i = 0; i < pa.Length; i++)
                    {
                        int d = Math.Max(Math.Abs(pa[i].r - pb[i].r), Math.Max(Math.Abs(pa[i].g - pb[i].g), Math.Abs(pa[i].b - pb[i].b)));
                        if (d == 0) continue;
                        n++; max = Math.Max(max, d); int x = i % a.width, y = i / a.width; x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y);
                    }
                if (n == 0 && pa.Length == pb.Length) same++; else differ++;
                rows.Add(new Dictionary<string, object> { ["name"] = name, ["pixels"] = pa.Length, ["differing"] = n, ["maxChannelDiff"] = max, ["bbox"] = n > 0 ? $"{x0},{y0}..{x1},{y1}" : "-" });
                Object.DestroyImmediate(a); Object.DestroyImmediate(b);
            }
            o["identical"] = same; o["differing"] = differ; o["rows"] = rows;
            var mine = report.TryGetValue("compiled", out var c) ? ((Dictionary<string, object>)c)["manifestSha256"] as string : null;
            var theirsFile = Path.Combine(other, "proof.json");
            if (mine != null && File.Exists(theirsFile))
            {
                var t = Suborned.World.Json.Parse(File.ReadAllText(theirsFile));
                var tc = J.Get(t, "compiled");
                o["compiledManifestSame"] = tc != null && J.S(tc, "manifestSha256") == mine;
                if (tc != null)
                {
                    var before = new Dictionary<string, string>(); foreach (var r in J.A(tc, "rows")) before[$"{J.S(r, "pass")}|{J.S(r, "stage")}|{J.S(r, "material")}|{J.S(r, "global")}"] = J.S(r, "sha256");
                    var now = new Dictionary<string, string>(); foreach (var r in ((List<object>)((Dictionary<string, object>)c)["rows"]).Cast<Dictionary<string, object>>()) now[$"{r["pass"]}|{r["stage"]}|{r["material"]}|{r["global"]}"] = (string)r["sha256"];
                    o["compiledProgramsCompared"] = now.Count(kv => before.ContainsKey(kv.Key));
                    o["compiledProgramsChanged"] = now.Where(kv => before.TryGetValue(kv.Key, out var bh) && bh != kv.Value).Select(kv => (object)kv.Key).ToList();
                }
            }
            return o;
        }

        // ================================================================ build: the isolated review scene
        sealed class Piece { public string Id, Fbx, Stone; public float LeafM; public float[] Px; public Vector3 At; public float Yaw; public string Json, JsonKey; }

        static readonly Piece[] Pieces =
        {
            // testbed garden-runtime.js: a projected 0.4 m leaf span, gameplay < 18 px, near < 32, close < 72 (troughs/metrics.json lod_plan)
            new() { Id = "PlanterRun_4_A_TB", Fbx = "out/decokit/troughs077/Models/DK_PlanterRun_4_A_TB.fbx", Stone = "Assets/Suborned/Kit/Models/Plaza/DK_PlanterRun_4_A.fbx",
                    LeafM = .4f, Px = new[] { 72f, 32f, 18f }, At = new Vector3(-3.2f, 0f, 2.2f), Yaw = 0f, Json = "out/decokit/troughs077/pieces.json", JsonKey = "PlanterRun_4_A_TB" },
            // fountain-garden.js updateLOD: a projected 0.55 m leaf, close >= 44 px, near >= 24, gameplay below
            new() { Id = "PlanterArc_TB", Fbx = "out/decokit/troughs077/Models/DK_PlanterArc_TB.fbx", Stone = "Assets/Suborned/Kit/Models/Plaza/DK_PlanterArc.fbx",
                    LeafM = .55f, Px = new[] { 44f, 24f }, At = new Vector3(3.4f, 0f, -1.6f), Yaw = 0f, Json = "out/decokit/troughs077/domes_pieces.json", JsonKey = "PlanterArc_TB" },
        };

        // testbed cafe-planting.js: foliage roughness .49 x ORM.g, petals .64 x ORM.g, soil 1 x ORM.g; atlas rows: leaves 0-3 on top (v .5..1), petals 4-7 below
        const float LeafK = .49f, PetalK = .64f, SoilK = 1f;
        const string Art = "the-suborned-store/testbed-art/cafe-plants";
        static readonly (string file, string sha)[] ArtFiles =
        {
            ("botanical-atlas-v2.png", "da2573c3f98e3e502ad1a6acd9e786ae43ae0961f0b8dd7a80dc1a75fa2f9bbe"), ("botanical-normal-v2.png", "9c95555b3906cf72a04fe28927dad2a6e6b0b97d84e770261f55819c65a29b79"),
            ("botanical-orm-v2.png", "c3574a250b1ea075f1b80b9c8fe51b805ab3e0dcb819ce5644d97c681b8d3130"), ("soil-base.png", "1e90af1a2f00040f49999e3b4b72b9043ae19aecd39bdabb0aae4ad4d1ce8f55"),
            ("soil-normal.png", "aed2d8f06c5fd2bc4d4ae7c93b309e4710446669507951941f07bcf2be03bc73"), ("soil-orm.png", "ff8db1f5b19b743c707284aedfb8536baff21f9df499675e0cfc422308e8c8cb"),
        };

        static bool Plant(string n) => n.StartsWith("DK_Leaf_", StringComparison.Ordinal) || n.StartsWith("DK_Bloom_", StringComparison.Ordinal) || n.StartsWith("DK_Flower_", StringComparison.Ordinal) || n == "DK_GroundCover";

        public static void Build() => HarbourWorldBuilder.Batch(() =>
        {
            var log = new Dictionary<string, object> { ["status"] = "review only; git-ignored scene; nothing placed in the game" };
            foreach (var d in new[] { Dir, Dir + "/Models", Dir + "/Textures", Dir + "/Materials", Dir + "/Meshes" })
                Directory.CreateDirectory(Path.Combine(Repo, "unity", d));
            // 1. art: copied as is (sha256 checked); the two masks derived from the ORMs
            string artRoot = Path.Combine(Path.GetDirectoryName(Repo), Art);
            var art = new Dictionary<string, object>();
            foreach (var (f, sha) in ArtFiles)
            {
                var src = Path.Combine(artRoot, f); var got = Sha(File.ReadAllBytes(src));
                if (got != sha) throw new InvalidDataException($"077: {f} sha256 {got}, expected {sha}");
                if (!f.Contains("-orm")) File.Copy(src, Path.Combine(Repo, "unity", Dir, "Textures", f), true);
                art[f] = sha;
            }
            log["art"] = art;
            log["masks"] = new Dictionary<string, object>
            {
                ["DK_Botanical_Mask_TB.png"] = Mask(Path.Combine(artRoot, "botanical-orm-v2.png"), Dir + "/Textures/DK_Botanical_Mask_TB.png", v => v >= .5f ? LeafK : PetalK, "leaves (rows 0-3, v >= .5) 1 - .49 g; petals (rows 4-7, v < .5) 1 - .64 g"),
                ["DK_Soil_Mask_TB.png"] = Mask(Path.Combine(artRoot, "soil-orm.png"), Dir + "/Textures/DK_Soil_Mask_TB.png", _ => SoilK, "1 - g"),
            };
            // 2. Vela's FBX candidates, sha256 against her pieces.json
            var fbx = new Dictionary<string, object>();
            foreach (var p in Pieces)
            {
                var src = Path.Combine(Repo, p.Fbx); var got = Sha(File.ReadAllBytes(src));
                var want = J.S(J.Get(J.Get(J.Get(Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(Repo, p.Json))), "pieces"), p.JsonKey), "fbx"), "sha256");
                if (got != want) throw new InvalidDataException($"077: {p.Fbx} sha256 {got}, {p.Json} says {want}");
                File.Copy(src, Path.Combine(Repo, "unity", Dir, "Models", Path.GetFileName(p.Fbx)), true);
                fbx[p.Id] = new Dictionary<string, object> { ["source"] = p.Fbx, ["sha256"] = got };
            }
            log["fbx"] = fbx;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            // 3. texture import: Vela's spec (troughs/metrics.json textures): BC7 colour and mask, BC5 normals, NPOT to nearest, full mips, aniso 8 as the testbed
            Tex("botanical-atlas-v2.png", true, false, TextureWrapMode.Clamp); Tex("botanical-normal-v2.png", false, true, TextureWrapMode.Clamp); Tex("DK_Botanical_Mask_TB.png", false, false, TextureWrapMode.Clamp);
            Tex("soil-base.png", true, false, TextureWrapMode.Repeat); Tex("soil-normal.png", false, true, TextureWrapMode.Repeat); Tex("DK_Soil_Mask_TB.png", false, false, TextureWrapMode.Repeat);
            // 4. materials (cafe-planting.js): leaf #afbea1, petal white, both cull off with the leaf keywords; soil cull back, no shadow cast
            Texture2D T(string f) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/Textures/{f}") ?? throw new FileNotFoundException(f);
            var leaf = Mat("DK_Leaf_TB", T("botanical-atlas-v2.png"), ColorUtility.TryParseHtmlString("#afbea1", out var lc) ? lc : Color.white, T("botanical-normal-v2.png"), .16f, T("DK_Botanical_Mask_TB.png"), true);
            var petal = Mat("DK_Petal_TB", T("botanical-atlas-v2.png"), Color.white, T("botanical-normal-v2.png"), .16f, T("DK_Botanical_Mask_TB.png"), true);
            var soil = Mat("DK_Soil_TB", T("soil-base.png"), Color.white, T("soil-normal.png"), .4f, T("DK_Soil_Mask_TB.png"), false);
            var byName = new Dictionary<string, Material> { ["DK_Leaf_TB"] = leaf, ["DK_Petal_TB"] = petal, ["DK_Soil_TB"] = soil };
            // 5. model import: LODGroups from the _LODn names, normals as authored, Mikk tangents, the three materials remapped
            foreach (var p in Pieces)
            {
                var path = $"{Dir}/Models/{Path.GetFileName(p.Fbx)}";
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                mi.globalScale = 1f; mi.useFileScale = true; mi.importCameras = false; mi.importLights = false; mi.importVisibility = false;
                mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.importBlendShapes = false; mi.importConstraints = false;
                mi.meshCompression = ModelImporterMeshCompression.Off; mi.isReadable = true; mi.optimizeMeshPolygons = true; mi.optimizeMeshVertices = true; mi.weldVertices = true;
                mi.importNormals = ModelImporterNormals.Import; mi.importTangents = ModelImporterTangents.CalculateMikk; mi.generateSecondaryUV = false; mi.addCollider = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
                foreach (var kv in byName) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                mi.SaveAndReimport();
            }
            // 6. the review scene from the swap's own look: open the swap, keep its cameras, directional lights, volumes and the reflection probes
            //    near the café, read our night from HarbourDistrict (read only), drop everything else, save under the probe name
            var scene = EditorSceneManager.OpenScene(SwapScene, OpenSceneMode.Single);
            var night = ReadNight();
            File.WriteAllText(Path.Combine(Repo, "unity", Dir, "night.json"), Json(night));
            var centre = OldTownSwap.Old3(CafePlan, 0f);
            var keep = new HashSet<GameObject>();
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include)) if (c.name == StudyName || c.name == SwapPlayLook.Name) keep.Add(c.gameObject);
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include)) if (l.type == LightType.Directional) keep.Add(l.gameObject);
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)) keep.Add(v.gameObject);
            foreach (var f in Object.FindObjectsByType<LensFlareComponentSRP>(FindObjectsInactive.Include)) keep.Add(f.gameObject);
            int probesKept = 0;
            foreach (var r in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include))
                if (Vector2.Distance(new Vector2(r.transform.position.x, r.transform.position.z), new Vector2(centre.x, centre.z)) < 90f) { keep.Add(r.gameObject); probesKept++; }
            foreach (var g in keep.ToList()) { bool under = false; for (var t = g.transform.parent; t; t = t.parent) if (keep.Contains(t.gameObject)) under = true; if (!under && g.transform.parent) g.transform.SetParent(null, true); }
            var keptNames = new List<object>();
            foreach (var r in scene.GetRootGameObjects()) if (!keep.Contains(r)) Object.DestroyImmediate(r); else keptNames.Add(r.name);
            log["kept"] = keptNames; log["reflectionProbesKept"] = probesKept;
            // ground: one paving quad (the kit's pale limestone paving), 2 m a tile
            var ground = new GameObject("review ground (DK_Ground_PaveLimestone)");
            var gm = new Mesh { name = "review-ground" };
            const float G = 120f;
            gm.vertices = new[] { new Vector3(-G, 0, -G), new Vector3(-G, 0, G), new Vector3(G, 0, G), new Vector3(G, 0, -G) };
            gm.uv = gm.vertices.Select(v => new Vector2(v.x, v.z) * .5f).ToArray(); gm.triangles = new[] { 0, 1, 2, 0, 2, 3 }; gm.RecalculateNormals(); gm.RecalculateTangents(); gm.RecalculateBounds();
            AssetDatabase.CreateAsset(gm, $"{Dir}/Meshes/review-ground.asset");
            ground.AddComponent<MeshFilter>().sharedMesh = gm;
            ground.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Suborned/Kit/Materials/DK_Ground_PaveLimestone.mat");
            ground.transform.position = centre;
            // the pieces: the kit's stonework (its non-plant slots, DK_Limestone as the civic restyle's DK_Limestone_Tex) and Vela's planting at the same transform
            var root = new GameObject("leaf077 review").transform;
            var tex = AssetDatabase.LoadAssetAtPath<Material>("Assets/Suborned/Kit/Materials/DK_Limestone_Tex.mat");
            var pieceRows = new List<object>();
            foreach (var p in Pieces)
            {
                var holder = new GameObject(p.Id).transform; holder.SetParent(root, false);
                holder.SetPositionAndRotation(centre + p.At, Quaternion.Euler(0f, p.Yaw, 0f));
                var stoneSrc = AssetDatabase.LoadAssetAtPath<GameObject>(p.Stone) ?? throw new FileNotFoundException(p.Stone);
                var sr = stoneSrc.GetComponentsInChildren<MeshRenderer>(true).OrderBy(r => r.name, StringComparer.Ordinal).First();
                var sm = sr.GetComponent<MeshFilter>().sharedMesh; var smats = sr.sharedMaterials;
                var keepSlots = Enumerable.Range(0, sm.subMeshCount).Where(i => !Plant(smats[i].name)).ToArray();
                var stone = new Mesh { name = p.Id + "-stone", indexFormat = sm.indexFormat };
                stone.vertices = sm.vertices; stone.normals = sm.normals; stone.uv = sm.uv; if (sm.tangents.Length > 0) stone.tangents = sm.tangents; if (sm.colors.Length > 0) stone.colors = sm.colors;
                stone.subMeshCount = keepSlots.Length; for (int i = 0; i < keepSlots.Length; i++) stone.SetTriangles(sm.GetTriangles(keepSlots[i]), i, false);
                stone.RecalculateBounds();
                AssetDatabase.CreateAsset(stone, $"{Dir}/Meshes/{p.Id}-stone.asset");
                var sgo = new GameObject("stonework (" + Path.GetFileNameWithoutExtension(p.Stone) + ", non-plant slots)");
                sgo.transform.SetParent(holder, false); sgo.transform.localPosition = sr.transform.position - stoneSrc.transform.position; sgo.transform.localRotation = sr.transform.rotation; sgo.transform.localScale = sr.transform.lossyScale;
                sgo.AddComponent<MeshFilter>().sharedMesh = stone;
                sgo.AddComponent<MeshRenderer>().sharedMaterials = keepSlots.Select(i => smats[i].name == "DK_Limestone" && tex ? tex : smats[i]).ToArray();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/Models/{Path.GetFileName(p.Fbx)}");
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                inst.name = p.Id + " (Vela, candidate)"; inst.transform.SetParent(holder, false);
                var lg = inst.GetComponent<LODGroup>() ?? throw new InvalidDataException($"077: {p.Id} imported without a LODGroup");
                var lods = lg.GetLODs();
                float size = lg.size;
                float H(float px) => size * (px / p.LeafM) / 1080f; // Unity's relative height at 1080 rows where the leaf spans px pixels
                for (int i = 0; i < lods.Length; i++) lods[i].screenRelativeTransitionHeight = i < p.Px.Length ? H(p.Px[i]) : .01f;
                lg.SetLODs(lods); PrefabUtility.RecordPrefabInstancePropertyModifications(lg);
                pieceRows.Add(new Dictionary<string, object> { ["id"] = p.Id, ["lodGroupSize"] = R(size), ["heights"] = lods.Select(l => (object)R(l.screenRelativeTransitionHeight)).ToList(), ["stoneSlots"] = keepSlots.Select(i => (object)smats[i].name).ToList() });
            }
            log["pieces"] = pieceRows;
            // night lamps: two of our street-lamp kind (light.json night.lamps street) beside the pieces, off by day
            var lamps = new GameObject("night lamps (street kind, review stand-ins)").transform;
            foreach (var p in Pieces)
            {
                var lgo = new GameObject("lamp " + p.Id); lgo.transform.SetParent(lamps, false);
                lgo.transform.SetPositionAndRotation(centre + p.At + new Vector3(1.6f, 4f, 1.2f), Quaternion.Euler(90f, 0f, 0f));
                var l = lgo.AddComponent<Light>(); l.type = LightType.Spot; l.shadows = LightShadows.None; l.enabled = false;
                l.color = night.TryGetValue("lampColour", out var c) ? (Color)FromList(c) : new Color(1f, .8f, .6f);
                l.intensity = night.TryGetValue("lampIntensity", out var li) ? Convert.ToSingle(li) : 19f; l.range = night.TryGetValue("lampRange", out var lr) ? Convert.ToSingle(lr) : 6.5f;
                l.spotAngle = night.TryGetValue("lampSpot", out var ls) ? Convert.ToSingle(ls) : 85f; l.innerSpotAngle = l.spotAngle * .35f;
            }
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException(ScenePath);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Scratch); File.WriteAllText(Path.Combine(Scratch, "build.json"), Json(log));
            Debug.Log($"[077 leaf] review scene -> {ScenePath}: kept {string.Join(", ", keptNames)}");
        });

        static object FromList(object o) { var l = (IList)o; return new Color(Convert.ToSingle(l[0]), Convert.ToSingle(l[1]), Convert.ToSingle(l[2]), l.Count > 3 ? Convert.ToSingle(l[3]) : 1f); }
        static Color Col(Dictionary<string, object> d, string k) => (Color)FromList(d[k]);
        static List<object> L(Color c) => new() { R(c.r), R(c.g), R(c.b), R(c.a) };

        /// <summary>The mask map URP Lit reads with _METALLICSPECGLOSSMAP: R metallic 0, G B 0, A smoothness = 1 - k x ORM.g (8-bit in, 8-bit out,
        /// rounded half away from zero), the source's own size; deterministic. k by the texel's atlas row (v = 1 - (y + .5) / h, as Unity samples).</summary>
        static Dictionary<string, object> Mask(string ormPath, string outAsset, Func<float, float> k, string rule)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!src.LoadImage(File.ReadAllBytes(ormPath))) throw new InvalidDataException(ormPath);
            int w = src.width, h = src.height; var px = src.GetPixels32(); // row 0 = the image's bottom row (Unity's v = 0)
            var o = new Color32[px.Length];
            for (int y = 0; y < h; y++)
            {
                float v = (y + .5f) / h, kk = k(v);
                for (int x = 0; x < w; x++) { int i = y * w + x; o[i] = new Color32(0, 0, 0, (byte)Math.Round(255.0 - kk * px[i].g, MidpointRounding.AwayFromZero)); }
            }
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false, true); dst.SetPixels32(o); dst.Apply();
            var bytes = dst.EncodeToPNG(); File.WriteAllBytes(Path.Combine(Repo, "unity", outAsset), bytes);
            Object.DestroyImmediate(src); Object.DestroyImmediate(dst);
            return new Dictionary<string, object> { ["from"] = Path.GetFileName(ormPath), ["fromSha256"] = Sha(File.ReadAllBytes(ormPath)), ["size"] = $"{w}x{h}", ["rule"] = rule, ["pngSha256"] = Sha(bytes), ["pixelSha256"] = Sha(o.SelectMany(c => new[] { c.r, c.g, c.b, c.a }).ToArray()) };
        }

        static void Tex(string f, bool srgb, bool normal, TextureWrapMode wrap)
        {
            var path = $"{Dir}/Textures/{f}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = srgb; ti.mipmapEnabled = true; ti.npotScale = TextureImporterNPOTScale.ToNearest; ti.wrapMode = wrap; ti.filterMode = FilterMode.Trilinear; ti.anisoLevel = 8;
            ti.alphaSource = normal ? TextureImporterAlphaSource.None : f.Contains("Mask") ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = false; ti.isReadable = false; ti.maxTextureSize = 4096; ti.textureCompression = TextureImporterCompression.CompressedHQ;
            var ps = ti.GetPlatformTextureSettings("Standalone"); ps.overridden = true; ps.maxTextureSize = 4096; ps.format = normal ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7;
            ti.SetPlatformTextureSettings(ps);
            ti.SaveAndReimport();
        }

        static Material Mat(string name, Texture2D albedo, Color colour, Texture2D nrm, float bump, Texture2D mask, bool twoSided)
        {
            var shader = Shader.Find(twoSided ? LeafShaderName : ShaderName) ?? throw new InvalidDataException("077: no shader " + (twoSided ? LeafShaderName : ShaderName));
            var path = $"{Dir}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(shader) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetFloat("_WorkflowMode", 1f); m.SetTexture("_BaseMap", albedo); m.SetColor("_BaseColor", colour);
            m.SetTexture("_BumpMap", nrm); m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", mask); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", 1f); m.SetFloat("_SmoothnessTextureChannel", 0f);
            m.SetFloat("_Cull", twoSided ? 0f : 2f); m.doubleSidedGI = twoSided; m.SetFloat("_ReceiveShadows", 1f);
            foreach (var k in LeafKeywords) if (twoSided) m.EnableKeyword(k); else m.DisableKeyword(k);
            m.SetShaderPassEnabled("ShadowCaster", twoSided); // the soil casts no shadow (troughs/metrics.json materials)
            m.renderQueue = -1;
            EditorUtility.SetDirty(m); AssetDatabase.SaveAssets();
            return m;
        }

        /// <summary>Our night, read only from HarbourDistrict.unity's LightPhase (NightPhase built it from light.json night): the moon, the
        /// trilight ambient, fog, the night sky, the night reflection cube, the night grade, one street lamp's light.</summary>
        static Dictionary<string, object> ReadNight()
        {
            var s = EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var lp = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Suborned.Play.LightPhase>(true)).FirstOrDefault() ?? throw new InvalidDataException("077: HarbourDistrict has no LightPhase");
                var n = lp.night;
                var street = lp.lamps.Where(l => l && l.type == LightType.Spot).OrderBy(l => Mathf.Abs(l.spotAngle - 85f)).ThenBy(l => l.name, StringComparer.Ordinal).FirstOrDefault();
                float lampI = street && lp.lampIntensity.Length == lp.lamps.Length ? lp.lampIntensity[Array.IndexOf(lp.lamps, street)] : 19f;
                return new Dictionary<string, object>
                {
                    ["source"] = HarbourWorldBuilder.ScenePath + " LightPhase.night (read only)",
                    ["sunColour"] = L(n.sunColour), ["sunIntensity"] = R(n.sunIntensity), ["sunRotation"] = new List<object> { R(n.sunRotation.x), R(n.sunRotation.y), R(n.sunRotation.z), R(n.sunRotation.w) },
                    ["ambientSky"] = L(n.ambientSky), ["ambientEquator"] = L(n.ambientEquator), ["ambientGround"] = L(n.ambientGround),
                    ["fogColour"] = L(n.fogColour), ["fogStart"] = R(n.fogStart), ["fogEnd"] = R(n.fogEnd), ["fogDensity"] = R(n.fogDensity),
                    ["skybox"] = n.skybox ? AssetDatabase.GetAssetPath(n.skybox) : "", ["reflection"] = n.reflection ? AssetDatabase.GetAssetPath(n.reflection) : "", ["reflectionIntensity"] = R(n.reflectionIntensity), ["probeIntensity"] = R(n.probeIntensity),
                    ["grade"] = n.grade && n.grade.sharedProfile ? AssetDatabase.GetAssetPath(n.grade.sharedProfile) : "",
                    ["lamp"] = street ? street.name : "-", ["lampColour"] = street ? L(street.color) : L(new Color(1f, .8f, .6f)), ["lampIntensity"] = R(lampI), ["lampRange"] = street ? R(street.range) : 6.5f, ["lampSpot"] = street ? R(street.spotAngle) : 85f,
                };
            }
            finally { EditorSceneManager.CloseScene(s, true); }
        }

        // ================================================================ review: captures and numbers
        sealed class View { public string Id; public float Yaw, Size; public Func<Vector3> Focus; }

        public static void Review() => HarbourWorldBuilder.Batch(() =>
        {
            var t0 = DateTime.UtcNow;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var night = (Dictionary<string, object>)Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(Repo, "unity", Dir, "night.json")));
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            var study = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).First(c => c.name == StudyName);
            study.enabled = true; study.farClipPlane = 2500f; study.nearClipPlane = .05f; study.useOcclusionCulling = false;
            var play = SwapPlayLook.Find(); var look = play.GetComponent<OldTownPlayLook>(); var ts = play.GetComponent<TacticalShadows>();
            var holders = Pieces.ToDictionary(p => p.Id, p => GameObject.Find("leaf077 review/" + p.Id).transform);
            Vector3 Centre(string id) { var b = holders[id].GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; }); return b.center; }
            var all = Pieces.Select(p => holders[p.Id]).SelectMany(h => h.GetComponentsInChildren<Renderer>()).Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
            var views = new List<View>
            {
                new() { Id = "play-y030", Yaw = 30f, Size = 14f, Focus = () => new Vector3(all.center.x, 0f, all.center.z) },
                new() { Id = "play-y210", Yaw = 210f, Size = 14f, Focus = () => new Vector3(all.center.x, 0f, all.center.z) },
                new() { Id = "close-run-y030", Yaw = 30f, Size = 2.6f, Focus = () => Centre("PlanterRun_4_A_TB") },
                new() { Id = "close-run-y210", Yaw = 210f, Size = 2.6f, Focus = () => Centre("PlanterRun_4_A_TB") },
                new() { Id = "close-arc-y030", Yaw = 30f, Size = 4.2f, Focus = () => Centre("PlanterArc_TB") },
            };
            void Pose(Camera c, View v)
            {
                c.orthographic = true; c.orthographicSize = v.Size; c.transform.rotation = Quaternion.Euler(38f, v.Yaw, 0f);
                c.transform.position = v.Focus() - c.transform.forward * 160f; c.nearClipPlane = .3f; c.farClipPlane = 1660f;
                if (c.TryGetComponent<TacticalShadows>(out var t)) t.focusDepth = 160f;
            }
            // the shader off: the same materials without the two leaf keywords (in memory, never saved)
            var onMats = new[] { "DK_Leaf_TB", "DK_Petal_TB" }.Select(n => AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/Materials/{n}.mat")).ToArray();
            var today = Shader.Find(ShaderName);
            var offOf = onMats.ToDictionary(m => m, m => { var o = new Material(m) { name = m.name + " (today's DecoKit/Lit Cutaway)" }; o.shader = today; foreach (var k in m.shaderKeywords) if (!LeafKeywords.Contains(k)) o.EnableKeyword(k); foreach (var k in LeafKeywords) o.DisableKeyword(k); o.renderQueue = m.renderQueue; return o; });
            var renderers = holders.Values.SelectMany(h => h.GetComponentsInChildren<Renderer>(true)).ToArray();
            var asBuilt = renderers.ToDictionary(r => r, r => r.sharedMaterials);
            Material Without(Material m, string k) { var o = new Material(m) { name = m.name + " (no " + k + ")" }; o.DisableKeyword(k); return o; }
            var colourOnly = onMats.ToDictionary(m => m, m => Without(m, "_DK_TWO_SIDED")); var facingOnly = onMats.ToDictionary(m => m, m => Without(m, "_DK_VERTEX_COLOR"));
            void SetState(string st)
            {
                var map = st switch { "off" => offOf, "colour" => colourOnly, "facing" => facingOnly, _ => null };
                foreach (var r in renderers) r.sharedMaterials = asBuilt[r].Select(m => map != null && m && map.TryGetValue(m, out var o) ? o : m).ToArray();
            }
            void SetShaderOn(bool on) => SetState(on ? "on" : "off");
            var lamps = Object.FindObjectsByType<Light>(FindObjectsInactive.Include).Where(l => l.name.StartsWith("lamp ", StringComparison.Ordinal)).ToArray();
            var nightState = NightSwitch(night, look);

            var dir = Path.Combine(Evidence, "captures"); Directory.CreateDirectory(dir);
            var shots = new List<object>();
            var numbers = new Dictionary<string, object> { ["schema"] = "suborned.leaf077.numbers/1", ["brief"] = "077", ["author"] = "Latch", ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"), ["scene"] = ScenePath, ["unity"] = Application.unityVersion };
            using (var session = new RenderSession())
            {
                Object.DestroyImmediate(DecoKitStage.Render(study, 640, 360));
                foreach (var (lookId, cam) in new[] { ("play", play), ("presentation", study) })
                    foreach (var phase in new[] { "day", "night" })
                    {
                        nightState(phase == "night", lookId == "play"); foreach (var l in lamps) l.enabled = phase == "night";
                        foreach (var on in new[] { true, false })
                        {
                            SetShaderOn(on);
                            foreach (var v in views)
                            {
                                Pose(cam, v);
                                string name = $"{lookId}-{phase}-{(on ? "on" : "off")}-{v.Id}";
                                Texture2D px = null; DecoKitStage.WithShadows(200f, () => px = DecoKitStage.Render(cam, 1920, 1080, 2));
                                File.WriteAllBytes(Path.Combine(dir, name + ".png"), px.EncodeToPNG()); Object.DestroyImmediate(px);
                                shots.Add(new Dictionary<string, object> { ["file"] = "captures/" + name + ".png", ["look"] = lookId, ["phase"] = phase, ["shader"] = on ? "on" : "off", ["view"] = v.Id, ["lods"] = LodsNow(cam) });
                            }
                        }
                    }
                nightState(false, true); nightState(false, false); foreach (var l in lamps) l.enabled = false;
                // each fix on its own (play look, day, close): vertex colour only, facing only
                foreach (var st in new[] { "colour", "facing" })
                {
                    SetState(st);
                    foreach (var v in views.Where(x => x.Id.StartsWith("close", StringComparison.Ordinal)))
                    {
                        Pose(play, v); string name = $"play-day-{st}-{v.Id}";
                        Texture2D px = null; DecoKitStage.WithShadows(200f, () => px = DecoKitStage.Render(play, 1920, 1080, 2));
                        File.WriteAllBytes(Path.Combine(dir, name + ".png"), px.EncodeToPNG()); Object.DestroyImmediate(px);
                        shots.Add(new Dictionary<string, object> { ["file"] = "captures/" + name + ".png", ["look"] = "play", ["phase"] = "day", ["shader"] = st, ["view"] = v.Id, ["lods"] = LodsNow(play) });
                    }
                }
                SetShaderOn(true);
                numbers["captures"] = shots;
                // counts: After072's estimator on the play camera, shader on and off (day); the review pieces' own variant table
                var counts = new Dictionary<string, object>();
                foreach (var on in new[] { true, false })
                {
                    SetShaderOn(on);
                    var spec = new After072.AbSpec { cam = play, shadows = ts, fade = null, standOff = 160f };
                    foreach (var v in views.Take(2)) { var vv = v; spec.views.Add(new After072.AbView { id = vv.Id, kind = "play", pose = c => Pose(c, vv), focusPlan = new Vector2(vv.Focus().x, vv.Focus().z), yaw = vv.Yaw }); }
                    counts[on ? "shaderOn" : "shaderOff"] = new Dictionary<string, object> { ["estimator"] = After072.AbCapture(spec), ["pieces"] = Variants(renderers, play, views[0], Pose) };
                }
                SetShaderOn(true);
                numbers["counts"] = counts;
            }
            numbers["shader"] = ShaderFacts(shader);
            numbers["leafShader"] = ShaderFacts(AssetDatabase.LoadAssetAtPath<Shader>(LeafShaderPath), LeafShaderPath);
            numbers["srpBatcherLeafAfterRender"] = SrpBatcher(AssetDatabase.LoadAssetAtPath<Shader>(LeafShaderPath));
            numbers["srpBatcherAfterRender"] = SrpBatcher(shader);
            numbers["materials"] = onMats.Concat(new[] { AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/Materials/DK_Soil_TB.mat") }).Select(m => (object)new Dictionary<string, object>
            {
                ["name"] = m.name, ["shader"] = m.shader.name, ["keywords"] = string.Join(" ", m.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal)), ["cull"] = R(m.GetFloat("_Cull")),
                ["baseColour"] = "#" + ColorUtility.ToHtmlStringRGB(m.GetColor("_BaseColor")), ["bumpScale"] = R(m.GetFloat("_BumpScale")), ["smoothnessScale"] = R(m.GetFloat("_Smoothness")),
                ["shadowCaster"] = m.GetShaderPassEnabled("ShadowCaster"), ["doubleSidedGI"] = m.doubleSidedGI,
                ["textures"] = new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" }.ToDictionary(t => t, t => (object)TexFacts(m.GetTexture(t))),
            }).ToList();
            numbers["meshes"] = Pieces.Select(p => (object)MeshFacts(p)).ToList();
            numbers["lodGroups"] = Pieces.Select(p => { var lg = holders[p.Id].GetComponentInChildren<LODGroup>(); return (object)new Dictionary<string, object> { ["id"] = p.Id, ["size"] = R(lg.size), ["heights"] = lg.GetLODs().Select(l => (object)R(l.screenRelativeTransitionHeight)).ToList(), ["renderersPerLod"] = lg.GetLODs().Select(l => (object)l.renderers.Length).ToList(), ["playZoomRelativeHeight"] = R(lg.size / 28f * QualitySettings.lodBias), ["lodBias"] = R(QualitySettings.lodBias) }; }).ToList();
            numbers["build"] = File.Exists(Path.Combine(Scratch, "build.json")) ? Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(Scratch, "build.json"))) : null;
            numbers["night"] = night;
            numbers["seconds"] = Math.Round((DateTime.UtcNow - t0).TotalSeconds, 1);
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Path.Combine(Evidence, "numbers.json"), Json(numbers));
            Debug.Log($"[077 leaf] review: {shots.Count} captures in {numbers["seconds"]} s -> {Evidence}");
        });

        /// <summary>Returns a switch: (night?, playLook?) sets our night (or puts the day back) on the scene light and the play look's own light.</summary>
        static Action<bool, bool> NightSwitch(Dictionary<string, object> n, OldTownPlayLook look)
        {
            var sun = RenderSettings.sun ? RenderSettings.sun : Object.FindObjectsByType<Light>(FindObjectsInactive.Include).First(l => l.type == LightType.Directional);
            var probes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include);
            var day = (rot: sun.transform.rotation, col: sun.color, i: sun.intensity, amb: RenderSettings.ambientMode, sky: RenderSettings.ambientSkyColor, eq: RenderSettings.ambientEquatorColor, gr: RenderSettings.ambientGroundColor,
                       probe: RenderSettings.ambientProbe, fog: RenderSettings.fogColor, fs: RenderSettings.fogStartDistance, fe: RenderSettings.fogEndDistance, fd: RenderSettings.fogDensity,
                       box: RenderSettings.skybox, refl: RenderSettings.defaultReflectionMode, cube: RenderSettings.customReflectionTexture, ri: RenderSettings.reflectionIntensity, probesI: probes.Select(p => p.intensity).ToArray());
            var lookDay = (rot: look.sunRotation, col: look.sunColour, i: look.sunIntensity, amb: look.ambientMode, sky: look.ambientSky, eq: look.ambientEquator, gr: look.ambientGround, ai: look.ambientIntensity, probe: look.ambientProbe, grade: look.playVolume ? look.playVolume.sharedProfile : null);
            var rotL = (IList)n["sunRotation"]; var moon = new Quaternion(Convert.ToSingle(rotL[0]), Convert.ToSingle(rotL[1]), Convert.ToSingle(rotL[2]), Convert.ToSingle(rotL[3]));
            var nightSky = AssetDatabase.LoadAssetAtPath<Material>((string)n["skybox"]); var nightCube = AssetDatabase.LoadAssetAtPath<Cubemap>((string)n["reflection"]);
            var nightGrade = AssetDatabase.LoadAssetAtPath<VolumeProfile>((string)n["grade"]);
            float ri = Convert.ToSingle(n["reflectionIntensity"]), pi = Convert.ToSingle(n["probeIntensity"]);
            return (toNight, playLook) =>
            {
                if (toNight)
                {
                    sun.transform.rotation = moon; sun.color = Col(n, "sunColour"); sun.intensity = Convert.ToSingle(n["sunIntensity"]);
                    RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = Col(n, "ambientSky"); RenderSettings.ambientEquatorColor = Col(n, "ambientEquator"); RenderSettings.ambientGroundColor = Col(n, "ambientGround");
                    RenderSettings.fogColor = Col(n, "fogColour"); RenderSettings.fogStartDistance = Convert.ToSingle(n["fogStart"]); RenderSettings.fogEndDistance = Convert.ToSingle(n["fogEnd"]); RenderSettings.fogDensity = Convert.ToSingle(n["fogDensity"]);
                    if (nightSky) RenderSettings.skybox = nightSky;
                    if (nightCube) { RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom; RenderSettings.customReflectionTexture = nightCube; }
                    RenderSettings.reflectionIntensity = ri; foreach (var p in probes) p.intensity = pi >= 0f ? pi : p.intensity;
                    DynamicGI.UpdateEnvironment();
                    // the play look puts its own light on while it renders: give it the same night
                    look.sunRotation = moon; look.sunColour = sun.color; look.sunIntensity = sun.intensity; look.ambientMode = AmbientMode.Trilight;
                    look.ambientSky = RenderSettings.ambientSkyColor; look.ambientEquator = RenderSettings.ambientEquatorColor; look.ambientGround = RenderSettings.ambientGroundColor; look.ambientIntensity = 1f;
                    look.ambientProbe = OldTownPlayLook.Flatten(RenderSettings.ambientProbe);
                    if (look.playVolume && nightGrade) look.playVolume.sharedProfile = nightGrade;
                }
                else
                {
                    sun.transform.rotation = day.rot; sun.color = day.col; sun.intensity = day.i;
                    RenderSettings.ambientMode = day.amb; RenderSettings.ambientSkyColor = day.sky; RenderSettings.ambientEquatorColor = day.eq; RenderSettings.ambientGroundColor = day.gr;
                    RenderSettings.fogColor = day.fog; RenderSettings.fogStartDistance = day.fs; RenderSettings.fogEndDistance = day.fe; RenderSettings.fogDensity = day.fd;
                    RenderSettings.skybox = day.box; RenderSettings.defaultReflectionMode = day.refl; RenderSettings.customReflectionTexture = day.cube; RenderSettings.reflectionIntensity = day.ri;
                    for (int i = 0; i < probes.Length; i++) probes[i].intensity = day.probesI[i];
                    RenderSettings.ambientProbe = day.probe; DynamicGI.UpdateEnvironment(); RenderSettings.ambientProbe = day.probe;
                    look.sunRotation = lookDay.rot; look.sunColour = lookDay.col; look.sunIntensity = lookDay.i; look.ambientMode = lookDay.amb; look.ambientSky = lookDay.sky; look.ambientEquator = lookDay.eq;
                    look.ambientGround = lookDay.gr; look.ambientIntensity = lookDay.ai; look.ambientProbe = lookDay.probe; if (look.playVolume) look.playVolume.sharedProfile = lookDay.grade;
                }
            };
        }

        /// <summary>Which LOD each review LODGroup draws for this camera now (Unity's rule: size x lossy scale / (2 x ortho size) x lodBias against the heights).</summary>
        static Dictionary<string, object> LodsNow(Camera cam)
        {
            var o = new Dictionary<string, object>();
            foreach (var lg in Object.FindObjectsByType<LODGroup>().OrderBy(l => l.name, StringComparer.Ordinal))
            {
                float rel = lg.size * Mathf.Max(lg.transform.lossyScale.x, Mathf.Max(lg.transform.lossyScale.y, lg.transform.lossyScale.z)) / (2f * cam.orthographicSize) * QualitySettings.lodBias;
                var lods = lg.GetLODs(); int pick = -1; for (int i = 0; i < lods.Length; i++) if (rel >= lods[i].screenRelativeTransitionHeight) { pick = i; break; }
                o[lg.name] = pick < 0 ? "culled" : $"LOD{pick} (relative height {rel:0.###})";
            }
            return o;
        }

        /// <summary>The review pieces' drawn submeshes at a pose: forward and shadow programs (shader variant + cull, what the SRP Batcher batches
        /// by), draws and triangles, for the LOD the camera picks.</summary>
        static Dictionary<string, object> Variants(Renderer[] rs, Camera cam, View v, Action<Camera, View> pose)
        {
            pose(cam, v);
            var fwd = new SortedDictionary<string, int>(StringComparer.Ordinal); var shadow = new SortedDictionary<string, int>(StringComparer.Ordinal);
            long tris = 0; int draws = 0, shadowDraws = 0;
            var drawn = new HashSet<Renderer>();
            foreach (var lg in rs.Select(r => r.GetComponentInParent<LODGroup>()).Where(l => l).Distinct())
            {
                float rel = lg.size / (2f * cam.orthographicSize) * QualitySettings.lodBias; var lods = lg.GetLODs();
                for (int i = 0; i < lods.Length; i++) if (rel >= lods[i].screenRelativeTransitionHeight) { foreach (var r in lods[i].renderers) drawn.Add(r); break; }
            }
            foreach (var r in rs) if (!r.GetComponentInParent<LODGroup>()) drawn.Add(r);
            foreach (var r in drawn)
            {
                var mesh = r.GetComponent<MeshFilter>().sharedMesh; var ms = r.sharedMaterials;
                for (int i = 0; i < mesh.subMeshCount && i < ms.Length; i++)
                {
                    var m = ms[i]; string kw = string.Join(" ", m.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal)), cull = $"cull {m.GetFloat("_Cull"):0}";
                    string f = $"{m.shader.name} | {kw} | {cull}"; fwd[f] = fwd.TryGetValue(f, out var a) ? a + 1 : 1; draws++; tris += mesh.GetIndexCount(i) / 3;
                    if (r.shadowCastingMode != ShadowCastingMode.Off && m.GetShaderPassEnabled("ShadowCaster"))
                    {   // the shadow pass declares only _ALPHATEST_ON and _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A of these: the leaf keywords do not make a shadow variant
                        var skw = string.Join(" ", m.shaderKeywords.Where(k => k is "_ALPHATEST_ON" or "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A").OrderBy(k => k, StringComparer.Ordinal));
                        string s = $"{m.shader.name} | {skw} | {cull}"; shadow[s] = shadow.TryGetValue(s, out var b) ? b + 1 : 1; shadowDraws++;
                    }
                }
            }
            return new Dictionary<string, object>
            {
                ["view"] = v.Id, ["renderers"] = drawn.Count, ["draws"] = draws, ["shadowDraws"] = shadowDraws, ["triangles"] = tris,
                ["forwardPrograms"] = fwd.ToDictionary(k => k.Key, k => (object)k.Value), ["shadowPrograms"] = shadow.ToDictionary(k => k.Key, k => (object)k.Value),
            };
        }

        static Dictionary<string, object> TexFacts(Texture t)
        {
            if (!t) return new Dictionary<string, object> { ["none"] = true };
            var p = AssetDatabase.GetAssetPath(t);
            return new Dictionary<string, object> { ["path"] = p, ["size"] = $"{t.width}x{t.height}", ["format"] = t is Texture2D t2 ? t2.format.ToString() : "-", ["mips"] = t.mipmapCount, ["MiB"] = R(After072.AbGpuBytes(t) / 1048576f), ["wrap"] = t.wrapMode.ToString(), ["aniso"] = t.anisoLevel, ["sourceSha256"] = FileSha(p) };
        }

        /// <summary>Per LOD and submesh of a piece as Unity imported it: material, triangles, vertices, the vertex colour's format and range, the uv v range.</summary>
        static Dictionary<string, object> MeshFacts(Piece p)
        {
            var path = $"{Dir}/Models/{Path.GetFileName(p.Fbx)}";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var rows = new List<object>(); bool split = true;
            foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true).OrderBy(r => r.name, StringComparer.Ordinal))
            {
                var mesh = r.GetComponent<MeshFilter>().sharedMesh; var ms = r.sharedMaterials; var uv = mesh.uv; var col = mesh.colors;
                var fmt = mesh.HasVertexAttribute(VertexAttribute.Color) ? $"{mesh.GetVertexAttributeFormat(VertexAttribute.Color)} x{mesh.GetVertexAttributeDimension(VertexAttribute.Color)}" : "none";
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var idx = mesh.GetIndices(s).Distinct().ToArray();
                    float vmin = idx.Min(i => uv[i].y), vmax = idx.Max(i => uv[i].y);
                    string mn = s < ms.Length && ms[s] ? ms[s].name : "-";
                    if (mn == "DK_Leaf_TB" && vmin < .5f) split = false; if (mn == "DK_Petal_TB" && vmax > .5f) split = false;
                    var row = new Dictionary<string, object> { ["renderer"] = r.name, ["submesh"] = s, ["material"] = mn, ["triangles"] = mesh.GetIndexCount(s) / 3, ["vertices"] = idx.Length, ["uvV"] = new List<object> { R(vmin), R(vmax) }, ["colourFormat"] = fmt };
                    if (col.Length > 0)
                    {
                        var c = idx.Select(i => col[i]).ToArray();
                        row["colourMin"] = L(new Color(c.Min(x => x.r), c.Min(x => x.g), c.Min(x => x.b), c.Min(x => x.a)));
                        row["colourMax"] = L(new Color(c.Max(x => x.r), c.Max(x => x.g), c.Max(x => x.b), c.Max(x => x.a)));
                        row["colourMean"] = L(new Color(c.Average(x => x.r), c.Average(x => x.g), c.Average(x => x.b), c.Average(x => x.a)));
                        row["distinctColours"] = c.Select(x => ((int)Math.Round(x.r * 255f) << 16) | ((int)Math.Round(x.g * 255f) << 8) | (int)Math.Round(x.b * 255f)).Distinct().Count();
                    }
                    rows.Add(row);
                }
            }
            return new Dictionary<string, object> { ["id"] = p.Id, ["fbx"] = path, ["sha256"] = FileSha(path), ["submeshes"] = rows, ["leafUvAboveHalfPetalBelow"] = split };
        }

        // ================================================================ helpers
        static object R(float f) => Math.Round(f, 5);
        static string Sha(byte[] b) { using var s = SHA256.Create(); return string.Concat(s.ComputeHash(b).Select(x => x.ToString("x2"))); }
        static string FileSha(string assetPath) => Sha(File.ReadAllBytes(Path.Combine(Repo, "unity", assetPath)));

        static string Json(object o)
        {
            var sb = new StringBuilder(); W(sb, o, 0); return sb.ToString() + "\n";
        }
        static void W(StringBuilder sb, object o, int d)
        {
            string pad = new(' ', d + 1), end = new(' ', d);
            switch (o)
            {
                case null: sb.Append("null"); break;
                case string s: sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")).Append('"'); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case float f: sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); break;
                case double x: sb.Append(x.ToString("R", CultureInfo.InvariantCulture)); break;
                case IDictionary dict:
                    sb.Append("{\n"); int i = 0;
                    foreach (DictionaryEntry kv in dict) { if (i++ > 0) sb.Append(",\n"); sb.Append(pad); W(sb, kv.Key.ToString(), d + 1); sb.Append(": "); W(sb, kv.Value, d + 1); }
                    sb.Append('\n').Append(end).Append('}'); break;
                case IEnumerable list:
                    var items = list.Cast<object>().ToList();
                    if (items.All(x => x is not IDictionary && (x is string || x is not IEnumerable))) { sb.Append('['); for (int k = 0; k < items.Count; k++) { if (k > 0) sb.Append(", "); W(sb, items[k], d + 1); } sb.Append(']'); }
                    else { sb.Append("[\n"); for (int k = 0; k < items.Count; k++) { if (k > 0) sb.Append(",\n"); sb.Append(pad); W(sb, items[k], d + 1); } sb.Append('\n').Append(end).Append(']'); }
                    break;
                default: sb.Append(Convert.ToString(o, CultureInfo.InvariantCulture)); break;
            }
        }
    }
}
#endif
