using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Play;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.People
{
    /// <summary>Brief 020: the stand-in figures (Forma, briefs/020/people/manifest.json; files installed by `bun tools/figures/run.ts --install`
    /// into Assets/Suborned/People, git-ignored) as one FigureLibrary and one AnimatorController with a state per clip, both generated into
    /// Assets/Suborned/People/Generated. PlayInstall bakes the library onto the host; the menu also bakes it onto the open scene's host.
    /// Menu: Suborned/People/Build Figure Library.</summary>
    public static class FigureBuild
    {
        const string Root = "Assets/Suborned/People", Gen = Root + "/Generated";
        public const string LibraryPath = Gen + "/FigureLibrary.asset", ControllerPath = Gen + "/Figures.controller";
        /// <summary>Who walks with walk_f (the manifest carries no gender): the women among the cell, the cast and the crowd, read from the
        /// contact sheets (briefs/020/people/cast.png, crowd.png).</summary>
        static readonly HashSet<string> Women = new() { "scout", "fixer", "waitress", "cafe_w2", "cafe_w3", "cafe_w5", "el1", "d6", "sh2", "rival_d" }; // rival_d: Forma 067 (the crew's woman, on Commuter C6's body)

        [MenuItem("Suborned/People/Build Figure Library")]
        static void Menu()
        {
            var lib = Build();
            var host = Object.FindAnyObjectByType<HandoverHost>();
            if (host && lib) { host.figures = lib; EditorUtility.SetDirty(host); EditorSceneManager.MarkSceneDirty(host.gameObject.scene); EditorSceneManager.SaveScene(host.gameObject.scene); }
            Debug.Log($"People: figure library {(lib ? lib.figures.Length : 0)} figures; host {(host ? "baked" : "none open")}");
        }

        /// <summary>Batchmode (Editor closed): rebuild the library and the controller from the installed manifest, in place (the same asset paths,
        /// so the scene's host keeps its reference), and log the figures and the pools. Unity -batchmode -quit -projectPath unity -executeMethod
        /// Suborned.World.People.FigureBuild.Cli (brief 067: Forma's Velvet Set, 26 figures and the pool role/rival).</summary>
        public static void Cli()
        {
            int code = 1;
            try
            {
                var lib = Build();
                if (lib)
                {
                    code = 0;
                    var pools = new List<string>(); for (int i = 0; i < lib.poolKeys.Length; i++) if (lib.poolKeys[i].StartsWith("role/")) pools.Add($"{lib.poolKeys[i]}={lib.poolIds[i]}");
                    Debug.Log($"People: figure library rebuilt: {lib.figures.Length} figures [{string.Join(" ", lib.figures.Select(f => f.id + (f.female ? "(f)" : "")))}]; {lib.speedStates.Length} clip speeds; pools {string.Join("; ", pools)}");
                }
            }
            catch (System.Exception e) { Debug.LogException(e); }
            EditorApplication.Exit(code);
        }

        /// <summary>The library, built from the installed manifest (null when the figures are not installed: the capsules stay).
        /// Brief 039 B: rebuilt whenever the installed manifest is newer than the controller (new clips, Forma's additive layers), so a player
        /// build never bakes a controller that lacks the installed clips.</summary>
        public static FigureLibrary Library()
        {
            var mpath = Path.Combine(Root, "manifest.json"); if (!File.Exists(mpath)) return null;
            var lib = AssetDatabase.LoadAssetAtPath<FigureLibrary>(LibraryPath);
            bool stale = lib == null || !File.Exists(ControllerPath) || File.GetLastWriteTimeUtc(mpath) > File.GetLastWriteTimeUtc(ControllerPath)
                || File.Exists(CellManifest) && File.GetLastWriteTimeUtc(CellManifest) > File.GetLastWriteTimeUtc(ControllerPath); // brief 112: the cell's own manifest
            if (stale) Debug.Log($"People: the figure library is {(lib == null ? "missing" : "older than the installed manifest")}: rebuilt");
            return stale ? Build() : lib;
        }

        /// <summary>The cell's manifest (brief 112, written by `bun tools/figures/cell_hml.ts`): the four Happy Mouse figures, Generic, each on its own skeleton.</summary>
        public static string CellManifest => Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "112", "people", "manifest-cell.json");
        static string Sha256(string path) { using var sha = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(path); return string.Concat(sha.ComputeHash(f).Select(b => b.ToString("x2"))); }

        /// <summary>The cell (brief 112; Sky 2026-11-12: "replace our main cell with these characters and animations"): Generic figures on their own skeletons, with
        /// their own clips. The shared Humanoid clips cannot move a Generic rig, so each gets an AnimatorOverrideController over the shared controller, every state's
        /// clip replaced by the same-named clip of its own FBX, and its own clip speeds (clipSpeedsMs), LOD heights and props. A clip the shared controller has no
        /// state for (idle_armed, holster, tinker, phone, ...) gets one, on an empty clip: only a figure that lists it in ownStates is ever given it (HostViews.Cell).
        /// A figure whose installed FBX is not the manifest's build (the stand-in is still there) is left as it is, with a warning.</summary>
        static void CellOverrides(FigureLibrary lib, AnimatorController ctl)
        {
            if (!File.Exists(CellManifest)) return;
            var cell = new List<(object row, FigureLibrary.Figure fig, string fbx, Dictionary<string, AnimationClip> own)>();
            foreach (var cf in J.A(Json.Parse(File.ReadAllText(CellManifest)), "figures"))
            {
                string id = J.S(cf, "id"); var fig = lib.figures.FirstOrDefault(x => x.id == id); if (fig == null) continue;
                var fbx = Path.Combine(Root, J.S(cf, "fbx"));
                if (!File.Exists(fbx) || Sha256(fbx) != J.S(cf, "sha256")) { Debug.LogWarning($"People: the cell's {id} is not installed (Figures/{id} holds another build): the stand-in stays. Run: bun tools/figures/cell_hml.ts --install"); continue; }
                var own = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).GroupBy(c => c.name).ToDictionary(g => g.Key, g => g.First());
                cell.Add((cf, fig, fbx, own));
            }
            if (cell.Count == 0) return;
            var sm = ctl.layers[0].stateMachine; var have = new HashSet<string>(sm.states.Select(x => x.state.name));
            foreach (var name in cell.SelectMany(c => c.own.Keys).Distinct().OrderBy(x => x).Where(x => !have.Contains(x)))
            {
                var empty = new AnimationClip { name = "cell_" + name }; AssetDatabase.AddObjectToAsset(empty, ctl);
                var st = sm.AddState(name); st.motion = empty; st.writeDefaultValues = true;
            }
            var states = sm.states.Select(x => x.state).Where(x => x.motion is AnimationClip).ToList();
            var log = new List<string>();
            foreach (var (cf, fig, fbx, own) in cell)
            {
                var ov = new AnimatorOverrideController(ctl) { name = "Figures_" + fig.id };
                var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(); ov.GetOverrides(pairs);
                var byState = new Dictionary<AnimationClip, AnimationClip>(); var mine = new List<string>();
                foreach (var st in states) if (own.TryGetValue(st.name, out var oc) && st.motion is AnimationClip bc && !byState.ContainsKey(bc)) { byState[bc] = oc; mine.Add(st.name); }
                ov.ApplyOverrides(pairs.Select(kv => new KeyValuePair<AnimationClip, AnimationClip>(kv.Key, byState.TryGetValue(kv.Key, out var o) ? o : null)).ToList());
                var ovPath = Gen + "/Figures_" + fig.id + ".overrideController"; AssetDatabase.DeleteAsset(ovPath); AssetDatabase.CreateAsset(ov, ovPath);
                fig.controller = ov; fig.ownStates = mine.ToArray();
                if (AssetDatabase.LoadAssetAtPath<GameObject>(fbx) is GameObject model) fig.model = model;
                if (J.Get(cf, "female") is bool fem) fig.female = fem;
                if (J.Get(cf, "lodScreenHeights") != null) fig.lodHeights = J.A(cf, "lodScreenHeights").Select(x => (float)(double)x).ToArray();
                var sp = J.Get(cf, "clipSpeedsMs") as Dictionary<string, object>;
                fig.speedStates = sp?.Keys.ToArray() ?? new string[0]; fig.speeds = sp?.Values.Select(v => (float)(double)v).ToArray() ?? new float[0];
                var nodes = new List<string>(); var shown = new List<string>();
                if (J.Get(cf, "props") != null)
                    foreach (var p in J.A(cf, "props")) { nodes.Add(J.S(p, "node")); shown.Add(J.Get(p, "always") is bool al && al ? "*" : string.Join(",", J.A(p, "states").Select(x => (string)x))); }
                fig.propNodes = nodes.ToArray(); fig.propStates = shown.ToArray();
                log.Add($"{fig.id} {mine.Count}/{states.Count} states own, props [{string.Join(",", nodes)}]");
            }
            Debug.Log("People: the cell's overrides: " + string.Join("; ", log));
        }

        /// <summary>Brief 039 B (Forma's install note, briefs/039/forma/pin-poses.md §D "Host"): two additive layers, no mask. "react" (weight 1):
        /// an empty default state plus flinch_add and kick_add, each back to empty at its end with no blend; the host restarts one per event
        /// (Play(state, react, 0)). "peek" (its weight driven by the host every frame): pin_peek_add looping. States on these layers don't write
        /// defaults (an additive layer adds only its clip). A clip missing from the manifest leaves its state out; the layers stay.</summary>
        static void AdditiveLayers(AnimatorController ctl, Dictionary<string, AnimationClip> clips)
        {
            ctl.AddLayer("react"); ctl.AddLayer("peek");
            var layers = ctl.layers;
            for (int i = 1; i < layers.Length; i++) { layers[i].blendingMode = AnimatorLayerBlendingMode.Additive; layers[i].defaultWeight = layers[i].name == "react" ? 1f : 0f; }
            ctl.layers = layers;
            var react = ctl.layers[1].stateMachine; var empty = react.AddState("empty"); empty.writeDefaultValues = false; react.defaultState = empty;
            foreach (var n in new[] { "flinch_add", "kick_add" })
            {
                if (!clips.TryGetValue(n, out var clip)) continue;
                var st = react.AddState(n); st.motion = clip; st.writeDefaultValues = false;
                var tr = st.AddTransition(empty); tr.hasExitTime = true; tr.exitTime = 1f; tr.duration = 0f; tr.hasFixedDuration = true;
            }
            var peek = ctl.layers[2].stateMachine;
            if (clips.TryGetValue("pin_peek_add", out var pc)) { var st = peek.AddState("pin_peek_add"); st.motion = pc; st.writeDefaultValues = false; peek.defaultState = st; }
            else { var e2 = peek.AddState("empty"); e2.writeDefaultValues = false; peek.defaultState = e2; }
        }

        /// <summary>Brief 069 A (Redline: about 15 % of a person's frame cost on today's figures): every installed figure imports with Optimize Game
        /// Objects on, so its skeleton lives inside the Animator and only the bones the play scene reads stay as transforms. The rule, from the code
        /// that reads bones (HostViews.Bone): Head and Hips on everyone (the attention cue and look-at read Head; a carried body, which can be anyone,
        /// is seated by its Hips); RightShoulder on the cell (playRoles op/*: only an agent carries, HostViews.cs carriers); the tray's bone on the
        /// figures that serve (manifest props[tray].attach.bone and perFigure). The paths are read from the model's own hierarchy (unoptimised first
        /// when they change), so nothing is written per figure. Skipped when the meta already exposes exactly these names.</summary>
        static void Optimise(object m)
        {
            var always = new[] { "Head", "Hips" };
            var carriers = new HashSet<string>();
            if (J.Get(m, "playRoles") is Dictionary<string, object> pr)
                foreach (var kv in pr) if (kv.Key.StartsWith("op/") && kv.Value is List<object> ids) foreach (var x in ids) carriers.Add((string)x);
            var hands = new Dictionary<string, string>();
            if (J.Get(m, "props") != null)
                foreach (var p in J.A(m, "props"))
                    if (J.Get(p, "attach") is { } at && J.S(at, "bone") is string bone && J.Get(at, "perFigure") is Dictionary<string, object> pf)
                        foreach (var id in pf.Keys) hands[id] = bone;
            var log = new List<string>();
            foreach (var f in J.A(m, "figures"))
            {
                string id = J.S(f, "id"), path = Path.Combine(Root, J.S(f, "fbx"));
                if (AssetImporter.GetAtPath(path) is not ModelImporter imp) continue;
                var want = always.Concat(carriers.Contains(id) ? new[] { "RightShoulder" } : new string[0]).Concat(hands.TryGetValue(id, out var hb) ? new[] { hb } : new string[0]).Distinct().OrderBy(x => x).ToArray();
                var have = imp.extraExposedTransformPaths.Select(x => x.Split('/').Last()).OrderBy(x => x).ToArray();
                if (imp.optimizeGameObjects && have.SequenceEqual(want)) continue;
                if (imp.optimizeGameObjects) { imp.optimizeGameObjects = false; imp.extraExposedTransformPaths = new string[0]; imp.SaveAndReimport(); }
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var paths = model.GetComponentsInChildren<Transform>(true).Where(t => t != model.transform && want.Contains(t.name)).GroupBy(t => t.name).Select(g => PathOf(g.First(), model.transform)).ToArray();
                imp.optimizeGameObjects = true; imp.extraExposedTransformPaths = paths; imp.SaveAndReimport();
                log.Add($"{id} [{string.Join(",", paths.Select(x => x.Split('/').Last()))}]");
            }
            if (log.Count > 0) Debug.Log($"People: Optimize Game Objects on {log.Count} figures: " + string.Join("; ", log));
        }
        static string PathOf(Transform t, Transform root) { var s = t.name; while (t.parent && t.parent != root) { t = t.parent; s = t.name + "/" + s; } return s; }

        public static FigureLibrary Build()
        {
            var mpath = Path.Combine(Root, "manifest.json"); if (!File.Exists(mpath)) { Debug.LogWarning("People: no installed figures (bun tools/figures/run.ts --install)"); return null; }
            var m = Json.Parse(File.ReadAllText(mpath));
            if (!AssetDatabase.IsValidFolder(Gen)) AssetDatabase.CreateFolder(Root, "Generated");
            Optimise(m);
            // one controller: a state per clip, named as the clip; idle first (the default)
            AssetDatabase.DeleteAsset(ControllerPath);
            var ctl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var sm = ctl.layers[0].stateMachine; var speedStates = new List<string>(); var speeds = new List<float>();
            var additive = new Dictionary<string, AnimationClip>();
            foreach (var c in J.A(m, "clips").OrderBy(c => J.S(c, "name") == "idle" ? 0 : 1))
            {
                string name = J.S(c, "name"), file = Path.Combine(Root, J.S(c, "file"));
                var clip = AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__preview__"));
                if (clip == null) throw new InvalidDataException($"People: no clip in {file}");
                if (J.Get(c, "additive") is bool ad && ad) { additive[name] = clip; continue; } // brief 039 B (Forma): additive rows stay off the base layer
                var st = sm.AddState(name); st.motion = clip; st.writeDefaultValues = true;
                if (J.Get(c, "naturalSpeedMs") is double ns) { speedStates.Add(name); speeds.Add((float)ns); }
            }
            sm.defaultState = sm.states.First(s => s.state.name == "idle").state;
            AdditiveLayers(ctl, additive);
            // the figures, their LOD heights and the pools
            var lib = AssetDatabase.LoadAssetAtPath<FigureLibrary>(LibraryPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<FigureLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }
            lib.controller = ctl; lib.speedStates = speedStates.ToArray(); lib.speeds = speeds.ToArray();
            lib.figures = J.A(m, "figures").Select(f =>
            {
                string id = J.S(f, "id"), fbx = Path.Combine(Root, J.S(f, "fbx"));
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx) ?? throw new InvalidDataException($"People: no model {fbx}");
                return new FigureLibrary.Figure { id = id, model = model, female = Women.Contains(id), lodHeights = J.A(f, "lodScreenHeights").Select(x => (float)(double)x).ToArray() };
            }).ToArray();
            var keys = new List<string>(); var ids = new List<string>();
            foreach (var kv in J.O(J.Get(m, "families"))) { keys.Add("family/" + kv.Key.Split(' ')[0]); ids.Add(string.Join(",", J.A(kv.Value).Select(x => (string)x))); }
            foreach (var kv in J.O(J.Get(m, "jobBlocks"))) { keys.Add(kv.Key); ids.Add(string.Join(",", J.A(kv.Value).Select(x => (string)x))); }
            lib.poolKeys = keys.ToArray(); lib.poolIds = ids.ToArray();
            CellOverrides(lib, ctl);
            // Forma round 2: the serving tray and its seat per figure (props[id == tray].attach.perFigure)
            lib.tray = null; var tf = new List<string>(); var tp = new List<Vector3>(); var te = new List<Vector3>();
            if (J.Get(m, "props") != null)
                foreach (var pr in J.A(m, "props"))
                {
                    if (J.S(pr, "id") != "tray") continue;
                    lib.tray = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(Root, J.S(pr, "fbx")));
                    Vector3 V(object a) { var l = J.A(a).Select(x => (float)(double)x).ToArray(); return new Vector3(l[0], l[1], l[2]); }
                    foreach (var kv in J.O(J.Get(J.Get(pr, "attach"), "perFigure"))) { tf.Add(kv.Key); tp.Add(V(J.Get(kv.Value, "localPosition"))); te.Add(V(J.Get(kv.Value, "localEuler"))); }
                }
            lib.trayFigures = tf.ToArray(); lib.trayPos = tp.ToArray(); lib.trayEuler = te.ToArray();
            EditorUtility.SetDirty(lib); AssetDatabase.SaveAssets();
            return lib;
        }
    }
}
