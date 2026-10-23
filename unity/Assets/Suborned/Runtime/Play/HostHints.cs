using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The training hints (briefs/011/motive-fights.md §8, Motive's table H1–H11; Fable's words in text.json `hints`): one at a time,
    /// in priority order, each once a run, shown only after 8 s of mission time with no order, gone on its gone-when act (or hidden while its
    /// trigger stops holding); H hides the layer for good (a setting, kept in PlayerPrefs). Each rings its target in the street, or a
    /// portrait. The triggers are Motive's prose, read here from the engine's state; H7's "the patrol's cone will cover the door within
    /// 15 s" is approximated as the patrol on its sweep within 20 m of the door. Also the run-seen toast (once, on the order after the first
    /// run.seen). Hints read the run; they never change it, so a replay is untouched.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class HintShown { public string Id = "", Line = "", Who = "", Cause = ""; public Vector3? Ring; public int Portrait = -1; }
        string trigWho = "", trigCause = ""; // the role a trigger found (brief 014: the short line's {who}); H13's cause (door or timer)
        /// <summary>H13's door cause, spent and learned apart from its timer cause (Fable: two causes, one hint).</summary>
        const string H13Door = "H13:door";
        /// <summary>The hint on screen now, else null.</summary>
        public HintShown HintNow { get; private set; }
        public bool HintsOn { get => Settings.Current.gameplay.hints; set { var s = Settings.Current; if (s.gameplay.hints == value) return; s.gameplay.hints = value; s.Save(); } } // brief 051: settings.json (migrated once from the Suborned.Play.Hints key)
        const string HintsPref = "Suborned.Play.Hints", LearnedPref = "Suborned.Play.HintsLearned";
        /// <summary>The hints that teach a control (hover a price, right click a contact, N, carry): the only place a control is taught (Fable's
        /// control-hint rule, calm-down batch 1). Once the player has done what one teaches, it is retired for good (PlayerPrefs, per profile).</summary>
        static readonly HashSet<string> ControlHints = new() { "H3", "H5", "H7", "H11", "H12", "H13", H13Door, "H17" };
        static HashSet<string> learned;
        static HashSet<string> Learned() => learned ??= new(PlayerPrefs.GetString(LearnedPref, "").Split(',', StringSplitOptions.RemoveEmptyEntries));
        static void Learn(string id)
        {
            if (!ControlHints.Contains(id) || !Learned().Add(id)) return;
            PlayerPrefs.SetString(LearnedPref, string.Join(",", learned)); PlayerPrefs.Save();
        }
        readonly HashSet<string> hintsDone = new();
        int hintOrdersAt; // the order count when the hint showed (its gone-when reads the orders after it)
        int lastOrderTick;

        /// <summary>A toast shown for a few seconds (the run-seen line), else null.</summary>
        public string Toast { get; private set; }
        public float ToastUntil { get; private set; }
        bool runSeenPending, runSeenShown;

        void ResetHints() { hintsDone.Clear(); HintNow = null; hintOrdersAt = 0; lastOrderTick = 0; Toast = null; runSeenPending = runSeenShown = false; }

        /// <summary>An order was taken (the player's): the idle clock restarts, and a pending toast shows.</summary>
        void OrderTaken()
        {
            lastOrderTick = Game.Tick;
            if (runSeenPending && !runSeenShown && Text != null && Text.Toasts.TryGetValue("run-seen", out var line) && line.Length > 0)
            { runSeenShown = true; runSeenPending = false; Toast = line; ToastUntil = Time.unscaledTime + 6f; Say("", line, Feedback.Refused); }
        }

        void HintLog(LogLine l) { if (l.Code == "run.seen" && !runSeenShown) runSeenPending = true; }

        /// <summary>Every half second of mission time: the current hint goes when its act happens; else the first hint whose trigger holds.</summary>
        void Hints()
        {
            if (Text == null || Game.Over || Game.Tick % 5 != 0) return;
            if (!HintsOn) { HintNow = null; return; }
            // GDD v0.24 (H13's door cause): the first time the public door is the next step and a wall hides it, the turn hint cuts in at
            // once, over any hint and without the idle wait; it is spent only by the door standing in clear view (DoorViewFrame)
            if (!ShowBriefing && HintNow is not { Id: "H13", Cause: "door" } && Trigger("H13", out var dr, out var dp) && trigCause == "door" && Text.Hints2.Find(x => x.Id == "H13") is { } h13)
            { HintNow = new HintShown { Id = "H13", Line = h13.DoorLine.Length > 0 ? h13.DoorLine : h13.Line, Ring = dr, Portrait = dp, Cause = "door" }; hintOrdersAt = Game.Orders.Count; return; }
            if (HintNow != null && Game.Tick - lastOrderTick >= 8 * Game.Hz && Text.Hints2.Find(x => x.Id == HintNow.Id) is { Priority: >= 0 })
            {   // the end steps (H14 to H16, negative priorities) cut in over a teaching hint on show: they say what wins the mission now (Sky, route B)
                var learnedNow = Learned();
                foreach (var h in Text.Hints2)
                {
                    if (h.Priority >= 0) break; if (hintsDone.Contains(h.Id) || learnedNow.Contains(h.Id) || h.Line.Length == 0) continue;
                    if (!Trigger(h.Id, out var r2, out var p2)) continue;
                    HintNow = new HintShown { Id = h.Id, Line = h.Line, Ring = r2, Portrait = p2, Who = trigWho }; hintOrdersAt = Game.Orders.Count; return;
                }
            }
            if (HintNow != null)
            {
                if (Gone(HintNow.Id)) { hintsDone.Add(HintNow.Id); Learn(HintNow.Id); HintNow = null; }
                else if (Trigger(HintNow.Id, out var ring, out var portrait) is false) HintNow = null; // its trigger stopped holding: hidden, not spent
                else { HintNow.Ring = ring; HintNow.Portrait = portrait; }
                return;
            }
            if (ShowBriefing || Game.Tick - lastOrderTick < 8 * Game.Hz) return;
            var known = Learned();
            foreach (var h in Text.Hints2)
            {
                if (h.Id != "H13" && (hintsDone.Contains(h.Id) || known.Contains(h.Id)) || h.Line.Length == 0) continue; // H13: each cause keeps its own spent state (Trigger)
                if (!Trigger(h.Id, out var ring, out var portrait)) continue;
                HintNow = new HintShown { Id = h.Id, Line = h.Id == "H13" && trigCause == "door" && h.DoorLine.Length > 0 ? h.DoorLine : h.Line, Ring = ring, Portrait = portrait, Who = trigWho, Cause = h.Id == "H13" ? trigCause : "" }; hintOrdersAt = Game.Orders.Count;
                return;
            }
        }

        /// <summary>The ✕ on the hint: this one is spent. H: the whole layer off (or back on).</summary>
        public void RelearnHints() { PlayerPrefs.DeleteKey(LearnedPref); learned = null; PlayerPrefs.Save(); HintsOn = true; Status = "Hints on, from the start."; PopKey("hints-on", null, SelectedWorld(), Feedback.Info, Status); }
        public void DismissHint() { if (HintNow != null) { hintsDone.Add(HintNow.Cause == "door" ? H13Door : HintNow.Id); HintNow = null; } }
        public void ToggleHints() { HintsOn = !HintsOn; if (!HintsOn) HintNow = null; Status = HintsOn ? "Hints on." : $"Hints off. {controls.Label(PlayAction.Hints)} brings them back."; PopKey(HintsOn ? "hints-on" : "hints-off", null, SelectedWorld(), Feedback.Info, Status); }

        IEnumerable<Order> OrdersTaken(int from = 0) { for (int i = from; i < Game.Orders.Count; i++) if (Game.Orders[i].ok) yield return Game.Orders[i].order; }
        bool Given(Func<Order, bool> f, int from = 0) { foreach (var o in OrdersTaken(from)) if (f(o)) return true; return false; }

        static readonly string[] Roles = { "waitress", "driver", "guard", "councillor" };
        Vector3? At(string viewKey) { var t = views.Find(viewKey); return t && t.gameObject.activeSelf ? t.position : (Vector3?)null; }

        /// <summary>Motive's trigger for a hint, from the engine's state, with what it rings.</summary>
        bool Trigger(string id, out Vector3? ring, out int portrait)
        {
            var g = Game; ring = null; portrait = -1; trigWho = ""; trigCause = "";
            switch (id)
            {
                case "H1":
                    {
                        foreach (var r in Roles) { var p = g.PersonByRole(r); if (p.KRoutine || p.KNeed || p.KTemperament) return false; }
                        if (Given(o => o.Do is "watch" or "listen")) return false;
                        ring = g.S.HasPlace("d:Between tables") ? frame.World(g.S.At("d:Between tables")) : At("waitress");
                        return true;
                    }
                case "H2":
                    foreach (var r in Roles)
                    {
                        var p = g.PersonByRole(r);
                        if (p.KRoutine && !(p.KNeed && p.KTemperament) && !Given(o => o.Do == "listen" && o.Target == r)) { ring = At(r); trigWho = r; return true; }
                    }
                    return false;
                case "H3":
                    foreach (var r in Roles)
                    {
                        if (r == "councillor") continue;
                        var p = g.PersonByRole(r);
                        if (p.KNeed && !p.Turned && !Given(o => o.Do == "turn" && o.Target == r)) { portrait = (int)OpId.Fixer; ring = At(r); trigWho = r; return true; }
                    }
                    return false;
                case "H4":
                    if (g.LoopOn || g.Tick < 45 * Game.Hz || doing[(int)OpId.Tech].Busy) return false;
                    ring = At("marker/cabinet"); return true;
                case "H5":
                    foreach (var r in new[] { "waitress", "driver" })
                        if (g.PersonByRole(r).Turned && !Given(o => o.Do == "call" && o.Target == r)) { ring = At(r); trigWho = r; return true; }
                    return false;
                case "H6":
                    foreach (var r in Roles) if (g.PersonByRole(r).KnownRefused.Count > 0) { ring = At(r); trigWho = r; return true; }
                    return false;
                case "H7":
                    {
                        var en = g.Ops[(int)OpId.Enforcer]; var pp = g.PatrolPose(); var door = g.S.At(g.S.GuardPost);
                        if (en.Inside || pp == null || g.PatrolDown || g.PatrolAwayUntil > g.Tick) return false;
                        if (!G.Within(en.Pos, door, 1000) || !G.Within(pp.Value.pos, door, 2000)) return false;
                        ring = At("patrol"); return true;
                    }
                case "H8":
                    if (g.Tick < 190 * Game.Hz || !g.RivalsPresent || g.Vale == ValeAt.WithEnforcer) return false;
                    if (Given(o => o.Target == "rivals")) return false;
                    ring = At("rivals/0"); return true;
                case "H9":
                    {
                        if (g.Tick < 240 * Game.Hz) return false;
                        foreach (var r in Roles) if (g.PersonByRole(r).Turned) return false;
                        if (Given(o => o.Do == "enter")) return false;
                        var d = g.PersonByRole("driver"); if (d.KRoutine || d.KNeed || d.KTemperament) return false;
                        ring = At("driver"); return true;
                    }
                case "H10":
                    {
                        if (g.Vale != ValeAt.WithEnforcer) return false;
                        var car = g.S.At(g.S.GetawayCar);
                        for (int k = 0; k < 3; k++) { var op = g.Ops[k]; if (op.Gun.W != null && !op.Down && !op.Inside && G.Within(op.Pos, car, 1500)) return false; }
                        ring = At("car/getaway"); return true;
                    }
                case "H12": // Sky 2026-10-16: the first long walk teaches the run toggle (G, or the chip's pace glyph)
                    for (int k = 0; k < 4; k++)
                    {
                        var op = g.Ops[k]; var d = doing[k];
                        if (op.Walk == null || op.Running || op.CarryBody.Length > 0 || op.Carrying.HasValue || op.Inside || d.TicksLeft < 5 * Game.Hz) continue;
                        if (k == (int)OpId.Enforcer && g.Vale == ValeAt.WithEnforcer) continue;
                        portrait = k; ring = frame.World(op.Walk.End); return true; // the text: it rings the walk's destination
                    }
                    return false;
                case "H14": // the end steps (Fable, Aster 2026-10-18): the door is open to the Enforcer; the same check as the door marker (HostValeSteps)
                    if (!EnterAvailable) return false;
                    ring = At("marker/door"); portrait = (int)OpId.Enforcer; return true;
                case "H15": // out with Vale, not boarded yet (before H10; brief 018: Board replaces Extract)
                    if (!BoardAvailable || Given(o => o.Do == "board" && o.Op == "enforcer")) return false;
                    ring = At("car/getaway"); portrait = (int)OpId.Enforcer; return true;
                case "H16": // route B: the driver turned, Vale at his car, not taken
                    if (!g.PersonByRole("driver").Turned || g.Vale != ValeAt.Pickup) return false;
                    ring = At("councillor"); portrait = (int)OpId.Enforcer; return true;
                case "H17": // Sky 2026-10-18: a fight is on and an armed agent other than the Enforcer has not fired yet: everyone armed can fire
                    {
                        if (!g.FightOn || HoldOn && !FightOnNow) return false; // brief 041: the right click focuses only while the engine's fight is on (FreeFightOn); the first shot is the right hold
                        for (int k = 0; k < 3; k++) if (Armed(k) && !g.Ops[k].Down && !g.Ops[k].Inside) { foreach (var h in g.Hostiles) if (!h.Down && h.Present) { ring = frame.World(h.Pos(g.Tick)); portrait = k; trigWho = h.Id.StartsWith("rival") ? "rival" : "officer"; return true; } } // Fable: {who} the hostile, "rival" or "officer" (they carry no names)
                        return false;
                    }
                case "H13": // brief 016 and GDD v0.24 (Fable: two causes, one hint; the HUD shows its keys as keycaps)
                    {
                        if (g.FightOn) return false;
                        var lrn = Learned(); bool showingDoor = HintNow is { Id: "H13", Cause: "door" };
                        // door: the public door is the next step (the door marker up with the Fixer or Enforcer selected, or ringed for Enter) and
                        // a wall hides it at this yaw; once shown it holds until the door has stood in clear view (DoorViewFrame spends it)
                        bool doorNext = (DoorMarked && (Selected[(int)OpId.Fixer] || Selected[(int)OpId.Enforcer])) || EnterAvailable;
                        if (!hintsDone.Contains(H13Door) && !lrn.Contains(H13Door) && doorNext && (DoorHiddenNow || showingDoor))
                        { trigCause = "door"; ring = DoorWorld; return true; }
                        // timer: 0:20 has passed and the view has never turned (the fallback)
                        if (hintsDone.Contains("H13") || lrn.Contains("H13") || showingDoor) return false;
                        trigCause = "timer"; return g.Tick >= 20 * Game.Hz && !TurnedThisRun;
                    }
                case "H18": // brief 018 §6.7 (Fable): Vale is in the car and no Go has been given; rings the car
                    if (g.Vale != ValeAt.Car || g.Over || Given(o => o.Do == "go")) return false;
                    ring = At("car/getaway"); return true;
                case "H19": // brief 031 (Fable): the first time a security watcher's attention reaches noticed; shown once
                    if (!AttnHint(out ring, out portrait)) return false;
                    foreach (var (w, view, st, op) in Cues()) if (st >= 1) { trigWho = op; break; } // rings nothing: the watcher's own cue shows it
                    return true;
                case "H11": // brief 018 §6.6: anyone a Go would leave captured (a downed one not carried rings first, else the first standing one)
                    if (g.FightBranch.Length == 0 && !g.FightOn) return false;
                    foreach (var (name, fate, why) in LeftBehindNow()) // brief 018: anyone not in the car stays behind (LeftBehind); a downed one not carried is the warning's ring
                    { var op = g.Ops[(int)Op.Parse(name)]; if (fate == "captured" && why == "downed" && !op.CarriedBy.HasValue) { ring = At("op/" + name); return true; } }
                    foreach (var (name, fate, _) in LeftBehindNow()) if (fate == "captured" && g.AnyInCar) { ring = At("op/" + name); return true; }
                    return false;
            }
            return false;
        }

        /// <summary>Motive's gone-when for a hint: an act among the orders given since it showed (or the state it waits for).</summary>
        bool Gone(string id)
        {
            var g = Game; int from = hintOrdersAt;
            switch (id)
            {
                case "H1": return Given(o => o.Do is "watch" or "listen", from);
                case "H2": return Given(o => o.Do == "listen", from);
                case "H3": return Given(o => o.Do == "turn", from);
                case "H4": return Given(o => o.Do == "loop", from);
                case "H5": return Given(o => o.Do == "call", from);
                case "H6": return Given(o => o.Op is "scout" or "fixer", from);
                case "H7": return Given(o => o.Do is "enter" or "reroute", from) || !G.Within(g.Ops[(int)OpId.Enforcer].Pos, g.S.At(g.S.GuardPost), 1000);
                case "H8": return Given(o => o.Do == "mislead", from) || g.Vale == ValeAt.WithEnforcer;
                case "H9": return Given(o => o.Target == "driver", from) || Array.Exists(Roles, r => g.PersonByRole(r).Turned);
                case "H10": return Given(o => o.Do == "draw", from);
                case "H11": return Given(o => o.Do is "carry" or "board", from);
                case "H12": return Given(o => o.Do == "move" && o.Run, 0); // a walk ending only hides it (its trigger stops holding); a run given retires it
                case "H13": return HintNow?.Cause != "door" && TurnedThisRun; // the door cause is spent by the door in clear view (DoorViewFrame)
                case "H17": return Given(o => o.Do == "fire" && o.Op != "enforcer", from) || !g.FightOn || HoldOn && !FightOnNow;
                case "H14": return Given(o => o.Do == "enter", from);
                case "H15": return Given(o => o.Do == "board" && o.Op == "enforcer", from);
                case "H18": return Given(o => o.Do == "go", from) || Game.Over;
                case "H16": return Given(o => o.Do == "take", from) || g.Vale != ValeAt.Pickup;
                case "H19": { foreach (var c in Cues()) if (c.stage >= 1) return false; return true; } // the agent moved on or out of sight, and the watcher cooled
            }
            return false;
        }
    }
}
