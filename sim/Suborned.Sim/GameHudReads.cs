// Read-only accessors for the takedown HUD (Glyph, briefs/013/glyph-takedown-hud.md, ask 1; Aster 2026-10-16). Each reads the state the
// engine already holds and changes nothing: no state, no hash moves, no order. The host calls them at will (hover, every frame).
//  · HeldUntil(door): when a held door's hold ends (the units' sweep, then the stand-down), Game.Never at Alert 3, -1 if not held.
//  · StashRoom(stash): the bodies in a stash and how many it holds (the pips).
//  · NextRound(stash): a container's next servicing round, and whether the cell knows it ("?" until it does).
//  · CarryPathSight(op, to): who sees which stretch of a carry walk, by SeenQuote's watcher test, second by second.
//  · NeedRecordOf(person): a person's need as kind, record and label (brief 017 phase 1, for Intel). The truth, not what the cell knows:
//    the host shows the kind's word while the need is only told and the label once learned, by Person.Knows("need") as before.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>One stretch of a carry walk that the same watchers see (CarryPathSight): seconds from the start, where it begins and ends,
    /// and SeenQuote's answer for it.</summary>
    public sealed class CarryStretch
    {
        public int FromS, ToS; public Pt From, To;
        public string Seen = "no";                                     // yes, ?, no (SeenQuote.Seen)
        public readonly List<string> SeenBy = new List<string>();      // known watchers that see it (the true answer)
        public readonly List<string> MaybeSeenBy = new List<string>(); // unknown watchers that could: "?"
    }

    /// <summary>The sight along a carry walk (Glyph §4: "seen on the way: who, n s").</summary>
    public sealed class CarrySight
    {
        public string Refused = "";                                    // why there is no walk ("" when there is one)
        public int S; public int PaceCmS;                              // the whole walk, seconds (rounded up), at the carrier's pace (cm/s)
        public string Seen = "no";                                     // yes if a known watcher sees any of it; ? if only unknown ones could; no
        public readonly List<CarryStretch> Stretches = new List<CarryStretch>();
        public readonly SortedDictionary<string, int> SeenS = new SortedDictionary<string, int>(StringComparer.Ordinal);  // known watcher → seconds seen
        public readonly SortedDictionary<string, int> MaybeS = new SortedDictionary<string, int>(StringComparer.Ordinal); // unknown watcher → seconds it could see
    }

    public sealed partial class Game
    {
        /// <summary>The tick a hold that never ends reports (Alert 3: the units never stand down).</summary>
        public const int Never = INF;

        /// <summary>When the hold on this entrance ends (Glyph §6, the ring to the stand-down): -1 if no unit holds it now; Never at Alert 3;
        /// else the later of the units' sweep at the scene (a check, then its search and the stashes it still has to open) and the stand-down
        /// (response.standDown after the last alarm). A later alarm moves it: this is the answer now.</summary>
        public int HeldUntil(string doorId)
        {
            if (!DoorHeld(doorId)) return -1;
            if (Alert >= 3) return Never;
            int end = lastAlarm < 0 ? Tick : lastAlarm + T(Alert >= 2 ? R.StandDownSearchS : R.StandDownInquiryS);
            if (!SceneHolds.Exists(h => h.Doors.Contains(doorId) && HoldActive(h))) return FixtureHeldUntil(doorId); // test only: held by a start fixture alone (N85)
            foreach (var h in SceneHolds)
            {
                if (!h.Doors.Contains(doorId) || !HoldActive(h)) continue;
                foreach (var u in Units)
                {
                    if (u.Robot || u.Act != h.Act || !u.Scene || u.Gun.Down) continue;
                    int sweep = u.St == "check" ? u.Until + T(R.SearchS) + u.ToOpen.Count * T(R.SearchStashS)
                        : u.St == "search" ? Math.Max(u.Until, u.ToOpen.Count == 0 ? u.Until : u.NextOpen + (u.ToOpen.Count - 1) * T(R.SearchStashS))
                        : -1;
                    end = Math.Max(end, sweep);
                }
            }
            return Math.Max(end, Tick);
        }

        /// <summary>A stash's room (Glyph §5, the pips): the bodies in it now and how many it holds (HoldsOf); (-1, -1) for an unknown id.</summary>
        public (int bodies, int holds) StashRoom(string stashId)
        {
            var s = StashById(stashId); if (s == null) return (-1, -1);
            return (InStash(stashId), HoldsOf(s));
        }

        /// <summary>A container's next servicing round (Pulse §4.1; takedown.service): the first tick from now on which the round opens it
        /// (its phase from the container's id, every service.period), and whether the cell knows it. -1 for a door or an unknown id (a door
        /// has no round). Known once a Scout's Watch on the container has completed this mission (GameStashWatch.cs); until then the HUD
        /// shows "?". Read-only.</summary>
        public (int tick, bool known) NextRound(string stashId)
        {
            var s = StashById(stashId); if (s == null || s.Kind != "container") return (-1, false);
            int period = T(R.ServicePeriodS), phase = (int)(Streams.Fnv1a32(s.Id) % (uint)period);
            int next = Tick + ((phase - Tick % period) % period + period) % period;
            return (next, KnowsRound(s.Id));
        }

        /// <summary>Who sees a carry walk from the carrier to `to` (Glyph §4): the grid route at the carrier's carrying pace (the Enforcer
        /// walks, the others takedown.carry.untrained), sampled once a second of the walk, each sample by SeenAt (SeenQuote's watcher test:
        /// known watchers give the true answer, unknown ones "?"), merged into stretches with the same answer. The watchers as they stand
        /// now: a camera's sweep or the patrol's later place is not forecast. Read-only.</summary>
        public CarrySight CarryPathSight(OpId carrier, Pt to)
        {
            var c = new CarrySight(); var op = OpOf(carrier);
            if (op.Down) { c.Refused = "agent-down"; return c; }
            if (op.Inside) { c.Refused = "enforcer-inside"; return c; }
            var path = RouteOf(op.Pos, to);
            int pace = carrier == OpId.Enforcer ? opSpeed : R.CarryUntrainedSpeed; // mm a tick (= cm/s)
            c.PaceCmS = pace;
            int ticks = (int)((path.Length + pace - 1) / pace); c.S = (ticks + Hz - 1) / Hz;
            CarryStretch? cur = null;
            for (int sec = 0; sec <= c.S; sec++)
            {
                var at = path.At(Math.Min(path.Length, (long)sec * Hz * pace));
                var q = SeenAt(at);
                string key = q.Seen + "|" + string.Join(",", q.SeenBy) + "|" + string.Join(",", q.MaybeSeenBy);
                string curKey = cur == null ? "" : cur.Seen + "|" + string.Join(",", cur.SeenBy) + "|" + string.Join(",", cur.MaybeSeenBy);
                if (cur == null || key != curKey)
                {
                    cur = new CarryStretch { FromS = sec, ToS = sec, From = at, To = at, Seen = q.Seen };
                    cur.SeenBy.AddRange(q.SeenBy); cur.MaybeSeenBy.AddRange(q.MaybeSeenBy);
                    c.Stretches.Add(cur);
                }
                else { cur.ToS = sec; cur.To = at; }
                if (sec == c.S) break; // the last sample is the end of the walk: no second after it
                foreach (var w in q.SeenBy) c.SeenS[w] = (c.SeenS.TryGetValue(w, out var n) ? n : 0) + 1;
                foreach (var w in q.MaybeSeenBy) c.MaybeS[w] = (c.MaybeS.TryGetValue(w, out var n) ? n : 0) + 1;
            }
            c.Seen = c.SeenS.Count > 0 || c.Stretches.Exists(x => x.Seen == "yes") ? "yes" : c.MaybeS.Count > 0 || c.Stretches.Exists(x => x.Seen == "?") ? "?" : "no";
            return c;
        }

        /// <summary>A person's need (brief 017): its kind (Person.Need, what every rule reads), the catalogue record's id and its label
        /// ("Bureau loan"), by id or role, cast or crowd. Null if there is no such person; record and label "" when the host loaded no people
        /// file or the catalogue has no record of the kind. Read-only: the truth, gated by the host on Knows("need").</summary>
        public (string kind, string record, string label)? NeedRecordOf(string idOrRole) => PersonById(idOrRole) is Person p ? NeedRecordOf(p) : ((string, string, string)?)null;
        /// <summary>The same for a person the engine handed out (a contact the debrief carries: Between.Contacts, whose need may be replaced).</summary>
        public (string kind, string record, string label) NeedRecordOf(Person p)
            => (p.Need, p.Record, p.Record.Length > 0 && People2 != null && People2.Records.TryGetValue(p.Record, out var r) ? r.label : "");

        /// <summary>The cell's open windows now (brief 043 §6, the window marker; engine 0.24.0), in a fixed order, so the host reads one
        /// list instead of five fields. Read-only: no state, no hash moves. Each is (Kind, Thing, Until): Kind the verb that opened it, Thing
        /// what it changed (the chip's anchor), Until the tick it closes, as the engine has it now (a later event may move it), or -1 when it
        /// is held with no end. The host's number is (Until − Tick) / Hz.
        ///  · call, guard: Call Inez, the guard away from the door until GuardAwayUntil (closes on guard.back); not while he is held or down.
        ///  · call, pickup: Call driver, the car blinds the pickup camera from car.blocks until BlockedUntil (car.clears), or until driverLast
        ///    if that comes first and the turned driver will take the car to the service lane (car.leaves).
        ///  · reroute, patrol: the patrol away on false orders until PatrolAwayUntil (patrol.returns).
        ///  · mislead, rivals: the False tip, the rival crew away until RivalsDelayedUntil (no log code at its end; a retreat under fire is
        ///    not a window).
        ///  · loop, the camera id (LoopCam): held while the Tech stays at the cabinet, Until -1 (loop.ends).</summary>
        public List<OpenWindow> Windows()
        {
            var l = new List<OpenWindow>();
            if (LureTick >= 0 && !GuardDown && !GuardHeld && Tick < GuardAwayUntil) l.Add(new OpenWindow("call", "guard", GuardAwayUntil));
            if (Tick >= BlockedFrom && !PickupCameraLive())
            {
                int until = BlockedUntil, last = T(driverLastS);
                if (Tick < last && last < until && Driver.Turned && !Driver.Down) until = last; // he drives it away at driverLast (CarTimeline)
                l.Add(new OpenWindow("call", "pickup", until));
            }
            if (!PatrolDown && PatrolAwayUntil > Tick) l.Add(new OpenWindow("reroute", "patrol", PatrolAwayUntil));
            if (RivalsDelayMethod == "mislead" && RivalsDelayedUntil > Tick && !RivalsAbsorbed) l.Add(new OpenWindow("mislead", "rivals", RivalsDelayedUntil));
            if (LoopOn) l.Add(new OpenWindow("loop", LoopCam, -1));
            return l;
        }
    }

    /// <summary>One open window (Game.Windows): the verb that opened it, the thing it changed, the tick it closes or -1 while held.</summary>
    public sealed class OpenWindow
    {
        public readonly string Kind, Thing; public readonly int Until;
        public OpenWindow(string kind, string thing, int until) { Kind = kind; Thing = thing; Until = until; }
        public override string ToString() => Kind + ":" + Thing + "@" + Until.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
