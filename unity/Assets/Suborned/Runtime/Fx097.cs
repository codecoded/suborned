using System.Collections.Generic;
using UnityEngine;

namespace Suborned
{
    /// <summary>Brief 097: the world's small life, made at runtime from code in the scene that holds it (Look097 adds one per scene load; no scene edit).
    /// The atom fountain (found by its pool's material, DK_Fountain_Water): the pool centre for Suborned/Fountain Water (_FountainCentre097), and at each
    /// of the 12 jets' landings (radius 2.02 m, every 30 degrees, as the water shader's rings) a splash of droplets and a breath of mist. Shuriken, two
    /// systems, two draw calls, a few hundred particles, simulated in world space. A look only. Materials: Resources/Fx097 (Editor Fx097Assets).</summary>
    public sealed partial class Fx097 : MonoBehaviour
    {
        static readonly int CentreId = Shader.PropertyToID("_FountainCentre097");
        const int Jets = 12; const float Landing = 2.02f;
        readonly System.Random rng = new(97); ParticleSystem spray, mist; Vector3 centre; float water; bool fountain; float sprayAcc, mistAcc;

        public static Fx097 Install()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-nofx") >= 0) return null; // the Air A/B: no particles, birds or event FX
            var go = new GameObject("Fx097 (fountain and street life)"); return go.AddComponent<Fx097>();
        }

        void Start()
        {
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var m = r.sharedMaterial; if (!m || !m.name.StartsWith("DK_Fountain_Water")) continue;
                var b = r.bounds; centre = new Vector3(b.center.x, b.max.y, b.center.z); water = b.max.y; fountain = true; break;
            }
            Events(); // brief 097: the play's moments (cameras, the police called)
            if (!fountain) { Shader.SetGlobalVector(CentreId, Vector4.zero); return; }
            Shader.SetGlobalVector(CentreId, new Vector4(centre.x, water, centre.z, 1f));
            var blend = Resources.Load<Material>("Fx097/Fx097-Blend"); var add = Resources.Load<Material>("Fx097/Fx097-Add");
            spray = Make("spray", blend, 420, 0.35f, 0.75f, new Vector2(0.025f, 0.06f), new Color(0.86f, 0.95f, 0.97f, 0.75f), 1.6f);
            mist = Make("mist", add, 90, 1.4f, 2.6f, new Vector2(0.35f, 0.8f), new Color(0.75f, 0.9f, 0.92f, 0.07f), -0.03f);
            var sz = mist.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 1.6f)));
            float groundY = Physics.Raycast(centre + new Vector3(9f, 30f, 0f), Vector3.down, out var gh, 40f) ? gh.point.y : water - 0.38f;
            gameObject.AddComponent<Birds097>().Init(new Vector3(centre.x, groundY, centre.z), groundY);
            Life(add, blend);
            Debug.Log($"Fx097: fountain at {centre} (water {water:F2} m, square {groundY:F2} m): {Jets} landings, spray and mist; birds; {vents.Count} vents, {beds.Count} beds shedding petals");
        }

        ParticleSystem Make(string n, Material mat, int max, float lifeMin, float lifeMax, Vector2 size, Color col, float gravity)
        {
            var go = new GameObject("Fx097 " + n); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = max; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax); main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = col; main.gravityModifier = gravity; main.startSpeed = 0f;
            var em = ps.emission; em.enabled = false; var sh = ps.shape; sh.enabled = false;
            var fade = ps.colorOverLifetime; fade.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0, 1) });
            fade.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            r.renderMode = ParticleSystemRenderMode.Billboard; r.sortingFudge = 0; r.minParticleSize = 0.0005f; r.maxParticleSize = 0.5f;
            ps.Play(); return ps;
        }

        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        int R(int a, int b) => rng.Next(a, b);
        Vector3 Ball() { Vector3 v; do v = new Vector3(R(-1f, 1f), R(-1f, 1f), R(-1f, 1f)); while (v.sqrMagnitude > 1f); return v; }

        // the street's small life: steam breathing from a few drain grates near the square, petals drifting off the flower beds down-wind
        readonly List<Vector3> vents = new(), beds = new(); ParticleSystem steam, petals; float steamAcc, petalAcc; Vector3 wind = new(0.82f, 0f, 0.57f);

        void Life(Material add, Material blend)
        {
            var grates = new List<Vector3>();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var b = r.bounds; if ((b.center - centre).sqrMagnitude > 70f * 70f) continue;
                var mf = r.GetComponent<MeshFilter>(); string n = r.name, mesh = mf && mf.sharedMesh ? mf.sharedMesh.name : "";
                if (n.Contains("DrainGrate") || n.Contains("ManholeCover") || mesh.Contains("DrainGrate") || mesh.Contains("ManholeCover")) { grates.Add(new Vector3(b.center.x, b.max.y, b.center.z)); continue; }
                if (beds.Count >= 10 || n.EndsWith("_soil")) continue;
                bool petals = n.Contains("BedPlanting") || n.EndsWith("_Planting");
                if (!petals) foreach (var m in r.sharedMaterials) if (m && m.name.StartsWith("DK_Petal")) { petals = true; break; }
                if (petals && b.size.x < 30f && b.size.z < 30f) beds.Add(new Vector3(b.center.x, b.max.y, b.center.z));
            }
            grates.Sort((x, y) => (x.x * 31f + x.z).CompareTo(y.x * 31f + y.z));
            for (int i = 0; i < grates.Count && vents.Count < 3; i += Mathf.Max(1, grates.Count / 3)) vents.Add(grates[i]);
            steam = Make("steam", blend, 160, 2.6f, 4.2f, new Vector2(0.25f, 0.45f), new Color(0.96f, 0.95f, 0.93f, 0.16f), -0.015f);
            var sz = steam.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(1, 4.5f)));
            petals = Make("petals", blend, 90, 5f, 8f, new Vector2(0.04f, 0.07f), new Color(0.98f, 0.55f, 0.45f, 0.95f), 0.012f);
            var rot = petals.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var nz = petals.noise; nz.enabled = true; nz.strength = 0.35f; nz.frequency = 0.6f; nz.scrollSpeed = 0.3f;
            var pm = petals.main; pm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.98f, 0.55f, 0.45f, 0.95f), new Color(0.95f, 0.4f, 0.62f, 0.95f));
            var wv = Shader.GetGlobalVector("_DKWind"); if (wv.x * wv.x + wv.y * wv.y > 0.01f) wind = new Vector3(wv.x, 0f, wv.y);
        }

        void Update()
        {
            PolicePulse(Time.deltaTime);
            if (!fountain) return;
            if (steam && vents.Count > 0)
            {
                steamAcc += Time.deltaTime * 5f * vents.Count; var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
                while (steamAcc >= 1f) { steamAcc -= 1f; var at = vents[R(0, vents.Count)]; e.position = at + new Vector3(R(-0.25f, 0.25f), 0.05f, R(-0.25f, 0.25f)); e.velocity = Vector3.up * R(0.35f, 0.7f) + wind * R(0.15f, 0.45f); steam.Emit(e, 1); }
            }
            if (petals && beds.Count > 0)
            {
                petalAcc += Time.deltaTime * 0.8f * beds.Count; var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
                while (petalAcc >= 1f) { petalAcc -= 1f; var at = beds[R(0, beds.Count)]; e.position = at + new Vector3(R(-1f, 1f), 0.1f, R(-1f, 1f)); e.velocity = wind * R(0.4f, 1.1f) + Vector3.up * R(0.2f, 0.5f); petals.Emit(e, 1); }
            }
            float dt = Time.deltaTime;
            sprayAcc += dt * 300f; mistAcc += dt * 26f;
            var p = new ParticleSystem.EmitParams();
            while (sprayAcc >= 1f)
            {
                sprayAcc -= 1f; int k = R(0, Jets); float a = k * Mathf.PI / 6f;
                var at = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Landing + Ball() * 0.06f; at.y = water + 0.02f;
                var outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                p.position = at; p.velocity = Vector3.up * R(0.6f, 1.35f) + Ball() * 0.45f + outward * R(-0.1f, 0.35f);
                p.applyShapeToPosition = false; spray.Emit(p, 1);
            }
            while (mistAcc >= 1f)
            {
                mistAcc -= 1f; int k = R(0, Jets); float a = k * Mathf.PI / 6f;
                var at = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Landing; at.y = water + 0.15f;
                p.position = at + Ball() * 0.15f; p.velocity = Vector3.up * R(0.05f, 0.18f) + Ball() * 0.08f;
                mist.Emit(p, 1);
            }
        }
    }
}
