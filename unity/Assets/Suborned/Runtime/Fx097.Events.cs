using System.Collections.Generic;
using UnityEngine;

namespace Suborned
{
    /// <summary>Brief 097: the play's own moments in the world, each tied to the engine's state so it tells the player something, never noise:
    ///  - a street camera put out (shot, disabled): a burst of hot sparks and a puff of smoke at its head, and sparks again where it lands;
    ///  - a camera looped by the Tech: a blue-white crackle at its lens as the loop takes, and again every couple of seconds while it holds;
    ///  - the police called (security.called): every police post's opal dome on the Handover streets pulses amber from then on.
    /// Called by the host (HostViews.HoverCam, HostCrucial). Hot sparks carry HDR colour so the play bloom (clamp 4) takes them as glints.</summary>
    public sealed partial class Fx097
    {
        public static Fx097 Live { get; private set; }
        ParticleSystem sparks, smoke; readonly List<Renderer> postLamps = new(); bool police; float policeT;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor"), BaseId = Shader.PropertyToID("_BaseColor");
        static MaterialPropertyBlock lampMpb;

        void Awake() { Live = this; }
        void OnDestroy() { if (Live == this) Live = null; }

        void Events()
        {
            var spark = Resources.Load<Material>("Fx097/Fx097-Spark") ?? Resources.Load<Material>("Fx097/Fx097-Add");
            var blend = Resources.Load<Material>("Fx097/Fx097-Blend");
            sparks = Make("sparks", spark, 200, 0.25f, 0.6f, new Vector2(0.02f, 0.045f), Color.white, 1.4f);
            var r = sparks.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.1f; r.lengthScale = 2f;
            smoke = Make("smoke", blend, 60, 1.4f, 2.4f, new Vector2(0.25f, 0.45f), new Color(0.45f, 0.44f, 0.43f, 0.35f), -0.05f);
            var sz = smoke.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 2.6f)));
            foreach (var mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (mr.name.Contains("PolicePost") && mr.name.Contains("Lamp")) postLamps.Add(mr);
        }

        /// <summary>A hover camera's state changed: 0 live, 1 looped, 2 dark.</summary>
        public static void Camera(Vector3 head, int was, int now)
        {
            var f = Live; if (!f || !f.sparks) return;
            if (!seenHead) { lastHead = head; seenHead = true; }
            Debug.Log($"Fx097: camera at {head} {(was == 0 ? "live" : was == 1 ? "looped" : "dark")} -> {(now == 0 ? "live" : now == 1 ? "looped" : "dark")}");
            if (now == 2 && was != 2) { f.Burst(head, 46, new Color(1f, 0.78f, 0.45f, 1f), 3.2f); f.Puff(head, 8); }
            else if (now == 1 && was == 0) f.Burst(head, 18, new Color(0.7f, 0.9f, 1f, 1f), 1.6f);
        }

        /// <summary>A looped camera's crackle while the loop holds (the host calls it every couple of seconds), or a dark one's sparks where it lands.</summary>
        public static void Crackle(Vector3 head, bool hot)
        {
            var f = Live; if (!f || !f.sparks) return;
            f.Burst(head, hot ? 18 : 14, hot ? new Color(1f, 0.8f, 0.5f, 1f) : new Color(0.7f, 0.9f, 1f, 1f), hot ? 1.8f : 1.2f);
        }

        public static void PoliceCalled() { var f = Live; if (f && !f.police) { f.police = true; f.policeT = 0f; Debug.Log($"Fx097: police called: {f.postLamps.Count} police-post domes pulse"); } }

        void Burst(Vector3 at, int n, Color c, float speed)
        {
            var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            // the flash: one bright billboard at the centre for a blink (HDR via the spark material, so the play bloom takes it), sized to read at play zoom
            e.position = at; e.velocity = Vector3.zero; e.startColor = c; e.startLifetime = 0.12f; e.startSize = 0.45f + n * 0.012f; sparks.Emit(e, 1);
            for (int i = 0; i < n; i++)
            {
                var d = Ball(); d.y = Mathf.Abs(d.y) * 0.8f + 0.25f;
                e.position = at + d * 0.08f; e.velocity = d.normalized * R(0.4f, 1f) * speed;
                e.startColor = c; e.startLifetime = R(0.25f, 0.6f); e.startSize = R(0.06f, 0.12f);
                sparks.Emit(e, 1);
            }
        }

        void Puff(Vector3 at, int n)
        {
            var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < n; i++) { e.position = at + Ball() * 0.12f; e.velocity = Vector3.up * R(0.2f, 0.5f) + Ball() * 0.2f; smoke.Emit(e, 1); }
        }

        // capture-only (-fxdemo): the camera moments on a fixed rhythm at the first hover camera's head, so a clip is sure to hold them; never in play
        static readonly bool Demo = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-fxdemo") >= 0;
        float demoT; int demoK; static Vector3 lastHead; static bool seenHead;
        void DemoTick(float dt)
        {
            if (!Demo) return; demoT += dt; if (demoT < 0.8f) return; demoT = 0f;
            if (!seenHead) return; var at = lastHead; // the head of the last camera whose state the host reported (each run's start reports them all)
            if (demoK++ % 2 == 0) Burst(at, 46, new Color(1f, 0.78f, 0.45f, 1f), 3.2f); else Burst(at, 14, new Color(0.7f, 0.9f, 1f, 1f), 1.6f);
            if (demoK % 4 == 1) Puff(at, 8);
            if (!police) PoliceCalled();
        }

        void PolicePulse(float dt)
        {
            DemoTick(dt);
            if (!police || postLamps.Count == 0) return;
            policeT += dt; lampMpb ??= new MaterialPropertyBlock();
            float k = 0.5f + 0.5f * Mathf.Sin(policeT * Mathf.PI * 2f * 0.9f); k = k * k;
            var amber = new Color(1f, 0.62f, 0.16f) * Mathf.Lerp(0.4f, 3.2f, k);
            foreach (var r in postLamps)
            {
                if (!r) continue;
                lampMpb.Clear(); lampMpb.SetColor(BaseId, new Color(1f, 0.86f, 0.62f)); lampMpb.SetColor(EmissionId, amber); r.SetPropertyBlock(lampMpb);
            }
        }
    }
}
