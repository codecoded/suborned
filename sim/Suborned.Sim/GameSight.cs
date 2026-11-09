// What the cell knows of the city's watchers, and the reads the HUD draws from it (Sky 2026-10-16: view cones are the Scout's skill;
// Glyph's review, briefs/011/ui/review-2026-10-16 §3.4; the rulebook's `sight` section).
//  · ConeFan(watcher): the watcher's sight now as a flat polygon on the ground, the engine's own test (rulebook perception: its cone or
//    sweep, the near circle, occluders at eye height; people on the street if asked). Integer only, read-only, cached by pose.
//  · Known(watcher): what the cell has learned of it, witnessed (a Scout Watch) or told (the start state), or known on arrival.
//    Learned knowledge is the cell's, not the Scout's: it stays when the Scout goes down.
//  · Routine(watcher, horizon): the watcher's schedule ahead (the patrol's turns and its door windows), once its routine is known.
//  · Detection(): every watcher that sees an operative now (the fill), known or not.
//  · Seen(...): the quote's "seen by": known watchers' true answer, "?" for an unknown watcher that could see (rulebook sight.unknown).
// None of these changes the game; only Watch (an order) and the arrival of the response unit or the rivals add knowledge.
// The fan's geometry is GameFan.cs and its shadows GameFanShadows.cs (split from here, brief 101).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>A watcher's sight as a polygon on the ground (cm, the block frame), for the HUD to fill.</summary>
    public sealed class Fan
    {
        public string Watcher = "";     // camera:<id>, guard, patrol, security-<n>, rivals
        public string Kind = "";        // camera, guard, patrol, security, rivals (Glyph's grammar: one shape cue each)
        public Pt At, Facing;           // where the watcher stands and the way it faces (cm; the facing is a direction, any length)
        public int Level;               // the ground the fan lies on, cm (0: the street; every Handover watcher stands on it)
        public bool Live;               // false: it sees nothing now (a looped or blocked camera, a rerouted patrol, the guard on the cell's side); the shape is its sight as it stands
        public int Reach;               // the farthest point from At, cm
        /// <summary>The polygon, counter-clockwise in the block frame (x right, y up), starting behind the watcher. When the watcher's own
        /// point is a corner (a camera's apex, the patrol's base) it is Points[0]. Empty when nothing is in sight.</summary>
        public Pt[] Points = Array.Empty<Pt>();
        /// <summary>What ends the sight at each point: apex, reach (the cone's or sweep's edge: the dashed far edge), near (the circle
        /// anyone within perception.near is seen in), blocked (a wall, roof or solid at eye height), crowd (a person on the street).</summary>
        public string[] Stop = Array.Empty<string>();
        /// <summary>A camera with several heads (the Records Square mast): the other heads' polygons and stops, as Points and Stop.</summary>
        public Pt[][] More = Array.Empty<Pt[]>(); public string[][] MoreStop = Array.Empty<string[]>();
        /// <summary>Brief 055 §7 (perception.mount): the shadows of solids the eye sees over or under, each a convex polygon the watcher does
        /// not see into although it lies inside Points (or More): a 2 m planting under a 4.79 m lens hides only a short stretch behind it.
        /// A point is in sight when it is in a polygon and in no shadow. Empty with the mount off.</summary>
        public Pt[][] Shadows = Array.Empty<Pt[]>();
    }

    /// <summary>What the cell knows of one watcher (rulebook sight).</summary>
    public sealed class WatcherKnown
    {
        public string Watcher = "", Kind = "";
        public bool Sight, Routine;     // its sight (the cone the HUD may draw), its routine (Routine() answers)
        public string How = "";         // watched, told, arrival; "" while unknown
        public int SightAt = -1, RoutineAt = -1; // the tick each was learned (-1: not, or known from the start)
        public int WatchedS;            // seconds of completed Scout Watch on it
        public int RoutineNeedsS;       // watched seconds its routine needs (sight.watch.routine), 0 when the routine comes with the sight or a person's Watch
    }

    /// <summary>A fact the cell learned of a watcher, in the order learned: the record (hashed when there is one).</summary>
    public sealed class SightFact { public string Watcher = "", What = "", How = ""; public int Tick; }

    /// <summary>One step of a watcher's schedule: turn-at-a, turn-at-b, sees-door, door-clear, back-on-sweep, off-routine, at-cafe, leaves-cafe, back-at-door.</summary>
    public sealed class RoutineEvent { public int Tick; public string What = ""; }

    /// <summary>A watcher noticing an operative now (the detection fill): drawn from the watcher toward the operative.</summary>
    public sealed class Noticing
    {
        public string Watcher = "", Op = "";
        public int PerMille;            // 0..1000; today 1000: the rules notice at once (rulebook perception; a gradual notice is Motive's to set)
        public bool Known;              // the cell knows this watcher's sight; false: the HUD shows "?" over the watcher
        public Pt From, To;             // the watcher, the operative
    }

    /// <summary>A quote's "seen by" (rulebook sight.unknown): the answer the card shows, never more than the cell knows.</summary>
    public sealed class SeenQuote
    {
        public string Seen = "no";                              // yes: a known watcher sees it; ?: none known does, an unknown one could; no
        public readonly List<string> SeenBy = new List<string>();      // known watchers that see it now (the true answer)
        public readonly List<string> MaybeSeenBy = new List<string>(); // unknown watchers that could (in their farthest sight, on a clear line): "?"
        public int AlertIfSeen;                                 // Alert if a run is seen, for "yes" and "?" alike: the Alert now since engine 0.34.0 (cell.runAlert retired; attention.run)
    }

    public sealed partial class Game
    {
        public readonly List<SightFact> SightFacts = new List<SightFact>();
        readonly SortedDictionary<string, int> watchedS = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>The kind of a watcher id: camera, guard, patrol, security, rivals; "" for anything else.</summary>
        public static string WatcherKind(string id) =>
            id.StartsWith("camera:", StringComparison.Ordinal) ? "camera" : id == "guard" ? "guard" : id == "patrol" ? "patrol"
            : id.StartsWith("security-", StringComparison.Ordinal) || id.StartsWith("police-", StringComparison.Ordinal) || id.StartsWith("robot-", StringComparison.Ordinal) ? "security" // brief 013: the response's units come for the cell like the response unit
            : IsCrewId(id) ? "rivals" : ""; // brief 059 K8: each member of the crew is a watcher, rival-1..5

        /// <summary>Every watcher of this mission, in a fixed order: the cameras, the guard, the patrol, the response officers, the rivals.</summary>
        public List<string> WatcherIds()
        {
            var l = new List<string>();
            foreach (var c in S.Cameras) l.Add("camera:" + c.Id);
            l.Add("guard"); l.Add("patrol");
            foreach (var g in SecGuns) l.Add(g.Id);
            if (!Range) foreach (var id in CrewIds) l.Add(id); // brief 059: the crew, member by member
            foreach (var u in Units) if (!u.Legacy && !u.Patrol) l.Add(u.Id); // brief 013: robots and police the dial sent, by id (none in a run without an alarm)
            return l;
        }
        bool IsWatcher(string id)
        {
            switch (WatcherKind(id))
            {
                case "camera": { var cid = id.Substring(7); foreach (var c in S.Cameras) if (c.Id == cid) return true; return false; }
                case "security": return SecIndex(id) >= 0 || UnitOf(id) != null;
                case "rivals": return !Range;
                case "": return false;
                default: return true;
            }
        }
        static bool IsPersonRoleName(string id) => id == "waitress" || id == "driver" || id == "guard" || id == "councillor";

        // ---------------------------------------------------------------- the shape a watcher sees with now

        /// <summary>A watcher's perception now: a cone (ahead within reach, side within half × ahead ÷ reach, and anyone within near) or the
        /// patrol's sweep (depth, and side within halfBase + depth × perMille); Live = it is watching (rulebook perception; Game.cs).</summary>
        struct Shape
        {
            public bool Sweep, Live; public Pt At, F; public int Reach, Half, Near, Depth, HalfBase, PerMille;
            public Pt[]? Heads; // a camera with several heads (the mast): its cone along each; F is the first
            public string Key() => $"{(Sweep ? 1 : 0)}|{(Live ? 1 : 0)}|{At.X},{At.Y}|{F.X},{F.Y}|{Reach}|{Half}|{Near}|{Depth}|{HalfBase}|{PerMille}|{Heads?.Length ?? 1}";
        }

        Shape? ShapeOf(string w)
        {
            switch (WatcherKind(w))
            {
                case "camera":
                    {
                        var id = w.Substring(7);
                        foreach (var c in S.Cameras)
                            if (c.Id == id)
                                return new Shape { At = c.At, F = c.Facing, Heads = c.Facings.Length > 1 ? c.Facings : null, Reach = c.Reach, Half = c.Half, Near = 0, Live = !(id == LoopCam && LoopOn) && !CameraDark(id) && !(id == "pickup" && !PickupCameraLive()) };
                        return null;
                    }
                case "guard":
                    {
                        var gp = GuardPose(); if (!gp.HasValue) return null;
                        var (pos, f, phase) = gp.Value; bool door = phase == "door";
                        return new Shape { At = pos, F = f, Reach = door ? R.DoorReachCm : R.FacingReachCm, Half = door ? R.DoorHalfCm : R.FacingHalfCm, Near = door ? 0 : near, Live = !GuardAlly };
                    }
                case "patrol":
                    {
                        if (PatrolDown) return null;
                        var (pos, f) = PatrolPose()!.Value;
                        if (PatrolChasing && PatrolAwayUntil <= Tick) return new Shape { At = pos, F = f, Reach = R.FacingReachCm, Half = R.FacingHalfCm, Near = 0, Live = true }; // brief 031 L2: a machine sees by its cone only
                        return new Shape { Sweep = true, At = pos, F = f, Depth = R.PatrolDepthCm, HalfBase = R.PatrolHalfBase, PerMille = R.PatrolHalfPerMille, Live = PatrolAwayUntil <= Tick };
                    }
                case "security":
                    {
                        if (UnitOf(w) is Unit u) { var up = UnitPose(u); if (!up.HasValue) return null; return new Shape { At = up.Value.pos, F = up.Value.facing, Reach = R.SecReachCm, Half = R.SecHalfCm, Near = u.Robot ? 0 : near, Live = true }; } // brief 031 L2: a robot by its cone only
                        int k = SecIndex(w); if (k < 0) return null;
                        var s = SecOfficerPose(k); if (!s.HasValue) return null;
                        return new Shape { At = s.Value.pos, F = s.Value.facing, Reach = R.SecReachCm, Half = R.SecHalfCm, Near = near, Live = true };
                    }
                case "rivals":
                    {
                        if (!RivalsDrawn && !R.Intent.RivalsStreet) return null; // engine 0.19.0 (Sky's answer 3): the crew watches the street from its arrival (ai.intent.rivals.street)
                        var m = CrewOf(w); if (m == null) return null; // brief 059: a member on foot, by his own cone (none for a rider)
                        var r = MemberPose(m); if (!r.HasValue) return null;
                        return new Shape { At = r.Value.pos, F = r.Value.facing, Reach = R.RivReachCm, Half = R.RivHalfCm, Near = near, Live = true };
                    }
            }
            return null;
        }

        /// <summary>The shape test alone, as the engine's (G.InCone; InSweepCone).</summary>
        bool InShape(Shape s, Pt p)
        {
            if (s.Sweep) return InSweepCone(s.At, s.F, p);
            if (s.Heads != null) { foreach (var f in s.Heads) if (G.InCone(s.At, f, s.Reach, s.Half, s.Near, p)) return true; return false; }
            return G.InCone(s.At, s.F, s.Reach, s.Half, s.Near, p);
        }

        /// <summary>Would this watcher see p from where it stands, ignoring whether it is watching now (a looped camera's sight as it
        /// stands)? The engine's test: its shape, a clear line at eye height, and (crowd) nobody on the street across the line. Rivals'
        /// own sight ignores people, as RivalsWatching does.</summary>
        public bool InSight(string watcher, Pt p, bool crowd = true)
        {
            var s = ShapeOf(watcher); if (!s.HasValue) return false;
            if (!InShape(s.Value, p) || !WatcherSees(watcher, s.Value.At, p)) return false;
            return !(crowd && WatcherKind(watcher) != "rivals" && WatcherCrowd(watcher, s.Value.At, p));
        }
        /// <summary>Brief 055 §7 (perception.mount): the watcher's eye, block z cm, at `at`: a camera's lens (its own piece never blocks it), the
        /// patrol robot's face, a response robot's lamp, a person's eye; null with the mount off (the flat test).</summary>
        (int z, bool own)? WatcherEye(string w, Pt at)
        {
            if (!R.MountOn) return null;
            if (w.StartsWith("camera:", StringComparison.Ordinal)) { var id = w.Substring(7); foreach (var c in S.Cameras) if (c.Id == id) return (LensZ(c), true); }
            if (w == "patrol") return (FloorAt(at) + R.MountPatrolCm, false);
            if (UnitOf(w) is Unit u) return (FloorAt(at) + UnitMount(u), false);
            return (FloorAt(at) + eye, false);
        }
        bool WatcherSees(string w, Pt at, Pt p) { var e = WatcherEye(w, at); return e.HasValue ? SeesFrom(at, e.Value.z, p, e.Value.own) : Sees(at, p); }
        bool WatcherCrowd(string w, Pt at, Pt p) { var e = WatcherEye(w, at); return e.HasValue ? CrowdBlocksZ(at, e.Value.z, p, FloorAt(p) + eye) : CrowdBlocks(at, p); }
        /// <summary>The watcher sees p now (Live and InSight): the same answer as CameraWatching, GuardWatching (unless the guard is the
        /// cell's ally), PatrolWatching, the officers' and RivalsWatching. The detection fill and the "seen by" truth read this.</summary>
        public bool SeesNow(string watcher, Pt p, bool crowd = true)
        {
            var s = ShapeOf(watcher); if (!s.HasValue || !s.Value.Live) return false;
            return InSight(watcher, p, crowd);
        }

        // ---------------------------------------------------------------- the fan

        readonly Dictionary<string, (string key, Fan fan)> fanCache = new Dictionary<string, (string, Fan)>(StringComparer.Ordinal);

        /// <summary>The watcher's sight now as a polygon on the ground (rulebook perception, the engine's own test), or null when the watcher
        /// is not on the street (down, not yet come, an unknown id). crowd: also cut by people on the street (Pulse §4; they move, so the
        /// shape changes every tick); without it the fan is the walls' and solids' alone, and stays put while the watcher does.
        /// Read-only and deterministic (integers only). Cost: rays from the watcher to every occluder corner in reach (three each), where an
        /// occluder edge crosses the shape's edge or another occluder's, the shape's corners and 64 around the near circle, each tested against the occluders'
        /// edges: about 300 rays × 100 edges on the civic block, ~0.1 ms; cached per watcher until its pose or state changes, so a still
        /// watcher (a camera, the guard at his door) costs a dictionary lookup per call. With crowd, the cache holds for the tick only.</summary>
        public Fan? ConeFan(string watcher, bool crowd = false)
        {
            var sh = ShapeOf(watcher); if (!sh.HasValue) return null;
            var s = sh.Value;
            bool useCrowd = crowd && WatcherKind(watcher) != "rivals" && People2 != null && Start.Crowd;
            string key = s.Key() + (useCrowd ? "|crowd@" + Tick.ToString(System.Globalization.CultureInfo.InvariantCulture) : "");
            string ck = watcher + (useCrowd ? "/crowd" : "");
            if (fanCache.TryGetValue(ck, out var hit) && hit.key == key) return hit.fan;
            var fan = BuildFan(watcher, s, useCrowd);
            if (R.MountOn) fan.Shadows = FanShadows(watcher, s, useCrowd);
            if (s.Heads != null) // a mast: one polygon a head; the first is the fan's own, the rest its More (brief 013)
            {
                var more = new List<Pt[]>(); var moreStop = new List<string[]>();
                for (int h = 1; h < s.Heads.Length; h++) { var sh2 = s; sh2.F = s.Heads[h]; sh2.Heads = null; var f2 = BuildFan(watcher, sh2, useCrowd); more.Add(f2.Points); moreStop.Add(f2.Stop); fan.Reach = Math.Max(fan.Reach, f2.Reach); }
                fan.More = more.ToArray(); fan.MoreStop = moreStop.ToArray();
            }
            fanCache[ck] = (key, fan);
            return fan;
        }

        /// <summary>Every watcher on the street whose sight the cell knows (the Alt view), in WatcherIds order.</summary>
        public List<Fan> KnownFans(bool crowd = false)
        {
            var l = new List<Fan>();
            foreach (var w in WatcherIds()) { var k = Known(w); if (k == null || !k.Sight) continue; var f = ConeFan(w, crowd); if (f != null) l.Add(f); }
            return l;
        }

        // ---------------------------------------------------------------- what the cell knows

        /// <summary>What the cell knows of this watcher now, or null for an id that is no watcher of this mission (rulebook sight).</summary>
        public WatcherKnown? Known(string watcher)
        {
            if (!IsWatcher(watcher)) return null;
            var kind = WatcherKind(watcher); var (learn, routine) = R.SightKinds[kind];
            var k = new WatcherKnown { Watcher = watcher, Kind = kind, WatchedS = watchedS.TryGetValue(watcher, out var ws) ? ws : 0, RoutineNeedsS = routine == "watched" ? R.SightRoutineS : 0 };
            if (learn == "arrival")
            {
                var unit = kind == "security" ? UnitOf(watcher) : null;
                bool came = unit != null ? unit.OnStreet(Tick) || unit.St == "gone" : kind == "security" ? Tick >= SecurityArrives : RivalsDrawn && RivalsArrived;
                if (came) { k.Sight = true; k.How = "arrival"; k.SightAt = unit != null ? unit.Launch : kind == "security" ? SecurityArrives : RivalsDrawnTick; k.Routine = routine == "none"; }
                return k;
            }
            foreach (var f in SightFacts)
            {
                if (f.Watcher != watcher) continue;
                if (f.What == "sight" && !k.Sight) { k.Sight = true; k.How = f.How; k.SightAt = f.Tick; }
                if (f.What == "routine" && !k.Routine) { k.Routine = true; k.RoutineAt = f.Tick; }
            }
            if (routine == "fixed" && k.Sight) { k.Routine = true; k.RoutineAt = k.SightAt; }
            if (routine == "person") { k.Routine = GuardP.KRoutine; k.RoutineAt = -1; }
            if (routine == "none") k.Routine = k.Sight;
            return k;
        }

        /// <summary>A fact learned (once each): the record the hash walks.</summary>
        void LearnSight(string watcher, string what, string how)
        {
            foreach (var f in SightFacts) if (f.Watcher == watcher && f.What == what) return;
            SightFacts.Add(new SightFact { Watcher = watcher, What = what, How = how, Tick = Tick });
        }

        /// <summary>Brief 031: each watcher's completed Watches as spans of mission time [from, to) ticks, merged, so that two agents watching at
        /// once count once (the patrol's routine is the union of watched time).</summary>
        readonly SortedDictionary<string, List<(int from, int to)>> watchedSpans = new SortedDictionary<string, List<(int, int)>>(StringComparer.Ordinal);

        /// <summary>A completed Watch of a watcher (rulebook sight learn watch), ending now and lasting `seconds`: its sight, the watched seconds
        /// (the union of every completed Watch's span: brief 031), and the routine once they reach sight.watch.routine (a watched routine).
        /// The guard's Watch is his person event's too: his routine is his person routine.</summary>
        void WatchedWatcher(string watcher, int seconds)
        {
            if (!watchedSpans.TryGetValue(watcher, out var spans)) watchedSpans[watcher] = spans = new List<(int, int)>();
            spans.Add((Tick - T(seconds), Tick));
            spans.Sort((a, b) => a.from != b.from ? a.from.CompareTo(b.from) : a.to.CompareTo(b.to));
            var merged = new List<(int from, int to)>();
            foreach (var sp in spans) { if (merged.Count > 0 && sp.from <= merged[merged.Count - 1].to) { var l = merged[merged.Count - 1]; merged[merged.Count - 1] = (l.from, Math.Max(l.to, sp.to)); } else merged.Add(sp); }
            spans.Clear(); spans.AddRange(merged);
            int ticks = 0; foreach (var sp in merged) ticks += sp.to - sp.from;
            watchedS[watcher] = ticks / Hz;
            LearnSight(watcher, "sight", "watched");
            if (R.SightKinds[WatcherKind(watcher)].routine == "watched" && watchedS[watcher] >= R.SightRoutineS) LearnSight(watcher, "routine", "watched");
        }

        /// <summary>The start state's told knowledge of a watcher (Start.Known: "sight", and "routine" for a watched routine).</summary>
        void ToldWatcher(string watcher, string what)
        {
            if (!IsWatcher(watcher)) throw new FormatException($"start known: '{watcher}' is no watcher (unknown = reject)");
            var (learn, routine) = R.SightKinds[WatcherKind(watcher)];
            if (learn != "watch") throw new FormatException($"start known: {watcher}'s sight is known on arrival (rulebook sight.kinds)");
            if (what == "sight") LearnSight(watcher, "sight", "told");
            else if (what == "routine" && routine == "watched") LearnSight(watcher, "routine", "told");
            else throw new FormatException($"start known: '{what}' of {watcher} (sight, or routine for a watched routine)");
        }

        /// <summary>The knowledge bytes for the checkpoint hash: nothing at all until the cell has learned something, so every run that
        /// never watches a watcher hashes as it always did.</summary>
        internal void HashSight(Hash.H h)
        {
            if (SightFacts.Count == 0) return;
            h.S("sight");
            foreach (var f in SightFacts) h.S(f.Watcher).S(f.What).S(f.How).I(f.Tick);
            foreach (var kv in watchedS) h.S(kv.Key).I(kv.Value);
            foreach (var kv in watchedSpans) if (kv.Value.Count > 1 || WatcherKind(kv.Key) == "patrol") { h.S(kv.Key); foreach (var (f, t) in kv.Value) h.I(f).I(t); } // brief 031: the spans the union is taken over
        }

        // ---------------------------------------------------------------- Watch on a watcher (the order)

        /// <summary>Whether this operative may Watch this watcher (rulebook sight.kinds.*.by; Sky 2026-10-18: the Tech watches the machines).</summary>
        /// <summary>A watcher whose sight is known on arrival (rulebook sight learn arrival): nothing to watch for, whoever asks.</summary>
        bool ArrivesKnown(string target) => IsWatcher(target) && R.SightKinds[WatcherKind(target)].learn == "arrival";
        bool WatchesKind(Op op, string target) => IsWatcher(target) && Array.IndexOf(R.SightBy[WatcherKind(target)], Sim.Op.Name(op.Id)) >= 0;

        /// <summary>A Watch on a camera or the patrol (rulebook sight.watch) by the Scout or the Tech (sight.kinds.*.by): within its reach of the
        /// watcher on a clear line; it takes watch.s and lands as WatchedWatcher, the cell's knowledge whoever watched (the patrol's seconds add
        /// up across both). The Tech doesn't Watch while his loop holds (tethered to the cabinet: run-tethered, as for a run). The guard is
        /// watched as a person (cell.watch) and Land teaches his sight as well.</summary>
        string WatchWatcher(Op op, Order o, bool engage = true)
        {
            var kind = WatcherKind(o.Target);
            if (!IsWatcher(o.Target)) return No("target-unknown", $"'{o.Target}' is no one to watch here");
            if (R.SightKinds[kind].learn != "watch") return No("target-unknown", $"the {kind}'s sight is known when they come: there is nothing to watch for");
            if (!WatchesKind(op, o.Target)) return No("only-scout", $"the {Sim.Op.Name(op.Id)} doesn't watch the {kind} (sight.kinds.{kind}.by)");
            if (op.Id == OpId.Tech && Tethered) return No("run-tethered", "the Tech is tethered to the cabinet while the loop holds");
            // Sky 2026-10-18: a Watch that can teach nothing is refused, free: its sight known, and its routine (a camera's comes with its sight;
            // the patrol's once the cell's Watches of it total sight.watch.routine seconds, or told)
            { var k = Known(o.Target)!; if (k.Sight && k.Routine) return No("already-watched", $"the cell knows the {o.Target.Replace("camera:", "")}{(kind == "camera" ? " camera" : "")}'s sight{(kind == "patrol" ? " and routine" : "")} already: a Watch teaches nothing more"); }
            var sh = ShapeOf(o.Target);
            if (!sh.HasValue) return kind == "patrol" ? No("patrol-down", "the patrol is down") : No("target-unknown", $"the {o.Target} is not on the street");
            var at = sh.Value.At;
            int reach = R.WatchReachCm(op.Id); // brief 031: the reach is the agent's
            if (!G.Within(op.Pos, at, reach)) return No("too-far", $"stand within {reach / 100} m of the {o.Target.Replace("camera:", "")} {(kind == "camera" ? "camera" : "")}".TrimEnd(), m: reach / 100);
            if (!SeesId(op.Pos, o.Target, at) || CrowdId(op.Pos, o.Target, at)) return No("no-clear-line", $"no clear line: move to where the {Sim.Op.Name(op.Id)} can see it");
            if (!engage) return "";
            Engage(op, o, R.SightWatchS);
            op.BusyQuote = null; op.BusyPaid = 0;
            return "";
        }

        /// <summary>An operative's name for the log's words: Scout, Fixer, Tech, Enforcer.</summary>
        static string OpTitle(OpId id) { var n = Sim.Op.Name(id); return char.ToUpperInvariant(n[0]) + n.Substring(1); }

        void LandWatchWatcher(Op op, Order o)
        {
            WatchedWatcher(o.Target, R.SightWatchS);
            var k = Known(o.Target)!;
            Note($"{Sim.Op.Name(op.Id)}.watch.watched", $"The {OpTitle(op.Id)} has watched the {o.Target.Replace("camera:", "") + (k.Kind == "camera" ? " camera" : "")}: its sight is known{(k.Routine ? ", and its routine" : k.RoutineNeedsS > 0 ? $" ({k.WatchedS} of {k.RoutineNeedsS} s toward its routine)" : "")}.");
        }

        // ---------------------------------------------------------------- the routine ahead (stage 2)

        /// <summary>The watcher's schedule from now to now + horizon, as watched (no interference assumed beyond what the cell already did):
        /// null while its routine is unknown. The patrol: where it stands (its first event now: sees-door or door-clear), then each turn at
        /// an end of its sweep and each change in whether it sees the public door; back-on-sweep after a reroute; off-routine while it
        /// chases. The guard: his visit if he is away (at-cafe, leaves-cafe, back-at-door), else nothing (he holds the door). A camera:
        /// nothing (fixed). Cost: one sweep test a tick of the horizon for the patrol (1,000 ticks ≈ 0.1 ms); call it once a second.</summary>
        public List<RoutineEvent>? Routine(string watcher, int horizonTicks)
        {
            var k = Known(watcher); if (k == null || !k.Routine) return null;
            var l = new List<RoutineEvent>(); int end = Tick + Math.Max(0, horizonTicks);
            void add(int t, string what) { if (t >= Tick && t <= end) l.Add(new RoutineEvent { Tick = t, What = what }); }
            switch (k.Kind)
            {
                case "patrol":
                    {
                        if (PatrolDown) return l;
                        if (PatrolChasing) { add(Tick, "off-routine"); return l; }
                        int from = Tick;
                        if (PatrolAwayUntil > Tick) { add(PatrolAwayUntil, "back-on-sweep"); from = PatrolAwayUntil; }
                        int period = T(R.PatrolPeriodS), half = period / 2;
                        bool? was = null;
                        for (int t = from; t <= end; t++)
                        {
                            int ph = (t + PatrolPhase) % period;
                            if (t > from && ph == 0) add(t, "turn-at-a");
                            if (t > from && ph == half) add(t, "turn-at-b");
                            bool sees = SweepSeesEntrance(t);
                            if (was != sees) add(t, sees ? "sees-door" : "door-clear");
                            was = sees;
                        }
                        l.Sort((a, b) => a.Tick != b.Tick ? a.Tick.CompareTo(b.Tick) : 0);
                        return l;
                    }
                case "guard":
                    {
                        if (GuardDown || GuardHeld || LureTick < 0 || Tick >= GuardAwayUntil) return l;
                        if (guardRunning) { add(GuardAwayUntil, "back-at-door"); return l; }
                        add(LureTick + walkT, "at-cafe"); add(LureTick + walkT + stayT, "leaves-cafe"); add(GuardAwayUntil, "back-at-door");
                        return l;
                    }
                default: return l;
            }
        }

        // ---------------------------------------------------------------- detection (the fill)

        /// <summary>Every watcher that notices an operative now, in WatcherIds then operative order: the fill toward the operative, and
        /// whether the cell knows the watcher (else the HUD's "?"). Today a watcher notices at once (rulebook perception): a sighting is
        /// logged on the whole second, a run or a drawn weapon counts the tick it is seen. An operative inside the Office or down is not
        /// noticed. Read-only; ~5 watchers × 4 operatives sight tests a call.</summary>
        public List<Noticing> Detection()
        {
            var l = new List<Noticing>();
            foreach (var w in WatcherIds())
            {
                var sh = ShapeOf(w); if (!sh.HasValue || !sh.Value.Live) continue;
                bool known = Known(w)!.Sight;
                foreach (var op in Ops)
                {
                    if (op.Inside || op.Down) continue;
                    if (SeesNow(w, op.Pos)) l.Add(new Noticing { Watcher = w, Op = Sim.Op.Name(op.Id), PerMille = 1000, Known = known, From = sh.Value.At, To = op.Pos });
                }
            }
            return l;
        }
        /// <summary>One watcher's detection of one operative, per mille (0..1000).</summary>
        public int DetectionOf(string watcher, string op)
        {
            var o = OpOf(Sim.Op.Parse(op)); if (o.Inside || o.Down) return 0;
            return SeesNow(watcher, o.Pos) ? 1000 : 0;
        }

        // ---------------------------------------------------------------- "seen by" on a card

        /// <summary>The "seen by" a card shows for an operative at these points now (rulebook sight.unknown): each watcher whose sight the
        /// cell knows gives its true answer (SeesNow, people on the street included); one it doesn't know shows "?" if it could see any point
        /// whatever its facing (within its farthest sight, on a clear line past the walls), and nothing if it couldn't. A watcher not
        /// watching now (looped, rerouted) shows nothing: the cell did that. The outcome never reads this; only the card does.</summary>
        public SeenQuote SeenAt(IReadOnlyList<Pt> points)
        {
            var q = new SeenQuote { AlertIfSeen = Alert }; // engine 0.34.0 (brief 067, Motive M067-B2): a run seen raises no Alert by itself (attention.run notices it)
            foreach (var w in WatcherIds())
            {
                var sh = ShapeOf(w); if (!sh.HasValue || !sh.Value.Live) continue;
                var k = Known(w)!;
                if (k.Sight) { foreach (var p in points) if (SeesNow(w, p)) { q.SeenBy.Add(w); break; } continue; }
                long reach = FarthestSight(sh.Value);
                foreach (var p in points) if (G.Within(sh.Value.At, p, (int)reach) && WatcherSees(w, sh.Value.At, p)) { q.MaybeSeenBy.Add(w); break; }
            }
            q.Seen = q.SeenBy.Count > 0 ? "yes" : q.MaybeSeenBy.Count > 0 ? "?" : "no";
            if (q.Seen == "no") q.AlertIfSeen = Alert;
            return q;
        }
        public SeenQuote SeenAt(Pt at) => SeenAt(new[] { at });

        /// <summary>The farthest a shape sees, any facing: its far corner, or its near circle.</summary>
        static long FarthestSight(Shape s) => s.Sweep
            ? G.Isqrt((long)s.Depth * s.Depth + (long)(s.HalfBase + (long)s.Depth * s.PerMille / 1000) * (s.HalfBase + (long)s.Depth * s.PerMille / 1000))
            : Math.Max(s.Near, G.Isqrt((long)s.Reach * s.Reach + (long)s.Half * s.Half));
    }
}
