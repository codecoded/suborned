using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned
{
    /// <summary>Brief 075 golden (Latch, 2026-10-28): the presentation life set (contracts/world/presentation.schema.ts, suborned.presentation/1;
    /// Pulse's out/world/harbour-district.presentation.json) drawn over the built district: traffic, trams, parked cars and the townspeople.
    /// Review and presentation only, never the game: it is installed only by the flyby (Flyby074 -life on), the golden harness (Golden075 -life t)
    /// and the free-fly cameras (FreeFly, the walk review's Fly mode); never in play, never in the Handover, never read by the sim.
    /// Built by the Editor (Suborned.World.Review.LifeInstall: the JSON, the kit prefabs, the figures' baked poses); this component only samples.
    /// Step(t) places everything at t exactly as tools/life/presentation-sample.ts does (tau = t mod period; timed routes linear in t, the
    /// heading held through a dwell; loops by arc length, offset `lat` to the mover's left; still fixed), so a time gives one frame.
    /// Figures are shared baked meshes (one set per figure, clip, frame and LOD) on plain MeshRenderers, swapped per frame: walkers play `walk`
    /// at |v| / 1.29 (its natural speed, as HostViews does), the rest their clip; each one's clip start offset by a hash of its id.
    /// Everything it makes is hidden from saving (DontSave); the two parked dressing trams are hidden while it lives and shown again after.</summary>
    public sealed class PresentationLife : MonoBehaviour
    {
        /// <summary>Set by the Editor (LifeInstall, InitializeOnLoad): builds the life for the free-fly camera; null in a player.</summary>
        public static Func<PresentationLife> Installer;

        public const float WalkNatural = 1.29f; // tools/figures/speeds.json: walk

        public sealed class Route { public float[] xy, t, cum; public float z; }
        public sealed class Loop { public float[] xyz, cum; public float len; }
        /// <summary>A figure's baked poses: per LOD, per part (one per skinned renderer), the frames of every clip back to back.</summary>
        public sealed class Baked
        {
            public sealed class Part { public Mesh[] frames; public Material[] mats; public Vector3 pos; public Quaternion rot; }
            public Part[][] lods = Array.Empty<Part[]>();
            public float[] lodHeights = Array.Empty<float>(); // screen heights: below [0] the last LOD, below [last] culled
            public string[] clips = Array.Empty<string>(); public int[] start = Array.Empty<int>(), frames = Array.Empty<int>(); public float[] length = Array.Empty<float>();
            public int Clip(string name) { int i = Array.IndexOf(clips, name); return i >= 0 ? i : Array.IndexOf(clips, "idle"); }
        }
        public sealed class Model { public string id; public GameObject prefab; public float pivotOffset; public Baked figure; }

        sealed class Body
        {
            public Transform t; public Model model; public uint hash;
            public GameObject[] lodRoots; public MeshFilter[][] parts; public int lod = -2, frame = -1; public int clip;
        }
        struct TimedE { public Body body; public Body[] consist; public float[] back; public bool[] flip; public int route; public float t0; }
        struct LoopE { public Body body; public int loop; public float s0, v, lat, bob; }

        public float Period = 720f;
        public Route[] Routes = Array.Empty<Route>();
        public Loop[] Loops = Array.Empty<Loop>();
        /// <summary>The camera whose distance picks each figure's LOD (the render camera).</summary>
        public Camera View;
        public float LastT { get; private set; } = float.NaN;
        public int Shown { get; private set; }

        readonly List<TimedE> timed = new();
        readonly List<LoopE> looping = new();
        readonly List<Body> still = new(), figs = new();
        readonly List<GameObject> hidden = new();

        static float Mod(float a, float m) { var r = a % m; return r < 0f ? r + m : r; }
        public static uint Hash(string s) { uint h = 2166136261; foreach (var c in s ?? "") { h ^= c; h *= 16777619; } return h; } // FNV-1a, as HostViews
        static float Frac01(uint h) => (h % 10007u) / 10007f;

        /// <summary>Unity rotation of a plan heading (radians, 0 = +x east, pi/2 = +y north; models front +Z): eulerY = 90 - deg(heading).</summary>
        public static Quaternion Heading(float h) => Quaternion.Euler(0f, 90f - h * Mathf.Rad2Deg, 0f);

        public static float[] LoopCum(float[] xyz)
        {
            int n = xyz.Length / 3; var c = new float[n + 1];
            for (int i = 1; i <= n; i++) { int a = (i - 1) * 3, b = (i % n) * 3; c[i] = c[i - 1] + Mathf.Sqrt(Sq(xyz[b] - xyz[a]) + Sq(xyz[b + 1] - xyz[a + 1])); }
            return c;
        }
        public static float[] RouteCum(float[] xy)
        {
            int n = xy.Length / 2; var c = new float[n];
            for (int i = 1; i < n; i++) c[i] = c[i - 1] + Mathf.Sqrt(Sq(xy[2 * i] - xy[2 * i - 2]) + Sq(xy[2 * i + 1] - xy[2 * i - 1]));
            return c;
        }
        static float Sq(float v) => v * v;

        /// <summary>presentation-sample.ts onLoop: the point at arc length s (wrapped), height and heading.</summary>
        public static Vector3 OnLoop(Loop l, float s, out float heading)
        {
            var c = l.cum; int n = l.xyz.Length / 3; s = Mod(s, c[n]);
            int lo = 0, hi = n; while (hi - lo > 1) { int m = (lo + hi) >> 1; if (c[m] <= s) lo = m; else hi = m; }
            int a = lo * 3, b = ((lo + 1) % n) * 3; float seg = c[lo + 1] - c[lo], f = (s - c[lo]) / (seg != 0f ? seg : 1f);
            float dx = l.xyz[b] - l.xyz[a], dy = l.xyz[b + 1] - l.xyz[a + 1];
            heading = Mathf.Atan2(dy, dx);
            return new Vector3(l.xyz[a] + dx * f, l.xyz[a + 1] + dy * f, l.xyz[a + 2] + (l.xyz[b + 2] - l.xyz[a + 2]) * f);
        }

        /// <summary>presentation-sample.ts onRoute: the plan point d seconds into the route (false outside it), its heading (held through a
        /// dwell) and its distance along the polyline.</summary>
        public static bool OnRoute(Route r, float d, out Vector2 p, out float heading, out float along)
        {
            var t = r.t; int n = t.Length; p = default; heading = 0f; along = 0f;
            if (d < 0f || d > t[n - 1]) return false;
            int lo = 0, hi = n - 1; while (hi - lo > 1) { int m = (lo + hi) >> 1; if (t[m] <= d) lo = m; else hi = m; }
            float dt = t[hi] - t[lo], f = (d - t[lo]) / (dt != 0f ? dt : 1f);
            float ax = r.xy[2 * lo], ay = r.xy[2 * lo + 1], bx = r.xy[2 * hi], by = r.xy[2 * hi + 1];
            int i = lo; float dx = bx - ax, dy = by - ay;
            while (dx * dx + dy * dy < 1e-12f && i > 0) { dx = r.xy[2 * i] - r.xy[2 * i - 2]; dy = r.xy[2 * i + 1] - r.xy[2 * i - 1]; i--; }
            p = new Vector2(ax + (bx - ax) * f, ay + (by - ay) * f); heading = Mathf.Atan2(dy, dx);
            along = r.cum[lo] + (r.cum[hi] - r.cum[lo]) * f;
            return true;
        }

        /// <summary>The plan point `s` metres along the route's polyline (clamped to its ends) and its heading.</summary>
        static Vector2 AtDistance(Route r, float s, out float heading)
        {
            var c = r.cum; int n = c.Length; s = Mathf.Clamp(s, 0f, c[n - 1]);
            int lo = 0, hi = n - 1; while (hi - lo > 1) { int m = (lo + hi) >> 1; if (c[m] <= s) lo = m; else hi = m; }
            float seg = c[hi] - c[lo], f = seg > 1e-6f ? (s - c[lo]) / seg : 0f;
            int i = lo; float dx = r.xy[2 * hi] - r.xy[2 * lo], dy = r.xy[2 * hi + 1] - r.xy[2 * lo + 1];
            while (dx * dx + dy * dy < 1e-12f && i > 0) { dx = r.xy[2 * i] - r.xy[2 * i - 2]; dy = r.xy[2 * i + 1] - r.xy[2 * i - 1]; i--; }
            heading = Mathf.Atan2(dy, dx);
            return new Vector2(r.xy[2 * lo] + (r.xy[2 * hi] - r.xy[2 * lo]) * f, r.xy[2 * lo + 1] + (r.xy[2 * hi + 1] - r.xy[2 * lo + 1]) * f);
        }

        // ---- building (called by the Editor's LifeInstall) ----

        Body Make(Model m, string id, string clip)
        {
            var b = new Body { model = m, hash = Hash(id) };
            if (m.figure != null)
            {
                var go = new GameObject(id) { hideFlags = HideFlags.DontSave }; go.transform.SetParent(transform, false); b.t = go.transform;
                b.lodRoots = new GameObject[m.figure.lods.Length]; b.parts = new MeshFilter[m.figure.lods.Length][];
                for (int l = 0; l < m.figure.lods.Length; l++)
                {
                    var lr = new GameObject("lod" + l) { hideFlags = HideFlags.DontSave }; lr.transform.SetParent(b.t, false); lr.SetActive(false);
                    b.lodRoots[l] = lr; var ps = m.figure.lods[l]; b.parts[l] = new MeshFilter[ps.Length];
                    for (int k = 0; k < ps.Length; k++)
                    {
                        var pg = new GameObject("part" + k) { hideFlags = HideFlags.DontSave }; pg.transform.SetParent(lr.transform, false);
                        pg.transform.SetLocalPositionAndRotation(ps[k].pos, ps[k].rot);
                        b.parts[l][k] = pg.AddComponent<MeshFilter>(); pg.AddComponent<MeshRenderer>().sharedMaterials = ps[k].mats;
                    }
                }
                b.clip = m.figure.Clip(clip);
                figs.Add(b);
            }
            else
            {
                var go = Instantiate(m.prefab, transform, false); go.name = id; go.hideFlags = HideFlags.DontSave; b.t = go.transform;
                foreach (var c in go.GetComponentsInChildren<Transform>(true)) c.gameObject.hideFlags = HideFlags.DontSave;
            }
            b.t.gameObject.SetActive(false);
            return b;
        }

        public void AddTimed(Model m, string id, int route, float t0, (Model m, float back, bool flip)[] consist)
        {
            var e = new TimedE { body = Make(m, id, null), route = route, t0 = t0 };
            var cs = new List<Body>(); var bk = new List<float>(); var fl = new List<bool>();
            if (consist != null) for (int i = 0; i < consist.Length; i++) if (consist[i].m != null) { cs.Add(Make(consist[i].m, $"{id}/car{i + 1}", null)); bk.Add(consist[i].back); fl.Add(consist[i].flip); }
            e.consist = cs.ToArray(); e.back = bk.ToArray(); e.flip = fl.ToArray();
            timed.Add(e);
        }
        public void AddLooping(Model m, string id, int loop, float s0, float v, float lat, float bob, string clip) =>
            looping.Add(new LoopE { body = Make(m, id, clip), loop = loop, s0 = s0, v = v, lat = lat, bob = bob });
        public void AddStill(Model m, string id, Vector2 at, float z, float heading, string clip)
        {
            var b = Make(m, id, clip); b.t.gameObject.SetActive(true);
            b.t.SetPositionAndRotation(new Vector3(at.x, z, at.y), Heading(heading));
            still.Add(b);
        }
        /// <summary>Hides a scene object while the life lives (the parked dressing trams); shown again when it goes.</summary>
        public void Hide(GameObject go) { if (go && go.activeSelf) { go.SetActive(false); hidden.Add(go); } }
        public int Count => timed.Count + looping.Count + still.Count;
        public int Figures => figs.Count;

        void OnDisable() { foreach (var g in hidden) if (g) g.SetActive(true); }
        void OnEnable() { foreach (var g in hidden) if (g) g.SetActive(false); }
        void OnDestroy() { foreach (var g in hidden) if (g) g.SetActive(true); hidden.Clear(); }

        // ---- sampling ----

        /// <summary>Everything at presentation time t (seconds; tau = t mod Period).</summary>
        public void Step(float t)
        {
            LastT = t; float tau = Mod(t, Period); int shown = still.Count;
            foreach (var e in timed)
            {
                var r = Routes[e.route];
                bool on = OnRoute(r, Mod(tau - e.t0, Period), out var p, out var h, out var along);
                Set(e.body, on); if (!on) { for (int i = 0; i < e.consist.Length; i++) Set(e.consist[i], false); continue; }
                Place(e.body, p, r.z, h); shown++;
                for (int i = 0; i < e.consist.Length; i++)
                {
                    var q = AtDistance(r, along - e.back[i], out var hc);
                    Set(e.consist[i], true); Place(e.consist[i], q, r.z, hc + (e.flip[i] ? Mathf.PI : 0f)); shown++;
                }
            }
            foreach (var e in looping)
            {
                var l = Loops[e.loop]; var q = OnLoop(l, e.s0 + e.v * tau, out var h); h += e.v < 0f ? Mathf.PI : 0f;
                float x = q.x - Mathf.Sin(h) * e.lat, y = q.y + Mathf.Cos(h) * e.lat, z = q.z;
                if (e.bob != 0f) z += e.bob * Mathf.Sin(2f * Mathf.PI * tau / 3f + Frac01(e.body.hash) * 2f * Mathf.PI);
                Set(e.body, true); e.body.t.SetPositionAndRotation(new Vector3(x, z, y), Heading(h)); shown++;
                if (e.body.model.figure is { } fb) Pose(e.body, fb, tau * Mathf.Abs(e.v) / WalkNatural);
            }
            foreach (var b in still) if (b.model.figure is { } fb) Pose(b, fb, tau);
            Lods();
            Shown = shown;
        }

        static void Set(Body b, bool on) { if (b.t.gameObject.activeSelf != on) b.t.gameObject.SetActive(on); }

        static void Place(Body b, Vector2 p, float z, float h)
        {
            // the kit's pivotOffset (metres along the heading), as the generator moved the parked cars' pivots
            if (b.model.pivotOffset != 0f) p -= new Vector2(Mathf.Cos(h), Mathf.Sin(h)) * b.model.pivotOffset;
            b.t.SetPositionAndRotation(new Vector3(p.x, z, p.y), Heading(h));
        }

        /// <summary>The clip frame at clip time `clipT` (seconds of the clip at its natural rate), offset by the body's hash.</summary>
        static void Pose(Body b, Baked fb, float clipT)
        {
            int c = b.clip; if (c < 0) return;
            float len = Mathf.Max(fb.length[c], 1e-3f), ph = Mod(clipT / len + Frac01(b.hash), 1f);
            b.frame = fb.start[c] + Mathf.Min(fb.frames[c] - 1, (int)(ph * fb.frames[c]));
        }

        /// <summary>Each figure's LOD by its screen height from View (1.75 m tall), as the figure library's LOD heights; then its frame's meshes.</summary>
        void Lods()
        {
            var cam = View; Vector3 eye = cam ? cam.transform.position : Vector3.zero;
            float k = cam ? 1f / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) : 0f;
            foreach (var b in figs)
            {
                if (!b.t.gameObject.activeSelf) continue;
                var fb = b.model.figure; int n = fb.lods.Length, lod = 0;
                if (cam && fb.lodHeights.Length > 0)
                {
                    float d = Mathf.Max(0.01f, Vector3.Distance(eye, b.t.position)), h = 1.75f * k / d;
                    lod = h >= fb.lodHeights[0] ? 0 : n - 1;
                    if (h < fb.lodHeights[^1]) lod = -1;
                }
                if (lod != b.lod)
                {
                    for (int l = 0; l < n; l++) { bool on = l == lod; if (b.lodRoots[l].activeSelf != on) b.lodRoots[l].SetActive(on); }
                    b.lod = lod; b.frame = b.frame < 0 ? 0 : b.frame;
                }
                if (lod < 0) continue;
                var ps = fb.lods[lod]; var mf = b.parts[lod]; int f = Mathf.Max(0, b.frame);
                for (int p = 0; p < mf.Length; p++) { var m = ps[p].frames[f]; if (mf[p].sharedMesh != m) mf[p].sharedMesh = m; }
            }
        }
    }
}
