using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Suborned
{
    /// <summary>Brief 097: the city's wind and cloud shadows, in every scene with a sun (no scene edit: it installs itself at load).
    /// Wind: DK_Wind097.hlsl's globals (_DKWind, _DKWindGust), read by every material with _DK_WIND in all its passes.
    /// Clouds: a tiling soft noise cookie (made once here, deterministic from its seed) on the main directional light, its offset drifting down-wind,
    /// so sunlit ground, walls and people darken a little as a cloud's shadow passes: URP's own main-light cookie (_LIGHT_COOKIES), one texture fetch
    /// per lit pixel. A look only: no rule reads the light (GDD §15). Values: Resources/Atmosphere/look097.json; -nowind / -noclouds turn either off.</summary>
    public sealed class Look097 : MonoBehaviour
    {
        [Serializable] sealed class WindCfg { public float dirDeg = 35, height = 4, speed = 1.1f, gust = 0.55f, gustScale = 0.045f, flutter = 9, strength = 1; }
        [Serializable] sealed class CloudCfg { public bool enabled = true; public float size = 90, drift = 1.4f, cover = 0.38f, soft = 0.2f, shade = 0.3f; public int seed = 97, res = 256; }
        [Serializable] sealed class RoomCfg { public float width = 3.6f, storey = 3.4f, depth = 4.5f, day = 0.55f, nightShare = 0f, night = 0f; }
        [Serializable] sealed class Cfg { public WindCfg wind = new(); public CloudCfg clouds = new(); public RoomCfg rooms = new(); public float bakedContact = 0.55f, bakedExposure = 0.4f; public float variation = 0.85f, variationTile = 9f; }

        static readonly int WindId = Shader.PropertyToID("_DKWind"), GustId = Shader.PropertyToID("_DKWindGust");
        static Look097 instance;
        Volume exposureVolume;
        Cfg cfg; Texture2D cookie, wash; Light sun; UniversalAdditionalLightData sunData; Vector2 dir; bool noWind, noClouds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance) return;
            var go = new GameObject("Look097 (wind, clouds)") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
            DontDestroyOnLoad(go); instance = go.AddComponent<Look097>();
        }

        void Awake()
        {
            var text = Resources.Load<TextAsset>("Atmosphere/look097");
            cfg = text ? JsonUtility.FromJson<Cfg>(text.text) : new Cfg();
            var args = Environment.GetCommandLineArgs();
            noWind = Array.IndexOf(args, "-nowind") >= 0; noClouds = Array.IndexOf(args, "-noclouds") >= 0 || !cfg.clouds.enabled;
            float a = cfg.wind.dirDeg * Mathf.Deg2Rad; dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Shader.SetGlobalVector(WindId, new Vector4(dir.x, dir.y, cfg.wind.height, cfg.wind.speed));
            Shader.SetGlobalVector(GustId, new Vector4(cfg.wind.gust, cfg.wind.gustScale, cfg.wind.flutter, noWind ? 0f : cfg.wind.strength));
            var rc = cfg.rooms; Shader.SetGlobalVector("_DKRooms", new Vector4(rc.width, rc.storey, rc.depth, rc.day)); Shader.SetGlobalVector("_DKRoomsNight", new Vector4(rc.nightShare, rc.night, 0, 0)); // DK_Interior097.hlsl
            if (Array.IndexOf(args, "-norooms") >= 0) { Shader.SetGlobalFloat("_DKInteriorOff", 1f); Shader.SetGlobalFloat("_DKInteriorOff097", 1f); }
            if (Array.IndexOf(args, "-noao") >= 0) Shader.SetGlobalFloat("_DKVertexAOOff", 1f);
            // DK_Variation097: the painted wash, made here in linear (an imported PNG went through sRGB and darkened the city ~9 %). Kept in a field and
            // marked DontUnloadUnusedAsset: as a bare local it was unloaded with the play scene's load (UnloadUnusedAssets), the global then sampled
            // Unity's default grey and the wash darkened the whole zone by about a quarter instead of averaging 1 (Sky 2026-11-06: "feels colder")
            wash = Wash(); if (wash) wash.hideFlags = HideFlags.DontUnloadUnusedAsset;
            bool noVar = Array.IndexOf(args, "-novariation") >= 0 || !wash;
            if (wash) Shader.SetGlobalTexture("_DKVariation097", wash);
            Shader.SetGlobalVector("_DKVariationParams097", new Vector4(1f / Mathf.Max(cfg.variationTile, 0.5f), noVar ? 0f : cfg.variation, 0f, 0f));
            if (Array.IndexOf(args, "-nogrd") >= 0 && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp) { urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled; Debug.Log("Look097: GPU Resident Drawer off (-nogrd)"); }
            SceneManager.sceneLoaded += (_, _) => { sun = null; if (wash) Shader.SetGlobalTexture("_DKVariation097", wash); Fx097.Install(); Baked(); };
            Baked();
            Fx097.Install(); // the scene already open at boot
            Debug.Log($"Look097: wind {(noWind ? "off" : $"{cfg.wind.strength} from {cfg.wind.dirDeg} deg")}, clouds {(noClouds ? "off" : $"{cfg.clouds.size} m tiles, shade {cfg.clouds.shade}")}");
        }

        /// <summary>A scene with the zone's lightmaps (ZoneBake097) holds the occlusion there already: the contact strips go to bakedContact of their strength.</summary>
        void Baked()
        {
            bool baked = LightmapSettings.lightmaps != null && LightmapSettings.lightmaps.Length > 0;
            if (baked && Array.IndexOf(Environment.GetCommandLineArgs(), "-nolightmaps") >= 0)
            {   // A/B (and the Air's switch): every lightmapped renderer back to probe light, the look as before the zone bake
                int n = 0; foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include)) if (r.lightmapIndex >= 0 && r.lightmapIndex < 0xFFFE) { r.lightmapIndex = -1; n++; }
                Debug.Log($"Look097: -nolightmaps: {n} renderers back to probe light"); baked = false;
            }
            Shader.SetGlobalFloat("_ContactScale097", baked ? cfg.bakedContact : 1f);
            // the baked zone keeps its sun-to-shade contrast in the lightmaps; the level the flat ambient used to add everywhere comes back as exposure
            // (post exposure only, overriding nothing else in the play grade; signals draw outside the grade)
            if (baked && cfg.bakedExposure != 0f && !exposureVolume)
            {
                var go = new GameObject("Look097 baked exposure"); var v = go.AddComponent<Volume>(); v.isGlobal = true; v.priority = 1000f;
                foreach (var other in FindObjectsByType<Volume>()) if (other != v && other.isGlobal) { go.layer = other.gameObject.layer; break; }
                var prof = ScriptableObject.CreateInstance<VolumeProfile>(); var ca = prof.Add<ColorAdjustments>(false); ca.postExposure.Override(cfg.bakedExposure);
                v.sharedProfile = prof; exposureVolume = v;
                Debug.Log($"Look097: the zone is lightmapped: contact x{cfg.bakedContact}, exposure +{cfg.bakedExposure} EV");
            }
        }

        void LateUpdate()
        {
            if (noClouds) return;
            if (!sun) Find();
            if (!sun || !sunData) return;
            // the cookie's offset in metres along the light's own axes; the drift is the wind on the ground, carried into the light's frame
            var right = sun.transform.right; var up = sun.transform.up;
            Vector2 d = dir * (cfg.clouds.drift * Time.time);
            var w = new Vector3(d.x, 0f, d.y);
            sunData.lightCookieOffset = new Vector2(Vector3.Dot(w, right), Vector3.Dot(w, up));
        }

        void Find()
        {
            sun = RenderSettings.sun;
            if (!sun) foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional && l.isActiveAndEnabled) { sun = l; break; }
            if (!sun) return;
            sunData = sun.GetComponent<UniversalAdditionalLightData>() ?? sun.gameObject.AddComponent<UniversalAdditionalLightData>();
            if (!cookie) cookie = Clouds(cfg.clouds);
            sun.cookie = cookie;
            sunData.lightCookieSize = new Vector2(cfg.clouds.size, cfg.clouds.size);
        }

        /// <summary>The painted wash: a soft sun-faded mottle (periodic value noise, four octaves, broad streaks), linear, its mean exactly 1 / 1.1 so the
        /// shader's x 1.1 averages to 1 (no surface gets darker on the whole), 256 px with mips. Deterministic.</summary>
        static Texture2D Wash()
        {
            const int N = 256; var rnd = new System.Random(4097); int[] cells = { 4, 8, 16, 32 }; var lat = new float[cells.Length][];
            for (int o = 0; o < cells.Length; o++) { int k = cells[o]; lat[o] = new float[k * k]; for (int i = 0; i < k * k; i++) lat[o][i] = (float)rnd.NextDouble(); }
            var g = new float[N * N]; double sum = 0;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float v = 0, amp = 0.55f, tot = 0;
                for (int o = 0; o < cells.Length; o++)
                {
                    int k = cells[o]; float fx = (float)x / N * k, fy = (float)y / N * k; int x0 = (int)fx, y0 = (int)fy; float tx = fx - x0, ty = fy - y0; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                    float a = lat[o][(y0 % k) * k + x0 % k], b = lat[o][(y0 % k) * k + (x0 + 1) % k], c = lat[o][((y0 + 1) % k) * k + x0 % k], d = lat[o][((y0 + 1) % k) * k + (x0 + 1) % k];
                    v += amp * Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty); tot += amp; amp *= 0.5f;
                }
                v /= tot; float streak = 0.5f + 0.5f * Mathf.Sin((x + y * 0.35f) / N * Mathf.PI * 6f + v * 4f);
                g[y * N + x] = Mathf.Lerp(0.9f, 1.08f, Mathf.SmoothStep(0f, 1f, v)) * Mathf.Lerp(0.985f, 1.015f, streak); sum += g[y * N + x];
            }
            float norm = (float)(N * N / sum) / 1.1f; var px = new Color32[N * N];
            for (int i = 0; i < px.Length; i++) { byte b8 = (byte)Mathf.Clamp(Mathf.RoundToInt(g[i] * norm * 255f), 0, 255); px[i] = new Color32(b8, b8, b8, 255); }
            var t = new Texture2D(N, N, TextureFormat.RGBA32, true, true) { name = "Look097 wash", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            t.SetPixels32(px); t.Apply(true, true); return t;
        }

        /// <summary>A tiling soft cloud mask: periodic value-noise fBm (5 octaves), cover and softness as a smoothstep, shade = what the sun keeps
        /// under a cloud's middle (1 - shade). Grey in rgb (URP's colour cookie), with mips so it never shimmers at the planning zooms.</summary>
        static Texture2D Clouds(CloudCfg c)
        {
            int n = Mathf.ClosestPowerOfTwo(Mathf.Clamp(c.res, 64, 1024));
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { name = "Look097 clouds", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 1 };
            var px = new Color32[n * n]; var rnd = new System.Random(c.seed);
            var lattice = new float[5][]; int[] cells = { 4, 8, 16, 32, 64 };
            for (int o = 0; o < 5; o++) { int k = cells[o]; lattice[o] = new float[k * k]; for (int i = 0; i < k * k; i++) lattice[o][i] = (float)rnd.NextDouble(); }
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = 0f, amp = 0.5f, sum = 0f;
                    for (int o = 0; o < 5; o++)
                    {
                        int k = cells[o]; float fx = (float)x / n * k, fy = (float)y / n * k;
                        int x0 = (int)fx, y0 = (int)fy; float tx = fx - x0, ty = fy - y0;
                        tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                        float a = lattice[o][(y0 % k) * k + x0 % k], b = lattice[o][(y0 % k) * k + (x0 + 1) % k];
                        float cc = lattice[o][((y0 + 1) % k) * k + x0 % k], d = lattice[o][((y0 + 1) % k) * k + (x0 + 1) % k];
                        v += amp * Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(cc, d, tx), ty); sum += amp; amp *= 0.5f;
                    }
                    v /= sum;
                    float cloud = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f - c.cover - c.soft, 1f - c.cover + c.soft, v));
                    byte g = (byte)Mathf.RoundToInt(255f * (1f - c.shade * cloud));
                    px[y * n + x] = new Color32(g, g, g, 255);
                }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }
    }
}
