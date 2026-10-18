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

        /// <summary>Board for an agent: its time (1 s, 4 s carrying Vale or a downed agent), the carried named.</summary>
        Intent BoardIntent(OpId k)
        {
            var g = Game; var op = g.Ops[(int)k]; var R = g.R;
            bool carrying = op.Carrying.HasValue || (k == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer);
            int secs = R.BoardS + (carrying ? R.CarryS : 0); // seconds (rulebook cell.board.s, carry.s)
            var i = new Intent { Slot = -1, Label = "Board", Order = new Order { Op = Op.Name(k), Do = "board", Target = "car" } };
            i.Time = $"{secs} s"; i.Usd = "$0"; i.Trail = "T 0"; i.Alert = "A 0";
            i.Outcome = k == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer ? "with Vale" : op.Carrying.HasValue ? "with the " + Op.Name(op.Carrying.Value) : "";
            return i;
        }

        /// <summary>Go: one order for the cell. The outcome names who stays (each with a fate) and "+1 T" when security would see it leave.</summary>
        Intent GoIntent(OpId by)
        {
            var g = Game; var i = new Intent { Slot = -1, Label = "Go", Order = new Order { Op = Op.Name(by), Do = "go", Target = "car" } };
            i.Time = "now"; i.Usd = "$0"; i.Alert = "A 0";
            string seen = ""; Guard("car watcher", () => seen = g.CarWatcher());
            i.Trail = seen.Length > 0 ? $"T +{g.R.WitnessTrail}" : "T 0";
            i.Outcome = g.Vale == ValeAt.Car ? "win" : "withdraw";
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
            if (op.InCar) return No("in-car", "already in the car");
            if (op.Down) return No("agent-down", "down");
            if (op.Running) return No("board-running", "running: stop first");
            if (k == OpId.Tech && g.LoopOn) return No("board-tethered", "tied to the cabinet while the loop holds");
            if (op.CarryBody.Length > 0) return No("already-carrying", "carrying a body");
            return new Answer { ok = true, code = "", why = "" };
        }

        /// <summary>The engine's own answer to Go now (car-held names the holders in `ids`; car-empty).</summary>
        public Answer GoLock() { Answer a = new Answer { ok = true, code = "", why = "" }; Guard("go query", () => a = Game.GoQuery()); a.code ??= ""; a.why ??= ""; return a; }

        /// <summary>Who a Go now would leave, with fate and why (the engine's LeftBehind: read-only).</summary>
        public List<(string op, string fate, string why)> LeftBehindNow()
        {
            var l = new List<(string op, string fate, string why)>(); if (Game == null) return l;
            Guard("left behind", () => l = Game.LeftBehind()); return l;
        }

        /// <summary>The Go card is up (the panel on the car with its Go row, or the commit hold): the roster flags who would stay.</summary>
        public bool GoCardOpen => Game != null && (Loud?.Left != null || (Panel != null && IsGetaway(Panel.T) && Panel.Rows.Exists(r => r.I.Order?.Do == "go")));

        /// <summary>An agent is in the car (the roster's in-car badge, the car marker's seats).</summary>
        public bool InCar(int k) => Game != null && Game.Ops[k].InCar;
        /// <summary>The car marker's five seat pips: Vale, then the Scout, Fixer, Tech and Enforcer; true when in the car.</summary>
        public bool[] Seats() { var s = new bool[5]; if (Game == null) return s; s[0] = Game.Vale == ValeAt.Car; for (int k = 0; k < 4; k++) s[k + 1] = Game.Ops[k].InCar; return s; }
        /// <summary>Anyone in the car: the car marker offers Go.</summary>
        public bool GoAvailable => Game != null && !Game.Over && Game.AnyInCar;

        /// <summary>Go given: with anyone left behind, the going-loud card's 0.5 s hold commits it (no pause); else at once.</summary>
        void GoGive(OpId by, Vector2? screen, string source)
        {
            var lk = GoLock();
            var at = frame.World(Game.S.At(Game.S.GetawayCar));
            if (!lk.ok) { Pop(ShortOf(new Order { Op = Op.Name(by), Do = "go" }, lk) ?? lk.why, screen, screen.HasValue ? null : at, Feedback.Refused, 2f, PopIcon(lk.code ?? ""), LockLine(new Order { Op = Op.Name(by), Do = "go" }, lk) ?? lk.why); if (lk.code == "car-held") HolderPulseAt = Time.unscaledTime; return; }
            var order = new Order { Op = Op.Name(by), Do = "go" };
            var left = LeftBehindNow();
            if (left.Count > 0 && Loud == null)
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
            foreach (var p in g.S.ExitLane) l.Add(frame.World(p));
            return (l, g.S.ExitHalfCm / 100f);
        }

        /// <summary>The Enforcer has Vale and is outside the car, with no Board given or walked for: the car marker calls him.</summary>
        public bool BoardAvailable { get { var g = Game; var op = g?.Ops[(int)OpId.Enforcer]; return op != null && g.Vale == ValeAt.WithEnforcer && !op.Down && !op.InCar && op.Busy?.Do != "board" && pending[(int)OpId.Enforcer]?.Do != "board"; } }

        /// <summary>The car's rows on its panel: Board for each selected agent not in the car, then Go once anyone is in (or greyed with its reason).</summary>
        void CarRows(ActionPanel pn, List<int> order, Target t)
        {
            foreach (var k in order)
            {
                var op = Game.Ops[k]; if (op.InCar) continue;
                var v = BoardIntent((OpId)k); var aw = ActPlace((OpId)k, v.Order, t);
                if (aw != null) { v.Walk = $"walks {aw.Cm / 100} m ({aw.Seconds} s) to the getaway car, then"; v.WalkS = aw.Seconds; }
                Words(v, t); var lk = BoardLock((OpId)k);
                pn.Rows.Add(new PanelRow { By = (OpId)k, Slot = -1, I = v, Verb = v.Name, Lock = lk, Reason = lk.ok ? "" : ShortOf(v.Order, lk) ?? lk.why });
            }
            var by = order.Count > 0 ? (OpId)order[0] : Agent;
            var gv = GoIntent(by); var goName = gv.Label; Words(gv, t); gv.Name = goName; var gl = GoLock(); // the row is the Go card's one line
            var goRow = new PanelRow { By = by, Slot = -1, I = gv, Verb = gv.Name, Lock = gl, Reason = gl.ok ? "" : ShortOf(gv.Order, gl) ?? gl.why };
            if (gl.ok) pn.Rows.Insert(0, goRow); else pn.Rows.Add(goRow); // a free Go tops the card
            foreach (var r in pn.Rows) { r.Available = r.Reason.Length == 0; if (!r.Available) r.Unlock = LockLine(r.I.Order, r.Lock) ?? ""; }
        }

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
