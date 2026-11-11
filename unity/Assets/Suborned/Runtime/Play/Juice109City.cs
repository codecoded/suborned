using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 109 moment 7, the living city, each on something the city already has (found by name at start, never placed by hand):
    ///  - sky traffic: a fading light trail behind each flying car of the sky lanes (the old town's SkyTraffic units);
    ///  - neon: three of the neon star walls (DK_NeonStar_Wall) now and then stutter (a 0.5 s dip and catch, each on its own 7 to 16 s);
    ///  - the robots' faces: a soft glow at each robot waiter's face display (DK_RobotWaiter_Head_Face) and the patrol robot's, stronger at night;
    ///  - at night (the run's light): three searchlights sweep the sky from the tallest roofs round the Handover's streets.
    /// Tram pantograph sparks are not built: no tram runs in the played scene (only the rails, the shelters and a far tram's sound), and the
    /// world has no overhead wires to spark on (art bible: rails flush in the road, no overhead wires).
    /// All the new effect shaders are the Shader Graph glow (Generated/Juice109/Juice_Glow.shadergraph). Deterministic choices (sorted by name and
    /// place, a fixed seed); none of it is read by anything.</summary>
    public sealed class Juice109City : MonoBehaviour
    {
        Material glowSrc; readonly List<TrailRenderer> trails = new(); readonly List<(Transform q, Transform face)> faces = new();
        readonly List<(Material m, Color e, float next, float at)> neon = new(); readonly List<(Transform t, float yaw0, float phase)> beams = new();
        readonly System.Random rng = new(1097); float findT, faceMatK = -1f; Material faceMat, beamMat; bool night, built;
        public static int Trails, Neon, Faces, Beams;

        void Start() { glowSrc = Resources.Load<Material>("Juice109/Juice-Glow"); }

        void Update()
        {
            var h = Juice109Fx.Host; if (!h || h.Game == null || !glowSrc) return;
            if (!built) { built = true; Build(h); }
            if ((findT -= Time.unscaledDeltaTime) <= 0f) { findT = 3f; SkyTrails(); } // sky units can be (re)spawned
            float now = Time.unscaledTime;
            for (int i = 0; i < neon.Count; i++)
            {
                var (m, e, next, at) = neon[i]; if (!m) continue;
                if (now >= next) { at = now; next = now + R(7f, 16f); neon[i] = (m, e, next, at); }
                float u = (now - at) / 0.5f; float k = u < 0f || u >= 1f ? 1f : u < 0.15f ? 0.15f : u < 0.3f ? 1f : u < 0.45f ? 0.3f : u < 0.6f ? 0.9f : u < 0.7f ? 0.1f : 1f;
                m.SetColor("_EmissionColor", e * k);
            }
            bool n = PlayView107.Night; if (n != night || faceMatK < 0f) { night = n; faceMatK = n ? 1.6f : 0.8f; if (faceMat) faceMat.SetColor("_Tint", new Color(0.55f, 0.95f, 1f, 1f) * faceMatK); NightBeams(h, n); }
            for (int i = 0; i < beams.Count; i++) { var (t, yaw0, ph) = beams[i]; if (t) t.rotation = Quaternion.Euler(-62f + 6f * Mathf.Sin(now * 0.07f + ph), yaw0 + 38f * Mathf.Sin(now * 0.11f + ph), 0f); }
        }

        void LateUpdate()
        {
            var cam = Juice109Fx.Host ? Juice109Fx.Host.View : null; if (!cam) return; float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 1.7f);
            foreach (var (q, face) in faces) if (q && face) { q.rotation = cam.transform.rotation; q.localScale = Vector3.one * 0.5f * pulse / Mathf.Max(1e-4f, face.lossyScale.x); }
        }

        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        void Build(HandoverHost h)
        {
            var rs = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            // the neon stars: three of them, the same three every run (sorted by place)
            var stars = new List<MeshRenderer>(); foreach (var r in rs) if (r.name.StartsWith("DK_NeonStar_Wall")) stars.Add(r);
            stars.Sort((a, b) => (a.transform.position.x * 31f + a.transform.position.z).CompareTo(b.transform.position.x * 31f + b.transform.position.z));
            for (int i = 1; i < stars.Count && neon.Count < 3; i += 3)
            {
                var ms = stars[i].sharedMaterials; int k = -1; for (int j = 0; j < ms.Length; j++) if (ms[j] && ms[j].HasProperty("_EmissionColor") && ms[j].GetColor("_EmissionColor").maxColorComponent > 0.05f) { k = j; break; }
                if (k < 0) continue;
                var copy = new Material(ms[k]) { name = ms[k].name + " (flicker)" }; ms[k] = copy; stars[i].sharedMaterials = ms;
                neon.Add((copy, copy.GetColor("_EmissionColor"), Time.unscaledTime + R(2f, 9f), -1f));
            }
            Neon = neon.Count;
            // the robots' faces: a soft glow parented to each face display (it moves and turns with the head)
            faceMat = new Material(glowSrc) { name = "Juice109 robot face glow" };
            foreach (var r in rs)
            {
                bool face = r.name.EndsWith("Head_Face") && !r.name.Contains("_Far");
                if (!face) foreach (var m in r.sharedMaterials) if (m && h.robotFaceLed && m == h.robotFaceLed) { face = true; break; }
                if (!face) continue;
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Juice109 face glow"; Destroy(q.GetComponent<Collider>());
                var mr = q.GetComponent<MeshRenderer>(); mr.sharedMaterial = faceMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                q.transform.SetParent(r.transform, false); q.transform.position = r.bounds.center; faces.Add((q.transform, r.transform));
            }
            Faces = faces.Count;
            SkyTrails();
            Debug.Log($"Juice109City: {Neon} neon stars stutter, {Faces} robot faces glow, {Trails} sky trails; searchlights at night");
            string At(Vector3 w) { var e = h.frame.Engine(w); return $"[{e.X}, {e.Y}]"; }
            for (int i = 0; i < faces.Count && i < 4; i++) Debug.Log($"Juice109City: face glow at {At(faces[i].face.position)} ({faces[i].face.name})");
            for (int i = 0; i < neon.Count; i++) Debug.Log($"Juice109City: neon {i} at {At(stars[Mathf.Min(stars.Count - 1, 1 + 3 * i)].transform.position)}");
        }

        /// <summary>A trail on every flying unit of the sky lanes (SkyTraffic's children), added once.</summary>
        void SkyTrails()
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!mb || mb.GetType().Name != "SkyTraffic") continue;
                foreach (Transform unit in mb.transform)
                {
                    if (unit.GetComponent<TrailRenderer>()) continue;
                    var tr = unit.gameObject.AddComponent<TrailRenderer>(); tr.time = 1.1f; tr.minVertexDistance = 1.5f; tr.widthMultiplier = 0.7f;
                    tr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)); tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tr.receiveShadows = false;
                    var m = new Material(glowSrc) { name = "Juice109 sky trail" }; m.SetTexture("_BaseMap", Texture2D.whiteTexture); m.SetColor("_Tint", new Color(0.55f, 0.6f, 0.65f, 1f)); tr.sharedMaterial = m;
                    var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.7f, 0.85f, 1f), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = g;
                    trails.Add(tr);
                }
            }
            Trails = trails.Count;
        }

        /// <summary>Night: three beams from the three tallest roofs within 160 m of the view's centre whose beams rise over the far city (this tilted orthographic
        /// view sees a beam rise up the screen, so only roofs in the frame's upper part: never across the play), at least 60 m apart, sweeping slowly; day: none.</summary>
        void NightBeams(HandoverHost h, bool on)
        {
            foreach (var b in beams) if (b.t) Destroy(b.t.gameObject); beams.Clear(); Beams = 0; if (!on) return;
            var c = Vector3.zero; if (h.View) { var ray = new Ray(h.View.transform.position, h.View.transform.forward); if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var hit)) c = ray.GetPoint(hit); } c.y = 0f; // the view's ground centre
            var tops = new List<Vector3>();
            var cam = h.View;
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var b = r.bounds; if (b.size.y < 20f || (new Vector3(b.center.x, 0f, b.center.z) - c).sqrMagnitude > 160f * 160f) continue;
                var top = new Vector3(b.center.x, b.max.y, b.center.z); if (!cam) { tops.Add(top); continue; }
                var vp = cam.WorldToViewportPoint(top); if (vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.45f && vp.y < 1.0f) tops.Add(top); // a beam rises up the screen: only roofs in the frame's upper part, so the shafts rise over the far city and never cross the play (readability first)
            }
            tops.Sort((a, b) => b.y != a.y ? b.y.CompareTo(a.y) : a.x != b.x ? a.x.CompareTo(b.x) : a.z.CompareTo(b.z)); // tallest first; ties by place, the same every run
            if (!beamMat) { beamMat = new Material(glowSrc) { name = "Juice109 searchlight" }; beamMat.SetTexture("_BaseMap", Texture2D.whiteTexture); beamMat.SetColor("_Tint", new Color(1f, 0.97f, 0.9f, 1f) * 0.5f); }
            var mesh = BeamMesh();
            foreach (var p in tops)
            {
                if (beams.Count >= 3) break; bool near = false; foreach (var b in beams) if ((b.t.position - p).sqrMagnitude < 60f * 60f) near = true; if (near) continue;
                var go = new GameObject("Juice109 searchlight"); go.transform.SetParent(transform, false); go.transform.position = p + Vector3.up * 0.5f;
                go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = beamMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                beams.Add((go.transform, beams.Count * 120f + 25f, beams.Count * 2.1f));
            }
            Beams = beams.Count; Debug.Log($"Juice109City: night: {Beams} searchlights at {string.Join(", ", beams.ConvertAll(b => b.t.position.ToString("F0")))} round {c:F0}");
        }

        /// <summary>An open cone along +Z, 220 m long, 0.6 m across at the lamp and 14 m at the far end; its vertex colour fades from 0.22 at the lamp to
        /// 0 along its length and toward its rim (the glow shader is additive: colour is light).</summary>
        static Mesh BeamMesh()
        {
            const int seg = 16, rings = 8; const float len = 220f, r0 = 0.3f, r1 = 7f;
            var v = new List<Vector3>(); var col = new List<Color>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int j = 0; j <= rings; j++)
            {
                float t = j / (float)rings, r = Mathf.Lerp(r0, r1, t), k = 0.35f * (1f - t);
                for (int i = 0; i <= seg; i++) { float a = i / (float)seg * Mathf.PI * 2f; v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, t * len)); col.Add(new Color(k, k, k, 1f)); uv.Add(new Vector2(0.5f, 0.5f)); }
            }
            for (int j = 0; j < rings; j++) for (int i = 0; i < seg; i++) { int a = j * (seg + 1) + i, b = a + seg + 1; tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var m = new Mesh { name = "Juice109 searchlight cone" }; m.SetVertices(v); m.SetColors(col); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds(); m.bounds = new Bounds(new Vector3(0, 0, len / 2f), new Vector3(2 * r1, 2 * r1, len)); return m;
        }
    }
}
