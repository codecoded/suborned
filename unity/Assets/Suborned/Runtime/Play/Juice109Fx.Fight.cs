using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 109 moment 6, gunfights, in the world at the shot (no shake, nothing screen-wide): a muzzle flash that lights the street for a
    /// frame or two (a pooled point light, no shadows, warm, 0.08 s, plus a glow at the gun), a tracer that runs from the gun to where the round
    /// went, stone chips with some weight where a miss or cover takes it, and glass where it hits a car or a camera. The HUD's own flash, miss
    /// tracer and sparks (HudToolkit.Fire) and Fx097's chip marks stay as they are. One call per engine shot (HostFight.Shot, via the host).</summary>
    public sealed partial class Juice109Fx
    {
        const int MuzzleLights = 6; const float FlashS = 0.08f, TracerMps = 140f;
        readonly Light[] muzzle = new Light[MuzzleLights]; readonly float[] muzzleAt = new float[MuzzleLights]; int muzzleNext;
        ParticleSystem tracer, chips, glass, flash; public static int Shots { get; private set; }

        void FightStart(Material blend, Material glowAdd)
        {
            for (int i = 0; i < MuzzleLights; i++)
            {
                var go = new GameObject("Juice109 muzzle light " + i); go.transform.SetParent(transform, false);
                var l = go.AddComponent<Light>(); l.type = LightType.Point; l.shadows = LightShadows.None; l.range = 9f; l.color = new Color(1f, 0.78f, 0.5f); l.intensity = 0f; l.enabled = false; muzzle[i] = l; muzzleAt[i] = -1f;
            }
            flash = Make("muzzle flash", glowAdd, 24, ParticleSystemRenderMode.Billboard); Grow(flash, 0.7f, 1.3f);
            tracer = Make("tracer", glowAdd, 48, ParticleSystemRenderMode.Stretch);
            { var r = tracer.GetComponent<ParticleSystemRenderer>(); r.velocityScale = 0.035f; r.lengthScale = 1f; var f = tracer.colorOverLifetime; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }); f.color = g; }
            chips = Make("stone chips", blend, 96, ParticleSystemRenderMode.Billboard);
            { var m = chips.main; m.gravityModifier = 1.6f; var sz = chips.sizeOverLifetime; sz.enabled = false; var rot = chips.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-9f, 9f); }
            glass = Make("glass", glowAdd, 96, ParticleSystemRenderMode.Billboard);
            { var m = glass.main; m.gravityModifier = 1.4f; var sz = glass.sizeOverLifetime; sz.enabled = false; var rot = glass.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-12f, 12f); }
        }

        /// <summary>An engine shot: from the gun (`src`) to where it went (`dst`: the body, the ground beside a miss, or the cover).</summary>
        public void Shot(Vector3 src, Vector3 dst, bool hit, bool covered, bool glassy)
        {
            if (!tracer) return; Shots++; if (Shots <= 3 || Shots % 25 == 0) Debug.Log($"Juice109Fx: shot {Shots} {(hit ? "hit" : covered ? "cover" : "miss")}{(glassy ? " glass" : "")} {src:F1} -> {dst:F1}");
            var l = muzzle[muzzleNext]; muzzleAt[muzzleNext] = Time.unscaledTime; muzzleNext = (muzzleNext + 1) % MuzzleLights;
            l.transform.position = src + Vector3.up * 0.15f; l.intensity = 7f; l.enabled = true;
            var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            e.position = src; e.velocity = Vector3.zero; e.startSize = 0.55f; e.startLifetime = 0.07f; e.startColor = new Color(1f, 0.86f, 0.6f, 1f); flash.Emit(e, 1);
            var way = dst - src; float dist = way.magnitude;
            if (dist > 0.5f) { e.position = src + way.normalized * 0.4f; e.velocity = way.normalized * TracerMps; e.startSize = 0.07f; e.startLifetime = Mathf.Max(0.03f, (dist - 0.4f) / TracerMps); e.startColor = new Color(1f, 0.92f, 0.75f, 1f); tracer.Emit(e, 1); }
            if (!hit || covered)
            {   // stone chips: grey-cream flakes kicked up and back toward the shooter, falling
                var back = -way; back.y = 0f; back = back.sqrMagnitude > 1e-4f ? back.normalized : Vector3.forward;
                for (int i = 0; i < (covered ? 10 : 7); i++)
                {
                    var d = Ball(); d.y = Mathf.Abs(d.y) + 0.6f;
                    e.position = dst + Vector3.up * 0.05f; e.velocity = (d.normalized * R(1.4f, 3.2f)) + back * R(0.5f, 1.5f);
                    e.startSize = R(0.035f, 0.08f); e.startLifetime = R(0.35f, 0.6f); float c = R(0.62f, 0.85f); e.startColor = new Color(c, c * 0.95f, c * 0.88f, 1f); e.rotation = R(0f, 360f); chips.Emit(e, 1);
                }
                e.rotation = 0f;
            }
            if (hit && glassy)
            {   // glass: pale bright shards bursting off the car or the camera and falling
                for (int i = 0; i < 12; i++)
                {
                    var d = Ball(); d.y = Mathf.Abs(d.y) * 0.8f + 0.3f;
                    e.position = dst + Ball() * 0.15f; e.velocity = d.normalized * R(1.5f, 3.5f);
                    e.startSize = R(0.03f, 0.07f); e.startLifetime = R(0.4f, 0.75f); e.startColor = new Color(0.8f, 0.95f, 1f, 1f); e.rotation = R(0f, 360f); glass.Emit(e, 1);
                }
                e.rotation = 0f;
            }
        }

        /// <summary>Per frame: the muzzle lights fade over their 0.08 s (a frame or two of lit street).</summary>
        void FightFrame()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < MuzzleLights; i++)
            {
                var l = muzzle[i]; if (!l || !l.enabled) continue;
                float u = (now - muzzleAt[i]) / FlashS; if (u >= 1f) { l.enabled = false; l.intensity = 0f; continue; }
                l.intensity = 7f * (1f - u) * (1f - u);
            }
        }
    }
}
