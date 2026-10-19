// Scratch Unity Editor script (brief 022, Forma): a figure playing a sit clip on the café set's real chair (kit CafeSet_Tulip_Play), rendered from the side, the front and the play
// camera, with the seat contact measured. Throwaway project like FigCheck (/tmp/fm/uproj, Assets/People = out/people-models/People, Assets/Kit/DK_CafeSet_Tulip_Play.fbx).
// unity -batchmode -quit -projectPath <scratch> -executeMethod FigSit.Run -figs c6,councillor -clip sit -phase 0.3 -seat "0,0.0,0.0" -out <dir> [-views side,front,play,close] [-lod 0]
//   -seat "right,up,forward": the figure root's offset from the chair's foot centre (the seat point), in the chair's frame (forward = toward the table). The chair is the one at kit (-0.72, 0).
using System; using System.IO; using System.Linq; using System.Collections.Generic;
using UnityEditor; using UnityEditor.Animations; using UnityEngine;

public static class FigSit
{
    static string Arg(string n, string d = "") { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }

    static Material Mat(string name)
    {
        var m = new Material(Shader.Find("Standard")) { name = name }; m.SetFloat("_Glossiness", 0.15f);
        string Find(string suf) => AssetDatabase.FindAssets($"{name}_{suf}").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => !p.EndsWith(".meta") && Path.GetFileNameWithoutExtension(p) == $"{name}_{suf}");
        var a = Find("albedo"); if (a != null) m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(a));
        return m;
    }

    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var figs = Arg("-figs", "c6").Split(','); var clips = Arg("-clip", "sit").Split(','); float phase = float.Parse(Arg("-phase", "0.3")); int lod = int.Parse(Arg("-lod", "0"));
        var seat = Arg("-seat", "0,0,0").Split(',').Select(float.Parse).ToArray(); var outDir = Arg("-out", "/tmp/fm/sit"); Directory.CreateDirectory(outDir);
        var views = Arg("-views", "side,front,play").Split(',');
        bool table = Arg("-table", "1") == "1";
        foreach (var clipName in clips) foreach (var id in figs)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.localScale = Vector3.one * 5; ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.78f, 0.76f, 0.70f) };
            var light = new GameObject("sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            GameObject set = null;
            if (table)
            {
                set = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kit/DK_CafeSet_Tulip_Play.fbx"));
                foreach (var r in set.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(_ => new Material(Shader.Find("Standard")) { color = new Color(0.9f, 0.88f, 0.82f) }).ToArray();
            }
            var chair = new Vector3(-0.72f, 0, 0);   // kit frame; the table is at the origin; the sitter faces +x (toward the table)
            var path = $"Assets/People/Figures/{id}/{id}.fbx"; var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); var lg = go.GetComponent<LODGroup>(); if (lg) lg.enabled = false;
            foreach (var s in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { s.sharedMaterials = s.sharedMaterials.Select(m => Mat(m ? m.name : "")).ToArray(); s.enabled = s.name.EndsWith("_LOD" + lod); }
            var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.avatar = avatar; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var c = AssetDatabase.LoadAllAssetsAtPath($"Assets/People/Clips/{clipName}.fbx").OfType<AnimationClip>().First(x => !x.name.StartsWith("__preview"));
            var ctrl = new AnimatorController(); ctrl.AddLayer("l"); var sm = ctrl.layers[0].stateMachine; var st = sm.AddState("s"); st.motion = c; sm.defaultState = st;
            an.runtimeAnimatorController = ctrl; an.Rebind(); an.Play("s", 0, phase); an.Update(0.0001f); an.Update(0f);
            var fwd = new Vector3(1, 0, 0); var right = Vector3.Cross(Vector3.up, fwd);   // chair frame: forward +x, right = up x forward
            go.transform.rotation = Quaternion.Euler(0, 90, 0); go.transform.position = chair + right * seat[0] + Vector3.up * seat[1] + fwd * seat[2];
            an.Update(0f);
            // measurements, in the chair frame about the seat point: (right, up, forward)
            Vector3 Rel(Vector3 p) { var d = p - chair; return new Vector3(Vector3.Dot(d, right), d.y, Vector3.Dot(d, fwd)); }
            var hips = an.GetBoneTransform(HumanBodyBones.Hips);
            var smr = go.GetComponentsInChildren<SkinnedMeshRenderer>().First(s => s.enabled);
            var baked = new Mesh(); smr.BakeMesh(baked); var bv = baked.vertices; var bw = smr.sharedMesh.boneWeights; var bones = smr.bones; var l2w = smr.transform.localToWorldMatrix;
            var seatLow = new List<float>(); var sole = new List<float>(); var butBack = float.MaxValue; var kneeFront = float.MinValue; var backMost = float.MaxValue; float thighTop = float.MinValue;
            var hipsRel = Rel(hips.position);
            for (int i = 0; i < bv.Length; i++)
            {
                var w = bw[i]; var dom = new[] { (w.weight0, w.boneIndex0), (w.weight1, w.boneIndex1), (w.weight2, w.boneIndex2), (w.weight3, w.boneIndex3) }.OrderByDescending(x => x.Item1).First();
                string bn = bones[dom.Item2] ? bones[dom.Item2].name : ""; var p = Rel(l2w.MultiplyPoint3x4(bv[i]));
                if (bn == "Hips" || bn.EndsWith("UpLeg")) { if (Mathf.Abs(p.z - hipsRel.z) < 0.22f && Mathf.Abs(p.x - hipsRel.x) < 0.22f && p.y < hipsRel.y) seatLow.Add(p.y); if (p.z > hipsRel.z + 0.05f) thighTop = Mathf.Max(thighTop, p.y); }
                if (bn.EndsWith("Foot") || bn.EndsWith("ToeBase")) sole.Add(p.y);
                if (bn == "Hips" || bn.EndsWith("UpLeg") || bn == "Spine" || bn == "Spine1" || bn == "Spine2") backMost = Mathf.Min(backMost, p.z);
                if (bn.EndsWith("Leg") && !bn.EndsWith("UpLeg")) kneeFront = Mathf.Max(kneeFront, p.z);
            }
            seatLow.Sort(); sole.Sort();
            // mid-thigh underside: the UpLeg-dominant vertices a quarter to three quarters of the way from the hip joint to the knee (cloth that hangs below the thigh counts: it rests on the seat too)
            var uL = an.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var kL = an.GetBoneTransform(HumanBodyBones.LeftLowerLeg); var uR = an.GetBoneTransform(HumanBodyBones.RightUpperLeg); var kR = an.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            var thighY = new List<float>();
            for (int i = 0; i < bv.Length; i++)
            {
                var w = bw[i]; var dom = new[] { (w.weight0, w.boneIndex0), (w.weight1, w.boneIndex1), (w.weight2, w.boneIndex2), (w.weight3, w.boneIndex3) }.OrderByDescending(x => x.Item1).First();
                if (dom.Item1 < 0.5f) continue; var b = bones[dom.Item2]; if (b != uL && b != uR) continue;
                var a0 = b == uL ? uL.position : uR.position; var a1 = b == uL ? kL.position : kR.position; var p = l2w.MultiplyPoint3x4(bv[i]); var t = Vector3.Dot(p - a0, a1 - a0) / (a1 - a0).sqrMagnitude;
                if (t > 0.25f && t < 0.75f) thighY.Add(Rel(p).y);
            }
            thighY.Sort();
            Debug.Log($"SIT {id} {clipName} phase {phase:0.00} seat {Arg("-seat", "0,0,0")}: hips r,u,f = {hipsRel.x:0.000},{hipsRel.y:0.000},{hipsRel.z:0.000}; buttock underside y {seatLow[0]:0.000} (5% {seatLow[seatLow.Count / 20]:0.000}); sole y {sole[0]:0.000}; thigh top y {thighTop:0.000}; THIGHUNDER {thighY[0]:0.000} (5% {thighY[thighY.Count / 20]:0.000}); hipjoint y {Rel(uL.position).y:0.000} knee y {Rel(kL.position).y:0.000} f {Rel(kL.position).z:0.000} ankle y {Rel(an.GetBoneTransform(HumanBodyBones.LeftFoot).position).y:0.000}; back-most torso f {backMost:0.000}; shin front f {kneeFront:0.000}");
            foreach (var view in views)
            {
                var cam = new GameObject("cam").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.78f, 0.76f, 0.70f);
                int w = 800, h = 800; var target = chair + Vector3.up * 0.7f; cam.orthographic = true;
                if (view == "side") { cam.orthographicSize = 0.95f; cam.transform.position = target + right * 10f; cam.transform.LookAt(target); }
                else if (view == "front") { cam.orthographicSize = 0.95f; cam.transform.position = target + fwd * 10f + Vector3.up * 0.0f; cam.transform.LookAt(target); }
                else if (view == "top") { cam.orthographicSize = 0.95f; cam.transform.position = chair + Vector3.up * 10f; cam.transform.rotation = Quaternion.Euler(90, 90, 0); }
                else if (view == "play") { cam.orthographicSize = float.Parse(Arg("-zoom", "1.15")); cam.transform.rotation = Quaternion.Euler(38, 30 + 180 + float.Parse(Arg("-yaw", "0")), 0); cam.transform.position = target - cam.transform.forward * 30f; }
                else { cam.orthographicSize = 1.1f; cam.transform.rotation = Quaternion.Euler(30, 180 + 60, 0); cam.transform.position = target - cam.transform.forward * 30f; }
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.Render();
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null;
                File.WriteAllBytes($"{outDir}/{id}_{clipName}_{phase:0.00}_{view}.png", tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(cam.gameObject); rt.Release();
            }
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(light); if (set) UnityEngine.Object.DestroyImmediate(set);
        }
    }
}
