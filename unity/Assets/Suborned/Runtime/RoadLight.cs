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
        /// <summary>Another material the dusk lights (Glint 2026-10-24, the evening phase). mode "emission": the material already glows; `evening` and `night` are multipliers on its own emission (the .mat's level is 1, by day too, so day is untouched).
        /// mode "tint" (Glint 2026-10-24, the lilac table lamp read violet beside the rivals' rings): the emission leans toward `colour` (keeping its own strength) by the share `evening` / `night` (0..1; 0 by day).
        /// mode "lit": a Suborned/DecoKit/GlassSky pane (DK_GlassDark, the facade windows); `evening` and `night` are the share of panes with their interior light on (_Lit, 0 by day), `colour` the light (_LitColour).</summary>
        [Serializable] public sealed class Glowp { public string material, mode, colour; public float evening, night; }
        /// <summary>People at dusk and night (Glint 2026-10-24, Glyph's 0.2.115 check: passers-by are black cut-outs): every SkinnedMeshRenderer's materials get a runtime copy whose emission is the
        /// `colour` times the phase's level (`evening`, `night`: linear; flat, or the albedo times it when `albedo`, which keeps the texture but lifts dark clothes (albedo 0.03) by nothing), and `smoothness` (night): the figure takes the
        /// dark environment's Fresnel at its edges, a faint rim from the reflection cube's horizon band. A fill and a rim, not a light: no pass, no rim mesh. A material whose name has "visor" is left as built (the cell's visor glow). `scanSeconds`: how often new figures are found.</summary>
        [Serializable] public sealed class Peoplep { public string colour; public float evening, night, scanSeconds, smoothness; public bool albedo; }
        [Serializable] public sealed class Data { public string schema; public Lensp lens; public Poolp pool; public float easeSeconds; public Glowp[] glow; public Peoplep people; }

        public static RoadLight Instance { get; private set; }
        /// <summary>The lens level drawn now, in the .mat's own scale (1.4 day, about 3 evening, 5 night); -1 before the first scan.</summary>
        public float LensLevel { get; private set; } = -1f;
        public float PoolAlpha { get; private set; } = -1f;
        public int Renderers { get; private set; }

        Data d;
        Material lens, pool; Color lensDay; float lensDayLevel;
        bool needScan = true, scanned, first = true;
        float lensTarget, poolTarget;
        // the other materials (roadlight.json `glow`): one runtime copy each, the value eased like the strip's (emission: a multiplier from 1; lit: a share from 0)
        sealed class Glow { public Glowp g; public Material m; public Color e0; public float level0 = 1f, now = -1f, target; public bool lit, tint; }
        readonly Dictionary<string, Glow> glowByName = new();
        // people (roadlight.json `people`): one runtime copy per source material, found again every scanSeconds while the look is not day
        readonly Dictionary<Material, Material> peopleCopy = new(); readonly HashSet<Material> peopleMade = new();
        float peopleNext, peopleLevel = -1f; Color peopleTint; int peopleLogged = -1; float peopleShare;
        readonly Dictionary<Material, float> smooth0 = new(); static readonly int idSmooth = Shader.PropertyToID("_Smoothness");
        static readonly int idEmissionMap = Shader.PropertyToID("_EmissionMap"), idBaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int idLit = Shader.PropertyToID("_Lit"), idLitColour = Shader.PropertyToID("_LitColour");
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
            if (l == null || string.IsNullOrEmpty(l.material) || l.gamma < 1f || l.gamma > 3f || l.day <= 0f || l.evening < l.day || l.night < l.day || l.night > 20f || l.evening > 20f)
                return "lens: material, gamma 1..3, 0 < day <= evening and night <= 20 (night may be below evening: the dark makes a dimmer line read)";
            if (p == null || string.IsNullOrEmpty(p.material) || p.day < 0f || p.evening < p.day || p.night < p.evening || p.night > 1f) return "pool: material, 0 <= day <= evening <= night <= 1";
            if (d.easeSeconds < 0f || d.easeSeconds > 10f) return "easeSeconds 0..10";
            if (d.people != null && (!ColorUtility.TryParseHtmlString(d.people.colour, out _) || d.people.evening < 0f || d.people.night < d.people.evening || d.people.night > 1f || d.people.scanSeconds < 0.2f || d.people.scanSeconds > 10f || d.people.smoothness < 0f || d.people.smoothness > 1f)) return "people: colour #rrggbb, 0 <= evening <= night <= 1, scanSeconds 0.2..10, smoothness 0..1";
            if (d.glow != null)
                foreach (var g in d.glow)
                {
                    if (g == null || string.IsNullOrEmpty(g.material)) return "glow: material";
                    if (g.mode == "emission") { if (g.evening < 0f || g.evening > 10f || g.night < 0f || g.night > 10f) return $"glow {g.material}: emission needs evening and night in 0..10 (multipliers on the material's own emission; below 1 dims it)"; }
                    else if (g.mode == "tint") { if (g.evening < 0f || g.evening > 1f || g.night < 0f || g.night > 1f || !ColorUtility.TryParseHtmlString(g.colour, out _)) return $"glow {g.material}: tint needs evening and night in 0..1 and a #rrggbb colour"; }
                    else if (g.mode == "lit") { if (g.evening < 0f || g.night < g.evening || g.night > 1f || !ColorUtility.TryParseHtmlString(g.colour, out _)) return $"glow {g.material}: lit needs 0 <= evening <= night <= 1 and a #rrggbb colour"; }
                    else return $"glow {g.material}: mode emission | lit";
                }
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
            if (!scanned && Mathf.Approximately(lensTarget, d.lens.day) && Mathf.Approximately(poolTarget, d.pool.day) && cur == LightPhase.Phase.Day) return;
            if (needScan) Scan();
            float a = first || d.easeSeconds <= 0f ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.05f, d.easeSeconds / 3f));
            DriveGlows(cur, a);
            if (d.people != null) DrivePeople(cur, a);
            if (!lens && !pool) { first = false; return; }
            // ease toward the target (unscaled: the pause tool does not freeze the dusk); the first sight of a phase snaps, so a night launch opens lit
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
            var byName = glowByName; int glowRenderers = 0, roadRenderers = 0;
            var all = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var ms = new List<Material>();
            foreach (var r in all)
            {
                r.GetSharedMaterials(ms); bool any = false;
                for (int i = 0; i < ms.Count; i++)
                {
                    var m = ms[i]; if (!m) continue;
                    if (m.name == d.lens.material) { if (!lens) Copy(m); ms[i] = lens; any = true; roadRenderers++; }
                    else if (m.name == d.pool.material) { if (!pool) { pool = new Material(m) { name = m.name + " (phase)" }; } ms[i] = pool; any = true; roadRenderers++; }
                    else if (d.glow != null && FindGlow(m.name) is { } gp)
                    {
                        if (!byName.TryGetValue(m.name, out var gl)) { gl = MakeGlow(gp, m); byName[m.name] = gl; }
                        if (gl != null) { ms[i] = gl.m; any = true; glowRenderers++; }
                    }
                }
                if (any) { r.SetSharedMaterials(ms); Renderers++; }
            }
            if (roadRenderers == 0) Debug.Log($"RoadLight: no renderer draws {d.lens.material} or {d.pool.material} in this scene (the strip light is not laid)");
            else Debug.Log($"RoadLight: {roadRenderers} material slots on {(lens ? d.lens.material : "-")} / {(pool ? d.pool.material : "-")} (runtime copies); lens {d.lens.day} / {d.lens.evening} / {d.lens.night}, pool alpha {d.pool.day} / {d.pool.evening} / {d.pool.night}");
            var found = new List<string>(); foreach (var g in glowByName.Values) if (g != null) found.Add(g.g.material);
            Debug.Log($"RoadLight: {glowRenderers} material slots on the other dusk lights ({(found.Count > 0 ? string.Join(", ", found) : "none drawn in this scene")}; roadlight.json lists {(d.glow != null ? d.glow.Length : 0)})");
        }

        /// <summary>The people's night fill: find the figures (a SkinnedMeshRenderer each) every scanSeconds, give each source material one runtime copy with emission = albedo x colour x level, and ease the level.</summary>
        void DrivePeople(LightPhase.Phase cur, float a)
        {
            var pp = d.people; float target = cur == LightPhase.Phase.Night ? pp.night : cur == LightPhase.Phase.Evening ? pp.evening : 0f;
            if (Time.unscaledTime >= peopleNext && (target > 0f || peopleLevel > 0f))
            {
                peopleNext = Time.unscaledTime + pp.scanSeconds;
                var smrs = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include); var ms = new List<Material>();
                foreach (var r in smrs)
                {
                    r.GetSharedMaterials(ms); bool any = false;
                    for (int i = 0; i < ms.Count; i++)
                    {
                        var m = ms[i]; if (!m || peopleMade.Contains(m)) continue; // a copy of ours already
                        if (!peopleCopy.TryGetValue(m, out var c))
                        {   // a material that already glows (the cell's visors) or has no emission slot is left as built
                            if (!m.HasProperty(idEmission) || m.name.Contains("visor")) { peopleCopy[m] = null; continue; }
                            c = new Material(m) { name = m.name + " (night fill)" }; c.EnableKeyword("_EMISSION"); c.SetTexture(idEmissionMap, pp.albedo && m.HasProperty(idBaseMap) ? m.GetTexture(idBaseMap) : null);
                            if (m.HasProperty(idSmooth)) smooth0[c] = m.GetFloat(idSmooth);
                            c.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; c.SetColor(idEmission, Color.black);
                            peopleCopy[m] = c; peopleMade.Add(c); c.SetColor(idEmission, PeopleColour(Mathf.Max(peopleLevel, 0f))); if (smooth0.TryGetValue(c, out var sm0)) c.SetFloat(idSmooth, Mathf.Lerp(sm0, Mathf.Max(sm0, pp.smoothness), peopleShare));
                        }
                        if (c) { ms[i] = c; any = true; }
                    }
                    if (any) r.SetSharedMaterials(ms);
                }
                int filled = 0, own = 0; foreach (var c in peopleCopy.Values) { if (c) filled++; else own++; }
                if (filled != peopleLogged) { peopleLogged = filled; Debug.Log($"RoadLight: night fill on {filled} figure materials ({own} left as built: visors or no emission slot), {smrs.Length} skinned renderers seen"); }
            }
            if (peopleLevel < 0f) { if (target <= 0f) return; peopleLevel = 0f; } // the first look past day: ease in from nothing
            float v = Mathf.Lerp(peopleLevel, target, a); if (Mathf.Abs(v - target) < 0.0005f) v = target;
            if (!Mathf.Approximately(v, peopleLevel))
            {
                var col = PeopleColour(v);
                float share = target > 0f ? Mathf.Clamp01(v / target) : peopleShare; peopleShare = share;
                foreach (var c in peopleCopy.Values) if (c) { c.SetColor(idEmission, col); if (smooth0.TryGetValue(c, out var s0)) c.SetFloat(idSmooth, Mathf.Lerp(s0, Mathf.Max(s0, pp.smoothness), share)); }
                peopleLevel = v;
            }
        }

        Color PeopleColour(float v)
        {
            if (peopleTint == default) { ColorUtility.TryParseHtmlString(d.people.colour, out peopleTint); peopleTint = peopleTint.linear; }
            return new Color(peopleTint.r * v, peopleTint.g * v, peopleTint.b * v, 1f);
        }

        Glowp FindGlow(string name) { foreach (var g in d.glow) if (g.material == name) return g; return null; }

        /// <summary>One runtime copy of a material the dusk lights. A material the entry cannot drive (an emission entry with no emission, a lit entry on another shader) is left alone (null).</summary>
        Glow MakeGlow(Glowp g, Material m)
        {
            var gl = new Glow { g = g };
            if (g.mode == "tint")
            {
                if (!m.HasProperty(idEmission)) { Debug.LogWarning($"RoadLight: {m.name} has no _EmissionColor; left as it is"); return null; }
                gl.e0 = m.GetColor(idEmission); gl.level0 = 0f; gl.tint = true;
            }
            else if (g.mode == "lit")
            {
                if (!m.HasProperty(idLit) || !m.HasProperty(idLitColour)) { Debug.LogWarning($"RoadLight: {m.name} has no _Lit / _LitColour (not Suborned/DecoKit/GlassSky); left as it is"); return null; }
                gl.lit = true; gl.level0 = 0f;
            }
            else
            {
                if (!m.HasProperty(idEmission)) { Debug.LogWarning($"RoadLight: {m.name} has no _EmissionColor; left as it is"); return null; }
                gl.e0 = m.GetColor(idEmission);
                if (gl.e0.r + gl.e0.g + gl.e0.b <= 0.0001f) { Debug.LogWarning($"RoadLight: {m.name} has no emission to scale; left as it is"); return null; }
            }
            gl.m = new Material(m) { name = m.name + " (phase)" };
            if (gl.lit) { gl.m.SetColor(idLitColour, ColorUtility.TryParseHtmlString(g.colour, out var c) ? c : Color.white); gl.m.SetFloat(idLit, 0f); }
            else gl.m.EnableKeyword("_EMISSION");
            return gl;
        }

        /// <summary>Ease every other dusk light toward the phase's value (a = this frame's ease step) and write it when it moves.</summary>
        void DriveGlows(LightPhase.Phase cur, float a)
        {
            foreach (var gl in glowByName.Values)
            {
                if (gl == null || !gl.m) continue;
                gl.target = cur == LightPhase.Phase.Night ? gl.g.night : cur == LightPhase.Phase.Evening ? gl.g.evening : gl.level0;
                float v = gl.now < 0f ? gl.target : Mathf.Lerp(gl.now, gl.target, a);
                if (Mathf.Abs(v - gl.target) < 0.002f) v = gl.target;
                if (Mathf.Approximately(v, gl.now)) continue;
                if (gl.tint)
                {   // the emission keeps its own strength (the biggest channel) and leans toward the colour by the share v
                    var c = ColorUtility.TryParseHtmlString(gl.g.colour, out var cc) ? cc : Color.white; float mx = Mathf.Max(gl.e0.r, gl.e0.g, gl.e0.b), cm = Mathf.Max(c.r, c.g, c.b, 1e-3f);
                    gl.m.SetColor(idEmission, new Color(Mathf.Lerp(gl.e0.r, c.r * mx / cm, v), Mathf.Lerp(gl.e0.g, c.g * mx / cm, v), Mathf.Lerp(gl.e0.b, c.b * mx / cm, v), gl.e0.a));
                }
                else if (gl.lit) gl.m.SetFloat(idLit, v);
                else gl.m.SetColor(idEmission, new Color(gl.e0.r * v, gl.e0.g * v, gl.e0.b * v, gl.e0.a * v)); // linear: the kit's HDR emission colours are the light itself (only the road lens is written as (linear x level)^(1/gamma))
                gl.now = v;
            }
        }

        void Copy(Material m)
        {
            lens = new Material(m) { name = m.name + " (phase)" };
            var e = m.GetColor(idEmission); lensDay = new Color(e.r, e.g, e.b, 1f); lensDayLevel = e.a > 0f ? e.a : d.lens.day;
            if (Mathf.Abs(lensDayLevel - d.lens.day) > 0.01f) Debug.LogWarning($"RoadLight: {m.name} is written at emission {lensDayLevel} but roadlight.json lens.day is {d.lens.day}; the levels scale from the .mat's own value");
            lens.EnableKeyword("_EMISSION");
        }

        void OnDestroy() { foreach (var c in peopleCopy.Values) if (c) Destroy(c); if (lens) Destroy(lens); if (pool) Destroy(pool); foreach (var g in glowByName.Values) if (g != null && g.m) Destroy(g.m); }
    }
}
