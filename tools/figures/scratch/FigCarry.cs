// Scratch Unity Editor script (brief 020, Forma): a carrier and a carried body seated as HostViews.Figures.cs will seat them, rendered from the play
// camera (zoom 14, pitch 38, yaw 30) and a follow-like camera. Throwaway project like FigCheck (/tmp/fm/uproj, Assets/People = out/people-models/People).
// unity -batchmode -quit -projectPath <scratch> -executeMethod FigCarry.Run -carrier enforcer -victim councillor -clip carried_drape -headings 0,90,180,270
//       -seat "x,y,z" (carrier right-shoulder-relative metres: along his right, up, along his heading) -yawadd 180 -mode (old|new) -out <dir>
using System; using System.IO; using System.Linq;
using UnityEditor; using UnityEditor.Animations; using UnityEngine;

public static class FigCarry
{
    static string Arg(string n, string d = "") { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }

    static Material Mat(string name)
    {
        var m = new Material(Shader.Find("Standard")) { name = name }; m.SetFloat("_Glossiness", 0.15f);
        string Find(string suf) => AssetDatabase.FindAssets($"{name}_{suf}").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => !p.EndsWith(".meta") && Path.GetFileNameWithoutExtension(p) == $"{name}_{suf}");
        var a = Find("albedo"); if (a != null) m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(a));
        return m;
    }

    static GameObject Make(string id, string clip, float phase, int lod, Vector3 pos, float yaw)
    {
        var path = $"Assets/People/Figures/{id}/{id}.fbx"; var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = id; var lg = go.GetComponent<LODGroup>(); if (lg) lg.enabled = false;
        foreach (var s in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { s.sharedMaterials = s.sharedMaterials.Select(m => Mat(m ? m.name : "")).ToArray(); s.enabled = s.name.EndsWith("_LOD" + lod); }
        var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.avatar = avatar; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var c = AssetDatabase.LoadAllAssetsAtPath($"Assets/People/Clips/{clip}.fbx").OfType<AnimationClip>().First(x => !x.name.StartsWith("__preview"));
        var ctrl = new AnimatorController(); ctrl.AddLayer("l"); var sm = ctrl.layers[0].stateMachine; var st = sm.AddState("s"); st.motion = c; sm.defaultState = st;
        an.runtimeAnimatorController = ctrl; an.Rebind(); an.Play("s", 0, phase); an.Update(0.0001f); an.Update(0f);
        go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        return go;
    }

    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string carrier = Arg("-carrier", "enforcer"), victim = Arg("-victim", "councillor"), clip = Arg("-clip", "carried_drape"), outDir = Arg("-out", "/tmp/fm/carry"), mode = Arg("-mode", "new");
        var seat = Arg("-seat", "0.10,0.10,0").Split(',').Select(float.Parse).ToArray(); float yawAdd = float.Parse(Arg("-yawadd", "180"));
        string carryClip = Arg("-carrierclip", "walk"); float cph = float.Parse(Arg("-carrierphase", "0.3"));
        var views = Arg("-views", "play,close,follow").Split(',');
        Directory.CreateDirectory(outDir);
        foreach (var hs in Arg("-headings", "0").Split(','))
        {
            float heading = float.Parse(hs);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.localScale = Vector3.one * 5; ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.78f, 0.76f, 0.70f) };
            var light = new GameObject("sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var car = Make(carrier, carryClip, cph, 2 > 0 ? int.Parse(Arg("-lod", "0")) : 0, Vector3.zero, heading);
            var v = Make(victim, clip, float.Parse(Arg("-vphase", "0.3")), int.Parse(Arg("-lod", "0")), Vector3.zero, heading);
            var can = car.GetComponent<Animator>(); var van = v.GetComponent<Animator>();
            var rs = can.GetBoneTransform(HumanBodyBones.RightShoulder); var ls = can.GetBoneTransform(HumanBodyBones.LeftShoulder);
            Vector3 fwd = Quaternion.Euler(0, heading, 0) * Vector3.forward, right = Quaternion.Euler(0, heading, 0) * Vector3.right;
            var hips = van.GetBoneTransform(HumanBodyBones.Hips);
            if (mode == "old") // Latch today: centred between the shoulder bones, turned 90, hips 0.14 above the shoulder line
            {
                float sh = (ls.position.y + rs.position.y) / 2f; v.transform.rotation = Quaternion.Euler(0, heading + 90f, 0);
                v.transform.position = car.transform.position; var want = new Vector3(car.transform.position.x, sh + 0.14f, car.transform.position.z); v.transform.position += want - hips.position;
            }
            else
            {
                v.transform.rotation = Quaternion.Euler(0, heading + yawAdd, 0);
                var sb = Arg("-side", "right") == "left" ? ls : rs; var want = sb.position + right * seat[0] + Vector3.up * seat[1] + fwd * seat[2]; v.transform.position += want - hips.position;
            }
            if (Arg("-probe", "") != "")
            {
                Vector3 Rel(Transform t) { var d = t.position - car.transform.position; return new Vector3(Vector3.Dot(d, right), d.y, Vector3.Dot(d, fwd)); }
                foreach (var hb in new[] { HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.LeftUpperArm, HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Neck })
                { var t = can.GetBoneTransform(hb); if (t) Debug.Log($"PROBE carrier {hb} r,u,f = {Rel(t).x:0.000},{Rel(t).y:0.000},{Rel(t).z:0.000}"); }
                foreach (var hb in new[] { HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot })
                { var t = van.GetBoneTransform(hb); if (t) Debug.Log($"PROBE carried {hb} r,u,f = {Rel(t).x:0.000},{Rel(t).y:0.000},{Rel(t).z:0.000}"); }
            }
            if (Arg("-stretch", "") != "")   // edge stretch of the carried figure's LOD mesh in this pose (posed length / rest length): where a skin tears
            {
                foreach (var smr in v.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!smr.enabled) continue;
                    var baked = new Mesh(); smr.BakeMesh(baked); var rest = smr.sharedMesh; var rv = rest.vertices; var bv = baked.vertices; var tri = rest.triangles; var bw = rest.boneWeights;
                    var edges = new System.Collections.Generic.HashSet<long>(); var list = new System.Collections.Generic.List<(float r, int a, int b)>();
                    for (int i = 0; i < tri.Length; i += 3) for (int k = 0; k < 3; k++)
                    {
                        int a = tri[i + k], b = tri[i + (k + 1) % 3]; long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; if (!edges.Add(key)) continue;
                        float l0 = (rv[a] - rv[b]).magnitude, l1 = (bv[a] - bv[b]).magnitude; if (l0 > 1e-4f) list.Add((l1 / l0, a, b));
                    }
                    list.Sort((x, y) => y.r.CompareTo(x.r));
                    string W(int i) { var w = bw[i]; var bs = smr.bones; return $"{(bs[w.boneIndex0] ? bs[w.boneIndex0].name : "?")}:{w.weight0:0.00} {(w.weight1 > 0 ? bs[w.boneIndex1].name + ":" + w.weight1.ToString("0.00") : "")} {(w.weight2 > 0 ? bs[w.boneIndex2].name + ":" + w.weight2.ToString("0.00") : "")} {(w.weight3 > 0 ? bs[w.boneIndex3].name + ":" + w.weight3.ToString("0.00") : "")}"; }
                    Debug.Log($"STRETCH {smr.name} edges {list.Count} over2 {list.Count(e => e.r > 2)} over3 {list.Count(e => e.r > 3)} over5 {list.Count(e => e.r > 5)}");
                    foreach (var e in list.Take(int.Parse(Arg("-stretch", "20")))) Debug.Log($"STRETCH x{e.r:0.0} a{e.a} rest({rv[e.a].x:0.00},{rv[e.a].y:0.00},{rv[e.a].z:0.00}) {W(e.a)} | b{e.b} rest({rv[e.b].x:0.00},{rv[e.b].y:0.00},{rv[e.b].z:0.00}) {W(e.b)}");
                }
            }
            foreach (var view in views)
            {
                var cam = new GameObject("cam").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.78f, 0.76f, 0.70f);
                int w = 1920, h = 1080; var target = new Vector3(0, 0.9f, 0);
                if (view == "play") { cam.orthographic = true; cam.orthographicSize = 14f; cam.transform.rotation = Quaternion.Euler(38, 30 + 180, 0); cam.transform.position = target - cam.transform.forward * 60f; }
                else if (view == "side" || view == "sideL" || view == "top" || view == "front")
                {
                    cam.orthographic = true; cam.orthographicSize = 1.15f; w = 700; h = 700;
                    var dir = view == "side" ? right : view == "sideL" ? -right : view == "front" ? fwd : Vector3.zero;
                    if (view == "top") { cam.transform.position = new Vector3(0, 12f, 0); cam.transform.rotation = Quaternion.Euler(90f, heading, 0); }
                    else { cam.transform.position = new Vector3(0, 0.95f, 0) + dir * 12f; cam.transform.LookAt(new Vector3(0, 0.95f, 0)); }
                }
                else if (view.StartsWith("hip"))   // hip, hipL, hipB, hipF, hipT: tight ortho on the carried hips (Forma, brief 022 tear check)
                {
                    cam.orthographic = true; cam.orthographicSize = 0.45f; w = 900; h = 900; var hp = hips.position;
                    Vector3 dir = view == "hipL" ? -right : view == "hipB" ? -fwd : view == "hipF" ? fwd : right;
                    if (view == "hipT") { cam.transform.position = hp + Vector3.up * 6f; cam.transform.rotation = Quaternion.Euler(90f, heading, 0); }
                    else { cam.transform.position = hp + dir * 6f; cam.transform.LookAt(hp); }
                }
                else if (view == "close") { cam.orthographic = true; cam.orthographicSize = 1.6f; cam.transform.rotation = Quaternion.Euler(38, 30 + 180, 0); cam.transform.position = target - cam.transform.forward * 30f; w = 800; h = 800; }
                else { cam.orthographic = false; cam.fieldOfView = 40; var back = Quaternion.Euler(0, heading, 0) * new Vector3(0.6f, 0, -1f).normalized; cam.transform.position = new Vector3(0, 0, 0) + back * 4.2f + Vector3.up * 2.4f; cam.transform.LookAt(new Vector3(0, 1.0f, 0)); w = 1000; h = 700; }
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.Render();
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null;
                if (view == "play") { int cx = w / 2, cy = h / 2, r = 90; var px = tex.GetPixels(cx - r, cy - r, 2 * r, 2 * r); tex = new Texture2D(2 * r, 2 * r, TextureFormat.RGB24, false); tex.SetPixels(px); tex.Apply(); }
                File.WriteAllBytes($"{outDir}/{mode}_{victim}_h{heading:0}_{view}.png", tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(cam.gameObject); rt.Release();
            }
            UnityEngine.Object.DestroyImmediate(car); UnityEngine.Object.DestroyImmediate(v); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(light);
        }
    }
}
