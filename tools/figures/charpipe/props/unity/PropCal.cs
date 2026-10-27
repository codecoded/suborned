using System.Linq; using System.IO; using System.Text; using System.Collections.Generic;
using UnityEngine; using UnityEditor; using UnityEngine.Playables; using UnityEngine.Animations;
/// Brief 069 props: a held prop's offset measured in Unity's own frame (the source of truth: Unity's Humanoid retarget, not Blender's).
/// Figures are imported by Latch's rule (FootDiag.E2E ImportFigureByRule: Create From This Model, then the _avatarT T-pose by bone name, auto-mapping
/// off; unoptimised so the bones can be read). The clip is the game's own (People/Clips meta: Copy From Other Avatar from walk).
/// -figs a,b -clip path -attach Attach_RightHand -out json. Per figure: the aim frames (hand within 5 cm of its highest above the hips), at each the
/// local rotation that lays the prop's +Z on the level aim line (between the shoulders out through the attach point) with +Y up; one offset = their
/// mean over every figure and frame; then the residual pitch and yaw of that one offset at every aim frame.
public static class PropCal {
    static void Configure(string path, System.Action<ModelImporter> edit) { var i = (ModelImporter)AssetImporter.GetAtPath(path); edit(i); i.SaveAndReimport(); }
    static void ImportByRule(string fig, string avT) {
        Configure(avT, i => { i.preserveHierarchy = false; i.autoGenerateAvatarMappingIfUnspecified = true; i.humanDescription = new HumanDescription { upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f, armStretch = 0.05f, legStretch = 0.05f }; i.animationType = ModelImporterAnimationType.Human; i.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; i.importAnimation = false; i.materialImportMode = ModelImporterMaterialImportMode.None; });
        var av = AssetDatabase.LoadAllAssetsAtPath(avT).OfType<Avatar>().First();
        Configure(fig, i => { i.animationType = ModelImporterAnimationType.Human; i.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; i.sourceAvatar = null; i.autoGenerateAvatarMappingIfUnspecified = true; i.humanDescription = new HumanDescription { upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f, armStretch = 0.05f, legStretch = 0.05f }; i.optimizeGameObjects = false; i.importAnimation = false; i.materialImportMode = ModelImporterMaterialImportMode.None; });
        Configure(fig, i => {
            var hd = AssetDatabase.LoadAllAssetsAtPath(i.assetPath).OfType<Avatar>().First().humanDescription;
            var rb = av.humanDescription.skeleton.Skip(1).GroupBy(b => b.name).ToDictionary(g => g.Key, g => g.First());
            hd.skeleton = hd.skeleton.Select(b => rb.TryGetValue(b.name, out var r) && b.name != "Armature" ? new SkeletonBone { name = b.name, position = r.position, rotation = r.rotation, scale = r.scale } : b).ToArray();
            hd.human = av.humanDescription.human; i.autoGenerateAvatarMappingIfUnspecified = false; i.humanDescription = hd; });
    }
    static Transform Find(Transform t, string n) => t.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == n);
    static string V(Vector3 v) => $"[{v.x:0.######},{v.y:0.######},{v.z:0.######}]";
    static string Q(Quaternion q) => $"[{q.x:0.######},{q.y:0.######},{q.z:0.######},{q.w:0.######}]";
    public static void Run() {
        var a = System.Environment.GetCommandLineArgs(); string Arg(string k) => a[System.Array.IndexOf(a, k) + 1];
        var figs = Arg("-figs").Split(','); var clipPath = Arg("-clip"); var attach = Arg("-attach"); var outp = Arg("-out"); var lp = new Vector3(0, 0, float.Parse(Arg("-forward")));
        var clip = AssetDatabase.LoadAllAssetsAtPath(clipPath).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
        var all = new List<Quaternion>(); var per = new Dictionary<string, List<(int fr, Quaternion local, Transform at, Vector3 aim)>>(); var sb = new StringBuilder("{\n");
        var keep = new List<(GameObject go, PlayableGraph g, AnimationClipPlayable cp, Animator an)>();
        foreach (var f in figs) {
            var fig = $"Assets/F/{f}.fbx"; ImportByRule(fig, $"Assets/F/{f}_avatarT.fbx");
            var go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fig)); var an = go.GetComponent<Animator>();
            an.avatar = AssetDatabase.LoadAllAssetsAtPath(fig).OfType<Avatar>().First(); an.applyRootMotion = false; an.enabled = true; an.cullingMode = AnimatorCullingMode.AlwaysAnimate; an.Rebind();
            var g = PlayableGraph.Create(); g.SetTimeUpdateMode(DirectorUpdateMode.Manual); var o = AnimationPlayableOutput.Create(g, "a", an);
            var cp = AnimationClipPlayable.Create(g, clip); cp.SetApplyFootIK(false); o.SetSourcePlayable(cp); g.Play();
            int n = Mathf.RoundToInt(clip.length * clip.frameRate); var hand = an.GetBoneTransform(HumanBodyBones.RightHand); var hips = an.GetBoneTransform(HumanBodyBones.Hips);
            var hs = new float[n + 1]; for (int k = 0; k <= n; k++) { cp.SetTime(k / clip.frameRate); g.Evaluate(0); hs[k] = hand.position.y - hips.position.y; }
            float mx = hs.Max(); var frames = Enumerable.Range(0, n + 1).Where(k => hs[k] >= mx - 0.05f).ToList(); var at = Find(go.transform, attach);
            per[f] = new List<(int, Quaternion, Transform, Vector3)>();
            foreach (var k in frames) {
                cp.SetTime(k / clip.frameRate); g.Evaluate(0);
                var mid = (an.GetBoneTransform(HumanBodyBones.LeftUpperArm).position + an.GetBoneTransform(HumanBodyBones.RightUpperArm).position) / 2;
                var aim = at.position - mid; aim.y = 0; var want = Quaternion.LookRotation(aim.normalized, Vector3.up); var loc = Quaternion.Inverse(at.rotation) * want;
                if (all.Count > 0 && Quaternion.Dot(all[0], loc) < 0) loc = new Quaternion(-loc.x, -loc.y, -loc.z, -loc.w);
                all.Add(loc); per[f].Add((k, loc, at, aim));
            }
            keep.Add((go, g, cp, an));
        }
        var m = new Vector4(); foreach (var q in all) m += new Vector4(q.x, q.y, q.z, q.w); m.Normalize(); var one = new Quaternion(m.x, m.y, m.z, m.w);
        var mean = one; int ri = System.Array.IndexOf(a, "-rot"); if (ri >= 0) { var r = a[ri + 1].Split(',').Select(float.Parse).ToArray(); one = new Quaternion(r[0], r[1], r[2], r[3]); }   // -rot x,y,z,w: the residuals of a given offset (the hand-seated one) instead of the fitted mean
        sb.Append($" \"fittedMeanRotation\": {Q(mean)},\n");
        sb.Append($" \"clipHuman\": {(clip.humanMotion ? 1 : 0)}, \"avatarHuman\": {(keep[0].an.avatar.isHuman ? 1 : 0)},\n");
        sb.Append($" \"source\": \"Unity {Application.unityVersion}, Humanoid retarget of {Path.GetFileName(clipPath)}\",\n \"bone\": \"{attach}\",\n \"localPosition\": {V(lp)},\n \"localRotation\": {Q(one)},\n \"localEulerAngles\": {V(one.eulerAngles)},\n \"figures\": {{\n");
        int fi = 0;
        foreach (var (go, g, cp, an) in keep) {
            var f = figs[fi]; var rows = new List<string>();
            foreach (var (k, loc, at, aim0) in per[f]) {
                cp.SetTime(k / clip.frameRate); g.Evaluate(0);
                var mid = (an.GetBoneTransform(HumanBodyBones.LeftUpperArm).position + an.GetBoneTransform(HumanBodyBones.RightUpperArm).position) / 2;
                var aim = at.position - mid; aim.y = 0; var barrel = at.rotation * one * Vector3.forward; var flat = barrel; flat.y = 0;
                float pitch = Mathf.Asin(Mathf.Clamp(barrel.y, -1, 1)) * Mathf.Rad2Deg; float yaw = Vector3.SignedAngle(aim, flat, Vector3.up);
                float own = Quaternion.Angle(loc, one);
                rows.Add($"   {{\"frame\": {k}, \"pitchDeg\": {pitch:0.##}, \"yawToAimDeg\": {yaw:0.##}, \"ownOffsetDegFromOne\": {own:0.##}}}");
            }
            sb.Append($"  \"{f}\": [\n{string.Join(",\n", rows)}\n  ]{(fi < keep.Count - 1 ? "," : "")}\n"); fi++;
            g.Destroy(); Object.DestroyImmediate(go);
        }
        sb.Append(" }\n}\n"); File.WriteAllText(outp, sb.ToString()); EditorApplication.Exit(0);
    }
}
