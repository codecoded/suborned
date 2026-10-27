using System.Linq; using System.IO; using System.Text; using System.Collections.Generic;
using UnityEngine; using UnityEditor; using UnityEngine.Playables; using UnityEngine.Animations;
/// Brief 069 weapons, measured in Unity (the truth): the figure imported by Latch's rule (PropCal.ImportByRule), the game's class aim clip, the
/// artist hand pose (props.json spec.handPose.artist) put on the right fingers over the clip (rest local rotation x a turn about each finger
/// bone's own X toward the palm, as Blender's), the weapon under Attach_RightHand at its seat. At each aim frame: wrap (middle, ring: the chain
/// knuckle-joints-tip turned about the grip line, the weapon's +Y through its pivot), crossings (right-hand vertices of the baked skin inside the
/// weapon: odd ray hits against its MeshCollider, rays both ways), the support socket's gap to Attach_LeftHand, the shoulder socket's to the right
/// upper arm. -cfg <json> -out <json>.
public static class WeaponCheck {
    [System.Serializable] class Cfg { public string figure, clip, weapon; public float[] seat, rot, support, shoulder; public string[] fingers; public float[] curl; }
    public static void Run() {
        var a = System.Environment.GetCommandLineArgs(); var cfg = JsonUtility.FromJson<Cfg>(File.ReadAllText(a[System.Array.IndexOf(a, "-cfg") + 1])); var outp = a[System.Array.IndexOf(a, "-out") + 1];
        var mi = typeof(PropCal).GetMethod("ImportByRule", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); var fig = $"Assets/F/{cfg.figure}.fbx";
        mi.Invoke(null, new object[] { fig, $"Assets/F/{cfg.figure}_avatarT.fbx" });
        var go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fig)); var an = go.GetComponent<Animator>(); an.avatar = AssetDatabase.LoadAllAssetsAtPath(fig).OfType<Avatar>().First(); an.applyRootMotion = false; an.enabled = true; an.cullingMode = AnimatorCullingMode.AlwaysAnimate; an.Rebind();
        var T = go.GetComponentsInChildren<Transform>(true).ToDictionary(t => t.name, t => t);
        var rest = new Dictionary<string, Quaternion>(); for (int i = 0; i < cfg.fingers.Length; i++) rest[cfg.fingers[i]] = T[cfg.fingers[i]].localRotation;
        var clip = AssetDatabase.LoadAllAssetsAtPath(cfg.clip).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
        var g = PlayableGraph.Create(); g.SetTimeUpdateMode(DirectorUpdateMode.Manual); var o = AnimationPlayableOutput.Create(g, "a", an); var cp = AnimationClipPlayable.Create(g, clip); cp.SetApplyFootIK(false); o.SetSourcePlayable(cp); g.Play();
        var w = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(cfg.weapon)); var at = T["Attach_RightHand"];
        w.transform.SetParent(at, false); w.transform.localPosition = new Vector3(cfg.seat[0], cfg.seat[1], cfg.seat[2]); w.transform.localRotation = new Quaternion(cfg.rot[0], cfg.rot[1], cfg.rot[2], cfg.rot[3]);
        var lod0 = w.GetComponentsInChildren<MeshFilter>().OrderBy(m => m.name.Contains("LOD1") || m.name.Contains("LOD2") ? 1 : 0).First(); var mc = lod0.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = lod0.sharedMesh;
        Physics.queriesHitBackfaces = true;
        var smr = go.GetComponentsInChildren<SkinnedMeshRenderer>().OrderBy(s => s.name.Contains("LOD0") ? 0 : 1).First(); var bones = smr.bones; var bw = smr.sharedMesh.boneWeights;
        var handIx = Enumerable.Range(0, bw.Length).Where(i => bones[bw[i].boneIndex0] && bones[bw[i].boneIndex0].name.StartsWith("RightHand")).ToArray();
        int n = Mathf.RoundToInt(clip.length * clip.frameRate); var hips = an.GetBoneTransform(HumanBodyBones.Hips); var hand = an.GetBoneTransform(HumanBodyBones.RightHand);
        var hs = new float[n + 1]; for (int k = 0; k <= n; k++) { cp.SetTime(k / clip.frameRate); g.Evaluate(0); hs[k] = hand.position.y - hips.position.y; }
        float mx = hs.Max(); var ok = Enumerable.Range(0, n + 1).Where(k => hs[k] >= mx - 0.05f).ToList(); var frames = new[] { ok[0], ok[ok.Count / 2], ok[ok.Count - 1] };
        var sb = new StringBuilder("{\"frames\": [\n");
        foreach (var k in frames) {
            cp.SetTime(k / clip.frameRate); g.Evaluate(0);
            for (int i = 0; i < cfg.fingers.Length; i++) T[cfg.fingers[i]].localRotation = rest[cfg.fingers[i]] * Quaternion.AngleAxis(-cfg.curl[i], Vector3.right);
            Physics.SyncTransforms();
            var gp = w.transform.position; var gax = w.transform.up; var wr = new List<string>();
            foreach (var f in new[] { "Middle", "Ring" }) {
                var pts = new[] { T[$"RightHand{f}1"].position, T[$"RightHand{f}2"].position, T[$"RightHand{f}3"].position, T[$"RightHand{f}3"].position + (T[$"RightHand{f}3"].position - T[$"RightHand{f}2"].position) };
                float tot = 0; for (int q = 0; q < 3; q++) tot += Vector3.SignedAngle(Vector3.ProjectOnPlane(pts[q] - gp, gax), Vector3.ProjectOnPlane(pts[q + 1] - gp, gax), gax);
                wr.Add($"\"{f}\": {Mathf.Abs(tot):0.#}");
            }
            var baked = new Mesh(); smr.BakeMesh(baked, true); var V = baked.vertices; int inside = 0;
            foreach (var i in handIx) {
                var p = smr.transform.TransformPoint(V[i]); int hits = Physics.RaycastAll(p, Vector3.up, 5f).Count(h => h.collider == mc) ; int hits2 = Physics.RaycastAll(p, Vector3.down, 5f).Count(h => h.collider == mc);
                if (hits % 2 == 1 && hits2 % 2 == 1) inside++;
            }
            string extra = "";
            if (cfg.support != null && cfg.support.Length == 3) extra += $", \"supportGapM\": {(w.transform.TransformPoint(new Vector3(cfg.support[0], cfg.support[1], cfg.support[2])) - T["Attach_LeftHand"].position).magnitude:0.###}";
            if (cfg.shoulder != null && cfg.shoulder.Length == 3) extra += $", \"shoulderGapM\": {(w.transform.TransformPoint(new Vector3(cfg.shoulder[0], cfg.shoulder[1], cfg.shoulder[2])) - an.GetBoneTransform(HumanBodyBones.RightUpperArm).position).magnitude:0.###}";
            sb.Append($"  {{\"frame\": {k}, \"wrapDeg\": {{{string.Join(", ", wr)}}}, \"handVertsInside\": {inside}, \"handVerts\": {handIx.Length}{extra}}},\n");
        }
        sb.Append(" {}]}\n"); File.WriteAllText(outp, sb.ToString()); g.Destroy(); EditorApplication.Exit(0);
    }
}
