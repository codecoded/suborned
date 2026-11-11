using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Play
{
    /// <summary>Brief 107 (Glint's rain): the falling rain, the splashes and the lamps' reflections in the wet, all in one shader (Resources/Shaders/Rain107),
    /// three static meshes built once and animated on the GPU: three draws, no particles, no CPU per drop. Drops are anchored in the world (they don't swim
    /// when the camera pans) in a box round the focus that grows with the zoom; they fall to the plan's level below (the deck and the concourse stop them by
    /// depth). The reflections are soft vertical smears laid on the ground under each lamp, toward the camera (warm cream only, never a signal's hue).
    /// Everything draws at queue 3005, before the ground rings (3010, 3011), so the rings and outlines stay on top. Look only: the unscaled clock.
    /// VFX Graph isn't in the project, and a .vfx graph can't be written from code; this is the same GPU-only idea in our own shader.</summary>
    public sealed class Rain107 : MonoBehaviour
    {
        public const int Streaks = 4000, Splashes = 700;
        static Rain107 inst; static float rain, streaks;
        public static Vector3 Focus;
        static readonly int CenterId = Shader.PropertyToID("_RainCenter"), ParamId = Shader.PropertyToID("_RainParams");
        Material mStreak, mSplash, mGlint; GameObject goStreak, goSplash, goGlint; int glintCount = -1;

        public static void Set(float rainAmount, float streakAmount)
        {
            rain = rainAmount; streaks = streakAmount;
            if (rain <= 0f && streaks <= 0f) { if (inst) inst.gameObject.SetActive(false); return; }
            if (!inst)
            {
                var sh = Resources.Load<Shader>("Shaders/Rain107");
                if (!sh || !sh.isSupported) { Debug.LogError("Rain107: no shader (Resources/Shaders/Rain107): no falling rain"); return; }
                var go = new GameObject("rain (brief 107)"); DontDestroyOnLoad(go); inst = go.AddComponent<Rain107>(); inst.Build(sh);
            }
            inst.gameObject.SetActive(true); inst.Levels();
        }

        void Build(Shader sh)
        {
            mStreak = Mat(sh, 0, "rain streaks"); mSplash = Mat(sh, 1, "rain splashes"); mGlint = Mat(sh, 2, "rain reflections");
            goStreak = Part("streaks", Quads(Streaks, 11), mStreak); goSplash = Part("splashes", Quads(Splashes, 23), mSplash);
        }

        static Material Mat(Shader sh, int mode, string name) { var m = new Material(sh) { name = name, hideFlags = HideFlags.DontSave, renderQueue = 3005 }; m.SetFloat("_Mode", mode); return m; }

        GameObject Part(string name, Mesh mesh, Material m)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        /// <summary>n quads: uv = the corner (-1..1), uv2 = four seeds per quad (a fixed hash: the same rain every run).</summary>
        static Mesh Quads(int n, int salt, List<Vector3> at = null)
        {
            var v = new Vector3[n * 4]; var uv = new Vector2[n * 4]; var s = new Vector4[n * 4]; var tri = new int[n * 6];
            var rnd = new System.Random(salt);
            for (int i = 0; i < n; i++)
            {
                var seed = new Vector4((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble());
                var p = at != null ? at[i] : Vector3.zero;
                for (int k = 0; k < 4; k++) { v[i * 4 + k] = p; uv[i * 4 + k] = new Vector2(k == 1 || k == 2 ? 1 : -1, k >= 2 ? 1 : -1); s[i * 4 + k] = seed; }
                int b = i * 4; tri[i * 6] = b; tri[i * 6 + 1] = b + 2; tri[i * 6 + 2] = b + 1; tri[i * 6 + 3] = b; tri[i * 6 + 4] = b + 3; tri[i * 6 + 5] = b + 2;
            }
            var m = new Mesh { name = "rain quads", indexFormat = n * 4 > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16, hideFlags = HideFlags.DontSave };
            m.vertices = v; m.uv = uv; m.SetUVs(1, s); m.triangles = tri; m.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            return m;
        }

        void Levels()
        {
            goStreak.SetActive(rain > 0f); goSplash.SetActive(rain > 0f);
            if (streaks > 0f && glintCount < 0)
            {   // the lamps' reflections: one smear under each lamp head PlayView107 and the LightPhase know
                var heads = PlayView107.LampHeads(); glintCount = heads.Count;
                if (heads.Count > 0) goGlint = Part("reflections", Quads(heads.Count, 37, heads), mGlint);
            }
            if (goGlint) goGlint.SetActive(streaks > 0f);
        }

        void LateUpdate()
        {
            var cam = Camera.main; float box = cam && cam.orthographic ? Mathf.Clamp(cam.orthographicSize * 4.5f, 30f, 100f) : 60f;
            Shader.SetGlobalVector(CenterId, new Vector4(Focus.x, Focus.y, Focus.z, box));
            Shader.SetGlobalVector(ParamId, new Vector4(rain, streaks, Time.unscaledTime, Weather107.Night ? 1f : 0f));
        }
    }
}
