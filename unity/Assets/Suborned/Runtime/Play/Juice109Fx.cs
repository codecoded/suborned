using UnityEngine;
using UnityEngine.VFX;

namespace Suborned.Play
{
    /// <summary>Brief 109: the world's share of the juice, all view only. An order: a dust puff where the agent sets off and a turquoise pulse where
    /// it goes (the Shader Graph glow, Generated/Juice109/Juice_Glow.shadergraph, on a ring sprite). The getaway: tyre smoke from the VFX Graph
    /// Juice_TyreSmoke (a GPU burst sent while the car pulls away), exhaust (the headlights and tail lights are Juice109Lights', round 4), and after the Go the view
    /// follows the car out until the player moves it. Shuriken where Fx097 already uses it; the car's size from its own renderers.</summary>
    [DefaultExecutionOrder(100)]
    public sealed partial class Juice109Fx : MonoBehaviour
    {
        public static HandoverHost Host { get; private set; }
        public static int Orders { get; private set; }
        public static int Launches { get; private set; }
        public static int TyreBursts { get; private set; }
        static readonly Color Turquoise = new(0.376f, 0.878f, 0.918f, 1f); // turquoise-light #60e0ea (kit.uss): the HUD's own "it took" colour

        ParticleSystem dust, ring, exhaust; Material glow; VisualEffect[] tyres; float findT;
        readonly System.Random rng = new(109);

        void Start()
        {
            var blend = Resources.Load<Material>("Fx097/Fx097-Blend"); var add = Resources.Load<Material>("Fx097/Fx097-Add");
            var ringSrc = Resources.Load<Material>("Juice109/Juice-Ring"); var glowSrc = Resources.Load<Material>("Juice109/Juice-Glow");
            var ringMat = ringSrc ? new Material(ringSrc) : add; if (ringSrc) ringMat.SetColor("_Tint", Color.white * 2.6f);
            glow = glowSrc ? new Material(glowSrc) : add; if (glowSrc) glow.SetColor("_Tint", new Color(2.3f, 2.1f, 1.8f, 1f));
            var glowAdd = glowSrc ? new Material(glowSrc) { name = "Juice109 glow (white)" } : add; if (glowSrc) glowAdd.SetColor("_Tint", Color.white * 1.6f);
            FightStart(blend, glowAdd); TurnStart(glowSrc, Resources.Load<Material>("Fx097/Fx097-Spark"), blend); // brief 109 moments 6 and 4 (Juice109Fx.Fight, .Turn)
            dust = Make("dust", blend, 160, ParticleSystemRenderMode.Billboard);
            Grow(dust, 0.7f, 2.4f);
            ring = Make("ring", ringMat, 24, ParticleSystemRenderMode.HorizontalBillboard);
            Grow(ring, 0.35f, 1f);
            exhaust = Make("exhaust", blend, 160, ParticleSystemRenderMode.Billboard);
            Grow(exhaust, 0.6f, 3.2f);
            var tyre = Resources.Load<VisualEffectAsset>("Juice109/Juice_TyreSmoke");
            if (tyre)
            {
                tyres = new VisualEffect[2];
                for (int i = 0; i < 2; i++) { var go = new GameObject("Juice109 tyre smoke " + i); go.transform.SetParent(transform, false); var v = go.AddComponent<VisualEffect>(); v.visualEffectAsset = tyre; v.initialEventName = ""; tyres[i] = v; go.SetActive(false); } // on only while a car pulls away
            }
            Debug.Log($"Juice109Fx: dust, ring ({(ringSrc ? "Shader Graph Juice_Glow" : "fallback Fx097-Add")}), exhaust, tyre smoke ({(tyre ? "VFX Graph Juice_TyreSmoke" : "none: no asset")})");
        }

        ParticleSystem Make(string n, Material mat, int max, ParticleSystemRenderMode mode)
        {
            var go = new GameObject("Juice109 " + n); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = max; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSpeed = 0f; main.useUnscaledTime = true;
            var em = ps.emission; em.enabled = false; var sh = ps.shape; sh.enabled = false;
            var fade = ps.colorOverLifetime; fade.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.5f, 0.55f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.renderMode = mode; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.minParticleSize = 0.0005f; r.maxParticleSize = 2f;
            ps.Play(); return ps;
        }
        static void Grow(ParticleSystem ps, float a, float b) { var s = ps.sizeOverLifetime; s.enabled = true; s.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, a, 0f, 2f * (b - a)), new Keyframe(1f, b, 0f, 0f))); }
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        Vector3 Ball() { Vector3 v; do v = new Vector3(R(-1f, 1f), R(-1f, 1f), R(-1f, 1f)); while (v.sqrMagnitude > 1f || v.sqrMagnitude < 1e-4f); return v; }

        /// <summary>An order taken: the puff where the agent sets off (only when it goes somewhere), the pulse where it goes; a Go arms the follow.</summary>
        public void Order(Vector3? from, Vector3? to, string verb)
        {
            Orders++;
            if (Orders <= 40) { var cam = Host ? Host.View : null; Debug.Log($"Juice109Fx: order {Orders} (alive: dust {(dust ? dust.particleCount : -1)}, ring {(ring ? ring.particleCount : -1)}, exhaust {(exhaust ? exhaust.particleCount : -1)}) {verb} from {(from.HasValue ? from.Value.ToString("F1") : "-")} to {(to.HasValue ? to.Value.ToString("F1") : "-")}{(cam && to.HasValue ? $" (screen {cam.WorldToScreenPoint(to.Value):F0})" : "")}"); }
            if (verb == "go") { followArmed = true; followT = 0f; }
            if (!dust) return;
            var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            if (from.HasValue && to.HasValue && (to.Value - from.Value).sqrMagnitude > 1.5f * 1.5f)
            {
                var f = from.Value; var away = to.Value - f; away.y = 0f; away.Normalize();
                for (int i = 0; i < 12; i++)
                {
                    float a = R(0f, Mathf.PI * 2f); var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    e.position = f + d * R(0.05f, 0.25f) + Vector3.up * 0.08f; e.velocity = (d * R(0.35f, 0.9f) - away * R(0.2f, 0.6f)) + Vector3.up * R(0.08f, 0.25f);
                    e.startSize = R(0.4f, 0.62f); e.startLifetime = R(0.6f, 0.95f); e.startColor = new Color(0.52f, 0.46f, 0.38f, 0.75f); dust.Emit(e, 1); // warm grey-brown: cream dust vanished on the cream paving
                }
            }
            if (to.HasValue)
            {
                var at = to.Value + Vector3.up * 0.06f;
                e.position = at; e.velocity = Vector3.zero; e.startColor = Turquoise; e.startSize = 3.2f; e.startLifetime = 0.6f; ring.Emit(e, 1);
                e.startSize = 2.2f; e.startLifetime = 0.85f; e.startColor = new Color(Turquoise.r, Turquoise.g, Turquoise.b, 0.7f); ring.Emit(e, 1);
            }
        }

        // ---------------------------------------------------------------- the getaway
        // `car` is the view's root (it only moves); `body` is the drawn model under it, which HostViews.Cars turns to the heading: every point on the car
        // is taken in the body's frame (the kit's front is +Z), so it moves and turns with it
        Transform car, body; Bounds carLocal; Vector3 carLast; float speed, launchT = -1f, burstT, exhaustAcc; bool wasDriving, followArmed; float followT;

        void Update()
        {
            if (!Host && (findT -= Time.unscaledDeltaTime) <= 0f) { findT = 0.5f; Host = FindAnyObjectByType<HandoverHost>(); }
            FightFrame(); HitFrame();
            var h = Host; if (!h || h.Game == null || h.Views == null) return;
            var c = h.Views.Find("car/getaway");
            if (c != car || car && !body) { car = c; body = car ? car.Find("model") ?? car : null; if (car) { carLocal = LocalBounds(body); carLast = car.position; } } // a new car, or its model rebuilt
            if (!car || !car.gameObject.activeInHierarchy) return;
            // our car drives on the engine's Drive (GetawayDriving), or leaves after the Go, which the view draws after the end (HostViews.Leaving)
            bool driving = h.Game.GetawayDriving || h.Views.GetawayLeftM > 0f || DemoDriving; DemoTick(h, Time.unscaledDeltaTime);
            float dt = Mathf.Max(1e-4f, Time.unscaledDeltaTime), simDt = h.Paused && !driving || h.Game.Over && !driving ? 0f : dt * h.Speed;
            var mv = car.position - carLast; mv.y = 0f; carLast = car.position;
            float now = simDt > 0f ? mv.magnitude / simDt : 0f; speed = Mathf.Lerp(speed, now, 1f - Mathf.Exp(-dt / 0.12f));
            if (driving && !wasDriving) { launchT = 0f; burstT = 0f; Launches++; TyresOn(true); }
            if (DemoDriving && launchT < 0f) { launchT = 0f; burstT = 0f; TyresOn(true); } // the demo: the launch's tyre smoke without end
            wasDriving = driving;
            if (launchT >= 0f) { Tyres(dt); launchT += dt; if (launchT > 1.2f) { launchT = -1f; if (tyres != null) foreach (var v in tyres) if (v) v.Stop(); tyresOffAt = Time.unscaledTime + 2.5f; } } // the template's spawner loops (2 s): stopped, or it would keep puffing
            if (tyresOffAt > 0f && Time.unscaledTime > tyresOffAt && launchT < 0f) { tyresOffAt = -1f; TyresOn(false); } // after the last puff has gone
            if (driving && (simDt > 0f || DemoDriving)) Exhaust(dt);
            if (followArmed)
            {
                followT += dt;
                if (driving) { if (!h.JuiceFollow(car.position)) followArmed = false; }
                else if (followT > 4f) { followArmed = false; h.JuiceFollowEnd(); } // the Go never drove (a wait for someone climbing in that ran out; a new Go re-arms)
            }
        }

        // capture-only (`-juicedemo order|getaway|turn|win|hit|shot`): one moment repeated on a fixed rhythm at the view's centre or the getaway car, so a perf
        // window measures what it costs while it runs (the cost table), never in play
        static readonly string Demo = GameBoot.ArgValue("-juicedemo") ?? ""; float demoT; int demoK; Transform demoSpot; static readonly string[] DemoKinds = { "turn", "paid", "misled", "call", "heard", "lent" };
        void DemoTick(HandoverHost h, float dt)
        {
            if (Demo.Length == 0 || h.View == null) return; demoT += dt;
            var ctr = h.View.transform.position + h.View.transform.forward * 60f; var ray = new Ray(h.View.transform.position, h.View.transform.forward);
            float gy = h.Views.Find("op/scout") is Transform sc ? sc.position.y : 0f; // the street's height (the play's ground is not at 0)
            if (new Plane(Vector3.up, new Vector3(0f, gy, 0f)).Raycast(ray, out var hit)) ctr = ray.GetPoint(hit);
            if (Demo == "order" && demoT >= 0.5f) { demoT = 0f; Order(ctr + new Vector3(-4f, 0f, 1f), ctr, "move"); }
            if ((Demo == "turn" || Demo == "win") && demoT >= 0.8f) { demoT = 0f; if (!demoSpot) demoSpot = new GameObject("Juice109 demo spot").transform; demoSpot.position = ctr + new Vector3(4f, 0f, -3f); SocialWin(demoSpot, Demo == "turn" ? "turn" : DemoKinds[demoK++ % DemoKinds.Length]); } // open paving beside the view's centre
            if (Demo == "hit" && demoT >= 0.25f) { demoT = 0f; HitFlash(h.Views.Find(demoK++ % 2 == 0 ? "op/scout" : "op/fixer")); }
            if (Demo == "shot" && demoT >= 0.15f) { demoT = 0f; int k = demoK++ % 3; Shot(ctr + new Vector3(-6f, 1.3f, 2f), ctr + new Vector3(R(-1f, 1f), k == 1 ? 0.05f : 1.2f, R(-1f, 1f)), k != 1, false, k == 2); }
        }
        bool DemoDriving => Demo == "getaway";

        static Bounds LocalBounds(Transform t)
        {
            var b = new Bounds(Vector3.zero, Vector3.zero); bool any = false;
            foreach (var r in t.GetComponentsInChildren<MeshRenderer>())
            {
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue; var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var p = t.InverseTransformPoint(r.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            return any && b.size.z > 1.5f ? b : new Bounds(new Vector3(0f, 0.75f, 0f), new Vector3(1.9f, 1.5f, 4.6f)); // a stand-in car's size when it has none
        }

        float tyresOffAt = -1f;
        void TyresOn(bool on) { if (tyres != null) foreach (var v in tyres) if (v && v.gameObject.activeSelf != on) v.gameObject.SetActive(on); }

        void Tyres(float dt)
        {
            if (tyres == null) return;
            burstT -= dt; if (burstT > 0f) return; burstT = 0.09f;
            for (int i = 0; i < 2; i++)
            {
                var v = tyres[i]; if (!v) continue;
                float x = (i == 0 ? -1f : 1f) * (carLocal.extents.x - 0.28f), z = carLocal.center.z - carLocal.extents.z * 0.6f; // the rear wheels
                v.transform.position = body.TransformPoint(new Vector3(carLocal.center.x + x, carLocal.min.y + 0.12f, z));
                v.SendEvent("OnPlay"); TyreBursts++;
            }
        }

        void Exhaust(float dt)
        {
            exhaustAcc += dt * 14f; var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            var pipe = body.TransformPoint(new Vector3(carLocal.center.x + carLocal.extents.x * 0.45f, carLocal.min.y + 0.32f, carLocal.center.z - (carLocal.extents.z + 0.05f)));
            while (exhaustAcc >= 1f)
            {
                exhaustAcc -= 1f;
                e.position = pipe; e.velocity = -body.forward * R(0.5f, 1.1f) + Vector3.up * R(0.12f, 0.3f) + new Vector3(R(-0.1f, 0.1f), 0f, R(-0.1f, 0.1f));
                e.startSize = R(0.16f, 0.26f); e.startLifetime = R(0.7f, 1.1f); e.startColor = new Color(0.66f, 0.66f, 0.67f, 0.3f); exhaust.Emit(e, 1);
            }
        }

        /// <summary>After every Update: the turn flares face the camera (Juice109Fx.Turn). The headlights are Juice109Lights' (round 4).</summary>
        void LateUpdate() { var cam = Host ? Host.View : null; TurnLate(cam); }
    }
}
