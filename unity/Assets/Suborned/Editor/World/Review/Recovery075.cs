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
    /// <summary>Provisional, opt-in review only. Does not register kit assets or run in normal builds.
    /// Keeps placement transforms, renderers, LODs, colliders and non-foliage submeshes; only selected LOD0 foliage changes.
    /// -recoveryFoliage selective is required to write a candidate scene. Baseline is the default.</summary>
    public static class Recovery075
    {
        const string Dir = "Assets/Suborned/Generated/Review075";
        static readonly string[] Selected = { "civic/run-west-cafe/1", "civic/civic-office-north/tuft-0", "civic/cyp-office-e" };
        static string Arg(string key, string d) { var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-" + key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : d; }
        static string Repo => HarbourWorldBuilder.RepoRoot;
        static string Hash(string s) { using var sha = SHA256.Create(); return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(s)).Select(b => b.ToString("x2"))); }
        static string V(Vector3 v) => $"{Mathf.RoundToInt(v.x * 100000f)},{Mathf.RoundToInt(v.y * 100000f)},{Mathf.RoundToInt(v.z * 100000f)}";
        static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
        static bool Plant(string n) => n.StartsWith("DK_Leaf_", StringComparison.Ordinal) || n.StartsWith("DK_Bloom_", StringComparison.Ordinal);
        static Material Resolve(string name)
        {
            if (name == "DK_Limestone") name = "DK_Limestone_Tex";
            return AssetDatabase.LoadAssetAtPath<Material>($"Assets/Suborned/Kit/Materials/{name}.mat") ?? throw new InvalidDataException("Recovery075: exact material missing: " + name);
        }

        public static void Import() => HarbourWorldBuilder.Batch(() =>
        {
            foreach (var path in Directory.GetFiles(Path.Combine(Application.dataPath, "Suborned/Generated/Review075"), "*.fbx"))
            {
                string ap = "Assets" + path.Substring(Application.dataPath.Length);
                AssetDatabase.ImportAsset(ap, ImportAssetOptions.ForceSynchronousImport);
                var imp = (ModelImporter)AssetImporter.GetAtPath(ap);
                imp.isReadable = true; imp.addCollider = false; imp.meshCompression = ModelImporterMeshCompression.Off;
                imp.importAnimation = false; imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
                imp.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ap);
                if (model.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidDataException("candidate collider " + ap);
                foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true))
                    foreach (var mat in r.sharedMaterials) Resolve(mat.name); // exact names, never slot numbers
                Debug.Log("Recovery075 imported " + ap);
            }
        });

        public static void Apply() => HarbourWorldBuilder.Batch(() =>
        {
            if (Arg("recoveryFoliage", "off") != "selective") throw new InvalidOperationException("Recovery075 is default-off; pass -recoveryFoliage selective for review");
            var scene = EditorSceneManager.OpenScene(Arg("recoveryScene", "Assets/Scenes/probe-oldtown-ours.unity"), OpenSceneMode.Single);
            var world = WorldReader.Load(Path.Combine(Repo, Arg("world", "out/world/harbour-district.world.json")));
            var edits = new List<object>();
            foreach (var id in Selected)
            {
                var mass = world.Civic.Single(m => m.Id == id);
                var roots = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name == id).ToArray();
                if (roots.Length != 1) throw new InvalidDataException($"Recovery075: {id} has {roots.Length} placements");
                var root = roots[0];
                var renderers = KitLods.Lod0(root.gameObject).ToArray();
                if (renderers.Length == 0) { edits.Add(Baked(root, mass, scene.GetRootGameObjects())); continue; }
                if (renderers.Length != 1) throw new InvalidDataException($"Recovery075: {id} LOD0 has {renderers.Length} renderers");
                var r = renderers[0]; var mf = r.GetComponent<MeshFilter>(); var original = mf.sharedMesh;
                string ap = $"{Dir}/{mass.Kit.Id}_Recovery075.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ap) ?? throw new FileNotFoundException(ap);
                var candidate = model.GetComponentsInChildren<MeshFilter>(true).Single();
                var cmats = candidate.GetComponent<MeshRenderer>().sharedMaterials;
                var originalMats = r.sharedMaterials;
                var combines = new List<CombineInstance>(); var mats = new List<Material>();
                for (int s = 0; s < original.subMeshCount; s++)
                    if (!Plant(originalMats[s].name)) { combines.Add(new CombineInstance { mesh = original, subMeshIndex = s, transform = Matrix4x4.identity }); mats.Add(originalMats[s]); }
                var kit = new KitPlacer(HarbourWorldBuilder.KitCataloguePath);
                var pose = Matrix4x4.TRS(new Vector3(mass.Kit.At.x, kit.PivotForLowest(mass.Kit, mass.Kit.Base), mass.Kit.At.y), Quaternion.Euler(0f, -mass.Kit.Yaw * Mathf.Rad2Deg, 0f), mass.Kit.Scale);
                var rel = r.transform.worldToLocalMatrix * pose * model.transform.worldToLocalMatrix * candidate.transform.localToWorldMatrix;
                for (int s = 0; s < candidate.sharedMesh.subMeshCount; s++)
                    if (Plant(cmats[s].name)) { combines.Add(new CombineInstance { mesh = candidate.sharedMesh, subMeshIndex = s, transform = rel }); mats.Add(Resolve(cmats[s].name)); }
                var mesh = new Mesh { name = original.name + "_Recovery075", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(combines.ToArray(), false, true, false); mesh.RecalculateBounds();
                // Non-foliage comes from the original, not the candidate. Do not copy any candidate collider, object or transform.
                var beforeBounds = original.bounds; var afterBounds = mesh.bounds;
                if (!Contains(beforeBounds, afterBounds, .015f)) throw new InvalidDataException($"Recovery075: import bounds escaped {id}: old {beforeBounds}, candidate {afterBounds}, matrix {rel}");
                string asset = $"{Dir}/selective3_{id.Replace('/', '_')}.asset";
                mesh = SaveMesh(mesh, asset);
                mf.sharedMesh = mesh; r.sharedMaterials = mats.ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(mf); PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                edits.Add(new Dictionary<string, object> { ["id"] = id, ["piece"] = mass.Kit.Id, ["renderer"] = PathOf(r.transform), ["originalMesh"] = AssetDatabase.GetAssetPath(original), ["candidateMesh"] = asset, ["beforeTriangles"] = (long)original.GetIndexCount(0) / 3 + Enumerable.Range(1, original.subMeshCount - 1).Sum(s => (long)original.GetIndexCount(s) / 3), ["afterTriangles"] = Enumerable.Range(0, mesh.subMeshCount).Sum(s => (long)mesh.GetIndexCount(s) / 3), ["materials"] = mats.Select(m => (object)m.name).ToList(), ["nonFoliage"] = "original submeshes retained verbatim; no candidate trough or trunk" });
            }
            CheckBounds(world, scene);
            var outScene = Arg("recoverySave", "Assets/Scenes/probe-recovery075-foliage3.unity");
            if (!EditorSceneManager.SaveScene(scene, outScene)) throw new IOException(outScene);
            Write("placements.json", new Dictionary<string, object> { ["status"] = "provisional-review-only; default-off", ["placements"] = edits });
            Debug.Log($"Recovery075: {Selected.Length} scoped LOD0 foliage substitutions -> {outScene}");
        });
        static Mesh SaveMesh(Mesh mesh, string path)
        {
            // Only the helper's own generated Review075 assets, including recovery of an interrupted Apply.
            if (!path.StartsWith(Dir + "/", StringComparison.Ordinal)) throw new IOException(path);
            var prior = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (prior) { EditorUtility.CopySerialized(mesh, prior); EditorUtility.SetDirty(prior); Object.DestroyImmediate(mesh); return prior; }
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static string Triangle(Vector3 a, Vector3 b, Vector3 c) => string.Join(";", new[] { Q(a), Q(b), Q(c) }.OrderBy(x => x, StringComparer.Ordinal));
        static string Q(Vector3 p) => $"{Mathf.RoundToInt(p.x * 1000f)},{Mathf.RoundToInt(p.y * 1000f)},{Mathf.RoundToInt(p.z * 1000f)}";
        static object Baked(Transform placement, MassDto mass, GameObject[] roots)
        {
            var kit = new KitPlacer(HarbourWorldBuilder.KitCataloguePath);
            var original = kit.Prefab(mass.Kit.Id);
            var source = KitLods.Lod0(original).Single(); var sm = source.GetComponent<MeshFilter>().sharedMesh;
            var pivot = kit.PivotForLowest(mass.Kit, mass.Kit.Base);
            var trs = Matrix4x4.TRS(new Vector3(mass.Kit.At.x, pivot, mass.Kit.At.y), Quaternion.Euler(0f, -mass.Kit.Yaw * Mathf.Rad2Deg, 0f), mass.Kit.Scale);
            var matrix = trs * original.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
            var expected = new Dictionary<string, int>(); int old = 0;
            using (var data = UnityEditor.MeshUtility.AcquireReadOnlyMeshData(sm))
            {
                using var vs = new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount, Unity.Collections.Allocator.Temp); data[0].GetVertices(vs);
                for (int si = 0; si < sm.subMeshCount; si++)
                {
                    var name = source.sharedMaterials[si].name; if (!Plant(name)) continue;
                    using var ix = new Unity.Collections.NativeArray<int>(data[0].GetSubMesh(si).indexCount, Unity.Collections.Allocator.Temp); data[0].GetIndices(ix, si);
                    for (int t = 0; t < ix.Length; t += 3)
                    {
                        string k = name + "|" + Triangle(matrix.MultiplyPoint3x4(vs[ix[t]]), matrix.MultiplyPoint3x4(vs[ix[t + 1]]), matrix.MultiplyPoint3x4(vs[ix[t + 2]]));
                        expected[k] = expected.TryGetValue(k, out var n) ? n + 1 : 1; old++;
                    }
                }
            }
            var touched = new List<object>(); int removed = 0;
            foreach (var r in roots.SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).Where(r => PathOf(r.transform).StartsWith("batches/civic/", StringComparison.Ordinal)))
            {
                if (!r.sharedMaterials.Any(mat => mat && Plant(mat.name))) continue;
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                var mesh = mf.sharedMesh; var vs = mesh.vertices; Mesh copy = null; int n = 0;
                for (int si = 0; si < mesh.subMeshCount; si++)
                {
                    string name = r.sharedMaterials[si].name; if (!Plant(name)) continue;
                    var tri = mesh.GetTriangles(si); var kept = new List<int>();
                    for (int t = 0; t < tri.Length; t += 3)
                    {
                        string k = name + "|" + Triangle(mf.transform.TransformPoint(vs[tri[t]]), mf.transform.TransformPoint(vs[tri[t + 1]]), mf.transform.TransformPoint(vs[tri[t + 2]]));
                        if (expected.TryGetValue(k, out var count) && count > 0) { expected[k] = count - 1; n++; } else { kept.Add(tri[t]); kept.Add(tri[t + 1]); kept.Add(tri[t + 2]); }
                    }
                    if (kept.Count != tri.Length) { if (!copy) copy = Object.Instantiate(mesh); copy.SetTriangles(kept, si, false); }
                }
                if (!copy) continue;
                string ap = Dir + "/selective3_batch_" + Hash(PathOf(r.transform) + "|" + mass.Id) + ".asset";
                copy = SaveMesh(copy, ap); mf.sharedMesh = copy; removed += n; touched.Add(PathOf(r.transform));
            }
            if (removed != old) throw new InvalidDataException($"Recovery075 baked match {mass.Id}: {removed}/{old} original foliage triangles found; refusing partial substitution");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{mass.Kit.Id}_Recovery075.fbx");
            var candidate = model.GetComponentsInChildren<MeshFilter>(true).Single(); var mats = candidate.GetComponent<MeshRenderer>().sharedMaterials;
            var combines = new List<CombineInstance>(); var resolved = new List<Material>();
            var rel = placement.worldToLocalMatrix * trs * model.transform.worldToLocalMatrix * candidate.transform.localToWorldMatrix;
            for (int si = 0; si < candidate.sharedMesh.subMeshCount; si++) if (Plant(mats[si].name)) { combines.Add(new CombineInstance { mesh = candidate.sharedMesh, subMeshIndex = si, transform = rel }); resolved.Add(Resolve(mats[si].name)); }
            var replacement = new Mesh { name = mass.Kit.Id + "_Recovery075_foliage_only", indexFormat = IndexFormat.UInt32 }; replacement.CombineMeshes(combines.ToArray(), false, true, false);
            string asset = Dir + "/selective3_" + mass.Id.Replace('/', '_') + ".asset"; replacement = SaveMesh(replacement, asset);
            placement.gameObject.AddComponent<MeshFilter>().sharedMesh = replacement;
            var renderer = placement.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterials = resolved.ToArray();
            // Original baked civic foliage casts shadows. No new transform, collider, trough or trunk.
            renderer.shadowCastingMode = ShadowCastingMode.On;
            int after = Enumerable.Range(0, replacement.subMeshCount).Sum(si => (int)replacement.GetIndexCount(si) / 3);
            return new Dictionary<string, object> { ["id"] = mass.Id, ["piece"] = mass.Kit.Id, ["renderer"] = PathOf(placement), ["beforeTriangles"] = old, ["afterTriangles"] = after, ["candidateMesh"] = asset, ["batchesChanged"] = touched, ["nonFoliage"] = "only exact original leaf/bloom triangles removed from civic batches; original trough/trunk triangles unchanged; candidate foliage on existing placement transform" };
        }

        static bool Contains(Bounds a, Bounds b, float tolerance) => b.min.x >= a.min.x - tolerance && b.min.y >= a.min.y - tolerance && b.min.z >= a.min.z - tolerance && b.max.x <= a.max.x + tolerance && b.max.y <= a.max.y + tolerance && b.max.z <= a.max.z + tolerance;

        public static void BoundsProof() => HarbourWorldBuilder.Batch(() =>
        {
            var scene = EditorSceneManager.OpenScene(Arg("recoveryScene", "Assets/Scenes/probe-recovery075-foliage3.unity"), OpenSceneMode.Single);
            var world = WorldReader.Load(Path.Combine(Repo, Arg("world", "out/world/harbour-district.world.json")));
            CheckBounds(world, scene);
        });
        static void CheckBounds(WorldDto world, UnityEngine.SceneManagement.Scene scene)
        {
            var kit = new KitPlacer(HarbourWorldBuilder.KitCataloguePath); var rows = new List<object>(); bool allContained = true;
            foreach (var id in Selected)
            {
                var mass = world.Civic.Single(m => m.Id == id); var prefab = kit.Prefab(mass.Kit.Id);
                var trs = Matrix4x4.TRS(new Vector3(mass.Kit.At.x, kit.PivotForLowest(mass.Kit, mass.Kit.Base), mass.Kit.At.y), Quaternion.Euler(0f, -mass.Kit.Yaw * Mathf.Rad2Deg, 0f), mass.Kit.Scale);
                var source = KitLods.Lod0(prefab).Single();
                var matrix = trs * prefab.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
                Bounds original = default; bool first = true;
                using (var data = UnityEditor.MeshUtility.AcquireReadOnlyMeshData(source.GetComponent<MeshFilter>().sharedMesh))
                {
                    using var vs = new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount, Unity.Collections.Allocator.Temp); data[0].GetVertices(vs);
                    foreach (var v in vs) { var p = matrix.MultiplyPoint3x4(v); if (first) { original = new Bounds(p, Vector3.zero); first = false; } else original.Encapsulate(p); }
                }
                var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Single(t => t.name == id);
                var r = root.GetComponentsInChildren<MeshRenderer>(true).Single(mr => mr.GetComponent<MeshFilter>() && AssetDatabase.GetAssetPath(mr.GetComponent<MeshFilter>().sharedMesh).StartsWith(Dir + "/", StringComparison.Ordinal));
                var mesh = r.GetComponent<MeshFilter>().sharedMesh; Bounds actual = default; first = true;
                foreach (var v in mesh.vertices) { var p = r.transform.TransformPoint(v); if (first) { actual = new Bounds(p, Vector3.zero); first = false; } else actual.Encapsulate(p); }
                bool contained = Contains(original, actual, .001f);
                rows.Add(new Dictionary<string, object> { ["id"] = id, ["sourceBounds"] = original.ToString("F5"), ["candidateBounds"] = actual.ToString("F5"), ["containedWithin1mmTolerance"] = contained });
                allContained &= contained;
            }
            Write("bounds-proof.json", new Dictionary<string, object> { ["status"] = allContained ? "PASS production envelope, tolerance 1 mm" : "FAIL production envelope; review-only rejected candidates", ["placements"] = rows });
            if (!allContained) throw new InvalidDataException("Recovery075: some imported render meshes escape original production envelopes; see bounds-proof.json");
            Debug.Log("Recovery075: bounds proof passed for selected placements");
        }

        // Ordered per-renderer manifests allow exact, bounded A/B comparison rather than trusting a successful build.
        public static void Manifest() => HarbourWorldBuilder.Batch(() =>
        {
            var scene = EditorSceneManager.OpenScene(Arg("recoveryScene", "Assets/Scenes/probe-oldtown-ours.unity"), OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var ts = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var rows = new List<object>(); long tris = 0; int slots = 0, shadow = 0, alpha = 0;
            var cache = new Dictionary<string, (string all, string non)>();
            foreach (var r in ts.Select(t => t.GetComponent<MeshRenderer>()).Where(r => r))
            {
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                var m = mf.sharedMesh; var mats = r.sharedMaterials; var count = Enumerable.Range(0, m.subMeshCount).Sum(s => (long)m.GetIndexCount(s) / 3);
                var key = m.GetEntityId() + "|" + string.Join("|", mats.Select(mat => mat ? mat.name : "-"));
                if (!cache.TryGetValue(key, out var fingerprints))
                {
                    using var data = UnityEditor.MeshUtility.AcquireReadOnlyMeshData(m);
                    using var v = new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount, Unity.Collections.Allocator.Temp);
                    data[0].GetVertices(v);
                    using var allHash = SHA256.Create(); using var nonHash = SHA256.Create();
                    for (int s = 0; s < m.subMeshCount; s++)
                    {
                        var desc = data[0].GetSubMesh(s); if (desc.topology != MeshTopology.Triangles) continue;
                        using var indices = new Unity.Collections.NativeArray<int>(desc.indexCount, Unity.Collections.Allocator.Temp); data[0].GetIndices(indices, s);
                        for (int k = 0; k < indices.Length; k += 3)
                        {
                            var bytes = Encoding.UTF8.GetBytes(V(v[indices[k]]) + ";" + V(v[indices[k + 1]]) + ";" + V(v[indices[k + 2]]) + "\n");
                            allHash.TransformBlock(bytes, 0, bytes.Length, null, 0);
                            if (s < mats.Length && !Plant(mats[s].name)) nonHash.TransformBlock(bytes, 0, bytes.Length, null, 0);
                        }
                    }
                    allHash.TransformFinalBlock(Array.Empty<byte>(), 0, 0); nonHash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    fingerprints = (BitConverter.ToString(allHash.Hash).Replace("-", ""), BitConverter.ToString(nonHash.Hash).Replace("-", "")); cache.Add(key, fingerprints);
                }
                var alphaNames = mats.Where(mat => mat && mat.HasProperty("_AlphaClip") && mat.GetFloat("_AlphaClip") > .5f).Select(mat => mat.name).ToArray();
                rows.Add(new Dictionary<string, object> { ["path"] = PathOf(r.transform), ["mesh"] = AssetDatabase.GetAssetPath(m), ["triangles"] = count, ["geometry"] = fingerprints.all, ["nonFoliage"] = fingerprints.non, ["materials"] = mats.Select(mat => (object)(mat ? mat.name : "-")).ToList(), ["shadow"] = r.shadowCastingMode.ToString(), ["enabled"] = r.enabled, ["active"] = r.gameObject.activeInHierarchy, ["alphaMaterials"] = alphaNames.Cast<object>().ToList() });
                tris += count; slots += mats.Length; if (r.shadowCastingMode != ShadowCastingMode.Off) shadow++; if (alphaNames.Length > 0) alpha++;
            }
            var cols = ts.SelectMany(t => t.GetComponents<Collider>()).Select(c => PathOf(c.transform) + "|" + (c is MeshCollider mc ? AssetDatabase.GetAssetPath(mc.sharedMesh) + ":" + (mc.sharedMesh ? mc.sharedMesh.name : "-") : c is BoxCollider bc ? V(bc.center) + ":" + V(bc.size) : c is CapsuleCollider cc ? V(cc.center) + ":" + cc.radius + ":" + cc.height + ":" + cc.direction : c is SphereCollider sc ? V(sc.center) + ":" + sc.radius : c.GetType().Name) + "|" + c.enabled + "|" + c.isTrigger + "|" + V(c.bounds.center) + "|" + V(c.bounds.size)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var transforms = ts.Select(t => PathOf(t) + "|" + V(t.localPosition) + "|" + t.localRotation.ToString("F7") + "|" + V(t.localScale) + "|" + t.gameObject.activeSelf).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var host = ts.Select(t => t.GetComponent<global::Suborned.Play.HandoverHost>()).FirstOrDefault(h => h);
            string nav = "none"; if (host && host.frame != null) nav = Hash(string.Join(",", host.frame.heights) + "|" + Convert.ToBase64String(host.frame.kinds ?? Array.Empty<byte>()));
            Write(Arg("recoveryTag", "manifest") + ".json", new Dictionary<string, object> { ["scene"] = scene.path, ["status"] = "provisional; G-3a 59>58 BLOCKED", ["renderers"] = rows, ["meshTrianglesAllLODsIncludingInactive"] = tris, ["materialSlotsAllLODs"] = slots, ["shadowCastingRendererCount"] = shadow, ["alphaClippedRendererCount"] = alpha, ["transforms"] = transforms.Cast<object>().ToList(), ["colliders"] = cols.Cast<object>().ToList(), ["nav"] = nav, ["costNote"] = "Counts are not GPU timings or shadow draw calls; alpha clipping can increase overdraw even with unchanged renderer count." });
            Debug.Log("Recovery075 manifest " + scene.path);
        });
        static void Write(string name, Dictionary<string, object> value)
        {
            var dir = Path.Combine(Repo, Arg("recoveryOut", "briefs/075/recovery/latch-integration")); Directory.CreateDirectory(dir);
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
