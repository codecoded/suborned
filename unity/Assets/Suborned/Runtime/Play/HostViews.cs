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
    public sealed partial class HostViews
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
            // brief 014 (Sky: "no idea who the waitress is"): the stand-ins are tinted by job family, with a role shape where it is cheap
            Person("waitress", Family("server"), shape: Shape("server")); Person("driver", Family("official-driver"), shape: Shape("official-driver"));
            Person("guard", Family("guard"), shape: Shape("guard")); Person("councillor", Family("councillor"));
            Person("patrol", host.securityColour);
            for (int i = 0; i < 2; i++) Person("security/" + i, host.securityColour);
            Box("car/official", new Vector3(1.8f, 1.4f, 4.6f), host.carColour, true);
            Box("car/getaway", new Vector3(1.8f, 1.4f, 4.6f), host.getawayColour, true);
            if (Visual("patrol", host.patrolRobot) is Transform pr) { if (things["patrol"].t.Find("head")?.GetComponent<MeshRenderer>() is MeshRenderer ph) ph.enabled = false; carModels["patrol"] = pr; } // the patrol is Vela's PatrolRobot (brief 031: its head is the attention face), facing where it goes
            if (Visual("car/official", host.sedanOfficial) is Transform so) carModels["car/official"] = so; // brief 020: Vela's sedans; the boxes stay the click targets
            if (Visual("car/getaway", host.sedanGetaway) is Transform sg) carModels["car/getaway"] = sg;
            RivalViewsInit(); // brief 059 K8: the five and their car (HostViews.Rivals)
            if (!g.Range) Marker("door", frame.World(s.At(s.GuardPost)) + Vector3.up * 1.2f, new Vector3(2.4f, 2.4f, 2.4f)); // brief 029: the range has no door, cabinet or cameras to draw
            if (s.HasPlace("cabinet")) Marker("cabinet", frame.World(s.At("cabinet")) + Vector3.up * 0.7f, new Vector3(1.4f, 1.4f, 1.4f));
            foreach (var c in g.Range ? new List<Suborned.Sim.Camera>() : s.Cameras)
            {
                var at = frame.World(c.At);
                var pole = Part(root, "camera/" + c.Id, cylinder, Lit(host.carColour), at + Vector3.up * 1.6f, new Vector3(0.25f, 1.6f, 0.25f));
                var col = pole.gameObject.AddComponent<CapsuleCollider>(); col.radius = 2.5f; col.height = 2f; // a fat target, pole scale makes it ~1.2 m
                things["camera/" + c.Id] = (pole, pole.position, pole.position, true);
                if (!c.World && (host.hoverCamera || host.cctvPole)) KitPole(c, pole); // brief 055: Vela's CCTVPole at the plan's 5.5 m mount
                CamHeadFor(c, at); // brief 031: the lens that shows the camera's attention
            }
            // brief 013: the stashes (a bin, a service door) are click targets: a box round each, named stash/<id>
            foreach (var st in s.Stashes)
            {
                var go = new GameObject("stash/" + st.Id); go.transform.SetParent(root, false);
                var at = frame.World(st.At) + Vector3.up * 0.8f; go.transform.position = at;
                var col = go.AddComponent<BoxCollider>(); col.size = st.Kind == "door" ? new Vector3(2.2f, 2.6f, 2.2f) : new Vector3(2.6f, 1.8f, 2.6f);
                things["stash/" + st.Id] = (go.transform, at, at, true);
            }
            crowdKeys = new string[g.Walkers.Count]; hostileKeys = new string[MaxHostiles];
            for (int i = 0; i < g.Walkers.Count; i++) Person(crowdKeys[i] = "crowd/" + i, Family(g.Walkers[i].P.Job), false, true, Shape(g.Walkers[i].P.Job)); // hoverable: "passer-by"
            for (int i = 0; i < MaxHostiles; i++) Person(hostileKeys[i] = "hostile/" + i, host.securityColour);
            // brief 020: the stand-in figures in the fixed views and the crowd (capsules stay for anyone without one)
            foreach (var k in new List<string>(things.Keys)) if (RoleFigure(k) is string fid) Dress(k, fid);
            DressCrowd(g);
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

        LineRenderer NewLine(string name, Material m, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = m; lr.useWorldSpace = true; lr.widthMultiplier = width; lr.positionCount = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; lr.enabled = false;
            return lr;
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
        /// <summary>Sky 2026-10-16: "not really into this line showing the route". No route line is drawn in play: the HUD pins the destination
        /// (and Alt draws the engine's path, dotted). Kept as an API so callers stay as they were.</summary>
        public static bool RouteLines = false;

        public void ShowAct(int k, List<Vector3> pts)
        {
            var lr = acts[k]; if (!lr) return;
            if (!RouteLines) pts = null;
            if (pts == null || pts.Count < 2) { if (lr.enabled) lr.enabled = false; return; }
            lr.positionCount = pts.Count; for (int i = 0; i < pts.Count; i++) lr.SetPosition(i, pts[i]); lr.enabled = true;
        }

        public Transform Find(string key) => things.TryGetValue(key, out var x) ? x.t : null;
        /// <summary>Every view by key (picking walks them: HostPick).</summary>
        public IEnumerable<(string key, Transform t)> All() { foreach (var kv in things) yield return (kv.Key, kv.Value.t); }
        LineRenderer path;

        /// <summary>The walk an action panel row would take (null hides it).</summary>
        public void ShowPath(List<Vector3> pts)
        {
            if (!path) return;
            if (!RouteLines) pts = null; // no preview line on a hovered row either: the price line gives the time and the eye
            if (pts == null || pts.Count < 2) { path.enabled = false; return; }
            path.positionCount = pts.Count; for (int i = 0; i < pts.Count; i++) path.SetPosition(i, pts[i]); path.enabled = true;
        }
        const int MaxHostiles = 24; // brief 029: the range spawns up to 6 a side of three sides
        static readonly string[] OpKeys = { "op/scout", "op/fixer", "op/tech", "op/enforcer" }, SelKeys = { "sel/0", "sel/1", "sel/2", "sel/3" }, DestKeys = { "dest/0", "dest/1", "dest/2", "dest/3" };
        string[] crowdKeys = System.Array.Empty<string>(), hostileKeys = System.Array.Empty<string>(); // no string building per tick

        /// <summary>Sets where each thing is after a tick (the frame eases from the previous one).</summary>
        public void Tick()
        {
            var g = host.Game; var s = g.S;
            for (int i = 0; i < 4; i++) { var op = g.Ops[i]; Set(OpKeys[i], op.Inside || op.InCar ? null : op.Pos, op.Down); } // brief 018: in the car is off the street (the car marker's seat pips say who)
            if (g.Range)
            {   // brief 029 (Keel): the fight range draws the cell and the spawned hostiles only; the Handover's cast, guard, patrol, Vale, cars and
                // cameras are the site format's required keys parked in the wings, and are never drawn
                foreach (var k in new[] { "waitress", "driver", "guard", "councillor", "patrol", "car/official", "car/getaway", "car/rivals" }) Set(k, null);
                foreach (var k in RivalKeys) Set(k, null); for (int i = 0; i < 2; i++) Set("security/" + i, null);
                faces.Clear(); phases.Clear();
                var gone = new HashSet<string>(); foreach (var rh in g.RangeHostiles()) if (rh.Gone) gone.Add(rh.Id);
                for (int i = 0; i < MaxHostiles; i++)
                {
                    var h = i < g.Hostiles.Count ? g.Hostiles[i] : null; bool show = h != null && h.Present && !gone.Contains(h.Id);
                    Set(hostileKeys[i], show ? h.Pos(g.Tick) : (Pt?)null, h != null && h.Down);
                    if (h != null) { Tint(hostileKeys[i], h.Id.StartsWith("rival") ? host.rivalBodyColour : host.securityColour); Dress(hostileKeys[i], h.Id.StartsWith("rival") ? RivalFigure(h.Id) : "security"); } // Forma 067: each member his own look
                }
                RangeCars(g); RangeBystanders(g); // engine 0.18: the range's cars and bystanders
                return;
            }
            var dpos = g.DriverPos(); bool driverIn = dpos.HasValue && !g.Driver.Down && dpos.Value == g.CarPos() && !(g.Vale == ValeAt.Office || g.Vale == ValeAt.Leaving); // brief 059 K1, K2: at the wheel (Vale in the back), never a figure standing in the car
            Set("waitress", g.PersonPos(g.Inez), g.Inez.Down); Set("driver", driverIn || g.DriverAboard ? null : dpos, g.Driver.Down); // brief 081: DriverAboard, never a figure at the car's centre
            Set("guard", g.GuardPose()?.pos, g.GuardDown); Set("councillor", g.Vale == ValeAt.Car || g.Vale == ValeAt.InCar || g.Vale == ValeAt.Taking && g.TakeInCar ? null : g.ValePos(), g.ValeP.Down); // brief 059 K1: in the back of his car (and taken from it) he is in the car, not on the street
            // brief 022: Inez's round gives her facing (to her table, to the guard when they stand together), the guard's pose his
            faces.Clear();
            phases.Clear();
            if (g.PersonPose(g.Inez) is { } ip) { if (ip.facing.X != 0 || ip.facing.Y != 0) faces["waitress"] = frame.Dir(ip.facing); phases["waitress"] = ip.phase; if (ip.phase == "together") phases["guard"] = "together"; } // he stands with her: both talk
            if (g.GuardPose() is { } gp && (gp.facing.X != 0 || gp.facing.Y != 0)) faces["guard"] = frame.Dir(gp.facing);
            var pp = g.PatrolPose(); Set("patrol", pp?.pos, g.PatrolDown);
            RivalViews(g); // brief 059 K8: one view per member and one for the car, from RivalPoses (the three followers at one point are gone)
            var sp = g.SecurityPose();
            for (int i = 0; i < 2; i++) Set("security/" + i, sp.HasValue ? (i == 0 ? sp.Value.pos : Follower(g, "security", sp.Value.pos, i, new Pt(0, i * 90))) : (Pt?)null);
            if (!sp.HasValue) trails.Remove("security");
            Set("car/official", g.Vale == ValeAt.Car && g.Result != Outcome.Success ? (Pt?)null : g.CarPos());
            Set("car/getaway", g.GetawayCarPos()); // brief 059 K6: its stand, or along its Drive
            // the crowd: people the engine puts on one point are spread for the eye only (HostCrowdSpread: hash-seeded spots 0.7 m apart, a
            // loose column when walking, drawn up to a wall when standing; the engine is not told)
            Spread(g);
            for (int i = 0; i < g.Walkers.Count; i++)
            {
                if (ListenOwns(crowdKeys[i])) continue; /* brief 085: a Listen's partner, drawn by the view alone */ if (!atNow[i].HasValue) { Set(crowdKeys[i], null); continue; }
                Set(crowdKeys[i], GiveWay(g, SpreadAt(i), i));
                if (SpreadFace(i) is { } sf) faces[crowdKeys[i]] = sf;
                else if (StillFacing(g, i) is { } ef) faces[crowdKeys[i]] = ef; // engine 0.34.0 (Pulse 067 L): one standing alone faces the way the engine reads him (Game.FacingNow: the Office's door guards face out, toward the kerb), so the figure and its cone agree
                // in a fight (Keel: startle, flee, shelter, return): a startled person cowers (squat and wide) where they stand, then runs for a
                // refuge at the crowd's run speed and is off the street while sheltered (the engine gives no position), so the square empties
                var ct = things[crowdKeys[i]].t; bool cower = g.Walkers[i].St == "startled";
                var want = cower ? new Vector3(1.2f, 0.75f, 1.2f) : Vector3.one; if (ct.localScale != want) ct.localScale = want;
            }
            Bodies(g);
            Units(g);
            RobotFaces(g); // brief 026: view only
            // the final fight's hostiles (officers in the loud branch, the rival ambush in the safe one)
            for (int i = 0; i < MaxHostiles; i++)
            {
                var h = i < g.Hostiles.Count ? g.Hostiles[i] : null;
                if (h != null && IsCrewId(h.Id)) { Set(hostileKeys[i], null); continue; } // brief 059 (059 §2 point 1): a crew member's fight body is drawn by his own view (RivalViews), never popped onto a fresh one
                Set(hostileKeys[i], h != null && h.Present ? h.Pos(g.Tick) : (Pt?)null, h != null && h.Down); // a downed hostile lies where it fell
                if (h != null) { Tint(hostileKeys[i], h.Id.StartsWith("rival") ? host.rivalBodyColour : host.securityColour); Dress(hostileKeys[i], h.Id.StartsWith("rival") ? RivalFigure(h.Id) : "security"); } // Forma 067: each member his own look
            }
        }

        // ---------------------------------------------------------------- brief 014: job families, role shapes, the crowd giving way

        /// <summary>The job families' calm tones, from the civic look's palette (contracts/world/civic-look.json). The signals (cones, guard, rivals)
        /// don't own their colours (Sky, 2026-10-16): they stay readable by drawing unlit and emissive with their own shape or motion cue, so a
        /// family tone needs no distance from them. Café staff peach (DK_Bloom_Peach),
        /// Office staff pool teal (DK_Pool_Civic), guards deep teal (DK_Enamel_Teal), drivers mint (DK_Enamel_Mint), everyone else steel (DK_Steel).
        /// The cell stays charcoal and the rivals glass dark: no family shares their tone.</summary>
        public static readonly Color Cafe = Hex(0xFCC4AC), Office = Hex(0x3A9AA6), GuardTone = Hex(0x1F6C67), DriverTone = Hex(0xA8D3BE), Passer = Hex(0xA9ABAE), Apron = Hex(0xEFE8D6), Cap = Hex(0x2B2F33);
        static Color Hex(int rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        /// <summary>A job's family: café (server, cook), Office (clerk, attendant, councillor), guard, driver, else a passer-by.</summary>
        public static string FamilyOf(string job) => job switch
        {
            "server" or "cook" or "waitress" => "cafe", "clerk" or "attendant" or "councillor" => "office", "guard" => "guard", "official-driver" or "driver" => "driver", _ => "passer",
        };
        static Color Family(string job) => FamilyOf(job) switch { "cafe" => Cafe, "office" => Office, "guard" => GuardTone, "driver" => DriverTone, _ => Passer };
        /// <summary>The cheap role shape: an apron band for café staff, a cap for guards and drivers.</summary>
        static string Shape(string job) => FamilyOf(job) switch { "cafe" => "apron", "guard" or "driver" => "cap", _ => "" };

        /// <summary>The crowd gives way to the people the cell knows (Inez, the door guard, the driver, Vale) for the eye only, so the known person
        /// draws on top of a cluster: a passer-by standing between the camera and one (up to 3 m in front, where a 1.75 m figure overlaps them at
        /// pitch 38) steps 0.7 m aside, and anyone within 0.7 m steps out to 0.7 m. The engine is not told.
        /// Suture's Q-SOLID give-way (0.2.122, briefs/060 "0.2.122 bot rows" cause B): on the cast's own point the side is the walker's hash (never
        /// always the camera's right), and a pushed point is kept only on free ground (the engine's walk grid); else the mirror side, else 1 m behind
        /// the cast person, else the engine's point (the relief guard on the door guard's post was pushed into the Office face).</summary>
        Pt GiveWay(Game g, Pt p, int walker)
        {
            var cam = host.View; Vector2 f = Vector2.up, r = Vector2.right;
            if (cam)
            {
                var o = frame.Engine(Vector3.zero); var ef = frame.Engine(new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized * 10f); var er = frame.Engine(new Vector3(cam.transform.right.x, 0, cam.transform.right.z).normalized * 10f);
                f = new Vector2(ef.X - o.X, ef.Y - o.Y).normalized; r = new Vector2(er.X - o.X, er.Y - o.Y).normalized;
            }
            foreach (var cp in new[] { g.PersonPos(g.Inez), g.GuardPose()?.pos, g.DriverPos(), g.ValePos() })
            {
                if (!cp.HasValue) continue;
                var d = new Vector2(p.X - cp.Value.X, p.Y - cp.Value.Y);
                float front = -Vector2.Dot(d, f), side = Vector2.Dot(d, r);
                var d0 = d;
                if (front > -40f && front < 300f && Mathf.Abs(side) < 70f) { side = side >= 0 ? 70f : -70f; d = -f * front + r * side; }
                if (d.sqrMagnitude < 70f * 70f) d = d.sqrMagnitude < 1f ? r * (walker >= 0 && walker < idHash.Length && (idHash[walker] & 1) != 0 ? -70f : 70f) : d.normalized * 70f;
                if (d == d0) continue; // not in the way: the point stands
                var c = cp.Value; Pt At(Vector2 v) => new Pt(c.X + Mathf.RoundToInt(v.x), c.Y + Mathf.RoundToInt(v.y));
                var grid = host.CarWalk; var pushed = At(d); // engine 0.33.1 (Keel's addendum): the free test sees the standing cars
                if (grid.Free(pushed)) { p = pushed; continue; }
                var mirror = At(d - 2f * r * Vector2.Dot(d, r)); if (grid.Free(mirror)) { p = mirror; continue; }
                var behind = At(f * 100f); if (grid.Free(behind)) { p = behind; continue; }
                GiveWayKept++; // none free: the engine's point
            }
            return p;
        }

        // ---------------------------------------------------------------- brief 013: bodies and the response's units

        /// <summary>A person's view key in orders and views: the cast by role, the crowd by id.</summary>
        public static string KeyOf(Person p) => p.Role.Length > 0 ? p.Role : p.Id;
        readonly Dictionary<Person, int> walkerIndex = new();
        static readonly Dictionary<string, string> CastKeys = new() { ["waitress"] = "waitress", ["driver"] = "driver", ["guard"] = "guard", ["councillor"] = "councillor" };

        /// <summary>Every body the engine keeps (Game.Bodies): lying where it fell (laid flat), on its carrier's shoulder, or hidden in a stash
        /// (not drawn: the stash marker counts it). The person's own view (cast or crowd) gives way to the body while they are down. Kills
        /// are shown (Sky 2026-10-16): the body lies on the ground until it is moved.</summary>
        void Bodies(Game g)
        {
            if (walkerIndex.Count == 0) for (int i = 0; i < g.Walkers.Count; i++) walkerIndex[g.Walkers[i].P] = i;
            foreach (var b in g.Bodies)
            {
                var p = b.P; var key = "body/" + KeyOf(p);
                if (!things.ContainsKey(key))
                {
                    var job = p.Role switch { "waitress" => "server", "driver" => "official-driver", "guard" => "guard", "councillor" => "councillor", _ => p.Job };
                    if (IsCrewId(p.Id)) { Person(key, host.rivalBodyColour); Dress(key, RivalFigure(p.Id)); } // engine 0.33.0: a rival taken down is a body like anyone's, in his own clothes (Forma 067: his own look)
                    else { Person(key, Family(job), shape: Shape(job)); Dress(key, BodyFigure(p)); }
                }
                bool down = p.Down || b.Seizing;
                if (down) // the person's own view hides; the body draws
                {
                    if (CastKeys.TryGetValue(p.Role, out var ck)) Set(ck, null);
                    else if (walkerIndex.TryGetValue(p, out var wi)) Set(crowdKeys[wi], null);
                    else if (IsCrewId(p.Id)) Set(p.Id, null);
                }
                if (!down || !b.Open) { Set(key, null); continue; }
                if (b.Seizing) { Set(key, b.At, false); continue; }                  // the struggle: still on their feet
                if (b.Carrier.HasValue) { Set(key, g.BodyPos(b), true, 1.15f); carriers[key] = OpKeys[(int)b.Carrier.Value]; continue; } // over the carrier's shoulder
                Set(key, b.At, true, Radius);                                            // where they fell, or were put down
            }
        }

        /// <summary>Engine 0.34.0 (Game.FacingNow): crowd person i's facing as the engine reads it, while the view stands still this tick (a walker
        /// faces where it walks, and the spread's knots face their middle: SpreadFace first); null when moving or the engine gives none.</summary>
        Vector3? StillFacing(Game g, int i)
        {
            if (!things.TryGetValue(crowdKeys[i], out var th)) return null; var d = th.to - th.from; d.y = 0f; if (d.sqrMagnitude > 1e-4f) return null;
            Pt? f = null; try { f = g.FacingNow(g.Walkers[i].P.Id); } catch (System.Exception) { return null; }
            return f is Pt p && (p.X != 0 || p.Y != 0) ? frame.Dir(p) : null;
        }

        /// <summary>Pulse 067 (briefs/067/pulse.md §1.3): a body wears the figure its person was drawn in: a walker's is DressCrowd's (re-picked when
        /// walkers who set out together drew the same), not the plain pick, so nobody changes clothes when knocked out (16 of the 126 did); the
        /// cast and anyone not drawn yet take the plain pick.</summary>
        string BodyFigure(Person p) => walkerIndex.TryGetValue(p, out var bw) && bw < crowdKeys.Length && figs.TryGetValue(crowdKeys[bw], out var wf) ? wf.id : PersonFigure(p);
        /// <summary>For the proofs (Pulse 067): the walkers whose body would change figure under the plain pick (DressCrowd re-picked them), and how
        /// many bodies drawn now wear another figure than their walker did (0 since the fix).</summary>
        public (int repicked, int bodiesChanged) BodyFigureCheck()
        {
            var g = host.Game; int re = 0, changed = 0; if (g == null) return (0, 0);
            if (walkerIndex.Count == 0) for (int i = 0; i < g.Walkers.Count; i++) walkerIndex[g.Walkers[i].P] = i;
            for (int i = 0; i < g.Walkers.Count && i < crowdKeys.Length; i++) if (figs.TryGetValue(crowdKeys[i], out var wf) && PersonFigure(g.Walkers[i].P) != wf.id) re++;
            foreach (var b in g.Bodies)
                if (walkerIndex.TryGetValue(b.P, out var bi) && bi < crowdKeys.Length && figs.TryGetValue(crowdKeys[bi], out var w) && figs.TryGetValue("body/" + KeyOf(b.P), out var bf) && bf.id != w.id) changed++;
            return (re, changed);
        }

        static readonly Color RobotColour = new(0.78f, 0.8f, 0.82f);
        /// <summary>The response dial's units (Game.Units, not the legacy officers or the patrol, which have their own views): robots as squat
        /// drums, officers as people, where UnitPose puts them; null off the street.</summary>
        void Units(Game g)
        {
            foreach (var u in g.Units)
            {
                if (u.Legacy || u.Patrol) continue;
                var key = "unit/" + u.Id;
                if (!things.ContainsKey(key)) { if (u.Robot) Drum(key, RobotColour); else { Person(key, host.securityColour); Dress(key, "security"); } }
                var pose = g.UnitPose(u); Set(key, pose?.pos);
            }
        }

        // ---------------------------------------------------------------- engine 0.18: the fight range's cars and bystanders

        readonly HashSet<string> rangeCarKeys = new(), carsSeen = new();
        public readonly HashSet<string> BystanderStartled = new();
        int bystanderViews;
        const int MaxBystanders = 40;
        static MaterialPropertyBlock carMpb;

        /// <summary>Each car the range spawned (Game.RangeCars): Vela's sedan on its slot, long side across the firing line (the engine's nose
        /// +y); scorched while it burns, charred once it blows (the wreck stays until a car is spawned on its slot). Smoke, flame, the blast and
        /// the char are the car effects' (brief 078, HostCarFx / CarFxPool), as for the Handover's cars; the blast ring stays the HUD's.</summary>
        void RangeCars(Game g)
        {
            carsSeen.Clear();
            foreach (var c in g.RangeCars())
            {
                var key = "car/" + c.Id.Substring(4); carsSeen.Add(key);
                if (!things.ContainsKey(key))
                {
                    Box(key, new Vector3(1.8f, 1.4f, 4.6f), host.carColour, true);
                    if (Visual(key, host.sedanOfficial) is Transform m) { m.localRotation = Quaternion.LookRotation(frame.Dir(c.Facing), Vector3.up) * carNose; carModels[key] = m; }
                    rangeCarKeys.Add(key);
                }
                Set(key, c.At);
                // brief 078: the paint's char is the car effects' (HostCarFx: CarTint, on the mission clock), as for the Handover's cars
            }
            foreach (var key in rangeCarKeys) if (!carsSeen.Contains(key)) Set(key, null);
        }

        readonly Dictionary<string, float> carTint = new();
        /// <summary>Brief 078: a car view's paint darkened to `k` (1 = as built), written only when it changes (HostCarFx, each frame).</summary>
        public void CarTint(string key, float k)
        {
            if (!things.ContainsKey(key)) return; k = Mathf.Clamp01(k);
            float was = carTint.TryGetValue(key, out var w) ? w : 1f; if (Mathf.Abs(was - k) < 0.01f && (k < 1f || was >= 1f)) return;
            carTint[key] = k; Scorch(key, k);
        }

        /// <summary>A car's paint darkened by `k` (1 = as built): every renderer under its view, by a property block (shared materials untouched).</summary>
        void Scorch(string key, float k)
        {
            carMpb ??= new MaterialPropertyBlock();
            foreach (var r in things[key].t.GetComponentsInChildren<Renderer>(true))
            {
                if (k >= 1f) { r.SetPropertyBlock(null); continue; }
                r.GetPropertyBlock(carMpb); var baseC = r.sharedMaterial && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
                carMpb.SetColor("_BaseColor", new Color(baseC.r * k, baseC.g * k, baseC.b * k, baseC.a)); r.SetPropertyBlock(carMpb);
            }
        }

        /// <summary>The range's bystanders (Game.RangeBystanders), in spawn order: passers-by from the figure pool, crouched while startled, running
        /// for the far refuges, off the floor while safe, lying where they fell. View key bystander/&lt;index in spawn order&gt;.</summary>
        void RangeBystanders(Game g)
        {
            var list = g.RangeBystanders(); BystanderStartled.Clear();
            for (int i = 0; i < list.Count && i < MaxBystanders; i++)
            {
                var b = list[i]; var key = "bystander/" + i;
                if (i >= bystanderViews)
                {
                    Person(key, Passer, false, true); bystanderViews = i + 1;
                    if (host.figures != null) Dress(key, host.figures.Pick("family/passer", b.Id));
                }
                Set(key, b.State == "safe" ? (Pt?)null : b.Pos, b.Down);
                if (!b.Moving && (b.Facing.X != 0 || b.Facing.Y != 0)) faces[key] = frame.Dir(b.Facing);
                bool cower = b.State == "startled"; if (cower) BystanderStartled.Add(key);
                var t = things[key].t; var want = cower ? new Vector3(1.2f, 0.75f, 1.2f) : Vector3.one; if (!figs.ContainsKey(key) && t.localScale != want) t.localScale = want;
            }
            for (int i = list.Count; i < bystanderViews; i++) Set("bystander/" + i, null);
        }

        /// <summary>A bystander's view key from its engine id, or null.</summary>
        public string BystanderKey(Game g, string id)
        {
            var list = g.RangeBystanders();
            for (int i = 0; i < list.Count && i < MaxBystanders; i++) if (list[i].Id == id) return "bystander/" + i;
            return null;
        }

        void Drum(string key, Color c)
        {
            var go = new GameObject(key); go.transform.SetParent(root, false);
            Part(go.transform, "body", cylinder, Lit(c), new Vector3(0, 0.6f, 0), new Vector3(0.75f, 0.6f, 0.75f));
            var lamp = Part(go.transform, "head", sphere, host.SignalMat(host.coneColour, false), new Vector3(0, 1.28f, 0), Vector3.one * 0.22f); // the lamp: a signal, unlit, no fog
            var col = go.AddComponent<CapsuleCollider>(); col.height = 1.4f; col.radius = 0.45f; col.center = new Vector3(0, 0.7f, 0);
            go.SetActive(false);
            things[key] = (go.transform, Vector3.zero, Vector3.zero, false);
            if (Visual(key, host.robotPorter) is Transform m) // brief 020: the porter; the lamp rides on top of it
            {
                float top = 0f; foreach (var r in m.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y - go.transform.position.y);
                if (top > 0.3f) lamp.localPosition = new Vector3(0, top + 0.12f, 0);
                carModels[key] = m; // it faces where it goes, as a car does
            }
        }

        void Tint(string key, Color c)
        {
            var r = things[key].t.Find("body")?.GetComponent<MeshRenderer>(); if (!r) return;
            if (r.sharedMaterial.GetColor("_BaseColor") != c) r.sharedMaterial.SetColor("_BaseColor", c);
        }

        /// <summary>Brief 025 (take control): the driven agent's view key, drawn from the host's controller (position, facing, ground speed), not
        /// from the engine's eased 10 Hz pose, so input feels immediate. Null when nobody is driven.</summary>
        public string DriveKey; public Vector3 DrivePos; public float DriveYaw, DriveSpeed; float driveY; string driveKeyWas;
        /// <summary>Every shooter's target this frame (view key → the target's drawn position) and who fired within 0.4 s: the figure, robot or
        /// vehicle turns to face it, whatever it is (Sky 2026-10-20: "anything shooting at a target - must turn to face it").</summary>
        public readonly Dictionary<string, Vector3> Aim = new(); public readonly HashSet<string> FiredNow = new();

        /// <summary>Per frame: eases positions, draws the cones and the selection rings.</summary>
        /// <summary>For checks (ProofLevels): the most a figure was lifted onto the floor at its own place since the last reset (m): how far the plain ease would have sunk it.</summary>
        public static float FeetRaisedMax;
        public void Frame(float alpha)
        {
            GetawayLeaving(); LentLeaving(); // brief 079: the car that left drives away (HostViews.Leaving); brief 096: Popovic's car too (HostViews.LeavingLent)
            FallenSilhouettes();
            foreach (var kv in things)
            {
                var (t, from, to, shown) = kv.Value;
                if (t.gameObject.activeSelf != shown) t.gameObject.SetActive(shown);
                if (shown && kv.Key == DriveKey)
                {   // the controller's (brief 025); brief 055: the floor comes in 0.25 m cells (a stair steps about 0.1 m a cell), so its height eases over ~0.1 s
                    float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / 0.05f); driveY = driveKeyWas == DriveKey && Mathf.Abs(DrivePos.y - driveY) < 1.5f ? Mathf.Lerp(driveY, DrivePos.y, k) : DrivePos.y; driveKeyWas = DriveKey;
                    t.position = new Vector3(DrivePos.x, driveY, DrivePos.z);
                }
                else if (shown && !kv.Key.StartsWith("camera/") && !kv.Key.StartsWith("marker/") && !kv.Key.StartsWith("stash/"))
                {
                    var p = Vector3.Lerp(from, to, alpha);
                    // Splice on ProofLevels (0.2.113): eased straight between two ticks, a figure climbing a stair (0.15-0.25 m a cell) cuts under the
                    // tread ahead for part of the tick; a figure is never drawn under the floor at its own place (the climb snaps up; a descent eases)
                    if (from.y != to.y && figs.ContainsKey(kv.Key) && !(lifted.TryGetValue(kv.Key, out var lf) && lf > 0.01f))
                    {
                        var b = frame.ToBlock(new Vector2(p.x, p.z)); float floor = frame.Height(b.x, b.y);
                        if (floor > p.y && floor - p.y < 0.5f) { FeetRaisedMax = Mathf.Max(FeetRaisedMax, floor - p.y); p.y = floor; }
                    }
                    t.position = p;
                }
            }
            var g = host.Game;
            Untangle();
            for (int i = 0; i < 4; i++)
            {
                var r = things[SelKeys[i]].t; var op = things[OpKeys[i]];
                bool on = host.Selected[i] && op.shown;
                if (r.gameObject.activeSelf != on) r.gameObject.SetActive(on);
                if (on) r.position = op.t.position + Vector3.up * 0.03f;
                var d = things[DestKeys[i]].t; var walk = g.Ops[i].Walk; bool dest = RouteLines && walk != null && op.shown; // the HUD's numbered pin replaces the ring
                if (d.gameObject.activeSelf != dest) d.gameObject.SetActive(dest);
                if (dest) { d.position = frame.World(walk.End) + Vector3.up * 0.03f; d.localScale = new Vector3(0.6f, 0.01f, 0.6f); }
            }
            host.AimTargets(Aim, FiredNow); // Sky 2026-10-20: every shooter faces its target (one rule, HandoverHost.AimTargets)
            Figures(alpha); Cars(); CellRings(); RivalRings();
            AttentionViews(); // brief 031: lenses, faces and heads (HostViews.Attention)
        }

        /// <summary>Glyph 022 (N119: the Enforcer at the public door stood in the guard, one figure where two were): the engine may put an agent
        /// on the very point a cast member stands (the door is the guard's post); the agent's view steps 0.7 m aside, across the other's facing,
        /// so both are drawn. View only: the engine's positions are untouched.</summary>
        void Untangle()
        {
            foreach (var who in new[] { "guard", "waitress", "driver" })
            {
                if (!things.TryGetValue(who, out var o) || !o.shown) continue; var op0 = o.t.position;
                Vector3 side = faces.TryGetValue(who, out var fc) ? new Vector3(fc.z, 0f, -fc.x).normalized : Vector3.right;
                for (int i = 0; i < 4; i++)
                {
                    var a = things[OpKeys[i]]; if (!a.shown) continue; var d = a.t.position - op0; d.y = 0f;
                    if (d.sqrMagnitude < 0.45f * 0.45f) a.t.position = op0 + side * 0.7f + Vector3.up * (a.t.position.y - op0.y);
                }
            }
        }

        /// <summary>Brief 020: where the signal figures stand as drawn (hostiles, units, the patrol, the rivals, the legacy officers), shown only.</summary>
        public IEnumerable<Vector3> SignalPeople()
        {
            foreach (var kv in things)
            {
                var k = kv.Key; if (!(k.StartsWith("hostile/") || k.StartsWith("unit/") || IsCrewId(k) || k.StartsWith("security/") || k == "patrol")) continue;
                if (lifted.ContainsKey(k)) continue; // brief 079: a fallen hostile needs no cut (the second half of the fight's patch through the café, open on his body to the end card)
                var (t, _, _, shown) = kv.Value; if (shown && t.gameObject.activeInHierarchy) yield return t.position;
            }
        }

        void Set(string key, Pt? at, bool down = false, float lift = 0f)
        {
            var x = things[key];
            if (!at.HasValue) { things[key] = (x.t, x.to, x.to, false); return; }
            var w = frame.World(at.Value) + Vector3.up * lift;
            if (down) lifted[key] = lift; else lifted.Remove(key); // brief 020: a figure lies (or rides a shoulder) instead of the capsule laid flat
            bool wasShown = x.shown;
            if (wasShown && key == "patrol" && (w - x.to).magnitude > 2.5f) wasShown = false; // brief 057 §2: a jump (the engine's reroute before it walks one) snaps, never a 40 m slide in a tick
            things[key] = (x.t, wasShown ? x.to : w, w, true);
            if (down) x.t.localRotation = Quaternion.Euler(90f, 0f, 0f); else if (x.t.localRotation != Quaternion.identity) x.t.localRotation = Quaternion.identity; // down: laid flat
        }

        void Person(string key, Color body, bool cell = false, bool collide = true, string shape = "")
        {
            var go = new GameObject(key); go.transform.SetParent(root, false);
            float bodyH = Tall - HeadD - 0.03f;
            Part(go.transform, "body", capsule, Lit(body), new Vector3(0, bodyH / 2, 0), new Vector3(Radius * 2, bodyH / 2, Radius * 2));
            Part(go.transform, "head", sphere, cell ? Lit(host.cellHeadColour) : head, new Vector3(0, Tall - HeadD / 2, 0), Vector3.one * HeadD);
            if (shape == "apron") Part(go.transform, "apron", cylinder, Lit(Apron), new Vector3(0, 0.78f, 0), new Vector3(Radius * 2 + 0.05f, 0.17f, Radius * 2 + 0.05f)); // a cream band from waist to hip
            if (shape == "cap") Part(go.transform, "cap", cylinder, Lit(Cap), new Vector3(0, Tall - 0.02f, 0), new Vector3(HeadD + 0.07f, 0.035f, HeadD + 0.07f));    // a flat crown with a brim's width
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

        // ---------------------------------------------------------------- group followers (Suture's bot triage, 0.2.115)

        readonly Dictionary<string, List<Pt>> trails = new();
        /// <summary>For checks: followers drawn this build that had to be moved onto free ground.</summary>
        public static int FollowerSnaps;
        /// <summary>Crowd points GiveWay left on the engine's point because no pushed side was free ground (Suture's Q-SOLID cause B).</summary>
        public static int GiveWayKept;

        /// <summary>Follower `idx` of a group whose leader the engine places at `lead`: on the leader's recent path 0.9 m x idx back while it walks
        /// (the path is free ground: the engine walked it), else `offset` beside it; then on a free walk cell (the nearest free cell within 2 m),
        /// else on the leader. View only.</summary>
        Pt Follower(Game g, string group, Pt lead, int idx, Pt offset)
        {
            if (!trails.TryGetValue(group, out var tr)) trails[group] = tr = new List<Pt>();
            if (tr.Count == 0 || G.Dist(tr[^1], lead) >= 10) { tr.Add(lead); if (tr.Count > 64) tr.RemoveAt(0); }
            long want = 90L * idx; Pt at = lead + offset; long run = 0; bool onPath = false;
            for (int k = tr.Count - 1; k > 0; k--)
            {
                long seg = G.Dist(tr[k], tr[k - 1]); if (seg <= 0) continue;
                if (run + seg >= want) { long t = want - run; at = new Pt(tr[k].X + (int)((tr[k - 1].X - tr[k].X) * t / seg), tr[k].Y + (int)((tr[k - 1].Y - tr[k].Y) * t / seg)); onPath = true; break; }
                run += seg;
            }
            if (onPath && G.Dist(lead, tr[^1]) > 300) onPath = false; // the leader jumped (a respawn): the old trail is not its path
            if (!onPath) at = lead + offset;
            var grid = g.S.Grid; if (grid.Free(at)) return at;
            FollowerSnaps++; var nf = grid.NearestFree(at);
            return nf.HasValue && G.Dist(nf.Value, at) <= 200 ? nf.Value : lead;
        }

        // ---------------------------------------------------------------- sight fans (the Scout's skill)

        sealed class FanDraw
        {
            public GameObject Go; public Mesh Fill, Rim, Under, RimS; public MeshRenderer FillR, RimR, UnderR, RimSR; public Fan For; public bool Live, Looped; public string Kind = "", Watcher = "";
            /// <summary>Redline 064 #2: each fill vertex's place from the lens to the far edge (0..1), so a fade rewrites colours only; Rim is the rest rim, RimS the strong.</summary>
            public readonly List<float> FillT = new();
            public Vector3 Hub; public readonly List<Vector3> Ring = new();
            /// <summary>Brief 057 §3: how strong it draws now (0 rest .. 1 strong), eased toward Want; the strength the meshes were last built at.</summary>
            public float Strength = -1f, Want, BuiltAt = -9f; public float Reach = 1f; public int Seen;
        }
        readonly Dictionary<string, FanDraw> fanBy = new(); readonly HashSet<string> fanSeen = new();
        readonly Dictionary<(Color, float), Material> fanMats = new();
        readonly Dictionary<Color, Material> fillMats = new();
        readonly List<Vector3> verts = new(); readonly List<int> tris = new(); readonly List<Color> cols = new();
        // brief 057 §3 (Sky 2026-10-24: "once watched its cone now permanently appears but we need to make it softer, kinda dominates"): two strengths
        // of one cone. Rest: fill 13 % at the lens fading to 1 % at the far edge, rim 0.05 m at 55 %. Strong (hovered, named, an agent in or near
        // it, a path through it, Alert 2, the watcher noticing): fill 34 % fading to 14 %, rim 0.085 m at 100 %. In over 0.25 s, out over 0.6 s.
        const float RestHub = 0.13f, RestFar = 0.01f, StrongHub = 0.34f, StrongFar = 0.14f, RestRimW = 0.05f, RestRimA = 0.55f, StrongRimW = 0.085f, InS = 0.25f, OutS = 0.6f;
        const float RimW = 0.085f, FillY = 0.05f, RimY = 0.07f; // Glyph techwatch r2 C: the rim 2 px or more at play zoom where it runs across the view (0.085 m x 38.6 px/m x sin 38), the fill a little up, so a cone under a ghosted tower's dither still reads
        float rimW = RimW;
        static readonly Color Grey = new(0.55f, 0.55f, 0.55f);
        /// <summary>The proof reads it: each drawn fan's watcher and strength now ("camera:entrance=0.00").</summary>
        /// <summary>The proof reads it: the watchers whose drawn fan is looped (hollow, dashed all round).</summary>
        public string LoopedFans { get { var l = new List<string>(); foreach (var kv in fanBy) if (kv.Value.Go.activeSelf && kv.Value.Looped) l.Add(kv.Key); return string.Join(" ", l); } }
        public string FanStrengths { get { var l = new List<string>(); foreach (var kv in fanBy) l.Add($"{kv.Key}={(kv.Value.Go.activeSelf ? kv.Value.Strength : 0f):0.00}"); return string.Join(" ", l); } } // brief 067 F: a cone gone from the street (0, hidden) still lists at 0

        /// <summary>The fans to draw this frame (every learned one at rest, the ones that matter strong); the rest hide. Each mesh is rebuilt
        /// when the engine hands back a different Fan (its cache keeps one per pose), its Live flag flips, or its strength moves.</summary>
        public void ShowFans(IReadOnlyList<Fan> list, IReadOnlyList<bool> strong = null)
        {
            var seen = fanSeen; seen.Clear(); int n = list?.Count ?? 0; float dt = Time.unscaledDeltaTime; // Redline 064 #2: one set, reused
            for (int i = 0; i < n; i++)
            {
                var f = list[i]; string key = f.Watcher.Length > 0 ? f.Watcher : "fan/" + i;
                if (CameraDead(f.Watcher) || FightCues.Blind && !f.Live) continue; // brief 084: the hollow cone's instead (HudToolkit.CuesBlind) // Glyph 058 §2: a camera cut or shot out has no cone (its dead disc stays)
                if (!seen.Add(key)) continue;
                if (!fanBy.TryGetValue(key, out var d)) fanBy[key] = d = NewFan(key);
                d.Want = strong == null ? 1f : i < strong.Count && strong[i] ? 1f : 0f;
                // brief 067 F3 (Glyph): no rest strength: a cone that stops mattering leaves the street (out over 0.6 s), one that starts comes in over 0.25 s
                if (!d.Go.activeSelf) { if (d.Want <= 0f) continue; d.Go.SetActive(true); d.Strength = 0f; d.For = null; }
                d.Strength = Mathf.MoveTowards(d.Strength, d.Want, dt / (d.Want > d.Strength ? InS : OutS));
                if (d.Strength <= 0f && d.Want <= 0f) { d.Go.SetActive(false); d.For = null; continue; }
                if (!ReferenceEquals(d.For, f) || d.Live != f.Live || d.Looped != LoopedFan(f)) Build(d, f);
                else if (Mathf.Abs(d.Strength - d.BuiltAt) > 0.02f || (d.Strength == d.Want && d.BuiltAt != d.Strength)) Restrength(d);
            }
            foreach (var kv in fanBy) if (!seen.Contains(kv.Key) && kv.Value.Go.activeSelf) { kv.Value.Go.SetActive(false); kv.Value.For = null; }
        }

        FanDraw NewFan(string key)
        {
            var d = new FanDraw { Go = new GameObject("fan/" + key), Watcher = key }; d.Go.transform.SetParent(root, false);
            (d.Fill, d.FillR) = MeshPart(d.Go.transform, "fill"); (d.Rim, d.RimR) = MeshPart(d.Go.transform, "rim"); (d.RimS, d.RimSR) = MeshPart(d.Go.transform, "rim-strong"); (d.Under, d.UnderR) = MeshPart(d.Go.transform, "shadow-edge"); // engine 0.28: the ink under a shadow cut's rim
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
            m = host.SignalMat(new Color(c.r, c.g, c.b, alpha), alpha < 1f); if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f); // Glint 2026-10-23: no fog on a signal
            if (m.HasProperty("_OffsetFactor")) { m.SetFloat("_OffsetFactor", -2f); m.SetFloat("_OffsetUnits", -2f); } // Aster 2026-10-23 (Glint's 150 m control): the flat fan wins its depth ties with kerbs and paving (0.64 -> 0.75 in view); no cost
            return fanMats[(c, alpha)] = m;
        }
        /// <summary>Brief 057 §3: the fill's material: transparent, its alpha from the mesh's vertex colour (SignalUnlit _VertexAlpha), so the fill
        /// fades from the lens to the far edge.</summary>
        Material FillMat(Color c)
        {
            if (fillMats.TryGetValue(c, out var m)) return m;
            m = FanMat(c, 0.999f); m = new Material(m) { name = "signal-fill" }; if (m.HasProperty("_VertexAlpha")) m.SetFloat("_VertexAlpha", 1f);
            return fillMats[c] = m;
        }

        Color KindColour(string kind) => kind switch { "guard" => host.guardColour, "rivals" => host.rivalColour, _ => host.coneColour }; // §10: cones red, guard amber, rivals violet

        /// <summary>Glyph 058 §2 (board 02): the camera the running loop plays on: its cone draws recorded, not live: still red, hollow, dashed all round.</summary>
        bool LoopedFan(Fan f) { var g = host.Game; return g != null && !f.Live && g.LoopOn && f.Watcher == "camera:" + g.LoopCam; }
        bool CameraDead(string watcher) { var g = host.Game; if (g == null || !watcher.StartsWith("camera:", System.StringComparison.Ordinal)) return false; try { return g.CameraDeadAs(watcher[7..]).Length > 0; } catch { return false; } }
        const float LoopRimW = 0.05f, LoopRimA = 0.5f, LoopDashOn = 0.13f, LoopDashOff = 0.10f;

        void Build(FanDraw d, Fan f)
        {
            d.For = f; d.Live = f.Live; d.Kind = f.Kind; d.Looped = LoopedFan(f);
            var pts = f.Points; int n = pts.Length;
            d.Ring.Clear(); for (int i = 0; i < n; i++) d.Ring.Add(frame.World(pts[i]));
            d.Hub = frame.World(f.At); d.Reach = 1f; foreach (var r in d.Ring) d.Reach = Mathf.Max(d.Reach, Flat(r - d.Hub).magnitude);
            Geometry(d); ApplyStrength(d);
        }

        /// <summary>A strength step. Redline 064 #2: colours and which rim shows only; the clipped geometry stays (PerfLegacy rebuilds it all, as
        /// the round did, for the perf probe).</summary>
        void Restrength(FanDraw d)
        {
            if (d.For == null) return;
            if (HandoverHost.PerfLegacy) Geometry(d);
            ApplyStrength(d);
        }

        /// <summary>The fan's meshes from its polygons, once a pose: the fill (shadows cut out; each vertex's place from the lens kept for its
        /// alpha), the rest rim and the strong rim (their widths), the shadow cuts' ink.</summary>
        /// <summary>The perf probe reads it: fan geometry builds since the last read.</summary>
        public static int FanGeometries;
        void Geometry(FanDraw d)
        {
            var f = d.For; if (f == null) return; FanGeometries++;
            // a mast (brief 013): one polygon a head; heads 2 and 3 are the fan's More, drawn the same way from the same pole
            var polys = new List<(List<Vector3> ring, string[] stop)> { (d.Ring, f.Stop) };
            for (int h = 0; h < f.More.Length; h++) { var r2 = new List<Vector3>(); foreach (var q in f.More[h]) r2.Add(frame.World(q)); polys.Add((r2, h < f.MoreStop.Length ? f.MoreStop[h] : System.Array.Empty<string>())); }
            // the fill: a fan of triangles from the watcher (the engine's polygon is star-shaped round it: its rays start there)
            verts.Clear(); tris.Clear();
            foreach (var (ring, _) in polys)
            {
                int m = ring.Count; if (m < 3) continue; int b0 = verts.Count;
                verts.Add(d.Hub + Vector3.up * FillY);
                for (int i = 0; i < m; i++) verts.Add(ring[i] + Vector3.up * FillY);
                for (int i = 0; i < m; i++) { tris.Add(b0); tris.Add(b0 + 1 + i); tris.Add(b0 + 1 + (i + 1) % m); }
            }
            ShadowsOf(f); CutFill(); // engine 0.28 (hud29 securityFan.shadows): Fan.Shadows cut out of the fill
            d.FillT.Clear(); foreach (var v in verts) d.FillT.Add(Mathf.Clamp01(Flat(v - d.Hub).magnitude / d.Reach));
            d.Fill.Clear(); d.Fill.SetVertices(verts); d.Fill.SetTriangles(tris, 0); d.Fill.RecalculateBounds();
            var c = f.Live || d.Looped ? KindColour(f.Kind) : Grey;
            // the rims: crisp along walls and the near circle; the far edge (both ends stopped by reach) in the kind's cue; rest and strong widths
            for (int pass = 0; pass < 2; pass++)
            {
                rimW = d.Looped ? LoopRimW : pass == 0 ? RestRimW : StrongRimW;
                verts.Clear(); tris.Clear(); float dash = 0f;
                foreach (var (ring, stop) in polys) { int m = ring.Count; if (d.Looped) { LoopRim(ring, ref dash); continue; } RimOf(d, f, ring, stop, ref dash); if (!f.Live && m >= 3) Hatch(ring); }
                ShadowRims(polys, c, d); // each cut gets a thin rim of the fan's colour over ink-deep (the ink in its own mesh)
                var mesh = pass == 0 ? d.Rim : d.RimS; mesh.Clear(); mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
            }
            rimW = RimW; d.BuiltAt = -9f;
        }

        /// <summary>The fan at its strength now: the fill's vertex alphas (rest 13 % to 1 %, strong 34 % to 14 %, lerped) and the rest or the strong
        /// rim (the switch at half way, as the rim's two materials were).</summary>
        void ApplyStrength(FanDraw d)
        {
            var f = d.For; if (f == null) return; float s = Mathf.Clamp01(d.Strength < 0f ? 1f : d.Strength); d.BuiltAt = s;
            var c = f.Live || d.Looped ? KindColour(f.Kind) : Grey;
            void Mat(Renderer r, Material m) { if (r.sharedMaterial != m) r.sharedMaterial = m; } // brief 045 F6: no material set when it is the same (a set flags the renderer for the GPU Resident Drawer)
            // brief 067 F3 (Glyph): one look, the strong one, faded by the strength (0 is not drawn): fill 34 % to 14 % by distance, the strong rim
            // (its alpha in tenths, so a fade uses at most ten materials a colour); the rest rim is retired
            float aHub = StrongHub * s, aFar = StrongFar * s;
            if (d.Looped) { aHub = 0f; aFar = 0f; } // looped: hollow (no fill), the rim at 50 % whatever its strength
            cols.Clear(); foreach (var t in d.FillT) cols.Add(new Color(1f, 1f, 1f, Mathf.Lerp(aHub, aFar, t)));
            if (cols.Count == d.Fill.vertexCount) d.Fill.SetColors(cols);
            float rimA = Mathf.Max(0.1f, Mathf.Round((d.Looped ? LoopRimA : 1f) * s * 10f) / 10f);
            Mat(d.FillR, FillMat(c)); Mat(d.RimSR, FanMat(c, rimA));
            if (d.RimR.enabled) d.RimR.enabled = false; bool on = s > 0.01f; if (d.RimSR.enabled != on) d.RimSR.enabled = on;
        }

        /// <summary>Glyph 058 §2: a looped cone's rim, dashed all round (0.13 m on, 0.10 m off: 5 px on, 4 off at play zoom), the phase carried.</summary>
        void LoopRim(List<Vector3> ring, ref float dash)
        {
            int n = ring.Count; float period = LoopDashOn + LoopDashOff;
            for (int i = 0; i < n && n >= 3; i++)
            {
                Vector3 a = ring[i] + Vector3.up * RimY, b = ring[(i + 1) % n] + Vector3.up * RimY; float len = Vector3.Distance(a, b); if (len < 1e-4f) continue;
                for (float t = -dash; t < len; t += period) { float s0 = Mathf.Max(0f, t), s1 = Mathf.Min(len, t + LoopDashOn); if (s1 > s0) QuadCut(Vector3.Lerp(a, b, s0 / len), Vector3.Lerp(a, b, s1 / len), rimW); }
                dash = (dash + len) % period;
            }
        }

        void RimOf(FanDraw d, Fan f, List<Vector3> Ring, string[] Stop, ref float dash)
        {
            int n = Ring.Count; float w = rimW;
            for (int i = 0; i < n && n >= 3; i++)
            {
                int j = (i + 1) % n; Vector3 a = Ring[i] + Vector3.up * RimY, b = Ring[j] + Vector3.up * RimY;
                bool far = Stop.Length == n && Stop[i] == "reach" && Stop[j] == "reach";
                if (!far) { QuadCut(a, b, w); continue; }
                var outward = Flat((a + b) * 0.5f - d.Hub).normalized;
                float len = Vector3.Distance(a, b);
                switch (f.Kind)
                {
                    case "guard":   // chevrons marching the way he faces, on a crisp rim (Glyph 020: the hairline left a flat amber wash)
                        QuadCut(a, b, w);
                        for (; dash < len; dash += 0.7f) if (!InShadow(Vector3.Lerp(a, b, dash / len))) Chevron(Vector3.Lerp(a, b, dash / len), outward, 0.32f, w);
                        dash -= len; break;
                    case "rivals":  // diamond ticks
                        QuadCut(a, b, w * 0.5f);
                        for (; dash < len; dash += 0.8f) if (!InShadow(Vector3.Lerp(a, b, dash / len))) Diamond(Vector3.Lerp(a, b, dash / len), outward, 0.3f);
                        dash -= len; break;
                    default:        // camera, patrol, officers: a dashed far edge (0.4 m on, 0.3 m off), the phase carried along the arc
                        for (float t = -dash; t < len; t += 0.7f) { float s0 = Mathf.Max(0f, t), s1 = Mathf.Min(len, t + 0.4f); if (s1 > s0) QuadCut(Vector3.Lerp(a, b, s0 / len), Vector3.Lerp(a, b, s1 / len), w); }
                        dash = (dash + len) % 0.7f; break;
                }
            }
        }
        // brief 057 §3: the sweeping scan line (brief 020's Scan, ScanEdges and its three line renderers) is gone: a fixed camera does not pan,
        // and the desk's 20 s scan already shows as the "reported" tag's draining underline

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

        void Chevron(Vector3 p, Vector3 o, float s, float w = RimW)
        {
            var side = Vector3.Cross(Vector3.up, o) * (s * 0.6f); var tip = p + o * (s * 0.5f); var back = p - o * (s * 0.3f);
            Quad(back + side, tip, w); Quad(back - side, tip, w);
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
