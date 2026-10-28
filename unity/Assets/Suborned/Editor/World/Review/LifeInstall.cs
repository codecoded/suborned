using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Suborned.Play;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Debug = UnityEngine.Debug;

namespace Suborned.World.Review
{
    /// <summary>Brief 075 golden (Latch, 2026-10-28): builds the presentation life (PresentationLife) from Pulse's
    /// out/world/harbour-district.presentation.json (suborned.presentation/1) over the open HarbourDistrict scene, for the flyby (Flyby074 -life on),
    /// the golden harness (Golden075 -life t) and the free-fly cameras only; never in play, never in the Handover, never saved.
    ///   models: adopted kit pieces by their Built prefab (KitPlacer, kit.json as it is now, so a piece Vela adopts later just appears); a candidate
    ///   falls back along its `fallback` (the box truck draws as the official sedan) or is skipped (scooter, sky rail, aircars until adopted).
    ///   figures: the 10 townspeople from the figure library (FigureBuild.Library), each clip the set uses (walk, idle, idle_alt, talk, phone,
    ///   look_around, sit) sampled through a PlayableGraph and baked per frame and LOD (SkinnedMeshRenderer.BakeMesh), shared by every instance.
    ///   the world's `dressing` layer (the two parked trams) is hidden while the life lives: the live trams run on that track.</summary>
    [InitializeOnLoad]
    public static class LifeInstall
    {
        public static string DefaultPath => Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "world", "harbour-district.presentation.json");
        const int WalkFrames = 24, MinFrames = 12, MaxFrames = 32; const float FramesPerSecond = 8f;

        static LifeInstall() => PresentationLife.Installer = () =>
        {
            if (!File.Exists(DefaultPath)) { Debug.LogWarning($"life: {DefaultPath} missing (bun run life:presentation); free-fly without life"); return null; }
            try { return Install(DefaultPath); } catch (Exception e) { Debug.LogException(e); return null; }
        };

        static float F(object o) => o is double d ? (float)d : o is long l ? l : 0f;
        static float[] Floats(object o) { var l = (List<object>)o; var a = new float[l.Count]; for (int i = 0; i < a.Length; i++) a[i] = F(l[i]); return a; }

        public static PresentationLife Install(string path = null, Camera view = null)
        {
            path ??= DefaultPath;
            if (!File.Exists(path)) throw new FileNotFoundException($"life: {path} missing (run `bun run world` then `bun run life:presentation`)");
            var clock = Stopwatch.StartNew();
            var root = Json.Parse(File.ReadAllText(path));
            if (J.S(root, "schema") != "suborned.presentation/1") throw new InvalidDataException($"life: {path}: schema {J.S(root, "schema")}, want suborned.presentation/1");
            foreach (var old in UnityEngine.Object.FindObjectsByType<PresentationLife>(FindObjectsInactive.Include)) UnityEngine.Object.DestroyImmediate(old.gameObject);

            var go = new GameObject("presentation-life") { hideFlags = HideFlags.DontSave };
            var life = go.AddComponent<PresentationLife>(); life.View = view;
            life.Period = J.F(J.Get(root, "clock"), "periodSec");
            life.Routes = J.A(root, "routes").Select(r => { var xy = Floats(J.Get(r, "xy")); return new PresentationLife.Route { xy = xy, t = Floats(J.Get(r, "t")), z = J.F(r, "z"), cum = PresentationLife.RouteCum(xy) }; }).ToArray();
            life.Loops = J.A(root, "loops").Select(l => { var xyz = Floats(J.Get(l, "xyz")); var c = PresentationLife.LoopCum(xyz); return new PresentationLife.Loop { xyz = xyz, cum = c, len = c[^1] }; }).ToArray();

            // which clips each figure needs
            var needs = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            void Need(string model, string clip) { if (!model.StartsWith("figure/")) return; if (!needs.TryGetValue(model, out var s)) needs[model] = s = new SortedSet<string>(StringComparer.Ordinal) { "idle" }; s.Add(clip); }
            foreach (var m in J.A(root, "looping")) Need(J.S(m, "model"), J.S(m, "clip"));
            foreach (var m in J.A(root, "still")) Need(J.S(m, "model"), J.S(m, "clip"));

            var placer = new KitPlacer(HarbourWorldBuilder.KitCataloguePath);
            var lib = People.FigureBuild.Library();
            var defs = (Dictionary<string, object>)J.Get(root, "models");
            var models = new Dictionary<string, PresentationLife.Model>(StringComparer.Ordinal);
            var report = new List<string>();
            PresentationLife.Model Resolve(string key, int depth = 0)
            {
                if (models.TryGetValue(key, out var have)) return have;
                if (depth > 4 || !defs.TryGetValue(key, out var d)) return models[key] = null;
                PresentationLife.Model m = null;
                if (J.S(d, "kind") == "figure")
                {
                    var e = lib ? lib.Find(J.S(d, "figure")) : null;
                    if (e?.model) m = new PresentationLife.Model { id = key, figure = Bake(e, lib, needs.TryGetValue(key, out var s) ? s : new SortedSet<string> { "idle" }) };
                    report.Add($"{key} {(m != null ? $"baked {m.figure.lods.Length} LODs, clips {string.Join("/", m.figure.clips)}" : "MISSING from the figure library: skipped")}");
                }
                else
                {
                    var kit = J.S(d, "kit");
                    if (kit != null && placer.Prefabs.ContainsKey(kit))
                    {
                        var po = J.Get(d, "pivotOffset") is List<object> pl && pl.Count > 1 ? F(pl[1]) : 0f;
                        m = new PresentationLife.Model { id = key, prefab = placer.Prefab(kit), pivotOffset = po };
                        report.Add($"{key} kit {kit}");
                    }
                    else if (J.S(d, "fallback") is string fb)
                    {
                        var f = Resolve(fb, depth + 1);
                        m = f; report.Add($"{key} kit {kit} not adopted: drawn as {fb}{(f == null ? " (also missing: skipped)" : "")}");
                    }
                    else report.Add($"{key} kit {kit} not adopted, no fallback: skipped");
                }
                return models[key] = m;
            }

            int skipped = 0;
            foreach (var m in J.A(root, "timed"))
            {
                var model = Resolve(J.S(m, "model"));
                (PresentationLife.Model, float, bool)[] consist = null;
                if (J.Get(m, "consist") is List<object> cs) consist = cs.Select(c => (Resolve(J.S(c, "model")), J.F(c, "back"), J.Get(c, "flip") is true)).ToArray();
                if (model == null) { skipped++; continue; }
                life.AddTimed(model, J.S(m, "id"), (int)J.F(m, "route"), J.F(m, "t0"), consist);
            }
            foreach (var m in J.A(root, "looping"))
            {
                var model = Resolve(J.S(m, "model")); if (model == null) { skipped++; continue; }
                life.AddLooping(model, J.S(m, "id"), (int)J.F(m, "loop"), J.F(m, "s0"), J.F(m, "v"), J.F(m, "lat"), J.Get(m, "bob") is double b ? (float)b : 0f, J.S(m, "clip"));
            }
            foreach (var m in J.A(root, "still"))
            {
                var model = Resolve(J.S(m, "model")); if (model == null) { skipped++; continue; }
                var at = Floats(J.Get(m, "at"));
                life.AddStill(model, J.S(m, "id"), new Vector2(at[0], at[1]), J.F(m, "z"), J.F(m, "heading"), J.S(m, "clip"));
            }
            // the parked dressing trams (TramStreet.DressingLayer under the world root) stand on the live trams' track
            int hid = 0;
            var world = GameObject.Find(HarbourWorldBuilder.RootName);
            if (world) foreach (var t in world.GetComponentsInChildren<Transform>(true)) if (t.name == TramStreet.DressingLayer && t.parent == world.transform) { life.Hide(t.gameObject); hid += t.childCount; }
            Debug.Log($"life: {path}: {life.Count} placed ({life.Figures} figures), {skipped} skipped (models not adopted), {hid} dressing trams hidden, period {life.Period} s, in {clock.ElapsedMilliseconds} ms\n  " + string.Join("\n  ", report));
            return life;
        }

        /// <summary>The figure's clips baked: each clip of `clips` the controller has, sampled at its frames, every LOD's skinned renderers baked.</summary>
        static PresentationLife.Baked Bake(FigureLibrary.Figure e, FigureLibrary lib, SortedSet<string> clips)
        {
            var inst = UnityEngine.Object.Instantiate(e.model); inst.hideFlags = HideFlags.HideAndDontSave;
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            try
            {
                var anim = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>(); anim.applyRootMotion = false;
                var byState = States(lib.ControllerFor(e));
                // LOD levels: the LODGroup's renderers per level (first and last), else every skinned renderer as one level
                var levels = new List<SkinnedMeshRenderer[]>();
                if (inst.GetComponentInChildren<LODGroup>() is LODGroup lg)
                {
                    var lods = lg.GetLODs(); var pick = lods.Length > 1 ? new[] { 0, lods.Length - 1 } : new[] { 0 };
                    foreach (var i in pick) levels.Add(lods[i].renderers.OfType<SkinnedMeshRenderer>().ToArray());
                }
                if (levels.Count == 0 || levels.Any(l => l.Length == 0)) levels = new List<SkinnedMeshRenderer[]> { inst.GetComponentsInChildren<SkinnedMeshRenderer>(true) };
                var b = new PresentationLife.Baked
                {
                    lodHeights = levels.Count > 1 && e.lodHeights.Length > 0 ? new[] { e.lodHeights[0], e.lodHeights[^1] } : Array.Empty<float>(),
                };
                var names = new List<string>(); var starts = new List<int>(); var counts = new List<int>(); var lens = new List<float>();
                var frames = levels.Select(l => l.Select(_ => new List<Mesh>()).ToArray()).ToArray();
                var graph = PlayableGraph.Create("life-bake"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                try
                {
                    var output = AnimationPlayableOutput.Create(graph, "pose", anim);
                    int total = 0;
                    foreach (var name in clips)
                    {
                        if (!byState.TryGetValue(name, out var clip) || !clip) continue;
                        var cp = AnimationClipPlayable.Create(graph, clip); cp.SetApplyFootIK(false); output.SetSourcePlayable(cp);
                        int n = name == "walk" ? WalkFrames : Mathf.Clamp(Mathf.RoundToInt(clip.length * FramesPerSecond), MinFrames, MaxFrames);
                        for (int f = 0; f < n; f++)
                        {
                            cp.SetTime(clip.length * f / n); graph.Evaluate(0f);
                            for (int l = 0; l < levels.Count; l++)
                                for (int p = 0; p < levels[l].Length; p++)
                                {
                                    var m = new Mesh { name = $"{e.id}-{name}-{f}-lod{l}-{p}", hideFlags = HideFlags.DontSave };
                                    levels[l][p].BakeMesh(m, true); m.UploadMeshData(true); frames[l][p].Add(m);
                                }
                        }
                        cp.Destroy();
                        names.Add(name); starts.Add(total); counts.Add(n); lens.Add(clip.length); total += n;
                    }
                }
                finally { graph.Destroy(); }
                if (names.Count == 0) throw new InvalidDataException($"life: figure {e.id}: none of {string.Join(", ", clips)} in its controller");
                b.clips = names.ToArray(); b.start = starts.ToArray(); b.frames = counts.ToArray(); b.length = lens.ToArray();
                b.lods = levels.Select((l, li) => l.Select((r, p) => new PresentationLife.Baked.Part
                {
                    frames = frames[li][p].ToArray(), mats = r.sharedMaterials,
                    pos = r.transform.position, rot = r.transform.rotation, // baked with scale, in the renderer's frame: place by its position and turn
                }).ToArray()).ToArray();
                return b;
            }
            finally { UnityEngine.Object.DestroyImmediate(inst); }
        }

        /// <summary>State name → clip, through an override controller to its clips.</summary>
        static Dictionary<string, AnimationClip> States(RuntimeAnimatorController rc)
        {
            var map = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            var ov = rc as AnimatorOverrideController;
            var ctl = (ov ? ov.runtimeAnimatorController : rc) as AnimatorController;
            if (!ctl) return map;
            foreach (var s in ctl.layers[0].stateMachine.states)
                if (s.state.motion is AnimationClip c) map[s.state.name] = ov && ov[c] ? ov[c] : c;
            return map;
        }
    }
}
