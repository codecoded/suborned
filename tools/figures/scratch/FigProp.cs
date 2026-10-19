// Scratch Unity Editor script (brief 022, Forma): a figure playing a clip with a held prop on a Humanoid bone, rendered from the play camera and close views;
// prints the local offset that seats the prop level in the hand at the reference phase. Throwaway project like FigCheck (/tmp/fm/uproj, Assets/People = out/people-models/People).
// unity -batchmode -quit -projectPath <scratch> -executeMethod FigProp.Run -figs cafe_w3,cafe_w2 -clip serve -phase 0.3 -prop Assets/People/Props/tray/tray.fbx -bone RightHand
//       -pos "x,y,z" (extra local metres) -rot "x,y,z" (extra local euler) -views top,side,front,close,play -headings 0,90 -out <dir>
using System; using System.IO; using System.Linq;
using UnityEditor; using UnityEditor.Animations; using UnityEngine;

public static class FigProp
{
    static string Arg(string n, string d = "") { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }
    static Material Mat(string name)
    {
        var m = new Material(Shader.Find("Standard")) { name = name }; m.SetFloat("_Glossiness", 0.15f);
        string Find(string suf) => AssetDatabase.FindAssets($"{name}_{suf}").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => !p.EndsWith(".meta") && Path.GetFileNameWithoutExtension(p) == $"{name}_{suf}");
        var a = Find("albedo"); if (a != null) m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(a));
        return m;
    }
    static Material PropMat(string name)
    {
        var m = new Material(Shader.Find("Standard")) { name = name };
        switch (name)
        {
            case "tray_cream": m.color = new Color(0.93f, 0.88f, 0.76f); m.SetFloat("_Glossiness", 0.45f); break;
            case "tray_chrome": m.color = new Color(0.82f, 0.84f, 0.86f); m.SetFloat("_Metallic", 1f); m.SetFloat("_Glossiness", 0.85f); break;
            case "tray_brass": m.color = new Color(0.80f, 0.62f, 0.28f); m.SetFloat("_Metallic", 0.9f); m.SetFloat("_Glossiness", 0.7f); break;
            default: m.color = new Color(0.16f, 0.09f, 0.05f); m.SetFloat("_Glossiness", 0.6f); break;
        }
        return m;
    }

    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var outDir = Arg("-out", "/tmp/fm/prop"); Directory.CreateDirectory(outDir);
        string clipName = Arg("-clip", "serve"); float phase = float.Parse(Arg("-phase", "0.3"));
        var xp = Arg("-pos", "0,0,0").Split(',').Select(float.Parse).ToArray(); var xr = Arg("-rot", "0,0,0").Split(',').Select(float.Parse).ToArray();
        var boneName = (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), Arg("-bone", "RightHand"));
        var propPath = Arg("-prop", ""); var propPrefab = propPath != "" ? AssetDatabase.LoadAssetAtPath<GameObject>(propPath) : null;
        foreach (var id in Arg("-figs", "cafe_w3").Split(','))
        {
            foreach (var hs in Arg("-headings", "0").Split(','))
            {
                float heading = float.Parse(hs);
                var path = $"Assets/People/Figures/{id}/{id}.fbx"; var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = id; var lg = go.GetComponent<LODGroup>(); if (lg) lg.enabled = false;
                foreach (var s in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { s.sharedMaterials = s.sharedMaterials.Select(m => Mat(m ? m.name : "")).ToArray(); s.enabled = s.name.EndsWith("_LOD" + Arg("-lod", "0")); }
                var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.avatar = avatar; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var c = AssetDatabase.LoadAllAssetsAtPath($"Assets/People/Clips/{clipName}.fbx").OfType<AnimationClip>().First(x => !x.name.StartsWith("__preview"));
                var ctrl = new AnimatorController(); ctrl.AddLayer("l"); var sm = ctrl.layers[0].stateMachine; var st = sm.AddState("s"); st.motion = c; sm.defaultState = st;
                an.runtimeAnimatorController = ctrl; an.Rebind(); an.Play("s", 0, phase); an.Update(0.0001f); an.Update(0f);
                go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.Euler(0, heading, 0); an.Update(0f);
                var bone = an.GetBoneTransform(boneName); var fore = an.GetBoneTransform(boneName == HumanBodyBones.RightHand ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
                GameObject prop = null;
                if (propPrefab && bone)
                {
                    prop = (GameObject)PrefabUtility.InstantiatePrefab(propPrefab); prop.name = "prop";
                    foreach (var r in prop.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(m => PropMat(m ? m.name : "")).ToArray();
                    // reference seat: level, pointing along the forearm's heading, its underside centre a palm-length along the hand and 1.5 cm above its bone line
                    Vector3 fingers = (bone.position - fore.position).normalized; Vector3 flat = Vector3.ProjectOnPlane(fingers, Vector3.up).normalized;
                    var wantRot = Quaternion.LookRotation(flat, Vector3.up);
                    var wantPos = bone.position + fingers * float.Parse(Arg("-palm", "0.05")) + Vector3.up * float.Parse(Arg("-lift", "0.05"));
                    prop.transform.SetParent(bone, true); prop.transform.rotation = wantRot; prop.transform.position = wantPos;
                    var lp = prop.transform.localPosition; var lr = prop.transform.localRotation;
                    Debug.Log($"SEAT {id} {clipName} {boneName}: localPosition ({lp.x:0.0000}, {lp.y:0.0000}, {lp.z:0.0000}) localEuler ({lr.eulerAngles.x:0.0}, {lr.eulerAngles.y:0.0}, {lr.eulerAngles.z:0.0}) hand-lossy-scale {bone.lossyScale.x:0.0000} fingers-world ({fingers.x:0.00}, {fingers.y:0.00}, {fingers.z:0.00}) hand world y {bone.position.y:0.000}");
                    var seatArg = Arg("-seat", "");
                    if (seatArg != "") { var sv = seatArg.Split(',').Select(float.Parse).ToArray(); prop.transform.localPosition = new Vector3(sv[0], sv[1], sv[2]); prop.transform.localRotation = Quaternion.Euler(sv[3], sv[4], sv[5]); }
                    if (xp.Any(v => v != 0f) || xr.Any(v => v != 0f)) { prop.transform.localPosition += new Vector3(xp[0], xp[1], xp[2]); prop.transform.localRotation *= Quaternion.Euler(xr[0], xr[1], xr[2]); }
                }
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.localScale = Vector3.one * 5; ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.78f, 0.76f, 0.70f) };
                var light = new GameObject("sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(50, -30, 0);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
                Vector3 fwd = Quaternion.Euler(0, heading, 0) * Vector3.forward, right = Quaternion.Euler(0, heading, 0) * Vector3.right;
                foreach (var view in Arg("-views", "front,side,top,close").Split(','))
                {
                    var cam = new GameObject("cam").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.78f, 0.76f, 0.70f);
                    int w = 700, h = 700; var target = bone ? bone.position : new Vector3(0, 1f, 0); cam.orthographic = true; cam.orthographicSize = 0.9f;
                    if (view == "play") { cam.orthographicSize = 14f; w = 1920; h = 1080; target = new Vector3(0, 0.9f, 0); cam.transform.rotation = Quaternion.Euler(38, 30 + 180, 0); cam.transform.position = target - cam.transform.forward * 60f; }
                    else if (view == "close") { cam.orthographicSize = 0.45f; cam.transform.rotation = Quaternion.Euler(38, 30 + 180, 0); cam.transform.position = target - cam.transform.forward * 20f; }
                    else if (view == "top") { cam.orthographicSize = 0.5f; cam.transform.position = target + Vector3.up * 10f; cam.transform.rotation = Quaternion.Euler(90f, heading, 0); }
                    else if (view == "full") { cam.orthographicSize = 1.1f; target = new Vector3(0, 0.9f, 0); cam.transform.rotation = Quaternion.Euler(20, heading + 150, 0); cam.transform.position = target - cam.transform.forward * 20f; }
                    else { var dir = view == "side" ? right : fwd; cam.orthographicSize = 0.5f; cam.transform.position = target + dir * 10f; cam.transform.LookAt(target); }
                    var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.Render();
                    var tex = new Texture2D(w, h, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null;
                    if (view == "play") { int cx = w / 2, cy = h / 2, r = 90; var px = tex.GetPixels(cx - r, cy - r, 2 * r, 2 * r); tex = new Texture2D(2 * r, 2 * r, TextureFormat.RGB24, false); tex.SetPixels(px); tex.Apply(); }
                    File.WriteAllBytes($"{outDir}/{id}_{clipName}_h{heading:0}_{view}.png", tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(cam.gameObject); rt.Release();
                }
                UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(light); if (prop) UnityEngine.Object.DestroyImmediate(prop);
            }
        }
    }
}
