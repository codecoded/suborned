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

        public sealed class Gate { public LoudCard Card; public readonly List<Order> Orders = new(); public string Source = "", What = ""; public float Start; public Vector2 Gui; public Vector3? At; /** brief 013: a takedown a known watcher sees */ public bool Takedown; public Target Target; public OpId By; /** brief 018: a Go that leaves someone behind (who, fate, why), and whether security sees the car leave */ public List<(string op, string fate, string why)> Left; public bool SeenLeaving; /** brief 038 §5: a hold that is not going loud (panic in a fight): no loud feed line on commit */ public bool Plain; }
        /// <summary>The going-loud card while it is held open, else null.</summary>
        public Gate Loud { get; private set; }
        /// <summary>For scripted checks: the open hold card closes, nothing given.</summary>
        public void SetLoudNull() => Loud = null;
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
                if (c.Loud || c.Hold) { loud = c; break; } // engine 0.15.0: a shot that would fail the mission always takes the hold (LoudCard.Hold)
            }
            if (loud != null && holdCommits && loud.Fails.Length == 0) { Send(orders, screen, at); if (loud.Loud) FeedLoud(loud, what); return; } // a held harmful wedge: its 0.5 s was the card's hold
            if (loud != null)
            {
                FreezeOnLoud(); // brief 027: freeze mode opens a freeze first; the card shows inside it and the queue holds the order
                Loud = new Gate { Card = loud, Source = source, What = what, Start = Time.unscaledTime, At = at, Gui = screen.HasValue ? new Vector2(screen.Value.x, (cam ? cam.pixelHeight : Screen.height) - screen.Value.y) : Mouse };
                Loud.Orders.AddRange(orders);
                Status = $"Going loud: hold {SourceName(source)} {HoldSeconds:0.0} s to commit, release to back off";
                return;
            }
            Send(orders, screen, at);
        }

        static string SourceName(string s) => s switch { "F" => "F", "V" => "V", "right" => "the right button", "both" => "both buttons", _ => "the button" };

        /// <summary>Orders the cell takes in silence (Fable §4): a click confirmed is not a line; the strip and the badge show it.</summary>
        static readonly HashSet<string> SilentAcks = new() { "move", "run", "cancel", "stance", "cease", "holster" };

        void Send(List<Order> orders, Vector2? screen, Vector3? at)
        {
            bool said = false;
            foreach (var o in orders)
            {
                // Sky 2026-10-21: a fire order stops a walking agent (the engine fires on the move; the walk and the act it was for are dropped
                // with the walk-only cancel, an order in the log). A running agent is left to the engine (Motive: running fire, its penalty), and a
                // walk into range the fire itself asked for (weapons.close) is the fire's own
                int fk = o.Do == "fire" && IsOpName(o.Op) ? (int)Op.Parse(o.Op) : -1;
                bool walked = fk >= 0 && Game.Ops[fk].Walk != null && !Game.Ops[fk].Running && !Game.Ops[fk].Driven;
                bool ok = Issue(o, screen, at);
                if (ok && walked && Game.Ops[fk].Walk != null && !Game.Ops[fk].CloseWalk && !Game.Ops[fk].Running) { CancelAgent(fk, null); FireStopped++; }
                if (ok && !said && !SilentAcks.Contains(o.Do))
                {
                    said = true;
                    string speaker = o.Op == "cell" ? "enforcer" : o.Op;
                    if (!Beat("ack", o.Do, speaker, who: Name(o.Target), kind: Feedback.Ok) && o.Op == "cell") Beat("ack", o.Do, "fixer", kind: Feedback.Ok);
                }
            }
        }

        /// <summary>For checks: walks a fire order has stopped this run.</summary>
        public int FireStopped { get; private set; }
        /// <summary>For scripted checks: the selection fires on `id` as a right click gives it (the going-loud hold committed at once).</summary>
        public void FireScripted(string id, bool force = false)
        {
            FireSelected(id, force, null, "right");
            if (Loud is Gate g && g.Left == null && !g.Takedown) { Loud = null; Send(new List<Order>(g.Orders), null, g.At); FeedLoud(g.Card, g.What); }
        }

        /// <summary>Each frame while the card is open: the hold fills; released early (or Esc), the card closes and nothing is given.</summary>
        void GateFrame(Keyboard kb, UnityEngine.InputSystem.Mouse mouse)
        {
            if (Loud == null) return;
            bool held = Loud.Source switch
            {
                "F" => controls.Held(kb, PlayAction.Draw),
                "V" => controls.Held(kb, PlayAction.Stance), // brief 027: V is panic with the lab
                "right" => mouse != null && mouse.rightButton.isPressed,
                "E" => controls.Held(kb, PlayAction.Act), // brief 030: third person, E fired on the focus (the act key)
                "both" => mouse != null && mouse.rightButton.isPressed && mouse.leftButton.isPressed,
                _ => mouse != null && mouse.leftButton.isPressed,
            };
            if (kb != null && kb.escapeKey.wasPressedThisFrame) held = false;
            if (!held) { Status = "Going loud: backed off"; Loud = null; return; } // the card closing is the answer (Fable B9)
            if (GateProgress < 1f) return;
            var g = Loud; Loud = null;
            if (g.Takedown) { Give(g.By, g.Orders[0], g.Target, null); Status = "Committed: the takedown is seen."; return; } // walk-then-act like any order
            if (g.Left != null) { Issue(g.Orders[0], null, g.At); Status = "Go: the car leaves."; return; } // brief 018: the Go that leaves someone behind
            Send(new List<Order>(g.Orders), null, g.At);
            if (g.Plain) { Status = "Panic."; return; }
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
            if (list.Count == 0) { PopKey("unarmed", null, SelectedWorld(), Feedback.Info, "Nobody selected is armed."); return; }
            Fight(list, "F", null, SelectedWorld(), any ? "holster" : "draw");
        }

        /// <summary>X: the selection ceases fire (their weapons stay drawn).</summary>
        void CeaseFire()
        {
            if (Game.Lab) // brief 027 (stances two): X is hold (it also ends the fire order and refunds a volley)
            {
                var hold = new List<Order>();
                for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k)) hold.Add(new Order { Op = Op.Name((OpId)k), Do = "stance", Stance = "hold" });
                if (hold.Count == 0) { PopKey("unarmed", null, SelectedWorld(), Feedback.Info, "Nobody selected is armed."); return; }
                Send(hold, null, SelectedWorld()); PopKey("stance-hold", null, SelectedWorld(), Feedback.Info, "stance: hold fire"); return;
            }
            var list = new List<Order>();
            for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k) && Drawn(k)) list.Add(new Order { Op = Op.Name((OpId)k), Do = "cease" });
            if (list.Count == 0) { PopKey("not-drawn", null, SelectedWorld(), Feedback.Info, "No weapon drawn."); return; }
            Send(list, null, SelectedWorld());
        }

        static readonly string[] Stances = { "hold", "return", "free" };
        public static string StanceName(string s) => s switch { "hold" => "hold fire", "return" => "return fire", "free" => "fire at will", _ => s };

        /// <summary>V: the selection's stance moves on one (hold → return → at will), from the first armed agent's.</summary>
        void CycleStance()
        {
            if (Game.Lab) { Panic(UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? Vector2.zero, "V"); return; } // brief 027: with the lab, V is panic (allowed outside a fight, gated by the going-loud card)
            int first = -1; for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k)) { first = k; break; }
            if (first < 0) { PopKey("unarmed", null, SelectedWorld(), Feedback.Info, "Nobody selected is armed."); return; }
            var next = Stances[(Array.IndexOf(Stances, Game.Ops[first].Gun.Stance) + 1) % 3];
            var list = new List<Order>();
            for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k)) list.Add(new Order { Op = Op.Name((OpId)k), Do = "stance", Stance = next });
            Send(list, null, SelectedWorld());
            PopKey("stance-" + next, null, SelectedWorld(), Feedback.Info, $"stance: {StanceName(next)}");
        }

        /// <summary>Both buttons: panic (every armed agent draws and fires at will), only once a fight is on. Outside a fight the gesture is not
        /// read at all (Controls), so nothing is sent, refused or said.</summary>
        void Panic(Vector2 screen, string source = "both")
        {
            if (!Game.FightOn && !Game.Lab) return;
            if (Panel != null) ClosePanel();
            if (Game.Lab) { Fight(new List<Order> { new Order { Op = "cell", Do = "panic" } }, source, screen, null, "panic"); return; } // brief 027: gated by GoingLoud
            // brief 038 §5: the one one-input irreversible order takes the going-loud card's 0.5 s hold (both buttons kept down), loud or not
            LoudCard c = null; Guard("loud", () => c = Game.GoingLoud(new Order { Op = "cell", Do = "panic" })); if (c == null) return;
            if (c.Refused.Length > 0) { Pop(c.Refused, screen, null, Feedback.Refused, 2f); return; }
            Loud = new Gate { Card = c, Source = source, What = "panic", Start = Time.unscaledTime, Gui = new Vector2(screen.x, (cam ? cam.pixelHeight : Screen.height) - screen.y), Plain = !c.Loud };
            Loud.Orders.Add(new Order { Op = "cell", Do = "panic" });
            Status = $"Panic: hold {SourceName(source)} {HoldSeconds:0.0} s";
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
                case "bystander": if (!ctrl) return false; id = t.Key; force = true; return true; // engine 0.18: a range bystander, only with Ctrl held
                case "patrol": case "rivals": case "security": if (!(drawn || ctrl)) return false; id = t.Kind; return true;
                case "unit": if (!(drawn || ctrl || Game.FightOn)) return false; id = t.Key; return true; // Keel 0.15.0 host notes: a unit is fired on by its own id (police-1, robot-1, …)
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
                bool walkIn = Driving && k == dAgent && at.HasValue && (!G.Within(op.Pos, at.Value, op.Gun.W.MaxCm) || !Game.S.Sight(op.Pos, at.Value, Game.R.Eye)); // Sky 2026-10-20: driven and out of reach, the lab's close walks it in
                if (walkIn) { DriveAutoBegin(); list.Add(new Order { Op = Op.Name((OpId)k), Do = "fire", Target = id, Force = force }); continue; }
                if (at.HasValue && !G.Within(op.Pos, at.Value, op.Gun.W.MaxCm)) { why = $"out of range ({G.Isqrt(G.D2(op.Pos, at.Value)) / 100} m, max {op.Gun.W.MaxCm / 100} m): walk closer"; continue; }
                if (at.HasValue && !Game.S.Sight(op.Pos, at.Value, Game.R.Eye)) { why = "no clear line"; continue; }
                list.Add(new Order { Op = Op.Name((OpId)k), Do = "fire", Target = id, Force = force });
            }
            if (list.Count == 0) { PopKey(why == "no clear line" ? "no-line" : why.Length > 0 ? "out-of-range" : "unarmed", screen, WorldOf(id), Feedback.Refused, why.Length > 0 ? why : "nobody selected can fire", System.Text.RegularExpressions.Regex.Match(why, @"\d+").Value); return; }
            sweepLast = id;
            Fight(list, source, screen, null, $"fire on {Name(id)}");
        }

        /// <summary>A Fire way taken from the ring or the panel (one agent, its order as the row has it, Force for a person): the going-loud card
        /// when it would go loud or fail the mission (a held harmful wedge has done that hold: it goes at once); driven and out of reach, the
        /// engine walks the agent in (both presets since engine 0.15.0).</summary>
        void FireRow(OpId by, Order o, Target t, Vector2? screen, string source)
        {
            int k = (int)by; if (!Armed(k)) { PopKey("unarmed", screen, TargetWorld(t), Feedback.Info, "Unarmed."); return; }
            var at = Game.PosOf(o.Target); var op = Game.Ops[k];
            if (Driving && k == dAgent && at.HasValue && (!G.Within(op.Pos, at.Value, op.Gun.W.MaxCm) || !Game.S.Sight(op.Pos, at.Value, Game.R.Eye))) DriveAutoBegin();
            var fo = new Order { Op = Op.Name(by), Do = "fire", Target = o.Target, Force = o.Force };
            sweepLast = o.Target;
            Fight(new List<Order> { fo }, source, screen, TargetWorld(t), $"fire on {Name(o.Target)}");
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
            PopKey("other-carries", screen, null, Feedback.Info, $"Another agent has to carry the {Cap(t.Key)}.");
            return true;
        }

        // ---------------------------------------------------------------- the crosshair (the engine's accuracy, its accumulator)

        /// <summary>The crosshair chip for the hovered target, or null: "guard · hits 3 in 4 · down in 5.5 s · +2 T hit · +2 T down".</summary>
        public string AimLine { get; private set; }
        public bool AimNoLine { get; private set; }

        void UpdateAim()
        {
            AimLine = null; AimNoLine = false; AimNow = null; LabChunk = null; LabFire = null; LabFireAgent = -1; if (!ModalGate.AllowsHover(Modal)) return;
            var t = Hover; if (t == null || Game == null || Game.Over) return;
            bool ctrl = Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
            if (t.Kind == "person" && t.Key == "councillor") { if (ctrl || AnyDrawn()) { AimLine = "Vale · never: shooting him fails the mission"; AimNow = new AimParts { Badge = "badges/npc_councillor", Refused = Text != null && Text.Pops.TryGetValue("never-vale", out var nv) ? nv : "Never Vale" }; } return; }
            if (!ShotAt(t, ctrl, out var id, out var force)) return;
            int k = -1; for (int i = 0; i < 4; i++) if (Selected[i] && Armed(i)) { k = i; break; }
            if (k < 0) return;
            Guard("aim", () => AimLine = Aim(k, id, force, t));
        }

        /// <summary>The time to down `id` from agent k at accuracy `acc` (the aim chip's number): the first shot (a draw, then aimNew on a new
        /// target), then one every W.Every; hits counted by the accumulator. Read only.</summary>
        int DownTicks(int k, string id, Gun tg, int acc)
        {
            var g = Game; var R = g.R; var gun = g.Ops[k].Gun; var w = gun.W;
            int need = Math.Max(1, (tg.Health + w.Damage - 1) / w.Damage), first;
            int a0 = gun.Target == id ? gun.Acc : R.AccStart;
            if (!(gun.Drawn && gun.HolsterAt < 0)) first = R.DrawTicks + R.AimNewTicks;
            else if (gun.Target != id) first = Math.Max(Math.Max(0, gun.NextShot - g.Tick), Math.Max(0, gun.DrawReady - g.Tick) + R.AimNewTicks);
            else first = Math.Max(0, gun.NextShot - g.Tick);
            int hits = 0, shots = 0, accum = a0;
            while (hits < need && shots < 500) { shots++; accum += acc; if (accum >= 1000) { accum -= 1000; hits++; } }
            return first + (shots - 1) * w.Every;
        }

        /// <summary>An agent firing: whole seconds until its target is down at the present odds (the timer chip's number), or -1 (no line,
        /// out of range, nothing there). Glyph fight r1 #6: the chip had a bar and no number.</summary>
        public int FireDownS(int k)
        {
            var g = Game; if (g == null) return -1; var R = g.R; var op = g.Ops[k]; var gun = op.Gun; var w = gun.W;
            string id = gun.Order.Length > 0 ? gun.Order : gun.Target; if (w == null || id.Length == 0) return -1; /* the standing fire order's target (Target is the aim, empty between shots) */ var tg = g.GunOf(id); var at = g.PosOf(id); if (tg == null || !at.HasValue || tg.Down) return -1;
            long d = G.Isqrt(G.D2(op.Pos, at.Value)); if (d > w.MaxCm || !g.S.Sight(op.Pos, at.Value, R.Eye)) return -1;
            bool targetMoving = false; foreach (var h in g.Hostiles) if (h.Id == id) targetMoving = h.Moving(g.Tick);
            int cover = targetMoving ? 1000 : CoverRules.Modifier(g.Range ? g.CoverNow() : g.S.Cover, at.Value, op.Pos, g.Tick / Game.Hz, R.CoverSnapCm, R.PointBlankCm, R.CoverQFull, R.CoverQPartial, R.ModCover, R.ModCoverPartial);
            int acc = R.Accuracy(w, d, op.Walk != null, cover, op.Id == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer, !gun.Trained); if (acc <= 0) return -1;
            int ticks = DownTicks(k, id, tg, acc); return (ticks + Game.Hz - 1) / Game.Hz;
        }

        bool AnyDrawn() { for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k) && Drawn(k)) return true; return false; }

        string Aim(int k, string target, bool force, Target t)
        {
            var g = Game; var R = g.R; var op = g.Ops[k]; var gun = op.Gun; var w = gun.W;
            string id = target;
            if (target is "security") { foreach (var s in g.SecGuns) if (g.PosOf(s.Id).HasValue) { id = s.Id; break; } }
            if (g.Lab) { var lp = AimNow = new AimParts { Civilian = force, Badge = t.Kind == "person" ? t.Key switch { "waitress" => "badges/npc_waitress", "driver" => "badges/npc_driver", "guard" => "badges/signal_guard", _ => "badges/npc_passer" } : id.StartsWith("rival") || t.Kind == "rivals" ? "badges/signal_rival" : "badges/signal_police" }; return LabAim(k, id, force, t, lp); } // brief 027 B1: PreviewFire
            var tg = g.GunOf(id); var at = g.PosOf(id);
            string who = force ? $"CIVILIAN · {Name(id)}" : Cap(Name(id));
            var parts = AimNow = new AimParts { Civilian = force, Badge = t.Kind == "person" ? t.Key switch { "waitress" => "badges/npc_waitress", "driver" => "badges/npc_driver", "guard" => "badges/signal_guard", _ => "badges/npc_passer" } : id.StartsWith("rival") || t.Kind == "rivals" ? "badges/signal_rival" : "badges/signal_police" };
            string P(string k, string d0) => Text != null && Text.Pops.TryGetValue(k, out var v) ? v : d0;
            if (tg == null || !at.HasValue || tg.Down) { parts.Refused = P("nothing-there", "Nothing there"); return $"{who} · not there to fire at"; }
            long d = G.Isqrt(G.D2(op.Pos, at.Value));
            if (d > w.MaxCm) { parts.Refused = (Text?.ShortLock("out-of-range") ?? "Out of range · {m} m").Replace("{m}", (d / 100).ToString()); return $"{who} · out of range ({d / 100} m, max {w.MaxCm / 100} m)"; }
            if (!g.S.Sight(op.Pos, at.Value, R.Eye)) { AimNoLine = true; parts.Refused = P("no-line", "No clear line"); return $"{who} · ✕ no clear line"; }
            bool moving = op.Walk != null, targetMoving = false;
            foreach (var h in g.Hostiles) if (h.Id == id) targetMoving = h.Moving(g.Tick);
            int cover = targetMoving ? 1000 : CoverRules.Modifier(g.Range ? g.CoverNow() : g.S.Cover, at.Value, op.Pos, g.Tick / Game.Hz, R.CoverSnapCm, R.PointBlankCm, R.CoverQFull, R.CoverQPartial, R.ModCover, R.ModCoverPartial);
            bool vale = op.Id == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer;
            int acc = R.Accuracy(w, d, moving, cover, vale, !gun.Trained);
            if (acc <= 0) { parts.Refused = P("no-line", "No clear line"); return $"{who} · can't hit from here"; }
            int ticks = DownTicks(k, id, tg, acc);
            string band = d <= R.PointBlankCm ? "point blank" : d <= w.EffCm ? "effective" : "long";
            string cost = g.FightBranch.Length > 0 && !R.FightTraced && g.Hostiles.Exists(h => h.Id == id) ? "untraced" : g.Gunfire > 0 ? $"+{R.HitTrail} T hit · +{R.DownTrail} T down" : $"+{R.GunfireTrail} T shooting · +{R.HitTrail} T hit · +{R.DownTrail} T down";
            int secs = (ticks + Game.Hz - 1) / Game.Hz;
            // Keel 0.15.0 (tuning.md §8.8) host notes: the chip at the shooter's gait and heading, now → on arrival (PreviewFire, both presets)
            FirePreview fp = null; Guard("aim preview", () => fp = g.PreviewFire((OpId)k, id, force));
            if (fp != null && fp.Line && fp.ShotsToDown > 0) { int ps = PreviewDownS(fp, k); if (ps >= 0) secs = ps; acc = fp.Acc; }
            if (fp != null) { parts.Gait = fp.Gait is "walk" or "run" or "run-away" ? fp.Gait : ""; if (fp.HasEnd && fp.ShotsToDownEnd > 0 && w != null) parts.ArriveS = (R.AimNewTicks + (fp.ShotsToDownEnd - 1) * w.Every + Game.Hz - 1) / Game.Hz; }
            parts.Down = $"{secs} s"; parts.Ratio = Ratio(acc); // Glyph fight r1 #2: whole seconds, as the rest of the HUD parts.Cover = cover < 1000;
            parts.Trail = g.FightBranch.Length > 0 && !R.FightTraced && g.Hostiles.Exists(h => h.Id == id) ? "" : $"+{R.HitTrail}/hit";
            return $"{who} · hits {Ratio(acc)} · down in {secs} s · {d / 100} m {band}{(cover < 1000 ? " · in cover" : "")} · {cost}";
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

        /// <summary>The move preview at a screen point now (no throttle): for scripted checks and shots.</summary>
        public void PreviewAt(Vector2 screen) { PointerHover(screen); if (Hover == null) UpdateMoveChip(screen, true); }

        void UpdateMoveChip(Vector2 screen, bool force)
        {
            if (!ModalGate.AllowsHover(Modal)) { MoveChip = null; MoveNow = null; return; }
            if (Hover != null || Panel != null || Game.Over || !cam) { MoveChip = null; MoveNow = null; return; }
            int k = Array.IndexOf(Selected, true); if (k < 0 || Game.Ops[k].Inside || Game.Ops[k].Down) { MoveChip = null; MoveNow = null; return; }
            if (!frame.Ground(cam.ScreenPointToRay(screen), out var w)) { MoveChip = null; MoveNow = null; return; }
            var at = frame.Engine(w); var cell = new Pt(at.X / 100 * 100, at.Y / 100 * 100);
            if (!force && cell == moveChipAt) return;
            if (Time.unscaledTime - moveChipTime < 0.2f && !force) { moveChipDirty = true; return; } // at most five quotes a second (an A* and a sweep each)
            moveChipAt = cell; moveChipTime = Time.unscaledTime; moveChipDirty = false;
            Pt? to = Game.S.Grid.Free(at) ? at : Game.S.Grid.NearestFree(at);
            string NoWay() => Text != null && Text.Pops.TryGetValue("no-way", out var nw) ? nw : "No way";
            if (!to.HasValue || G.Dist(to.Value, at) > 300) { MoveChip = "can't walk there"; MoveNow = new MoveParts { Refused = NoWay() }; return; }
            MoveQuote q = null; if (!Guard("move quote", () => q = Game.QuoteMove(Op.Name((OpId)k), to.Value))) return;
            if (q.WalkS == 0 && q.RunS == 0 && q.RunRefused.Length > 0) { MoveChip = q.RunRefused == "no walk there" ? "can't walk there" : q.RunRefused; MoveNow = new MoveParts { Refused = NoWay() }; return; }
            MoveNow = new MoveParts { WalkS = q.WalkS, RunS = q.RunRefused.Length > 0 ? -1 : q.RunS, RunFree = q.RunFree, Eye = q.RunFree ? "" : EyeOf(q.Seen), Alert = q.RunFree || q.Seen.Seen == "no" ? "" : q.Seen.AlertIfSeen.ToString() };
            // "seen by" is the engine's SeenQuote (rulebook sight.unknown): a known watcher's true answer, "?" for one the Scout hasn't watched that
            // could; never RunSeenBy, which knows every watcher (Sky 2026-10-16: sight is the Scout's skill)
            var seen = SeenWords(q.Seen);
            string run = q.RunRefused.Length > 0 ? $"can't run: {q.RunRefused}" : q.RunFree ? $"run {q.RunS} s"
                : $"run {q.RunS} s{(seen == "?" ? " · seen by: ?" : seen.Length > 0 ? $" · seen by {seen} (Alert {q.Seen.AlertIfSeen})" : "")}";
            MoveChip = $"{AgentNames[k]}: walk {q.WalkS} s · {run}";
            bool carrying = Game.Ops[k].CarryBody.Length > 0;
            if (carrying) { MoveChip = CarryChip(k, to.Value) ?? MoveChip; if (lastCarry != null) { MoveNow.CarryS = lastCarry.S; MoveNow.Eye = lastCarry.Seen == "?" ? "unk" : lastCarry.SeenS.Count > 0 ? "open" : "shut"; MoveNow.RunS = -1; } } // brief 013: carrying, the chip is the carry's (Glyph §4)
            Guard("move preview", () => PreviewRoute(k, to.Value, MoveNow, q.Seen, carrying && lastCarry != null && lastCarry.SeenS.Count > 0)); // Glyph's ring: its state and the route's glow
            { var mp = MoveNow; var tp = to.Value; Guard("cover snap", () => CoverSnapPreview(k, tp, mp)); } // brief 027 B5: the ring moves to the cover point the move will end on
        }

        /// <summary>Brief 027 B5 (HostCover.cs, with Keel's cover snap): where the move will snap. A no-op without it.</summary>
        partial void CoverSnapPreview(int k, Pt to, MoveParts mp);

        // ---------------------------------------------------------------- shots, read from the engine's log

        /// <summary>A shot in the log ("{by} fires on {t} at {d} m ({acc} ‰): hit."): a tracer and a flinch or a puff.</summary>
        void Shot(LogLine l)
        {
            int f = l.Text.IndexOf(" fires on ", StringComparison.Ordinal), a = l.Text.IndexOf(" at ", f + 1, StringComparison.Ordinal);
            if (f <= 0 || a <= f) return;
            string by = l.Text[..f], t = l.Text[(f + 10)..a];
            views.ShotEvent(ViewKey(by)); // the shooter's recoil, one per engine shot (HostViews.Figures), whatever became of the target
            var from = WorldOf(by); var to = WorldOf(t); if (!from.HasValue || !to.HasValue) return;
            bool hit = l.Code == "shot.hit";
            lastShot[by] = (t, Time.unscaledTime); // Sky 2026-10-20: the shooter turns to face it (AimTargets)
            var src = from.Value + Vector3.up * 1.3f; var dst = to.Value + Vector3.up * 1.2f;
            if (!hit) // a miss runs past and ends on the ground beside the target, never on the body
            {
                var dir = (dst - src); dir.y = 0; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
                var side = Vector3.Cross(Vector3.up, dir) * (((l.Tick + by.Length) & 1) == 0 ? 0.7f : -0.7f);
                dst = to.Value + dir * 1.8f + side; dst.y = to.Value.y + 0.05f;
            }
            string shotSide = by is "scout" or "fixer" or "tech" or "enforcer" ? "cell" : by.StartsWith("rival") ? "rival" : "guard";
            views.Tracer(src, dst, shotSide); // the HUD draws it (Glyph fight r2)
            ShotFx.Add((src, dst, hit, shotSide, Time.unscaledTime)); while (ShotFx.Count > 24) ShotFx.RemoveAt(0); // brief 024 Firing: the flash and the spark (HudToolkit.Fire), both views
            { var e = frame.Engine(from.Value); views.LastShotFrom = new Pt((int)System.Math.Round(e.X / 100.0) * 100, (int)System.Math.Round(e.Y / 100.0) * 100); } // brief 023: the startled turn away from it (1 m, so a frame's easing never moves it)
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

        /// <summary>Brief 027: where the player sees a fighter: its view's position when it has one drawn (a security view can stand up to 1.5 m off
        /// its engine point), else WorldOf. The fight overlay (bars, forecasts, badges, lines) anchors here so it sits on the drawn body.</summary>
        public Vector3? DrawnAt(string id)
        {
            var tr = views?.Find(ViewKey(id)); if (tr && tr.gameObject.activeInHierarchy) return tr.position;
            return WorldOf(id);
        }

        readonly Dictionary<string, (string t, float at)> lastShot = new();
        /// <summary>Each shot the engine logged (muzzle, where it went, hit, side, unscaled time): the HUD's muzzle flash and hit spark.</summary>
        public readonly List<(Vector3 from, Vector3 to, bool hit, string side, float born)> ShotFx = new();
        const float AimAfterShotS = 1.2f;

        /// <summary>Sky 2026-10-20 (the fight range): "when they shoot they need to be facing their target!", then "same for anything shooting at
        /// a target - must turn to face it". One rule for every shooter, whatever it is: any gun in the engine (the cell, the guard, the patrol,
        /// the rivals, the officers, the response's units and robots, a final-fight hostile) drawn and aiming at a target on the street, or
        /// any body that fired in the last 1.2 s (the log's shots: whoever fired, so a new armed thing needs no code), faces that target.
        /// Fills view key → the target's drawn position; `fired`: the view keys that shot within 0.4 s (the fire clip). View only.</summary>
        public void AimTargets(Dictionary<string, Vector3> into, HashSet<string> fired)
        {
            into.Clear(); fired.Clear(); var g = Game; if (g == null || views == null) return;
            float now = Time.unscaledTime;
            void Face(string by, string target)
            {
                if (string.IsNullOrEmpty(by) || string.IsNullOrEmpty(target) || by == target) return;
                Vector3? at = null; Guard("aim", () => at = DrawnAt(target)); if (!at.HasValue) return;
                var key = ViewKey(by);
                if (by == "rivals") { for (int i = 0; i < 3; i++) into["rivals/" + i] = at.Value; return; } // the crew shoots as one gun
                into[key] = at.Value;
            }
            bool Aiming(Gun gun) => gun != null && !gun.Down && gun.W != null && gun.Drawn && gun.HolsterAt < 0 && gun.Target.Length > 0 && g.PosOf(gun.Target).HasValue;
            Guard("aim", () =>
            {
                var guns = new List<Gun>(); foreach (var op in g.Ops) guns.Add(op.Gun);
                guns.Add(g.GuardGun); guns.Add(g.PatrolGun); guns.Add(g.RivalsGun); guns.AddRange(g.SecGuns);
                foreach (var u in g.Units) guns.Add(u.Gun); foreach (var h in g.Hostiles) guns.Add(h.Gun);
                foreach (var gun in guns) if (Aiming(gun)) Face(gun.Id, gun.Target);
                for (int k = 0; k < 4; k++) { var fs = FireNow[k]; if (fs.Target is { Length: > 0 } ft && fs.Mode.Length > 0 && !(Driving && k == dAgent && this.Aiming)) Face(Op.Name((OpId)k), ft); } // brief 024: the trigger, a lock or a follow: the agent faces its target (the aiming driven agent faces its reticle: HostAim)
            });
            var stale = new List<string>();
            foreach (var kv in lastShot)
            {
                float age = now - kv.Value.at; if (age > AimAfterShotS) { stale.Add(kv.Key); continue; }
                Face(kv.Key, kv.Value.t); if (age < 0.4f) { var k = ViewKey(kv.Key); if (kv.Key == "rivals") for (int i = 0; i < 3; i++) fired.Add("rivals/" + i); else fired.Add(k); }
            }
            foreach (var k in stale) lastShot.Remove(k);
        }

        /// <summary>The view an engine id draws as (an agent, a cast person, the patrol, the rivals, an officer, a fight hostile, a unit).</summary>
        public string ViewKey(string id)
        {
            if (id is "scout" or "fixer" or "tech" or "enforcer") return "op/" + id;
            if (id == "rivals") return "rivals/0";
            if (id.StartsWith("car:")) return "car/" + id[4..]; // engine 0.16: a car target is drawn by its car view
            if (id.StartsWith("camera:")) return "camera/" + id[7..]; // engine 0.15: a camera target
            if (Game.Range && id.StartsWith("bystander-")) return views?.BystanderKey(Game, id) ?? id; // engine 0.18: a range bystander
            for (int i = 0; i < Game.Hostiles.Count; i++) if (Game.Hostiles[i].Id == id) return "hostile/" + i;
            for (int i = 0; i < Game.SecGuns.Length; i++) if (Game.SecGuns[i].Id == id) return "security/" + Math.Min(i, 1);
            foreach (var u in Game.Units) if (u.Id == id) return "unit/" + id; // a response officer or robot
            return id;
        }

        /// <summary>Everyone the HUD puts health pips over: the cell when a fight is on, drawn or hurt; and anyone with a gun who has fired,
        /// is aiming or is a fight hostile.</summary>
        public IEnumerable<(string id, Gun gun)> Fighters()
        {
            var g = Game; bool fight = g.FightOn;
            for (int k = 0; k < 4; k++) { var gun = g.Ops[k].Gun; if (gun.W != null && !g.Ops[k].InCar && (fight || gun.Drawn || gun.Health < gun.Max || g.Ops[k].Down)) yield return (Op.Name((OpId)k), gun); }
            foreach (var h in g.Hostiles) if (h.Present && !h.Down) yield return (h.Id, h.Gun);
            foreach (var id in new[] { "guard", "patrol", "rivals" }) { var gun = g.GunOf(id); if (gun != null && !gun.Down && (gun.Fired || gun.Engaged || gun.Health < gun.Max || (fight && id == "guard" && g.GuardAlly))) yield return (id, gun); }
            foreach (var gun in g.SecGuns) if (!gun.Down && (gun.Fired || gun.Engaged) && g.PosOf(gun.Id).HasValue) yield return (gun.Id, gun);
        }
    }
}
