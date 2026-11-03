#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    /// <summary>Brief 077 at adoption (Latch): the real build, shot and checked; nothing is saved.
    ///   Day: OldTown.unity (the swap, rebuilt from HarbourDistrict by the normal chain) through its play camera (our play look), the probe's 10 views
    ///        (play zoom yaw 30 / 210 at the café square and the Office front, close views of the troughs, an island and a civic bed), After072's
    ///        counts at the four play stops, and the dome check: every renderer of our civic block (paths with "civic") by material, failing on any
    ///        dome planting material or a missing one, with the testbed pieces' LODGroups, their play-zoom levels and the soil's shadows.
    ///   Night: HarbourDistrict.unity through cameras/play at the same views, day and night (LightPhase.Set), people off: the globe lamps and the opal glow.
    /// Out: -plantOut (briefs/077/build): captures/ (local), numbers-day.json, numbers-night.json.</summary>
    public static class PlantingBuild077
    {
        static string Repo => HarbourWorldBuilder.RepoRoot;
        static string Out => Path.Combine(Repo, DecoKitStage.Arg("plantOut", "briefs/077/build"));
        // the dome pieces' own planting materials, pruned from the kit at adoption (READY-adopt): any triangle in one is a dome left over. The old
        // fountain beds' DK_Leaf_Shrub / DK_Leaf_CoreShrub / DK_Flower_Coral are shared with other kit pieces (the café's HangingBasket overlays), so
        // they are listed (sharedAt) but not failed
        static readonly string[] Dome = { "DK_Leaf_Civic", "DK_Leaf_CivicDark", "DK_GroundCover" };
        static readonly string[] Shared = { "DK_Leaf_CoreShrub", "DK_Leaf_Shrub", "DK_Flower_Coral" };
        static bool IsDome(string n) => Dome.Contains(n) || n.StartsWith("DK_Bloom_", StringComparison.Ordinal);
        internal static readonly (string id, Vector2 focus, float yaw, float size)[] Views =
        {
            ("cafe-square-y030", new(-170.5f, -227.5f), 30f, 14f), ("cafe-square-y210", new(-170.5f, -227.5f), 210f, 14f),
            ("office-front-y030", new(-160f, -281f), 30f, 14f), ("office-front-y210", new(-160f, -281f), 210f, 14f),
            ("close-troughs-y030", new(-179.5f, -220.5f), 30f, 4.5f), ("close-troughs-y210", new(-179.5f, -220.5f), 210f, 4.5f),
            ("close-island-y030", new(-164.3f, -232.2f), 30f, 5f), ("close-island-y210", new(-155.7f, -240.8f), 210f, 5f),
            ("close-bed-y030", new(-141.1f, -294.9f), 30f, 6f), ("close-bed-y210", new(-141.1f, -294.9f), 210f, 6f),
        };
        static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;

        public static void Day() => HarbourWorldBuilder.Batch(() =>
        {   // as the probe shot it (no old render-settings swap), so the two compare
            var t0 = DateTime.UtcNow; var dir = Path.Combine(Out, "captures"); Directory.CreateDirectory(dir);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/OldTown.unity", OpenSceneMode.Single);
            DecoKitHarbour.Ocean();
            var play = SwapPlayLook.Find() ?? throw new InvalidDataException("077: the swap has no play camera");
            var n = new Dictionary<string, object> { ["scene"] = "Assets/Scenes/OldTown.unity", ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"), ["unity"] = Application.unityVersion };
            n["civic"] = Civic(scene);
            var shots = new List<object>();
            using (var session = new RenderSession())
            {
                SwapShots.Play(play, Views[0].focus, Views[0].yaw, Views[0].size);
                for (int i = 0; i < 3; i++) DecoKitStage.WithShadows(200f, () => Object.DestroyImmediate(DecoKitStage.Render(play, 1920, 1080, 2)));
                foreach (var v in Views)
                {
                    SwapShots.Play(play, v.focus, v.yaw, v.size);
                    Texture2D px = null; DecoKitStage.WithShadows(200f, () => px = DecoKitStage.Render(play, 1920, 1080, 2));
                    File.WriteAllBytes(Path.Combine(dir, $"{v.id}-build.png"), px.EncodeToPNG()); Object.DestroyImmediate(px);
                    shots.Add(new Dictionary<string, object> { ["view"] = v.id, ["file"] = $"captures/{v.id}-build.png" });
                }
                var spec = new After072.AbSpec { cam = play, shadows = play.GetComponent<TacticalShadows>(), fade = null, standOff = 160f };
                foreach (var v in Views.Where(x => x.size >= 14f)) { var vv = v; spec.views.Add(new After072.AbView { id = vv.id, kind = "play", pose = c => SwapShots.Play(c, vv.focus, vv.yaw, vv.size), focusPlan = vv.focus, yaw = vv.yaw }); }
                n["counts"] = After072.AbCapture(spec);
            }
            n["captures"] = shots; n["seconds"] = Math.Round((DateTime.UtcNow - t0).TotalSeconds, 1);
            Directory.CreateDirectory(Out); File.WriteAllText(Path.Combine(Out, "numbers-day.json"), Json(n));
            var civ = (Dictionary<string, object>)n["civic"];
            Debug.Log($"[077 build] day: {shots.Count} shots; dome triangles in our civic {civ["domeTriangles"]}, missing-material submeshes {civ["missingMaterialSubmeshes"]}, testbed LODGroups {civ["testbedGroups"]}");
            if (Convert.ToInt64(civ["domeTriangles"]) != 0 || Convert.ToInt64(civ["missingMaterialSubmeshes"]) != 0) throw new InvalidDataException("077: dome planting (or a missing material) is still in our civic block; see numbers-day.json");
        });

        /// <summary>Our civic block in the swap: triangles by material over every renderer whose path holds "civic", the dome materials and missing ones,
        /// and every LODGroup drawing a testbed material (DK_*_TB): its name, level count, play-zoom level and the soil renderers' shadows.</summary>
        static Dictionary<string, object> Civic(UnityEngine.SceneManagement.Scene scene)
        {
            var byMat = new SortedDictionary<string, long>(StringComparer.Ordinal); long dome = 0; int missing = 0, renderers = 0;
            var domeAt = new SortedDictionary<string, long>(StringComparer.Ordinal); // renderer path | material -> triangles
            var sharedAt = new SortedDictionary<string, long>(StringComparer.Ordinal);
            var rs = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).Where(r => PathOf(r.transform).Contains("civic")).ToArray();
            foreach (var r in rs)
            {
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue; renderers++;
                var m = mf.sharedMesh; var ms = r.sharedMaterials;
                for (int s = 0; s < m.subMeshCount; s++)
                {
                    long t = m.GetIndexCount(s) / 3; var mat = s < ms.Length ? ms[s] : null;
                    var name = mat ? mat.name : "(missing)"; if (!mat) missing++;
                    byMat[name] = (byMat.TryGetValue(name, out var c) ? c : 0) + t;
                    if (mat && IsDome(mat.name)) { dome += t; var k = $"{PathOf(r.transform)} | {mat.name} @ {AssetDatabase.GetAssetPath(mat)}"; domeAt[k] = (domeAt.TryGetValue(k, out var dc) ? dc : 0) + t; }
                    if (mat && Shared.Contains(mat.name)) { var k = $"{PathOf(r.transform)} | {mat.name}"; sharedAt[k] = (sharedAt.TryGetValue(k, out var sc) ? sc : 0) + t; }
                }
            }
            var groups = new List<object>(); int soilOff = 0, soilOn = 0;
            foreach (var lg in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LODGroup>(true)))
            {
                var lods = lg.GetLODs(); var all = lods.SelectMany(l => l.renderers).Where(x => x).ToArray();
                if (!all.Any(x => x.sharedMaterials.Any(mm => mm && mm.name.EndsWith("_TB", StringComparison.Ordinal)))) continue;
                foreach (var x in all.Where(x => x.sharedMaterials.All(mm => mm && mm.name == "DK_Soil_TB"))) { if (x.shadowCastingMode == ShadowCastingMode.Off) soilOff++; else soilOn++; }
                float rel = lg.size * lg.transform.lossyScale.x / 28f * QualitySettings.lodBias; int pick = -1; for (int i = 0; i < lods.Length; i++) if (rel >= lods[i].screenRelativeTransitionHeight) { pick = i; break; }
                groups.Add($"{PathOf(lg.transform)} | levels {lods.Length} | size {lg.size:0.##} | play zoom L{pick}");
            }
            return new Dictionary<string, object>
            {
                ["renderers"] = renderers, ["trianglesByMaterial"] = byMat.ToDictionary(k => k.Key, k => (object)k.Value), ["domeTriangles"] = dome, ["domeAt"] = domeAt.ToDictionary(k => k.Key, k => (object)k.Value), ["sharedPlantMaterialsAt"] = sharedAt.ToDictionary(k => k.Key, k => (object)k.Value),
                ["testbedGroups"] = groups.Count, ["groups"] = groups, ["soilRenderersShadowOff"] = soilOff, ["soilRenderersShadowOn"] = soilOn,
            };
        }

        public static void Night() => HarbourWorldBuilder.Batch(() =>
        {
            var t0 = DateTime.UtcNow; var dir = Path.Combine(Out, "captures"); Directory.CreateDirectory(dir);
            var scene = EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var cam = (GameObject.Find("cameras/play") ?? throw new InvalidDataException("077: no cameras/play")).GetComponent<Camera>();
            var ts = cam.GetComponent<TacticalShadows>(); var fade = cam.GetComponent<ArchitectureFade>();
            var people = GameObject.Find("people"); if (people) people.SetActive(false);
            var lp = Object.FindAnyObjectByType<Suborned.Play.LightPhase>(FindObjectsInactive.Include) ?? throw new InvalidDataException("077: no LightPhase");
            var globes = lp.lamps.Where(l => l && l.name.StartsWith("globe/", StringComparison.Ordinal)).ToArray();
            var n = new Dictionary<string, object>
            {
                ["scene"] = HarbourWorldBuilder.ScenePath, ["globeLamps"] = globes.Select(l => (object)$"{l.name} at {l.transform.position} {l.type} {l.intensity} range {l.range} shadows {l.shadows}").ToList(),
                ["glows"] = lp.glows.Select(g => (object)$"{(g.material ? g.material.name + " " + AssetDatabase.GetAssetPath(g.material) : "-")} day {g.day} evening {g.evening} night {g.night}").ToList(),
                ["opalRenderers"] = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).Count(r => r.sharedMaterials.Any(m => m && m.name == "DK_Opal_TB")),
            };
            Vector3 focus = Vector3.zero; lp.Focus = () => focus;
            var shots = new List<object>(); var live = new Dictionary<string, object>();
            using (var session = new RenderSession())
            {
                After072.AbPlayPose(cam, ts, fade, Views[0].focus, Views[0].yaw, 160f); session.Grab(cam, 64, 64);
                foreach (var phase in new[] { "day", "night" })
                    foreach (var v in Views)
                    {
                        After072.AbPlayPose(cam, ts, fade, v.focus, v.yaw, 160f); cam.orthographicSize = v.size;
                        focus = new Vector3(v.focus.x, 0f, v.focus.y); lp.Set(phase == "night");
                        session.Grab(cam, 64, 64);
                        var px = session.Grab(cam, 1920, 1080); File.WriteAllBytes(Path.Combine(dir, $"harbour-{v.id}-{phase}.png"), px.EncodeToPNG());
                        shots.Add(new Dictionary<string, object> { ["view"] = v.id, ["phase"] = phase, ["file"] = $"captures/harbour-{v.id}-{phase}.png" });
                        if (phase == "night") live[v.id] = $"{lp.LiveLamps} lamps lit, globes lit {globes.Count(g => g.enabled)}";
                    }
                lp.Set(false);
            }
            n["captures"] = shots; n["liveLamps"] = live; n["seconds"] = Math.Round((DateTime.UtcNow - t0).TotalSeconds, 1);
            File.WriteAllText(Path.Combine(Out, "numbers-night.json"), Json(n));
            Debug.Log($"[077 build] night: {shots.Count} shots, {globes.Length} globe lamps, {lp.glows.Length} glows");
        });

        /// <summary>Diagnostic: a kit piece's generated prefab and its FBX, slot by slot (-plantDiag id).</summary>
        public static void Diag() => HarbourWorldBuilder.Batch(() =>
        {
            var id = DecoKitStage.Arg("plantDiag", "PottedTopiary_Play"); var kit = new KitPlacer(HarbourWorldBuilder.KitCataloguePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var (label, go) in new[] { ("source", kit.Source(id)), ("fbx", AssetDatabase.LoadAssetAtPath<GameObject>(kit.Generated.TryGetValue(id, out var f) ? f : "")) })
                if (go) foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                    Debug.Log($"[077 diag] {label} {r.name}: submeshes {r.GetComponent<MeshFilter>().sharedMesh?.subMeshCount} slots [{string.Join(", ", r.sharedMaterials.Select(m => m ? m.name + "@" + AssetDatabase.GetAssetPath(m) : "NULL"))}]");
            foreach (var n in new[] { "DK_Bark_Fibre", "DK_Chrome", "DK_Enamel_Ivory", "DK_Enamel_Teal", "DK_Leaf_CoreConifer", "DK_Leaf_Cypress", "DK_Leaf_CypressOlive", "DK_Soil" })
                Debug.Log($"[077 diag] {n}: {(AssetDatabase.LoadAssetAtPath<Material>($"Assets/Suborned/Kit/Materials/{n}.mat") ? "loads" : "NULL")} guid {AssetDatabase.AssetPathToGUID($"Assets/Suborned/Kit/Materials/{n}.mat")}");
        });

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
