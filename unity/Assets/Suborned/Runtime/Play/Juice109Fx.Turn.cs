using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 109 moment 4, turning someone: when a turn lands (the engine's turn.*.turned), a warm flare on the person, in the world: a soft
    /// gold halo round them that swells and fades (alpha-blended, so it reads gold on the pale stone by day) with the Shader Graph glow's warm core
    /// over it (0.9 s), a gold ring opening on the ground at their feet, and warm motes rising round them. Nothing screen-wide; it plays where it happened, on screen or not.</summary>
    public sealed partial class Juice109Fx
    {
        const int TurnFlares = 4; const float TurnS = 1.2f;
        readonly Transform[] turnFlare = new Transform[TurnFlares]; readonly float[] turnAt = new float[TurnFlares]; readonly Transform[] turnWho = new Transform[TurnFlares];
        Material turnMat; ParticleSystem motes, halo, goldRing; int turnNext; public static int Turns { get; private set; }
        static readonly Color Warm = new(1f, 0.72f, 0.36f, 1f);

        void TurnStart(Material glowSrc, Material spark, Material blend)
        {
            // in daylight on cream stone an additive glow washes to white: the warmth is an alpha-blended gold (URP's particle shader, Fx097-Blend) that
            // reads on the pale paving, with the additive core over it for the light
            if (blend)
            {
                halo = Make("turn halo", blend, 8, ParticleSystemRenderMode.Billboard); Grow(halo, 0.55f, 1.15f);
                var rm = new Material(blend) { name = "Juice109 gold ring" }; var rt = Resources.Load<Texture2D>("Juice109/juice-ring"); if (rt) rm.SetTexture("_BaseMap", rt);
                goldRing = Make("turn ring", rm, 8, ParticleSystemRenderMode.HorizontalBillboard); Grow(goldRing, 0.3f, 1f);
            }
            turnMat = glowSrc ? new Material(glowSrc) { name = "Juice109 turn flare" } : glow; if (glowSrc) turnMat.SetColor("_Tint", new Color(2.6f, 1.7f, 0.75f, 1f));
            for (int i = 0; i < TurnFlares; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Juice109 turn flare " + i; Destroy(q.GetComponent<Collider>()); q.transform.SetParent(transform, false);
                var r = q.GetComponent<MeshRenderer>(); r.sharedMaterial = turnMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; q.SetActive(false); turnFlare[i] = q.transform; turnAt[i] = -1f;
            }
            motes = Make("turn motes", spark ? spark : glow, 64, ParticleSystemRenderMode.Billboard); Grow(motes, 1f, 0.3f);
            { var m = motes.main; m.gravityModifier = -0.12f; }
        }

        /// <summary>A turn landed on the person drawn at `who` (their view's root, at their feet).</summary>
        public void Turned(Transform who)
        {
            if (!who || !turnMat) return; Turns++; if (Turns <= 10) Debug.Log($"Juice109Fx: turn {Turns} landed on {who.name} at {who.position:F1}");
            int i = turnNext; turnNext = (turnNext + 1) % TurnFlares; turnWho[i] = who; turnAt[i] = Time.unscaledTime; turnFlare[i].gameObject.SetActive(true);
            var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            var at = who.position; e.velocity = Vector3.zero;
            if (goldRing) { e.position = at + Vector3.up * 0.06f; e.startColor = new Color(0.96f, 0.6f, 0.1f, 1f); e.startSize = 3.4f; e.startLifetime = 1.1f; goldRing.Emit(e, 1); }
            if (halo) { e.position = at + Vector3.up * 1.35f; e.startColor = new Color(1f, 0.68f, 0.2f, 0.85f); e.startSize = 2.8f; e.startLifetime = 1.2f; halo.Emit(e, 1); }
            for (int k = 0; k < 14; k++)
            {
                float a = R(0f, Mathf.PI * 2f), r = R(0.25f, 0.6f);
                e.position = at + new Vector3(Mathf.Cos(a) * r, R(0.4f, 1.6f), Mathf.Sin(a) * r); e.velocity = Vector3.up * R(0.3f, 0.8f) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.15f;
                e.startSize = R(0.2f, 0.3f); e.startLifetime = R(0.9f, 1.4f); e.startColor = new Color(1f, 0.78f, 0.35f, 1f); motes.Emit(e, 1);
            }
        }

        /// <summary>LateUpdate: each flare at its person's head, facing the camera, swelling 0.6 to 2 m and fading out.</summary>
        void TurnLate(Camera cam)
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < TurnFlares; i++)
            {
                var f = turnFlare[i]; if (!f || !f.gameObject.activeSelf) continue;
                float u = (now - turnAt[i]) / TurnS; if (u >= 1f || !turnWho[i]) { f.gameObject.SetActive(false); continue; }
                float grow = 1f - (1f - u) * (1f - u), fade = u < 0.15f ? u / 0.15f : 1f - Mathf.SmoothStep(0f, 1f, (u - 0.15f) / 0.85f);
                f.position = turnWho[i].position + Vector3.up * 1.55f; if (cam) f.rotation = cam.transform.rotation;
                f.localScale = Vector3.one * Mathf.Lerp(0.6f, 2f, grow) * Mathf.Max(0.001f, fade);
            }
        }
    }
}
