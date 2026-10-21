using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The end steps (Sky, playing, 2026-10-18: "it's really unclear on how to take Vale. When is that an option?"; Aster's queue, Fable's
    /// words H14 to H16 and the objective's steps in text.json). Read only: nothing here changes the run.
    /// The door is read from what the cell has learned, never the engine's live check (Glyph's ruling): DoorState go, unk or seen. H14 and the
    /// door marker both read it, so they always agree. While the Enforcer has no Enter order the
    /// door gets a marker (a dashed ring with the Enter glyph); while he is inside it shows his progress; once he is out with Vale the getaway car
    /// gets the marker with the car glyph and his first knob becomes "Vale into the car". A click on a marker with the Enforcer selected gives the
    /// order as his first knob and a right click would (walk, then the act; priced on hover as the target itself); without him selected, it
    /// selects him.</summary>
    public sealed partial class HandoverHost
    {
        int doorTick = -1; string doorState = ""; string doorEye = "";

        /// <summary>The door as the cell knows it (Glyph's ruling, Aster 2026-10-18: told versus learned): "go" when every watcher that can see
        /// the door is learned and, by what is learned, does not see it; "unk" when one of them is unlearned (a fact only told counts as
        /// unlearned); "seen" when a learned one sees it (DoorEye names whose eye: guard, camera, patrol); "" when Enter is not open at all
        /// (Vale not inside, the door held or locked). The engine still decides when the order runs: acting on "unk" can fail.</summary>
        public string DoorState { get { DoorRead(); return doorState; } }
        public string DoorEye { get { DoorRead(); return doorEye; } }

        void DoorRead()
        {
            var g = Game; if (g == null) { doorState = ""; return; }
            if (doorTick == g.Tick) return; doorTick = g.Tick; doorState = ""; doorEye = "";
            Guard("door state", () =>
            {
                if (g.Vale != ValeAt.Office || g.EntryInProgress) return;
                if (g.DoorHeld(g.EnterDoor) || (g.Alert >= 3 && g.DialNow.DoorsLock)) return;
                var door = g.S.At(g.S.GuardPost); bool unk = false;
                bool Learned(string w, bool routine) => g.Known(w) is { } k && k.Sight && k.How != "told" && (!routine || k.Routine);
                // the guard at his door is in plain sight: his eye
                if (g.GuardAtDoor) { doorState = "seen"; doorEye = "guard"; return; }
                // the entrance camera: the cell's own loop blinds it; else only its learned sight says
                if (!(g.LoopOn && g.LoopCam == "entrance"))
                {
                    if (!Learned("camera:entrance", false)) unk = true;
                    else if (g.CameraWatching("entrance", door)) { doorState = "seen"; doorEye = "camera"; return; }
                }
                // the patrol: down, or away on the cell's own reroute; else its learned sight and routine say
                if (!g.PatrolDown && g.PatrolAwayUntil <= g.Tick)
                {
                    if (!Learned("patrol", true)) unk = true;
                    else if (g.PatrolWatching(door)) { doorState = "seen"; doorEye = "patrol"; return; }
                }
                doorState = unk ? "unk" : "go";
            });
        }

        /// <summary>The Enforcer has been given Enter: walking to the door for it, at it, or inside.</summary>
        public bool EnterGiven { get { var op = Game?.Ops[(int)OpId.Enforcer]; return op != null && (op.Inside || op.Busy?.Do == "enter" || pending[(int)OpId.Enforcer]?.Do == "enter"); } }

        /// <summary>The door marker shows (any state) for an Enforcer who is outside, standing and has not been given Enter.</summary>
        public bool DoorMarked { get { var op = Game?.Ops[(int)OpId.Enforcer]; return op != null && !op.Inside && !op.Down && op.CarryBody.Length == 0 && !EnterGiven && DoorState.Length > 0; } }
        /// <summary>The door is learned open: the marker says go, the knob is ringed, H14 shows.</summary>
        public bool EnterAvailable => DoorMarked && DoorState == "go";

        /// <summary>Inside: Enter's progress per mille (the door marker's ring), else -1.</summary>
        public int EnterProgress { get { var op = Game?.Ops[(int)OpId.Enforcer]; return op != null && op.Inside ? doing[(int)OpId.Enforcer].Progress : -1; } }

        /// <summary>Out with Vale and no Board given: the car marker calls the Enforcer (brief 018; HostGetaway.BoardAvailable). Kept by name for the HUD.</summary>
        public bool ExtractAvailable => BoardAvailable;

        /// <summary>The objective's step (Fable's less.objective.steps): take, then bring once Enter is given, then carry once Vale is with him.</summary>
        public string ObjectiveKey => Game == null ? "take" : Game.Vale == ValeAt.Car ? "go" : Game.Vale == ValeAt.WithEnforcer ? "carry" : Game.Vale == ValeAt.Office && EnterGiven ? "bring" : "take";

        /// <summary>Intel's next-step chip (HudToolkit.Intel): a Watch by the agent it names (brief 031: any agent) or the Scout's Listen on a
        /// person, given as the action panel gives it (walk, then act; refused orders pop their reason), or the Fixer's panel on the person for
        /// a turn.</summary>
        public void IntelOrder(string role, string verb, OpId by)
        {
            if (Game == null || Modal != ModalGate.Modal.None) return;
            var t = new Target { Kind = "person", Key = role, Label = Name(role), Id = role };
            if (verb == "turn")
            {
                var tr = views?.Find(role); if (!tr || !cam) return;
                var vp = cam.WorldToViewportPoint(tr.position); if (vp.z <= 0 || vp.x < 0.1f || vp.x > 0.9f || vp.y < 0.1f || vp.y > 0.9f) LookAt(tr.position);
                var sp = cam.WorldToScreenPoint(tr.position + Vector3.up); Physics.SyncTransforms(); PointerHover(sp); OpenPanel(t, sp); return;
            }
            if (verb == "listen") by = OpId.Scout; // Listen stays the Scout's
            var plate = PlateOf(by); int slot = Array.FindIndex(plate, x => x.Id == verb); if (slot < 0) return;
            Intent i = null; Guard("intel", () => i = Verb(by, slot, t)); if (i?.Order == null) return;
            var lk = Locked(by, i, t);
            if (!lk.ok) { var w = views?.Find(role); Pop(ShortOf(i.Order, lk) ?? lk.why, null, w ? w.position : (Vector3?)null, Feedback.Refused, 2f, PopIcon(lk.code ?? ""), lk.why); return; }
            if (SeenGate(i, t, null, "left")) return;
            Give(by, i.Order, t, null);
        }

        readonly Dictionary<string, (float at, int sel, int tick, OpId? by)> watchPick = new();

        /// <summary>Brief 031 (Intel's chip and the door's next step): who should Watch `target` (a role, camera:&lt;id&gt;, patrol): the selected
        /// agent if it can (too far or out of line still counts: it walks to its ring); else the free agent who'd learn soonest (the walk to
        /// its ring plus the Watch), ties to the Scout; else the busy one free soonest. Null when nobody can (already watched, tethered, down).
        /// Read-only (LearnQuery, one A* each); kept a second.</summary>
        public OpId? WatchBy(string target)
        {
            var g = Game; if (g == null) return null;
            int sel = Selected[(int)Agent] ? (int)Agent : -1;
            if (watchPick.TryGetValue(target, out var c) && c.sel == sel && Time.unscaledTime - c.at < 1f && Math.Abs(g.Tick - c.tick) < 2 * Game.Hz) return c.by;
            Answer Q(int k) { Answer a = default; Guard("watch by", () => a = g.LearnQuery(Op.Name((OpId)k), "watch", target)); return a; }
            bool Can(Answer a, bool busy) => a.ok || a.code is "too-far" or "no-clear-line" || (busy && a.code == "agent-busy");
            OpId? by = null;
            if (sel >= 0 && Can(Q(sel), false)) by = (OpId)sel;
            else
            {
                var t = target.StartsWith("camera:") || target == "patrol" ? WatcherTarget(target) : new Target { Kind = "person", Key = target, Label = Name(target), Id = target };
                long best = long.MaxValue;
                for (int k = 0; k < 4; k++)
                {
                    var a = Q(k); if (!Can(a, true)) continue;
                    long s = g.R.SightWatchS;
                    if (a.code == "agent-busy") s += 100000 + Math.Max(0, g.Ops[k].BusyEnd - g.Tick) / Game.Hz; // a free agent first
                    else { ActWalk w = null; Guard("watch by", () => w = ActPlace((OpId)k, new Order { Op = Op.Name((OpId)k), Do = "watch", Target = target }, t)); if (w != null) s += w.Seconds; }
                    long score = s * 10 + k; // ties: the Scout first (then the plate order)
                    if (score < best) { best = score; by = (OpId)k; }
                }
            }
            watchPick[target] = (Time.unscaledTime, sel, g.Tick, by);
            return by;
        }

        /// <summary>The door marker's next step while it reads "unk" (Sky 2026-10-18, the Tech's Watch on machines; brief 031: any agent): the
        /// first watcher of the door the cell has not learned (the entrance camera unless looped, then the patrol), and who should watch it
        /// (WatchBy). Null when the door is not "unk" or nobody can.</summary>
        public (OpId by, string watcher)? DoorNext()
        {
            var g = Game; if (g == null || DoorState != "unk") return null;
            bool Learned(string w, bool routine) => g.Known(w) is { } k && k.Sight && k.How != "told" && (!routine || k.Routine);
            string watcher = !(g.LoopOn && g.LoopCam == "entrance") && !Learned("camera:entrance", false) ? "camera:entrance"
                : !g.PatrolDown && g.PatrolAwayUntil <= g.Tick && !Learned("patrol", true) ? "patrol" : null;
            if (watcher == null) return null;
            return WatchBy(watcher) is OpId b ? (b, watcher) : null;
        }

        /// <summary>The Tech's Watch knob greys while his loop holds (he is tied to the cabinet: the engine refuses run-tethered); its rows on a
        /// camera or the patrol still show, locked, with the short reason.</summary>
        public bool KnobTethered(OpId by, Slot s) => by == OpId.Tech && s.Id == "watch" && Game != null && Game.LoopOn;

        Target WatcherTarget(string watcher) => watcher.StartsWith("camera:") ? new Target { Kind = "camera", Key = watcher[7..], Label = WatcherName(watcher), Id = watcher } : new Target { Kind = "patrol", Key = "patrol", Label = WatcherName(watcher), Id = "patrol" };

        /// <summary>The door's next-step chip clicked: the agent selected first; selected, the Watch is given (walk, then watch).</summary>
        public void DoorNextClick()
        {
            if (Game == null || Modal != ModalGate.Modal.None || DoorNext() is not { } nx) return; var (by, watcher) = nx;
            if (!Selected[(int)by] || Agent != by) { Select(by, false); return; }
            var t = WatcherTarget(watcher); var plate = PlateOf(by); int slot = Array.FindIndex(plate, x => x.Id == "watch"); if (slot < 0) return;
            Intent i = null; Guard("door next", () => i = Verb(by, slot, t)); if (i?.Order == null) return;
            var lk = Locked(by, i, t);
            if (!lk.ok && lk.code is not ("too-far" or "agent-busy")) { Pop(ShortOf(i.Order, lk) ?? lk.why, null, frame.World(Game.S.At(Game.S.GuardPost)), Feedback.Refused, 2f, PopIcon(lk.code ?? ""), lk.why); return; }
            Give(by, i.Order, t, null);
        }

        /// <summary>The getaway car is held (brief 018 §1: a holder within 8 m or on the exit lane; the engine's CarHeld). Read only.</summary>
        public bool CarHeldNow { get { var g = Game; if (g == null) return false; bool held = false; Guard("car held", () => held = g.CarHeld()); return held; } }
        /// <summary>The car's hold reach in metres (rulebook fight-hold-m; brief 018 takes it to 8), and whether the final fight is on (the ring
        /// is drawn only then).</summary>
        public float CarHoldM => Game != null ? Game.R.HoldCm / 100f : 0f;
        public bool CarRingOn => Game is { } g && g.FightBranch.Length > 0 && g.FightOverTick > g.Tick && !g.Over;

        /// <summary>A right click on a person's pin (the HUD's pip): as a right click on the person (Sky, route B: Vale's star over his car).</summary>
        public void RightOnPerson(string role)
        {
            if (!cam || views == null) return; var t = views.Find(role); if (!t || !t.gameObject.activeSelf) return;
            var sp = cam.WorldToScreenPoint(t.position + Vector3.up * 1.0f); if (sp.z <= 0) return;
            Physics.SyncTransforms(); PointerHover(sp); PointerRight(sp);
        }

        /// <summary>A marker clicked (door or car): with the Enforcer selected, his first knob on that target; else select him.</summary>
        public void StepMarkClick(string which)
        {
            if (Game == null || Modal != ModalGate.Modal.None || !cam) return;
            var e = (int)OpId.Enforcer;
            if (!Selected[e] && !(which == "car" && GoAvailable)) { Select(OpId.Enforcer, false); Status = "Enforcer selected"; return; }
            if (which == "door" && DoorState == "seen") { PopKey("door-seen", null, frame.World(Game.S.At(Game.S.GuardPost)), Feedback.Refused, "Seen there."); return; } // a learned eye on the door: no order
            if (which == "car" && GoAvailable) { GoGive(Agent, null, "left"); return; } // brief 018: anyone in the car, the marker is Go
            if (Agent != OpId.Enforcer) Select(OpId.Enforcer, false);
            var t = views.Find(which == "car" ? "car/getaway" : "marker/door"); if (!t) return;
            var sp = cam.WorldToScreenPoint(t.position + Vector3.up * (which == "car" ? 0.8f : 1.0f)); if (sp.z <= 0) return;
            Physics.SyncTransforms(); int was = Picked; Pick(0); PointerHover(sp); PointerRight(sp);
            if (was != 0 && Picked == 0) { Picked = was; intentKey = ""; } // the click gave the order: the knob is not left armed
        }
    }
}
