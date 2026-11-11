using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 109 moment 4 and round 4 (Sky: "I like that turning idea. Can use it more"): the social win, our signature for "someone is on our side",
    /// in the world at the person (or the thing) it landed on, sized and varied by kind so it never gets samey. Its parts: a warm halo of light round
    /// them (additive warm orange over the pale stone reads as warm light, never the brown a filled gold made), a thin gold ring opening at their feet
    /// (alpha-blended, the shape), and motes that rise, fall like coins or drift away. With it the done chip's bobble-head pop (HudToolkit.Juice109).
    ///   turn     a person turned (turn.*.turned): the full signature, halo, ring and a burst of rising motes
    ///   paid     a rival takes the money (rivals.paid): a smaller halo, motes that pop up and fall like coins
    ///   misled   a rival takes the false word (rivals.misled): no halo, a ring and motes drifting off the way he goes
    ///   call     a contact takes the call (call.role): two rings one after the other, a few motes
    ///   heard    a Listen learns something (*.listen.heard): small, a faint halo, a narrow ring, a few motes
    ///   watched  a Watch learns a routine (*.watch.watched): the same, at the watcher
    ///   lent     the councillor's car is lent to us (lend.lent): halo and wide ring at the car
    /// Nothing screen-wide; it plays where it happened, on screen or not.</summary>
    public sealed partial class Juice109Fx
    {
        sealed class WinStyle { public float halo, ring, ring2, motes, moteSize = 0.24f, life = 1.2f, fall, drift; public Color haloC, ringC, moteC; }
        static readonly WinStyle Turn = new() { halo = 3f, ring = 3.4f, motes = 16, haloC = new Color(1f, 0.52f, 0.12f, 0.9f), ringC = new Color(1f, 0.8f, 0.3f, 0.95f), moteC = new Color(1f, 0.78f, 0.35f, 1f) };
        static readonly WinStyle Paid = new() { halo = 2.1f, ring = 2.6f, motes = 12, fall = 1, moteSize = 0.2f, haloC = new Color(1f, 0.6f, 0.1f, 0.8f), ringC = new Color(1f, 0.84f, 0.25f, 0.95f), moteC = new Color(1f, 0.86f, 0.3f, 1f) };
        static readonly WinStyle Misled = new() { ring = 2.6f, motes = 9, drift = 1, haloC = Color.clear, ringC = new Color(1f, 0.85f, 0.45f, 0.9f), moteC = new Color(1f, 0.9f, 0.6f, 1f) };
        static readonly WinStyle Call = new() { ring = 2.2f, ring2 = 3.2f, motes = 6, haloC = Color.clear, ringC = new Color(1f, 0.82f, 0.38f, 0.95f), moteC = new Color(1f, 0.82f, 0.45f, 1f) };
        static readonly WinStyle Heard = new() { halo = 1.3f, ring = 1.7f, motes = 5, moteSize = 0.18f, life = 0.9f, haloC = new Color(1f, 0.7f, 0.3f, 0.5f), ringC = new Color(1f, 0.9f, 0.6f, 0.85f), moteC = new Color(1f, 0.92f, 0.7f, 1f) };
        static readonly WinStyle Lent = new() { halo = 2.8f, ring = 4.2f, motes = 12, haloC = new Color(1f, 0.55f, 0.15f, 0.85f), ringC = new Color(1f, 0.82f, 0.3f, 0.95f), moteC = new Color(1f, 0.8f, 0.4f, 1f) };

        ParticleSystem motes, halo, goldRing; public static int Turns { get; private set; }
        public static string LastWin { get; private set; } = "";

        void TurnStart(Material glowSrc, Material spark, Material blend)
        {
            var haloMat = glowSrc ? new Material(glowSrc) { name = "Juice109 win halo" } : glow; if (glowSrc) haloMat.SetColor("_Tint", Color.white * 1.4f); // additive: light, not paint
            halo = Make("win halo", haloMat, 16, ParticleSystemRenderMode.Billboard); Grow(halo, 0.55f, 1.15f);
            if (blend)
            {
                var rm = new Material(blend) { name = "Juice109 gold ring" }; var rt = Resources.Load<Texture2D>("Juice109/juice-ring"); if (rt) rm.SetTexture("_BaseMap", rt);
                goldRing = Make("win ring", rm, 16, ParticleSystemRenderMode.HorizontalBillboard); Grow(goldRing, 0.3f, 1f);
            }
            motes = Make("win motes", spark ? spark : glow, 96, ParticleSystemRenderMode.Billboard); Grow(motes, 1f, 0.3f); { var mm = motes.main; mm.gravityModifier = -0.12f; }
        }

        /// <summary>A turn landed on the person drawn at `who` (moment 4).</summary>
        public void Turned(Transform who) => SocialWin(who, "turn");

        /// <summary>A social win of `kind` landed on `who` (its view's root, at their feet).</summary>
        public void SocialWin(Transform who, string kind)
        {
            if (!who || !motes) return; Turns++; LastWin = kind;
            var st = kind switch { "paid" => Paid, "misled" => Misled, "call" => Call, "heard" or "watched" => Heard, "lent" => Lent, _ => Turn };
            if (Turns <= 20) Debug.Log($"Juice109Fx: social win ({kind}) on {who.name} at {who.position:F1}");
            var e = new ParticleSystem.EmitParams { applyShapeToPosition = false }; var at = who.position; e.velocity = Vector3.zero;
            if (goldRing && st.ring > 0f) { e.position = at + Vector3.up * 0.06f; e.startColor = st.ringC; e.startSize = st.ring; e.startLifetime = st.life * 0.9f; goldRing.Emit(e, 1); }
            if (goldRing && st.ring2 > 0f) { e.position = at + Vector3.up * 0.07f; e.startColor = st.ringC; e.startSize = st.ring2; e.startLifetime = st.life * 1.25f; goldRing.Emit(e, 1); }
            if (st.halo > 0f) { e.position = at + Vector3.up * 1.3f; e.startColor = st.haloC; e.startSize = st.halo; e.startLifetime = st.life; halo.Emit(e, 1); }
            var away = R(0f, Mathf.PI * 2f); var awayDir = new Vector3(Mathf.Cos(away), 0f, Mathf.Sin(away));
            for (int k = 0; k < st.motes; k++)
            {
                float a = R(0f, Mathf.PI * 2f), r = R(0.25f, 0.6f); var round = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                e.position = at + round * r + Vector3.up * R(0.4f, 1.6f);
                e.velocity = st.fall > 0f ? Vector3.up * R(1.2f, 2f) + round * R(0.3f, 0.7f)   // coins: up, then down
                           : st.drift > 0f ? awayDir * R(0.6f, 1.2f) + Vector3.up * R(0.1f, 0.3f)
                           : Vector3.up * R(0.3f, 0.8f) + round * 0.15f;
                e.startSize = st.moteSize * R(0.8f, 1.25f); e.startLifetime = st.life * R(0.75f, 1.15f); e.startColor = st.moteC; motes.Emit(e, 1);
            }
            if (st.fall > 0f) { var m = motes.main; m.gravityModifier = 0.9f; coinT = Time.unscaledTime + st.life * 1.2f; }
        }
        float coinT = -1f;

        /// <summary>LateUpdate: the motes rise again once the coins have fallen (one system serves every kind).</summary>
        void TurnLate(Camera cam) { if (coinT > 0f && Time.unscaledTime > coinT && motes) { var m = motes.main; m.gravityModifier = -0.12f; coinT = -1f; } }
    }
}
