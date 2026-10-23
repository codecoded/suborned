using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 041 (the hold preset), the rest of the playground's port: the live words the pill and the portrait plates say (status.js,
    /// fire.js fireStatus, ui.js orderWords), read from the engine's state (activity, guns, the shot log); pause and plan (orders.js flushPlan:
    /// paused with "start together", an order waits as a ghost and every ghost starts on the tick the clock runs again); the portrait drag
    /// (app.js: a portrait dropped on a target orders that agent, or the selection it is part of; on a hostile it locks; the world at ¼ speed
    /// while dragging and 0.5 s after); the commander follow (C) and the eased centring; the keys (1–4, Shift or Ctrl toggles, 5 or Ctrl/Cmd+A
    /// all four, X stops, Tab steps an overlap); the hover's hysteresis. View and host memory only: the engine sees the same orders.</summary>
    public sealed partial class HandoverHost
    {
        // ---------------------------------------------------------------- what each agent was last given (the pill's live words)

        struct ActMem { public Target T; public Order O; public float At, EndAt; public bool Active, Begun, Cancelled, Stopped; public string Ing, Done, Head; }
        readonly ActMem[] acts = new ActMem[4];

        /// <summary>The act in its -ing form, as the pill says it ("Watching", "Paying", "Looping the camera").</summary>
        static string IngOf(Order o, Target t)
        {
            switch (o?.Do ?? "")
            {
                case "watch": return "Watching"; case "listen": return "Listening"; case "call": return "Calling"; case "loop": return "Looping the camera";
                case "reroute": return "Rerouting the patrol"; case "mislead": return "Tipping off the rivals"; case "enter": return "Going in"; case "take": return "Taking Vale";
                case "board": return "Boarding"; case "carry": return "Lifting"; case "restrain": return "Tying"; case "stash": return "Hiding"; case "drop": return "Putting down";
                case "takedown": return o.How == "kill" ? "Killing" : "Knocking out";
                case "turn": return o.Approach switch { "ask" => "Asking", "pay" => "Paying", "help" => "Helping", "pressure" => "Pressuring", _ => "Talking" };
            }
            return "Busy";
        }
        /// <summary>What "Done · …" says when the act ran out.</summary>
        static string DoneOf(Order o, Target t)
        {
            switch (o?.Do ?? "")
            {
                case "watch": return t != null && t.Kind is "camera" or "patrol" ? "learned its sight" : "learned their round"; case "listen": return "heard them"; case "call": return "call made";
                case "loop": return "camera looped"; case "reroute": return "patrol rerouted"; case "mislead": return "tip given"; case "enter": return "inside"; case "take": return "taken";
                case "board": return "aboard"; case "carry": return "lifted"; case "restrain": return "tied"; case "stash": return "hidden"; case "drop": return "put down";
                case "takedown": return o.How == "kill" ? "done" : "knocked out";
                case "turn": return o.Approach switch { "ask" => "asked", "pay" => "paid", "help" => "helped", "pressure" => "pressed", _ => "done" };
            }
            return "done";
        }

        /// <summary>Per frame: each remembered act is under way (walking to it, or doing it), or it ended: done, or stopped (a cancel).</summary>
        void HoldStatusFrame()
        {
            if (Game == null) return; float now = Time.unscaledTime;
            for (int k = 0; k < 4; k++)
            {
                ref var a = ref acts[k]; if (!a.Active) continue;
                var op = Game.Ops[k]; bool walking = pending[k] != null && pending[k].Do == a.O.Do || op.Act != null && op.Act.Do == a.O.Do, doingIt = op.Busy != null && op.Busy.Do == a.O.Do;
                if (walking || doingIt) { a.Begun = true; continue; }
                if (now - a.At < 0.3f && !a.Begun && a.O.Do != "call") continue; // the order is a tick from showing
                a.Active = false; a.EndAt = now; a.Stopped = a.Cancelled;
                if (!a.Begun && a.O.Do != "call" && !a.Cancelled) a.EndAt = -9f; // refused or never under way: nothing to say
            }
            // fire: the target down ends the hold's words with a beat; a lock lapses with its order
            void Down(FireSession s) { var tg = Game.GunOf(s.Id); var car = Game.CarOf(s.Id); bool down = car != null ? car.DisabledAt >= 0 : tg != null && tg.Down; if (down && fireBeatOn != s.Id) { fireBeatOn = s.Id; fireBeatAt = now; fireBeatName = s.Name; fireBeatIs = car != null ? "disabled" : "down"; } }
            if (FireHold != null) Down(FireHold);
            for (int i = FireLocks.Count - 1; i >= 0; i--) { var s = FireLocks[i]; Down(s); if (now - s.Start > 0.5f && !s.Agents.Exists(k => Game.Ops[k].Gun.Order == s.Id)) FireLocks.RemoveAt(i); }
        }
        /// <summary>CancelAgent's hook: what agent k was doing is stopped (the pill says "Stopped").</summary>
        void HoldCancelled(int k) { if (k >= 0 && k < 4 && acts[k].Active) acts[k].Cancelled = true; }

        float fireBeatAt = -9f, fireStoppedAt = -9f; string fireBeatOn = "", fireBeatName = "", fireBeatIs = "down", fireStoppedOn = "";
        readonly Dictionary<string, (string target, bool hit, float at)> lastHit = new();
        /// <summary>Shot's hook: who fired on what, hit or miss (the pill's "hit" or "miss").</summary>
        void HoldShotSeen(string by, string target, bool hit) => lastHit[by] = (target, hit, Time.unscaledTime);

        /// <summary>The pill's live words: what it is on (a target the HUD anchors to), the line, and the faces of those it is about.</summary>
        public sealed class LiveWords { public Target T; public string Id = "", Text = ""; public readonly List<int> Agents = new(); public readonly List<(int k, string words)> Items = new(); public bool Fire; public Order O; }

        static string Cap1(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
        static string Bare(string name) { foreach (var a in new[] { "the ", "an ", "a " }) if (name.StartsWith(a)) return name[a.Length..]; return name; }

        int WalkLeftS(int k) { var op = Game.Ops[k]; if (op.Walk == null) return 0; int sp = op.WalkSpeed > 0 ? op.WalkSpeed : Game.R.CellSpeed; return (int)Math.Max(0, (op.Walk.Length - op.WalkMm + sp * Game.Hz - 1) / Math.Max(1, sp * Game.Hz)); }
        int ActLeftS(int k) { var op = Game.Ops[k]; return op.Busy == null ? 0 : Math.Max(0, (op.BusyEnd - Game.Tick + Game.Hz - 1) / Game.Hz); }

        /// <summary>Status.js orderStatus: the orders on a thing now ("… · ordered", "Walking there · 9 s", "Watching · 12 s left", one face each
        /// for several), else "Done · …" for 2.5 s, "Stopped" for 1 s. Null: nothing to say.</summary>
        public LiveWords OrderStatus(Target t)
        {
            if (t == null || Game == null) return null; float now = Time.unscaledTime; var w = new LiveWords { T = t };
            for (int k = 0; k < 4; k++) if (acts[k].Active && SameTarget(acts[k].T, t)) w.Agents.Add(k);
            if (w.Agents.Count > 0)
            {
                var a = acts[w.Agents[0]]; w.O = a.O;
                if (now - a.At < 0.8f) { w.Text = $"{a.Head} · ordered"; return w; }
                if (w.Agents.Count == 1) { int k = w.Agents[0]; w.Text = Game.Ops[k].Busy == null ? (Game.Ops[k].Act != null ? WalkLine(Game.Ops[k].Act) + $" · {WalkLeftS(k)} s" : $"{(Game.Ops[k].Running ? "Running" : "Walking")} · {WalkLeftS(k)} s") : $"{a.Ing} · {ActLeftS(k)} s left"; return w; }
                foreach (var k in w.Agents) w.Items.Add((k, PlateWords(k))); return w;
            }
            return null; // brief 043: the old "Done · …" and "Stopped" lines are the gain chip and the cancel pop now (shown without hover)
        }

        /// <summary>One agent's fire on `id` now: draw, aim, fire, or why it waits.</summary>
        (string phase, string words) FireOne(int k, string id)
        {
            var g = Game; var op = g.Ops[k]; var gun = op.Gun;
            if (!(gun.Drawn && gun.HolsterAt < 0) || g.Tick < gun.DrawReady) return ("draw", "drawing");
            if (op.CloseWalk && op.Walk != null) return ("walk", (Text?.DoneWord("walk.plate", "→ {who}") ?? "→ {who}").Replace("{who}", Bare(FireName(id)))); // item 13: walking in for the shot, not "too far"
            var at = g.PosOf(id); if (!at.HasValue) return ("blocked", "No clear line");
            if (gun.W != null && G.Isqrt(G.D2(op.Pos, at.Value)) > gun.W.MaxCm) return ("blocked", "Too far");
            if (!g.S.Sight(op.Pos, at.Value, g.R.Eye)) return ("blocked", "No clear line");
            bool shot = lastHit.TryGetValue(Op.Name((OpId)k), out var lh) && lh.target == id && Time.unscaledTime - lh.at < 6f;
            if (FireNow[k].Firing && shot) return ("fire", "firing");
            return ("aim", "aiming");
        }

        /// <summary>Fire.js fireStatus, one quiet line (Sky: "I worry we've lost the Syndicate simplicity"; Fable's wording row 17): "Drawing",
        /// "Aiming · 1 s", "Firing at an officer · down in 3 s", "Officer down", "Too far", "No clear line", "Stopped". The newest lasting fire
        /// order the host gave first; else, in a fight, the weapons-free picks (the engine's, FireStateOf mode weapons-free), the biggest group.
        /// Null when nothing fires. Hits and misses show on the tracer and the bar, not in words.</summary>
        public LiveWords FireStatus(Target autoOn = null, bool anyAuto = false)
        {
            if (Game == null) return null; float now = Time.unscaledTime;
            if (now - fireBeatAt < 1f) return new LiveWords { Id = fireBeatOn, Fire = true, Text = $"{Cap1(Bare(fireBeatName))} {fireBeatIs}" };
            FireSession s = null; for (int i = FireLocks.Count - 1; i >= 0 && s == null; i--) if (FireLocks[i].Agents.Count > 0) s = FireLocks[i];
            List<int> on;
            if (s != null) on = s.Agents.FindAll(k => Armed(k) && (Game.Ops[k].Gun.Order == s.Id || Game.Ops[k].Gun.Order.Length == 0 && now - s.Start < 0.5f));
            else
            {
                on = new List<int>(); string best = "";
                string want = ""; if (autoOn != null && !anyAuto) want = FireIdOf(autoOn, out _);
                if (FightOnNow && (anyAuto || want.Length > 0))
                {
                    var groups = new Dictionary<string, List<int>>();
                    for (int k = 0; k < 4; k++) { var fs = FireNow[k]; if (fs.Mode == "weapons-free" && fs.Target is { Length: > 0 } ft && (anyAuto || ft == want)) { if (!groups.TryGetValue(ft, out var l)) groups[ft] = l = new List<int>(); l.Add(k); } }
                    foreach (var kv in groups) if (kv.Value.Count > on.Count || kv.Value.Count == on.Count && string.CompareOrdinal(kv.Key, best) < 0) { on = kv.Value; best = kv.Key; }
                }
                if (on.Count == 0) return now - fireStoppedAt < StoppedS ? new LiveWords { Id = fireStoppedOn, Fire = true, Text = "Stopped" } : null;
                s = new FireSession { Agents = on, Id = best, Name = FireName(best), Start = now - 1f };
            }
            var w = new LiveWords { Id = s.Id, Fire = true };
            if (on.Count == 0) { w.Text = "Stopped"; return w; }
            var ph = on.ConvertAll(k => (k, FireOne(k, s.Id)));
            var firing = ph.FindAll(x => x.Item2.phase == "fire"); var aiming = ph.FindAll(x => x.Item2.phase == "aim"); var blocked = ph.FindAll(x => x.Item2.phase == "blocked");
            if (firing.Count > 0)
            {
                int down = -1; foreach (var (k, _) in firing) { int d = FireDownS(k); if (d >= 0 && (down < 0 || d < down)) down = d; }
                w.Text = $"Firing at {s.Name}{(down >= 0 ? $" · {(Game.CarOf(s.Id) is { } c && c.DisabledAt < 0 ? "disabled" : "down")} in {down} s" : "")}"; foreach (var (k, _) in firing) w.Agents.Add(k); return w;
            }
            if (aiming.Count > 0) { int left = int.MaxValue; foreach (var (k, _) in aiming) left = Math.Min(left, Math.Max(0, Game.Ops[k].Gun.NextShot - Game.Tick)); w.Text = $"Aiming · {Math.Max(1, (left + Game.Hz - 1) / Game.Hz)} s"; foreach (var (k, _) in aiming) w.Agents.Add(k); return w; }
            var walking = ph.FindAll(x => x.Item2.phase == "walk");
            if (walking.Count > 0 && walking.Count + blocked.Count == ph.Count) { w.Text = walking[0].Item2.words; foreach (var (k, _) in walking) w.Agents.Add(k); return w; } // item 13: "→ officer" while they walk in
            if (blocked.Count == ph.Count) { w.Text = blocked[0].Item2.words; foreach (var (k, _) in blocked) w.Agents.Add(k); return w; }
            w.Text = "Drawing"; foreach (var (k, _) in ph) w.Agents.Add(k); return w;
        }

        /// <summary>The words on agent k's plate (ui.js orderWords): "queued", "drawing", "aiming", "firing", "walking 9 s", "watching 12 s"; "" idle.</summary>
        public string PlateWords(int k)
        {
            if (Game == null || k < 0 || k > 3) return "";
            if (Plan.Exists(x => x.K == k)) return "queued";
            if (DoneTone(k) == "cancel" && DonePlate(k) is { Length: > 0 } cw) return cw; // brief 043: a dropped act says "cancelled" on the plate first, even as the new order starts
            var op = Game.Ops[k];
            if (op.Down) return Text?.DoneWord("plate.down", null) ?? Text?.DoneWord("fire.agent-down", "down") ?? "down"; // item 13 (d): down is said, not only drawn grey
            if (FireRefusalWord(k) is { Length: > 0 } fr) return fr; // item 13: why this agent took no part in the squad's fire order (two words)
            string fid = op.Gun.Order.Length > 0 ? op.Gun.Order : Driving && k == dAgent && TriggerDown ? TriggerTarget : FireNow[k].Firing ? FireNow[k].Target : "";
            // at most two words (the HUD budget: three are running text): the plate's icon says walk, run or the act, the words its time
            if (fid.Length > 0 && op.Busy == null) { var (ph, wd) = FireOne(k, fid); return ph == "blocked" ? (wd == "Too far" ? Text?.DoneWord("fire.out-of-range", "too far") ?? "too far" : Text?.DoneWord("fire.no-clear-line", "no line") ?? "no line") : wd; }
            if (op.Busy != null) return $"{ActLeftS(k)} s";
            if (op.Act != null) return WalkPlate(op.Act); // engine 0.23 walk-in (Fable less.done.walk.plate): "→ Inez"; the seconds stay on the pill
            if (op.Walk != null) return $"{WalkLeftS(k)} s";
            if (DonePlate(k) is { Length: > 0 } dw) return dw; // brief 043: done, turned, not yet, refused, reported, cancelled (2.2 s)
            // brief 049 row 10: no "under fire" plate: the portrait's pulse ring, the status slot's cause and the ground arc say it
            if (op.Gun.StandDown >= 0 && FightOnNow && Drawn(k)) return "holding"; // X under weapons free: stood down until shot at
            return "";
        }

        /// <summary>Fable's walk-in words (less.done.walk): the plate "→ Inez", the pill "to Inez · Listen".</summary>
        string WalkPlate(Order act) => (Text?.DoneWord("walk.plate", "→ {who}") ?? "→ {who}").Replace("{who}", Text?.DoneWord("walk.who." + act.Target, Bare(Name(act.Target))) ?? Bare(Name(act.Target)));
        public string WalkLine(Order act) => (Text?.DoneWord("walk.line", "to {target} · {verb}") ?? "to {target} · {verb}").Replace("{target}", Name(act.Target)).Replace("{verb}", VerbWord(act));
        /// <summary>The plate's done word for agent k now (the plate's icon reads its tone), "" none.</summary>
        public string PlateDoneTone(int k) => DonePlate(k).Length > 0 && Game != null && Game.Ops[k].Busy == null && Game.Ops[k].Walk == null ? DoneTone(k) : "";

        /// <summary>For the range panel's readout: the fire words now (the hold's, else agent k's own fire), "" none.</summary>
        public string FireWords(int k)
        {
            var s = FireStatus(null, true); if (s != null && s.Text.Length > 0) return s.Text;
            if (Game == null || k < 0) return ""; var fs = FireNow[k]; if (!fs.Firing || fs.Target.Length == 0) return "";
            var (ph, wd) = FireOne(k, fs.Target); return ph == "fire" ? $"Firing at {FireName(fs.Target)}{(FireDownS(k) is int d && d >= 0 ? $" · down in {d} s" : "")}" : ph == "aim" ? "Aiming" : ph == "draw" ? "Drawing" : wd;
        }

        // ---------------------------------------------------------------- pause and plan

        public sealed class PlanItem { public int K; public string Label = "", Icon = ""; public List<Vector3> Path = new(); public Action Exec; }
        /// <summary>Orders given while paused with "start together": one per agent, drawn as ghosts, given in order on the tick the clock runs.</summary>
        public readonly List<PlanItem> Plan = new();
        bool flushing, wasPaused;
        public int PlanFlushed { get; private set; }
        public int PlanFlushTick { get; private set; } = -1;
        bool Queueing => HoldOn && Paused && controls.planTogether && !flushing && !ShowBriefing && Game != null && !Game.Over && Modal == ModalGate.Modal.None;

        void QueuePlan(int k, string label, string icon, List<Vector3> path, Action exec)
        {
            Plan.RemoveAll(x => x.K == k);
            if (path.Count == 0 || (path[0] - (frame.World(Game.Ops[k].Pos) + Vector3.up * 0.1f)).sqrMagnitude > 0.25f) path.Insert(0, frame.World(Game.Ops[k].Pos) + Vector3.up * 0.1f);
            Plan.Add(new PlanItem { K = k, Label = label, Icon = icon, Path = path, Exec = exec });
            Status = "Paused · orders queue";
        }

        string PlanLabel(Intent i, Target t) { var parts = HudToolkit.PillLine(this, i).Split(new[] { " · " }, StringSplitOptions.None); return parts.Length > 1 ? parts[0] + " · " + parts[1] : parts[0]; }

        /// <summary>The clock runs again: every ghost is given now, on this one tick, in the order given.</summary>
        void FlushPlan()
        {
            if (Plan.Count == 0) return; var items = new List<PlanItem>(Plan); Plan.Clear(); flushing = true;
            try { foreach (var it in items) it.Exec(); } finally { flushing = false; }
            PlanFlushed += items.Count; PlanFlushTick = Game.Tick; Status = $"Started · {items.Count} order{(items.Count > 1 ? "s" : "")}";
        }
        /// <summary>Per frame, after the keys: the pause lifted gives the plan.</summary>
        void PlanFrame() { if (HoldOn && wasPaused && !Paused) FlushPlan(); wasPaused = Paused; }

        // ---------------------------------------------------------------- the portrait drag

        struct PDrag { public bool On, Drag, Add; public int K; public Vector2 At, Now; public List<int> Carry; }
        PDrag pd;
        /// <summary>The world runs at ¼ speed while a portrait is dragged and until this (unscaled s).</summary>
        public float DragSlowUntil { get; private set; } = -9f;
        float DragSlow => HoldOn && (pd.Drag || Time.unscaledTime < DragSlowUntil) ? 0.25f : 1f;
        public bool PortraitDragging => pd.On && pd.Drag;
        /// <summary>A portrait is pressed (the proof: whether the kit's PointerDown arrived).</summary>
        public bool PortraitHeld => pd.On;
        public int PortraitDragFrom => pd.K;
        public Vector2 PortraitDragAt => pd.Now;
        public IReadOnlyList<int> PortraitCarry => pd.Carry ?? (IReadOnlyList<int>)Array.Empty<int>();
        public int PortraitDrops { get; private set; }

        /// <summary>A portrait pressed (the kit's PointerDown): the hold preset waits for the release (a click selects) or a drag (an order).</summary>
        public void PortraitPress(int k, bool add)
        {
            if (!HoldOn) { PortraitClick(k, add); return; }
            var m = UnityEngine.InputSystem.Mouse.current; var at = PointerWorld is Vector3 pw && cam ? (Vector2)cam.WorldToScreenPoint(pw) : m != null ? m.position.ReadValue() : Vector2.zero;
            pd = new PDrag { On = true, K = k, Add = add, At = at, Now = at };
        }

        bool PortraitDragFrame(UnityEngine.InputSystem.Mouse mouse, Vector2 p)
        {
            if (!pd.On || mouse == null) return false; float now = Time.unscaledTime; pd.Now = p;
            if (!mouse.leftButton.isPressed)
            {
                var d = pd; pd = default;
                if (!d.Drag) { PortraitClick(d.K, d.Add); return true; }
                DragSlowUntil = now + 0.5f; PortraitDrop(d.Carry, p, controls.Held(Keyboard.current, PlayAction.AddToGroup)); return true;
            }
            if (!pd.Drag && (p - pd.At).magnitude > RingMovePx * KitScale) { pd.Drag = true; pd.Carry = Selected[pd.K] ? SelectedList() : new List<int> { pd.K }; }
            if (pd.Drag) DragSlowUntil = now + 0.5f;
            return true;
        }

        void PortraitDrop(List<int> carry, Vector2 p, bool run)
        {
            if (carry == null || carry.Count == 0 || Game == null) return;
            if (OverHud(p)) { Status = "Nothing there"; return; }
            PortraitDrops++; var t = TargetAt(p);
            if (t != null && t.Kind == "op") { Status = "Nothing there"; return; }
            if (t != null && (t.Kind == "hostile" || Combatant(t)))
            {
                var id = FireIdOf(t, out var force); var who = carry.FindAll(Armed); if (id.Length == 0 || who.Count == 0) return;
                var list = new List<Order>(); foreach (var k in who) list.Add(new Order { Op = Op.Name((OpId)k), Do = "fire", Target = id, Force = force });
                holdCommits = true; try { Fight(list, "left", p, TargetWorld(t), $"fire at {Name(id)}"); } finally { holdCommits = false; }
                LocksDrop(who); FireLocks.Add(new FireSession { Agents = who, Id = id, Name = FireName(id), Force = force, Start = Time.unscaledTime, Pin = true }); return; // a portrait lock holds against focus clicks and weapons free
            }
            if (t != null && t.Kind != "crowd")
            {
                var keep = (bool[])Selected.Clone(); var keepAgent = Agent;
                Array.Clear(Selected, 0, 4); foreach (var k in carry) Selected[k] = true; Agent = (OpId)carry[0];
                ContextPlan plan = null; Guard("panel", () => plan = ContextFor(t, p)); int way = MainWay(plan);
                if (way >= 0) { HoldGive(plan, way, t, p, run, "left", false); Array.Copy(keep, Selected, 4); Agent = keepAgent; intentKey = ""; return; }
                if (plan != null && plan.Choice.Order.Count > 0) { OpenRing(plan, p, false); if (Ring != null) { Ring.Sticky = true; Ring.Always = false; } intentKey = ""; return; } // the ring for the agents carried (they stay selected)
                Array.Copy(keep, Selected, 4); Agent = keepAgent; intentKey = "";
                PopKey("nothing-here", p, TargetWorld(t), Feedback.Info); return;
            }
            if (cam && frame.Ground(cam.ScreenPointToRay(p), out var w)) GroupWalk(frame.Engine(w), run, p, carry);
        }

        // ---------------------------------------------------------------- the commander camera: follow (C) and the eased centring

        bool camFollow; Vector3? centreGoal;
        public bool CamFollowing => camFollow;
        void HandMoved() { centreGoal = null; if (camFollow) { camFollow = false; Status = "Camera free"; } }
        void CamFollowFrame()
        {
            if (!HoldOn || Following || !cam || Game == null) return;
            Vector3? goal = centreGoal;
            if (camFollow) { int k = Array.IndexOf(Selected, true); if (k < 0) k = (int)Agent; goal = views?.Find("op/" + Op.Name((OpId)k)) is Transform t && t.gameObject.activeSelf ? t.position : frame.World(Game.Ops[k].Pos); }
            if (!goal.HasValue) return;
            var g = new Vector3(goal.Value.x, 0f, goal.Value.z); var f = Vector3.Lerp(focus, g, 1f - Mathf.Exp(-Mathf.Min(Time.unscaledDeltaTime, 0.1f) / 0.15f));
            focus = f; TurnLookAt(f); PlaceCamera();
            if (!camFollow && (f - g).magnitude < 0.05f) centreGoal = null;
        }

        // ---------------------------------------------------------------- keys

        /// <summary>The hold preset's keys: 1–4 select (Shift or Ctrl/Cmd toggles; twice quickly centres), 5 or Ctrl/Cmd+A all four, C the camera
        /// follows the lead, X stops every selected order (and its fire), Tab steps through the things under the cursor.</summary>
        void HoldKeys(Keyboard kb, bool shift)
        {
            if (kb == null) return; var c = controls; bool mod = c.Held(kb, PlayAction.Force) || kb.leftCommandKey.isPressed || kb.rightCommandKey.isPressed;
            for (int i = 0; i < 4; i++) if (c.Down(kb, PlayAction.Agent1 + i) && Ring == null) { if (Driving) Select((OpId)i, false); else AgentKey(i, shift || mod); }
            if ((c.Down(kb, PlayAction.SelectAll) || mod && kb.aKey.wasPressedThisFrame) && Ring == null && !Driving) SelectAll();
            if (c.Down(kb, PlayAction.Cancel)) { camFollow = !camFollow; centreGoal = null; Status = camFollow ? "Camera follows" : "Camera free"; }
            if (c.Down(kb, PlayAction.Cease)) HoldStop();
            if (c.Down(kb, PlayAction.CycleAgent) && Ring == null) HoldTab();
            if (c.Down(kb, PlayAction.Gait)) ToggleFree(); // brief 041 (Sky): G toggles weapons free (a run is a double click or Shift)
            if (kb.escapeKey.wasPressedThisFrame && Charge is { Done: false }) { CancelHold(); escTaken = true; } // Esc cancels a hold (and does nothing else this frame)
        }

        public void SelectAll() { for (int k = 0; k < 4; k++) Selected[k] = !Game.Ops[k].Down; if (Array.IndexOf(Selected, true) < 0) Selected[0] = true; Agent = (OpId)Array.IndexOf(Selected, true); Picked = -1; intentKey = ""; Status = "Squad: all four"; }

        /// <summary>X: every selected agent's order stops (a walk, an act, a queued ghost) and its fire ceases.</summary>
        public void HoldStop()
        {
            if (Game == null) return;
            for (int k = 0; k < 4; k++) if (Selected[k] && (doing[k].CanCancel || pending[k] != null)) CancelAgent(k, null);
            Plan.RemoveAll(x => Selected[x.K]);
            // the fire: a lasting order ceases; under weapons free a drawn agent stands down (it picks again once shot at: engine 0.19 cease)
            var list = new List<Order>(); var who = new List<int>();
            for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k) && (Game.Ops[k].Gun.Order.Length > 0 || Game.WeaponsFree && Drawn(k))) { list.Add(new Order { Op = Op.Name((OpId)k), Do = "cease" }); who.Add(k); }
            if (list.Count > 0) { Send(list, null, null); fireStoppedAt = Time.unscaledTime; HoldStops++; }
            LocksDrop(who);
        }

        public int HoldStops { get; private set; }
        public int FreeToggles { get; private set; }
        bool escTaken;

        /// <summary>G (brief 041, tuning.md §14): weapons free on or off for the cell, a logged order ({do: free, op: cell, on}). On, drawn agents
        /// pick their own targets in a fight; off, they only return fire (Fable: "Return fire only").</summary>
        public void ToggleFree()
        {
            if (Game == null || Game.Over || Fault != null) return; bool on = !Game.WeaponsFree;
            if (Issue(new Order { Op = "cell", Do = "free", On = on }, null, null)) { FreeToggles++; Status = on ? "Weapons free" : "Return fire only"; FreeSaidAt = Time.unscaledTime; FireLogAdd($"free\t{on}"); }
        }
        /// <summary>When G was last pressed (the toggle's word shows for a moment).</summary>
        public float FreeSaidAt { get; private set; } = -9f;

        /// <summary>Third person, Shift+T: the next agent (the camera glides to it).</summary>
        void DriveNextAgent()
        {
            for (int s = 1; s <= 4; s++) { int k = (dAgent + s) % 4; var op = Game.Ops[k]; if (op.Down || op.Inside || op.InCar) continue; Select((OpId)k, false); fSwingFromPos = cam.transform.position; fSwingFromRot = cam.transform.rotation; fSwingStart = Time.unscaledTime; return; }
        }

        // ---------------------------------------------------------------- the hover: hysteresis, Tab, and the offer

        Target stick, tabT; float stickSeenAt = -9f; Vector2 tabAt;
        public int TabSteps { get; private set; }

        void HoldTab()
        {
            if (HoverStackCount < 2) return; StepStack(); tabT = stack[stackIndex % stack.Count]; tabAt = lastHover; TabSteps++; intentKey = ""; PointerHover(lastHover);
        }

        /// <summary>The playground's crowd rules: the thing the pointer is on stays while it is still under the pointer (someone walking across
        /// does not take the pill), or for 150 ms after it left; a Tab choice stays while the pointer stays within 18 px.</summary>
        Target Sticky(Target t, Vector2 screen)
        {
            float now = Time.unscaledTime;
            if (tabT != null) { if ((screen - tabAt).magnitude > 18f * KitScale || !stack.Exists(x => SameTarget(x, tabT))) tabT = null; else return tabT; }
            bool inStack = stick != null && (SameTarget(t, stick) || stack.Exists(x => SameTarget(x, stick)));
            if (inStack) stickSeenAt = now;
            if (stick != null && !SameTarget(t, stick) && (inStack || now - stickSeenAt < 0.15f)) return stick;
            stick = t != null && t.Kind != "crowd" ? t : null; stickSeenAt = now; return t;
        }

        /// <summary>The hovered thing's main calm act (the pill's offer), its plan and its doers; the stand spot's "seen there"; the Watch's reach.</summary>
        public ContextPlan HoverPlan { get; private set; }
        public int HoverWay { get; private set; } = -1;
        public readonly List<int> HoverDoers = new();
        public bool HoverSeen { get; private set; }
        /// <summary>The hover offers only a right hold's fire (no calm act there).</summary>
        public bool HoverFire { get; private set; }
        /// <summary>A Watch hovered: around which target, from which agent's reach.</summary>
        public (Target t, int k)? WatchReach { get; private set; }
        float hoverPlanAt = -9f;

        /// <summary>The right hold's offer on t: "Fire at …" with its price (down in n s, Trail a hit, Alert); null when nobody selected can fire on it.</summary>
        public Intent FireOfferFor(Target t)
        {
            var (id, force) = HoldFireId(t); if (id.Length == 0) return null;
            int fk = Driving ? dAgent : Array.FindIndex(Selected, s => s); var R = Game.R;
            var ex = FireExtra(id, force); var m = System.Text.RegularExpressions.Regex.Match(ex, @"(\d+) s");
            return new Intent { Label = force ? $"Fire at {t.Label}" : "Fire", Order = new Order { Op = Op.Name((OpId)Math.Max(0, fk)), Do = "fire", Target = id, Force = force }, Time = m.Success ? m.Groups[1].Value + " s" : "", Trail = $"T +{R.HitTrail}", Alert = $"A ≥{R.GunfireAlert}" }; // brief 049 row 3: a combatant wears its ring and the tail points at it: no noun; a civilian keeps it (the warning)
        }

        Intent HoldIntent(Target t)
        {
            HoverPlan = null; HoverWay = -1; HoverDoers.Clear(); HoverSeen = false; HoverFire = false; WatchReach = null;
            if (t == null || t.Kind == "op" || Game == null) { views?.ShowPath(null); return null; }
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, lastHover)) || plan == null) return null;
            HoverPlan = plan; hoverPlanAt = Time.unscaledTime; int way = MainWay(plan); HoverWay = way;
            if (way < 0 || FightOnNow && (t.Kind == "hostile" || Combatant(t))) // in a fight a hostile's offer is fire (the playground's inFight)
            {
                views?.ShowPath(null);
                if (!FireOffered(t)) return null; // a fire offer where fire is the thing to do (Aster 2026-10-22: security too, with its price); never a passer-by or a contact
                var fi = FireOfferFor(t); if (fi != null) HoverFire = true; return fi;
            }
            var doers = DoersOf(plan, way); HoverDoers.AddRange(doers); int k0 = doers.Count > 0 ? doers[0] : (int)Agent;
            var r = RowFor(plan, way, k0) ?? plan.Row(way); var i = r.I;
            ActWalk aw = null; Guard("walk", () => aw = ActPlace((OpId)k0, i.Order, t));
            var spot = aw?.To ?? Game.Ops[k0].Pos; SeenQuote q = null; Guard("seen", () => q = Game.SeenAt(spot)); HoverSeen = q != null && q.Seen == "yes";
            if (i.Order.Do == "watch") WatchReach = (t, k0);
            if (Ring == null && Panel == null) views?.ShowPath(aw != null ? RouteWorld(k0, aw.To) : null);
            return i;
        }
    }
}
