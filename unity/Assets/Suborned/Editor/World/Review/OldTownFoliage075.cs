using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Suborned.World.Review
{
    /// <summary>Brief 075 (Latch, Opus): one scoped review trial of the ORIGINAL OldTown FlowerBed and ShrubMound, with their original OldTown
    /// materials bound by GUID, in the two café troughs (Vela's briefs/075/recovery/opus-oldtown-foliage/placements.json). Default-off: Apply
    /// refuses without -oldtownFoliage trial, and no normal builder calls it. The baseline is the pinned civic-paving scene; it is opened and
    /// saved under a new name, never written. Only the two troughs' baked leaf/bloom triangles leave the civic batches (exact world-triangle
    /// match, full count required); the trough's slot 0 stone stays verbatim in its batch. Each plant is an instance of the source FBX model
    /// (its shared, immutable mesh: no copy, no clamp, no decimation), uniform scale, quarter-turn yaw, no collider, no LODGroup.</summary>
    public static class OldTownFoliage075
    {
        const string Dir = "Assets/Suborned/Generated/OldTownFoliage075";
        const string Mats = "Assets/Suborned/OldTownArt/DecoKit/Materials/";
        const string Models = "Assets/Suborned/OldTownArt/DecoKit/Models/Plants/";
        const string Group = "oldtown-foliage075";
        static readonly Dictionary<string, string> MatGuid = new()
        {
            ["DK_Leaf_CoreShrub"] = "de6d3a3621ee263b731210f987c73135",
            ["DK_Leaf_Shrub"] = "00b375d66a47c774cf48bcca018dc153",
            ["DK_Flower_Coral"] = "65f10738bc12a73e17d454785d89c36c",
        };

        sealed class Inst { public string Name; public float X, Z, Yaw, Scale; }
        sealed class Trough
        {
            public string Id, Fbx, FbxGuid, FbxSha; public float Top; public string[] Slots; public Vector3 NativeMin, NativeMax; public Inst[] Plants;
        }

        // placements.json, Vela 2026-10-29: local Blender frame of the trough (+x along the run, +z up, origin = trough pivot, bottom centre).
        static Trough[] Troughs(float flowerZ) => new[]
        {
            new Trough { Id = "civic/run-west-cafe/1", Fbx = Models + "DK_FlowerBed.fbx", FbxGuid = "90a0ac43ff402f62a9c88c504f1fffb1",
                FbxSha = "77dfd48e9ef1a857ffb008e509a7235e8d7ba2949b661f655f05968e059617c4", Top = 1.0616f,
                Slots = new[] { "DK_Leaf_CoreShrub", "DK_Leaf_Shrub", "DK_Flower_Coral" },
                NativeMin = new(-0.9933f, -0.3076f, -0.0870f), NativeMax = new(0.9873f, 0.3098f, 0.3743f),
                Plants = new[] { new Inst { Name = "FlowerBed_0", X = -1f, Z = flowerZ, Yaw = 0, Scale = 1f }, new Inst { Name = "FlowerBed_1", X = 1f, Z = flowerZ, Yaw = 0, Scale = 1f } } },
            new Trough { Id = "civic/run-west-cafe/0", Fbx = Models + "DK_ShrubMound.fbx", FbxGuid = "297bda42c1b23e124c6ea0a9e870d50d",
                FbxSha = "857103fb43ccaf9710bf590f5bde28187e2b0239ef3f8bfb0fd887051b32aec1", Top = 1.1201f,
                Slots = new[] { "DK_Leaf_CoreShrub", "DK_Leaf_Shrub" },
                NativeMin = new(-0.4586f, -0.4908f, -0.0609f), NativeMax = new(0.4938f, 0.4965f, 0.7281f),
                Plants = new[] { -1.55f, -.93f, -.31f, .31f, .93f, 1.55f }.Select((x, i) => new Inst { Name = "ShrubMound_" + i, X = x, Z = .645f, Yaw = new[] { 0f, 180f, 90f, 270f, 180f, 0f }[i], Scale = .64f }).ToArray() },
        };

        static string Arg(string key, string d) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-" + key); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }
        static string Repo => HarbourWorldBuilder.RepoRoot;
        static bool Plant(string n) => n.StartsWith("DK_Leaf_", StringComparison.Ordinal) || n.StartsWith("DK_Bloom_", StringComparison.Ordinal);
        static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
        static string FileSha(string assetPath) { using var s = SHA256.Create(); return string.Concat(s.ComputeHash(File.ReadAllBytes(Path.Combine(Repo, "unity", assetPath))).Select(b => b.ToString("x2"))); }
        static string Hash(string s) { using var sha = SHA256.Create(); return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(s)).Select(b => b.ToString("x2"))); }
        static string Q(Vector3 p) => $"{Mathf.RoundToInt(p.x * 1000f)},{Mathf.RoundToInt(p.y * 1000f)},{Mathf.RoundToInt(p.z * 1000f)}";
        static string Tri(Vector3 a, Vector3 b, Vector3 c) => string.Join(";", new[] { Q(a), Q(b), Q(c) }.OrderBy(x => x, StringComparer.Ordinal));
        static List<object> L(Vector3 v) => new() { R(v.x), R(v.y), R(v.z) };
        static object R(float f) => Math.Round(f, 5);
        static Dictionary<string, object> B(Bounds b) => new() { ["min"] = L(b.min), ["max"] = L(b.max) };
        static void Enc(ref Bounds? b, Vector3 p) { if (b is { } h) { h.Encapsulate(p); b = h; } else b = new Bounds(p, Vector3.zero); }

        /// <summary>The trough's frame: world point of a local Blender point (x along the run, y across, z up), from the kit placement.</summary>
        sealed class Frame
        {
            public Matrix4x4 BlenderToWorld, Trs; public float PivotY;
            public Vector3 World(Vector3 b) => BlenderToWorld.MultiplyPoint3x4(b);
            public Vector3 Local(Vector3 w) => BlenderToWorld.inverse.MultiplyPoint3x4(w);
        }
        // The DecoKit FBX convention, measured on both plant models (ImportAxes) and stated for the kit in tools/decokit/sides.py ("the kit's Unity
        // front +Z is Blender -Y"): Blender (x, y, z) -> Unity (-x, z, -y), a handedness flip. placements.json's "(-x,z,y)" drops the y sign; every
        // trial instance sits at local y 0 and the coping is symmetric across, so only this frame's reading of y changes, never a pose.
        static readonly Matrix4x4 BlenderToUnity = new(new Vector4(-1, 0, 0, 0), new Vector4(0, 0, -1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1)); // columns: x→-x, y→-z, z→y
        static Frame FrameOf(KitPlacer kit, MassDto mass)
        {
            float py = kit.PivotForLowest(mass.Kit, mass.Kit.Base);
            var rot = Quaternion.Euler(0f, -mass.Kit.Yaw * Mathf.Rad2Deg, 0f);
            var trs = Matrix4x4.TRS(new Vector3(mass.Kit.At.x, py, mass.Kit.At.y), rot, mass.Kit.Scale);
            return new Frame { Trs = trs, PivotY = py, BlenderToWorld = Matrix4x4.TRS(new Vector3(mass.Kit.At.x, py, mass.Kit.At.y), rot, Vector3.one) * BlenderToUnity };
        }

        /// <summary>The imported model's axes: the signed permutation A (model root space = A · Blender) whose image of the native Blender bounds
        /// matches the imported vertices' bounds. Unique to 1 mm or reject.</summary>
        static (Matrix4x4 a, float err, Bounds model) ImportAxes(GameObject model, Trough t)
        {
            Bounds? mb = null;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = model.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                using var data = MeshUtility.AcquireReadOnlyMeshData(mf.sharedMesh);
                using var vs = new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount, Unity.Collections.Allocator.Temp); data[0].GetVertices(vs);
                foreach (var v in vs) Enc(ref mb, m.MultiplyPoint3x4(v));
            }
            var best = new List<(Matrix4x4, float)>();
            foreach (var perm in new[] { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } })
                for (int s = 0; s < 8; s++)
                {
                    var a = Matrix4x4.zero; a[3, 3] = 1;
                    for (int c = 0; c < 3; c++) a[perm[c], c] = ((s >> c) & 1) == 0 ? 1 : -1;
                    Bounds? nb = null;
                    for (int i = 0; i < 8; i++) Enc(ref nb, a.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? t.NativeMin.x : t.NativeMax.x, (i & 2) == 0 ? t.NativeMin.y : t.NativeMax.y, (i & 4) == 0 ? t.NativeMin.z : t.NativeMax.z)));
                    float err = Mathf.Max((nb.Value.min - mb.Value.min).magnitude, (nb.Value.max - mb.Value.max).magnitude);
                    best.Add((a, err));
                }
            best.Sort((x, y) => x.Item2.CompareTo(y.Item2));
            Debug.Log($"OldTownFoliage075 axes {t.Fbx}: model min {mb.Value.min.ToString("F4")} max {mb.Value.max.ToString("F4")}; " + string.Join(" || ", best.Take(4).Select(b => $"{b.Item2:F5}: {b.Item1.ToString("F0").Replace("\n", " ")}")));
            if (best[0].Item2 > .001f || best[1].Item2 <= .001f) throw new InvalidDataException($"OldTownFoliage075: {t.Fbx} import axes not uniquely identified (best {best[0].Item2:F5}, next {best[1].Item2:F5})");
            return (best[0].Item1, best[0].Item2, mb.Value);
        }

        static Material Bound(string name)
        {
            var path = AssetDatabase.GUIDToAssetPath(MatGuid[name]);
            if (path != Mats + name + ".mat") throw new InvalidDataException($"OldTownFoliage075: GUID {MatGuid[name]} is {path}, not the OldTown {name}");
            return AssetDatabase.LoadAssetAtPath<Material>(path) ?? throw new FileNotFoundException(path);
        }

        /// <summary>Opt-in, read-only shader/material/texture audit of the three OldTown materials and their current Kit twins.</summary>
        static Dictionary<string, object> MaterialAudit()
        {
            var rows = new List<object>();
            var srp = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var srpWhy = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var name in MatGuid.Keys)
                foreach (var (kind, path) in new[] { ("oldtown", Mats + name + ".mat"), ("currentTwin", "Assets/Suborned/Kit/Materials/" + name + ".mat") })
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path); if (!mat) { rows.Add(new Dictionary<string, object> { ["material"] = path, ["missing"] = true }); continue; }
                    var sh = mat.shader; var shPath = AssetDatabase.GetAssetPath(sh);
                    object srpCode = "unavailable", srpReason = "unavailable";
                    try { if (srp != null) { srpCode = Convert.ToInt32(srp.Invoke(null, new object[] { sh, 0 })); if (srpWhy != null) srpReason = srpWhy.Invoke(null, new object[] { sh, 0, srpCode }); } } catch (Exception e) { srpCode = "error: " + e.GetBaseException().Message; }
                    var msgs = ShaderUtil.GetShaderMessages(sh).Select(m => (object)$"{m.severity}: {m.message} ({m.file}:{m.line})").ToList();
                    var tex = new List<object>();
                    foreach (var prop in mat.GetTexturePropertyNames())
                    {
                        var tx = mat.GetTexture(prop); if (!tx) continue; var tp = AssetDatabase.GetAssetPath(tx);
                        var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
                        tex.Add(new Dictionary<string, object> { ["prop"] = prop, ["path"] = tp, ["guid"] = AssetDatabase.AssetPathToGUID(tp), ["sha256"] = FileSha(tp), ["size"] = $"{tx.width}x{tx.height}", ["filter"] = tx.filterMode.ToString(), ["aniso"] = tx.anisoLevel, ["wrap"] = tx.wrapMode.ToString(), ["mipmaps"] = ti ? ti.mipmapEnabled : (object)"-", ["sRGB"] = ti ? ti.sRGBTexture : (object)"-", ["alphaIsTransparency"] = ti ? ti.alphaIsTransparency : (object)"-", ["alphaSource"] = ti ? ti.alphaSource.ToString() : "-", ["mipMapsPreserveCoverage"] = ti ? ti.mipMapsPreserveCoverage : (object)"-", ["maxSize"] = ti ? ti.maxTextureSize : (object)"-", ["compression"] = ti ? ti.textureCompression.ToString() : "-" });
                    }
                    rows.Add(new Dictionary<string, object>
                    {
                        ["kind"] = kind, ["material"] = path, ["guid"] = AssetDatabase.AssetPathToGUID(path), ["sha256"] = FileSha(path), ["shader"] = sh.name, ["shaderPath"] = shPath,
                        ["shaderGuid"] = AssetDatabase.AssetPathToGUID(shPath), ["shaderSha256"] = shPath.StartsWith("Assets/") ? FileSha(shPath) : "-", ["shaderSupported"] = sh.isSupported, ["shaderHasError"] = ShaderUtil.ShaderHasError(sh),
                        ["shaderMessages"] = msgs, ["srpBatcherCode(0=compatible)"] = srpCode, ["srpBatcherReason"] = Convert.ToString(srpReason), ["passes"] = sh.passCount,
                        ["keywords"] = mat.shaderKeywords.Cast<object>().ToList(), ["renderQueue"] = mat.renderQueue, ["alphaClip"] = mat.HasProperty("_AlphaClip") ? R(mat.GetFloat("_AlphaClip")) : "-",
                        ["cutoff"] = mat.HasProperty("_Cutoff") ? R(mat.GetFloat("_Cutoff")) : "-", ["cull"] = mat.HasProperty("_Cull") ? R(mat.GetFloat("_Cull")) : "-", ["renderType"] = mat.GetTag("RenderType", false, "-"),
                        ["shadowCasterPass"] = mat.GetShaderPassEnabled("ShadowCaster"), ["textures"] = tex,
                    });
                }
            return new Dictionary<string, object> { ["materials"] = rows };
        }

        public static void Audit() => HarbourWorldBuilder.Batch(() => { Write("material-audit.json", MaterialAudit()); Debug.Log("OldTownFoliage075 material audit written"); });

        public static void Apply() => HarbourWorldBuilder.Batch(() =>
        {
            if (Arg("oldtownFoliage", "off") != "trial") throw new InvalidOperationException("OldTownFoliage075 is default-off; pass -oldtownFoliage trial for the review trial");
            string baseline = Arg("oldtownScene", "Assets/Scenes/probe-oldtown-ours.unity"), outScene = Arg("oldtownSave", "Assets/Scenes/probe-oldtown-foliage075.unity");
            if (outScene == baseline || !outScene.StartsWith("Assets/Scenes/probe-", StringComparison.Ordinal)) throw new IOException("OldTownFoliage075: refusing to save over " + outScene);
            var scene = EditorSceneManager.OpenScene(baseline, OpenSceneMode.Single);
            var world = WorldReader.Load(Path.Combine(Repo, Arg("world", "out/world/harbour-district.world.json")));
            var kit = new KitPlacer(HarbourWorldBuilder.KitCataloguePath);
            var roots = scene.GetRootGameObjects();
            var report = new Dictionary<string, object> { ["status"] = "review trial only; default-off; not production", ["baselineScene"] = baseline, ["savedScene"] = outScene, ["world"] = Arg("world", "out/world/harbour-district.world.json") };

            // 1. frames, envelopes and the exact baked plant triangles of both troughs (one sweep: they share the civic batches)
            var expected = new Dictionary<string, int>(); var ownerOf = new Dictionary<string, string>(); var perTrough = new Dictionary<string, int>(); var removed = new Dictionary<string, int>();
            var frames = new Dictionary<string, Frame>(); var envelopes = new Dictionary<string, Bounds>(); var containers = new Dictionary<string, Bounds>(); var profiles = new Dictionary<string, List<(float z, float x, float y)>>();
            var troughRows = new List<object>();
            float flowerZ = float.Parse(Arg("oldtownFlowerZ", "0.655"), System.Globalization.CultureInfo.InvariantCulture);
            var troughs = Troughs(flowerZ);
            foreach (var t in troughs)
            {
                var mass = world.Civic.Single(m => m.Id == t.Id); var f = FrameOf(kit, mass); frames[t.Id] = f;
                var prefab = kit.Prefab(mass.Kit.Id); var src = KitLods.Lod0(prefab).Single(); var sm = src.GetComponent<MeshFilter>().sharedMesh; var mats = src.sharedMaterials;
                if (mats.Length != sm.subMeshCount || Plant(mats[0].name) || mats.Skip(1).Any(m => !Plant(m.name))) throw new InvalidDataException($"OldTownFoliage075: {t.Id} slots are not [stone, plant...]: {string.Join(",", mats.Select(m => m.name))}");
                var matrix = f.Trs * prefab.transform.worldToLocalMatrix * src.transform.localToWorldMatrix;
                Bounds? env = null, cont = null; int plantTris = 0; var prof = new List<(float, float, float)>();
                using (var data = MeshUtility.AcquireReadOnlyMeshData(sm))
                {
                    using var vs = new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount, Unity.Collections.Allocator.Temp); data[0].GetVertices(vs);
                    for (int si = 0; si < sm.subMeshCount; si++)
                    {
                        using var ix = new Unity.Collections.NativeArray<int>(data[0].GetSubMesh(si).indexCount, Unity.Collections.Allocator.Temp); data[0].GetIndices(ix, si);
                        for (int k = 0; k < ix.Length; k += 3)
                        {
                            var a = matrix.MultiplyPoint3x4(vs[ix[k]]); var b = matrix.MultiplyPoint3x4(vs[ix[k + 1]]); var c = matrix.MultiplyPoint3x4(vs[ix[k + 2]]);
                            foreach (var p in new[] { a, b, c }) { Enc(ref env, p); if (si == 0) { Enc(ref cont, p); var lp = f.Local(p); prof.Add((lp.z, Mathf.Abs(lp.x), Mathf.Abs(lp.y))); } }
                            if (si == 0) continue;
                            string key = mats[si].name + "|" + Tri(a, b, c); expected[key] = expected.TryGetValue(key, out var n) ? n + 1 : 1; plantTris++;
                            if (!ownerOf.TryGetValue(key, out var ow)) ownerOf[key] = t.Id; else if (ow != t.Id) throw new InvalidDataException("OldTownFoliage075: a plant triangle is shared by both troughs");
                        }
                    }
                }
                perTrough[t.Id] = plantTris; removed[t.Id] = 0; envelopes[t.Id] = env.Value; containers[t.Id] = cont.Value; profiles[t.Id] = prof;
                Bounds? envLocal = null; foreach (var p in Corners(env.Value)) Enc(ref envLocal, f.Local(p));
                troughRows.Add(new Dictionary<string, object>
                {
                    ["id"] = t.Id, ["piece"] = mass.Kit.Id, ["at"] = new List<object> { R(mass.Kit.At.x), R(mass.Kit.At.y) }, ["base"] = R(mass.Kit.Base), ["kitYawRad"] = R(mass.Kit.Yaw), ["kitScale"] = L(mass.Kit.Scale), ["pivotY"] = R(f.PivotY),
                    ["slots"] = mats.Select(m => (object)m.name).ToList(), ["slot0Triangles"] = (long)sm.GetIndexCount(0) / 3, ["plantTrianglesRemoved"] = plantTris,
                    ["currentRendererEnvelopeWorld"] = B(env.Value), ["currentRendererEnvelopeLocal"] = B(envLocal.Value), ["containerWorld"] = B(cont.Value),
                    ["blenderXToWorld"] = L(f.BlenderToWorld.MultiplyVector(Vector3.right)), ["blenderYToWorld"] = L(f.BlenderToWorld.MultiplyVector(Vector3.up)), ["blenderZToWorld"] = L(f.BlenderToWorld.MultiplyVector(Vector3.forward)), ["containerLocalBounds"] = B(LocalBounds(f, containers[t.Id])),
                    ["containerProfileLocal"] = prof.GroupBy(q => Mathf.Round(q.Item1 * 1000f) / 1000f).OrderBy(g => g.Key).Select(g => (object)new List<object> { R(g.Key), R(g.Max(q => q.Item2)), R(g.Max(q => q.Item3)) }).ToList(),
                });
            }
            report["troughs"] = troughRows;

            var touched = new List<object>(); Material[] plantBatchSample = null; MeshRenderer sampleRenderer = null;
            foreach (var r in roots.SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).Where(r => PathOf(r.transform).StartsWith("batches/civic/", StringComparison.Ordinal)))
            {
                if (!r.sharedMaterials.Any(m => m && Plant(m.name))) continue;
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                var mesh = mf.sharedMesh; var vs = mesh.vertices; Mesh copy = null; int n = 0; var byTrough = new Dictionary<string, int>();
                for (int si = 0; si < mesh.subMeshCount; si++)
                {
                    string name = r.sharedMaterials[si].name; if (!Plant(name)) continue;
                    var tri = mesh.GetTriangles(si); var kept = new List<int>();
                    for (int k = 0; k < tri.Length; k += 3)
                    {
                        var a = mf.transform.TransformPoint(vs[tri[k]]); var b = mf.transform.TransformPoint(vs[tri[k + 1]]); var c = mf.transform.TransformPoint(vs[tri[k + 2]]);
                        string key = name + "|" + Tri(a, b, c);
                        if (expected.TryGetValue(key, out var count) && count > 0)
                        {
                            expected[key] = count - 1; n++;
                            var owner = ownerOf[key];
                            removed[owner]++; byTrough[owner] = byTrough.TryGetValue(owner, out var bn) ? bn + 1 : 1;
                        }
                        else { kept.Add(tri[k]); kept.Add(tri[k + 1]); kept.Add(tri[k + 2]); }
                    }
                    if (kept.Count != tri.Length) { if (!copy) copy = Object.Instantiate(mesh); copy.SetTriangles(kept, si, false); }
                }
                if (!copy) continue;
                sampleRenderer ??= r; plantBatchSample ??= r.sharedMaterials;
                string ap = Dir + "/batch_" + Hash(PathOf(r.transform)) + ".asset";
                touched.Add(new Dictionary<string, object> { ["renderer"] = PathOf(r.transform), ["originalMesh"] = AssetDatabase.GetAssetPath(mesh) + "#" + mesh.name, ["trianglesRemoved"] = n, ["byTrough"] = byTrough.ToDictionary(k => k.Key, k => (object)k.Value), ["generated"] = ap, ["layer"] = r.gameObject.layer, ["renderingLayerMask"] = (long)r.renderingLayerMask, ["lightProbes"] = r.lightProbeUsage.ToString(), ["reflectionProbes"] = r.reflectionProbeUsage.ToString(), ["shadow"] = r.shadowCastingMode.ToString(), ["static"] = GameObjectUtility.GetStaticEditorFlags(r.gameObject).ToString() });
                mf.sharedMesh = SaveMesh(copy, ap);
            }
            foreach (var t in troughs) if (removed[t.Id] != perTrough[t.Id]) throw new InvalidDataException($"OldTownFoliage075: {t.Id} baked plant match {removed[t.Id]}/{perTrough[t.Id]}; refusing partial removal");
            report["batchesChanged"] = touched;
            if (!sampleRenderer) throw new InvalidDataException("OldTownFoliage075: no plant batch changed");

            // 2. original FBX instances, original OldTown materials by GUID, on the trough's placement transform
            var instRows = new List<object>(); var checks = new List<object>(); bool allPass = true;
            foreach (var t in troughs)
            {
                if (AssetDatabase.AssetPathToGUID(t.Fbx) != t.FbxGuid) throw new InvalidDataException("OldTownFoliage075: FBX GUID " + t.Fbx);
                if (FileSha(t.Fbx) != t.FbxSha) throw new InvalidDataException("OldTownFoliage075: FBX sha256 changed " + t.Fbx);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(t.Fbx);
                if (model.GetComponentsInChildren<Collider>(true).Length != 0 || model.GetComponentsInChildren<LODGroup>(true).Length != 0) throw new InvalidDataException("OldTownFoliage075: source has a collider or LODGroup " + t.Fbx);
                var srcRenderers = model.GetComponentsInChildren<MeshRenderer>(true);
                if (srcRenderers.Length != 1) throw new InvalidDataException($"OldTownFoliage075: {t.Fbx} has {srcRenderers.Length} renderers");
                var defaults = srcRenderers[0].sharedMaterials;
                if (!defaults.Select(m => m ? m.name : "-").SequenceEqual(t.Slots)) throw new InvalidDataException($"OldTownFoliage075: {t.Fbx} slots {string.Join(",", defaults.Select(m => m ? m.name : "-"))} != {string.Join(",", t.Slots)}");
                var bound = t.Slots.Select(Bound).ToArray();
                var (axes, axesErr, modelBounds) = ImportAxes(model, t);
                var f = frames[t.Id];
                var placement = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(x => x.name == t.Id).ToArray();
                if (placement.Length != 1) throw new InvalidDataException($"OldTownFoliage075: {t.Id} has {placement.Length} placements");
                if ((placement[0].lossyScale - Vector3.one).sqrMagnitude > 1e-8f) throw new InvalidDataException($"OldTownFoliage075: {t.Id} placement scale {placement[0].lossyScale}");
                var group = new GameObject(Group).transform; group.SetParent(placement[0], false);
                Bounds? trial = null; // the retained slot 0 stone, then every plant vertex
                Enc(ref trial, containers[t.Id].min); Enc(ref trial, containers[t.Id].max);
                int bodyBreach = 0, belowCoping = 0, coplanar = 0; var flat = new Dictionary<string, (int n, float area)>(); float minLocalZ = float.MaxValue, maxLocalZ = float.MinValue; var instBounds = new List<object>();
                // the stone's solid cross-section: half-width across (y) and along (x) at each height, from slot 0's own vertices
                var prof = profiles[t.Id]; float copingTop = prof.Max(q => q.z);
                float copingHalfY = prof.Max(q => q.y);
                float copingBottom = prof.Where(q => q.y > copingHalfY - .002f).Min(q => q.z);
                float bodyHalfY = prof.Where(q => q.z < copingBottom - .002f && q.z > .05f).Select(q => q.y).DefaultIfEmpty(copingHalfY).Max();
                float bodyHalfX = prof.Where(q => q.z < copingBottom - .002f && q.z > .05f).Select(q => q.x).DefaultIfEmpty(prof.Max(q => q.x)).Max();
                foreach (var p in t.Plants)
                {
                    var rz = Matrix4x4.Rotate(Quaternion.AngleAxis(p.Yaw, Vector3.forward)); // Blender: counter-clockwise about +z
                    var want = f.BlenderToWorld * Matrix4x4.Translate(new Vector3(p.X, 0f, p.Z)) * rz * Matrix4x4.Scale(Vector3.one * p.Scale) * axes.inverse;
                    var pos = want.GetColumn(3); var rot = Quaternion.LookRotation(want.GetColumn(2), want.GetColumn(1));
                    var check = Matrix4x4.TRS(pos, rot, Vector3.one * p.Scale);
                    for (int i = 0; i < 16; i++) if (Mathf.Abs(check[i] - want[i]) > 1e-5f) throw new InvalidDataException($"OldTownFoliage075: {p.Name} pose is not a uniform TRS");
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                    go.name = p.Name; go.transform.SetParent(group, false); go.transform.SetPositionAndRotation(pos, rot); go.transform.localScale = Vector3.one * p.Scale;
                    var r = go.GetComponentInChildren<MeshRenderer>(true); var mf = r.GetComponent<MeshFilter>();
                    r.sharedMaterials = bound; // explicit per slot, the OldTown GUIDs; no name-based remap
                    r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
                    r.lightProbeUsage = sampleRenderer.lightProbeUsage; r.reflectionProbeUsage = sampleRenderer.reflectionProbeUsage;
                    r.renderingLayerMask = sampleRenderer.renderingLayerMask; // as the baked planting it replaces
                    foreach (var x in go.GetComponentsInChildren<Transform>(true)) { x.gameObject.layer = sampleRenderer.gameObject.layer; GameObjectUtility.SetStaticEditorFlags(x.gameObject, GameObjectUtility.GetStaticEditorFlags(sampleRenderer.gameObject)); }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    if (go.GetComponentsInChildren<Collider>(true).Length != 0 || go.GetComponentsInChildren<LODGroup>(true).Length != 0) throw new InvalidDataException("OldTownFoliage075: instance collider/LOD " + p.Name);
                    if (AssetDatabase.GetAssetPath(mf.sharedMesh) != t.Fbx) throw new InvalidDataException("OldTownFoliage075: instance mesh is not the source FBX's " + p.Name);
                    Bounds? ib = null; Bounds? il = null;
                    using (var data = MeshUtility.AcquireReadOnlyMeshData(mf.sharedMesh))
                    {
                        using var vs = new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount, Unity.Collections.Allocator.Temp); data[0].GetVertices(vs);
                        foreach (var v in vs)
                        {
                            var w = mf.transform.TransformPoint(v); var lp = f.Local(w); Enc(ref ib, w); Enc(ref il, lp); Enc(ref trial, w);
                            minLocalZ = Mathf.Min(minLocalZ, lp.z); maxLocalZ = Mathf.Max(maxLocalZ, lp.z);
                            if (lp.z < copingBottom - .0005f) { belowCoping++; if (Mathf.Abs(lp.y) > bodyHalfY + .0005f || Mathf.Abs(lp.x) > bodyHalfX + .0005f) bodyBreach++; }
                        }
                        // flat faces lying on the coping top (z-fight risk): triangles whose three vertices sit within 1 mm of it
                        for (int si = 0; si < mf.sharedMesh.subMeshCount; si++)
                        {
                            using var ix = new Unity.Collections.NativeArray<int>(data[0].GetSubMesh(si).indexCount, Unity.Collections.Allocator.Temp); data[0].GetIndices(ix, si);
                            for (int k = 0; k < ix.Length; k += 3)
                                if (new[] { ix[k], ix[k + 1], ix[k + 2] }.All(q => Mathf.Abs(f.Local(mf.transform.TransformPoint(vs[q])).z - copingTop) < .001f))
                                {
                                    var w0 = mf.transform.TransformPoint(vs[ix[k]]); var w1 = mf.transform.TransformPoint(vs[ix[k + 1]]); var w2 = mf.transform.TransformPoint(vs[ix[k + 2]]);
                                    var nrm = Vector3.Cross(w1 - w0, w2 - w0); // Unity winding: clockwise = front, so the front normal is -cross for a left-handed view
                                    var mat = r.sharedMaterials[si]; bool culled = mat.HasProperty("_Cull") && mat.GetFloat("_Cull") > .5f;
                                    // Unity's front face is clockwise seen from the camera: front normal = Cross(b - a, c - a) in Unity's left-handed space
                                    string side = nrm.y > 0 ? "frontFacesUp" : "frontFacesDown";
                                    string key = $"{mat.name}|cull{(culled ? "Back" : "Off")}|{side}";
                                    flat[key] = (flat.TryGetValue(key, out var fv) ? fv.n : 0) + 1 is var nn ? (nn, (flat.TryGetValue(key, out var fa) ? fa.area : 0f) + nrm.magnitude * .5f) : default;
                                    if (side == "frontFacesUp" || !culled) coplanar++;
                                }
                        }
                    }
                    var sub = Enumerable.Range(0, mf.sharedMesh.subMeshCount).Select(s => (object)((long)mf.sharedMesh.GetIndexCount(s) / 3)).ToList();
                    instRows.Add(new Dictionary<string, object>
                    {
                        ["trough"] = t.Id, ["name"] = p.Name, ["path"] = PathOf(go.transform), ["renderer"] = PathOf(r.transform), ["local"] = new List<object> { R(p.X), 0, R(p.Z) }, ["yawDegBlender"] = R(p.Yaw), ["scale"] = R(p.Scale),
                        ["worldPosition"] = L(go.transform.position), ["worldEuler"] = L(go.transform.eulerAngles), ["worldRotation"] = go.transform.rotation.ToString("F6"), ["lossyScale"] = L(r.transform.lossyScale),
                        ["mesh"] = AssetDatabase.GetAssetPath(mf.sharedMesh) + "#" + mf.sharedMesh.name, ["importerDefaultMaterialGuids"] = defaults.Select(m => (object)AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m))).ToList(), ["meshVertices"] = mf.sharedMesh.vertexCount, ["submeshTriangles"] = sub,
                        ["materials"] = r.sharedMaterials.Select(m => (object)(AssetDatabase.GetAssetPath(m) + " " + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m)))).ToList(),
                        ["worldBounds"] = B(ib.Value), ["localBounds"] = B(il.Value), ["layer"] = r.gameObject.layer, ["renderingLayerMask"] = (long)r.renderingLayerMask, ["cutawayBit20"] = (r.renderingLayerMask & (1u << 20)) != 0,
                        ["shadow"] = r.shadowCastingMode.ToString(), ["lightProbes"] = r.lightProbeUsage.ToString(), ["reflectionProbes"] = r.reflectionProbeUsage.ToString(), ["colliders"] = 0, ["lodGroups"] = 0,
                    });
                    instBounds.Add(B(il.Value));
                }
                var env = envelopes[t.Id]; bool inEnv = Contains(env, trial.Value, .001f);
                Bounds? trialLocal = null; foreach (var c in Corners(trial.Value)) Enc(ref trialLocal, f.Local(c));
                bool topOk = maxLocalZ <= t.Top + .0005f; bool bodyOk = bodyBreach == 0;
                bool pass = inEnv && topOk && bodyOk && coplanar == 0;
                allPass &= pass;
                checks.Add(new Dictionary<string, object>
                {
                    ["trough"] = t.Id, ["fbx"] = t.Fbx, ["fbxGuid"] = t.FbxGuid, ["fbxSha256"] = t.FbxSha, ["importAxesBlenderToModel"] = axes.ToString("F0").Replace("\n", " | "), ["importAxesBoundsError"] = R(axesErr), ["modelBounds"] = B(modelBounds),
                    ["currentEnvelopeWorld"] = B(env), ["trialWorld"] = B(trial.Value), ["trialLocal"] = B(trialLocal.Value), ["withinCurrentEnvelope1mm"] = inEnv,
                    ["plantLocalZ"] = new List<object> { R(minLocalZ), R(maxLocalZ) }, ["inheritedTop"] = R(t.Top), ["topWithinInherited"] = topOk,
                    ["copingTop"] = R(copingTop), ["copingBottom"] = R(copingBottom), ["copingHalfAcross"] = R(copingHalfY), ["bodyHalfAcross"] = R(bodyHalfY), ["bodyHalfAlong"] = R(bodyHalfX),
                    ["plantVerticesBelowCoping"] = belowCoping, ["plantVerticesOutsideBodyBelowCoping"] = bodyBreach, ["bodyOk"] = bodyOk, ["trianglesOnCopingTop1mm(visibleFromAbove)"] = coplanar, ["flatOnCopingTop"] = flat.ToDictionary(k => k.Key, k => (object)$"{k.Value.n} tris, {k.Value.area:F5} m2"), ["pass"] = pass,
                });
            }
            report["instances"] = instRows; report["fit"] = checks; report["flowerZ"] = R(flowerZ); report["allPass"] = allPass;
            Write(Arg("oldtownTag", "apply") + ".json", report);
            if (!allPass) throw new InvalidDataException("OldTownFoliage075: fit check failed; see " + Arg("oldtownTag", "apply") + ".json; scene not saved");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Repo, "unity", outScene)));
            if (!EditorSceneManager.SaveScene(scene, outScene)) throw new IOException(outScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"OldTownFoliage075: {instRows.Count} original OldTown plants in 2 troughs -> {outScene}");
        });

        static Bounds LocalBounds(Frame f, Bounds w) { Bounds? b = null; foreach (var c in Corners(w)) Enc(ref b, f.Local(c)); return b.Value; }
        static IEnumerable<Vector3> Corners(Bounds b) { for (int i = 0; i < 8; i++) yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z); }
        static bool Contains(Bounds a, Bounds b, float tol) => b.min.x >= a.min.x - tol && b.min.y >= a.min.y - tol && b.min.z >= a.min.z - tol && b.max.x <= a.max.x + tol && b.max.y <= a.max.y + tol && b.max.z <= a.max.z + tol;

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            if (!path.StartsWith(Dir + "/", StringComparison.Ordinal)) throw new IOException(path);
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Suborned/Generated", "OldTownFoliage075");
            var prior = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (prior) { EditorUtility.CopySerialized(mesh, prior); EditorUtility.SetDirty(prior); Object.DestroyImmediate(mesh); return prior; }
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        /// <summary>Counts for the café: every renderer under the two placements and every civic plant batch, with LODGroup presence.</summary>
        public static void Census() => HarbourWorldBuilder.Batch(() =>
        {
            var scene = EditorSceneManager.OpenScene(Arg("oldtownScene", "Assets/Scenes/probe-oldtown-foliage075.unity"), OpenSceneMode.Single);
            var rows = new List<object>(); long tris = 0; int subs = 0, rs = 0;
            foreach (var id in new[] { "civic/run-west-cafe/0", "civic/run-west-cafe/1" })
            {
                var p = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Single(x => x.name == id);
                foreach (var r in p.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var m = r.GetComponent<MeshFilter>().sharedMesh; long n = Enumerable.Range(0, m.subMeshCount).Sum(s => (long)m.GetIndexCount(s) / 3);
                    tris += n; subs += m.subMeshCount; rs++;
                    rows.Add(new Dictionary<string, object> { ["path"] = PathOf(r.transform), ["triangles"] = n, ["submeshes"] = m.subMeshCount, ["lodGroupAbove"] = r.GetComponentInParent<LODGroup>(true) ? true : false, ["enabled"] = r.enabled && r.gameObject.activeInHierarchy });
                }
            }
            // SRP Batcher compatibility is only initialised once the pipeline has drawn: render the play camera once, then ask (read-only)
            var srp = new Dictionary<string, object>();
            var cam = GameObject.Find("cameras/play")?.GetComponent<Camera>();
            if (cam) using (var session = new RenderSession())
            {   // framed on the two troughs at play zoom, so the OldTown shader actually draws
                Baseline072.Pose(cam, cam.GetComponent<TacticalShadows>(), cam.GetComponent<ArchitectureFade>(), new Vector2(-179.5f, -220.5f), 210f, Baseline072.DefaultStandOff);
                session.Grab(cam, 64, 64); session.Grab(cam, 1920, 1080);
            }
            var code = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var why = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            foreach (var sp in new[] { "Assets/Suborned/OldTownArt/DecoKit/Shaders/DK_LitCutaway.shader", "Assets/Suborned/Shaders/DecoKit/DK_LitCutaway.shader" })
            {
                var sh = AssetDatabase.LoadAssetAtPath<Shader>(sp); object c = "unavailable", w = "unavailable";
                try { c = Convert.ToInt32(code.Invoke(null, new object[] { sh, 0 })); w = why.Invoke(null, new object[] { sh, 0, c }); } catch (Exception e) { c = "error: " + e.GetBaseException().Message; }
                srp[sp] = $"code {c} ({w})";
            }
            Write(Arg("oldtownTag", "census") + ".json", new Dictionary<string, object> { ["scene"] = scene.path, ["srpBatcherAfterRender(0=compatible)"] = srp, ["renderedOnce"] = cam ? true : false, ["placementRenderers"] = rows, ["renderers"] = rs, ["triangles"] = tris, ["submeshDraws"] = subs, ["note"] = "placement renderers only; the trough stone stays in the civic batches (shared with other civic pieces), so its 60 triangles are not counted here" });
        });

        static void Write(string name, Dictionary<string, object> value)
        {
            var dir = Path.Combine(Repo, Arg("oldtownOut", "briefs/075/recovery/oldtown-foliage-unity")); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, name), JsonText(value));
        }
        static string JsonText(object o)
        {
            if (o == null) return "null";
            if (o is string s) return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";
            if (o is bool b) return b ? "true" : "false";
            if (o is Dictionary<string, object> d) return "{" + string.Join(",", d.Select(k => JsonText(k.Key) + ":" + JsonText(k.Value))) + "}";
            if (o is List<object> l) return "[" + string.Join(",", l.Select(JsonText)) + "]";
            return Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
