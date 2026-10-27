using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Suborned.World.Review
{
    /// <summary>Brief 069 A (Latch), a check only (nothing saved but its own report and stills): the installed figures and clips after Forma's
    /// neutral-leg avatar rule and FigureBuild.Optimise.
    /// 1. Figures: avatar valid and humanoid, Optimize Game Objects and the exposed bones. Clips: avatar setup and source, every clip reimported with
    ///    the console captured (a "Copied Avatar Rig Configuration mis-match" or an invalid avatar shows here).
    /// 2. FootDiag (briefs/069 left-foot and bow-leg measures) in walk, run and idle on c6, guard, d6 and the enforcer: per frame the foot's yaw
    ///    against the hips' facing (+ toward the figure's left), Foot→ToeBase pitch and height; planted = within 3 cm of the lowest; the thigh's
    ///    sideways angle (+ outward). Each figure's flat rest pitch is its avatar rest (humanDescription.skeleton applied).
    /// 3. The own-avatar clips (avatarSetup Create From This Model) on c6 and the waitress, the thighs and knees against the clip on its own skeleton.
    /// 4. Stills of the waitress serving with the tray seated as HostViews seats it (the optimised figure's exposed RightHand).
    /// The figure instances are deoptimised (AnimatorUtility.DeoptimizeTransformHierarchy) for the measures; the stills use them as the game does.
    /// Batchmode: -executeMethod Suborned.World.Review.AvatarCheck069.Run -out &lt;dir&gt;</summary>
    public static class AvatarCheck069
    {
        const string Root = "Assets/Suborned/People";
        static string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        public static void Run()
        {
            int code = 1;
            try { Inner(); code = 0; } catch (System.Exception e) { Debug.LogException(e); }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static void Inner()
        {
            var outDir = Path.GetFullPath(Arg("-out") ?? Path.Combine(HarbourWorldBuilder.RepoRoot, "out/scratch/latch/069a"));
            Directory.CreateDirectory(outDir);
            var m = Json.Parse(File.ReadAllText(Path.Combine(Root, "manifest.json")));
            var sb = new StringBuilder();
            // 1a. figures
            int ok = 0, n = 0;
            sb.AppendLine("## Figures");
            foreach (var f in J.A(m, "figures"))
            {
                string id = J.S(f, "id"), path = Path.Combine(Root, J.S(f, "fbx")); n++;
                var imp = AssetImporter.GetAtPath(path) as ModelImporter; var av = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                bool good = av && av.isValid && av.isHuman; if (good) ok++;
                sb.AppendLine($"{id}: avatar {(av ? (av.isValid ? "valid" : "INVALID") + (av.isHuman ? " humanoid" : " generic") : "NONE")}, {av?.humanDescription.human?.Length ?? 0} human bones; optimise {imp?.optimizeGameObjects}; exposed [{string.Join(",", imp?.extraExposedTransformPaths.Select(x => x.Split('/').Last()) ?? new string[0])}]");
            }
            sb.AppendLine($"figures valid humanoid: {ok}/{n}");
            // 1b. clips, reimported with the console captured
            sb.AppendLine("\n## Clips");
            var msgs = new List<string>(); string cur = null;
            void Hook(string msg, string st, LogType t) { if (cur != null && t != LogType.Log) msgs.Add($"{cur}: {t}: {msg.Split('\n')[0]}"); }
            Application.logMessageReceived += Hook;
            var clipRows = new List<(string name, string setup, string src, bool human)>();
            try
            {
                foreach (var c in J.A(m, "clips"))
                {
                    string name = J.S(c, "name"), file = Path.Combine(Root, J.S(c, "file")); cur = name;
                    AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    var imp = AssetImporter.GetAtPath(file) as ModelImporter; var clip = AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__"));
                    clipRows.Add((name, imp?.avatarSetup.ToString(), imp?.sourceAvatar ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(imp.sourceAvatar)) : "", clip && clip.humanMotion));
                }
            }
            finally { Application.logMessageReceived -= Hook; cur = null; }
            foreach (var g in clipRows.GroupBy(r => r.setup + (r.src.Length > 0 ? " from " + r.src : ""))) sb.AppendLine($"{g.Key}: {g.Count()} [{string.Join(", ", g.Select(r => r.name + (r.human ? "" : " (NOT humanoid)")))}]");
            sb.AppendLine($"import messages (warnings and errors) while reimporting {clipRows.Count} clips: {msgs.Count}");
            foreach (var s in msgs.Distinct()) sb.AppendLine("  " + s);
            // 1c. takes (brief 069 round 3): each clip's take and frame range against its FBX's own take, the clip's length and frames, and the manifest's frames
            sb.AppendLine("\n## Takes");
            int takeOk = 0, takeN = 0;
            foreach (var c in J.A(m, "clips"))
            {
                string name = J.S(c, "name"), file = Path.Combine(Root, J.S(c, "file")); takeN++;
                var imp = AssetImporter.GetAtPath(file) as ModelImporter; var clip = AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__"));
                var ti = imp.importedTakeInfos; var ca = imp.clipAnimations.Length > 0 ? imp.clipAnimations[0] : null;
                var take = ca != null ? ti.FirstOrDefault(t => t.name == ca.takeName) : ti.FirstOrDefault();
                float tf0 = take.name != null ? take.startTime * take.sampleRate : -1, tf1 = take.name != null ? take.stopTime * take.sampleRate : -1;
                int frames = clip ? Mathf.RoundToInt(clip.length * clip.frameRate) + 1 : 0; object mf = (c as Dictionary<string, object>)?.TryGetValue("frames", out var fv) == true ? fv : null;
                bool match = ca != null && take.name == ca.takeName && Mathf.Abs(ca.firstFrame - tf0) < 0.01f && Mathf.Abs(ca.lastFrame - tf1) < 0.01f && clip && AnimationUtility.GetCurveBindings(clip).Length > 0;
                if (match) takeOk++;
                sb.AppendLine($"{name}: takes [{string.Join(", ", ti.Select(t => $"{t.name} {F(t.startTime * t.sampleRate)}-{F(t.stopTime * t.sampleRate)}"))}]; clip {ca?.takeName} {F(ca?.firstFrame ?? -1)}-{F(ca?.lastFrame ?? -1)}; length {F(clip ? clip.length : 0)} s, {frames} frames (manifest {mf ?? "-"}), curves {(clip ? AnimationUtility.GetCurveBindings(clip).Length : 0)}; {(match ? "ok" : "MISMATCH")}");
            }
            sb.AppendLine($"clips whose clip range is their FBX's own take: {takeOk}/{takeN}");
            {
                var ctl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(Root + "/Generated/Figures.controller");
                var states = ctl.layers.SelectMany(l => l.stateMachine.states.Select(x => l.name + "/" + x.state.name)).ToList();
                sb.AppendLine($"controller: {states.Count} states over {ctl.layers.Length} layers ({string.Join(", ", ctl.layers.Select(l => $"{l.name} {l.stateMachine.states.Length}"))})");
                File.WriteAllText(Path.Combine(outDir, "states.txt"), string.Join("\n", ctl.layers.SelectMany(l => l.stateMachine.states.Select(x => x.state.name)).OrderBy(x => x, System.StringComparer.Ordinal)) + "\n");
            }
            File.WriteAllText(Path.Combine(outDir, "check.md"), sb.ToString());

            // 2. FootDiag
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var figs = new[] { "c6", "guard", "d6", "enforcer" };
            var rows = new StringBuilder("figure\tclip\trestPitchL\trestPitchR\tplantedPitchL\tplantedPitchR\tplantedYawL\tplantedYawR\tthighL\tthighR\tkneeL\tkneeR\n");
            foreach (var cn in new[] { "walk", "run", "idle" })
            {
                var src = SourceFeet(cn);
                rows.Append($"SOURCE(raw)\t{cn}\t-\t-\t{F(src.pL)}\t{F(src.pR)}\t{F(src.yL)}\t{F(src.yR)}\t{F(src.aL)}\t{F(src.aR)}\t{F(src.kL)}\t{F(src.kR)}\n");
            }
            foreach (var id in figs)
            {
                var (model, path) = Fig(m, id); var rest = RestPitch(model, path);
                foreach (var cn in new[] { "walk", "run", "idle" })
                {
                    var s = Measure(model, Clip(m, cn));
                    rows.Append($"{id}\t{cn}\t{F(rest.l)}\t{F(rest.r)}\t{F(s.pL)}\t{F(s.pR)}\t{F(s.yL)}\t{F(s.yR)}\t{F(s.aL)}\t{F(s.aR)}\t{F(s.kL)}\t{F(s.kR)}\n");
                }
            }
            File.WriteAllText(Path.Combine(outDir, "footdiag.tsv"), rows.ToString());

            // 3. own-avatar clips: thighs and knees on two figures against the clip on its own skeleton
            var own = new StringBuilder("clip\twho\tthighL\tthighR\tkneeL\tkneeR\n");
            foreach (var c in J.A(m, "clips"))
            {
                string name = J.S(c, "name"), file = Path.Combine(Root, J.S(c, "file"));
                if ((AssetImporter.GetAtPath(file) as ModelImporter)?.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) continue;
                var clip = AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__")); var self = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                if (!clip || !(AssetDatabase.LoadAllAssetsAtPath(file).OfType<Avatar>().FirstOrDefault() is Avatar sav && sav.isHuman)) { own.Append($"{name}\tNO HUMANOID CLIP (avatar creation failed)\t\t\t\t\n"); continue; }
                var ss = Measure(self, clip, false);
                own.Append($"{name}\tits own skeleton\t{F(ss.aL)}\t{F(ss.aR)}\t{F(ss.kL)}\t{F(ss.kR)}\n");
                var raw = SourceFeet(name); own.Append($"{name}\tsource raw (Generic)\t{F(raw.aL)}\t{F(raw.aR)}\t{F(raw.kL)}\t{F(raw.kR)}\n");
                foreach (var id in new[] { "c6", "waitress" }) { var s = Measure(Fig(m, id).model, clip); own.Append($"{name}\t{id}\t{F(s.aL)}\t{F(s.aR)}\t{F(s.kL)}\t{F(s.kR)}\n"); }
            }
            File.WriteAllText(Path.Combine(outDir, "own-avatar-clips.tsv"), own.ToString());

            // 3b. stills: c6 (optimised, as the game draws it) beside the clip on its own skeleton (a sphere per bone, a capsule per limb), mid-clip
            // OwnStills(m, outDir, new[] { "sit", "sit_talk", "cover_fire", "cover_pin", "run_lean", "carry_shoulder", "serve", "serve_walk", "stand_to_sit", "carried_drape" });
            // 4. the tray on the waitress (the optimised figure, the exposed RightHand, the library's seat)
            TrayStills(m, outDir);
            Debug.Log($"AvatarCheck069: written {outDir}\n{sb}");
        }

        static (GameObject model, string path) Fig(object m, string id)
        {
            var f = J.A(m, "figures").First(x => J.S(x, "id") == id); var path = Path.Combine(Root, J.S(f, "fbx"));
            return (AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
        }
        static AnimationClip Clip(object m, string name)
        {
            var c = J.A(m, "clips").First(x => J.S(x, "name") == name);
            return AssetDatabase.LoadAllAssetsAtPath(Path.Combine(Root, J.S(c, "file"))).OfType<AnimationClip>().First(x => !x.name.StartsWith("__preview__"));
        }
        static Transform Find(Transform t, string n) { foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == n || c.name.EndsWith(":" + n)) return c; return null; }

        struct S { public float yL, yR, pL, pR, aL, aR, kL, kR; }
        struct Frame { public float yL, yR, pL, pR, hL, hR, aL, aR, kL, kR; }

        static Frame One(Transform root)
        {
            Vector3 P(string n) => Find(root, n).position;
            var lat = P("RightUpLeg") - P("LeftUpLeg"); lat.y = 0; lat.Normalize(); var fwd = Vector3.Cross(lat, Vector3.up);
            var fr = new Frame();
            foreach (var (s, outward) in new[] { ("Left", -lat), ("Right", lat) })
            {
                var foot = P(s + "Foot"); var d = P(s + "ToeBase") - foot; float dy = d.y; d.y = 0;
                float yaw = -Vector3.SignedAngle(fwd, d.normalized, Vector3.up), pitch = Mathf.Atan2(dy, d.magnitude) * Mathf.Rad2Deg, h = foot.y - root.position.y;
                Vector3 hip = P(s + "UpLeg"), knee = P(s + "Leg"); float len = (knee - hip).magnitude + (foot - knee).magnitude;
                var v = knee - hip; var vf = v - Vector3.Dot(v, fwd) * fwd;
                float abd = Mathf.Atan2(Vector3.Dot(vf, outward), -Vector3.Dot(vf, Vector3.up)) * Mathf.Rad2Deg, kn = Vector3.Dot(knee - hip, outward) / len;
                if (s == "Left") { fr.yL = yaw; fr.pL = pitch; fr.hL = h; fr.aL = abd; fr.kL = kn; } else { fr.yR = yaw; fr.pR = pitch; fr.hR = h; fr.aR = abd; fr.kR = kn; }
            }
            return fr;
        }

        static float Med(IEnumerable<float> x) { var a = x.OrderBy(v => v).ToList(); return a.Count == 0 ? float.NaN : a[a.Count / 2]; }
        static S Sum(List<Frame> f)
        {
            float lo = f.Min(x => x.hL), ro = f.Min(x => x.hR);
            var pl = f.Where(x => x.hL < lo + 0.03f).ToList(); var pr = f.Where(x => x.hR < ro + 0.03f).ToList();
            return new S { yL = Med(pl.Select(x => x.yL)), yR = Med(pr.Select(x => x.yR)), pL = Med(pl.Select(x => x.pL)), pR = Med(pr.Select(x => x.pR)), aL = Med(f.Select(x => x.aL)), aR = Med(f.Select(x => x.aR)), kL = Med(f.Select(x => x.kL)), kR = Med(f.Select(x => x.kR)) };
        }

        /// <summary>The clip on the model through its Animator (Humanoid, foot IK off, root motion off), 30 samples per second over one length.</summary>
        static S Measure(GameObject model, AnimationClip clip, bool deoptimise = true)
        {
            var go = (GameObject)Object.Instantiate(model); go.transform.position = Vector3.zero;
            if (deoptimise) AnimatorUtility.DeoptimizeTransformHierarchy(go);
            var an = go.GetComponent<Animator>(); an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var g = PlayableGraph.Create(); g.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var o = AnimationPlayableOutput.Create(g, "a", an); var cp = AnimationClipPlayable.Create(g, clip); cp.SetApplyFootIK(false); o.SetSourcePlayable(cp); g.Play();
            int n = Mathf.Max(2, Mathf.RoundToInt(clip.length * 30f)); var fr = new List<Frame>();
            for (int i = 0; i < n; i++) { cp.SetTime(i / 30f); g.Evaluate(0f); fr.Add(One(go.transform)); }
            g.Destroy(); Object.DestroyImmediate(go);
            return Sum(fr);
        }

        /// <summary>The walk read raw: the clip FBX copied to a temporary Generic import and sampled on its own transforms.</summary>
        static S SourceFeet(string name)
        {
            var tmp = $"Assets/Suborned/People/Generated/_{name}_generic_069.fbx";
            AssetDatabase.DeleteAsset(tmp); AssetDatabase.CopyAsset(Path.Combine(Root, "Clips", name + ".fbx"), tmp);
            var imp = (ModelImporter)AssetImporter.GetAtPath(tmp); imp.animationType = ModelImporterAnimationType.Generic; imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; imp.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(tmp); var clip = AssetDatabase.LoadAllAssetsAtPath(tmp).OfType<AnimationClip>().First(x => !x.name.StartsWith("__preview__"));
            var go = (GameObject)Object.Instantiate(model); var fr = new List<Frame>();
            int n = Mathf.RoundToInt(clip.length * 30f);
            for (int i = 0; i < n; i++) { clip.SampleAnimation(go, i / 30f); fr.Add(One(go.transform)); }
            Object.DestroyImmediate(go); AssetDatabase.DeleteAsset(tmp);
            return Sum(fr);
        }

        /// <summary>The figure's flat rest: its avatar's rest (humanDescription.skeleton) applied to its bones, Foot→ToeBase pitch per side.</summary>
        static (float l, float r) RestPitch(GameObject model, string path)
        {
            var go = (GameObject)Object.Instantiate(model); AnimatorUtility.DeoptimizeTransformHierarchy(go);
            var av = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().First();
            var all = go.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
            foreach (var sb in av.humanDescription.skeleton) if (all.TryGetValue(sb.name, out var t) && t != go.transform) { t.localPosition = sb.position; t.localRotation = sb.rotation; t.localScale = sb.scale; }
            var f = One(go.transform); Object.DestroyImmediate(go); return (f.pL, f.pR);
        }


        static void OwnStills(object m, string outDir, string[] names)
        {
            var dir = Path.Combine(outDir, "stills"); Directory.CreateDirectory(dir);
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main; var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.color = new Color(0.9f, 0.3f, 0.1f);
            foreach (var name in names)
            {
                var file = Path.Combine(Root, "Clips", name + ".fbx"); var clip = AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().FirstOrDefault(x => !x.name.StartsWith("__preview__")); if (!clip) continue;
                var fig = (GameObject)Object.Instantiate(Fig(m, "c6").model); fig.transform.position = new Vector3(0.6f, 0f, 0f);
                foreach (var smr in fig.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.forceMatrixRecalculationPerRender = true; smr.updateWhenOffscreen = true; }
                if (fig.GetComponentInChildren<LODGroup>() is LODGroup lg) lg.ForceLOD(0);
                var self = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(file)); self.transform.position = new Vector3(-0.6f, 0f, 0f);
                var gs = new List<PlayableGraph>();
                foreach (var go in new[] { fig, self })
                {
                    var an = go.GetComponent<Animator>(); an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var g = PlayableGraph.Create(); g.SetTimeUpdateMode(DirectorUpdateMode.Manual); var o = AnimationPlayableOutput.Create(g, "a", an); var cp = AnimationClipPlayable.Create(g, clip); cp.SetApplyFootIK(false); o.SetSourcePlayable(cp); g.Play();
                    cp.SetTime(clip.length * 0.5f); g.Evaluate(0f); gs.Add(g);
                }
                var marks = new List<GameObject>();
                foreach (var t in self.GetComponentsInChildren<Transform>(true))
                {
                    if (t == self.transform || t.parent == null || t.parent == self.transform || t.name.Contains("Armature")) continue;
                    var a = t.parent.position; var b = t.position; float len = (b - a).magnitude; if (len < 0.01f || len > 0.8f) continue;
                    var c = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(c.GetComponent<Collider>()); c.GetComponent<Renderer>().sharedMaterial = mat;
                    c.transform.position = (a + b) * 0.5f; c.transform.rotation = Quaternion.LookRotation(b - a); c.transform.localScale = new Vector3(0.03f, 0.03f, len); marks.Add(c);
                }
                foreach (var (v, pos) in new[] { ("front", new Vector3(0.0f, 1.2f, 3.6f)), ("side", new Vector3(3.6f, 1.2f, 0.2f)) })
                {
                    cam.fieldOfView = 35f; cam.transform.position = pos; cam.transform.LookAt(new Vector3(0f, 0.7f, 0f));
                    var rt = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    cam.targetTexture = rt; cam.Render(); cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
                    var tex = new Texture2D(1280, 720, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, $"own_{name}_{v}.png"), tex.EncodeToPNG()); Object.DestroyImmediate(tex); RenderTexture.ReleaseTemporary(rt);
                }
                foreach (var g in gs) g.Destroy(); foreach (var x in marks) Object.DestroyImmediate(x); Object.DestroyImmediate(fig); Object.DestroyImmediate(self);
            }
        }

        static void TrayStills(object m, string outDir)
        {
            var lib = AssetDatabase.LoadAssetAtPath<FigureLibrary>(Root + "/Generated/FigureLibrary.asset"); if (!lib || !lib.tray) { Debug.LogWarning("AvatarCheck069: no tray in the library"); return; }
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main; var dir = Path.Combine(outDir, "stills"); Directory.CreateDirectory(dir);
            foreach (var id in lib.trayFigures)
            {
                var e = lib.Find(id); if (e == null) continue; int seat = lib.TraySeat(id);
                var go = (GameObject)Object.Instantiate(e.model); go.transform.position = Vector3.zero;
                var an = go.GetComponent<Animator>(); an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.forceMatrixRecalculationPerRender = true; smr.updateWhenOffscreen = true; }
                if (go.GetComponentInChildren<LODGroup>() is LODGroup lg) lg.ForceLOD(0);
                var hand = an.GetBoneTransform(HumanBodyBones.RightHand) ?? Find(go.transform, "RightHand");
                if (!hand) { Debug.LogWarning($"AvatarCheck069: {id} has no RightHand"); Object.DestroyImmediate(go); continue; }
                var tray = Object.Instantiate(lib.tray, hand, false).transform; tray.localPosition = lib.trayPos[seat]; tray.localRotation = Quaternion.Euler(lib.trayEuler[seat]); tray.localScale = Vector3.one;
                foreach (var cn in new[] { "serve", "serve_walk" })
                {
                    var clip = Clip(m, cn);
                    var g = PlayableGraph.Create(); g.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var o = AnimationPlayableOutput.Create(g, "a", an); var cp = AnimationClipPlayable.Create(g, clip); cp.SetApplyFootIK(false); o.SetSourcePlayable(cp); g.Play();
                    foreach (var ph in new[] { 0.3f, 0.7f })
                    {
                        cp.SetTime(clip.length * ph); g.Evaluate(0f);
                        var at = hand.position; cam.fieldOfView = 30f; cam.transform.position = at + new Vector3(1.4f, 0.35f, 1.6f); cam.transform.LookAt(at);
                        var rt = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                        cam.targetTexture = rt; cam.Render(); cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
                        var tex = new Texture2D(1280, 720, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
                        File.WriteAllBytes(Path.Combine(dir, $"tray_{id}_{cn}_{(int)(ph * 100)}.png"), tex.EncodeToPNG()); Object.DestroyImmediate(tex); RenderTexture.ReleaseTemporary(rt);
                    }
                    g.Destroy();
                }
                Object.DestroyImmediate(go);
            }
        }
    }
}
