using System.Collections.Generic;
using UnityEngine;

namespace Suborned
{
    /// <summary>Brief 097: pigeons and a few white doves on the Records Square (round the fountain), a look only (no rule reads them, nothing in the sim
    /// knows them). Each bird walks and pecks; anyone who comes within a couple of metres, or runs past within five, puts it up, and the birds near it
    /// go too: they beat up, wheel and settle again elsewhere on the square. One instanced draw (Suborned/Bird, a mesh made here), a small shadow.
    /// People are found as the scene's animated figures (Animator), re-read twice a second. Deterministic from its seed; its own random stream.</summary>
    public sealed class Birds097 : MonoBehaviour
    {
        const int Count = 16;
        sealed class Bird { public Vector3 p, v, target; public float yaw, phase, t, peck; public int state; public float dove; } // state 0 ground, 1 flying
        readonly List<Bird> birds = new(); readonly System.Random rng = new(1097);
        readonly List<Transform> people = new(); readonly Dictionary<Transform, Vector3> last = new();
        Mesh mesh; Material mat; Vector3 centre; float ground; float scanAt;
        Matrix4x4[] mats = new Matrix4x4[Count]; Vector4[] props = new Vector4[Count]; MaterialPropertyBlock mpb;
        static readonly int BirdId = Shader.PropertyToID("_Bird");

        public void Init(Vector3 square, float groundY)
        {
            centre = square; ground = groundY; mat = Resources.Load<Material>("Fx097/Fx097-Bird"); mesh = Make(); mpb = new MaterialPropertyBlock();
            for (int i = 0; i < Count; i++) birds.Add(new Bird { p = Spot(), yaw = R(0f, 360f), phase = R(0f, 6.28f), dove = i % 5 == 0 ? 1f : 0f, t = R(0f, 3f) });
        }

        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        Vector3 Spot()
        {   // a ring 6.5..17 m round the fountain, on whatever the ground is there (never on a parasol or a bed's leaves: a hit above 0.9 m is refused)
            for (int k = 0; k < 12; k++)
            {
                float a = R(0f, Mathf.PI * 2f), r = R(6.5f, 17f);
                var p = centre + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (Physics.Raycast(p + Vector3.up * 30f, Vector3.down, out var hit, 40f) && hit.point.y < ground + 0.9f) return new Vector3(p.x, hit.point.y, p.z);
                if (k == 11) return new Vector3(p.x, ground, p.z);
            }
            return centre;
        }

        void Update()
        {
            if (!mat || birds.Count == 0) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (Time.time >= scanAt) { scanAt = Time.time + 0.5f; Scan(); }
            foreach (var b in birds)
            {
                if (b.state == 0)
                {
                    b.t -= dt;
                    if (b.t <= 0f) { b.t = R(0.6f, 2.4f); b.yaw += R(-70f, 70f); b.peck = R(0f, 1f) < 0.5f ? 0.8f : 0f; }
                    b.peck = Mathf.Max(0f, b.peck - dt);
                    float walk = b.peck > 0f ? 0f : 0.22f;
                    var fwd = Quaternion.Euler(0f, b.yaw, 0f) * Vector3.forward;
                    var np = b.p + fwd * (walk * dt);
                    if ((np - centre).sqrMagnitude < 6f * 6f || (np - centre).sqrMagnitude > 18f * 18f) b.yaw += 180f; else b.p = np;
                    if (Threat(b.p, out var from)) Scare(b, from);
                }
                else
                {   // fly toward the target, high in the middle of the flight, then glide down
                    var to = b.target - b.p; float d = to.magnitude;
                    var want = to.normalized * Mathf.Clamp(d * 1.2f, 2.5f, 7.5f); want.y += d > 6f ? 1.6f : -0.4f;
                    b.v = Vector3.Lerp(b.v, want, dt * 2.2f); b.p += b.v * dt;
                    if (b.v.sqrMagnitude > 0.01f) b.yaw = Mathf.Atan2(b.v.x, b.v.z) * Mathf.Rad2Deg;
                    if (d < 0.35f) { b.state = 0; b.p = b.target; b.v = Vector3.zero; b.t = R(1f, 3f); }
                }
            }
            for (int i = 0; i < birds.Count; i++)
            {
                var b = birds[i]; float pitch = b.state == 0 && b.peck > 0f ? 30f * Mathf.Sin(b.peck * 12f) : b.state == 1 ? -Mathf.Clamp(b.v.y * 6f, -25f, 25f) : 0f;
                mats[i] = Matrix4x4.TRS(b.p + Vector3.up * 0.06f, Quaternion.Euler(pitch, b.yaw, 0f), Vector3.one);
                props[i] = new Vector4(b.phase, b.state == 1 ? 1f : 0f, b.dove, 0f);
            }
            mpb.SetVectorArray(BirdId, props);
            var rp = new RenderParams(mat) { matProps = mpb, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On, receiveShadows = true, layer = 0 };
            Graphics.RenderMeshInstanced(rp, mesh, 0, mats, birds.Count);
        }

        void Scan()
        {
            people.Clear();
            foreach (var a in FindObjectsByType<Animator>(FindObjectsSortMode.None))
                if (a.isActiveAndEnabled && (a.transform.position - centre).sqrMagnitude < 26f * 26f) people.Add(a.transform);
        }

        bool Threat(Vector3 p, out Vector3 from)
        {
            from = p;
            foreach (var t in people)
            {
                if (!t) continue; var q = t.position; float d2 = (new Vector3(q.x, p.y, q.z) - p).sqrMagnitude;
                last.TryGetValue(t, out var was); last[t] = q; float speed = (q - was).magnitude / Mathf.Max(Time.deltaTime, 1e-3f);
                if (d2 < 2.2f * 2.2f || (d2 < 5f * 5f && speed > 2.6f && speed < 20f)) { from = q; return true; }
            }
            return false;
        }

        void Scare(Bird b, Vector3 from)
        {
            foreach (var o in birds)
            {
                if (o.state == 1 || (o.p - b.p).sqrMagnitude > 3.5f * 3.5f) continue;
                o.state = 1; var away = (o.p - from); away.y = 0f; away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
                o.v = away * R(2f, 3.5f) + Vector3.up * R(2.5f, 4f);
                Vector3 t = Spot(); for (int k = 0; k < 4 && (t - from).sqrMagnitude < 8f * 8f; k++) t = Spot();
                o.target = t;
            }
        }

        /// <summary>A pigeon of 20 triangles, 0.32 m long: a body, a head, a tail and two wings (vertex colour r = out along the wing, g = its side, b = the neck).</summary>
        static Mesh Make()
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 d, Color ca, Color cb, Color cd) { int i = v.Count; v.Add(a); v.Add(b); v.Add(d); c.Add(ca); c.Add(cb); c.Add(cd); t.Add(i); t.Add(i + 1); t.Add(i + 2); }
            Color body = new(0, 0, 0, 1), neck = new(0, 0, 1, 1);
            Vector3 nose = new(0, 0.13f, 0.17f), crown = new(0, 0.17f, 0.12f), chest = new(0, 0.07f, 0.08f), back = new(0, 0.12f, -0.02f), belly = new(0, 0.02f, 0f), tail = new(0, 0.08f, -0.16f);
            Vector3 l = new(-0.055f, 0.08f, 0.02f), r = new(0.055f, 0.08f, 0.02f);
            foreach (var (s, side) in new[] { (l, 0f), (r, 1f) })
            {
                Tri(chest, back, s, body, body, body); Tri(belly, chest, s, body, body, body); Tri(belly, s, tail, body, body, body); Tri(back, tail, s, body, body, body);
                Tri(chest, crown, s, neck, neck, body); Tri(crown, nose, s, neck, body, body);
                var tip = new Vector3(side > 0.5f ? 0.26f : -0.26f, 0.11f, -0.04f); var root0 = new Vector3(s.x, 0.11f, 0.06f); var root1 = new Vector3(s.x, 0.11f, -0.07f);
                Tri(root0, tip, root1, new Color(0, side, 0, 1), new Color(1, side, 0, 1), new Color(0, side, 0, 1));
                var mid = new Vector3(side > 0.5f ? 0.15f : -0.15f, 0.11f, 0.05f);
                Tri(root0, mid, tip, new Color(0, side, 0, 1), new Color(0.6f, side, 0, 1), new Color(1, side, 0, 1));
            }
            Tri(back, tail, new Vector3(0.04f, 0.09f, -0.2f), body, body, body); Tri(back, new Vector3(-0.04f, 0.09f, -0.2f), tail, body, body, body);
            var m = new Mesh { name = "Bird097" }; m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); m.bounds = new Bounds(Vector3.zero, Vector3.one); return m;
        }
    }
}
