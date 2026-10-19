// What failed the run, as data for the fail camera (Sky 2026-10-18: "if you fail mission, the camera should pan around the item / person
// that failed you so you know what it was for next time"). Read-only: built at the tick the run ends from state the engine already holds,
// never hashed, never read by a rule, so every replay and checkpoint is the one it was. One cause per ending (a failure, or a withdrawal).
//  · culprits: what decided it, the first is the one to orbit: a camera, the guard, the patrol, an officer or robot, the rivals, a person,
//    Vale, the car, or an operative (who fired, who killed). Where it stood and faced at the deciding tick, in the block frame (cm).
//  · ops: the cell's operatives involved (who was seen, who was downed, who fired).
//  · single: one culprit decided it. When false (the seal clock, an engine error, a withdrawal) `origin` names what started it, if
//    anything did: the report that raised the response, its reporter where they stood when it landed, and the operatives it names.
// Ids use the engine's own grammar so a host can ask the engine about them again: "camera:<id>" (Game.ConeFan, Known), "guard", "patrol",
// "security-<n>", "robot-<n>", "police-<n>", "rivals", "rival-<n>", "officer-<n>", a person's role (cast) or id (crowd), an operative
// ("enforcer"), "car" (the official car), "getaway-car", "desk" (the check-in desk: a missed mark), "heard" (shots heard, nobody saw).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>One thing the fail camera can look at: what it is, what it did, where it stood and faced at the tick it is read.</summary>
    public sealed class Blamed
    {
        public string Id = "";    // the engine's id (see the file header)
        public string Kind = "";  // camera, guard, patrol, officer, robot, rival, person, vale, op, car, desk, heard
        public string Role = "";  // saw, shot, reached, killed, left, reported, victim
        public bool Placed;       // false: off the street (inside, gone), no position to orbit
        public Pt At;             // cm, block frame; level 0 (every Handover watcher and body stands on the street)
        public bool Faces;        // a facing is known (cameras, the guard, the patrol, officers, robots, the rivals, a shooter, a walking operative)
        public Pt Facing;         // a direction (any length)

        public void Write(JWriter w)
        {
            w.Obj().Prop("id", Id).Prop("kind", Kind).Prop("role", Role);
            if (Placed) w.Prop("x", At.X).Prop("y", At.Y).Prop("level", 0);
            if (Placed && Faces) w.Prop("fx", Facing.X).Prop("fy", Facing.Y);
            w.EndObj();
        }
        public override string ToString() => $"{Kind}:{Id}/{Role}" + (Placed ? "@" + At : "") + (Faces ? ">" + Facing : "");
    }

    /// <summary>What started a chain that has no single culprit: the report that raised the response (or made the final fight loud).</summary>
    public sealed class CauseOrigin
    {
        public int Tick;                 // when the report landed (or the shot was fired)
        public string Report = "";       // what was reported: gunfire:<target>, drawn-seen, attack, post-empty, report:<kind>:<victim>, ...
        public string Because = "";      // what the seal clock began from: response (a report called the response), confirmed (a unit confirmed a reported act), loud-fight
        public Blamed? Reporter;         // who reported it, where they stood when it landed (a camera, the guard, a witness, the desk, "heard")
        public Blamed? Confirmer;        // because "confirmed": the unit or watcher that confirmed it, where it stood then
        public bool ScenePlaced; public Pt Scene; // where it happened
        public readonly List<string> Ops = new List<string>();

        public void Write(JWriter w)
        {
            w.Obj().Prop("tick", Tick).Prop("clock", Sim.Report.Clock(Tick)).Prop("report", Report).Prop("because", Because);
            if (Reporter != null) { w.Key("reporter"); Reporter.Write(w); }
            if (Confirmer != null) { w.Key("confirmer"); Confirmer.Write(w); }
            if (ScenePlaced) w.Open("scene").Prop("x", Scene.X).Prop("y", Scene.Y).Prop("level", 0).EndObj();
            w.OpenArr("ops"); foreach (var o in Ops) w.Str(o); w.EndArr();
            w.EndObj();
        }
    }

    /// <summary>The cause of an ending: the fail code (or "withdraw"), the deciding tick, why, the culprits, the operatives, the origin.</summary>
    public sealed class FailCause
    {
        public string Fail = "";         // Game.FailCode, or "withdraw" for a withdrawal
        public string Why = "";          // stable machine reason: seen, beat-to-pickup, reached-car, downed-enforcer, reached-at-close, too-late, shot, killed, sealed, error, withdrew, car-left-without-vale
        public int Tick;                 // the deciding moment (the run ends at it)
        public bool Single;              // one culprit decided it; false: see Origin
        public readonly List<Blamed> Culprits = new List<Blamed>();
        public readonly List<string> Ops = new List<string>();
        public CauseOrigin? Origin;
        /// <summary>The seal only: a later report that moved the seal clock earlier (the one whose time ran out), when it is not the origin.</summary>
        public CauseOrigin? LastMove;

        public void Write(JWriter w)
        {
            w.Obj().Prop("fail", Fail).Prop("why", Why).Prop("tick", Tick).Prop("clock", Sim.Report.Clock(Tick)).Prop("single", Single);
            w.OpenArr("culprits"); foreach (var c in Culprits) c.Write(w); w.EndArr();
            w.OpenArr("ops"); foreach (var o in Ops) w.Str(o); w.EndArr();
            if (Origin != null) { w.Key("origin"); Origin.Write(w); }
            if (LastMove != null) { w.Key("lastMove"); LastMove.Write(w); }
            w.EndObj();
        }
        public string Json() { var w = new JWriter(); Write(w); return w.ToString(); }
    }

    public sealed partial class Game
    {
        /// <summary>Why the run ended (a failure or a withdrawal): set once, at the tick it ends; null while it runs and on success. Output only.</summary>
        public FailCause? Cause;

        // what the seal clock (and a loud final fight) began from: kept as they happen, read only when the run fails. Never hashed.
        CauseOrigin? sealOrigin, sealLast, alertTwoOrigin, gunfireOrigin, pendingSealOrigin, shotNow;
        readonly Dictionary<int, CauseOrigin> actOrigin = new Dictionary<int, CauseOrigin>();
        string landingBy = ""; Pt landingAt; // the report landing now (LandAct), for an Alert it raises

        /// <summary>The cause of the failure now; a builder that throws leaves a cause with why "unresolved" (it can never change the run).</summary>
        FailCause MakeCause(string code, Func<FailCause>? build)
        {
            FailCause c;
            try { c = build?.Invoke() ?? new FailCause { Why = "unresolved" }; }
            catch (Exception e) when (!(e is OutOfMemoryException)) { c = new FailCause { Why = "unresolved" }; }
            c.Fail = code; c.Tick = Tick;
            return c;
        }

        static FailCause Because(string why, bool single, params Blamed?[] culprits)
        {
            var c = new FailCause { Why = why, Single = single };
            foreach (var b in culprits) if (b != null) c.Culprits.Add(b);
            return c;
        }
        static FailCause With(FailCause c, params string[] ops) { foreach (var o in ops) if (o.Length > 0 && !c.Ops.Contains(o)) c.Ops.Add(o); return c; }

        /// <summary>Where a thing stands and faces now, by the engine's id. look: the point it acted on (a mast's head that faces it; a shooter
        /// or an operative standing still faces it). fallback: where it is when the engine keeps no position (the desk's post, a phone).</summary>
        public Blamed Locate(string id, string role, Pt? look = null, Pt? fallback = null)
        {
            var b = new Blamed { Id = id, Role = role };
            void at(Pt? p) { if (p.HasValue) { b.Placed = true; b.At = p.Value; } else if (fallback.HasValue) { b.Placed = true; b.At = fallback.Value; } }
            void face(Pt? f) { if (f.HasValue && (f.Value.X != 0 || f.Value.Y != 0)) { b.Faces = true; b.Facing = f.Value; } }
            void toward() { if (b.Placed && look.HasValue) face(look.Value - b.At); }
            if (id.StartsWith("camera:", StringComparison.Ordinal))
            {
                b.Kind = "camera"; var cid = id.Substring(7);
                foreach (var c in S.Cameras)
                    if (c.Id == cid)
                    {
                        at(c.At); var f = c.Facing;
                        if (look.HasValue) foreach (var h in c.Facings) if (G.InCone(c.At, h, c.Reach, c.Half, 0, look.Value)) { f = h; break; } // the head that saw it
                        face(f);
                    }
                return b;
            }
            if (IsOpId(id)) { b.Kind = "op"; var op = OpOf(Sim.Op.Parse(id)); at(op.Pos); if (op.Walk != null) face(op.Walk.Heading(op.WalkMm)); else toward(); return b; }
            switch (id)
            {
                case "desk": b.Kind = "desk"; at(null); return b;
                case "heard": b.Kind = "heard"; at(null); return b;
                case "car": b.Kind = "car"; at(CarPos()); return b;
                case "getaway-car": b.Kind = "car"; at(S.At(S.GetawayCar)); return b;
                case "guard":
                    {
                        b.Kind = "guard"; var gp = GuardPose();
                        if (gp.HasValue) { at(gp.Value.pos); face(gp.Value.facing); }
                        else { at(BodyOf(GuardP) is Body gb ? BodyPos(gb) : (Pt?)null); }
                        return b;
                    }
                case "patrol": { b.Kind = "patrol"; var pp = PatrolPose(); if (pp.HasValue) { at(pp.Value.pos); face(pp.Value.facing); } else at(null); return b; }
                case "rivals": { b.Kind = "rival"; var rp = RivalsPose(); if (rp.HasValue) { at(rp.Value.pos); face(rp.Value.facing); } else at(null); return b; }
            }
            int k = SecIndex(id);
            if (k >= 0) { b.Kind = "officer"; var sp = SecOfficerPose(k); if (sp.HasValue) { at(sp.Value.pos); face(sp.Value.facing); } else at(null); return b; }
            foreach (var u in Units)
                if (u.Id == id && !u.Legacy && !u.Patrol)
                {
                    b.Kind = u.Robot ? "robot" : "officer"; var up = UnitPose(u);
                    if (up.HasValue) { at(up.Value.pos); face(up.Value.facing); } else at(null);
                    return b;
                }
            if (HostileOf(id) is Hostile ho)
            {
                b.Kind = IsRival(id) ? "rival" : "officer"; at(ho.Pos(Tick));
                var tp = ho.Gun.Target.Length > 0 ? PosOf(ho.Gun.Target) : null;
                if (tp.HasValue) face(tp.Value - b.At);
                else if (ho.Run != null && ho.Moving(Tick)) face(ho.Run.Heading((long)Math.Max(0, Tick - ho.RunStart) * ho.Speed));
                else toward();
                return b;
            }
            if (PersonById(id) is Person p)
            {
                b.Kind = p == ValeP ? "vale" : "person";
                Pt? pos = p == ValeP ? ValePos() : PosOfPerson(p);
                if (!pos.HasValue && BodyOf(p) is Body pb) pos = BodyPos(pb);
                at(pos); toward();
                return b;
            }
            b.Kind = "unknown"; at(null);
            return b;
        }
        /// <summary>A shooter at the moment of its shot: where it fired from, facing its target.</summary>
        Blamed Shooter(string by, Pt from, Pt at, string role = "shot")
        {
            var b = Locate(by, role); b.Placed = true; b.At = from;
            if (at != from) { b.Faces = true; b.Facing = at - from; }
            return b;
        }
        /// <summary>The id if it is an operative's, else "".</summary>
        static string OpIn(string id) => IsOpId(id) ? id : "";

        /// <summary>An origin now: what was reported, by whom (where they stand now, or `fallback`), where, and the operatives it names.</summary>
        CauseOrigin OriginNow(string report, string because, string reporter, Pt? scene, Pt? fallback, Pt? look, params string[] ops)
        {
            var o = new CauseOrigin { Tick = Tick, Report = report, Because = because, Reporter = reporter.Length > 0 ? Locate(reporter, "reported", look ?? scene, fallback) : null };
            if (scene.HasValue) { o.ScenePlaced = true; o.Scene = scene.Value; }
            foreach (var x in ops) if (x.Length > 0 && !o.Ops.Contains(x)) o.Ops.Add(x);
            return o;
        }
        static CauseOrigin Recast(CauseOrigin o, string because)
        {
            var n = new CauseOrigin { Tick = o.Tick, Report = o.Report, Because = because, Reporter = o.Reporter, Confirmer = o.Confirmer, ScenePlaced = o.ScenePlaced, Scene = o.Scene };
            n.Ops.AddRange(o.Ops); return n;
        }

        /// <summary>A reported act confirmed at Alert 2 or more: its first landed report, and who confirmed it now.</summary>
        CauseOrigin Confirmed(TdAct a, string by)
        {
            var o = actOrigin.TryGetValue(a.Id, out var first) ? Recast(first, "confirmed") : OriginNow($"report:{a.How}:{a.Victim}", "confirmed", "", a.Scene, null, null, "enforcer");
            o.Confirmer = Locate(by == "security" ? "security-1" : by, "confirmed", a.Scene);
            return o;
        }

        /// <summary>The seal clock moved: the first report that set it is the origin (what started the chain); a later one that moved it is
        /// the last move (whose time ran out). Read when the district seals.</summary>
        void SealOrigin(int before, Func<CauseOrigin?> origin)
        {
            if (Lockdown == before) return;
            try
            {
                var o = origin(); if (o == null) return;
                if (before >= INF || sealOrigin == null) { sealOrigin = o; sealLast = null; } else sealLast = o;
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { }
        }
        FailCause Sealed()
        {
            var c = Because("sealed", false); c.Origin = sealOrigin; c.LastMove = sealLast;
            if (sealOrigin != null) With(c, sealOrigin.Ops.ToArray());
            if (sealLast != null) With(c, sealLast.Ops.ToArray());
            return c;
        }

        /// <summary>Alert reached 2 (search): who raised it, read from the ledger's cause and the report landing now. For a loud final fight.</summary>
        void AlertTwo(string cause)
        {
            try
            {
                var parts = cause.Split(':'); string first = parts[0], last = parts[parts.Length - 1], who = "", op = "";
                switch (first)
                {
                    case "report": if (parts.Length >= 3 && landingBy.Length > 0) { alertTwoOrigin = OriginNow(cause, "alert", landingBy, landingAt, landingAt, null, "enforcer"); return; } who = last; break; // a landed act's reporter; else the person who reported the approach
                    case "drawn-seen": case "runner-seen": op = last; who = parts.Length > 2 ? string.Join(":", parts, 1, parts.Length - 2) : ""; if (who == "people") who = "heard"; break;
                    case "violence-seen": who = last; break;
                    case "takedown-fails": who = last; op = "enforcer"; break;
                    case "gunfire": if (shotNow != null) { alertTwoOrigin = Recast(shotNow, "alert"); return; } who = last; break; // the cell's shot (CellGunfire), or a city gunman's (Resolve: gunfire:<shooter>)
                    case "sighting": who = last; break;
                    case "confirmed": who = landingBy; break;
                }
                if (who == "security") who = "security-1";
                alertTwoOrigin = OriginNow(cause, "alert", who, null, null, null, op);
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { alertTwoOrigin = new CauseOrigin { Tick = Tick, Report = cause, Because = "alert" }; }
        }

        /// <summary>Brief 018's rival at the car: every rival within cell.atReach of it (nearest first, ties on id), then Vale in the car.</summary>
        FailCause RivalsAtCar()
        {
            var car = S.At(S.GetawayCar); var near = new List<(long d, string id)>();
            foreach (var h in Hostiles) if (IsRival(h.Id) && h.Present && Up(h) && G.Within(h.Pos(Tick), car, R.AtReachCm)) near.Add((G.D2(h.Pos(Tick), car), h.Id));
            near.Sort((x, y) => x.d != y.d ? x.d.CompareTo(y.d) : string.CompareOrdinal(x.id, y.id));
            var c = Because("reached-car", true);
            foreach (var (_, id) in near) c.Culprits.Add(Locate(id, "reached", car));
            c.Culprits.Add(Locate("councillor", "victim", null, car));
            return c;
        }
        /// <summary>A shot that ended the run (the shot being resolved): the shooter where it fired from, facing its target, then the target.</summary>
        FailCause ShotDown(string why, string target)
        {
            var c = Because(why, true);
            if (resolving != null) { c.Culprits.Add(Shooter(resolving.By, resolving.From, resolving.At)); With(c, OpIn(resolving.By)); }
            if (target.Length > 0) c.Culprits.Add(Locate(target, "victim", null, resolving?.At));
            With(c, OpIn(target));
            return c;
        }

        /// <summary>The cause of a withdrawal: the cell pulled out, or the car left without Vale.</summary>
        void Withdrew(bool carLeft)
        {
            Cause = MakeCause("withdraw", () => carLeft
                ? Because("car-left-without-vale", false, Locate("getaway-car", "left"))
                : Because("withdrew", false));
        }
    }
}
