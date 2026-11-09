using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Sight is the Scout's skill (Sky 2026-10-16; Glyph's review §3.4; Keel's GameSight): no cone by default. One learned cone at a time,
    /// the focused watcher's: the one under the cursor, else the action panel's target, else the one the Scout is watching (or has just watched,
    /// until the Scout's next order). Alt shows every cone the cell has learned. A watcher noticing an agent while the cell doesn't know its sight
    /// gets a "?" over it (Game.Detection: Known false). The fans are the engine's (Game.ConeFan, cached per pose, read-only), so this never
    /// touches the run: a replay is untouched.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The watchers noticing an agent now whose sight the cell doesn't know: where to put the "?" (world), refreshed each tick.</summary>
        public readonly List<Vector3> Unknowns = new();
        /// <summary>The watcher whose cone is drawn now (null: none), and whether Alt is showing every learned cone.</summary>
        public string FocusWatcher { get; private set; }
        public bool Peeking { get; private set; }
        /// <summary>Alt held by a script (the budget shots' Alt moment): the same as holding the key.</summary>
        public bool PeekLatch { get; set; }
        /// <summary>The eye button on the scope's rim (Sky 2026-11-07, the cones round; never key-only): latched on, every learned cone shows, as Alt
        /// shows them (rule 6) and in the same merged look; Alt's other detail (prices' sums, words) stays on Alt. A view setting: the run never reads it.</summary>
        public bool ConesAll { get; set; }
        public void ConesAllToggle() => ConesAll = !ConesAll;
        /// <summary>Per agent (OpId order): the learned watchers that see it now (the engine's Detection, sight known), a combatant not counted while a
        /// fight is on (rule 3 is off for it then): the HUD marks a selected agent seen by two or more with an eye and the count, so a crew whose cones
        /// merge into one shape still says how many see (Sky 2026-11-07: a crew counts once). Refreshed after each step.</summary>
        public readonly int[] SeenBy = new int[4];
        /// <summary>Seconds Alt must be held before the detail layer shows (Glyph, brief 016 review §5).</summary>
        public const float PeekDelay = 0.25f;
        float peekHeldSince = -1f;
        readonly List<Fan> shownFans = new();
        string lastWatch; int lastWatchOrders = -1;

        /// <summary>A target's watcher id (camera:&lt;id&gt;, guard, patrol, rivals, security-&lt;n&gt;), else null.</summary>
        string WatcherOf(Target t)
        {
            if (t == null) return null;
            switch (t.Kind)
            {
                case "camera": return "camera:" + t.Key;
                case "patrol": return "patrol";
                case "rivals": return HostViews.IsCrewId(t.Key) ? t.Key : null; // brief 067 F7 (Glyph): a member is his own watcher (engine 0.32: the crew member by member); the car itself sees nothing
                case "person": return t.Key == "guard" ? "guard" : null;
                case "security": { string first = null; foreach (var w in Game.WatcherIds()) if (w.StartsWith("security-", StringComparison.Ordinal)) { if (w == t.Key) return w; first ??= w; } return first; } // brief 101: the officer himself, else the first
                case "unit": return Game.WatcherKind(t.Key).Length > 0 ? t.Key : null; // brief 067 F7: a response officer or robot, by its own id (view unit/<id>)
                case "hostile": return Game.WatcherKind(t.Key).Length > 0 ? t.Key : null;
            }
            return null;
        }

        /// <summary>A watcher in the player's words: "the entrance camera", "the guard", "the patrol", "the rival crew", "an officer".</summary>
        public string WatcherName(string id) => Game.WatcherKind(id) switch
        {
            "camera" => $"the {id.Substring(7)} camera", "guard" => Name("guard"), "patrol" => Name("patrol"), "rivals" => Name("rivals"), "security" => "an officer", _ => id,
        };

        /// <summary>The watcher the Scout is watching now, or the one the Scout's last order watched (until the Scout's next order).</summary>
        string ScoutWatch()
        {
            foreach (var w in CellOrder) // brief 031: every agent watches machines
            { var sc = Game.Ops[(int)w]; if (sc.Busy != null && sc.Busy.Do == "watch" && Game.WatcherKind(sc.Busy.Target).Length > 0) return sc.Busy.Target; }
            if (lastWatchOrders != Game.Orders.Count)
            {
                lastWatchOrders = Game.Orders.Count; lastWatch = null;
                for (int i = Game.Orders.Count - 1; i >= 0; i--)
                {
                    var (o, ok) = (Game.Orders[i].order, Game.Orders[i].ok);
                    if (!ok || o.Op is not ("scout" or "fixer" or "tech" or "enforcer")) continue;
                    if (o.Do == "watch" && Game.WatcherKind(o.Target).Length > 0) lastWatch = o.Target;
                    break;
                }
            }
            return lastWatch;
        }

        /// <summary>Per frame: which learned fans draw (brief 067 F, Glyph; Sky on 0.2.122: "the vision cones on the police, rivals etc. are too
        /// much, making the map too busy"). A learned cone is drawn only when it matters now, never "at rest" (the plot scope keeps every learned
        /// cone): (1) it is the hovered thing's, lingering 0.15 s; (2) the order being set names it (a ring wedge, the hold's charge or the pill
        /// on a Watch, Loop, Disable or Reroute, the open panel's target), 0.15 s; (3) it can see a selected agent now or is about to
        /// (within 4 s), 1 s; (4) a selected agent is acting on it (a busy Watch, Loop, Disable or Reroute; Glyph Rev 2: never a Listen), a running loop's camera, the
        /// patrol while it is away, and 3 s after a Watch teaches it (the reveal); (5) a walk being ordered or under way crosses it, 1 s; (6) Alt
        /// shows every learned cone. In a fight (or with a selected agent drawn) the combatants' cones (officers, robots, rivals, an engaged guard
        /// and patrol) take only rules 1, 2, 4 and Alt: the target rings say who aims at whom; cameras keep every rule. Strength is 0 or 1, in
        /// over 0.25 s and out over 0.6 s (HostViews.ShowFans); the "?" over an unlearned watcher and the attention cues are unchanged.</summary>
        void ShowSight()
        {
            if (Game == null || views == null) return;
            shownFans.Clear(); fanStrong.Clear();
            // Glyph's 016 review §5: the detail layer shows once Alt has been held 0.25 s, so an Alt chord (Alt + middle turns the view) or a quick
            // tap never flashes it; nothing shows while the view is being turned
            bool held = controls.Held(Keyboard.current, PlayAction.Peek); if (!held) peekHeldSince = -1f; else if (peekHeldSince < 0f) peekHeldSince = Time.unscaledTime;
            Peeking = (PeekLatch || held && Time.unscaledTime - peekHeldSince >= PeekDelay) && Ending.Length == 0 && !TurnDragging; PricePeekFrame(); // brief 096: a price rested on shows its breakdown (HostKeys096)
            FocusWatcher = null;
            Guard("sight", () =>
            {
                var g = Game; float now = Time.unscaledTime;
                string hovered = WatcherOf(Hover);
                // (2) named by the order being set
                namedNow.Clear();
                if (RingFocus is (string _, string rw) && rw.Length > 0) namedNow.Add(rw);
                if (Ring is { } rs) { int ri = rs.Hover >= 0 ? rs.Hover : rs.Lit; if (ri >= 0 && ri < rs.Segs.Count && OrderWatcher(rs.Segs[ri].Row?.I?.Order) is string ow) namedNow.Add(ow); }
                if (Charge is { Done: false, Fire: false, Plan: not null } ch && OrderWatcher(ch.Plan.Row(ch.Way)?.I?.Order) is string cw) namedNow.Add(cw);
                if (HoverIntent is { Actionable: true } hi && OrderWatcher(hi.Order) is string hw) namedNow.Add(hw);
                if (WatcherOf(Panel?.T) is string pw) namedNow.Add(pw);
                string focus = hovered ?? (namedNow.Count > 0 ? namedNow[0] : null) ?? ScoutWatchingNow();
                if (focus != null && g.Known(focus) is { Sight: true }) FocusWatcher = focus;
                // (4) acted on: a selected agent's busy act on it, the running loop's camera, the patrol while away
                actedNow.Clear();
                for (int k = 0; k < 4; k++) { if (!Selected[k]) continue; var b = g.Ops[k].Busy; if (b != null && b.Do is "watch" or "loop" or "disable" or "reroute" && OrderWatcher(b) is string aw) actedNow.Add(aw); } // Glyph 067 Rev 2 F2: Listen is no cone cause
                if (g.LoopOn && g.LoopCam.Length > 0) actedNow.Add("camera:" + g.LoopCam);
                if (g.PatrolAwayUntil > g.Tick) actedNow.Add("patrol");
                bool fight = g.FightOn || AnyDrawn(); bool? watchLanded = null;
                foreach (var f in g.KnownFans())
                {
                    var w = f.Watcher; float until = fanUntil.TryGetValue(w, out var u) ? u : -9f;
                    // (4) the reveal: 3 s of the mission clock after a Watch teaches it (it holds while paused). Only a Watch's landing (an agent's
                    // <op>.watch.watched within the last second) reveals: a fan that first appears because its watcher came (the response's officers,
                    // whose sight is known when they come; a crew member's fight body) is not news, and at a fight's start those reveals drew every
                    // combatant's cone at once (host round 4's loud fight frame: eight)
                    if (!fanLearned.Contains(w)) { fanLearned.Add(w); if (fanLearnedInit && (watchLanded ??= WatchJustLanded(g))) fanReveal[w] = g.Tick + 3 * Suborned.Sim.Game.Hz; }
                    if (w == hovered) until = Mathf.Max(until, now + 0.15f);
                    if (namedNow.Contains(w) || actedNow.Contains(w)) until = Mathf.Max(until, now + 0.15f);
                    bool combat = fight && CombatWatcher(w);
                    if (!combat && (SeesSelectedSoon(f) || FanMattersNow(f))) until = Mathf.Max(until, now + 1f); // (3), (5)
                    fanUntil[w] = until;
                    shownFans.Add(f); fanStrong.Add(Peeking || ConesAll || now < until || fanReveal.TryGetValue(w, out var rv) && g.Tick < rv);
                }
                fanLearnedInit = true;
            });
            views.ShowFans(shownFans, fanStrong);
            // the mast's heads, numbered at their rims (Glyph §9): the farthest point of each head's polygon
            MastHeads.Clear();
            foreach (var f in shownFans)
            {
                if (f.More.Length == 0 || !(Peeking || ConesAll || f.Watcher == FocusWatcher)) continue;
                MastHeads.Add(Far(f, f.Points)); foreach (var m in f.More) MastHeads.Add(Far(f, m));
            }
        }
        /// <summary>Brief 067 F rule 5: the walk the pill or the lit wedge previews now (engine points), else null.</summary>
        Pt[] PreviewWalk;
        readonly List<string> namedNow = new(), actedNow = new(); readonly Dictionary<string, float> fanUntil = new(); readonly Dictionary<string, int> fanReveal = new(); readonly HashSet<string> fanLearned = new(); bool fanLearnedInit;
        /// <summary>Brief 067 F: the cones' memory starts again with a run (the reveal never fires for what the cell knew at the start).</summary>
        void ResetCones() { fanUntil.Clear(); fanReveal.Clear(); fanLearned.Clear(); fanLearnedInit = false; }
        /// <summary>An agent's Watch landed within the last second of the mission clock (the log's &lt;op&gt;.watch.watched): the cell just learned a
        /// sight, so a fan appearing now is the reveal's news (F rule 4).</summary>
        static bool WatchJustLanded(Suborned.Sim.Game g)
        {
            for (int i = g.Log.Count - 1; i >= 0; i--) { var l = g.Log[i]; if (l.Tick < g.Tick - Suborned.Sim.Game.Hz) break; if (l.Code.EndsWith(".watch.watched", StringComparison.Ordinal)) return true; }
            return false;
        }

        /// <summary>The watcher an order is about: a Watch's target, a Loop's or Disable's camera, a Reroute's patrol; else null (Glyph 067 Rev 2 F2:
        /// a Listen draws no cone, it learns the person, not the sight).</summary>
        string OrderWatcher(Order o)
        {
            if (o == null || Game == null) return null;
            switch (o.Do)
            {
                case "watch": return Game.WatcherKind(o.Target).Length > 0 ? o.Target : null;
                case "loop": case "disable": return o.Target.Length > 0 ? (o.Target.StartsWith("camera:") ? o.Target : "camera:" + o.Target) : null;
                case "reroute": return "patrol";
            }
            return null;
        }

        /// <summary>Brief 067 F5: a watcher that fights (an officer, a response unit, a rival, the guard and the patrol once a fight is on); never a camera.</summary>
        bool CombatWatcher(string w) { var k = Game.WatcherKind(w); return k is "security" or "rivals" || k is "guard" or "patrol" && Game.FightOn; }

        /// <summary>Brief 067 F2 rule 3 (Glyph): the fan can see a selected agent now (inside its polygon), or is about to: the agent within 4 s ×
        /// (its speed + the watcher's) of the polygon, with a 3 m floor while it walks; or its walk ahead enters the polygon within 4 s of walking.
        /// Engine 0.34.0's walk split (M067-B1): the agent's pace is the cell's (cell.speed 1.8 m/s walking, cell.runSpeed 3.6 m/s running, standing 0),
        /// the watcher's the city's (crowd.walk 1.4 m/s; a camera 0). Once a tick a fan.</summary>
        bool SeesSelectedSoon(Fan f)
        {
            if (Game.Tick != seesTick) { seesSoon.Clear(); seesTick = Game.Tick; seesSel = SelMask(); }
            if (seesSel != SelMask()) { seesSoon.Clear(); seesSel = SelMask(); }
            if (seesSoon.TryGetValue(f.Watcher, out var v)) return v;
            bool any = false; var g = Game; float vw = Game.WatcherKind(f.Watcher) == "camera" ? 0f : g.R.PeopleWalk / 100f;
            for (int k = 0; k < 4 && !any; k++)
            {
                if (!Selected[k]) continue; var op = g.Ops[k]; if (op.Down || op.Inside || op.InCar) continue;
                if (InFan(f, op.Pos)) { any = true; break; }
                float va = op.Walk == null ? 0f : (op.WalkSpeed > 0 ? op.WalkSpeed : op.Running ? g.R.RunSpeed : g.R.CellSpeed) / 100f; int reach = (int)(400f * (va + vw)); if (op.Walk != null) reach = Mathf.Max(reach, 300); // the walk's own pace (cm/s), else the rulebook's
                if (reach > 0 && NearFan(f, op.Pos, reach)) { any = true; break; }
                var w = op.Walk; if (w == null || w.Points.Length < 2) continue;
                int sp = op.WalkSpeed > 0 ? op.WalkSpeed : g.R.CellSpeed; long ahead = (long)sp * Game.Hz * 4; // mm in 4 s
                for (long mm = op.WalkMm; mm <= Math.Min(w.Length, op.WalkMm + ahead); mm += 500) if (InFan(f, w.At(mm))) { any = true; break; }
            }
            return seesSoon[f.Watcher] = any;
        }
        readonly Dictionary<string, bool> seesSoon = new(); int seesTick = -1, seesSel = -1;
        int SelMask() { int m = 0; for (int k = 0; k < 4; k++) if (Selected[k]) m |= 1 << k; return m; }
        readonly List<bool> fanStrong = new();
        /// <summary>Redline 064 #2: FanMatters once an engine tick a watcher (agents and walks move only on a tick), not every frame.</summary>
        readonly Dictionary<string, bool> fanMatters = new(); int fanMattersTick = -1;
        bool FanMattersNow(Fan f)
        {
            if (HandoverHost.PerfLegacy) return FanMatters(f);
            if (Game.Tick != fanMattersTick) { fanMatters.Clear(); fanMattersTick = Game.Tick; }
            if (fanMatters.TryGetValue(f.Watcher, out var m)) return m;
            return fanMatters[f.Watcher] = FanMatters(f);
        }
        readonly HashSet<string> noticingNow = new();

        /// <summary>The watcher a cell agent is watching now (the busy Watch), else null: brief 057 §3, no fallback to a finished Watch.</summary>
        static readonly OpId[] CellOrder = { OpId.Scout, OpId.Fixer, OpId.Tech, OpId.Enforcer };
        static readonly string[] OpNames = { Op.Name(OpId.Scout), Op.Name(OpId.Fixer), Op.Name(OpId.Tech), Op.Name(OpId.Enforcer) }; // Noticing.Op, by OpId index
        string ScoutWatchingNow()
        {
            foreach (var w in CellOrder)
            { var sc = Game.Ops[(int)w]; if (sc.Busy != null && sc.Busy.Do == "watch" && Game.WatcherKind(sc.Busy.Target).Length > 0) return sc.Busy.Target; }
            return null;
        }

        /// <summary>Brief 067 F2 rule 5 (Glyph; brief 057 §3 kept): the walk being ordered crosses the fan (the pill's or the lit wedge's preview, PreviewWalk),
        /// or a selected agent's walk ahead does. Being in the fan or near it is rule 3 (SeesSelectedSoon), for the selection only.</summary>
        bool FanMatters(Fan f)
        {
            var g = Game;
            if (PreviewWalk is Pt[] pw && pw.Length >= 2)
                for (int i = 1; i < pw.Length; i++) { Pt a = pw[i - 1], b = pw[i]; long d = Math.Max(1, (long)Math.Sqrt(G.D2(a, b))); int n = (int)Math.Min(40, d / 100 + 1); for (int s = 0; s <= n; s++) if (InFan(f, new Pt(a.X + (int)((b.X - a.X) * (long)s / n), a.Y + (int)((b.Y - a.Y) * (long)s / n)))) return true; }
            for (int k = 0; k < 4; k++)
            {
                var op = g.Ops[k]; if (op.Down || op.Inside || op.InCar || !Selected[k]) continue;
                var w = op.Walk; if (w == null || w.Points.Length < 2) continue;
                // Redline 064 #4: the walk still ahead only (from WalkMm; PerfLegacy samples the whole walk, as the round did)
                long walked = HandoverHost.PerfLegacy ? 0 : op.WalkMm, run = 0; bool started = walked <= 0;
                for (int i = 1; i < w.Points.Length; i++)
                {
                    var a = w.Points[i - 1]; var b = w.Points[i]; long seg = (long)Math.Sqrt(G.D2(a, b)) * 10; // cm to mm
                    if (!started) { if (run + seg <= walked) { run += seg; continue; } a = w.At(walked); started = true; }
                    run += seg;
                    long d = Math.Max(1, (long)Math.Sqrt(G.D2(a, b))); int n = (int)Math.Min(40, d / 100 + 1);
                    for (int s = 1; s <= n; s++) if (InFan(f, new Pt(a.X + (int)((b.X - a.X) * (long)s / n), a.Y + (int)((b.Y - a.Y) * (long)s / n)))) return true;
                }
            }
            return false;
        }
        static bool InFan(Fan f, Pt p) { if (InPoly(f.Points, p)) return true; foreach (var m in f.More) if (InPoly(m, p)) return true; return false; }
        static bool InPoly(Pt[] poly, Pt p)
        {
            bool inside = false; int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++) { var a = poly[i]; var b = poly[j]; if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (double)(b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside; }
            return inside;
        }
        static bool NearFan(Fan f, Pt p, int cm)
        {
            var poly = f.Points; int n = poly.Length; long c2 = (long)cm * cm;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var a = poly[j]; var b = poly[i]; double vx = b.X - a.X, vy = b.Y - a.Y, l2 = vx * vx + vy * vy; double t = l2 <= 0 ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * vx + (p.Y - a.Y) * vy) / l2));
                double dx = a.X + vx * t - p.X, dy = a.Y + vy * t - p.Y; if (dx * dx + dy * dy <= c2) return true;
            }
            return false;
        }

        /// <summary>The mast heads' rim points (world), for their number plates; empty unless a mast's cone shows.</summary>
        public readonly List<Vector3> MastHeads = new();
        Vector3 Far(Fan f, Pt[] pts) { long best = -1; Pt at = f.At; foreach (var p in pts) { long d = G.D2(p, f.At); if (d > best) { best = d; at = p; } } return frame.World(at) + Vector3.up * 0.3f; }

        /// <summary>After each step: the watchers noticing an agent whose sight the cell doesn't know (the "?").</summary>
        void Notice()
        {
            Unknowns.Clear(); System.Array.Clear(SeenBy, 0, SeenBy.Length);
            List<Noticing> n = null; if (!Guard("detection", () => n = Game.Detection())) return;
            noticingNow.Clear(); foreach (var x in n) noticingNow.Add(x.Watcher); // brief 057 §3 (4): a watcher noticing someone draws strong
            bool fightEyes = OneInput && Game.FightOn; // Glyph N4 (0.2.137): a hostile in the fight (an officer, a unit, a rival, the guard and patrol once it is on) wears no "?": he is already hostile; cameras and the rest keep theirs
            foreach (var x in n) { if (x.Known || fightEyes && CombatWatcher(x.Watcher) || Game.WatcherKind(x.Watcher) == "camera") continue; var at = frame.World(x.From) + Vector3.up * 3.2f; if (!Unknowns.Contains(at)) Unknowns.Add(at); } // brief 101: a camera wears its "?" always while unwatched (HudToolkit.UnwatchedWatchers)
            bool fight = Game.FightOn || AnyDrawn(); // as ShowSight: in a fight the combatants' cones take no rule 3, so they are not counted
            foreach (var x in n) { if (!x.Known || fight && CombatWatcher(x.Watcher)) continue; int k = System.Array.IndexOf(OpNames, x.Op); if (k >= 0) SeenBy[k]++; }
        }

        /// <summary>"seen by" for a quote, in words (rulebook sight.unknown: never more than the cell knows): "" when no one, "?" when only an
        /// unknown watcher could, else the known watchers that see it.</summary>
        public string SeenWords(SeenQuote q) => q == null || q.Seen == "no" ? "" : q.Seen == "?" ? "?" : string.Join(", ", q.SeenBy.ConvertAll(WatcherName));
    }
}
