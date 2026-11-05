using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 041, "Click walks, hold acts" (Sky 2026-10-21: "this is all playing pretty well - lets move it into the game"), ported from
    /// the web playground (testbed/controls: data.js PRESETS.hold, app.js, orders.js, fire.js, status.js). The left button never harms: a click
    /// walks (on a thing, to where its main act is done, without acting; a double click runs); a hold fills over 0.4 s and, let go when full,
    /// gives the main calm act (Sky 2026-10-22, commit on release; Shift runs there first). The right button: a tap opens the ring; a hold on a
    /// person or a car fills the same way and, let go when full, gives a lasting fire order to every selected drawn agent (all draw if none is):
    /// draw, aim, fire on its own until the target is down, X or ×, or a new act. Once a fight is on (Game.FreeFightOn) a right click on a
    /// hostile focuses fire at once (engine 0.19 focus). A hold latches to what it was pressed on; travel of 80 px, an early release, a right click
    /// or Esc cancels it. A squad act takes its doer out of the selection. Your own agent under the cursor always wins a click. In third person
    /// the right hold commits on release for the driven agent ("Right aims, left fires" stays brief 024's aim). Every act is an engine order
    /// (move, the act, fire, focus, free, cease, cancel), so every run replays exactly; the charge, the plan's ghosts and the ¼ speed of a
    /// portrait drag are host-side only.</summary>
    public sealed partial class HandoverHost
    {
        bool HoldOn => controls.HoldActs;
        public const float ChargeS = 0.4f, ChargeShowS = 0.2f, RightTapS = 0.2f; // Sky 2026-10-21: under 0.2 s a right press is a tap (the ring); the fire charge begins only at 0.2 s and fills to the same 0.4 s total

        /// <summary>A hold under way: what it is on, which button, since when, how long it needs; Done once it acted.</summary>
        public sealed class HoldCharge
        {
            public Target T; public int Button; public float Start; public float NeedS = ChargeS; public float Need => NeedS * ChargeScale; public bool Done, Fire, Force, Run, Bystander;
            public string FireId = "", Verb = "", Extra = ""; public ContextPlan Plan; public int Way = -1;
            /// <summary>Brief 067 C2: the pill's own intent for this hold when it differs from the row (a group Board's slowest time), else null.</summary>
            public Intent PillI;
            /// <summary>Who does it, chosen at the press and kept (the best placed then: the playground's charge.only).</summary>
            public List<int> Doers;
            /// <summary>Full and waiting for the button to come up: the pill says "Let go · …" (commit on release, Sky 2026-10-22).</summary>
            public bool Ready => !Done && Progress >= 1f;
            public float Progress => Button == 1 ? Mathf.Clamp01((Time.unscaledTime - Start - RightTapS) / Mathf.Max(0.05f, Need - RightTapS)) : Mathf.Clamp01((Time.unscaledTime - Start) / Mathf.Max(0.05f, Need));
            /// <summary>The pill shows the charge after a short wait (a quick click is still a click; a right tap is still the ring).</summary>
            public bool Shown => !Done && Time.unscaledTime - Start >= (Button == 0 ? ChargeShowS : RightTapS);
        }
        public HoldCharge Charge { get; private set; }
        /// <summary>Shots only: a charge takes this many times longer (the proof's still of a half-filled pill); 1 in play.</summary>
        public static float ChargeScale = 1f;

        struct Press { public bool On, Drag, Used, Cancelled; public Vector2 At; public float T0; public Target T; }
        Press lp, rp;
        float lastClickAt = -9f; Vector2 lastClickP; Target lastClickT;
        bool ringRun; // Shift while the ring is open: a wedge taken runs there
        bool giveRun;           // set while an order is given as a run (Give's walk carries Run)

        /// <summary>The proof's counters.</summary>
        public int HoldActsDone { get; private set; }
        public int HoldCancels { get; private set; }
        public int ThingWalks { get; private set; }
        public int FireHoldsBegun { get; private set; }

        void HoldControlsChanged() { Charge = null; lp = default; rp = default; Plan.Clear(); FireHold = null; camFollow = false; }

        void HoldReset() { HoldControlsChanged(); Array.Clear(acts, 0, 4); fireBeatAt = -9f; fireStoppedAt = -9f; FireLocks.Clear(); pd = default; DragSlowUntil = -9f; stick = null; tabT = null; lastGroupWalk.Clear(); }

        // ---------------------------------------------------------------- the main calm act on a thing

        /// <summary>The thing's main calm act for the selection: the first available way ContextRank lists that is not violent, not Pressure,
        /// not a refused approach (the primary agent's first). -1 none. A hold is deliberate and the pill names it, so a gamble or a tie is fine.</summary>
        int MainWay(ContextPlan plan)
        {
            if (plan == null) return -1;
            if (IsCellCar(plan.T)) return CarMainWay(plan); // brief 067 C: Board (the whole selection), then Go with Vale aboard, else Drive
            // brief 043 §3.3: an act that just opened (fresh, 90 s) leads, in ContextRank's order among the fresh ones
            for (int pass = 0; pass < 2; pass++)
            {
                int bestW = -1, bestSeq = -1;
                foreach (var w in plan.Choice.Order)
                {
                    var r = plan.Row(w); if (r == null || !r.Available || r.I?.Order == null) continue; int seq = FreshRank(plan.T, r.I.Order); if (seq < 0) continue;
                    var o = r.I.Order; if (o.Do is "fire" or "takedown" or "go" || o.Do == "turn" && (o.Approach == "pressure" || ContextRank.Refuses(r.I.Outcome))) continue;
                    if (pass == 0 && !plan.Givers[w].Contains(Agent)) continue;
                    if (seq > bestSeq) { bestSeq = seq; bestW = w; } // the newest news first, then ContextRank's order
                }
                if (bestW >= 0) return bestW;
            }
            for (int pass = 0; pass < 2; pass++)
                foreach (var w in plan.Choice.Order)
                {
                    var r = plan.Row(w); if (r == null || !r.Available || r.I?.Order == null) continue;
                    var o = r.I.Order; if (o.Do is "fire" or "takedown" or "go") continue;
                    if (o.Do == "turn" && (o.Approach == "pressure" || ContextRank.Refuses(r.I.Outcome))) continue;
                    if (ContextRank.Tier(new ContextRank.Way { Do = o.Do }) > 5) continue;
                    if (pass == 0 && !plan.Givers[w].Contains(Agent)) continue;
                    return w;
                }
            return -1;
        }

        (ContextPlan plan, int way) MainOf(Target t, Vector2 screen)
        {
            if (t == null || t.Kind == "op" && !DownedOp(t) || Game == null) return (null, -1); // item 31: a downed agent's main way is Carry (a hold)
            if (HoverPlan != null && SameTarget(HoverPlan.T, t) && Time.unscaledTime - hoverPlanAt < 0.5f) return (HoverPlan, HoverWay);
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, screen)) || plan == null) return (null, -1);
            return (plan, MainWay(plan));
        }

        /// <summary>Who gives a way (Sky 2026-10-21: fixed, no setting): the best placed selected agent who can (the shortest walk to it); the
        /// rest hold. A hostile is the right hold's: every selected drawn agent fires (BeginFireHold).</summary>
        public List<int> DoersOf(ContextPlan plan, int way)
        {
            var who = new List<int>(); if (plan == null || way < 0 || way >= plan.Givers.Count) return who;
            foreach (var g in plan.Givers[way]) if (Selected[(int)g]) who.Add((int)g);
            if (who.Count == 0) foreach (var g in plan.Givers[way]) who.Add((int)g);
            if (plan.Row(way)?.I is { } wi && (wi.Order?.Do == "board" || wi.How == StepOutHow)) return who; // brief 067 C (Sky: "I wanted the whole squad to just get in the car"): Board goes to every selected agent who can, one order each in the same tick Step out too (Sky 2026-11-04: everyone aboard steps out)
            if (Driving && who.Contains(dAgent)) return new List<int> { dAgent }; // brief 085: in third person the agent in hand does what he can; the squad does the rest
            var idle = who.FindAll(k => !BusyAgent(k)); if (idle.Count > 0) who = idle; // Sky 2026-10-22: best placed never takes a busy agent while an idle one can
            if (who.Count < 2) return who;
            int best = who[0], bs = int.MaxValue;
            foreach (var k in who) { var r = RowFor(plan, way, k); int s = 0; if (r != null) Guard("walk", () => { var aw = ActPlace((OpId)k, r.I.Order, plan.T); s = aw?.Seconds ?? 0; }); if (s < bs) { bs = s; best = k; } }
            return new List<int> { best };
        }

        /// <summary>On an act (walking to it, doing it) or holding a queued one; a walk alone is not busy (orders.js busy).</summary>
        /// <summary>Item 31: a downed agent of the cell (a thing with ways: Carry), not one standing.</summary>
        bool DownedOp(Target t) => t != null && t.Kind == "op" && IsOpName(t.Key) && Game != null && Game.Ops[(int)Op.Parse(t.Key)].Down;
        /// <summary>Item 33: the ring at a clicked thing, sticky, nothing taken (ContextRank's quick take is never used for a click). False when
        /// the thing has no ways (the click then walks as before).</summary>
        public int ClickRings { get; private set; }
        /// <summary>Brief 067 E: rings a click opened with every way shut (the proof reads it).</summary>
        public int ShutRings { get; private set; }
        public int RightTaps { get; private set; }
        /// <summary>Proofs: a left click as the hold preset reads one (HoldClick) at a screen point.</summary>
        public void LeftClickScripted(Vector2 p) => HoldClick(p, false);
        bool ClickRing(Target t, Vector2 p) => ClickRing(t, p, out _);
        bool ClickRing(Target t, Vector2 p, out ContextPlan plan)
        {
            plan = null; if (t == null || Game == null) return false;
            ContextPlan pl = null; if (!Guard("panel", () => pl = ContextFor(t, p)) || pl == null) return false; plan = pl;
            // a held Go still rings (shut, with its holders: brief 057 §4.2); brief 067 E (Suture's 0.2.122 findings §2): so does any thing whose
            // ways are all locked for the selection, its ways shut with their short locks (Sky's ten clicks on Inez at 7:47 to 8:01 all walked)
            if (plan.Choice.Order.Count == 0 && !plan.Pn.Rows.Exists(r => !r.Available && r.I?.Order != null)) return false;
            OpenRing(plan, p, false); if (Ring == null) return false;
            Ring.Sticky = true; Ring.Always = false; Ring.FromHold = false; Ring.ByClick = true; ClickRings++; RightClickUsed = true; // brief 057 (Aster): the first-act hint retires on the first ring a left click opens (item 33: the right tap does nothing now)
            if (plan.Choice.Order.Count == 0) ShutRings++;
            SideLog("ring", plan.Choice.Order.Count == 0 ? "open-shut" : "open", t); return true;
        }

        bool BusyAgent(int k) => Game != null && (Game.Ops[k].Busy != null || Game.Ops[k].Act != null || pending[k] != null || Plan.Exists(x => x.K == k));

        /// <summary>Agent k's own row for a way (the same order), else null.</summary>
        static PanelRow RowFor(ContextPlan plan, int way, int k)
        {
            var rep = plan.Row(way); if (rep == null) return null; if ((int)rep.By == k) return rep;
            var o = rep.I.Order;
            foreach (var r in plan.Pn.Rows) if ((int)r.By == k && r.Available && r.I?.Order is Order q && q.Do == o.Do && q.Approach == o.Approach && q.How == o.How && q.Target == o.Target && q.To == o.To) return r;
            return null;
        }

        /// <summary>A way given by the hold, the ring, a portrait drop: to each doer, a new order replacing the old (the cancel says what is lost);
        /// paused with "start together", it waits as a ghost.</summary>
        /// <summary>fromSel: an order from the selection (the hold, the ring), not a portrait drop: in a squad whoever goes leaves the selection
        /// (Sky 2026-10-22: "if you do squad mode, and an agent is actioned, they leave the squad"); the next order picks from who is left, Select
        /// all brings everyone back, and a finished agent does not rejoin. only: the doers the hold chose at its press.</summary>
        void HoldGive(ContextPlan plan, int way, Target t, Vector2 at, bool run, string source, bool fromSel = true, List<int> only = null)
        {
            var rep = plan?.Row(way); if (rep == null) return;
            if (rep.I.Order?.Do == "fire") { DoWay(rep, t, at, plan.Pn.World, source); return; } // a Fire wedge: every selected agent who can, one card (HostSquad)
            var doers = only?.FindAll(k => RowFor(plan, way, k) != null); if (doers == null || doers.Count == 0) doers = DoersOf(plan, way);
            var given = new List<int>();
            foreach (var k in doers)
            {
                var r = RowFor(plan, way, k); if (r == null) continue;
                var row = r; int kk = k;
                if (Queueing) { QueuePlan(k, PlanLabel(row.I, t), HudModel.AtlasIcon(row.I.Order.Do, row.I.Order.Approach, ""), PlanRoute(k, row.I.Order, t), () => GiveNow(kk, row, t, at, run, source)); given.Add(k); continue; }
                int n0 = Game.Orders.Count; GiveNow(k, row, t, at, run, source);
                bool ok = pending[k] != null || Game.Ops[k].Busy != null; for (int i = n0; i < Game.Orders.Count && !ok; i++) ok = Game.Orders[i].ok && Game.Orders[i].order.Op == Op.Name((OpId)k) && Game.Orders[i].order.Do != "cancel";
                if (ok) given.Add(k);
            }
            bool board = rep.I.Order?.Do == "board";
            if (board && given.Count > 0) { if (given.Count > 1) SquadBoards++; BoardRefusals(plan, given); } // brief 067 C4: the rest say why (their why chips)
            if (!fromSel || given.Count == 0) return;
            // brief 067 C1.2 (Glyph): whoever boards leaves the selection, even the whole squad (a boarded agent is stepped out by any move order, and
            // a ground click walks the selection); the selection may be empty: the car answers with none (BuildPanel), 1–4 and Squad bring them back
            var sel = SelectedList(); if (!board && (sel.Count < 2 || !sel.Exists(k => !given.Contains(k)))) return; // alone, or the whole squad goes: nobody leaves
            var left = new List<string>(); foreach (var k in given) if (Selected[k] && !(Driving && k == dAgent)) { Selected[k] = false; left.Add(Op.Name((OpId)k)); } // brief 085: never the agent in hand: the view never moves by itself
            if (left.Count == 0) return;
            int firstLeft = Array.IndexOf(Selected, true); Agent = Driving ? (OpId)dAgent : firstLeft >= 0 ? (OpId)firstLeft : (OpId)given[0]; Picked = -1; intentKey = ""; SquadLeaves++; // the pill keeps a face: the first aboard
            Status = $"{string.Join(", ", left)} {(left.Count > 1 ? "leave" : "leaves")} the squad";
        }

        void GiveNow(int k, PanelRow r, Target t, Vector2 at, bool run, string source)
        {
            if (Game == null || Game.Over) return;
            var op = Game.Ops[k];
            if (SameActRunning(k, r.I.Order, run)) return; // brief 083: the act already under way is never restarted (Sky's 0.2.159 run: a hold on Inez while Watching her: "Watch cancelled · 3 s lost", then the same Watch again)
            if (op.Busy != null || op.Act != null || pending[k] != null) CancelAgent(k, null); // a new order replaces the old one (engine 0.23: a walk in too; brief 043: the cancel pops) (CancelAgent's card says what was lost)
            EndLasting(k); // and a lasting fire order (brief 041: a fire order lasts until down, X or a new order)
            Unfresh(t, r.I.Order); // brief 043: the pip goes once the opened act is given
            acts[k] = new ActMem { T = t, O = r.I.Order, At = Time.unscaledTime, Active = true, Ing = IngOf(r.I.Order, t), Done = DoneOf(r.I.Order, t), Head = r.I.Label };
            giveRun = run; try { DoRow(r, t, at, TargetWorld(t), source); } finally { giveRun = false; }
            HoldActsDone++;
        }

        // ---------------------------------------------------------------- walking: a click on a thing or the ground

        /// <summary>Where the way's doer stands to do it (the end of its walk), null when already in reach.</summary>
        Pt? StandSpot(ContextPlan plan, int way)
        {
            var who = DoersOf(plan, way); if (who.Count == 0) return null;
            var r = RowFor(plan, way, who[0]) ?? plan.Row(way); ActWalk aw = null; Guard("walk", () => aw = ActPlace((OpId)who[0], r.I.Order, plan.T));
            return aw?.To;
        }

        readonly List<(int k, Pt to)> lastGroupWalk = new(); float lastGroupWalkAt = -9f;

        /// <summary>The selection walks (or runs) to an engine point in formation: each keeps its place relative to the others when they are already
        /// spaced (1.1 m apart, within 4 m of their middle), else they spread on a 1.2 m ring; each spot the nearest walkable one.</summary>
        public void GroupWalk(Pt centre, bool run, Vector2? screen, List<int> agents = null)
        {
            agents ??= SelectedList(); agents = agents.FindAll(k => !Game.Ops[k].Inside && !Game.Ops[k].Down && !(Driving && k == dAgent));
            lastGroupWalk.Clear();
            if (agents.Count == 0) return;
            int n = agents.Count; long cx = 0, cy = 0; foreach (var k in agents) { cx += Game.Ops[k].Pos.X; cy += Game.Ops[k].Pos.Y; }
            var mid = new Pt((int)(cx / n), (int)(cy / n)); var offs = new List<Pt>();
            foreach (var k in agents) offs.Add(new Pt(Game.Ops[k].Pos.X - mid.X, Game.Ops[k].Pos.Y - mid.Y));
            bool spaced = n > 1; int far = 0;
            for (int i = 0; i < n && spaced; i++) { far = Math.Max(far, G.Dist(offs[i], new Pt(0, 0))); for (int j = 0; j < n; j++) if (i != j && G.Dist(offs[i], offs[j]) < 110) spaced = false; }
            if (far > 400) spaced = false;
            lastGroupWalk.Clear(); lastGroupWalkAt = Time.unscaledTime; bool said = false;
            for (int i = 0; i < n; i++)
            {
                int k = agents[i]; var off = n == 1 ? new Pt(0, 0) : spaced ? offs[i] : new Pt((int)(Math.Cos(i * 6.2832 / n + 0.8) * 120), (int)(Math.Sin(i * 6.2832 / n + 0.8) * 120));
                var want = new Pt(centre.X + off.X, centre.Y + off.Y);
                var wg = CarWalk; Pt? to = wg.Free(want) ? want : wg.NearestFree(want); // engine 0.33.1: never a spot on a standing car's ground (Game.Walk)
                if (!to.HasValue || G.Dist(to.Value, want) > 300) { if (!said) PopKey("no-way", screen, null, Feedback.Refused, "Can't walk there."); said = true; continue; }
                lastGroupWalk.Add((k, to.Value));
                var dest = to.Value; int kk = k;
                if (Queueing) { QueuePlan(k, $"{(run ? "Run" : "Walk")} · {WalkSeconds(k, dest, run)} s", run ? "run" : "s_moving", RouteWorld(k, dest), () => WalkNow(kk, dest, run, null)); continue; }
                WalkNow(k, dest, run, screen);
            }
        }

        void WalkNow(int k, Pt to, bool run, Vector2? screen)
        {
            if (Game == null || Game.Over) return;
            if (Game.Ops[k].Busy != null || Game.Ops[k].Act != null || pending[k] != null) CancelAgent(k, null); // brief 043: an act dropped by a walk says so
            pending[k] = null;
            if (Issue(new Order { Op = Op.Name((OpId)k), Do = "move", Point = to, Run = run && RunInRulebook }, screen, null)) { Pulses.Add((frame.World(to), Feedback.Ok, Time.unscaledTime)); while (Pulses.Count > 8) Pulses.RemoveAt(0); }
        }

        /// <summary>A double click on the walk just given turns it into a run (the same ends, Run on).</summary>
        bool UpgradeRun()
        {
            if (Time.unscaledTime - lastGroupWalkAt > controls.doubleSeconds + 0.1f || lastGroupWalk.Count == 0) return false;
            var list = new List<(int, Pt)>(lastGroupWalk); lastGroupWalkAt = -9f;
            foreach (var (k, to) in list) { var op = Game.Ops[k]; if (op.Running) continue; var keep = pending[k]; var keepAt = pendingAt[k]; var keepFrom = pendingFrom[k]; if (Issue(new Order { Op = Op.Name((OpId)k), Do = "move", Point = to, Run = true }, null, null)) { pending[k] = keep; pendingAt[k] = keepAt; pendingFrom[k] = keepFrom; } }
            return true;
        }

        List<int> SelectedList() { var l = new List<int>(); for (int k = 0; k < 4; k++) if (Selected[k]) l.Add(k); return l; }
        int WalkSeconds(int k, Pt to, bool run) { var r = MoveRoute(Game.Ops[k].Pos, to); if (r == null) return 0; int sp = Math.Max(1, run ? Game.R.RunSpeed : Game.R.CellSpeed); return (int)((new Sim.Path(r).Length + sp * Game.Hz - 1) / (sp * Game.Hz)); }
        List<Vector3> RouteWorld(int k, Pt to) { var pts = new List<Vector3>(); var r = MoveRoute(Game.Ops[k].Pos, to); if (r != null) foreach (var p in r) pts.Add(frame.World(p) + Vector3.up * 0.1f); return pts; }
        List<Vector3> PlanRoute(int k, Order o, Target t) { ActWalk aw = null; Guard("walk", () => aw = ActPlace((OpId)k, o, t)); return aw == null ? new List<Vector3> { frame.World(Game.Ops[k].Pos) + Vector3.up * 0.1f } : RouteWorld(k, aw.To); }

        // ---------------------------------------------------------------- the commander's pointer

        /// <summary>Brief 041 (Sky 2026-10-21/22, "Commit on release adopted", the hold latch): a hold latches to what it was pressed on (the pill
        /// and the charge follow it as it moves), fills over 0.4 s, then says "Let go · …"; letting go then commits on it wherever the cursor is.
        /// Cursor travel of more than CancelPx from the press, an early release, a right click or Esc cancels; the pill dims from DimFrom of it.</summary>
        public const float CancelPx = 80f, DimFrom = 0.55f;
        /// <summary>Brief 083 (Sky on 0.2.159: "change the drag LMB to select squads, to move map"): a left press that travels this far (kit px) before the hold's pill
        /// shows (ChargeShowS, 0.2 s) is a pan, on any thing or on the ground: the charge is dropped unseen and the release is no click. Under it, a click or a hold (a shaky
        /// hand on a click stays a click); once the pill shows the hold has the pointer and only CancelPx of travel ends it. RingMovePx (6) was tight for a mouse click.</summary>
        public const float DragPanPx = 8f;
        /// <summary>The proof reads it: left drags that panned the map, and the agents a click skipped or asked about because they are busy.</summary>
        public int PanDrags { get; private set; }
        /// <summary>How far the pointer has gone from the held press (kit px), for the pill's dim; 0 with no hold.</summary>
        public float HoldTravel { get; private set; }
        public int HoldTravelCancels { get; private set; }
        public int FocusClicks { get; private set; }
        public int SquadLeaves { get; private set; }

        void CancelHold(bool travel = false)
        {
            var c = Charge; if (c == null || c.Done) return; Charge = null; HoldCancels++; if (travel) HoldTravelCancels++;
            Status = $"{(c.Fire ? "Fire" : c.Verb)} cancelled"; if (c.Button == 0) lp.Cancelled = true; else rp.Cancelled = true;
        }

        /// <summary>Per frame in the commander view (the hold preset): the left and right buttons. True when it took the pointer.</summary>
        bool HoldFrame(UnityEngine.InputSystem.Mouse mouse, Vector2 p, bool onHud)
        {
            var kb = Keyboard.current; bool shift = controls.Held(kb, PlayAction.AddToGroup), ctrl = controls.Held(kb, PlayAction.Force) || kb != null && (kb.leftCommandKey.isPressed || kb.rightCommandKey.isPressed);
            float now = Time.unscaledTime;
            if (Ring != null) { ringRun = shift; lp = default; if (!rp.On) return false; } // the ring has the pointer (RightFrame)
            if (!ModalGate.AllowsClicks(Modal)) { lp = default; rp = default; Charge = null; return false; }
            HoldLatchFrame(p);
            // ---- the left button
            if (mouse.leftButton.wasPressedThisFrame && !onHud && !rp.On && !TrackpadOrbitHeld(kb)) // brief 051: Alt + left drag turns the view when Trackpad drag orbit is on
            {
                var own = AgentNear(p); // your own agent under the cursor always wins (Sky: "couldn't click the enforcer")
                if (own != null && Game.Ops[(int)Op.Parse(own.Key)].Down) own = null; // item 31: a downed agent is a thing: a hold carries it (priced, on the pill), a click walks
                lp = new Press { On = true, At = p, T0 = now, T = own ?? PressTarget(p) }; if (Panel != null) ClosePanel();
                if (own == null) { var (plan, way) = MainOf(lp.T, p); if (way >= 0) Charge = new HoldCharge { T = lp.T, Button = 0, Start = now, Plan = plan, Way = way, Run = shift, Verb = plan.Row(way).I.Label, Doers = DoersOf(plan, way), PillI = way == HoverWay && ReferenceEquals(plan, HoverPlan) && HoverIntent != null && !ReferenceEquals(HoverIntent, plan.Row(way).I) && HoverIntent.Actionable ? HoverIntent : null }; } // best placed is chosen at the press and stays (brief 067 C: a group Board, everyone who can)
                return true;
            }
            if (lp.On)
            {
                var c = Charge?.Button == 0 ? Charge : null;
                if (mouse.rightButton.wasPressedThisFrame && c != null && !c.Done) { CancelHold(); rp = new Press { On = true, Used = true, At = p, T0 = now }; return true; } // a right click cancels the hold, and is nothing else
                if (!mouse.leftButton.isPressed)
                {
                    var was = lp; lp = default;
                    if (was.Cancelled) return true;
                    if (was.Drag) { if (c != null) Charge = null; BoxScreen = null; PanDrags++; return true; } // brief 083: a drag pans; its release is nothing else
                    if (was.Used || c != null && c.Done) { if (c != null) Charge = null; return true; }
                    if (c != null)
                    {
                        Charge = null;
                        if (c.Ready) { HoldGive(c.Plan, c.Way, c.T, was.At, c.Run || shift, "left", true, c.Doers); return true; } // let go after full: the order
                        if (now - c.Start >= ChargeShowS) { HoldCancels++; Status = $"{c.Verb} cancelled"; return true; } // let go early: nothing
                    }
                    HoldClick(p, shift);
                    return true;
                }
                // brief 083: a drag is the map's (Controls goes on grabbing while lp.Drag). Never after a right click cancelled this hold (Glyph's 0.2.94 review #12), nor once the hold's pill shows
                if (!lp.Drag && !lp.Cancelled && !lp.Used && (c == null || !c.Done && !c.Shown) && (p - lp.At).magnitude > DragPanPx * KitScale) { lp.Drag = true; Charge = c != null ? null : Charge; ClearHover(); Grab(lp.At, true, true); }
                return true;
            }
            // ---- the right button
            if (mouse.rightButton.wasPressedThisFrame && !onHud)
            {
                var own = AgentNear(p); rp = new Press { On = true, At = p, T0 = now, T = own ?? FirePressTarget(p) }; if (Panel != null) ClosePanel(); // brief 067 D: a moving target just off the cursor is still the press's
                if (own == null && FocusTarget(rp.T, out var fid, out var fforce)) { rp.Used = true; FocusFire(rp.T, fid, fforce, p, null); } // Syndicate: once a fight is on a right click on a hostile focuses fire, no hold
                return true; // under 0.2 s it is a tap: no charge, no pill, no draw (the charge begins below at 0.2 s)
            }
            if (rp.On && mouse.rightButton.isPressed && Charge?.Button != 1 && !rp.Used && !rp.Cancelled && now - rp.T0 >= RightTapS && rp.T?.Kind != "op")
            { var (id, force) = HoldFireId(rp.T); if (id.Length > 0) Charge = new HoldCharge { T = rp.T, Button = 1, Start = rp.T0, Fire = true, FireId = id, Force = force, Bystander = force, Verb = "Fire", Extra = FireExtra(id, force) }; }
            if (rp.On)
            {
                if (!mouse.rightButton.isPressed)
                {
                    var was = rp; rp = default; float held = now - was.T0; var c = Charge?.Button == 1 ? Charge : null;
                    if (was.Used || was.Cancelled) { if (c != null) Charge = null; return true; }
                    if (c != null) { Charge = null; if (c.Ready) { BeginFireLock(c, was.At); return true; } HoldCancels++; Status = "Fire cancelled"; return true; } // full: a lasting fire order; early: nothing
                    if (held < RightTapS && (p - was.At).magnitude < RingMovePx * KitScale && Ring == null)
                    {
                        RightTaps++; // item 33 (Sky 2026-10-24: "Right click then doesn't do anything"): a right tap does nothing, on a target or the ground (the walk stop it gave on the ground is S, Esc or a new click); the right button fires (its hold)
                    }
                    return true;
                }
                if (!rp.Used && rp.T is { Kind: "person", Key: "councillor" } && now - rp.T0 >= RightTapS) { rp.Used = true; PopKey("never-vale", rp.At, TargetWorld(rp.T), Feedback.Refused, $"never fire on {ValeWord}: the mission fails"); }
                return true;
            }
            return !onHud && (mouse.leftButton.isPressed || mouse.rightButton.isPressed); // a press that began on the HUD: the street hears none of it
        }

        /// <summary>The latch, per frame: a hold under way follows what it was pressed on; cursor travel past CancelPx from the press cancels it.</summary>
        void HoldLatchFrame(Vector2 p)
        {
            var c = Charge; HoldTravel = 0f; if (c == null || c.Done) return;
            var press = c.Button == 0 ? lp : rp; if (!press.On) return;
            if (c.Button == 0 && !c.Shown && !Driving) return; // brief 083: before the pill shows, travel is a pan (HoldFrame), not a hold pulled away
            HoldTravel = (p - press.At).magnitude / KitScale;
            // brief 067 D (Sky on 0.2.122: "when the rivals move I kept losing the target on them"): a fire hold is latched to its target, so the
            // cursor following a moving target is not travel away: the travel is from the press or from the target's drawn point, whichever is nearer
            if (c.Fire && cam && c.T != null && TargetWorld(c.T) is Vector3 tw) { var sp = cam.WorldToScreenPoint(tw + Vector3.up * 1f); if (sp.z > 0f) HoldTravel = Mathf.Min(HoldTravel, ((Vector2)sp - p).magnitude / KitScale); }
            if (HoldTravel > CancelPx) { CancelHold(true); HoldTravel = 0f; }
        }

        /// <summary>Brief 067 D: what a right press is on. As PressTarget; but when the pointer is on the ground or a passer-by, a fire target
        /// (a hostile, a combatant, a rival, security: what the hover offers fire on) within FireAcquirePx of the pointer, nearest first, or the
        /// one the hover was on within the last 0.4 s (a rival walking out from under a still cursor), is the press's target.</summary>
        Target FirePressTarget(Vector2 p)
        {
            var t = PressTarget(p); if (t != null && t.Kind != "crowd") return t;
            if (!cam || Game == null) return t;
            if (rawHover != null && FireOffered(rawHover) && Time.unscaledTime - rawHoverAt < 0.4f && TargetWorld(rawHover) is Vector3 hw && cam.WorldToScreenPoint(hw + Vector3.up) is var hs && hs.z > 0f && ((Vector2)hs - p).magnitude < FireAcquirePx * 2f * KitScale) { FireAcquires++; return rawHover; }
            Target best = null; float bd = FireAcquirePx * KitScale;
            foreach (var pk in Pickables())
            {
                if (pk.T.Kind is "op" or "crowd" || !FireOffered(pk.T)) continue;
                var sp = cam.WorldToScreenPoint(pk.Anchor); if (sp.z <= 0f) continue; float d = ((Vector2)sp - p).magnitude; if (d < bd) { bd = d; best = pk.T; }
            }
            if (best != null) FireAcquires++;
            return best ?? t;
        }
        /// <summary>Brief 067 D: the last thing under the pointer that was not the ground, a passer-by or an agent, and when (PointerHover sets it).</summary>
        Target rawHover; float rawHoverAt = -9f;
        /// <summary>Brief 067 D: how near (kit px at 1080p) a fire target must be for a right press beside it to take it.</summary>
        public const float FireAcquirePx = 34f;
        /// <summary>The proof reads it: right presses that took a fire target beside the pointer.</summary>
        public int FireAcquires { get; private set; }

        /// <summary>The pill's opacity for a hold the cursor is pulling away from (1 near the press, 0.35 at the cancel edge).</summary>
        public float HoldDim => HoldTravel <= CancelPx * DimFrom ? 1f : 1f - 0.65f * Mathf.Clamp01((HoldTravel - CancelPx * DimFrom) / (CancelPx * (1f - DimFrom)));

        /// <summary>Your own agent near a screen point (within 26 px of its body, feet to head), nearest first: a click on it always selects it,
        /// whatever stands behind or beside it (the playground's agentNear). Null: none.</summary>
        public Target AgentNear(Vector2 screen)
        {
            if (!cam || views == null || Game == null) return null; float best = 26f * KitScale; int bk = -1;
            for (int k = 0; k < 4; k++)
            {
                var op = Game.Ops[k]; if (op.Inside || op.InCar || Driving && k == dAgent && Following) continue;
                var tr = views.Find("op/" + Op.Name((OpId)k)); if (!tr || !tr.gameObject.activeInHierarchy) continue;
                var f = cam.WorldToScreenPoint(tr.position); var hd = cam.WorldToScreenPoint(tr.position + Vector3.up * 1.9f); if (f.z <= 0f || hd.z <= 0f) continue;
                Vector2 a = f, v = (Vector2)hd - a; float t = Mathf.Clamp01(Vector2.Dot(screen - a, v) / Mathf.Max(1e-3f, v.sqrMagnitude)); float d = (screen - (a + v * t)).magnitude; if (op.Down) d = ((Vector2)f - screen).magnitude * 1.25f; // item 31: a downed agent lies down: near its body only, never the column of air above it
                if (d < best) { best = d; bk = k; }
            }
            if (bk < 0) return null; var n = Op.Name((OpId)bk);
            return new Target { Kind = "op", Key = n, Label = Game.Ops[bk].Down ? $"the downed {n}" : n };
        }

        /// <summary>What a press is on: the thing the pill is on when the pointer has not moved off it (the hover's hold and a Tab choice), else what is under the pointer.</summary>
        Target PressTarget(Vector2 p) { var t = TargetAt(p); if (Hover != null && Hover.Kind != "op" && (p - lastHover).magnitude < 4f * KitScale && (t == null || !SameTarget(t, Hover)) && (stack.Exists(x => SameTarget(x, Hover)) || tabT != null)) return Hover; return t; }

        /// <summary>A left click: on an agent, select it (Shift adds); on a thing with a main act, walk to where it is done (a double click runs);
        /// anything else is the ground under it: walk there (a double click runs; Shift + click runs).</summary>
        void HoldClick(Vector2 p, bool shift)
        {
            float now = Time.unscaledTime; var t = AgentNear(p) ?? TargetAt(p); // your own agent first: a click on it always selects it (even out of the squad, even busy)
            bool dbl = now - lastClickAt < controls.doubleSeconds && (t != null ? SameTarget(t, lastClickT) : lastClickT == null && (p - lastClickP).magnitude < 24f * KitScale);
            if (dbl) { lastClickAt = -9f; lastClickT = null; } else { lastClickAt = now; lastClickP = p; lastClickT = t; }
            if (t != null && t.Kind == "op" && DownedOp(t)) { ClickRing(t, p); SideLog("click", "ring", t); return; } // items 31 + 33: a click on a downed agent opens its ring (Carry is a way you take, never the click); beside it the ground walks
            if (t != null && t.Kind == "op") { Select(Op.Parse(t.Key), shift); SideLog("click", "select", t); return; }
            ContextPlan cp = null;
            if (t != null && !dbl && ClickRing(t, p, out cp)) { SideLog("click", "ring", t); return; } // a passer-by too when the selection has a way on them (the Enforcer's takedown); none: the click walks // item 33 (Sky 2026-10-24: "left click brings up the action ring"): a left click on a thing opens its ring, nothing taken
            if (t != null && dbl && Ring == null && ClickRing(t, p, out cp)) { SideLog("click", "ring", t); return; }
            // brief 067 E1/E4 (Glyph): a person or thing the selection has no row on, that someone else in the cell has: the "who can" pop, no walk;
            // only scenery nobody has a row on, a passer-by or the ground walks
            if (t != null && t.Kind is not ("crowd" or "op") && cp != null && cp.Pn.Rows.Count == 0 && WhoCanPop(t, p)) { SideLog("click", "who-can", t); return; }
            if (t != null && t.Kind != "crowd")
            {
                var (plan, way) = MainOf(t, p);
                if (way >= 0)
                {
                    if (dbl && UpgradeRun()) { SideLog("click", "run", t); return; }
                    var spot = StandSpot(plan, way);
                    if (!spot.HasValue) { Status = $"already in reach of {t.Label}"; return; }
                    if (BusyClick(p, out var whoT)) { SideLog("click", "busy", t); return; } // brief 083
                    ThingWalks++; GroupWalk(spot.Value, dbl || shift, p, whoT); SideLog("click", "walk-thing", t); return;
                }
            }
            if (!cam || !frame.Ground(cam.ScreenPointToRay(p), out var w)) return;
            if (dbl && UpgradeRun()) { SideLog("click", "run", t); return; }
            if (BusyClick(p, out var whoG)) { SideLog("click", "busy", t); return; } // brief 083: a click never stops an act by itself
            GroupWalk(frame.Engine(w), dbl || shift, p, whoG); SideLog("click", t != null ? "walk-ground-from-thing" : "walk-ground", t); // brief 067 E: a click on a thing that walks is recorded as such (Suture: "click:walk-from-thing")
        }

        // ---------------------------------------------------------------- the right hold: fire

        /// <summary>What a right hold fires on: a person (any: a bystander or a contact forced, no Ctrl), a hostile, the patrol, the rivals, the
        /// police, a car. Vale, the cell, cameras and things: nothing.</summary>
        (string id, bool force) HoldFireId(Target t)
        {
            if (t == null || Game == null) return ("", false);
            if (t.Kind is "person" && t.Key == "councillor") return ("", false);
            if (t.Kind is "op" or "door" or "cabinet" or "stash" or "body") return ("", false); // a camera is security: Aster 2026-10-22, the right hold fires on it (engine 0.15: camera:<id>)
            var id = FireIdOf(t, out var force); bool any = false; for (int k = 0; k < 4; k++) any |= Selected[k] && Armed(k) || Driving && k == dAgent && Armed(k);
            return any ? (id, force) : ("", false);
        }

        /// <summary>Brief 080: the preview FireExtra read last: the offer's odds, and the sum under Alt (HostOdds).</summary>
        public FirePreview OfferPreview { get; private set; }
        string FireExtra(string id, bool force)
        {
            int k = -1; for (int i = 0; i < 4; i++) if (Selected[i] && Armed(i)) { k = i; break; }
            if (Driving) k = dAgent; if (k < 0) return "";
            FirePreview fp = null; Guard("aim", () => fp = Game.PreviewFire((OpId)k, id, force)); int s = PreviewDownS(fp, k, id); OfferPreview = fp;
            return s >= 0 ? $"{(fp?.DownIs == "disabled" ? "disabled" : "down")} in {s} s" : "";
        }

        /// <summary>A lasting fire order the host gave (a right hold let go, a focus click, a portrait lock): who, on what, since when. Pin: a
        /// portrait lock, which a focus click leaves alone; Focus: a focus click in a fight.</summary>
        public sealed class FireSession { public List<int> Agents = new(); public string Id = "", Name = ""; public bool Force, Trigger, Pin, Focus; public float Start; }
        /// <summary>Kept for the old "keeps firing while held" (no longer given: brief 041, fire commits on release); always null now.</summary>
        public FireSession FireHold { get; private set; }
        /// <summary>The lasting fire orders the host gave, newest last, read for the pill's words while they last.</summary>
        public readonly List<FireSession> FireLocks = new();

        /// <summary>Agents taken off whatever lasting fire session they were in (a new fire order, a new act, X).</summary>
        void LocksDrop(IEnumerable<int> agents) { var set = new HashSet<int>(agents); for (int i = FireLocks.Count - 1; i >= 0; i--) { FireLocks[i].Agents.RemoveAll(set.Contains); if (FireLocks[i].Agents.Count == 0) FireLocks.RemoveAt(i); } }

        /// <summary>A new act given to agent k ends its lasting fire order (brief 041: until down, X or a new order). A walk or a run keeps it: the
        /// engine fires on the move (Sky 2026-10-22, engine 0.21.0), and it ends out of range (fire.out-of-range: DoneLog drops the lock and pops "Too far").</summary>
        void EndLasting(int k)
        {
            if (Game == null || !Armed(k) || Game.Ops[k].Gun.Order.Length == 0) return;
            if (OneInput) { LocksDrop(new[] { k }); return; } // engine 0.35 (sticky focus): the engine ends the focus on an act itself; no cease
            Send(new List<Order> { new Order { Op = Op.Name((OpId)k), Do = "cease" } }, null, null); LocksDrop(new[] { k });
        }

        /// <summary>The plate's ×: the agent's act stops, and its lasting fire order (brief 041: "until down, X or ×, or a new order").</summary>
        public void PlateStop(int k) { if (Game == null) return; if (doing[k].CanCancel || pending[k] != null) CancelAgent(k, null); EndLasting(k); }

        /// <summary>Who fires on a right hold or a focus click (briefs/041/next-batch.md item 13, engine 0.26.0): every selected agent the engine
        /// would take the order from (Game.FireQuery: a holstered agent draws, one out of reach or line walks in; a focus never walks); never an
        /// agent a portrait locked when `unpinned` (a focus click leaves portrait locks alone); in third person the driven agent and the squad. With `mark`, each
        /// selected agent it refuses gets its two words on the plate (Fable's less.done.fire.&lt;code&gt;) for FireRefusalS. With no target id
        /// (nothing to ask about) the armed selection, as before but without the drawn-first filter that left holstered agents out.</summary>
        List<int> Shooters(bool unpinned, string id = "", bool force = false, bool focus = false, bool mark = false)
        {
            var able = new List<int>();
            for (int k = 0; k < 4; k++)
            {
                if (Driving && k == dAgent) { if (Armed(k)) able.Add(k); continue; } // brief 085 (Sky: "only one agent would start to shoot ... unless I pressed F"): the agent in hand fires as he always did (a busy one is cancelled: Playable) and the squad with him; it was the driven agent alone
                if (!Selected[k] || unpinned && FireLocks.Exists(s => s.Pin && s.Agents.Contains(k))) continue;
                if (id.Length == 0) { if (Armed(k) && !(k == (int)OpId.Enforcer && Game.Vale == ValeAt.WithEnforcer)) able.Add(k); continue; }
                Answer a = default; bool walk = false;
                if (!Guard("fire-query", () => a = Game.FireQuery((OpId)k, id, force, focus, out walk))) continue;
                if (a.ok) { able.Add(k); continue; }
                if (!mark) continue;
                if (a.code == "engine-error") { FireLogAdd($"fire-query\t{k}\tengine-error\t{a.why}"); continue; } // logged, never shown (Aster: no "restart" on a plate)
                fireRefused[k] = a.code ?? ""; fireRefusedAt[k] = Time.unscaledTime; FireLogAdd($"fire-refused\t{k}\t{a.code}");
            }
            return able;
        }

        /// <summary>The faces for the fire pill: who a right hold on `t` would fire with now (FireQuery per selected agent; asked once per target,
        /// selection and second, never every frame: the walk check is a grid search).</summary>
        public List<int> FireShooters(Target t = null)
        {
            if (Game == null) return new List<int>();
            var (id, force) = t != null ? HoldFireId(t) : ("", false); int mask = 0; for (int k = 0; k < 4; k++) if (Selected[k]) mask |= 1 << k;
            string key = $"{id}|{force}|{mask}|{Game.Tick / Game.Hz}|{Driving}|{dAgent}";
            if (key != shootersKey) { shootersKey = key; shootersNow = Shooters(false, id, force); }
            return new List<int>(shootersNow);
        }
        string shootersKey = ""; List<int> shootersNow = new();
        /// <summary>The proof asks (item 13): who a squad fire order on `t` would go to now, marking each refused agent's plate as a real order would.</summary>
        public List<int> ShootersFor(Target t, bool mark) { if (Game == null || t == null) return new List<int>(); var (id, force) = HoldFireId(t); return Shooters(false, id, force, mark: mark); }

        /// <summary>Nobody selected takes the fire order: one pop at the click, the first refusal's words (else "Nobody selected is armed.").</summary>
        void NobodyFires(Vector2 at, Target t)
        {
            string w = ""; for (int k = 0; k < 4 && w.Length == 0; k++) if (Selected[k]) w = FireRefusalWord(k);
            PopKey("unarmed", at, TargetWorld(t), Feedback.Info, w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : "Nobody selected is armed.");
        }

        /// <summary>A fight is on (the engine's: contact, settling 3 s after the last; Sky's §14 note "ends on contact"): the focus click works.</summary>
        public bool FightOnNow => Game != null && Game.FreeFightOn;

        /// <summary>Glyph N2 (0.2.137): the focus (GDD §10: "one ring under the focus … pips over the focus only"), one rule for the ring, the lock
        /// disc and the pips: a target is a focus while an agent on foot (not down, inside or in a car) holds a lasting fire order on it (Gun.Order).
        /// A target he fires at on his own (return fire, FireNow) is not a focus: before, the ring read FireNow and the disc Gun.Order, so a ring
        /// stood with no disc over the rival he answered.</summary>
        public bool IsFocus(string id)
        {
            var g = Game; if (g == null || string.IsNullOrEmpty(id)) return false;
            for (int k = 0; k < 4; k++) { var op = g.Ops[k]; if (op.Gun.Order == id && !op.Down && !op.Inside && !op.InCar) return true; }
            return false;
        }

        /// <summary>A right click on a hostile while a fight is on: a focus target (its fire id). Not the cell, not Vale, not a calm person.</summary>
        bool FocusTarget(Target t, out string id, out bool force)
        {
            id = ""; force = false; if (!FightOnNow || t == null || !(t.Kind == "hostile" || Combatant(t))) return false;
            (id, force) = HoldFireId(t); return id.Length > 0;
        }

        /// <summary>The focus click (tuning.md §14, engine 0.19.0): one fire order with focus per shooter, on this tick: a lock until the target is
        /// down (never a walk-in), drawn if holstered. Portrait locks stay.</summary>
        void FocusFire(Target t, string id, bool force, Vector2 at, List<int> who)
        {
            who ??= Shooters(true, id, force, focus: true, mark: true); if (who.Count == 0) { NobodyFires(at, t); return; }
            var list = new List<Order>(); foreach (var k in who) list.Add(new Order { Op = Op.Name((OpId)k), Do = "fire", Target = id, Force = force, Focus = true });
            holdCommits = true; try { Fight(list, "right", at, TargetWorld(t), $"fire at {Name(id)}"); } finally { holdCommits = false; }
            LocksDrop(who); FireLocks.Add(new FireSession { Agents = new List<int>(who), Id = id, Name = FireName(id), Force = force, Start = Time.unscaledTime, Focus = true });
            FocusClicks++; FireLogAdd($"focus\t{string.Join(",", who)}\t{id}");
        }

        /// <summary>A right hold let go when full (commit on release, Sky 2026-10-22: "I'm either letting go to stop, or to fire"): a lasting fire
        /// order for every shooter: draw, aim, fire on its own until the target is down, X or ×, or a new order.</summary>
        void BeginFireLock(HoldCharge c, Vector2 at)
        {
            var who = Shooters(false, c.FireId, c.Force, mark: true);
            if (who.Count == 0) { NobodyFires(at, c.T); return; }
            var list = new List<Order>(); foreach (var k in who) list.Add(new Order { Op = Op.Name((OpId)k), Do = "fire", Target = c.FireId, Force = c.Force });
            holdCommits = true; try { Fight(list, "right", at, TargetWorld(c.T), $"fire at {Name(c.FireId)}"); } finally { holdCommits = false; }
            LocksDrop(who); FireLocks.Add(new FireSession { Agents = who, Id = c.FireId, Name = FireName(c.FireId), Force = c.Force, Start = Time.unscaledTime });
            FireHoldsBegun++; FireLogAdd($"hold-fire\t{string.Join(",", who)}\t{c.FireId}");
        }

        // ---------------------------------------------------------------- third person (the hold preset)

        /// <summary>Third person: the focus when the pill offers fire on it (security, a hostile, a combatant; never a passer-by the pill did not
        /// name), so the right hold and the pill agree (next-batch item 7).</summary>
        Target TpFireFocus() => DriveFocus is Target f && f.Kind != "op" && FireOffered(f) ? f : null;

        /// <summary>Where the hover offers a right hold's fire (Aster 2026-10-22, next-batch item 8): hostiles and combatants, and security at rest
        /// (the door guard, the patrol, police and robots, cameras: the price is the point); in the range its bystanders and cars. Never a
        /// passer-by, a contact or Vale (a right hold still fires on a passer-by, forced).</summary>
        public bool FireOffered(Target t) => t != null && (t.Kind is "hostile" or "patrol" or "security" or "unit" or "rivals" or "camera" || t.Kind == "person" && t.Key == "guard" || Combatant(t) || Game != null && Game.Range && t.Kind is "bystander" or "car");

        /// <summary>The nearest hostile in front of the driven agent within 25 m (the playground's aim assist with nothing under the cursor).</summary>
        Target TpHostileAhead()
        {
            if (!Driving || views == null) return null; Target best = null; float bd = 25f; var me = views.DrivePos; var fwd = Quaternion.Euler(0f, fYaw, 0f) * Vector3.forward;
            foreach (var pk in Pickables())
            {
                if (!Combatant(pk.T) && pk.T.Kind != "hostile") continue; var d = pk.Anchor - me; d.y = 0f; float m = d.magnitude;
                if (m < bd && Vector3.Angle(fwd, d) < 52f) { bd = m; best = pk.T; }
            }
            return best;
        }
    }
}
