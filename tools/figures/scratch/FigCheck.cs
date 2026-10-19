// Scratch Unity Editor script (brief 020, Forma): renders a figure FBX at a clip phase from a chosen view with the Standard shader, and reports avatar, LOD group and bone mapping.
// Not part of the game: it lives in a throwaway project (e.g. /tmp/fm/uproj with Assets/People = out/people-models/People and this file in Assets/Editor).
// unity -batchmode -quit -projectPath <scratch> -executeMethod FigCheck.Run -figs scout,fixer -mode shots -poses idle:0.1,walk:0.3 -lod 0 -views front,close,play,three -out <dir>
using System; using System.IO; using System.Linq; using System.Collections.Generic;
using UnityEditor; using UnityEditor.Animations; using UnityEngine;

public static class FigCheck
{
    static string Arg(string n, string d = "") { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : d; }

    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var figs = Arg("-figs").Split(',', StringSplitOptions.RemoveEmptyEntries);
        var poses = Arg("-poses", "idle:0").Split(',', StringSplitOptions.RemoveEmptyEntries); // clip:fraction
        var outDir = Arg("-out", "/tmp/fm/shots"); Directory.CreateDirectory(outDir);
        var mode = Arg("-mode", "report");
        foreach (var id in figs)
        {
            var path = $"Assets/People/Figures/{id}/{id}.fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) { Debug.LogError($"FIG {id}: no model at {path}"); continue; }
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var lg = go.GetComponent<LODGroup>();
            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var b = new Bounds(); bool first = true;
            foreach (var s in smrs.Where(s => s.name.EndsWith("_LOD0"))) { if (first) { b = s.bounds; first = false; } else b.Encapsulate(s.bounds); }
            Debug.Log($"FIG {id}: avatar valid={avatar?.isValid} human={avatar?.isHuman} lodGroup={(lg ? lg.lodCount : -1)} smrs={smrs.Length} bounds y {b.min.y:0.000}..{b.max.y:0.000} (h {b.size.y:0.000}) x {b.size.x:0.00} z {b.size.z:0.00} rootRot={go.transform.rotation.eulerAngles} scale={go.transform.localScale} rootChildren={string.Join(",", Enumerable.Range(0, go.transform.childCount).Select(i => go.transform.GetChild(i).name + "@" + go.transform.GetChild(i).localScale.x))}");
            if (avatar != null && avatar.isHuman) { var hd = avatar.humanDescription; Debug.Log($"FIG {id}: human bones mapped {hd.human.Length}: " + string.Join(" ", hd.human.Select(h => h.humanName + "=" + h.boneName))); }
            if (mode == "mats")
            {
                foreach (var s0 in smrs) foreach (var m0 in s0.sharedMaterials) Debug.Log($"MAT {id} {s0.name}: {(m0 ? AssetDatabase.GetAssetPath(m0) + " shader=" + m0.shader.name + " base=" + (m0.HasProperty("_BaseMap") && m0.GetTexture("_BaseMap") ? m0.GetTexture("_BaseMap").name : "-") + " bump=" + (m0.HasProperty("_BumpMap") && m0.GetTexture("_BumpMap") ? m0.GetTexture("_BumpMap").name : "-") + " metal=" + (m0.HasProperty("_MetallicGlossMap") && m0.GetTexture("_MetallicGlossMap") ? m0.GetTexture("_MetallicGlossMap").name : "-") + " emis=" + (m0.HasProperty("_EmissionMap") && m0.GetTexture("_EmissionMap") ? m0.GetTexture("_EmissionMap").name : "-") : "NULL")}");
            }
            if (mode != "shots") { UnityEngine.Object.DestroyImmediate(go); continue; }
            // materials: Standard from the maps beside the model
            foreach (var s in smrs)
            {
                var ms = s.sharedMaterials.Select(m => Mat(id, m ? m.name : "")).ToArray(); s.sharedMaterials = ms;
            }
            if (lg) lg.enabled = false;
            foreach (var s in smrs) s.enabled = s.name.EndsWith("_LOD" + Arg("-lod", "0"));
            var animator = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); animator.avatar = avatar; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var proto = go; proto.SetActive(false);
            foreach (var p in poses)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = id; var lg2 = go.GetComponent<LODGroup>(); if (lg2) lg2.enabled = false;
                foreach (var s in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { s.sharedMaterials = s.sharedMaterials.Select(m => Mat(id, m ? m.name : "")).ToArray(); s.enabled = s.name.EndsWith("_LOD" + Arg("-lod", "0")); }
                animator = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); animator.avatar = avatar; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var kv = p.Split(':'); var clip = AssetDatabase.LoadAllAssetsAtPath($"Assets/People/Clips/{kv[0]}.fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
                Debug.Log("FIG clips in " + kv[0] + ": " + string.Join(", ", AssetDatabase.LoadAllAssetsAtPath($"Assets/People/Clips/{kv[0]}.fbx").OfType<AnimationClip>().Select(c => c.name + " " + c.length.ToString("0.00") + "s loop=" + c.isLooping + " human=" + c.isHumanMotion)));
                if (clip == null) { Debug.LogError($"FIG no clip {kv[0]}"); continue; }
                float f = kv.Length > 1 ? float.Parse(kv[1]) : 0f;
                var ctrl = new AnimatorController(); ctrl.AddLayer("l"); var sm = ctrl.layers[0].stateMachine; var st = sm.AddState("s"); st.motion = clip; sm.defaultState = st;
                animator.runtimeAnimatorController = ctrl; animator.Rebind(); animator.Play("s", 0, f); animator.Update(0.0001f); animator.Update(0f);
                Shot(go, $"{outDir}/{id}_{kv[0]}_{f:0.00}", Arg("-lod", "0"));
                UnityEngine.Object.DestroyImmediate(go);
            }
            go = proto;
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    static Material Mat(string id, string name)
    {
        var m = new Material(Shader.Find("Standard")) { name = name };
        if (Arg("-plain") == "1") { m.color = new Color(0.5f, 0.5f, 0.5f); m.SetFloat("_Glossiness", 0.1f); return m; }
        m.SetFloat("_Glossiness", Arg("-gloss") != "" ? float.Parse(Arg("-gloss")) : 0.5f);
        string Find(string suf) { var g = AssetDatabase.FindAssets($"{name}_{suf}").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => !p.EndsWith(".meta") && Path.GetFileNameWithoutExtension(p) == $"{name}_{suf}"); return g; }
        var a = Find("albedo"); if (a != null) m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(a));
        var n = Find("normal"); if (n != null) { m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(n)); m.EnableKeyword("_NORMALMAP"); }
        var mt = Find("metal"); if (mt != null) { m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(mt)); m.EnableKeyword("_METALLICGLOSSMAP"); }
        var e = Find("emissive"); if (e != null) { m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(e)); m.SetColor("_EmissionColor", Color.white); m.EnableKeyword("_EMISSION"); }
        return m;
    }

    static void Shot(GameObject go, string stem, string lod)
    {
        var views = Arg("-views", "front,play,close").Split(',');
        var light = new GameObject("sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.localScale = Vector3.one * 5; ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Arg("-ground") == "m" ? new Color(1f, 0f, 1f) : new Color(0.78f, 0.78f, 0.74f) };
        var cam = new GameObject("cam").AddComponent<Camera>(); cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.78f, 0.78f, 0.74f);
        foreach (var v in views)
        {
            int w, h; float size, pitch, yaw;
            switch (v)
            {
                case "front": w = 480; h = 640; size = 1.15f; pitch = 0; yaw = 0; break;
                case "back": w = 480; h = 640; size = 1.15f; pitch = 0; yaw = 180; break;
                case "side": w = 480; h = 640; size = 1.15f; pitch = 0; yaw = 90; break;
                case "three": w = 480; h = 640; size = 1.15f; pitch = 10; yaw = 55; break;
                case "close": w = 480; h = 640; size = 1.15f; pitch = 38; yaw = 30; break;
                default: w = 1920; h = 1080; size = 14f; pitch = 38; yaw = 30; break; // play zoom
            }
            cam.orthographicSize = size; var target = new Vector3(0, v == "play" ? 0.85f : 0.9f, 0);
            cam.transform.rotation = Quaternion.Euler(pitch, yaw + 180f, 0); // yaw 0 = camera on +Z looking toward -Z
            cam.transform.position = target - cam.transform.forward * 30f;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.Render();
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null;
            File.WriteAllBytes($"{stem}_{v}.png", tex.EncodeToPNG());
            if (v == "play")
            {
                ground.SetActive(false); cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null; ground.SetActive(true);
                var px = tex.GetPixels32(); var bg = (Color32)cam.backgroundColor; int x0 = w, x1 = -1, y0 = h, y1 = -1;
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { var c = px[y * w + x]; if (Mathf.Abs(c.r - bg.r) + Mathf.Abs(c.g - bg.g) + Mathf.Abs(c.b - bg.b) > 24) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; } }
                Debug.Log($"FIGPX {Path.GetFileName(stem)} lod{lod}: {(x1 - x0 + 1)} x {(y1 - y0 + 1)} px (figure box at 1080p, ortho 14, pitch 38)");
            }
            UnityEngine.Object.DestroyImmediate(tex); rt.Release();
        }
        UnityEngine.Object.DestroyImmediate(cam.gameObject); UnityEngine.Object.DestroyImmediate(light.gameObject); UnityEngine.Object.DestroyImmediate(ground);
    }
}
