using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>What the host draws from the engine each frame (brief 011 D, graybox): the cell's four operatives, the four cast roles, the
    /// patrol, the rivals, the response unit, the official and getaway cars, and the markers a click can hit (the public door, the Tech's
    /// cabinet, the two cameras). People are capsules as the people layer draws them (1.75 m, ivory head). Sight is the Scout's skill (Sky
    /// 2026-10-16; Glyph's review §3.4): no cone is drawn by default; the host asks for the fans it wants (ShowFans) and each is drawn as the
    /// engine's own sight polygon (Game.ConeFan): a flat fill at 30 %, a crisp rim, the far edge (the fan's reach points) in its kind's cue
    /// (camera, patrol and officers dashed red, the camera with a scan edge and two fading ghosts; the guard amber with chevrons; the rivals
    /// violet with diamonds), unlit; grey when the watcher sees nothing now. Positions are the engine's, eased between its 10 Hz ticks.
    /// Nothing here feeds back into the engine.</summary>
    public sealed class HostViews
    {
        public const float Tall = 1.75f, Radius = 0.28f, HeadD = 0.3f;
        readonly Transform root; readonly SiteFrame frame; readonly HandoverHost host;
        readonly Material head, ring, select;
        readonly Dictionary<string, (Transform t, Vector3 from, Vector3 to, bool shown)> things = new();
        static Mesh capsule, sphere, cylinder, cube;

        public HostViews(HandoverHost host, Transform root, SiteFrame frame)
        {
            this.host = host; this.root = root; this.frame = frame;
            capsule ??= Prim(PrimitiveType.Capsule); sphere ??= Prim(PrimitiveType.Sphere); cylinder ??= Prim(PrimitiveType.Cylinder); cube ??= Prim(PrimitiveType.Cube);
            head = Lit(host.headColour); ring = Unlit(Color.white); select = Unlit(host.selectColour);
            var g = host.Game; var s = g.S;
            for (int i = 0; i < 4; i++) Person("op/" + Op.Name((OpId)i), host.cellColour, true);
            Person("waitress", host.waitressColour); Person("driver", host.civicColour); Person("guard", host.civicColour); Person("councillor", host.civicColour);
            Person("patrol", host.securityColour);
            for (int i = 0; i < 3; i++) Person("rivals/" + i, host.rivalBodyColour);
            for (int i = 0; i < 2; i++) Person("security/" + i, host.securityColour);
            Box("car/official", new Vector3(1.8f, 1.4f, 4.6f), host.carColour, true);
            Box("car/getaway", new Vector3(1.8f, 1.4f, 4.6f), host.getawayColour, true);
            Marker("door", frame.World(s.At(s.GuardPost)) + Vector3.up * 1.2f, new Vector3(2.4f, 2.4f, 2.4f));
            Marker("cabinet", frame.World(s.At("cabinet")) + Vector3.up * 0.7f, new Vector3(1.4f, 1.4f, 1.4f));
            foreach (var c in s.Cameras)
            {
                var at = frame.World(c.At);
                var pole = Part(root, "camera/" + c.Id, cylinder, Lit(host.carColour), at + Vector3.up * 1.6f, new Vector3(0.25f, 1.6f, 0.25f));
                var col = pole.gameObject.AddComponent<CapsuleCollider>(); col.radius = 2.5f; col.height = 2f; // a fat target, pole scale makes it ~1.2 m
                things["camera/" + c.Id] = (pole, pole.position, pole.position, true);
            }
            crowdKeys = new string[g.Walkers.Count]; hostileKeys = new string[MaxHostiles];
            for (int i = 0; i < g.Walkers.Count; i++) Person(crowdKeys[i] = "crowd/" + i, host.JobColour(g.Walkers[i].P.Job), false, true); // hoverable: "passer-by"
            for (int i = 0; i < MaxHostiles; i++) Person(hostileKeys[i] = "hostile/" + i, host.securityColour);
            Ring("sel/0"); Ring("sel/1"); Ring("sel/2"); Ring("sel/3");
            Ring("dest/0"); Ring("dest/1"); Ring("dest/2"); Ring("dest/3"); // where each walking agent will stop (the snapped point)
            var pg = new GameObject("path"); pg.transform.SetParent(root, false);
            path = pg.AddComponent<LineRenderer>(); path.sharedMaterial = select; path.useWorldSpace = true; path.widthMultiplier = 0.18f; path.positionCount = 0;
            path.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; path.receiveShadows = false; path.alignment = LineAlignment.TransformZ;
            pg.transform.rotation = Quaternion.Euler(90f, 0f, 0f); path.enabled = false;
            for (int k = 0; k < 4; k++) // each agent walking to an act: the rest of the walk on the ground (the HUD puts the act's icon at its end)
            {
                var ag = new GameObject("act/" + k); ag.transform.SetParent(root, false); ag.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var lr = ag.AddComponent<LineRenderer>(); lr.sharedMaterial = select; lr.useWorldSpace = true; lr.widthMultiplier = 0.1f; lr.positionCount = 0;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; lr.alignment = LineAlignment.TransformZ; lr.enabled = false;
                acts[k] = lr;
            }
        }

        readonly LineRenderer[] acts = new LineRenderer[4], ranges = new LineRenderer[4];
        readonly List<(LineRenderer lr, float born)> tracers = new();
        readonly Dictionary<Color, Material> tracerMats = new();
        int nextTracer;

        LineRenderer NewLine(string name, Material m, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = m; lr.useWorldSpace = true; lr.widthMultiplier = width; lr.positionCount = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; lr.enabled = false;
            return lr;
        }

        /// <summary>One shot: a thin line from the muzzle to where it ends, for 0.15 s (placeholder for Glyph's tracers).</summary>
        public void Tracer(Vector3 from, Vector3 to, Color c)
        {
            if (!tracerMats.TryGetValue(c, out var m)) tracerMats[c] = m = Unlit(c);
            if (tracers.Count < 16) tracers.Add((NewLine("tracer/" + tracers.Count, m, 0.06f), 0f));
            int i = nextTracer++ % tracers.Count; var lr = tracers[i].lr;
            lr.sharedMaterial = m; lr.positionCount = 2; lr.SetPosition(0, from); lr.SetPosition(1, to); lr.enabled = true;
            tracers[i] = (lr, Time.unscaledTime);
        }

        /// <summary>The weapon's effective range round agent k when its weapon is drawn and it is selected (radius in m; 0 hides).</summary>
        public void ShowRange(int k, Vector3 centre, float radius)
        {
            var lr = ranges[k];
            if (!lr) { lr = ranges[k] = NewLine("range/" + k, select, 0.05f); lr.loop = true; }
            if (radius <= 0f) { if (lr.enabled) lr.enabled = false; return; }
            const int n = 64; lr.positionCount = n;
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2f / n; lr.SetPosition(i, centre + new Vector3(Mathf.Cos(a) * radius, 0.06f, Mathf.Sin(a) * radius)); }
            lr.enabled = true;
        }

        /// <summary>The rest of agent k's walk to an act (null or fewer than two points hides it).</summary>
        public void ShowAct(int k, List<Vector3> pts)
        {
            var lr = acts[k]; if (!lr) return;
            if (pts == null || pts.Count < 2) { if (lr.enabled) lr.enabled = false; return; }
            lr.positionCount = pts.Count; for (int i = 0; i < pts.Count; i++) lr.SetPosition(i, pts[i]); lr.enabled = true;
        }

        public Transform Find(string key) => things.TryGetValue(key, out var x) ? x.t : null;
        LineRenderer path;

        /// <summary>The walk an action panel row would take (null hides it).</summary>
        public void ShowPath(List<Vector3> pts)
        {
            if (!path) return;
            if (pts == null || pts.Count < 2) { path.enabled = false; return; }
            path.positionCount = pts.Count; for (int i = 0; i < pts.Count; i++) path.SetPosition(i, pts[i]); path.enabled = true;
        }
        const int MaxHostiles = 8;
        static readonly string[] OpKeys = { "op/scout", "op/fixer", "op/tech", "op/enforcer" }, SelKeys = { "sel/0", "sel/1", "sel/2", "sel/3" }, DestKeys = { "dest/0", "dest/1", "dest/2", "dest/3" };
        string[] crowdKeys = System.Array.Empty<string>(), hostileKeys = System.Array.Empty<string>(); // no string building per tick
        readonly Dictionary<Pt, int> stacked = new();

        /// <summary>Sets where each thing is after a tick (the frame eases from the previous one).</summary>
        public void Tick()
        {
            var g = host.Game; var s = g.S;
            for (int i = 0; i < 4; i++) { var op = g.Ops[i]; Set(OpKeys[i], op.Inside ? null : op.Pos, op.Down); }
            Set("waitress", g.PersonPos(g.Inez), g.Inez.Down); Set("driver", g.DriverPos(), g.Driver.Down);
            Set("guard", g.GuardPose()?.pos, g.GuardDown); Set("councillor", g.ValePos(), g.ValeP.Down);
            var pp = g.PatrolPose(); Set("patrol", pp?.pos, g.PatrolDown);
            var rp = g.RivalsPose();
            for (int i = 0; i < 3; i++) Set("rivals/" + i, rp.HasValue ? rp.Value.pos + new Pt(i == 1 ? 90 : 0, i == 2 ? 90 : 0) : (Pt?)null);
            var sp = g.SecurityPose();
            for (int i = 0; i < 2; i++) Set("security/" + i, sp.HasValue ? sp.Value.pos + new Pt(0, i * 90) : (Pt?)null);
            Set("car/official", g.Vale == ValeAt.Car && g.Result != Outcome.Success ? (Pt?)null : g.CarPos());
            Set("car/getaway", s.At(s.GetawayCar));
            // the crowd: people the engine puts on one place point are fanned out on a 0.5 m sunflower for the eye only (the engine is not told)
            stacked.Clear();
            for (int i = 0; i < g.Walkers.Count; i++)
            {
                var at = g.CrowdPos(g.Walkers[i]);
                if (!at.HasValue) { Set(crowdKeys[i], null); continue; }
                int k = stacked.TryGetValue(at.Value, out var n) ? n + 1 : 0; stacked[at.Value] = k;
                float r = 50f * Mathf.Sqrt(k), th = k * 2.399963f;
                Set(crowdKeys[i], at.Value + new Pt(Mathf.RoundToInt(r * Mathf.Cos(th)), Mathf.RoundToInt(r * Mathf.Sin(th))));
                // in a fight (Keel: startle, flee, shelter, return): a startled person cowers (squat and wide) where they stand, then runs for a
                // refuge at the crowd's run speed and is off the street while sheltered (the engine gives no position), so the square empties
                var ct = things[crowdKeys[i]].t; bool cower = g.Walkers[i].St == "startled";
                var want = cower ? new Vector3(1.2f, 0.75f, 1.2f) : Vector3.one; if (ct.localScale != want) ct.localScale = want;
            }
            // the final fight's hostiles (officers in the loud branch, the rival ambush in the safe one)
            for (int i = 0; i < MaxHostiles; i++)
            {
                var h = i < g.Hostiles.Count ? g.Hostiles[i] : null;
                Set(hostileKeys[i], h != null && h.Present ? h.Pos(g.Tick) : (Pt?)null, h != null && h.Down); // a downed hostile lies where it fell
                if (h != null) Tint(hostileKeys[i], h.Id.StartsWith("rival") ? host.rivalBodyColour : host.securityColour);
            }
        }

        void Tint(string key, Color c)
        {
            var r = things[key].t.Find("body")?.GetComponent<MeshRenderer>(); if (!r) return;
            if (r.sharedMaterial.GetColor("_BaseColor") != c) r.sharedMaterial.SetColor("_BaseColor", c);
        }

        /// <summary>Per frame: eases positions, draws the cones and the selection rings.</summary>
        public void Frame(float alpha)
        {
            float now = Time.unscaledTime;
            foreach (var (lr, born) in tracers) if (lr.enabled && now - born > 0.15f) lr.enabled = false;
            foreach (var kv in things)
            {
                var (t, from, to, shown) = kv.Value;
                if (t.gameObject.activeSelf != shown) t.gameObject.SetActive(shown);
                if (shown && !kv.Key.StartsWith("camera/") && !kv.Key.StartsWith("marker/")) t.position = Vector3.Lerp(from, to, alpha);
            }
            var g = host.Game;
            for (int i = 0; i < 4; i++)
            {
                var r = things[SelKeys[i]].t; var op = things[OpKeys[i]];
                bool on = host.Selected[i] && op.shown;
                if (r.gameObject.activeSelf != on) r.gameObject.SetActive(on);
                if (on) r.position = op.t.position + Vector3.up * 0.03f;
                var d = things[DestKeys[i]].t; var walk = g.Ops[i].Walk; bool dest = walk != null && op.shown;
                if (d.gameObject.activeSelf != dest) d.gameObject.SetActive(dest);
                if (dest) { d.position = frame.World(walk.End) + Vector3.up * 0.03f; d.localScale = new Vector3(0.6f, 0.01f, 0.6f); }
            }
        }

        void Set(string key, Pt? at, bool down = false)
        {
            var x = things[key];
            if (!at.HasValue) { things[key] = (x.t, x.to, x.to, false); return; }
            var w = frame.World(at.Value);
            bool wasShown = x.shown;
            things[key] = (x.t, wasShown ? x.to : w, w, true);
            if (down) x.t.localRotation = Quaternion.Euler(90f, 0f, 0f); else if (x.t.localRotation != Quaternion.identity) x.t.localRotation = Quaternion.identity; // down: laid flat
        }

        void Person(string key, Color body, bool cell = false, bool collide = true)
        {
            var go = new GameObject(key); go.transform.SetParent(root, false);
            float bodyH = Tall - HeadD - 0.03f;
            Part(go.transform, "body", capsule, Lit(body), new Vector3(0, bodyH / 2, 0), new Vector3(Radius * 2, bodyH / 2, Radius * 2));
            Part(go.transform, "head", sphere, cell ? Lit(host.cellHeadColour) : head, new Vector3(0, Tall - HeadD / 2, 0), Vector3.one * HeadD);
            if (collide) { var col = go.AddComponent<CapsuleCollider>(); col.height = Tall; col.radius = Radius + 0.12f; col.center = new Vector3(0, Tall / 2, 0); } // the crowd is not a target (orders name roles)
            go.SetActive(false);
            things[key] = (go.transform, Vector3.zero, Vector3.zero, false);
        }

        void Box(string key, Vector3 size, Color c, bool collide)
        {
            var go = new GameObject(key); go.transform.SetParent(root, false);
            Part(go.transform, "body", cube, Lit(c), new Vector3(0, size.y / 2, 0), size);
            if (collide) { var col = go.AddComponent<BoxCollider>(); col.size = size; col.center = new Vector3(0, size.y / 2, 0); }
            go.SetActive(false);
            things[key] = (go.transform, Vector3.zero, Vector3.zero, false);
        }

        void Marker(string key, Vector3 at, Vector3 size)
        {
            var go = new GameObject("marker/" + key); go.transform.SetParent(root, false); go.transform.position = at;
            var col = go.AddComponent<BoxCollider>(); col.size = size;
            things["marker/" + key] = (go.transform, at, at, true);
        }

        void Ring(string key)
        {
            var t = Part(root, key, cylinder, select, Vector3.zero, new Vector3(1.1f, 0.01f, 1.1f));
            t.gameObject.SetActive(false);
            things[key] = (t, Vector3.zero, Vector3.zero, false);
        }

        // ---------------------------------------------------------------- sight fans (the Scout's skill)

        sealed class FanDraw
        {
            public GameObject Go; public Mesh Fill, Rim; public MeshRenderer FillR, RimR; public Fan For; public bool Live; public string Kind = "";
            public readonly LineRenderer[] Scan = new LineRenderer[3]; public float A0, A1; public Vector3 Hub; public readonly List<Vector3> Ring = new();
        }
        readonly List<FanDraw> fans = new();
        readonly Dictionary<(Color, float), Material> fanMats = new();
        readonly List<Vector3> verts = new(); readonly List<int> tris = new();
        const float FillAlpha = 0.3f, RimW = 0.06f, FillY = 0.05f, RimY = 0.07f;
        static readonly Color Grey = new(0.55f, 0.55f, 0.55f);

        /// <summary>The fans to draw this frame (the host's choice: the focused watcher's, or every learned one while Alt is held); the rest hide.
        /// Each mesh is rebuilt only when the engine hands back a different Fan (its cache keeps one per pose) or its Live flag flips.</summary>
        public void ShowFans(IReadOnlyList<Fan> list)
        {
            int n = list?.Count ?? 0;
            for (int i = 0; i < n; i++)
            {
                var f = list[i]; if (fans.Count <= i) fans.Add(NewFan(i));
                var d = fans[i];
                if (!d.Go.activeSelf) d.Go.SetActive(true);
                if (!ReferenceEquals(d.For, f) || d.Live != f.Live) Build(d, f);
                Scan(d);
            }
            for (int i = n; i < fans.Count; i++) if (fans[i].Go.activeSelf) { fans[i].Go.SetActive(false); fans[i].For = null; }
        }

        FanDraw NewFan(int i)
        {
            var d = new FanDraw { Go = new GameObject("fan/" + i) }; d.Go.transform.SetParent(root, false);
            (d.Fill, d.FillR) = MeshPart(d.Go.transform, "fill"); (d.Rim, d.RimR) = MeshPart(d.Go.transform, "rim");
            for (int k = 0; k < 3; k++)
            {
                var lr = d.Scan[k] = NewLine("scan" + k, select, k == 0 ? RimW * 0.8f : RimW * 1.6f); lr.transform.SetParent(d.Go.transform, false);
                lr.alignment = LineAlignment.TransformZ; lr.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            return d;
        }

        static (Mesh, MeshRenderer) MeshPart(Transform parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name }; mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return (mesh, r);
        }

        /// <summary>A signal material: the fan template (transparent unlit, both faces) at an alpha, or the opaque unlit template at alpha 1.</summary>
        Material FanMat(Color c, float alpha)
        {
            if (fanMats.TryGetValue((c, alpha), out var m)) return m;
            var tpl = alpha < 1f && host.fanTemplate ? host.fanTemplate : host.unlitTemplate;
            m = new Material(tpl); m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, alpha)); if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            return fanMats[(c, alpha)] = m;
        }

        Color KindColour(string kind) => kind switch { "guard" => host.guardColour, "rivals" => host.rivalColour, _ => host.coneColour }; // §10: cones red, guard amber, rivals violet

        void Build(FanDraw d, Fan f)
        {
            d.For = f; d.Live = f.Live; d.Kind = f.Kind;
            var c = f.Live ? KindColour(f.Kind) : Grey;
            d.FillR.sharedMaterial = FanMat(c, FillAlpha); d.RimR.sharedMaterial = FanMat(c, 1f);
            d.Scan[0].sharedMaterial = FanMat(c, 1f); d.Scan[1].sharedMaterial = FanMat(c, 0.45f); d.Scan[2].sharedMaterial = FanMat(c, 0.22f);
            var pts = f.Points; int n = pts.Length;
            d.Ring.Clear(); for (int i = 0; i < n; i++) d.Ring.Add(frame.World(pts[i]));
            d.Hub = frame.World(f.At);
            // the fill: a fan of triangles from the watcher (the engine's polygon is star-shaped round it: its rays start there)
            verts.Clear(); tris.Clear();
            if (n >= 3)
            {
                verts.Add(d.Hub + Vector3.up * FillY);
                for (int i = 0; i < n; i++) verts.Add(d.Ring[i] + Vector3.up * FillY);
                for (int i = 0; i < n; i++) { tris.Add(0); tris.Add(1 + i); tris.Add(1 + (i + 1) % n); }
            }
            d.Fill.Clear(); d.Fill.SetVertices(verts); d.Fill.SetTriangles(tris, 0); d.Fill.RecalculateBounds();
            // the rim: crisp along walls and the near circle; the far edge (both ends stopped by reach) in the kind's cue
            verts.Clear(); tris.Clear(); float dash = 0f;
            for (int i = 0; i < n && n >= 3; i++)
            {
                int j = (i + 1) % n; Vector3 a = d.Ring[i] + Vector3.up * RimY, b = d.Ring[j] + Vector3.up * RimY;
                bool far = f.Stop.Length == n && f.Stop[i] == "reach" && f.Stop[j] == "reach";
                if (!far) { Quad(a, b, RimW); continue; }
                var outward = Flat((a + b) * 0.5f - d.Hub).normalized;
                float len = Vector3.Distance(a, b);
                switch (f.Kind)
                {
                    case "guard":   // chevrons marching the way he faces, on a hairline
                        Quad(a, b, RimW * 0.5f);
                        for (; dash < len; dash += 0.7f) Chevron(Vector3.Lerp(a, b, dash / len), outward, 0.32f);
                        dash -= len; break;
                    case "rivals":  // diamond ticks
                        Quad(a, b, RimW * 0.5f);
                        for (; dash < len; dash += 0.8f) Diamond(Vector3.Lerp(a, b, dash / len), outward, 0.3f);
                        dash -= len; break;
                    default:        // camera, patrol, officers: a dashed far edge (0.4 m on, 0.3 m off), the phase carried along the arc
                        for (float t = -dash; t < len; t += 0.7f) { float s0 = Mathf.Max(0f, t), s1 = Mathf.Min(len, t + 0.4f); if (s1 > s0) Quad(Vector3.Lerp(a, b, s0 / len), Vector3.Lerp(a, b, s1 / len), RimW); }
                        dash = (dash + len) % 0.7f; break;
                }
            }
            if (!f.Live && n >= 3) Hatch(d.Ring); // a camera that sees nothing now (looped): grey and hatched (Glyph §3.4)
            d.Rim.Clear(); d.Rim.SetVertices(verts); d.Rim.SetTriangles(tris, 0); d.Rim.RecalculateBounds();
            // the camera's scan sweeps between its outermost reach points
            d.A0 = d.A1 = 0f;
            if (f.Kind == "camera" && n >= 3)
            {
                var fwd = Flat(frame.Dir(f.Facing)); float lo = float.MaxValue, hi = float.MinValue;
                for (int i = 0; i < n; i++) if (f.Stop.Length == n && f.Stop[i] == "reach") { float a = Vector3.SignedAngle(fwd, Flat(d.Ring[i] - d.Hub), Vector3.up); lo = Mathf.Min(lo, a); hi = Mathf.Max(hi, a); }
                if (lo < hi) { d.A0 = lo; d.A1 = hi; }
            }
        }

        /// <summary>The camera's scan edge and its two fading ghosts: from the lens to where the fan ends along a sweeping bearing.</summary>
        void Scan(FanDraw d)
        {
            bool on = d.Kind == "camera" && d.Live && d.A1 > d.A0;
            for (int k = 0; k < 3; k++)
            {
                var lr = d.Scan[k];
                if (!on) { if (lr.enabled) lr.enabled = false; continue; }
                float t = Time.unscaledTime * 0.9f - k * 0.18f, s = 0.5f + 0.5f * Mathf.Sin(t);
                var fwd = Flat(frame.Dir(d.For.Facing)); var dir = Quaternion.AngleAxis(Mathf.Lerp(d.A0, d.A1, s), Vector3.up) * fwd;
                if (!Exit(d, dir, out var end)) { lr.enabled = false; continue; }
                lr.positionCount = 2; lr.SetPosition(0, d.Hub + Vector3.up * (RimY + 0.005f)); lr.SetPosition(1, end + Vector3.up * (RimY + 0.005f)); lr.enabled = true;
            }
        }

        /// <summary>Where a ray from the watcher leaves its fan (the farthest crossing of the polygon's edges, on the ground plane).</summary>
        static bool Exit(FanDraw d, Vector3 dir, out Vector3 end)
        {
            float best = -1f; end = d.Hub; var o = new Vector2(d.Hub.x, d.Hub.z); var r = new Vector2(dir.x, dir.z); int n = d.Ring.Count;
            for (int i = 0; i < n; i++)
            {
                var a = new Vector2(d.Ring[i].x, d.Ring[i].z); var b = new Vector2(d.Ring[(i + 1) % n].x, d.Ring[(i + 1) % n].z); var e = b - a;
                float den = r.x * e.y - r.y * e.x; if (Mathf.Abs(den) < 1e-6f) continue;
                var w = a - o; float t = (w.x * e.y - w.y * e.x) / den, u = (w.x * r.y - w.y * r.x) / den;
                if (t > 0.05f && u >= 0f && u <= 1f && t > best) { best = t; end = Vector3.Lerp(d.Ring[i], d.Ring[(i + 1) % n], u); }
            }
            return best > 0f;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        readonly List<float> hatchCuts = new();
        /// <summary>Diagonal hatching across the fan (0.9 m apart, hairline): each line of x − z = c is cut by the polygon's edges and drawn
        /// between pairs of crossings (even-odd), so it stays inside the notched shape.</summary>
        void Hatch(List<Vector3> ring)
        {
            int n = ring.Count; float lo = float.MaxValue, hi = float.MinValue, y = 0f;
            for (int i = 0; i < n; i++) { float c = ring[i].x - ring[i].z; lo = Mathf.Min(lo, c); hi = Mathf.Max(hi, c); y += ring[i].y / n; }
            y += RimY;
            for (float c = Mathf.Ceil(lo / 0.9f) * 0.9f; c < hi; c += 0.9f)
            {
                hatchCuts.Clear();
                for (int i = 0; i < n; i++)
                {
                    var a = ring[i]; var b = ring[(i + 1) % n]; float ca = a.x - a.z - c, cb = b.x - b.z - c;
                    if ((ca < 0f) == (cb < 0f)) continue;
                    hatchCuts.Add(Mathf.Lerp(a.x + a.z, b.x + b.z, ca / (ca - cb))); // position along the line (x + z)
                }
                hatchCuts.Sort();
                for (int k = 0; k + 1 < hatchCuts.Count; k += 2)
                {
                    float s0 = hatchCuts[k], s1 = hatchCuts[k + 1];
                    Quad(new Vector3((s0 + c) * 0.5f, y, (s0 - c) * 0.5f), new Vector3((s1 + c) * 0.5f, y, (s1 - c) * 0.5f), RimW * 0.5f);
                }
            }
        }

        void Quad(Vector3 a, Vector3 b, float w)
        {
            var side = Vector3.Cross(Vector3.up, Flat(b - a)).normalized * (w * 0.5f); int k = verts.Count;
            verts.Add(a - side); verts.Add(a + side); verts.Add(b + side); verts.Add(b - side);
            tris.Add(k); tris.Add(k + 1); tris.Add(k + 2); tris.Add(k); tris.Add(k + 2); tris.Add(k + 3);
        }

        void Chevron(Vector3 p, Vector3 o, float s)
        {
            var side = Vector3.Cross(Vector3.up, o) * (s * 0.6f); var tip = p + o * (s * 0.5f); var back = p - o * (s * 0.3f);
            Quad(back + side, tip, RimW); Quad(back - side, tip, RimW);
        }

        void Diamond(Vector3 p, Vector3 o, float s)
        {
            var side = Vector3.Cross(Vector3.up, o) * (s * 0.35f); int k = verts.Count;
            verts.Add(p + o * (s * 0.5f)); verts.Add(p + side); verts.Add(p - o * (s * 0.5f)); verts.Add(p - side);
            tris.Add(k); tris.Add(k + 1); tris.Add(k + 2); tris.Add(k); tris.Add(k + 2); tris.Add(k + 3);
        }

        static Transform Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            return go.transform;
        }

        static Mesh Prim(PrimitiveType t) { var g = GameObject.CreatePrimitive(t); var m = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); return m; }

        // from the templates the scene holds (PlayInstall), so a player build carries their shaders
        Material Lit(Color c) { var m = new Material(host.litTemplate) { enableInstancing = true }; m.SetColor("_BaseColor", c); return m; }
        Material Unlit(Color c) { var m = new Material(host.unlitTemplate); m.SetColor("_BaseColor", c); return m; }
    }
}
