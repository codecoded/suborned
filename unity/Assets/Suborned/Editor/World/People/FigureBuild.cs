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
        static readonly HashSet<string> Women = new() { "scout", "fixer", "waitress", "cafe_w2", "cafe_w3", "cafe_w5", "el1", "d6", "sh2" };

        [MenuItem("Suborned/People/Build Figure Library")]
        static void Menu()
        {
            var lib = Build();
            var host = Object.FindAnyObjectByType<HandoverHost>();
            if (host && lib) { host.figures = lib; EditorUtility.SetDirty(host); EditorSceneManager.MarkSceneDirty(host.gameObject.scene); EditorSceneManager.SaveScene(host.gameObject.scene); }
            Debug.Log($"People: figure library {(lib ? lib.figures.Length : 0)} figures; host {(host ? "baked" : "none open")}");
        }

        /// <summary>The library, built from the installed manifest (null when the figures are not installed: the capsules stay).</summary>
        public static FigureLibrary Library() => File.Exists(Path.Combine(Root, "manifest.json")) ? AssetDatabase.LoadAssetAtPath<FigureLibrary>(LibraryPath) ?? Build() : null;

        public static FigureLibrary Build()
        {
            var mpath = Path.Combine(Root, "manifest.json"); if (!File.Exists(mpath)) { Debug.LogWarning("People: no installed figures (bun tools/figures/run.ts --install)"); return null; }
            var m = Json.Parse(File.ReadAllText(mpath));
            if (!AssetDatabase.IsValidFolder(Gen)) AssetDatabase.CreateFolder(Root, "Generated");
            // one controller: a state per clip, named as the clip; idle first (the default)
            AssetDatabase.DeleteAsset(ControllerPath);
            var ctl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var sm = ctl.layers[0].stateMachine; var speedStates = new List<string>(); var speeds = new List<float>();
            foreach (var c in J.A(m, "clips").OrderBy(c => J.S(c, "name") == "idle" ? 0 : 1))
            {
                string name = J.S(c, "name"), file = Path.Combine(Root, J.S(c, "file"));
                var clip = AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__preview__"));
                if (clip == null) throw new InvalidDataException($"People: no clip in {file}");
                var st = sm.AddState(name); st.motion = clip; st.writeDefaultValues = true;
                if (J.Get(c, "naturalSpeedMs") is double ns) { speedStates.Add(name); speeds.Add((float)ns); }
            }
            sm.defaultState = sm.states.First(s => s.state.name == "idle").state;
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
