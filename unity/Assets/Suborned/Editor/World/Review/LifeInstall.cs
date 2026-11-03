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
            // brief 075 roll-out: Pulse's far tiers (`lod`), read from the file so he can tune them by rerun
            if (J.Get(root, "lod") is { } lodSpec)
            {
                float From(string list, string draw) { foreach (var x in J.A(lodSpec, list)) if (J.S(x, "draw") == draw) return J.F(x, "from"); throw new InvalidDataException($"life: lod.{list} has no `{draw}`"); }
                life.FigureTiers = new[] { From("figures", "library"), From("figures", "last-lod-slow"), From("figures", "far-card") };
                life.CarTiers = new[] { From("cars", "kit-lods"), From("cars", "far-box") };
                life.FarBox = FarBoxMesh(); life.FarCard = FarCardMesh();
            }
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
                    if (e?.model) { m = new PresentationLife.Model { id = key, figure = Bake(e, lib, needs.TryGetValue(key, out var s) ? s : new SortedSet<string> { "idle" }) }; if (life.FarCard) m.figure.cardMats = CardMats(key, m.figure); }
                    report.Add($"{key} {(m != null ? $"baked {m.figure.lods.Length} LODs, clips {string.Join("/", m.figure.clips)}" : "MISSING from the figure library: skipped")}");
                }
                else
                {
                    var kit = J.S(d, "kit");
                    if (kit != null && placer.Prefabs.ContainsKey(kit))
                    {
                        var po = J.Get(d, "pivotOffset") is List<object> pl && pl.Count > 1 ? F(pl[1]) : 0f;
                        m = new PresentationLife.Model { id = key, prefab = placer.Prefab(kit), pivotOffset = po };
                        if (J.S(d, "kind") is "car" or "van")
                        {   // brief 075 roll-out: paint slots (Pulse's paintSlots; Vela's carPaints slots) and the far box's frame and default look
                            m.car = true;
                            if (J.Get(d, "paintSlots") is { } ps) { m.paintBody = J.S(ps, "body"); m.paintRoof = J.S(ps, "roof"); }
                            m.box = Lod0Bounds(m.prefab, out var bodyMat);
                            m.boxMats = new[] { bodyMat, bodyMat };
                        }
                        report.Add($"{key} kit {kit}{(m.paintBody != null ? $" (paints {m.paintBody} / {m.paintRoof})" : "")}");
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

            // brief 075 roll-out: car paints (Pulse's `paints`, from Vela's kit.json carPaints): the key's materials where named, else a copy of the
            // slot's own material with only _BaseColor moved (one shared copy per slot and key)
            var paintDefs = J.Get(root, "paints") as Dictionary<string, object> ?? new Dictionary<string, object>();
            var paintCache = new Dictionary<string, PresentationLife.Paint>(StringComparer.Ordinal);
            var copies = new Dictionary<string, Material>(StringComparer.Ordinal);
            int painted = 0, paintCopies = 0, paintLib = 0; var paintMiss = new SortedSet<string>(StringComparer.Ordinal);
            PresentationLife.Paint PaintFor(PresentationLife.Model model, object entry)
            {
                if (model == null || !model.car || J.S(entry, "paint") is not string key) return null;
                if (!paintDefs.TryGetValue(key, out var pd)) { paintMiss.Add(key); return null; }
                var ck = model.id + "|" + key;
                if (paintCache.TryGetValue(ck, out var have)) return have;
                Material Slot(string part, string slotName)
                {
                    var named = J.Get(pd, "materials") is { } mats ? J.S(mats, part) : null;
                    if (named != null && KitImport.Library(named) is Material lib) { paintLib++; return lib; }
                    if (slotName == null) return null;
                    var hex = J.Get(pd, "hex") is { } hx ? J.S(hx, part) : null;
                    var src = SlotMaterial(model.prefab, slotName);
                    if (!src || hex == null || !ColorUtility.TryParseHtmlString(hex, out var c)) return null;
                    var k2 = slotName + "|" + key;
                    if (!copies.TryGetValue(k2, out var cp))
                    {
                        cp = new Material(src) { name = $"{src.name}~{key}", hideFlags = HideFlags.DontSave };
                        if (cp.HasProperty("_BaseColor")) cp.SetColor("_BaseColor", c); else cp.color = c;
                        copies[k2] = cp; paintCopies++;
                    }
                    return cp;
                }
                var p = new PresentationLife.Paint { key = key, body = Slot("body", model.paintBody), roof = Slot("roof", model.paintRoof) };
                return paintCache[ck] = p;
            }

            int skipped = 0;
            foreach (var m in J.A(root, "timed"))
            {
                var model = Resolve(J.S(m, "model"));
                (PresentationLife.Model, float, bool)[] consist = null;
                if (J.Get(m, "consist") is List<object> cs) consist = cs.Select(c => (Resolve(J.S(c, "model")), J.F(c, "back"), J.Get(c, "flip") is true)).ToArray();
                if (model == null) { skipped++; continue; }
                var pt = PaintFor(model, m); if (pt != null) painted++;
                life.AddTimed(model, J.S(m, "id"), (int)J.F(m, "route"), J.F(m, "t0"), consist, pt);
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
                var pt = PaintFor(model, m); if (pt != null) painted++;
                life.AddStill(model, J.S(m, "id"), new Vector2(at[0], at[1]), J.F(m, "z"), J.F(m, "heading"), J.S(m, "clip"), pt);
            }
            // the parked dressing trams (TramStreet.DressingLayer under the world root) stand on the live trams' track
            int hid = 0;
            var world = GameObject.Find(HarbourWorldBuilder.RootName);
            if (world) foreach (var t in world.GetComponentsInChildren<Transform>(true)) if (t.name == TramStreet.DressingLayer && t.parent == world.transform) { life.Hide(t.gameObject); hid += t.childCount; }
            Debug.Log($"life: {path}: {life.Count} placed ({life.Figures} figures), {skipped} skipped (models not adopted), {hid} dressing trams hidden, period {life.Period} s, in {clock.ElapsedMilliseconds} ms\n  " + string.Join("\n  ", report));
            Debug.Log($"life: paints: {painted} cars painted over {paintCache.Count} model x key pairs ({paintLib} slots from kit materials, {paintCopies} slot copies by hex){(paintMiss.Count > 0 ? $"; keys missing from `paints`: {string.Join(", ", paintMiss)}" : "")}; " +
                      (life.FigureTiers.Length >= 3 ? $"far tiers figures library > {life.FigureTiers[0]} > slow > {life.FigureTiers[1]} > card > {life.FigureTiers[2]}, cars kit > {life.CarTiers[0]} > box > {life.CarTiers[1]}" : "no `lod` in the file: the library's heights only"));
            return life;
        }

        /// <summary>The prefab's LOD0 bounds in its own frame (the far box) and its biggest renderer's first material (an unpainted car's box).</summary>
        static Bounds Lod0Bounds(GameObject prefab, out Material main)
        {
            var root = prefab.transform; Renderer[] rs = root.GetComponentInChildren<LODGroup>() is LODGroup lg && lg.GetLODs().Length > 0 ? lg.GetLODs()[0].renderers : root.GetComponentsInChildren<Renderer>(true);
            Bounds? b = null; main = null; float most = -1f;
            foreach (var r in rs)
            {
                if (!r || r.GetComponent<MeshFilter>() is not MeshFilter mf || !mf.sharedMesh) continue;
                var m = root.worldToLocalMatrix * r.transform.localToWorldMatrix; var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) * 2 - 1, (i & 2) - 1, ((i & 4) >> 1) - 1)));
                    if (b is Bounds have) { have.Encapsulate(c); b = have; } else b = new Bounds(c, Vector3.zero);
                }
                float vol = mb.size.x * mb.size.y * mb.size.z; if (vol > most && r.sharedMaterial) { most = vol; main = r.sharedMaterial; }
            }
            return b ?? new Bounds(new Vector3(0f, 0.65f, 0f), new Vector3(1.8f, 1.3f, 4.7f));
        }

        /// <summary>The prefab's material of a slot name (the library material the importer remapped it to).</summary>
        static Material SlotMaterial(GameObject prefab, string name)
        {
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials) if (m && m.name == name) return m;
            return null;
        }

        /// <summary>A unit box about its centre: sides and bottom submesh 0 (10 tris), top submesh 1 (2 tris), faces outward.</summary>
        static Mesh FarBoxMesh()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var side = new List<int>(); var top = new List<int>();
            foreach (var axis in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                var u = Mathf.Abs(axis.y) > 0.5f ? Vector3.right : Vector3.up; var w = Vector3.Cross(axis, u);
                int b = v.Count; var c = axis * 0.5f;
                foreach (var (a, bb) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) }) { v.Add(c + (u * a + w * bb) * 0.5f); n.Add(axis); }
                var list = axis == Vector3.up ? top : side;
                bool cw = Vector3.Dot(Vector3.Cross(v[b + 1] - v[b], v[b + 2] - v[b]), axis) > 0f;
                if (cw) list.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 }); else list.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }
            var m = new Mesh { name = "life-far-box", hideFlags = HideFlags.DontSave, subMeshCount = 2 };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(side, 0); m.SetTriangles(top, 1); m.RecalculateBounds(); return m;
        }

        /// <summary>The far card: a 1.75 m figure as two quads facing +Z, coat (0 to 1.45 m, 0.5 wide) submesh 0 and head (1.45 to 1.75, 0.3 wide) submesh 1.</summary>
        static Mesh FarCardMesh()
        {
            var v = new List<Vector3> { new(-0.25f, 0f, 0f), new(0.25f, 0f, 0f), new(0.25f, 1.45f, 0f), new(-0.25f, 1.45f, 0f), new(-0.15f, 1.45f, 0f), new(0.15f, 1.45f, 0f), new(0.15f, 1.75f, 0f), new(-0.15f, 1.75f, 0f) };
            var m = new Mesh { name = "life-far-card", hideFlags = HideFlags.DontSave, subMeshCount = 2 };
            m.SetVertices(v); m.SetNormals(Enumerable.Repeat(Vector3.forward, 8).ToList());
            // front face +Z: cross(b - a, c - a) along +Z
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0); m.SetTriangles(new[] { 4, 6, 5, 4, 7, 6 }, 1);
            if (Vector3.Dot(Vector3.Cross(v[2] - v[0], v[1] - v[0]), Vector3.forward) < 0f) { m.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0); m.SetTriangles(new[] { 4, 5, 6, 4, 6, 7 }, 1); }
            m.RecalculateBounds(); return m;
        }

        static Material cardBase;
        /// <summary>The far card's two materials for a figure: its coat (the mean colour of its last LOD's biggest part's texture, times the material's
        /// colour) and a skin tone head. Plain lit, instanced, no shadows.</summary>
        static Material[] CardMats(string key, PresentationLife.Baked fb)
        {
            if (!cardBase) { var sh = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit"); cardBase = new Material(sh) { hideFlags = HideFlags.DontSave, enableInstancing = true }; }
            var last = fb.lods[^1]; Material src = null; int most = -1;
            foreach (var p in last) if (p.frames.Length > 0 && p.frames[0] && p.mats.Length > 0 && p.frames[0].vertexCount > most) { most = p.frames[0].vertexCount; src = p.mats[0]; }
            var coat = src ? Mean(src) : new Color(0.6f, 0.55f, 0.5f);
            var a = new Material(cardBase) { name = $"life-card-coat~{key}", hideFlags = HideFlags.DontSave }; a.SetColor("_BaseColor", coat);
            var b = new Material(cardBase) { name = $"life-card-skin~{key}", hideFlags = HideFlags.DontSave }; b.SetColor("_BaseColor", new Color(0.78f, 0.6f, 0.48f));
            return new[] { a, b };
        }

        /// <summary>A material's mean colour: its base map averaged (blitted to 64 x 64 and read back, so an unreadable texture works) times its colour.</summary>
        static Color Mean(Material m)
        {
            var tint = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.color : Color.white;
            var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture;
            if (!tex) return tint;
            var rt = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            try
            {
                Graphics.Blit(tex, rt); RenderTexture.active = rt;
                var t2 = new Texture2D(64, 64, TextureFormat.RGBA32, false); t2.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); t2.Apply();
                var px = t2.GetPixels(); UnityEngine.Object.DestroyImmediate(t2);
                Color sum = Color.clear; foreach (var c in px) sum += c; sum /= px.Length; sum.a = 1f;
                return sum * tint;
            }
            finally { RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt); }
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
