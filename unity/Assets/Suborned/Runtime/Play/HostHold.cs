using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 041, "Click walks, hold acts" (Sky 2026-10-21: "this is all playing pretty well - lets move it into the game"), ported from
    /// the web playground (testbed/controls: data.js PRESETS.hold, app.js, orders.js, fire.js, status.js). The left button never harms: a click
    /// walks (on a thing, to where its main act is done, without acting; a double click runs), a hold of 0.4 s does the main calm act (Shift +
    /// hold runs there first; an early release cancels; Kill, Knock out and Fire are never a left default). The right button: a tap opens the
    /// ring; a hold on a person or a car fires (the engine's fire order draws first), every selected drawn agent at once, and keeps firing while
    /// held; the release ceases. Bystanders take the same hold, no Ctrl. In third person the setting "Right hold fires" (the default) draws and
    /// presses the driven agent's logged trigger; "Right aims, left fires" is brief 024's aim. Every act is an engine order (move, the act, fire,
    /// cease, trigger, cancel), so every run replays exactly; the charge, the plan's ghosts and the ¼ speed of a portrait drag are host-side only.</summary>
    public sealed partial class HandoverHost
    {
        bool HoldOn => controls.HoldActs;
        public const float ChargeS = 0.4f, ChargeShowS = 0.2f, RightTapS = 0.2f; // Sky 2026-10-21: under 0.2 s a right press is a tap (the ring); the fire charge begins only at 0.2 s and fills to the same 0.4 s total

        /// <summary>A hold under way: what it is on, which button, since when, how long it needs; Done once it acted.</summary>
        public sealed class HoldCharge
        {
            public Target T; public int Button; public float Start; public float Need => ChargeS * ChargeScale; public bool Done, Fire, Force, Run, Bystander;
            public string FireId = "", Verb = "", Extra = ""; public ContextPlan Plan; public int Way = -1;
            public float Progress => Button == 1 ? Mathf.Clamp01((Time.unscaledTime - Start - RightTapS) / Mathf.Max(0.05f, Need - RightTapS)) : Mathf.Clamp01((Time.unscaledTime - Start) / Mathf.Max(0.05f, Need));
            /// <summary>The pill shows the charge after a short wait (a quick click is still a click; a right tap is still the ring).</summary>
            public bool Shown => !Done && Time.unscaledTime - Start >= (Button == 0 ? ChargeShowS : RightTapS);
        }
        public HoldCharge Charge { get; private set; }
        /// <summary>Shots only: a charge takes this many times longer (the proof's still of a half-filled pill); 1 in play.</summary>
        public static float ChargeScale = 1f;

        struct Press { public bool On, Drag, Used; public Vector2 At; public float T0; public Target T; }
        Press lp, rp;
        float lastClickAt = -9f; Vector2 lastClickP; Target lastClickT;
        bool ringRun; // Shift while the ring is open: a wedge taken runs there
        bool giveRun;           // set while an order is given as a run (Give's walk carries Run)

        /// <summary>The proof's counters.</summary>
        public int HoldActsDone { get; private set; }
        public int HoldCancels { get; private set; }
        public int ThingWalks { get; private set; }
        public int FireHoldsBegun { get; private set; }
        public int FireHoldsEnded { get; private set; }

        void HoldControlsChanged() { Charge = null; lp = default; rp = default; Plan.Clear(); FireHold = null; camFollow = false; }

        void HoldReset() { HoldControlsChanged(); Array.Clear(acts, 0, 4); fireBeatAt = -9f; fireStoppedAt = -9f; FireLocks.Clear(); pd = default; DragSlowUntil = -9f; stick = null; tabT = null; lastGroupWalk.Clear(); }

        // ---------------------------------------------------------------- the main calm act on a thing

        /// <summary>The thing's main calm act for the selection: the first available way ContextRank lists that is not violent, not Pressure,
        /// not a refused approach (the primary agent's first). -1 none. A hold is deliberate and the pill names it, so a gamble or a tie is fine.</summary>
        int MainWay(ContextPlan plan)
        {
            if (plan == null) return -1;
            for (int pass = 0; pass < 2; pass++)
                foreach (var w in plan.Choice.Order)
                {
                    var r = plan.Row(w); if (r == null || !r.Available || r.I?.Order == null) continue;
                    var o = r.I.Order; if (o.Do is "fire" or "takedown" or "go") continue;
                    if (o.Do == "turn" && (o.Approach == "pressure" || r.I.Outcome == "refused")) continue;
                    if (ContextRank.Tier(new ContextRank.Way { Do = o.Do }) > 5) continue;
                    if (pass == 0 && !plan.Givers[w].Contains(Agent)) continue;
                    return w;
                }
            return -1;
        }

        (ContextPlan plan, int way) MainOf(Target t, Vector2 screen)
        {
            if (t == null || t.Kind == "op" || Game == null) return (null, -1);
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
            if (who.Count < 2) return who;
            int best = who[0], bs = int.MaxValue;
            foreach (var k in who) { var r = RowFor(plan, way, k); int s = 0; if (r != null) Guard("walk", () => { var aw = ActPlace((OpId)k, r.I.Order, plan.T); s = aw?.Seconds ?? 0; }); if (s < bs) { bs = s; best = k; } }
            return new List<int> { best };
        }

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
        void HoldGive(ContextPlan plan, int way, Target t, Vector2 at, bool run, string source)
        {
            var rep = plan?.Row(way); if (rep == null) return;
            if (rep.I.Order?.Do == "fire") { DoRow(rep, t, at, plan.Pn.World, source); return; } // a Fire wedge: the fire path
            foreach (var k in DoersOf(plan, way))
            {
                var r = RowFor(plan, way, k); if (r == null) continue;
                var row = r; int kk = k;
                if (Queueing) { QueuePlan(k, PlanLabel(row.I, t), HudModel.AtlasIcon(row.I.Order.Do, row.I.Order.Approach, ""), PlanRoute(k, row.I.Order, t), () => GiveNow(kk, row, t, at, run, source)); continue; }
                GiveNow(k, row, t, at, run, source);
            }
        }

        void GiveNow(int k, PanelRow r, Target t, Vector2 at, bool run, string source)
        {
            if (Game == null || Game.Over) return;
            var op = Game.Ops[k];
            if (op.Busy != null || pending[k] != null) CancelAgent(k, null); // a new order replaces the old one (CancelAgent's card says what was lost)
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
                Pt? to = Game.S.Grid.Free(want) ? want : Game.S.Grid.NearestFree(want);
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
            pending[k] = null; if (Game.Ops[k].Busy != null) CancelAgent(k, null);
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
        int WalkSeconds(int k, Pt to, bool run) { var r = Game.S.Grid.Route(Game.Ops[k].Pos, to); if (r == null) return 0; int sp = Math.Max(1, run ? Game.R.RunSpeed : Game.R.CellSpeed); return (int)((new Sim.Path(r).Length + sp * Game.Hz - 1) / (sp * Game.Hz)); }
        List<Vector3> RouteWorld(int k, Pt to) { var pts = new List<Vector3>(); var r = Game.S.Grid.Route(Game.Ops[k].Pos, to); if (r != null) foreach (var p in r) pts.Add(frame.World(p) + Vector3.up * 0.1f); return pts; }
        List<Vector3> PlanRoute(int k, Order o, Target t) { ActWalk aw = null; Guard("walk", () => aw = ActPlace((OpId)k, o, t)); return aw == null ? new List<Vector3> { frame.World(Game.Ops[k].Pos) + Vector3.up * 0.1f } : RouteWorld(k, aw.To); }

        // ---------------------------------------------------------------- the commander's pointer

        /// <summary>Per frame in the commander view (the hold preset): the left and right buttons. True when it took the pointer.</summary>
        bool HoldFrame(UnityEngine.InputSystem.Mouse mouse, Vector2 p, bool onHud)
        {
            var kb = Keyboard.current; bool shift = controls.Held(kb, PlayAction.AddToGroup), alt = controls.Held(kb, PlayAction.Peek), ctrl = controls.Held(kb, PlayAction.Force) || kb != null && (kb.leftCommandKey.isPressed || kb.rightCommandKey.isPressed);
            float now = Time.unscaledTime;
            if (Ring != null) { ringRun = shift; lp = default; if (!rp.On) return false; } // the ring has the pointer (RightFrame); a right hold that is firing keeps its release
            if (!ModalGate.AllowsClicks(Modal)) { lp = default; rp = default; Charge = null; return false; }
            // ---- the left button
            if (mouse.leftButton.wasPressedThisFrame && !onHud && !rp.On)
            {
                lp = new Press { On = true, At = p, T0 = now, T = PressTarget(p) }; if (Panel != null) ClosePanel();
                var (plan, way) = MainOf(lp.T, p);
                if (way >= 0) Charge = new HoldCharge { T = lp.T, Button = 0, Start = now, Plan = plan, Way = way, Run = shift, Verb = plan.Row(way).I.Label };
                return true;
            }
            if (lp.On)
            {
                if (!mouse.leftButton.isPressed)
                {
                    var was = lp; lp = default; var c = Charge?.Button == 0 ? Charge : null;
                    if (was.Drag) { if (c != null) Charge = null; if (BoxScreen.HasValue) BoxSelect(BoxScreen.Value, shift || ctrl); BoxScreen = null; return true; }
                    if (was.Used || c != null && c.Done) { if (c != null) Charge = null; return true; }
                    if (c != null) { Charge = null; if (now - c.Start >= ChargeShowS) { HoldCancels++; Status = $"{c.Verb} cancelled"; return true; } }
                    HoldClick(p, shift);
                    return true;
                }
                if (!lp.Drag && (p - lp.At).magnitude > RingMovePx * KitScale) { lp.Drag = true; if (Charge?.Button == 0 && !Charge.Done) Charge = null; }
                if (lp.Drag) BoxScreen = GuiRect(lp.At, p);
                if (Charge is { Button: 0, Done: false } ch && now - ch.Start >= ch.Need) { ch.Done = true; lp.Used = true; HoldGive(ch.Plan, ch.Way, ch.T, lp.At, ch.Run || shift, "left"); }
                return true;
            }
            // ---- the right button
            if (mouse.rightButton.wasPressedThisFrame && !onHud)
            {
                RightClickUsed = true; rp = new Press { On = true, At = p, T0 = now, T = PressTarget(p) }; if (Panel != null) ClosePanel();
                return true; // under 0.2 s it is a tap: no charge, no pill, no draw (the charge begins below at 0.2 s)
            }
            if (rp.On && mouse.rightButton.isPressed && Charge?.Button != 1 && FireHold == null && !rp.Used && now - rp.T0 >= RightTapS) { var (id, force) = HoldFireId(rp.T); if (id.Length > 0) Charge = new HoldCharge { T = rp.T, Button = 1, Start = rp.T0, Fire = true, FireId = id, Force = force, Bystander = force, Verb = "Fire", Extra = FireExtra(id, force) }; }
            if (rp.On)
            {
                if (!mouse.rightButton.isPressed)
                {
                    var was = rp; rp = default; float held = now - was.T0; var c = Charge?.Button == 1 ? Charge : null;
                    if (FireHold != null) { EndFireHold(); if (c != null) Charge = null; return true; }
                    if (c != null && c.Done) { Charge = null; return true; }
                    if (c != null) { Charge = null; if (held >= RightTapS) { HoldCancels++; Status = "Fire cancelled"; return true; } }
                    if (held < RightTapS && (p - was.At).magnitude < RingMovePx * KitScale && Ring == null) { if (was.T == null || was.T.Kind == "op") ActContext(was.T, was.At); else TapRing(was.T, was.At); }
                    return true;
                }
                if (Charge is { Button: 1, Done: false } ch && now - ch.Start >= ch.Need) { ch.Done = true; BeginFireHold(ch, rp.At); }
                if (!rp.Used && rp.T is { Kind: "person", Key: "councillor" } && now - rp.T0 >= RightTapS) { rp.Used = true; PopKey("never-vale", rp.At, TargetWorld(rp.T), Feedback.Refused, "never fire on Vale: the mission fails"); }
                return true;
            }
            return !onHud && (mouse.leftButton.isPressed || mouse.rightButton.isPressed); // a press that began on the HUD: the street hears none of it
        }

        /// <summary>What a press is on: the thing the pill is on when the pointer has not moved off it (the hover's hold and a Tab choice), else what is under the pointer.</summary>
        Target PressTarget(Vector2 p) { var t = TargetAt(p); if (Hover != null && Hover.Kind != "op" && (p - lastHover).magnitude < 4f * KitScale && (t == null || !SameTarget(t, Hover)) && (stack.Exists(x => SameTarget(x, Hover)) || tabT != null)) return Hover; return t; }

        /// <summary>A left click: on an agent, select it (Shift adds); on a thing with a main act, walk to where it is done (a double click runs);
        /// anything else is the ground under it: walk there (a double click runs; Shift + click runs).</summary>
        void HoldClick(Vector2 p, bool shift)
        {
            float now = Time.unscaledTime; var t = TargetAt(p);
            bool dbl = now - lastClickAt < controls.doubleSeconds && (t != null ? SameTarget(t, lastClickT) : lastClickT == null && (p - lastClickP).magnitude < 24f * KitScale);
            if (dbl) { lastClickAt = -9f; lastClickT = null; } else { lastClickAt = now; lastClickP = p; lastClickT = t; }
            if (t != null && t.Kind == "op") { if (CarryDowned(t, p)) return; Select(Op.Parse(t.Key), shift); return; }
            if (t != null && t.Kind != "crowd")
            {
                var (plan, way) = MainOf(t, p);
                if (way >= 0)
                {
                    if (dbl && UpgradeRun()) return;
                    var spot = StandSpot(plan, way);
                    if (!spot.HasValue) { Status = $"already in reach of {t.Label}"; return; }
                    ThingWalks++; GroupWalk(spot.Value, dbl || shift, p); return;
                }
            }
            if (!cam || !frame.Ground(cam.ScreenPointToRay(p), out var w)) return;
            if (dbl && UpgradeRun()) return;
            GroupWalk(frame.Engine(w), dbl || shift, p);
        }

        // ---------------------------------------------------------------- the right hold: fire

        /// <summary>What a right hold fires on: a person (any: a bystander or a contact forced, no Ctrl), a hostile, the patrol, the rivals, the
        /// police, a car. Vale, the cell, cameras and things: nothing.</summary>
        (string id, bool force) HoldFireId(Target t)
        {
            if (t == null || Game == null) return ("", false);
            if (t.Kind is "person" && t.Key == "councillor") return ("", false);
            if (t.Kind is "op" or "camera" or "door" or "cabinet" or "stash" or "body") return ("", false);
            var id = FireIdOf(t, out var force); bool any = false; for (int k = 0; k < 4; k++) any |= Selected[k] && Armed(k) || Driving && k == dAgent && Armed(k);
            return any ? (id, force) : ("", false);
        }

        string FireExtra(string id, bool force)
        {
            int k = -1; for (int i = 0; i < 4; i++) if (Selected[i] && Armed(i)) { k = i; break; }
            if (Driving) k = dAgent; if (k < 0) return "";
            FirePreview fp = null; Guard("aim", () => fp = Game.PreviewFire((OpId)k, id, force)); int s = PreviewDownS(fp, k, id);
            return s >= 0 ? $"{(fp?.DownIs == "disabled" ? "disabled" : "down")} in {s} s" : "";
        }

        /// <summary>A right hold firing now (commander): who, on what, since when.</summary>
        public sealed class FireSession { public List<int> Agents = new(); public string Id = "", Name = ""; public bool Force; public float Start; public bool Trigger; }
        public FireSession FireHold { get; private set; }
        /// <summary>Locks a portrait drop gave (a lasting fire order each), read for the pill's words while they last.</summary>
        public readonly List<FireSession> FireLocks = new();

        void BeginFireHold(HoldCharge c, Vector2 at)
        {
            var able = new List<int>(); for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k) && !(k == (int)OpId.Enforcer && Game.Vale == ValeAt.WithEnforcer)) able.Add(k);
            if (able.Count == 0) { PopKey("unarmed", at, TargetWorld(c.T), Feedback.Info, "Nobody selected is armed."); return; }
            var drawn = able.FindAll(Drawn); var who = drawn.Count > 0 ? drawn : able; // every selected drawn agent; none drawn: all of them draw
            var list = new List<Order>(); foreach (var k in who) list.Add(new Order { Op = Op.Name((OpId)k), Do = "fire", Target = c.FireId, Force = c.Force });
            holdCommits = true; try { Fight(list, "right", at, TargetWorld(c.T), $"fire on {Name(c.FireId)}"); } finally { holdCommits = false; }
            FireHold = new FireSession { Agents = who, Id = c.FireId, Name = FireName(c.FireId), Force = c.Force, Start = Time.unscaledTime };
            FireHoldsBegun++; FireLogAdd($"hold-fire\t{string.Join(",", who)}\t{c.FireId}");
        }

        void EndFireHold()
        {
            var s = FireHold; FireHold = null; if (s == null || Game == null) return;
            var list = new List<Order>(); foreach (var k in s.Agents) if (Armed(k) && Game.Ops[k].Gun.Order.Length > 0) list.Add(new Order { Op = Op.Name((OpId)k), Do = "cease" });
            if (list.Count > 0) Send(list, null, null);
            fireStoppedAt = Time.unscaledTime; fireStoppedOn = s.Id; FireHoldsEnded++; FireLogAdd($"hold-cease\t{string.Join(",", s.Agents)}\t{s.Id}");
        }

        // ---------------------------------------------------------------- third person (the hold preset)

        /// <summary>Third person in the hold preset: a right hold fires (the setting on: draw, then the logged trigger, held while the button is;
        /// release lets go), a right tap opens the ring; a left hold acts on the thing under the cursor (or the focus); a left click does nothing
        /// (walking is the keys). True when it took the pointer.</summary>
        bool TpHold(UnityEngine.InputSystem.Mouse mouse, Vector2 p, bool onHud)
        {
            if (!HoldOn || Ring != null) return false;
            var kb = Keyboard.current; bool shift = controls.Held(kb, PlayAction.AddToGroup), alt = controls.Held(kb, PlayAction.Peek); float now = Time.unscaledTime;
            if (controls.tpRightFires)
            {
                if (mouse.rightButton.wasPressedThisFrame && !onHud && !Aiming)
                {
                    RightClickUsed = true; rp = new Press { On = true, At = p, T0 = now, T = TargetAt(p) };
                    return true; // under 0.2 s it is a tap (the ring); the charge begins at 0.2 s
                }
                if (rp.On && mouse.rightButton.isPressed && Charge?.Button != 1 && !Aiming && !rp.Used && now - rp.T0 >= RightTapS)
                {
                    rp.Used = true; var (id, force) = HoldFireId(rp.T);
                    if (id.Length == 0 && TpHostileAhead() is Target ha) { rp.T = ha; (id, force) = HoldFireId(ha); }
                    if (id.Length > 0) Charge = new HoldCharge { T = rp.T, Button = 1, Start = rp.T0, Fire = true, FireId = id, Force = force, Bystander = force, Verb = "Fire", Extra = FireExtra(id, force) };
                }
                if (rp.On)
                {
                    if (!mouse.rightButton.isPressed)
                    {
                        var was = rp; rp = default; var c = Charge?.Button == 1 ? Charge : null;
                        if (Aiming) { AimEnd(); fireStoppedAt = now; FireHold = null; FireHoldsEnded++; if (c != null) Charge = null; return true; }
                        if (c != null && !c.Done) { Charge = null; if (now - was.T0 >= RightTapS) { HoldCancels++; Status = "Fire cancelled"; return true; } }
                        if (now - was.T0 < RightTapS && (p - was.At).magnitude < RingMovePx * KitScale) { var t = was.T; if (t == null || t.Kind == "op") ActContext(t, was.At); else TapRing(t, was.At); }
                        return true;
                    }
                    if (Charge is { Button: 1, Done: false } ch && now - ch.Start >= ch.Need) { ch.Done = true; TpFireBegin(ch, p); }
                    if (Aiming) AimFrame(mouse, p, onHud); // the agent faces the reticle; the trigger stays on the held target (trigByScript)
                    return true;
                }
            }
            if (mouse.leftButton.wasPressedThisFrame && !onHud && !Aiming)
            {
                var t = TargetAt(p); if (t == null || t.Kind == "op") t = DriveFocus;
                var (plan, way) = MainOf(t, p);
                lp = new Press { On = true, At = p, T0 = now, T = t };
                if (way >= 0) Charge = new HoldCharge { T = t, Button = 0, Start = now, Plan = plan, Way = way, Run = shift, Verb = plan.Row(way).I.Label };
                return true;
            }
            if (lp.On)
            {
                var c = Charge?.Button == 0 ? Charge : null;
                if ((p - lp.At).magnitude >= RingMovePx * KitScale && !lp.Used) { var from = lp.At; lp = default; if (c != null && !c.Done) Charge = null; dragFrom = from; dragging = true; orbiting = false; return false; } // a drag: the orbit takes it
                if (!mouse.leftButton.isPressed) { var was = lp; lp = default; if (c != null) { Charge = null; if (!c.Done && now - c.Start >= ChargeShowS) { HoldCancels++; Status = $"{c.Verb} cancelled"; } } return true; }
                if (c != null && !c.Done && now - c.Start >= c.Need) { c.Done = true; lp.Used = true; HoldGive(c.Plan, c.Way, c.T, lp.At, c.Run || shift, "left"); }
                return true;
            }
            return false;
        }

        void TpFireBegin(HoldCharge c, Vector2 p)
        {
            if (!Driving || dAgent < 0) return;
            if (!Armed(dAgent)) { PopKey("unarmed", null, views.DrivePos, Feedback.Info, "Unarmed."); return; }
            holdCommits = true; try { AimBegin(p); } finally { holdCommits = false; } // the draw goes at once: the hold was its commit
            if (!Aiming) return; aimByScript = false; trigByScript = true;
            TriggerPress(c.FireId, c.Force, null, p);
            FireHold = new FireSession { Agents = new List<int> { dAgent }, Id = c.FireId, Name = FireName(c.FireId), Force = c.Force, Start = Time.unscaledTime, Trigger = true };
            FireHoldsBegun++;
        }

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
