// Scratch Unity Editor script (brief 020, Forma): plays a figure the way HostViews.Figures.cs does (one controller, a state per clip, applyRootMotion off) and measures it.
// Modes (-executeMethod FigAnim.<mode> -figs <ids> -states <clips>): Run (hips height, drift, lowest ankle per clip), Speeds (planted-foot speed, the manifest naturalSpeedMs),
// Arms (upper-arm angle vs the Mixamo original), Compare (hand swing, foot height, toe pitch on the figure and on the Mixamo skeleton it came from), FootYaw (toe-out).
using System; using System.IO; using System.Linq; using System.Collections.Generic;
using UnityEditor; using UnityEditor.Animations; using UnityEngine;

// Latch's way of playing a figure (HostViews.Figures.cs): one controller with a state per clip, applyRootMotion off, CrossFade(state), then look where the body is.
public static class FigAnim
{
    static string Arg(string n, string d = "") { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }
    static AnimatorController Controller(string dir)
    {
        var path = "Assets/FigAnimTest.controller"; AssetDatabase.DeleteAsset(path);
        var ctl = AnimatorController.CreateAnimatorControllerAtPath(path); var sm = ctl.layers[0].stateMachine;
        foreach (var f in Directory.GetFiles(dir, "*.fbx").OrderBy(x => Path.GetFileName(x) == "idle.fbx" ? 0 : 1))
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(f.Replace('\\', '/')).OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__preview__"));
            var st = sm.AddState(Path.GetFileNameWithoutExtension(f)); st.motion = clip; st.writeDefaultValues = true;
        }
        sm.defaultState = sm.states.First(s => s.state.name == "idle").state; AssetDatabase.SaveAssets(); return ctl;
    }
    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var ctl = Controller("Assets/People/Clips");
        var figs = Arg("-figs").Split(',', StringSplitOptions.RemoveEmptyEntries); var states = Arg("-states").Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var id in figs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/People/Figures/{id}/{id}.fbx");
            foreach (var st in states)
            {
                var inst = UnityEngine.Object.Instantiate(prefab); inst.name = "figure"; var lg = inst.GetComponent<LODGroup>(); if (lg) lg.enabled = false;
                var had = inst.GetComponent<Animator>(); var an = had ?? inst.AddComponent<Animator>();
                an.runtimeAnimatorController = ctl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                an.Rebind(); an.CrossFade(st, 0f, 0, 0f); an.Update(0.0001f);
                var hips = an.GetBoneTransform(HumanBodyBones.Hips); var head = an.GetBoneTransform(HumanBodyBones.Head);
                var lf = an.GetBoneTransform(HumanBodyBones.LeftFoot); var rf = an.GetBoneTransform(HumanBodyBones.RightFoot);
                var clip = ctl.layers[0].stateMachine.states.First(s => s.state.name == st).state.motion as AnimationClip; float len = clip.length;
                var rows = new List<string>(); float maxDrift = 0, minHipY = 9, maxHipY = -9, minFoot = 9;
                for (int k = 0; k < 6; k++)
                {
                    an.Update(len / 6f);
                    var hp = hips.position; minHipY = Mathf.Min(minHipY, hp.y); maxHipY = Mathf.Max(maxHipY, hp.y);
                    maxDrift = Mathf.Max(maxDrift, new Vector2(hp.x, hp.z).magnitude); minFoot = Mathf.Min(minFoot, Mathf.Min(lf.position.y, rf.position.y));
                    rows.Add($"{k}:hipsY={hp.y:0.00} up.y={(hips.up.y):0.00} head={(head.position.y):0.00} root={inst.transform.position.x:0.00},{inst.transform.position.y:0.00},{inst.transform.position.z:0.00}");
                }
                Debug.Log($"ANIM {id} {st}: animatorOnRoot={(had != null)} avatar={(an.avatar ? an.avatar.isHuman : false)} len={len:0.00}s hipsY {minHipY:0.00}..{maxHipY:0.00} hipsDrift(xz)={maxDrift:0.00} lowestFoot={minFoot:0.00} | {string.Join(" ", rows.Take(3))}");
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }
    }

    // planted-foot speed and a time profile, for one figure and a list of clips
    public static void Speeds()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var ctl = Controller("Assets/People/Clips"); var id = Arg("-figs"); var states = Arg("-states").Split(',', StringSplitOptions.RemoveEmptyEntries);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/People/Figures/{id}/{id}.fbx");
        foreach (var st in states)
        {
            var inst = UnityEngine.Object.Instantiate(prefab); var lg = inst.GetComponent<LODGroup>(); if (lg) lg.enabled = false;
            var an = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>(); an.runtimeAnimatorController = ctl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate; an.Rebind();
            an.CrossFade(st, 0f, 0, 0f); an.Update(0.0001f);
            var clip = ctl.layers[0].stateMachine.states.First(x => x.state.name == st).state.motion as AnimationClip; float len = clip.length; int n = Mathf.Min(120, Mathf.RoundToInt(len * 30));
            var feet = new[] { an.GetBoneTransform(HumanBodyBones.LeftFoot), an.GetBoneTransform(HumanBodyBones.RightFoot) }; var hips = an.GetBoneTransform(HumanBodyBones.Hips);
            var pos = new List<Vector3[]>(); var hp = new List<Vector3>();
            for (int k = 0; k < n; k++) { an.Update(len / n); pos.Add(new[] { feet[0].position, feet[1].position }); hp.Add(hips.position); }
            float ground = pos.Min(p => Mathf.Min(p[0].y, p[1].y)); var sp = new List<float>();
            for (int k = 1; k < n; k++) for (int f = 0; f < 2; f++) { var a = pos[k - 1][f]; var b = pos[k][f]; if (a.y < ground + 0.04f && b.y < ground + 0.04f) sp.Add(new Vector2(b.x - a.x, b.z - a.z).magnitude * n / len); }
            sp.Sort(); float med = sp.Count > 3 ? sp[sp.Count / 2] : 0f;
            string prof = string.Join(" ", Enumerable.Range(0, 10).Select(i => { var h = hp[Mathf.Min(n - 1, i * n / 10)]; return $"({h.x:0.0},{h.y:0.00},{h.z:0.0})"; }));
            Debug.Log($"SPEED {id} {st}: len {len:0.00}s plantedFoot median {med:0.00} m/s (n={sp.Count}) averageSpeed {clip.averageSpeed.magnitude:0.00} hips {prof}");
            UnityEngine.Object.DestroyImmediate(inst);
        }
    }

    static float ArmAngle(Animator an, bool left)
    {
        var u = an.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm); var l = an.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
        return Vector3.Angle(l.position - u.position, Vector3.down);
    }
    public static void Arms()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var ctl = Controller("Assets/People/Clips");
        foreach (var id in Arg("-figs").Split(','))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/People/Figures/{id}/{id}.fbx");
            var inst = UnityEngine.Object.Instantiate(prefab); var lg = inst.GetComponent<LODGroup>(); if (lg) lg.enabled = false;
            var an = inst.GetComponent<Animator>(); float rest = ArmAngle(an, true);
            an.runtimeAnimatorController = ctl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate; an.Rebind(); an.CrossFade("idle", 0f, 0, 0f); an.Update(0.0001f);
            float idle = ArmAngle(an, true); float idleR = ArmAngle(an, false);
            an.CrossFade("walk", 0f, 0, 0f); an.Update(0.0001f); float walk0 = ArmAngle(an, true);
            Debug.Log($"ARMS {id}: rest(bind) upper-arm angle from down {rest:0}deg, idle L {idle:0} R {idleR:0}, walk0 {walk0:0}");
            UnityEngine.Object.DestroyImmediate(inst);
        }
    }

    static Vector3 L(Transform root, Transform t) => root.InverseTransformPoint(t.position);
    // the same clip on the figure and on the Mixamo skeleton it came from, as fractions of hips height
    public static void Compare()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var ctl = Controller("Assets/People/Clips"); var id = Arg("-figs"); var clips = Arg("-states").Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var cn in clips)
        {
            var figPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/People/Figures/{id}/{id}.fbx"); var refPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/People/Clips/{cn}.fbx");
            var fig = UnityEngine.Object.Instantiate(figPrefab); var lg = fig.GetComponent<LODGroup>(); if (lg) lg.enabled = false;
            var rf = UnityEngine.Object.Instantiate(refPrefab); rf.transform.position = new Vector3(3, 0, 0);
            var clip = ctl.layers[0].stateMachine.states.First(x => x.state.name == cn).state.motion as AnimationClip;
            foreach (var go in new[] { fig, rf }) { var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.runtimeAnimatorController = ctl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate; an.Rebind(); an.CrossFade(cn, 0f, 0, 0f); an.Update(0.0001f); }
            string Stat(GameObject go, string tag)
            {
                var an = go.GetComponent<Animator>(); an.Rebind(); an.CrossFade(cn, 0f, 0, 0f); an.Update(0.0001f);
                Transform B(HumanBodyBones b) => an.GetBoneTransform(b);
                float hipsH = go.transform.InverseTransformPoint(B(HumanBodyBones.Hips).position).y; if (hipsH < 0.05f) hipsH = B(HumanBodyBones.Hips).position.y; if (hipsH < 0.05f) hipsH = 1f;
                var lh = new List<float>(); var rh = new List<float>(); var elb = new List<float>(); var fl = new List<float>(); var toe = new List<float>(); var sh = new List<float>(); var ft = new List<float>();
                int n = 24; float len = clip.length;
                for (int k = 0; k < n; k++)
                {
                    an.Update(len / n);
                    var hips = B(HumanBodyBones.Hips); Vector3 P(HumanBodyBones b) => (B(b).position - go.transform.position) / hipsH;
                    var fwd = go.transform.forward; if (cn.StartsWith("walk") || cn.StartsWith("run")) { }
                    lh.Add(Vector3.Dot(P(HumanBodyBones.LeftHand) - P(HumanBodyBones.Hips), fwd)); rh.Add(Vector3.Dot(P(HumanBodyBones.RightHand) - P(HumanBodyBones.Hips), fwd));
                    elb.Add(Vector3.Angle(B(HumanBodyBones.LeftUpperArm).position - B(HumanBodyBones.LeftLowerArm).position, B(HumanBodyBones.LeftHand).position - B(HumanBodyBones.LeftLowerArm).position));
                    fl.Add(P(HumanBodyBones.LeftFoot).y); ft.Add(P(HumanBodyBones.RightFoot).y);
                    var f = B(HumanBodyBones.LeftFoot); var t = B(HumanBodyBones.LeftToes); if (t) { var d = t.position - f.position; toe.Add(Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg); }
                    sh.Add(Vector3.Angle(B(HumanBodyBones.LeftShoulder) ? B(HumanBodyBones.LeftUpperArm).position - B(HumanBodyBones.LeftShoulder).position : Vector3.right, Vector3.right));
                }
                string R(List<float> v) => v.Count == 0 ? "-" : $"{v.Min():0.00}..{v.Max():0.00}";
                return $"{tag}: handL fwd {R(lh)} handR {R(rh)} elbowL {R(elb)}deg footL y {R(fl)} footR y {R(ft)} toePitchL {R(toe)}deg shoulder->arm {R(sh)}";
            }
            Debug.Log($"CMP {id} {cn} | " + Stat(rf, "mixamo") + " | " + Stat(fig, "figure"));
            UnityEngine.Object.DestroyImmediate(fig); UnityEngine.Object.DestroyImmediate(rf);
        }
    }

    public static void FootYaw()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var ctl = Controller("Assets/People/Clips");
        foreach (var id in Arg("-figs").Split(','))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/People/Figures/{id}/{id}.fbx");
            foreach (var st in Arg("-states").Split(','))
            {
                var inst = UnityEngine.Object.Instantiate(prefab); var lg = inst.GetComponent<LODGroup>(); if (lg) lg.enabled = false;
                var an = inst.GetComponent<Animator>(); an.runtimeAnimatorController = ctl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate; an.Rebind(); an.CrossFade(st, 0f, 0, 0f); an.Update(0.0001f);
                var clip = ctl.layers[0].stateMachine.states.First(x => x.state.name == st).state.motion as AnimationClip; float len = clip.length; int n = 30;
                var f = an.GetBoneTransform(HumanBodyBones.LeftFoot); var t = an.GetBoneTransform(HumanBodyBones.LeftToes); var yaws = new List<float>(); var rolls = new List<float>(); var ys = new List<float>();
                for (int k = 0; k < n; k++)
                {
                    an.Update(len / n); var d = t.position - f.position; ys.Add(f.position.y);
                    yaws.Add(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);   // 0 = straight ahead (+z), + = toward the figure's right = toes of the left foot turned in; negative = out
                    var sole = Vector3.Cross(Vector3.up, new Vector3(d.x, 0, d.z)); rolls.Add(Vector3.SignedAngle(f.right, new Vector3(1, 0, 0), Vector3.forward));
                }
                float mn = yaws.Min(), mx = yaws.Max(), mean = yaws.Average();
                Debug.Log($"YAW {id} {st}: left toe yaw mean {mean:0.0} range {mn:0.0}..{mx:0.0} deg (negative = turned out)");
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }
    }
}
