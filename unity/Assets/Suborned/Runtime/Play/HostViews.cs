using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>What the host draws from the engine each frame (brief 011 D, graybox): the cell's four operatives, the four cast roles, the
    /// patrol, the rivals, the response unit, the official and getaway cars, and the markers a click can hit (the public door, the Tech's
    /// cabinet, the two cameras). People are capsules as the people layer draws them (1.75 m, ivory head). Sight is the Scout's skill (Sky
    /// 2026-10-16; Glyph's review §3.4): no cone is drawn by default; the host asks for the fans it wants (ShowFans, HostViews.Fans), each the
    /// engine's own sight polygon (Game.ConeFan), and a signal colour's fans draw as one shape (HostViews.FanUnion: one light fill, one outline,
    /// the far edge in its kind's cue: red dashes, the guard's amber chevrons, the rivals' violet diamonds), unlit; grey when the watcher sees
    /// nothing now. Positions are the engine's, eased between its 10 Hz ticks.
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
                if (!c.World && (host.hoverCamera || host.cctvPole)) KitPole(c, pole); else if (c.World) MastPole(c, pole); // brief 055: Vela's CCTVPole at the plan's 5.5 m mount; brief 101: a world mast's own piece (HostViews.Mast)
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
            for (int i = 0; i < 2; i++) // brief 098 L9: each officer where the engine lays him (SecOfficerPose), facing his own sector at the end of his run (was a follower on the lead's trail);
            {                           // joined to the final fight, his hostile view draws him (HostFight.ViewKey), never two figures
                var op = i < g.SecGuns.Length && !InFight(g, g.SecGuns[i].Id) ? g.SecOfficerPose(i) : null; Set("security/" + i, op?.pos);
                if (op is { } o && (o.facing.X != 0 || o.facing.Y != 0)) faces["security/" + i] = frame.Dir(o.facing);
            }
            Set("car/official", g.Vale == ValeAt.Car && g.Result != Outcome.Success ? (Pt?)null : g.CarPos());
            Set("car/getaway", g.GetawayCarPos()); // brief 059 K6: its stand, or along its Drive
            // the crowd, where the engine lays each body (brief 098 L9: 60 cm apart, queues and arcs at a stop); HostCrowdSpread's eye-only spread
            // (hash-seeded spots, columns, GiveWay round the cast) runs only for a before-and-after (HostSpread)
            TrafficViews(g); GrowCrowd(g); Spread(g); // brief 098 step 1: the passing cars, and a crowd view for each driver who got out (HostViews.Traffic)
            for (int i = 0; i < g.Walkers.Count; i++)
            {
                if (ListenOwns(crowdKeys[i])) continue; /* brief 085: a Listen's partner, drawn by the view alone */ if (!atNow[i].HasValue) { Set(crowdKeys[i], null); continue; }
                Set(crowdKeys[i], HostSpread ? GiveWay(g, SpreadAt(i), i) : atNow[i].Value); // brief 098 L9: where the engine lays the body (HostCrowdSpread: the host's spread is off)
                if (SpreadFace(i) is { } sf) faces[crowdKeys[i]] = sf;
                else if (StillFacing(g, i) is { } ef) faces[crowdKeys[i]] = ef; // engine 0.34.0 (Pulse 067 L): one standing alone faces the way the engine reads him (Game.FacingNow: the Office's door guards face out, toward the kerb), so the figure and its cone agree
                // in a fight (Keel: startle, flee, shelter, return): a startled person cowers (squat and wide) where they stand, then runs for a
                // refuge at the crowd's run speed and is off the street while sheltered (the engine gives no position), so the square empties
                var ct = things[crowdKeys[i]].t; bool cower = g.Walkers[i].St == "startled";
                var want = cower ? new Vector3(1.2f, 0.75f, 1.2f) : Vector3.one; if (ct.localScale != want) ct.localScale = want;
            }
            GiveTick(g); Bodies(g); // brief 098 L9: the give and take (HostViews.Give: squeezing past, making room, a chair taken instead)
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

        // ---------------------------------------------------------------- brief 013: bodies and the response's units

        static bool InFight(Game g, string id) { foreach (var h in g.Hostiles) if (h.Id == id) return true; return false; }
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
            carMpb ??= new MaterialPropertyBlock(); // brief 098 step 1: a block per material slot (one per renderer gave every slot slot 0's colour: the kit cars are one renderer of ~10 slots, so tyres, glass and chrome took the body's)
            foreach (var r in things[key].t.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials; r.SetPropertyBlock(null); if (k >= 1f) { carMpb.Clear(); for (int i = 0; i < ms.Length; i++) r.SetPropertyBlock(carMpb, i); continue; } // as built: no overrides
                for (int i = 0; i < ms.Length; i++) { var baseC = ms[i] && ms[i].HasProperty("_BaseColor") ? ms[i].GetColor("_BaseColor") : Color.white; carMpb.Clear(); carMpb.SetColor("_BaseColor", new Color(baseC.r * k, baseC.g * k, baseC.b * k, baseC.a)); r.SetPropertyBlock(carMpb, i); }
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
            if (Visual(key, host.robotUnit) is Transform m) // brief 020: the robot model; brief 104: PatrolBot104 carries its own beacon, so the drum's lamp goes
            {
                float top = 0f; foreach (var r in m.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y - go.transform.position.y);
                if (Suborned.Robots.PatrolBot.Is(host.robotUnit)) lamp.gameObject.SetActive(false); else if (top > 0.3f) lamp.localPosition = new Vector3(0, top + 0.12f, 0);
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
            RivalsLeaving(); GetawayLeaving(); LentLeaving(); TrafficFrame(alpha); // brief 109 round 2: a rival car driving off when the clock stopped drives on out (HostViews.LeavingRivals), before ours reads where it is // brief 098 step 1: the passing cars' headings, eased; brief 079: the car that left drives away (HostViews.Leaving); brief 096: Popovic's car too (HostViews.LeavingLent)
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
            Figures(alpha); Cars(); Bots(); CellRings(); RivalRings(); // brief 104: the robots posed after they have turned (HostViews.PatrolBot)
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
