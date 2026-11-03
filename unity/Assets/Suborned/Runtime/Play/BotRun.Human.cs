using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Suborned.Sim;
using UnityEngine;
using Key = UnityEngine.InputSystem.Key;

namespace Suborned.Play
{
    /// <summary>The `human` policy (Sky 2026-10-25: "a bot that's indecisive. Humans aren't consistent with their actions."). The row carries a tape
    /// (tools/qa/human.ts, drawn from the row's seed): one recorded winning route as the plan, its orders as steps, and each step's noise. This side
    /// only reads it and plays toward the plan through the same real input as the other policies: an agent key, a ground click (a double click for
    /// a run) for a move, a left click on the thing and its wedge for an act (held when the wedge or the going-loud card asks), F for a draw, V for a
    /// stance, both buttons for panic. Steps go in script order as human.ts nextStep says (mirrored in NextStep and Ready below); a step's noise is
    /// acted out on its first try (gap, idle, turn, switch, a look through third person, an abandon, a misclick, a ring opened and shut, the
    /// neighbouring wedge, a cancel); a retry is played clean. Every indecision is logged in the row's report (human.events) and named on the next
    /// anomaly within HumanNoiseS, so a finding can be tied to the indecision that caused it.</summary>
    public sealed partial class BotRun
    {
        [Serializable] public class HumanStep
        {
            public int i, tick, after = -1, afterN, gap, opPrev = -1, x, y, turn, switchTo = -1, wrong, abandon = -1;
            public string op = "", act = "", target = "", approach = "", how = "", stance = "", place = "", cancelBy = "";
            public bool run, xy, ringClose, misclick, cancel, reorder, repeat;
            public float gapS, idleS, peekS, missDx, missDy, wrongNoticeS, cancelS, wanderS, cancelDx, cancelDy;
        }
        [Serializable] public class HumanRoute { public string route = "", group = ""; public HumanStep[] steps; }
        [Serializable] public class HumanSource { public string file = ""; public string[] fromFile, defaulted; }
        [Serializable] public class HumanTape { public string schema = "", route = "", group = ""; public int seed; public bool thirdPerson; public HumanStep[] steps; public HumanRoute[] alts; public HumanSource profileSource; }

        /// <summary>human.ts: LOOKAHEAD, WANDER. A step that cannot be given (refused, not on the ring, its agent never free) is given up after
        /// HumanTries tries or HumanGiveUpS of real time; one in flight (the host walking the agent in to give it) is waited on HumanFlightS.</summary>
        public const int HumanLookahead = 4, HumanWander = -2, HumanTries = 4;
        public const int HumanMaxEvents = 900, TpMovedCm = 15; // TpMovedCm: a drive sample this far from where the look began moved the agent (over the engine's drive slack)
        public const float HumanGiveUpS = 75f, HumanBlockedS = 90f, HumanDownS = 20f, HumanFlightS = 45f, HumanConfirmS = 1.5f, HumanNoiseS = 10f;

        /// <summary>One logged event. Op, Act, Target: the step (what the bot meant). A "given" event (Suture's Q-INTENT) also says what the game gave:
        /// the engine order that came of the try (Gave*: op, verb, target, where to; GaveOk, GaveCode its answer), Gave false when nothing came.</summary>
        sealed class HEvent { public float T; public int Tick, Step; public string Kind = "", Route = "", Op = "", Act = "", Target = "", Detail = "", GaveOp = "", GaveAct = "", GaveTarget = "", GaveTo = "", GaveCode = ""; public bool Gave, GaveOk; }
        sealed class HumanState
        {
            public string Route = "", Group = ""; public HumanStep[] Steps; public int[] Issued, Tries; public bool[] Resolved, Noised; public char[] Mark; public float[] FirstTry, BlockedSince;
            /// <summary>Tries given to the host and not yet answered: one per agent at most (a walk-in on one never hides another's answer).</summary>
            public readonly List<Flight> Flights = new();
            public bool[] Repeated; public readonly List<int> RepeatQ = new();
            public bool InFlight(int i) => Flights.Exists(f => f.Step == i && !f.Repeat);
            public bool OpInFlight(string op) => Flights.Exists(f => Steps[f.Step].op == op);
            public void Load(HumanStep[] steps, string route, string group)
            {
                Steps = steps ?? new HumanStep[0]; Route = route; Group = group; int n = Steps.Length;
                Issued = Enumerable.Repeat(-1, n).ToArray(); Tries = new int[n]; Resolved = new bool[n]; Noised = new bool[n]; Mark = Enumerable.Repeat('-', n).ToArray();
                FirstTry = Enumerable.Repeat(-1f, n).ToArray(); BlockedSince = Enumerable.Repeat(-1f, n).ToArray(); Flights.Clear(); Repeated = new bool[n]; RepeatQ.Clear();
            }
            public string Trace => $"{Route}: " + string.Concat(Enumerable.Range(0, Steps.Length).Select(i => Mark[i] == 'd' && Tries[i] > 1 ? (char)('0' + Math.Min(9, Tries[i])) : Mark[i]));
        }
        sealed class Flight { public int Step, Mark, Op = -1, Tick; public float At; public bool SeenPending, Repeat; }
        sealed class HumanRes
        {
            public string Route = "", Group = "", Profile = ""; public bool ThirdPerson; public readonly List<string> Routes = new(), Traces = new(); public int Steps, Done, Skipped;
            public readonly Dictionary<string, int> Counts = new(); public readonly List<HEvent> Events = new();
        }

        HumanState hs; (float t, string text)? lastNoise;

        // ---------------------------------------------------------------- the follower (human.ts ready / nextStep)

        bool OpFree(string op)
        {
            var g = h.Game; if (op == "cell") return true;
            int k; try { k = (int)Op.Parse(op); } catch { return false; }
            var o = g.Ops[k]; return Able(k) && o.Busy == null && o.Act == null && h.PendingOf(k).verb.Length == 0;
        }
        int? Earliest(int i)
        {
            var s = hs.Steps[i]; int e = s.tick; if (s.after < 0) return e;
            for (int j = s.after; j < s.after + s.afterN && j < hs.Steps.Length; j++) { int at = hs.Issued[j]; if (at < 0) { if (!hs.Resolved[j]) return null; continue; } e = Math.Max(e, at + s.gap); } // a step given up keeps no gap
            return e;
        }
        /// <summary>Ready but for the agent (blocked: past its tick, its agent's earlier step done, the agent not free).</summary>
        bool Due(int i) { var s = hs.Steps[i]; if (hs.Resolved[i] || hs.InFlight(i)) return false; if (s.opPrev >= 0 && !hs.Resolved[s.opPrev]) return false; var e = Earliest(i); return e != null && h.Game.Tick >= e; }
        bool Ready(int i) => Due(i) && OpFree(hs.Steps[i].op) && (hs.Steps[i].act != "panic" || h.Game.FightOn || h.OneInput) && !hs.OpInFlight(hs.Steps[i].op);
        int NextStep(out int over)
        {
            over = -1; int first = Array.IndexOf(hs.Resolved, false); if (first < 0) return -1;
            int end = Math.Min(hs.Steps.Length, first + HumanLookahead + 1), c = -1;
            for (int i = first; i < end; i++) if (Ready(i)) { c = i; break; }
            if (c < 0) return -1;
            if (hs.Steps[c].reorder) for (int j = c + 1; j < end; j++) if (hs.Steps[j].op != hs.Steps[c].op && Ready(j)) { over = c; return j; }
            return c;
        }

        // ---------------------------------------------------------------- the row

        IEnumerator Human(BotRow row)
        {
            var tape = row.human; var hr = new HumanRes { Profile = tape.profileSource == null || string.IsNullOrEmpty(tape.profileSource.file) ? "defaults" : $"{tape.profileSource.file} ({tape.profileSource.fromFile?.Length ?? 0} fields measured)" };
            cur.Human = hr; hs = new HumanState(); hs.Load(tape.steps, tape.route, tape.group); hr.Route = tape.route; hr.Group = tape.group; hr.ThirdPerson = tape.thirdPerson; hr.Routes.Add(tape.route); hr.Steps = hs.Steps.Length; lastNoise = null;
            float menuSince = -1f;
            while (true)
            {
                if (h.Modal == ModalGate.Modal.Menu) { if (menuSince < 0f) menuSince = Time.realtimeSinceStartup; else if (Time.realtimeSinceStartup - menuSince > 1f) { yield return Keys(new[] { Key.Escape }, 2); menuSince = -1f; } yield return null; continue; } // a stray Esc opened the menu: close it
                menuSince = -1f;
                if (h.Modal != ModalGate.Modal.None || capturing) { yield return null; continue; }
                FlightFrame();
                if (hs.RepeatQ.Count > 0 && hs.RepeatQ.FindIndex(j => OpFree(hs.Steps[j].op) && !hs.OpInFlight(hs.Steps[j].op)) is int rq && rq >= 0) // a repeat goes before the agent's next step
                {
                    int j = hs.RepeatQ[rq]; hs.RepeatQ.RemoveAt(rq); var rs0 = hs.Steps[j]; Noise("repeat", rs0, "the same again"); yield return Wait(rs0.gapS);
                    if (OpFree(rs0.op)) { var rr = new StepRes(); yield return PlayStep(rs0, true, rr); if (rr.Given && !rr.Done) hs.Flights.Add(new Flight { Step = j, Repeat = true, Mark = rr.Mark, Tick = rr.MarkTick >= 0 ? rr.MarkTick : h.Game.Tick, At = Time.realtimeSinceStartup, Op = rs0.op == "cell" ? -1 : (int)Op.Parse(rs0.op) }); else if (!rr.Given) Given(rs0, null, false, "", $"the repeat gave nothing: {rr.Why}"); }
                    continue;
                }
                int pick = NextStep(out int over);
                if (pick < 0) { BlockedFrame(); yield return Wait(0.2f); continue; }
                var s = hs.Steps[pick]; cur.Decisions++;
                if (over >= 0) Noise("reorder", s, $"step {over} ({Say(hs.Steps[over])}) waits");
                if (!hs.Noised[pick])
                {
                    hs.Noised[pick] = true;
                    if (s.abandon == HumanWander) { Noise("abandon", s, $"wanders {s.wanderS:0} s, then back to {hs.Route}"); yield return Wander(s.wanderS); continue; }
                    if (s.abandon >= 0 && tape.alts != null && s.abandon < tape.alts.Length && tape.alts[s.abandon].steps?.Length > 0)
                    {
                        var alt = tape.alts[s.abandon]; Noise("abandon", s, $"drops {hs.Route} at step {s.i} for {alt.route}");
                        Settle(hr); hs.Load(alt.steps, alt.route, alt.group); hr.Routes.Add(alt.route); hr.Steps += hs.Steps.Length; continue;
                    }
                    yield return Wait(s.gapS);
                    if (s.idleS > 0f) { Noise("idle", s, $"{s.idleS:0.0} s"); yield return Wait(s.idleS); }
                    if (s.turn != 0) { Noise("turn", s, s.turn < 0 ? "Q" : "E"); cur.Turns++; yield return Keys(new[] { s.turn < 0 ? Key.Q : Key.E }, 2); yield return Wait(0.4f); }
                    if (s.switchTo >= 0 && s.switchTo < 4) { bool centre = (s.i & 1) == 0; Noise("switch", s, $"looks at the {Op.Name((OpId)s.switchTo)}{(centre ? " (centred)" : "")}"); yield return SelectOp(s.switchTo, centre); yield return Wait(0.6f + (s.gapS % 1f)); }
                    if (s.peekS > 0f && !h.Following) yield return Peek(s);
                    if (!Ready(pick)) continue; // the world moved while the person dithered
                }
                else yield return Wait(1.5f); // a retry: a beat to look again
                if (!Ready(pick)) continue;
                hs.Tries[pick]++; if (hs.FirstTry[pick] < 0f) hs.FirstTry[pick] = Time.realtimeSinceStartup;
                bool clean = hs.Tries[pick] > 1; var res = new StepRes();
                yield return PlayStep(s, clean, res);
                if (res.Given)
                {
                    hs.Issued[pick] = h.Game.Tick;
                    if (res.Done) { Given(s, null, true, "", res.Why.Length > 0 ? res.Why : "nothing to give: already so"); Resolve(pick, 'd'); continue; }
                    var fl = new Flight { Step = pick, Mark = res.Mark, Tick = res.MarkTick >= 0 ? res.MarkTick : h.Game.Tick, At = Time.realtimeSinceStartup, Op = s.op == "cell" ? -1 : (int)Op.Parse(s.op) }; hs.Flights.Add(fl);
                    if (!clean && s.cancel)
                    {
                        yield return Wait(s.cancelS); FlightFrame();
                        int ck = fl.Op; var cop = ck >= 0 ? h.Game.Ops[ck] : null;
                        bool live = cop != null && (cop.Busy != null || cop.Act != null || cop.Walk != null || h.PendingOf(ck).verb.Length > 0);
                        if (!live) Noise("cancel", s, $"X {s.cancelS:0.0} s after giving it: too late, nothing to stop");
                        else
                        {
                            bool redirect = s.cancelBy == "redirect";
                            Noise("cancel", s, redirect ? $"sent elsewhere {s.cancelS:0.0} s after giving it (a ground click {s.cancelDx:0},{s.cancelDy:0} px off); given again later" : $"stopped {s.cancelS:0.0} s after giving it (X); given again later");
                            if (redirect) yield return Redirect(ck, new Vector2(s.cancelDx, s.cancelDy)); else yield return Stop(s.op);
                            if (hs.Mark[pick] == 'd') hr.Done--;
                            hs.Flights.Remove(fl); hs.Resolved[pick] = false; hs.Mark[pick] = '-'; hs.Issued[pick] = -1; hs.RepeatQ.Remove(pick); hs.Repeated[pick] = false; // undone: its repeat waits for the real one
                        }
                    }
                    continue;
                }
                if (res.Why.Length > 0 && (res.Hard && hs.Tries[pick] >= HumanTries || Time.realtimeSinceStartup - hs.FirstTry[pick] > HumanGiveUpS)) { Resolve(pick, 's'); Noise("skip", s, $"given up after {hs.Tries[pick]} tries: {res.Why}"); }
            }
        }

        void Resolve(int i, char mark)
        {
            hs.Resolved[i] = true; hs.Mark[i] = mark; hs.Flights.RemoveAll(f => f.Step == i && !f.Repeat); var hr = cur.Human; if (hr != null) { if (mark == 'd') hr.Done++; else hr.Skipped++; }
            if (mark == 'd' && hs.Steps[i].repeat && !hs.Repeated[i]) { hs.Repeated[i] = true; hs.RepeatQ.Add(i); }
        }
        void Settle(HumanRes hr) { hr.Traces.Add(hs.Trace); }

        /// <summary>A step in flight (given to the host): done when the engine takes a matching order; given back for a retry when the engine refuses
        /// it, or when nothing came of it (no order, and the host is not walking the agent in to give it).</summary>
        void FlightFrame() { foreach (var f in hs.Flights.ToArray()) FlightOne(f); }

        void FlightOne(Flight f)
        {
            int i = f.Step; var s = hs.Steps[i]; var g = h.Game; Order other = null; bool otherOk = false;
            for (int n = f.Mark; n < g.Orders.Count; n++)
            {
                var (o, ok, code, _) = g.Orders[n];
                if (!EngineMatch(o, s)) { if (other == null && o != null && o.Do != "drive" && (s.op == "cell" || o.Op == s.op)) { other = o; otherOk = ok; } continue; } // what the game gave instead (Q-INTENT)
                Given(s, o, ok, code, f.Repeat ? "the repeat" : "", f.Tick);
                hs.Flights.Remove(f); if (f.Repeat) return;
                if (ok) { Resolve(i, 'd'); return; }
                hs.Issued[i] = -1; if (hs.Tries[i] >= HumanTries) { Resolve(i, 's'); Noise("skip", s, $"refused {hs.Tries[i]} times: {code}"); } return;
            }
            bool pend = f.Op >= 0 && h.PendingOf(f.Op).verb.Length > 0; if (pend) f.SeenPending = true;
            float age = Time.realtimeSinceStartup - f.At;
            if (!pend && age > HumanConfirmS || age > HumanFlightS)
            {
                string why = other != null ? "not what was meant" : f.SeenPending ? "walked in, never given" : "no order came of it";
                Given(s, other, otherOk, "", f.Repeat ? "the repeat: " + why : why, f.Tick);
                hs.Flights.Remove(f); if (f.Repeat) return;
                hs.Issued[i] = -1; if (hs.Tries[i] >= HumanTries) { Resolve(i, 's'); Noise("skip", s, why); }
            }
        }

        /// <summary>The "given" event of a try: what the bot meant (the step) against what the game gave (the engine order, or nothing).</summary>
        /// <summary>`tick`: when the try's input was given (-1: now); the engine's order, if any, comes at or after it.</summary>
        void Given(HumanStep s, Order o, bool ok, string code, string detail, int tick = -1)
        {
            var hr = cur?.Human; if (hr == null) return; var g = h.Game;
            hr.Counts["given"] = (hr.Counts.TryGetValue("given", out var n) ? n : 0) + 1;
            if (hr.Events.Count >= HumanMaxEvents) return;
            string to = o == null ? "" : o.Point is Pt pt ? $"{pt.X},{pt.Y}" : o.To ?? "";
            hr.Events.Add(new HEvent { T = Time.realtimeSinceStartup - rowStart, Tick = tick >= 0 ? tick : g?.Tick ?? 0, Kind = "given", Route = hs?.Route ?? "", Step = s.i, Op = s.op, Act = s.act, Target = s.target.Length > 0 ? s.target : s.place.Length > 0 ? s.place : s.xy ? $"{s.x},{s.y}" : "", Detail = detail,
                Gave = o != null, GaveOp = o?.Op ?? "", GaveAct = o?.Do ?? "", GaveTarget = o?.Target ?? "", GaveTo = to, GaveOk = ok, GaveCode = code ?? "" });
        }

        /// <summary>A step whose agent has not been free for HumanBlockedS since it fell due is given up (an agent down, inside for good, or held).</summary>
        void BlockedFrame()
        {
            int first = Array.IndexOf(hs.Resolved, false); if (first < 0) return; int end = Math.Min(hs.Steps.Length, first + HumanLookahead + 1); float now = Time.realtimeSinceStartup;
            for (int i = first; i < end; i++)
            {
                if (!Due(i) || Ready(i)) { hs.BlockedSince[i] = -1f; continue; }
                var op = hs.Steps[i].op; bool down = op != "cell" && h.Game.Ops[(int)Op.Parse(op)].Down; float limit = down ? HumanDownS : HumanBlockedS; // a downed agent's steps go quickly: each would wait in turn
                if (hs.BlockedSince[i] < 0f) hs.BlockedSince[i] = now; else if (now - hs.BlockedSince[i] > limit) { Resolve(i, 's'); Noise("skip", hs.Steps[i], $"the {op} was {(down ? "down" : "not free")} for {limit:0} s"); }
            }
        }

        // ---------------------------------------------------------------- one step through real input

        /// <summary>Why a try gave nothing: Hard (the step cannot be given this way: no such place, unarmed) is given up after HumanTries; a soft one
        /// (locked now, not on the ring, off screen) is tried again until HumanGiveUpS.</summary>
        sealed class StepRes { public bool Given, Done, Hard; public int Mark, MarkTick = -1; public string Why = ""; }

        IEnumerator PlayStep(HumanStep s, bool clean, StepRes res)
        {
            var g = h.Game; int k = s.op == "cell" ? -1 : (int)Op.Parse(s.op);
            switch (s.act)
            {
                case "move": yield return PlayMove(s, k, clean, res); yield break;
                case "draw":
                    if (k < 0 || g.Ops[k].Gun.W == null) { res.Why = "unarmed"; res.Hard = true; yield break; }
                    if (g.Ops[k].Gun.Drawn) { res.Given = res.Done = true; yield break; } // F would holster
                    yield return SelectOp(k, false); res.Mark = g.Orders.Count; res.MarkTick = g.Tick; yield return HoldKey(Key.F, 0.15f); res.Given = true; yield break;
                case "stance" when h.OneInput: case "free" when h.OneInput: case "cease" when h.OneInput: res.Given = res.Done = true; yield break; // engine 0.35 (one input; Keel's oneinput.py): retired, weapons free is always on
                case "panic" when h.OneInput:
                {   // engine 0.35: the panic is F on each of the four (oneinput.py): select all, F draws every holstered one
                    for (int i = 0; i < 4; i++) if (g.Ops[i].Gun.W != null && !g.Ops[i].Down && !g.Ops[i].Gun.Drawn) { yield return SelectOp(i, false); res.Mark = g.Orders.Count; res.MarkTick = g.Tick; yield return HoldKey(Key.F, 0.15f); }
                    res.Given = res.Done = true; yield break;
                }
                case "stance":
                    if (k < 0 || g.Ops[k].Gun.W == null) { res.Why = "unarmed"; res.Hard = true; yield break; }
                    yield return SelectOp(k, false);
                    for (int n = 0; n < 3 && g.Ops[k].Gun.Stance != s.stance; n++) { yield return Keys(new[] { Key.V }, 2); yield return Wait(0.25f); }
                    if (g.Ops[k].Gun.Stance == s.stance) res.Given = res.Done = true; else { res.Why = $"stance {g.Ops[k].Gun.Stance}, wanted {s.stance}"; res.Hard = true; }
                    yield break;
                case "panic":
                {
                    var c = new Vector2(Screen.width * 0.5f, Screen.height * 0.45f); c = OffThings(c);
                    res.Mark = g.Orders.Count; res.MarkTick = g.Tick; yield return Both(c, 1.2f); res.Given = true; yield break;
                }
                default: yield return PlayRing(s, k, clean, res); yield break;
            }
        }

        IEnumerator PlayMove(HumanStep s, int k, bool clean, StepRes res)
        {
            var g = h.Game; if (k < 0) { res.Why = "a move with no agent"; res.Hard = true; yield break; }
            Pt to; if (s.xy) to = new Pt(s.x, s.y); else if (g.S.HasPlace(s.place)) to = g.S.Stand(s.place); else { res.Why = $"no place '{s.place}'"; res.Hard = true; yield break; }
            yield return SelectOp(k, false);
            var world = h.frame.World(to); var on = new bool[1]; yield return OnScreen(world, k, on);
            if (!on[0]) { res.Why = "the place would not come on screen"; yield break; }
            var s3 = h.View.WorldToScreenPoint(world); var sp = new Vector2(s3.x, s3.y);
            if (!clean && s.misclick) { sp += new Vector2(s.missDx, s.missDy) * Kpx; Noise("misclick", s, $"the ground {s.missDx:0},{s.missDy:0} px off"); }
            int tr = s.i < hs.Tries.Length ? hs.Tries[s.i] : 1; if (tr > 1) { float a = (s.i * 97 + tr * 131) % 360 * Mathf.Deg2Rad; sp += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (tr - 1) * 18f * Kpx; } // a retry clicks a little aside (the first point was a roof, a wall or a solid)
            sp = OffThings(sp);
            res.Mark = g.Orders.Count; res.MarkTick = g.Tick; cur.Moves++;
            if (s.run) { cur.Runs++; yield return Click(sp); yield return Wait(0.06f); }
            yield return Click(sp); res.Given = true;
        }

        IEnumerator PlayRing(HumanStep s, int k, bool clean, StepRes res)
        {
            var g = h.Game;
            if (k >= 0) yield return SelectOp(k, false);
            else if (s.act == "go" && Enumerable.Range(0, 4).FirstOrDefault(j => g.Ops[j].InCar) is int kc && g.Ops[kc].InCar) yield return SelectOp(kc, false); // Go is given from the car, by one aboard
            var found = new Found[1]; yield return FindTarget(s, k, found);
            var f = found[0]; if (f?.P == null) { res.Why = "nothing near offers it"; yield break; }
            var on = new bool[1]; yield return OnScreen(f.P.Anchor, k, on); if (!on[0]) { res.Why = "the thing would not come on screen"; yield break; }
            Vector2 Sp() { var q = h.View.WorldToScreenPoint(f.P.Anchor); return new Vector2(q.x, q.y); }
            if (f.Locked.Length > 0) { res.Why = f.Locked; yield break; } // shown, but locked now (Vale not at his car yet): wait and try again
            if (!clean && s.misclick)
            {
                var miss = OffThings(Sp() + new Vector2(s.missDx, s.missDy) * Kpx); cur.Clicks++; yield return Click(miss); yield return Wait(0.35f);
                Noise("misclick", s, $"{s.missDx:0},{s.missDy:0} px off the {f.P.T.Kind} {f.P.T.Key}{(h.Ring != null ? $" (opened the {h.Ring.Plan?.T?.Kind} ring)" : " (a walk)")}");
                if (h.Ring != null) yield return Keys(new[] { Key.Escape }, 2);
                yield return Wait(0.5f);
            }
            var ring = new HandoverHost.RingState[1]; yield return OpenRingOn(Sp, ring); if (ring[0] == null) { res.Why = "no ring opened"; yield break; }
            int seg = SegFor(ring[0], s, k);
            if (seg < 0) { yield return ViaPanel(ring[0], s, k, res); yield break; } // past the ring's five: its ⋯ wedge and the full panel
            if (!clean && s.ringClose)
            {
                yield return Wait(0.5f + (s.gapS % 0.7f)); Noise("ring-close", s, $"opened the {f.P.T.Kind} ring and shut it"); if (h.Ring != null) yield return Keys(new[] { Key.Escape }, 2);
                yield return Wait(0.4f + (s.gapS % 0.6f)); yield return OpenRingOn(Sp, ring); if (ring[0] == null) { res.Why = "no ring opened"; yield break; }
                seg = SegFor(ring[0], s, k); if (seg < 0) { yield return ViaPanel(ring[0], s, k, res); yield break; }
            }
            var rs = ring[0];
            if (!clean && s.wrong != 0 && k >= 0 && WrongSeg(rs, seg, s.wrong, k) is int ws && ws >= 0)
            {
                var wseg = rs.Segs[ws]; Noise("wrong", s, $"took {wseg.Word} for {rs.Segs[seg].Word}; noticed {s.wrongNoticeS:0.0} s later (X)");
                yield return Take(rs, ws); yield return Wait(s.wrongNoticeS); yield return Stop(s.op); yield return Wait(0.4f);
                res.Why = "wrong wedge"; yield break; // the retry is clean
            }
            res.Mark = g.Orders.Count; res.MarkTick = g.Tick; yield return Take(rs, seg); res.Given = true;
        }

        /// <summary>A way the ring does not show (it holds five): the ⋯ wedge opens the full panel, and the person clicks the way's row there. When the
        /// kit draws no such row on screen, or the press never reaches it (an unfocused player's UI Toolkit), the row's own handler takes it, logged.</summary>
        IEnumerator ViaPanel(HandoverHost.RingState rs, HumanStep s, int k, StepRes res)
        {
            var g = h.Game; int more = rs.Segs.FindIndex(x => x.More);
            if (more < 0) { res.Why = "not on the ring"; if (h.Ring != null) yield return Keys(new[] { Key.Escape }, 2); yield break; }
            yield return Take(rs, more);
            float until = Time.realtimeSinceStartup + 0.8f; while (h.Panel == null && Time.realtimeSinceStartup < until) yield return null;
            var pn = h.Panel; if (pn == null) { res.Why = "the panel did not open"; yield break; }
            int row = -1; for (int i = 0; i < pn.Rows.Count; i++) { var r = pn.Rows[i]; if (r.Available && (k < 0 || r.By == (OpId)k) && RowMatch(r.I?.Order, s)) { row = i; break; } }
            if (row < 0) { res.Why = "not on the ring or the panel"; if (h.Panel != null) yield return Keys(new[] { Key.Escape }, 2); yield break; }
            var od = pn.Rows[row].I.Order; offered = (Op.Name(pn.Rows[row].By), od.Do ?? "", od.Target ?? "", Time.realtimeSinceStartup, false);
            res.Mark = g.Orders.Count; res.MarkTick = g.Tick; var at = HudToolkit.PanelRowScreen(row);
            if (at is Vector2 a) { cur.Clicks++; yield return Click(a); yield return Wait(0.3f); }
            if (h.Panel != null && ReferenceEquals(h.Panel, pn)) { Noise("panel-direct", s, at == null ? "the kit drew no such row" : "the row's press was not taken"); h.PanelDo(row); }
            res.Given = true;
        }

        /// <summary>The wedge's press: held while the wedge's own hold or the going-loud card it opened wants it (Fire, Kill, a seen act), else a click.
        /// A card that would fail the mission waits for a hold on the card: the person backs off it, as the other policies do.</summary>
        IEnumerator Take(HandoverHost.RingState rs, int seg)
        {
            var s = rs.Segs[seg]; var od = s.Row?.I?.Order; cur.Taken++;
            offered = od == null ? null : (s.By.Count == 1 ? Op.Name(s.By[0]) : "", od.Do ?? "", od.Target ?? "", Time.realtimeSinceStartup, s.Shut || s.Row?.Available == false);
            var at = WedgeAt(rs, seg, rs.Segs.Count); float minHold = s.HoldWedge || s.Harmful || od?.Do == "fire" ? 0.9f : 0f, t0 = Time.realtimeSinceStartup;
            MouseState0(at, false, false); yield return null; MouseState0(at, true, false); yield return null; yield return null;
            while (true)
            {
                float held = Time.realtimeSinceStartup - t0;
                bool want = held < minHold || held < 2.5f && (h.Loud != null && h.Loud.Source != "wait" || h.Ring != null && h.Ring.HoldSeg >= 0);
                if (!want) break; MouseState0(at, true, false); yield return null;
            }
            MouseState0(at, false, false); yield return null;
            if (h.Loud != null && h.Loud.Source == "wait") { Noise("backed-off", null, "a going-loud card that would fail the mission"); yield return RightTap(at); }
            yield return Wait(0.2f);
        }

        IEnumerator OpenRingOn(Func<Vector2> sp, HandoverHost.RingState[] ring)
        {
            ring[0] = null; cur.Clicks++; yield return Click(sp());
            float until = Time.realtimeSinceStartup + 0.8f; while (h.Ring == null && Time.realtimeSinceStartup < until) yield return null;
            if (h.Ring == null) { cur.RingMisses++; yield break; }
            cur.Rings++; ring[0] = h.Ring;
        }

        int SegFor(HandoverHost.RingState rs, HumanStep s, int k)
        {
            int any = -1;
            for (int i = 0; i < rs.Segs.Count; i++)
            {
                var sg = rs.Segs[i]; if (sg.More || sg.Shut || sg.Row?.Available != true || !RowMatch(sg.Row.I?.Order, s)) continue;
                if (k < 0 || sg.By.Contains((OpId)k)) return i; if (any < 0) any = i;
            }
            return k < 0 ? any : -1;
        }

        /// <summary>The neighbouring wedge (dir -1 or +1) the same agent gives, when it is a calm one: a slip never Kills, Fires, Goes, Enters or Takes.</summary>
        static int WrongSeg(HandoverHost.RingState rs, int seg, int dir, int k)
        {
            int n = rs.Segs.Count;
            for (int step = 1; step < n; step++)
            {
                int j = ((seg + dir * step) % n + n) % n; if (j == seg) break; var sg = rs.Segs[j];
                if (sg.More || sg.Shut || sg.Row?.Available != true || !sg.By.Contains((OpId)k)) continue;
                var o = sg.Row.I?.Order; if (o == null || sg.Violent || sg.Harmful || sg.HoldWedge || o.Do is "go" or "enter" or "take" or "board" or "stash" or "takedown" or "fire" || o.Do == "turn" && o.Approach == "pressure") return -1;
                return j;
            }
            return -1;
        }

        // ---------------------------------------------------------------- third person (T), and Sky's rule on it

        /// <summary>A look through third person: T on the selected agent, nothing pressed for the look's seconds, T back. Sky (2026-10-22, after
        /// 0.2.56): "when I go third person, it shouldn't stop the character walking / running or doing whatever action they were until I intervene."
        /// The bot presses nothing in between, so an act under way that ends early with a cancel, a drive sample that moves the agent, or a move for
        /// it in the orders (or the Tech's loop let go) is the host's doing: Q-TP-KEEP.</summary>
        IEnumerator Peek(HumanStep s)
        {
            var g = h.Game; int k = (int)h.Agent; if (k < 0 || k > 3 || !h.Selected[k]) yield break; var op = g.Ops[k];
            if (op.Down || op.Inside || op.InCar) yield break; // T on an agent who can't be taken only pops a refusal
            var busy = op.Busy; int busyEnd = op.BusyEnd; var act = op.Act; bool loop = k == (int)OpId.Tech && g.LoopOn; int mark = g.Orders.Count; var from = op.Pos;
            string had = busy != null ? $"{busy.Do} {busy.Target}".Trim() : act != null ? $"walking in to {act.Do} {act.Target}".Trim() : loop ? "holding the loop" : op.Walk != null ? "walking" : "";
            Noise("third-person", s, $"{s.peekS:0.0} s behind the {Op.Name((OpId)k)}{(had.Length > 0 ? $", {had}" : "")}");
            yield return Keys(new[] { Key.T }, 2);
            // the walk-off case (Sky, 2026-10-25): on every other look from an idle agent, W held there, then the agent key of one at work with W still
            // down: the key held over must not walk the agent taken (the host ignores it until it is pressed again)
            int j = Enumerable.Range(0, 4).FirstOrDefault(x => x != k && Works(x)); bool swapTest = (s.i & 1) == 1 && h.Driving && !Works(k) && j != k && Works(j);
            if (swapTest)
            {
                var oj = g.Ops[j]; var jb = oj.Busy; int jEnd = oj.BusyEnd; var ja = oj.Act; bool jl = j == (int)OpId.Tech && g.LoopOn; int jm = g.Orders.Count; var jf = oj.Pos;
                Noise("third-person", s, $"W held on the {Op.Name((OpId)k)}, then the {Op.Name((OpId)j)} taken with W still down");
                yield return HoldKeys(new[] { Key.W }, 0.4f, keepDown: true); yield return HoldKeys(new[] { Key.W, Key.F1 + j }, 0f, keepDown: true); yield return HoldKeys(new[] { Key.W }, 0.8f);
                string jlost = h.DriveAgent == j ? TpLost(j, jb, jEnd, ja, jl, jm, jf) : null;
                if (jlost != null) yield return Hit("Q-TP-KEEP", $"{Op.Name((OpId)j)}:held-key", "A key held over a swap walked the agent taken", "a movement key held from the last agent is ignored until it is pressed again (Sky's walk-off, 2026-10-25)", jlost, h.frame.World(g.Ops[j].Pos));
            }
            yield return Wait(s.peekS);
            string lost = swapTest ? null : TpLost(k, busy, busyEnd, act, loop, mark, from); // with the swap test the bot walked the first agent itself
            if (h.Following) yield return Keys(new[] { Key.T }, 2);
            yield return Wait(0.3f); if (!swapTest) lost ??= TpLost(k, busy, busyEnd, act, loop, mark, from); // the let-go too
            if (lost != null) yield return Hit("Q-TP-KEEP", $"{Op.Name((OpId)k)}:{busy?.Do ?? act?.Do ?? (loop ? "loop" : "")}", "Third person stopped an act", "an act under way when T is pressed goes on until the player intervenes (Sky 2026-10-22)", lost, h.frame.World(g.Ops[k].Pos));
        }

        /// <summary>Agent x is at work: an act, a walk-in, a walk, a queued act, or (the Tech) the loop held.</summary>
        bool Works(int x) { var g = h.Game; var o = g.Ops[x]; return !o.Down && !o.Inside && !o.InCar && (o.Busy != null || o.Act != null || o.Walk != null || h.PendingOf(x).verb.Length > 0 || x == (int)OpId.Tech && g.LoopOn); }

        /// <summary>What third person cost agent k since `mark`, null nothing: the act it was on ended early, its walk-in dropped, or the loop let go,
        /// with an order for it in between that the bot did not press for (a cancel, a move, a drive sample that moved it).</summary>
        string TpLost(int k, Order busy, int busyEnd, Order act, bool loop, int mark, Pt from)
        {
            var g = h.Game; var op = g.Ops[k]; string name = Op.Name((OpId)k), by = "";
            for (int n = mark; n < g.Orders.Count && by.Length == 0; n++)
            {
                var (o, ok, _, _) = g.Orders[n]; if (o == null || o.Op != name || !ok) continue;
                if (o.Do is "cancel" or "move") by = o.Do; else if (o.Do == "drive" && !o.Release && o.Point is Pt p && G.Dist(p, from) > TpMovedCm) by = "a drive sample that moved it";
            }
            if (by.Length == 0) return null;
            string clock = HandoverHost.MissionClock(g.Tick);
            if (busy != null && !ReferenceEquals(op.Busy, busy) && g.Tick < busyEnd - 1) return $"the {name}'s {busy.Do} {busy.Target} ended at {clock}, {(busyEnd - g.Tick) / (float)Game.Hz:0.0} s early, after {by} the bot never gave";
            if (act != null && op.Act == null && op.Busy?.Do != act.Do) return $"the {name}'s walk-in to {act.Do} {act.Target} dropped at {clock}, after {by} the bot never gave";
            if (loop && !g.LoopOn) return $"the Tech's loop let go at {clock}, after {by} the bot never gave";
            return null;
        }

        // ---------------------------------------------------------------- finding the thing

        sealed class Found { public HandoverHost.Pickable P; public string Locked = ""; }

        /// <summary>The thing whose ring offers the step: by name first (the person's role, the camera's id, the kind), then the kinds the verb acts
        /// on, then the nearest things to the agent; each asked through the host's own ContextFor. Locked: it offers the way, but shut now.</summary>
        IEnumerator FindTarget(HumanStep s, int k, Found[] found)
        {
            var g = h.Game; var all = h.Pickables().ToList(); var opPos = k >= 0 ? h.frame.World(g.Ops[k].Pos) : h.View.transform.position;
            bool Named(HandoverHost.Pickable p) => s.target.Length > 0 && (p.T.Key == s.target || p.T.Id == s.target || p.T.Kind == s.target || SameTarget(p.T.Key, s.target));
            bool Kind(HandoverHost.Pickable p) => s.act switch
            {
                "enter" => p.T.Kind == "door",
                "take" => p.T.Kind == "person" && p.T.Key is "councillor" or "driver" || p.T.Kind == "car" && p.T.Key == "official",
                "board" or "go" => p.T.Kind == "car" && p.T.Key == "getaway",
                "loop" or "disable" or "reroute" => p.T.Kind is "cabinet" or "camera" or "patrol",
                "carry" or "restrain" or "drop" => p.T.Kind is "body" or "person" or "op",
                "stash" => p.T.Kind is "stash" or "door",
                "fire" or "mislead" => p.T.Kind is "rivals" or "hostile" or "security" or "unit" or "patrol" or "person" or "camera" or "car",
                _ => false,
            };
            var order = all.Where(p => p.T.Kind != "op" || g.Ops[(int)Op.Parse(p.T.Key)].Down).OrderBy(p => Named(p) ? 0 : Kind(p) ? 1 : 2).ThenBy(p => (p.Anchor - opPos).sqrMagnitude).Take(10).ToList();
            Found locked = null; int asked = 0;
            foreach (var p in order)
            {
                if (asked >= 3 && !Named(p) && !Kind(p)) break; // past the likely ones, three of the nearest at most
                asked++;
                var s3 = h.View.WorldToScreenPoint(p.Anchor); HandoverHost.ContextPlan plan = null;
                if (!HandoverHost.FrameCost.Contains("bot")) HandoverHost.FrameCost += HandoverHost.FrameCost.Length > 0 ? "+bot" : "bot"; // the offers are priced this frame: Q-FRAME does not count the bot's own work
                try { plan = h.ContextFor(p.T, new Vector2(s3.x, s3.y)); } catch (Exception e) { Debug.LogWarning($"BotRun: human ContextFor {p.T.Kind}/{p.T.Key}: {e.Message}"); }
                if (plan == null) continue;
                bool open = false; string lockWhy = "";
                foreach (var r in plan.Pn.Rows) { if (!RowMatch(r.I?.Order, s) || k >= 0 && r.By != (OpId)k) continue; if (r.Available) { open = true; break; } if (lockWhy.Length == 0) lockWhy = $"locked: {r.Lock.code ?? r.Reason}"; }
                if (open) { found[0] = new Found { P = p }; yield break; }
                if (lockWhy.Length > 0 && locked == null) locked = new Found { P = p, Locked = lockWhy };
                if (asked % 3 == 0) yield return null; // spread the pricing over frames
            }
            found[0] = locked; // null: nothing near offers it at all
        }

        static string Norm(string x) { x = (x ?? "").ToLowerInvariant(); foreach (var pre in new[] { "camera:", "stash/", "car:", "body/" }) if (x.StartsWith(pre)) x = x[pre.Length..]; return x.TrimEnd('s'); }
        /// <summary>The route's target and the host's: the same, one the other's family ("rivals" and "rival-2", "security" and "security-1"), or the
        /// same person by role and by id (the route's "guard", the host's body "p-records-office-01"; Game.PersonById reads a cache, no state).</summary>
        bool SameTarget(string a, string b)
        {
            if (a == b) return true; string na = Norm(a), nb = Norm(b); if (na.Length == 0 || nb.Length == 0) return false;
            if (na == nb || na.StartsWith(nb) || nb.StartsWith(na)) return true;
            var g = h.Game; if (g == null) return false; var pa = g.PersonById(a); return pa != null && ReferenceEquals(pa, g.PersonById(b));
        }
        bool RowMatch(Order o, HumanStep s) => o != null && o.Do == s.act && (s.approach.Length == 0 || o.Approach == s.approach) && (s.how.Length == 0 || o.How == s.how) && (s.target.Length == 0 || o.Target.Length == 0 || SameTarget(o.Target, s.target));
        bool EngineMatch(Order o, HumanStep s) => o != null && (s.op == "cell" || o.Op == s.op) && (s.act == "move" ? o.Do == "move" : RowMatch(o, s));

        // ---------------------------------------------------------------- the person's hands

        static float Kpx => Screen.height / 1080f;

        /// <summary>The agent alone selected (its key); `centre` presses it twice, the host's centre-on.</summary>
        IEnumerator SelectOp(int k, bool centre)
        {
            if (h.Ring != null) { yield return Keys(new[] { Key.Escape }, 2); yield return null; } // the agent keys are not read while a ring is open
            bool alone = h.Selected[k] && h.Selected.Count(x => x) == 1 && h.Agent == (OpId)k;
            if (!alone || centre) yield return Keys(new[] { Key.F1 + k }, 2);
            if (centre) { yield return Wait(0.08f); yield return Keys(new[] { Key.F1 + k }, 2); yield return Wait(0.6f); }
        }

        /// <summary>Brings a world point inside the screen's middle (not under the HUD): the agent's key twice when it is near the agent, else the pan keys toward it.</summary>
        IEnumerator OnScreen(Vector3 world, int k, bool[] on)
        {
            var cam = h.View; on[0] = false; if (cam == null) yield break;
            for (int n = 0; n < 10; n++)
            {
                var s3 = cam.WorldToScreenPoint(world); var sp = new Vector2(s3.x, s3.y); float m = 140f * Kpx;
                if (s3.z > 0 && sp.x > m && sp.y > m && sp.x < Screen.width - m && sp.y < Screen.height - m && !HandoverHost.OverHud(sp)) { on[0] = true; yield break; }
                if (n == 0 && k >= 0 && (h.frame.World(h.Game.Ops[k].Pos) - world).magnitude < 25f) { yield return SelectOp(k, true); continue; }
                var d = new Vector2(sp.x - Screen.width / 2f, sp.y - Screen.height / 2f); if (s3.z < 0) d = -d;
                var keys = new List<Key>(); if (d.y > 40) keys.Add(Key.W); if (d.y < -40) keys.Add(Key.S); if (d.x > 40) keys.Add(Key.D); if (d.x < -40) keys.Add(Key.A);
                if (keys.Count == 0) { on[0] = s3.z > 0; yield break; }
                cur.Pans++; yield return HoldKeys(keys.ToArray(), Mathf.Clamp(d.magnitude / (700f * Kpx), 0.08f, 1.2f)); yield return Wait(0.1f);
            }
        }

        /// <summary>A click point moved off any thing and any agent of the cell (a click on one would open its ring or select it, not walk).</summary>
        Vector2 OffThings(Vector2 sp)
        {
            var cam = h.View; if (cam == null) return sp; float r = (HandoverHost.PickPx + 10f) * Kpx;
            var pts = h.Pickables().Select(p => cam.WorldToScreenPoint(p.Anchor)).Where(q => q.z > 0).Select(q => new Vector2(q.x, q.y)).ToList();
            for (int k = 0; k < 4; k++) { var q = cam.WorldToScreenPoint(h.frame.World(h.Game.Ops[k].Pos) + Vector3.up); if (q.z > 0) pts.Add(new Vector2(q.x, q.y)); }
            for (int n = 0; n < 8; n++)
            {
                var near = pts.Where(p => (p - sp).magnitude < r).ToList(); if (near.Count == 0 && !HandoverHost.OverHud(sp)) return sp;
                var away = Vector2.zero; foreach (var p in near) away += (sp - p).sqrMagnitude > 1e-3f ? (sp - p).normalized : Vector2.right; if (away == Vector2.zero || HandoverHost.OverHud(sp)) away = new Vector2(Screen.width / 2f, Screen.height / 2f) - sp;
                sp += away.normalized * 14f * Kpx;
            }
            return sp;
        }

        /// <summary>A cancel as Sky mostly made one: a ground click `off` px (at 1080p) from the agent, a new walk that replaces the order.</summary>
        IEnumerator Redirect(int k, Vector2 off)
        {
            if (k < 0) yield break; yield return SelectOp(k, false);
            var q = h.View.WorldToScreenPoint(h.frame.World(h.Game.Ops[k].Pos)); if (q.z <= 0f) { yield return Keys(new[] { Key.X }, 2); yield break; }
            var sp = OffThings(new Vector2(q.x, q.y) + off * Kpx); sp.x = Mathf.Clamp(sp.x, 90f, Screen.width - 90f); sp.y = Mathf.Clamp(sp.y, 90f, Screen.height - 90f);
            cur.Moves++; yield return Click(sp);
            var op = h.Game.Ops[k]; if (op.Busy != null || op.Act != null || h.PendingOf(k).verb.Length > 0) { yield return Wait(0.5f); yield return Click(sp); } // brief 083: on an act the first click only asks; the second stops it
        }

        /// <summary>X: the selected agent's order stops (the hold preset's stop).</summary>
        IEnumerator Stop(string op) { if (op == "cell") yield break; yield return SelectOp((int)Op.Parse(op), false); yield return Keys(new[] { Key.X }, 2); }

        /// <summary>The plan dropped a while: ground walks and pans by any agent (never an act), then back.</summary>
        IEnumerator Wander(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                if (h.Modal != ModalGate.Modal.None || h.Game.Over) { yield return null; continue; }
                var able = Enumerable.Range(0, 4).Where(Able).ToList(); if (able.Count == 0) yield break;
                int k = able[rng.Next(able.Count)]; yield return SelectOp(k, false);
                if (rng.NextDouble() < 0.7) yield return GroundMove(k, rng.NextDouble() < 0.2); else yield return PanToward(k);
                yield return Wait(1.5f + (float)rng.NextDouble() * 2.5f);
            }
        }

        /// <summary>Keys held for `seconds` (at least a frame), then let go; keepDown leaves them down for the next sequence to change.</summary>
        static IEnumerator HoldKeys(Key[] keys, float seconds, bool keepDown = false)
        {
            if (UnityEngine.InputSystem.Keyboard.current is not UnityEngine.InputSystem.Keyboard kb) yield break;
            float until = Time.realtimeSinceStartup + seconds;
            do { UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(keys)); yield return null; } while (Time.realtimeSinceStartup < until);
            if (keepDown) yield break;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState()); yield return null;
        }
        /// <summary>A key held while the going-loud card it opens wants it (F's draw at Alert), at least `min` seconds.</summary>
        IEnumerator HoldKey(Key key, float min)
        {
            if (UnityEngine.InputSystem.Keyboard.current is not UnityEngine.InputSystem.Keyboard kb) yield break;
            float t0 = Time.realtimeSinceStartup;
            while (true)
            {
                float held = Time.realtimeSinceStartup - t0; if (!(held < min || held < 2.5f && h.Loud != null && h.Loud.Source != "wait")) break;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(key)); yield return null;
            }
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState()); yield return null;
        }
        /// <summary>Both buttons down together (panic) for `seconds`, the going-loud card's hold included.</summary>
        static IEnumerator Both(Vector2 sp, float seconds)
        {
            MouseState0(sp, false, false); yield return null; MouseState0(sp, true, true); yield return null;
            float until = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < until) { MouseState0(sp, true, true); yield return null; }
            MouseState0(sp, false, false); yield return null;
        }

        // ---------------------------------------------------------------- the log

        static string Say(HumanStep s) => s == null ? "" : $"{s.op} {s.act}{(s.target.Length > 0 ? " " + s.target : s.place.Length > 0 ? " " + s.place : s.xy ? $" ({s.x / 100f:0.#}, {s.y / 100f:0.#})" : "")}";

        void Noise(string kind, HumanStep s, string detail)
        {
            var hr = cur?.Human; if (hr == null) return; var g = h.Game;
            hr.Counts[kind] = (hr.Counts.TryGetValue(kind, out var n) ? n : 0) + 1;
            if (hr.Events.Count < HumanMaxEvents) hr.Events.Add(new HEvent { T = Time.realtimeSinceStartup - rowStart, Tick = g?.Tick ?? 0, Kind = kind, Route = hs?.Route ?? "", Step = s?.i ?? -1, Op = s?.op ?? "", Act = s?.act ?? "", Target = s == null ? "" : s.target.Length > 0 ? s.target : s.place, Detail = detail });
            if (kind is not ("skip" or "panel-direct" or "backed-off" or "tp-check")) lastNoise = (Time.realtimeSinceStartup, $"{kind}: {(s != null ? $"step {s.i} {Say(s)}" : "")}{(detail.Length > 0 ? $" ({detail})" : "")}");
        }

        /// <summary>The indecision just before an anomaly (within HumanNoiseS), for its report line.</summary>
        string NoiseNow() { if (lastNoise is not { } ln) return ""; float dt = Time.realtimeSinceStartup - ln.t; return dt <= HumanNoiseS ? $"{ln.text}, {dt:0.0} s before" : ""; }

        void WriteHuman(StringBuilder sb, HumanRes hr)
        {
            var traces = new List<string>(hr.Traces); if (hs != null && hr.Routes.Count > 0 && hr.Routes[^1] == hs.Route && traces.Count < hr.Routes.Count) traces.Add(hs.Trace);
            sb.Append(", \"human\": {\"route\": \"").Append(Esc(hr.Route)).Append("\", \"group\": \"").Append(Esc(hr.Group)).Append("\", \"thirdPerson\": ").Append(hr.ThirdPerson ? "true" : "false").Append(", \"routes\": [").Append(string.Join(", ", hr.Routes.Select(r => "\"" + Esc(r) + "\""))).Append("], \"profile\": \"").Append(Esc(hr.Profile))
              .Append("\", \"steps\": ").Append(hr.Steps).Append(", \"done\": ").Append(hr.Done).Append(", \"skipped\": ").Append(hr.Skipped).Append(", \"trace\": \"").Append(Esc(string.Join(" > ", traces))).Append('"')
              .Append(", \"counts\": {").Append(string.Join(", ", hr.Counts.Select(kv => $"\"{Esc(kv.Key)}\": {kv.Value}"))).Append("}, \"events\": [");
            for (int i = 0; i < hr.Events.Count; i++)
            {
                var e = hr.Events[i];
                sb.Append(i == 0 ? "\n    " : ",\n    ").Append("{\"t\": ").Append(F(e.T)).Append(", \"tick\": ").Append(e.Tick).Append(", \"clock\": \"").Append(HandoverHost.MissionClock(e.Tick)).Append("\", \"kind\": \"").Append(Esc(e.Kind)).Append("\", \"route\": \"").Append(Esc(e.Route))
                  .Append("\", \"step\": ").Append(e.Step).Append(", \"op\": \"").Append(Esc(e.Op)).Append("\", \"act\": \"").Append(Esc(e.Act)).Append("\", \"target\": \"").Append(Esc(e.Target)).Append("\", \"detail\": \"").Append(Esc(e.Detail)).Append('"');
                if (e.Kind == "given") sb.Append(", \"gave\": ").Append(!e.Gave ? "null" : $"{{\"op\": \"{Esc(e.GaveOp)}\", \"act\": \"{Esc(e.GaveAct)}\", \"target\": \"{Esc(e.GaveTarget)}\", \"to\": \"{Esc(e.GaveTo)}\", \"ok\": {(e.GaveOk ? "true" : "false")}, \"code\": \"{Esc(e.GaveCode)}\"}}");
                sb.Append('}');
            }
            sb.Append(hr.Events.Count > 0 ? "\n  ]}" : "]}");
        }
    }
}
