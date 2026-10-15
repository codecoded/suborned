using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Real-time shoot-outs at the controls (GDD v0.22.0 §10; briefs/011/motive-fights.md §2.5–2.6; Sky approved all five answers):
    /// F draws or holsters the selection · a right click on a combatant fires until down (on security with weapons drawn; Ctrl forces a
    /// civilian or contact; never Vale) · holding the right button and sweeping retargets · X ceases fire · V cycles the stance (hold →
    /// return → at will) · both buttons together is panic, once a fight is on. The first order that would make the run loud opens the
    /// going-loud card (Game.GoingLoud) at the cursor: holding the same input for the rulebook's loudHold (0.5 s) commits it, releasing or
    /// Esc backs off, and the clock keeps running. Every order is an engine order, so a session replays as played.
    /// Readability: the crosshair chip ("hits 3 in 4 · down in 5.5 s", the engine's own accuracy rule and accumulator), tracers from the
    /// engine's shot log, health pips over everyone in a fight, the downed with a carry hint, and the walk-or-run chip on empty ground
    /// (Game.QuoteMove).</summary>
    public sealed partial class HandoverHost
    {
        // ---------------------------------------------------------------- the going-loud gate

        public sealed class Gate { public LoudCard Card; public readonly List<Order> Orders = new(); public string Source = "", What = ""; public float Start; public Vector2 Gui; public Vector3? At; }
        /// <summary>The going-loud card while it is held open, else null.</summary>
        public Gate Loud { get; private set; }
        public float GateProgress => Loud == null ? 0f : Mathf.Clamp01((Time.unscaledTime - Loud.Start) / HoldSeconds);
        float HoldSeconds => Mathf.Max(0.1f, Game.R.LoudHoldTicks / (float)Game.Hz);

        /// <summary>Fight orders for the selection: if any would make the run loud, the card opens and waits for the hold on `source` (F, right,
        /// both, card); else they go at once. Only the first agent speaks its ack.</summary>
        void Fight(List<Order> orders, string source, Vector2? screen, Vector3? at, string what)
        {
            if (orders.Count == 0 || Game.Over) return;
            LoudCard loud = null;
            foreach (var o in orders)
            {
                LoudCard c = null; if (!Guard("loud", () => c = Game.GoingLoud(o))) return;
                if (c.Loud) { loud = c; break; }
            }
            if (loud != null)
            {
                Loud = new Gate { Card = loud, Source = source, What = what, Start = Time.unscaledTime, At = at, Gui = screen.HasValue ? new Vector2(screen.Value.x, (cam ? cam.pixelHeight : Screen.height) - screen.Value.y) : Mouse };
                Loud.Orders.AddRange(orders);
                Status = $"Going loud: hold {SourceName(source)} {HoldSeconds:0.0} s to commit, release to back off";
                return;
            }
            Send(orders, screen, at);
        }

        static string SourceName(string s) => s switch { "F" => "F", "right" => "the right button", "both" => "both buttons", _ => "the button" };

        /// <summary>Orders the cell takes in silence (Fable §4): a click confirmed is not a line; the strip and the badge show it.</summary>
        static readonly HashSet<string> SilentAcks = new() { "move", "run", "cancel", "stance", "cease", "holster" };

        void Send(List<Order> orders, Vector2? screen, Vector3? at)
        {
            bool said = false;
            foreach (var o in orders)
                if (Issue(o, screen, at) && !said && !SilentAcks.Contains(o.Do))
                {
                    said = true;
                    string speaker = o.Op == "cell" ? "enforcer" : o.Op;
                    if (!Beat("ack", o.Do, speaker, who: Name(o.Target), kind: Feedback.Ok) && o.Op == "cell") Beat("ack", o.Do, "fixer", kind: Feedback.Ok);
                }
        }

        /// <summary>Each frame while the card is open: the hold fills; released early (or Esc), the card closes and nothing is given.</summary>
        void GateFrame(Keyboard kb, UnityEngine.InputSystem.Mouse mouse)
        {
            if (Loud == null) return;
            bool held = Loud.Source switch
            {
                "F" => controls.Held(kb, PlayAction.Draw),
                "right" => mouse != null && mouse.rightButton.isPressed,
                "both" => mouse != null && mouse.rightButton.isPressed && mouse.leftButton.isPressed,
                _ => mouse != null && mouse.leftButton.isPressed,
            };
            if (kb != null && kb.escapeKey.wasPressedThisFrame) held = false;
            if (!held) { Status = "Going loud: backed off"; Loud = null; return; } // the card closing is the answer (Fable B9)
            if (GateProgress < 1f) return;
            var g = Loud; Loud = null;
            Send(new List<Order>(g.Orders), null, g.At);
            FeedLoud(g.Card, g.What);
            if (Text != null && Text.Events.TryGetValue("going-loud", out var e)) WorldBeat(e, null, "", Feedback.Refused, 1);
            Status = "The run is loud.";
        }

        /// <summary>The card clicked (its commit ring): the left button held on it commits like the input that opened it.</summary>
        public void GateHoldByCard() { if (Loud != null) { Loud.Source = "card"; Loud.Start = Time.unscaledTime; } }

        // ---------------------------------------------------------------- the orders

        bool Armed(int k) { var op = Game.Ops[k]; return op.Gun.W != null && !op.Down && !op.Inside; }
        bool Drawn(int k) { var g = Game.Ops[k].Gun; return g.Drawn && g.HolsterAt < 0; }

        /// <summary>F: draw the selection's weapons, or holster them if any is drawn.</summary>
        void DrawOrHolster()
        {
            var list = new List<Order>(); bool any = false;
            for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k) && Drawn(k)) any = true;
            for (int k = 0; k < 4; k++)
                if (Selected[k] && Armed(k) && Drawn(k) == any) list.Add(new Order { Op = Op.Name((OpId)k), Do = any ? "holster" : "draw" });
            if (list.Count == 0) { Pop("Nobody selected is armed.", null, SelectedWorld(), Feedback.Info); return; }
            Fight(list, "F", null, SelectedWorld(), any ? "holster" : "draw");
        }

        /// <summary>X: the selection ceases fire (their weapons stay drawn).</summary>
        void CeaseFire()
        {
            var list = new List<Order>();
            for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k) && Drawn(k)) list.Add(new Order { Op = Op.Name((OpId)k), Do = "cease" });
            if (list.Count == 0) { Pop("No weapon drawn.", null, SelectedWorld(), Feedback.Info); return; }
            Send(list, null, SelectedWorld());
        }

        static readonly string[] Stances = { "hold", "return", "free" };
        public static string StanceName(string s) => s switch { "hold" => "hold fire", "return" => "return fire", "free" => "fire at will", _ => s };

        /// <summary>V: the selection's stance moves on one (hold → return → at will), from the first armed agent's.</summary>
        void CycleStance()
        {
            int first = -1; for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k)) { first = k; break; }
            if (first < 0) { Pop("Nobody selected is armed.", null, SelectedWorld(), Feedback.Info); return; }
            var next = Stances[(Array.IndexOf(Stances, Game.Ops[first].Gun.Stance) + 1) % 3];
            var list = new List<Order>();
            for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k)) list.Add(new Order { Op = Op.Name((OpId)k), Do = "stance", Stance = next });
            Send(list, null, SelectedWorld());
            Pop($"stance: {StanceName(next)}", null, SelectedWorld(), Feedback.Info);
        }

        /// <summary>Both buttons: panic (every armed agent draws and fires at will), only once a fight is on. Outside a fight the gesture is not
        /// read at all (Controls), so nothing is sent, refused or said.</summary>
        void Panic(Vector2 screen)
        {
            if (!Game.FightOn) return;
            if (Panel != null) ClosePanel();
            Send(new List<Order> { new Order { Op = "cell", Do = "panic" } }, screen, null);
        }

        /// <summary>What a right click would fire on, for the selection: a final-fight hostile always; security (guard, patrol, the rival crew,
        /// the response unit) when a selected weapon is drawn or Ctrl is held; a civilian or contact only with Ctrl; Vale never.</summary>
        bool ShotAt(Target t, bool ctrl, out string id, out bool force)
        {
            id = ""; force = false;
            if (t == null) return false;
            bool armed = false, drawn = false;
            for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k)) { armed = true; drawn |= Drawn(k); }
            if (!armed) return false;
            switch (t.Kind)
            {
                case "hostile": id = t.Key; return true;
                case "patrol": case "rivals": case "security": if (!(drawn || ctrl)) return false; id = t.Kind; return true;
                case "person":
                    if (t.Key == "councillor") return false;
                    if (t.Key == "guard") { if (!(drawn || ctrl)) return false; id = "guard"; return true; }
                    if (!ctrl) return false;
                    id = t.Key; force = true; return true;
            }
            return false;
        }

        /// <summary>The selection fires on `id` until it is down (each agent its own fire order).</summary>
        void FireSelected(string id, bool force, Vector2? screen, string source)
        {
            // only agents in range with a clear line are given the order (the card is for a shot that would happen); none, the reason shows
            var list = new List<Order>(); string why = "";
            var at = Game.PosOf(id == "security" ? FirstOfficer() : id);
            for (int k = 0; k < 4; k++)
            {
                if (!Selected[k] || !Armed(k)) continue;
                var op = Game.Ops[k];
                if (at.HasValue && !G.Within(op.Pos, at.Value, op.Gun.W.MaxCm)) { why = $"out of range ({G.Isqrt(G.D2(op.Pos, at.Value)) / 100} m, max {op.Gun.W.MaxCm / 100} m): walk closer"; continue; }
                if (at.HasValue && !Game.S.Sight(op.Pos, at.Value, Game.R.Eye)) { why = "no clear line"; continue; }
                list.Add(new Order { Op = Op.Name((OpId)k), Do = "fire", Target = id, Force = force });
            }
            if (list.Count == 0) { Pop(why.Length > 0 ? why : "nobody selected can fire", screen, WorldOf(id), Feedback.Refused); return; }
            sweepLast = id;
            Fight(list, source, screen, null, $"fire on {Name(id)}");
        }

        bool sweeping; string sweepLast = "";
        string FirstOfficer() { foreach (var s in Game.SecGuns) if (Game.PosOf(s.Id).HasValue) return s.Id; return "security"; }

        /// <summary>The right button held after a fire order: sweeping onto another combatant moves the selection's fire to it (release leaves
        /// the last order standing; empty ground is never fired on).</summary>
        void Sweep(Vector2 screen, bool held)
        {
            if (!held) { sweeping = false; return; }
            if (!sweeping || Loud != null) return;
            var t = TargetAt(screen);
            if (t == null || t.Kind == "person" || !ShotAt(t, false, out var id, out _) || id == sweepLast) return;
            FireSelected(id, false, screen, "right");
        }

        Vector3? SelectedWorld() { int f = Array.IndexOf(Selected, true); return f >= 0 ? frame.World(Game.Ops[f].Pos) : (Vector3?)null; }

        /// <summary>A downed agent right clicked: the first selected agent who is up walks to them and lifts them (carry; walk them to the car).</summary>
        bool CarryDowned(Target t, Vector2 screen)
        {
            var down = Op.Parse(t.Key); if (!Game.Ops[(int)down].Down) return false;
            for (int k = 0; k < 4; k++)
            {
                if (!Selected[k] || k == (int)down || Game.Ops[k].Down || Game.Ops[k].Inside) continue;
                Give((OpId)k, new Order { Op = Op.Name((OpId)k), Do = "carry", Target = t.Key }, t, screen);
                return true;
            }
            Pop($"Another agent has to carry the {Cap(t.Key)}.", screen, null, Feedback.Info);
            return true;
        }

        // ---------------------------------------------------------------- the crosshair (the engine's accuracy, its accumulator)

        /// <summary>The crosshair chip for the hovered target, or null: "guard · hits 3 in 4 · down in 5.5 s · +2 T hit · +2 T down".</summary>
        public string AimLine { get; private set; }
        public bool AimNoLine { get; private set; }

        void UpdateAim()
        {
            AimLine = null; AimNoLine = false;
            var t = Hover; if (t == null || Game == null || Game.Over) return;
            bool ctrl = Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
            if (t.Kind == "person" && t.Key == "councillor") { if (ctrl || AnyDrawn()) AimLine = "Vale · never: shooting him fails the mission"; return; }
            if (!ShotAt(t, ctrl, out var id, out var force)) return;
            int k = -1; for (int i = 0; i < 4; i++) if (Selected[i] && Armed(i)) { k = i; break; }
            if (k < 0) return;
            Guard("aim", () => AimLine = Aim(k, id, force, t));
        }

        bool AnyDrawn() { for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k) && Drawn(k)) return true; return false; }

        string Aim(int k, string target, bool force, Target t)
        {
            var g = Game; var R = g.R; var op = g.Ops[k]; var gun = op.Gun; var w = gun.W;
            string id = target;
            if (target is "security") { foreach (var s in g.SecGuns) if (g.PosOf(s.Id).HasValue) { id = s.Id; break; } }
            var tg = g.GunOf(id); var at = g.PosOf(id);
            string who = force ? $"CIVILIAN · {Name(id)}" : Cap(Name(id));
            if (tg == null || !at.HasValue || tg.Down) return $"{who} · not there to fire at";
            long d = G.Isqrt(G.D2(op.Pos, at.Value));
            if (d > w.MaxCm) return $"{who} · out of range ({d / 100} m, max {w.MaxCm / 100} m)";
            if (!g.S.Sight(op.Pos, at.Value, R.Eye)) { AimNoLine = true; return $"{who} · ✕ no clear line"; }
            bool moving = op.Walk != null, targetMoving = false;
            foreach (var h in g.Hostiles) if (h.Id == id) targetMoving = h.Moving(g.Tick);
            int cover = targetMoving ? 1000 : CoverRules.Modifier(g.S.Cover, at.Value, op.Pos, g.Tick / Game.Hz, R.CoverSnapCm, R.PointBlankCm, R.CoverQFull, R.CoverQPartial, R.ModCover, R.ModCoverPartial);
            bool vale = op.Id == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer;
            int acc = R.Accuracy(w, d, moving, cover, vale, !gun.Trained);
            if (acc <= 0) return $"{who} · can't hit from here";
            // the time to down: the first shot (a draw, then aimNew on a new target), then one every W.Every; hits counted by the accumulator
            int need = Math.Max(1, (tg.Health + w.Damage - 1) / w.Damage), first;
            int a0 = gun.Target == id ? gun.Acc : R.AccStart;
            if (!(gun.Drawn && gun.HolsterAt < 0)) first = R.DrawTicks + R.AimNewTicks;
            else if (gun.Target != id) first = Math.Max(Math.Max(0, gun.NextShot - g.Tick), Math.Max(0, gun.DrawReady - g.Tick) + R.AimNewTicks);
            else first = Math.Max(0, gun.NextShot - g.Tick);
            int hits = 0, shots = 0, accum = a0;
            while (hits < need && shots < 500) { shots++; accum += acc; if (accum >= 1000) { accum -= 1000; hits++; } }
            int ticks = first + (shots - 1) * w.Every;
            string band = d <= R.PointBlankCm ? "point blank" : d <= w.EffCm ? "effective" : "long";
            string cost = g.FightBranch.Length > 0 && !R.FightTraced && g.Hostiles.Exists(h => h.Id == id) ? "untraced" : g.Gunfire > 0 ? $"+{R.HitTrail} T hit · +{R.DownTrail} T down" : $"+{R.GunfireTrail} T shooting · +{R.HitTrail} T hit · +{R.DownTrail} T down";
            return $"{who} · hits {Ratio(acc)} · down in {ticks / Game.Hz}.{ticks % Game.Hz} s · {d / 100} m {band}{(cover < 1000 ? " · in cover" : "")} · {cost}";
        }

        /// <summary>A per-mille accuracy as the nearest small fraction: 750 → "3 in 4", 1000 → "every shot".</summary>
        static string Ratio(int acc)
        {
            if (acc >= 995) return "every shot";
            int bn = 1, bd = 1; float best = 9f;
            for (int d = 2; d <= 10; d++) { int n = Mathf.RoundToInt(acc * d / 1000f); if (n < 1) continue; float e = Mathf.Abs(n / (float)d - acc / 1000f); if (e < best - 0.004f) { best = e; bn = n; bd = d; } }
            return $"{bn} in {bd}";
        }

        // ---------------------------------------------------------------- the walk-or-run chip on empty ground

        /// <summary>Over empty ground with an agent selected: walk and run times there and who would see the run (Game.QuoteMove), else null.</summary>
        public string MoveChip { get; private set; }
        Pt moveChipAt = new(int.MinValue, int.MinValue); float moveChipTime; bool moveChipDirty;

        void UpdateMoveChip(Vector2 screen, bool force)
        {
            if (Hover != null || Panel != null || Game.Over || !cam) { MoveChip = null; return; }
            int k = Array.IndexOf(Selected, true); if (k < 0 || Game.Ops[k].Inside || Game.Ops[k].Down) { MoveChip = null; return; }
            if (!frame.Ground(cam.ScreenPointToRay(screen), out var w)) { MoveChip = null; return; }
            var at = frame.Engine(w); var cell = new Pt(at.X / 100 * 100, at.Y / 100 * 100);
            if (!force && cell == moveChipAt) return;
            if (Time.unscaledTime - moveChipTime < 0.2f && !force) { moveChipDirty = true; return; } // at most five quotes a second (an A* and a sweep each)
            moveChipAt = cell; moveChipTime = Time.unscaledTime; moveChipDirty = false;
            Pt? to = Game.S.Grid.Free(at) ? at : Game.S.Grid.NearestFree(at);
            if (!to.HasValue || G.Dist(to.Value, at) > 300) { MoveChip = "can't walk there"; return; }
            MoveQuote q = null; if (!Guard("move quote", () => q = Game.QuoteMove(Op.Name((OpId)k), to.Value))) return;
            if (q.WalkS == 0 && q.RunS == 0 && q.RunRefused.Length > 0) { MoveChip = q.RunRefused == "no walk there" ? "can't walk there" : q.RunRefused; return; }
            string run = q.RunRefused.Length > 0 ? $"can't run: {q.RunRefused}" : q.RunFree ? $"run {q.RunS} s (free: nobody counts a runner now)"
                : $"run {q.RunS} s{(q.RunSeenBy.Count > 0 ? $" · seen by {string.Join(", ", q.RunSeenBy)} (Alert {q.AlertIfRun})" : " · unseen by security")}{(q.RunPeople > 0 ? $" · {q.RunPeople} people near" : "")}";
            MoveChip = $"{AgentNames[k]}: walk {q.WalkS} s · {run}";
        }

        // ---------------------------------------------------------------- shots, read from the engine's log

        /// <summary>A shot in the log ("{by} fires on {t} at {d} m ({acc} ‰): hit."): a tracer and a flinch or a puff.</summary>
        void Shot(LogLine l)
        {
            int f = l.Text.IndexOf(" fires on ", StringComparison.Ordinal), a = l.Text.IndexOf(" at ", f + 1, StringComparison.Ordinal);
            if (f <= 0 || a <= f) return;
            string by = l.Text[..f], t = l.Text[(f + 10)..a];
            var from = WorldOf(by); var to = WorldOf(t); if (!from.HasValue || !to.HasValue) return;
            bool hit = l.Code == "shot.hit";
            var src = from.Value + Vector3.up * 1.3f; var dst = to.Value + Vector3.up * 1.2f;
            if (!hit) // a miss runs past and ends on the ground beside the target, never on the body
            {
                var dir = (dst - src); dir.y = 0; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
                var side = Vector3.Cross(Vector3.up, dir) * (((l.Tick + by.Length) & 1) == 0 ? 0.7f : -0.7f);
                dst = to.Value + dir * 1.8f + side; dst.y = to.Value.y + 0.05f;
            }
            var colour = by is "scout" or "fixer" or "tech" or "enforcer" ? cellHeadColour : by.StartsWith("rival") ? rivalColour : guardColour;
            views.Tracer(src, dst, colour);
            if (hit) { Pulses.Add((to.Value, Feedback.Refused, Time.unscaledTime)); while (Pulses.Count > 12) Pulses.RemoveAt(0); } // a miss: the tracer is enough
        }

        /// <summary>Where a shooter or target stands in the world: the engine's position, else (down, gone) its view's last.</summary>
        public Vector3? WorldOf(string id)
        {
            Pt? p = null; Guard("pos", () => p = Game.PosOf(id));
            foreach (var h in Game.Hostiles) if (h.Id == id) p = h.Pos(Game.Tick);
            if (p.HasValue) return frame.World(p.Value);
            var tr = views.Find(ViewKey(id)); return tr ? tr.position : (Vector3?)null;
        }

        /// <summary>The view an engine id draws as (an agent, a cast person, the patrol, the rivals, an officer, a fight hostile).</summary>
        public string ViewKey(string id)
        {
            if (id is "scout" or "fixer" or "tech" or "enforcer") return "op/" + id;
            if (id == "rivals") return "rivals/0";
            for (int i = 0; i < Game.Hostiles.Count; i++) if (Game.Hostiles[i].Id == id) return "hostile/" + i;
            for (int i = 0; i < Game.SecGuns.Length; i++) if (Game.SecGuns[i].Id == id) return "security/" + Math.Min(i, 1);
            return id;
        }

        /// <summary>Everyone the HUD puts health pips over: the cell when a fight is on, drawn or hurt; and anyone with a gun who has fired,
        /// is aiming or is a fight hostile.</summary>
        public IEnumerable<(string id, Gun gun)> Fighters()
        {
            var g = Game; bool fight = g.FightOn;
            for (int k = 0; k < 4; k++) { var gun = g.Ops[k].Gun; if (gun.W != null && (fight || gun.Drawn || gun.Health < gun.Max || g.Ops[k].Down)) yield return (Op.Name((OpId)k), gun); }
            foreach (var h in g.Hostiles) if (h.Present && !h.Down) yield return (h.Id, h.Gun);
            foreach (var id in new[] { "guard", "patrol", "rivals" }) { var gun = g.GunOf(id); if (gun != null && !gun.Down && (gun.Fired || gun.Engaged || gun.Health < gun.Max || (fight && id == "guard" && g.GuardAlly))) yield return (id, gun); }
            foreach (var gun in g.SecGuns) if (!gun.Down && (gun.Fired || gun.Engaged) && g.PosOf(gun.Id).HasValue) yield return (gun.Id, gun);
        }
    }
}
