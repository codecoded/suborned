using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The getaway (brief 018 §2, §6; Sky 2026-10-18: "I want to get my team into the car, and get away"). Board and Go replace Extract:
    /// Board is any standing agent's order on the getaway car (1 s, +3 s carrying, the carried first); a move on a boarded agent steps them out
    /// (the engine's StepOut); Go is one order for the cell from the car's card, refused while the car is held (the engine's GoQuery names the
    /// holders). Go commits with the going-loud card's 0.5 s hold when anyone would be left behind (LeftBehind is the card's preview), else at
    /// once. Everything here reads the engine (CarHolders, LeftBehind, GoQuery, CarWatcher, the site's exit lane) and gives engine orders, so a
    /// session replays as played. The look is Glyph's, the words Fable's (placeholders in PlayText until text.json carries board and go).</summary>
    public sealed partial class HandoverHost
    {
        bool IsGetaway(Target t) => t != null && t.Kind == "car" && t.Key == "getaway";
        /// <summary>Brief 059 K6: where the getaway car is drawn now (its stand, or along its Drive), as a world point.</summary>
        public Vector3 GetawayWorld => Game == null ? Vector3.zero : frame.World(Game.Range ? Game.S.At(Game.S.GetawayCar) : Game.GetawayCarPos());

        /// <summary>Board for an agent: its time (1 s, 4 s carrying Vale or a downed agent), the carried named.</summary>
        Intent BoardIntent(OpId k)
        {
            var g = Game; var op = g.Ops[(int)k]; var R = g.R;
            bool carrying = op.Carrying.HasValue || (k == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer);
            int secs = R.BoardS + (carrying ? R.CarryS : 0); // seconds (rulebook cell.board.s, carry.s)
            var i = new Intent { Slot = -1, Label = "Board", Order = new Order { Op = Op.Name(k), Do = "board", Target = "car" } };
            i.Time = $"{secs} s"; i.Usd = "$0"; i.Trail = "T 0"; i.Alert = "A 0";
            i.Outcome = k == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer ? $"with {ValeWord}" : op.Carrying.HasValue ? "with the " + Op.Name(op.Carrying.Value) : "";
            return i;
        }

        /// <summary>Go: one order for the cell. The outcome names who stays (each with a fate) and "+1 T" when security would see it leave.</summary>
        Intent GoIntent(OpId by)
        {
            var g = Game; var i = new Intent { Slot = -1, Label = "Go", Order = new Order { Op = Op.Name(by), Do = "go", Target = "car" } };
            i.Time = "now"; i.Usd = "$0"; i.Alert = "A 0";
            string seen = ""; Guard("car watcher", () => seen = g.CarWatcher());
            i.Trail = seen.Length > 0 ? $"T +{g.R.WitnessTrail}" : "T 0";
            i.Outcome = g.Vale == ValeAt.Car || ValeClimbingIn ? "win" : g.Lent ? "clear" : "withdraw"; // brief 096 (engine 0.42.0): once Popovic's car is lent, ours leaves with whoever is aboard (gets the rest clear), never a withdraw
            i.Label = GoLine(seen.Length > 0); return i;
        }

        /// <summary>The Go card's one line (Fable, less.go): "Go", or "Go · leaves Tech (down), Scout", each captured name followed by its why, and
        /// "+1 T if seen" when security would see the car leave.</summary>
        public string GoLine(bool seen)
        {
            var t = Text; var left = LeftBehindNow();
            string line = left.Count == 0 ? t?.GoNone ?? "Go" : (t?.GoLeaves ?? "Go · leaves {list}").Replace("{list}", string.Join(", ", left.ConvertAll(x => AgentNames[(int)Op.Parse(x.op)] + (x.fate == "captured" && t != null && t.GoWhy.TryGetValue(x.why, out var w) ? $" ({w})" : ""))));
            if (seen && Game != null) line += " · " + (t?.GoSeen ?? "+{trail} T if seen").Replace("{trail}", Game.R.WitnessTrail.ToString());
            return line;
        }

        /// <summary>Why an agent can't Board now (the engine's codes; distance is a walk, never a lock).</summary>
        Answer BoardLock(OpId k)
        {
            var g = Game; var op = g.Ops[(int)k];
            Answer No(string code, string why) => new Answer { ok = false, code = code, why = why, m = -1, cm = -1, usd = -1, tick = -1 };
            // brief 067 C1 (Glyph): the engine's Board tests in its order (GameGetaway.Board: running, tethered, carrying a body), then the host's own
            if (op.Running) return No("board-running", "running: stop first");
            if (k == OpId.Tech && g.LoopOn && !g.R.LoopRuns) return No("board-tethered", "the Tech is tethered to the cabinet while the loop holds"); // the engine's Tethered (a loop with a tether; Loop v2 runs on its own)
            if (op.CarryBody.Length > 0) return No("already-carrying", "carrying a body");
            if (op.Down) return No("agent-down", "down");
            if (op.Inside) return No("enforcer-inside", "inside the Office");
            if (op.InCar) return No("in-car", "already in the car");
            if (g.CarDisabled("car:getaway")) return No("car-disabled", "our car is shot out"); // engine 0.53.0 (brief 105): a shot-out car seats nobody
            return new Answer { ok = true, code = "", why = "" };
        }

        /// <summary>Where a cell order given with no click shows its refusal: over the car a Go is on (ours, or Popovic's when lent), else nowhere (the HUD's own place).</summary>
        Vector3? CellOrderWorld(Order o) => o.Do != "go" || Game == null ? null : LentOrder(o.Target) ? LentWorld : GetawayWorld;
        /// <summary>The engine's own answer to Go now (car-held names the holders in `ids`; car-empty).</summary>
        public Answer GoLock() { Answer a = new Answer { ok = true, code = "", why = "" }; Guard("go query", () => a = Game.CarDisabled("car:getaway") && !Game.Range ? new Answer { ok = false, code = "car-disabled", why = "our car is shot out", m = -1, cm = -1, usd = -1, tick = -1 } : Game.GoQuery()); /* engine 0.53.0: Go on a shot-out car is refused car-disabled (GameGetaway.Go) */ a.code ??= ""; a.why ??= ""; return a; }

        /// <summary>Who a Go now would leave, with fate and why (the engine's LeftBehind: read-only).</summary>
        public List<(string op, string fate, string why)> LeftBehindNow()
        {
            var l = new List<(string op, string fate, string why)>(); if (Game == null) return l;
            Guard("left behind", () => l = Game.LeftBehind()); return l;
        }

        /// <summary>The Go card is up (the panel on the car with its Go row, or the commit hold): the roster flags who would stay.</summary>
        public bool GoCardOpen => Game != null && (Loud?.Left != null || (Panel != null && IsCellCar(Panel.T) && Panel.Rows.Exists(r => r.I.Order?.Do == "go")));

        /// <summary>An agent is in the car (the roster's in-car badge, the car marker's seats).</summary>
        public bool InCar(int k) => Game != null && Game.Ops[k].InCar;
        /// <summary>The car marker's five seat pips: Vale, then the Scout, Fixer, Tech and Enforcer; true when in the car.</summary>
        public bool[] Seats() { var s = new bool[5]; if (Game == null) return s; s[0] = Game.Vale == ValeAt.Car; for (int k = 0; k < 4; k++) s[k + 1] = Game.Ops[k].InCar && !Game.Ops[k].InLent; return s; }
        /// <summary>Brief 067 C3 (Glyph): the seat pips' states this frame: filled (Seats), pending (a Board ordered and not landed: the host's queued
        /// Board, the engine's walk-in to it, or getting in; Vale's with the Enforcer's when he carries him), and when each seat last filled (the
        /// 0.3 s landing beat). Read once a frame by the HUD.</summary>
        public (bool[] filled, bool[] pending, float[] filledAt) SeatState()
        {
            var f = Seats(); var p = new bool[5]; if (Game == null) return (f, p, seatFilledAt);
            for (int k = 0; k < 4; k++) { var op = Game.Ops[k]; p[k + 1] = !op.InCar && !op.Down && (pending[k]?.Do == "board" || op.Act?.Do == "board" || op.Busy?.Do == "board"); }
            p[0] = !f[0] && p[(int)OpId.Enforcer + 1] && Game.Vale == ValeAt.WithEnforcer;
            float now = Time.unscaledTime; for (int i = 0; i < 5; i++) { if (f[i] && !seatWas[i]) seatFilledAt[i] = now; seatWas[i] = f[i]; }
            return (f, p, seatFilledAt);
        }
        readonly bool[] seatWas = new bool[5]; readonly float[] seatFilledAt = { -9f, -9f, -9f, -9f, -9f };
        /// <summary>Engine 0.33.1 (rulebook car.footprint; Suture's 0.2.122 done-check "nobody but a boarding agent is inside any footprint"): the
        /// drawn figures whose feet stand inside a standing car's body now (Game.StandingCars, Game.CarBodyCm 0), as "key@car" each; agents in
        /// the car and a carried body are not drawn and never count. A host read for the proofs.</summary>
        public List<string> FiguresInCars()
        {
            var l = new List<string>(); var g = Game; if (g == null || views == null || g.Range) return l;
            List<Game.StandingCar> cars = null; Guard("standing cars", () => cars = g.StandingCars()); if (cars == null || cars.Count == 0) return l;
            foreach (var (key, feet, skip) in views.FigureFeet())
            {
                if (skip) continue; var e = frame.Engine(feet);
                foreach (var c in cars) { int d = -1; Guard("car body", () => d = g.CarBodyCm(c.Id, e)); if (d == 0) { l.Add($"{key}@{c.Id}"); break; } }
            }
            return l;
        }

        /// <summary>Brief 067 C3: a Board is ordered and not landed for some agent (the pips show with it).</summary>
        public bool AnyBoardPending { get { if (Game == null) return false; for (int k = 0; k < 4; k++) { var op = Game.Ops[k]; if (!op.InCar && (pending[k]?.Do == "board" || op.Act?.Do == "board" || op.Busy?.Do == "board")) return true; } return false; } }

        /// <summary>Anyone in the car, or climbing in (engine 0.43.0 G1): the car marker offers Go.</summary>
        public bool GoAvailable => Game != null && !Game.Over && (Game.AnyInCar || Climbers("car").Count > 0); // G1: one climbing in counts as in

        /// <summary>Go given: with anyone left behind, the going-loud card's 0.5 s hold commits it (no pause); else at once.</summary>
        void GoGive(OpId by, Vector2? screen, string source, string car = "")
        {
            if (LentOrder(car)) { LentGoGive(by, screen); return; } // brief 096: Popovic's car has its own Go
            var lk = GoLock();
            var at = GetawayWorld;
            if (!lk.ok) { Pop(ShortOf(new Order { Op = Op.Name(by), Do = "go" }, lk) ?? lk.why, screen, screen.HasValue ? null : at, Feedback.Refused, 2f, PopIcon(lk.code ?? ""), LockLine(new Order { Op = Op.Name(by), Do = "go" }, lk) ?? lk.why); if (lk.code == "car-held") HolderPulseAt = Time.unscaledTime; else PulseBoxers(lk); return; } // brief 098 step 1b: boxed in by traffic, the car named pulses
            var order = new Order { Op = Op.Name(by), Do = "go" };
            var left = LeftBehindNow();
            if ((Game.Lent ? left.Exists(x => x.fate == "captured") : left.Count > 0) && Loud == null) // brief 096: while lent, only a capture asks for the hold (the rest stay on)
            {
                string seen = ""; Guard("car watcher", () => seen = Game.CarWatcher());
                var card = new LoudCard { Loud = true, Why = "go-left-behind", AlertTo = Game.Alert, ReportedBy = seen.Length > 0 ? "seen" : "none" };
                Loud = new Gate { Card = card, Source = source, What = "Go", Start = Time.unscaledTime, At = at, Gui = screen.HasValue ? new Vector2(screen.Value.x, (cam ? cam.pixelHeight : Screen.height) - screen.Value.y) : Mouse, Left = left, SeenLeaving = seen.Length > 0 };
                Loud.Orders.Add(order);
                Status = $"Go leaves {left.Count}: hold {SourceName(source)} {HoldSeconds:0.0} s to commit, release to back off";
                return;
            }
            Issue(order, screen, at);
        }

        /// <summary>Engine 0.33.0 (the gate, Motive M059-S1.7 "Rivals coming · 18 s"): while the rival car holds Go (its riders still to come), the
        /// seconds to the first rider's step-out (GoQuery's tick), else -1 (no car:rivals hold, or the car still driving with no step-out set).
        /// Read once a tick.</summary>
        public int RivalsComingS()
        {
            var g = Game; if (g == null || g.Range) return -1;
            if (rivalsComingTick != g.Tick) { rivalsComingTick = g.Tick; var a = GoLock(); rivalsComing = a.code == "car-held" && (a.ids ?? "").Contains("car:rivals") && a.tick >= 0 ? Math.Max(0, (a.tick - g.Tick + Game.Hz - 1) / Game.Hz) : -1; }
            return rivalsComing;
        }
        int rivalsComingTick = -1, rivalsComing = -1;

        /// <summary>When a refused Go last pulsed the holder marks (§6.5).</summary>
        public float HolderPulseAt { get; set; } = -10f;

        /// <summary>The holders of the car now, by the engine's CarHolders (ids in id order), each at its world point.</summary>
        public List<(string id, Vector3 at)> CarHolders()
        {
            var list = new List<(string, Vector3)>(); var g = Game; if (g == null) return list;
            List<string> ids = null; Guard("car holders", () => ids = g.CarHolders()); if (ids == null) return list;
            foreach (var id in ids)
            {
                Vector3? w = null;
                var h = g.Hostiles.Find(x => x.Id == id); if (h != null) w = frame.World(h.Pos(g.Tick));
                else { Pt? p = null; Guard("holder pos", () => p = g.PosOf(id)); if (p.HasValue) w = frame.World(p.Value); }
                if (w.HasValue) list.Add((id, w.Value));
            }
            return list;
        }

        /// <summary>The getaway's exit lane (site getaway.exit): its centre line in world points and its half width (m); empty when the site has none.</summary>
        public (List<Vector3> line, float half) ExitLane()
        {
            var l = new List<Vector3>(); var g = Game; if (g == null) return (l, 0f);
            foreach (var p in g.CurStand.Exit) l.Add(frame.World(p)); // brief 059 K6: the current stand's lane
            return (l, g.CurStand.ExitHalfCm / 100f);
        }

        /// <summary>The Enforcer has Vale and is outside the car, with no Board given or walked for: the car marker calls him.</summary>
        public bool BoardAvailable { get { var g = Game; var op = g?.Ops[(int)OpId.Enforcer]; return op != null && g.Vale == ValeAt.WithEnforcer && !op.Down && !op.InCar && op.Busy?.Do != "board" && pending[(int)OpId.Enforcer]?.Do != "board"; } }

        /// <summary>The car's rows on its panel: Board for each selected agent not in the car, then Go once anyone is in (or greyed with its reason).</summary>
        void CarRows(ActionPanel pn, List<int> order, Target t)
        {
            if (IsLentCar(t)) { LentRows(pn, order, t); return; } // brief 096: Popovic's car, lent
            foreach (var k in order)
            {
                var op = Game.Ops[k]; if (op.InCar) continue;
                var v = BoardIntent((OpId)k); var aw = ActPlace((OpId)k, v.Order, t);
                if (aw != null) { v.Walk = $"walks {aw.Cm / 100} m ({aw.Seconds} s) to the getaway car, then"; v.WalkS = aw.Seconds; }
                Words(v, t); var lk = BoardLock((OpId)k);
                pn.Rows.Add(new PanelRow { By = (OpId)k, Slot = -1, I = v, Verb = v.Name, Lock = lk, Reason = lk.ok ? "" : ShortOf(v.Order, lk) ?? lk.why });
            }
            var by = order.Count > 0 ? (OpId)order[0] : Agent;
            var gv = GoIntent(by); Words(gv, t); var gl = GoLock(); // the row is the Go card's one line
            if (gv.Outcome == "withdraw" && Text?.GoNoVale is { Length: > 0 } nv) gv.Does = nv; // Fable 067 less.go.noVale: before Vale is aboard, Go calls the attempt off (the engine's Leave: Withdrawn)
            GoWaitWords(gv, "car"); gv.Name = gv.Label; // engine 0.43.0 (G1): someone climbing in, the Go goes once he is in (HostGoWaits)
            var goRow = new PanelRow { By = by, Slot = -1, I = gv, Verb = gv.Name, Lock = gl, Reason = gl.ok ? "" : ShortOf(gv.Order, gl) ?? gl.why };
            if (gl.ok) pn.Rows.Insert(0, goRow); else pn.Rows.Add(goRow); // a free Go tops the card
            CarDriveRows(pn, order);
            StepOutRows(pn, false);
            foreach (var r in pn.Rows) { r.Available = r.Reason.Length == 0; if (!r.Available) r.Unlock = LockLine(r.I.Order, r.Lock) ?? ""; }
        }

        /// <summary>Marks a Step out way (Intent.How; the ring ranks it and gives it to every agent aboard: DoersOf).</summary>
        public const string StepOutHow = "step-out";

        /// <summary>Brief 067 C (Sky on 0.2.122: "very annoying getting them into the car and then trying to make the car go"): once anyone is aboard,
        /// the car's own ways include Drive to each of its other stands, by the agent who would drive (a selected agent aboard, else the first
        /// aboard), so the car is one click for the whole getaway; the stands' footprints on the ground stay targets too. No Drive once the final
        /// fight has started (drive-fight), as on the stands.</summary>
        void CarDriveRows(ActionPanel pn, List<int> order)
        {
            var g = Game; if (g == null || g.Range || !g.R.DriveCarOn || g.S.Stands.Count < 2) return;
            int drv = -1; foreach (var k in order) if (g.Ops[k].InCar && !g.Ops[k].InLent && !g.Ops[k].Down) { drv = k; break; }
            if (drv < 0) for (int k = 0; k < 4; k++) if (g.Ops[k].InCar && !g.Ops[k].InLent && !g.Ops[k].Down) { drv = k; break; }
            if (drv < 0) return;
            foreach (var st in g.S.Stands)
            {
                if (st == g.CurStand && !g.GetawayDriving) continue; // the car is there already (drive-here): never a way
                var v = DriveIntent((OpId)drv, st); var stName = Text?.TargetName("stand-" + st.Id, "the " + st.Id) ?? st.Id;
                v.Name = Text?.WithTarget("drive-car", stName) ?? $"Drive to {stName}"; v.Label = v.Name; v.Lead = "arrive"; // Glyph 067 C: Drive's icon is arrive on the car's ring (three cars would read alike)
                var lk = DriveLock((OpId)drv, st);
                pn.Rows.Add(new PanelRow { By = (OpId)drv, Slot = -1, I = v, Verb = v.Name, Lock = lk, Reason = lk.ok ? "" : ShortOf(v.Order, lk) ?? lk.why });
            }
        }

        /// <summary>Brief 067 C2 (Glyph): the group Board's pill: one number, the slowest doer's walk + act (Board 1 s, 4 s carrying), $0, 0 T, no
        /// Alert; a copy (the panel's own rows are untouched).</summary>
        Intent GroupBoardIntent(ContextPlan plan, int way, List<int> doers)
        {
            var i0 = plan.Row(way).I; int worst = 0;
            foreach (var k in doers) { var r = RowFor(plan, way, k); if (r?.I == null) continue; int t = (int)ContextRank.Num(r.I.Time) + Math.Max(0, r.I.WalkS); if (t > worst) worst = t; }
            return new Intent { Order = i0.Order, Slot = i0.Slot, Label = i0.Label, Name = i0.Name, Lead = i0.Lead, Does = i0.Does, Time = $"{worst} s", Usd = i0.Usd, Trail = i0.Trail, Alert = i0.Alert, Outcome = i0.Outcome, WalkS = 0 };
        }

        /// <summary>Brief 067 C: the car's one obvious way for the hold and the pill: Board while a selected agent is still outside (every one who
        /// can: DoersOf), then Go once Vale is aboard (the win; Go without him is a withdraw and never the obvious way), else Drive to the first
        /// other stand. -1 when none is open (the pill then shows the lock).</summary>
        int CarMainWay(ContextPlan plan)
        {
            // Glyph 067 C5c (Rev 2) and GDD §10 ("the getaway car: Board for the selection, Go once aboard"): Board while a selected agent is outside
            // and able (every one: DoersOf); else Go once Vale is aboard and the car is free (the Go card's 0.5 s hold still commits it when anyone
            // would be left: GoGive); else Drive when exactly one stand is open, Vale is not aboard and the car is free; else nothing (a click opens
            // the ring: with two stands open the ring decides). Go before Vale (a withdraw) is never a hold
            int b = -1, d = -1, go = -1, drives = 0;
            foreach (var w in plan.Choice.Order)
            {
                var r = plan.Row(w); if (r?.Available != true) continue; var o = r.I?.Order; if (o == null) continue;
                if (o.Do == "board" && b < 0 && plan.Givers[w].Exists(x => Selected[(int)x])) b = w;
                if (o.Do == "go" && go < 0 && r.I.Outcome is "win" or "clear") go = w; // brief 096: our car's Go once Popovic is lent gets the rest clear
                if (o.Do == "drive-car") { drives++; if (d < 0) d = w; }
            }
            if (b >= 0) return b;
            if (go >= 0) return go;
            return drives == 1 && Game.Vale != ValeAt.Car && !Game.Lent ? d : -1;
        }

        /// <summary>Brief 067 C4 (Glyph): after a squad Board, each selected agent it did not take (but one already aboard) says why on its portrait,
        /// the why chip: the cause's own icon and two words for 3 s (WhyChipOf).</summary>
        void BoardRefusals(ContextPlan plan, List<int> given)
        {
            if (plan?.Pn == null) return;
            for (int k = 0; k < 4; k++)
            {
                if (!Selected[k] || given.Contains(k) || Game.Ops[k].InCar) continue;
                var lk = BoardLock((OpId)k); if (lk.ok) continue;
                boardRefused[k] = lk.code ?? ""; boardRefusedAt[k] = Time.unscaledTime; BoardRefused++;
            }
        }
        readonly string[] boardRefused = { "", "", "", "" }; readonly float[] boardRefusedAt = { -99f, -99f, -99f, -99f };

        /// <summary>Brief 067 C4 (Glyph): agent k's why chip now: the cause's kit icon and two words (Fable's less.done.board.&lt;code&gt;, else
        /// less.done.fire.&lt;code&gt; for a squad fire refusal), the newest refusal within FireRefusalS (3 s, faded over 0.3 s after); ("", "", 0)
        /// when none, or when the agent has had an order since (the busy chip takes the slot).</summary>
        public (string icon, string words, float alpha) WhyChipOf(int k)
        {
            if (Game == null || k < 0 || k > 3) return ("", "", 0f);
            float now = Time.unscaledTime, ab = now - boardRefusedAt[k], af = now - fireRefusedAt[k];
            bool board = boardRefused[k].Length > 0 && ab < FireRefusalS + 0.3f, fire = fireRefused[k].Length > 0 && af < FireRefusalS + 0.3f;
            if (!board && !fire) return ("", "", 0f);
            bool useBoard = board && (!fire || ab <= af); string code = useBoard ? boardRefused[k] : fireRefused[k]; float age = useBoard ? ab : af;
            var op = Game.Ops[k]; if (op.InCar || op.Busy != null || op.Act != null || pending[k] != null) return ("", "", 0f);
            string words = useBoard ? Text?.DoneWord("board." + code, null) ?? code.Replace('-', ' ') : FireRefusalWord(k) is { Length: > 0 } fw ? fw : Text?.DoneWord("fire." + code, null) ?? code.Replace('-', ' '); // Fable 067: less.done.board.* (down, inside, stop first, hands full, looping, busy, too far, mission over, restart)
            string icon = code switch { "agent-down" => "s_downed", "enforcer-inside" => "s_inside", "already-carrying" or "fire-carrying" or "fire-carrying-vale" or "carry-vale" => "s_carrying", "board-tethered" => "s_looping", "board-running" => "run", _ => "refused" };
            return (icon, words, Mathf.Clamp01((FireRefusalS + 0.3f - age) / 0.3f));
        }

        /// <summary>The proof reads them: squad Boards given (every selected agent who could), and the refusals said.</summary>
        public int SquadBoards { get; private set; }
        public int BoardRefused { get; private set; }

        /// <summary>A right click on the car with more than one agent selected boards each of them (brief 018 §2: "boards each op in turn"); the
        /// ones that can't say why. True when it acted.</summary>
        bool BoardSelection(Target t, Vector2 screen)
        {
            int n = 0; for (int k = 0; k < 4; k++) if (Selected[k]) n++;
            if (n < 2 || !IsGetaway(t)) return false;
            bool any = false;
            for (int k = 0; k < 4; k++)
            {
                if (!Selected[k] || Game.Ops[k].InCar) continue;
                var lk = BoardLock((OpId)k); if (!lk.ok) continue;
                Give((OpId)k, BoardIntent((OpId)k).Order, t, screen); any = true;
            }
            return any;
        }
    }
}
