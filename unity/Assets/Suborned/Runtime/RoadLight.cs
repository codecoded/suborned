using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Suborned.Play;

namespace Suborned
{
    /// <summary>Brief 048 (Glint; Sky, 2026-10-24: "modern strip lighting around the road edges, like a futuristic city would"): the kerb strip light follows the phase of the day.
    /// The kit's DK_Road_Lens is written at the DAY emission (a pale cream line, no glow); at evening and night this raises it, and fades in DK_Road_Pool (the additive soft pool on
    /// the carriageway, Suborned/Cross Glow mode 3, alpha 0 by day). Emission plus the grade's bloom, a pool quad per batch: no light per metre, no extra pass.
    /// Levels are Resources/Atmosphere/roadlight.json (suborned.roadlight/1), read from LightPhase.Current each frame and eased (easeSeconds). Day is untouched: nothing is scanned or
    /// swapped until the look first leaves day, so day frames are byte-identical. The two materials are COPIED once and the copies assigned to the renderers that draw them (the
    /// shared .mat assets are never written: in the Editor a play-mode edit of a shared material sticks to the asset and breaks the kit's hash check). View only.</summary>
    public sealed class RoadLight : MonoBehaviour
    {
        public const string ResourcePath = "Atmosphere/roadlight";

        [Serializable] public sealed class Lensp { public string material; public float gamma, day, evening, night; }
        [Serializable] public sealed class Poolp { public string material; public float day, evening, night; }
        [Serializable] public sealed class Data { public string schema; public Lensp lens; public Poolp pool; public float easeSeconds; }

        public static RoadLight Instance { get; private set; }
        /// <summary>The lens level drawn now, in the .mat's own scale (1.4 day, about 3 evening, 5 night); -1 before the first scan.</summary>
        public float LensLevel { get; private set; } = -1f;
        public float PoolAlpha { get; private set; } = -1f;
        public int Renderers { get; private set; }

        Data d;
        Material lens, pool; Color lensDay; float lensDayLevel;
        bool needScan = true, scanned, first = true;
        float lensTarget, poolTarget;
        static readonly int idEmission = Shader.PropertyToID("_EmissionColor"), idAlpha = Shader.PropertyToID("_Alpha");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Instance) return;
            var text = Resources.Load<TextAsset>(ResourcePath);
            if (!text) { Debug.LogError($"RoadLight: Resources/{ResourcePath}.json not found; the strip light stays at its day level"); return; }
            Data d;
            try { d = JsonUtility.FromJson<Data>(text.text); } catch (Exception e) { Debug.LogError($"RoadLight: roadlight.json: {e.Message}"); return; }
            string why = Check(d);
            if (why != null) { Debug.LogError($"RoadLight: roadlight.json rejected ({why}); the strip light stays at its day level"); return; }
            var go = new GameObject("road light"); DontDestroyOnLoad(go);
            go.AddComponent<RoadLight>().d = d;
        }

        static string Check(Data d)
        {
            if (d == null) return "empty";
            if (d.schema != "suborned.roadlight/1") return "schema suborned.roadlight/1 expected";
            var l = d.lens; var p = d.pool;
            if (l == null || string.IsNullOrEmpty(l.material) || l.gamma < 1f || l.gamma > 3f || l.day <= 0f || l.evening < l.day || l.night < l.evening || l.night > 20f)
                return "lens: material, gamma 1..3, 0 < day <= evening <= night <= 20";
            if (p == null || string.IsNullOrEmpty(p.material) || p.day < 0f || p.evening < p.day || p.night < p.evening || p.night > 1f) return "pool: material, 0 <= day <= evening <= night <= 1";
            if (d.easeSeconds < 0f || d.easeSeconds > 10f) return "easeSeconds 0..10";
            return null;
        }

        void OnEnable() { Instance = this; SceneManager.sceneLoaded += OnScene; }
        void OnDisable() { if (Instance == this) Instance = null; SceneManager.sceneLoaded -= OnScene; }
        void OnScene(Scene s, LoadSceneMode m) { needScan = true; }

        void Update()
        {
            var ph = LightPhase.Instance; var cur = ph ? ph.Current : LightPhase.Phase.Day;
            lensTarget = cur == LightPhase.Phase.Night ? d.lens.night : cur == LightPhase.Phase.Evening ? d.lens.evening : d.lens.day;
            poolTarget = cur == LightPhase.Phase.Night ? d.pool.night : cur == LightPhase.Phase.Evening ? d.pool.evening : d.pool.day;
            // day and never left it: nothing to do (and nothing scanned)
            if (!scanned && Mathf.Approximately(lensTarget, d.lens.day) && Mathf.Approximately(poolTarget, d.pool.day)) return;
            if (needScan) Scan();
            if (!lens && !pool) return;
            // ease toward the target (unscaled: the pause tool does not freeze the dusk); the first sight of a phase snaps, so a night launch opens lit
            float a = first || d.easeSeconds <= 0f ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.05f, d.easeSeconds / 3f));
            float L = LensLevel < 0f ? lensTarget : Mathf.Lerp(LensLevel, lensTarget, a), P = PoolAlpha < 0f ? poolTarget : Mathf.Lerp(PoolAlpha, poolTarget, a);
            if (Mathf.Abs(L - lensTarget) < 0.005f) L = lensTarget;
            if (Mathf.Abs(P - poolTarget) < 0.0005f) P = poolTarget;
            first = false;
            if (lens && !Mathf.Approximately(L, LensLevel))
            {
                float k = Mathf.Pow(L / lensDayLevel, 1f / d.lens.gamma);
                lens.SetColor(idEmission, new Color(lensDay.r * k, lensDay.g * k, lensDay.b * k, L));
            }
            if (pool && !Mathf.Approximately(P, PoolAlpha)) pool.SetFloat(idAlpha, P);
            LensLevel = L; PoolAlpha = P;
        }

        /// <summary>Copy the two materials once and give the copies to every renderer that draws them. Runs the first time the look leaves day, and again after a scene load.</summary>
        void Scan()
        {
            needScan = false; scanned = true; Renderers = 0;
            var all = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var ms = new List<Material>();
            foreach (var r in all)
            {
                r.GetSharedMaterials(ms); bool any = false;
                for (int i = 0; i < ms.Count; i++)
                {
                    var m = ms[i]; if (!m) continue;
                    if (m.name == d.lens.material) { if (!lens) Copy(m); ms[i] = lens; any = true; }
                    else if (m.name == d.pool.material) { if (!pool) { pool = new Material(m) { name = m.name + " (phase)" }; } ms[i] = pool; any = true; }
                }
                if (any) { r.SetSharedMaterials(ms); Renderers++; }
            }
            if (Renderers == 0) Debug.Log($"RoadLight: no renderer draws {d.lens.material} or {d.pool.material} in this scene (the strip light is not laid); nothing to drive");
            else Debug.Log($"RoadLight: {Renderers} renderers on {(lens ? d.lens.material : "-")} / {(pool ? d.pool.material : "-")} (runtime copies); lens {d.lens.day} / {d.lens.evening} / {d.lens.night}, pool alpha {d.pool.day} / {d.pool.evening} / {d.pool.night}");
        }

        void Copy(Material m)
        {
            lens = new Material(m) { name = m.name + " (phase)" };
            var e = m.GetColor(idEmission); lensDay = new Color(e.r, e.g, e.b, 1f); lensDayLevel = e.a > 0f ? e.a : d.lens.day;
            if (Mathf.Abs(lensDayLevel - d.lens.day) > 0.01f) Debug.LogWarning($"RoadLight: {m.name} is written at emission {lensDayLevel} but roadlight.json lens.day is {d.lens.day}; the levels scale from the .mat's own value");
            lens.EnableKeyword("_EMISSION");
        }

        void OnDestroy() { if (lens) Destroy(lens); if (pool) Destroy(pool); }
    }
}
