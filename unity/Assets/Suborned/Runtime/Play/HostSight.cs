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
                case "rivals": return "rivals";
                case "person": return t.Key == "guard" ? "guard" : null;
                case "security": foreach (var w in Game.WatcherIds()) if (w.StartsWith("security-", StringComparison.Ordinal)) return w; return null;
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

        /// <summary>Per frame: which fans to draw and how strong (brief 057 §3, Sky 2026-10-24: "once watched its cone now permanently appears
        /// but we need to make it softer"): every learned cone at rest strength (quiet: the rim is the memory), strong when it matters: hovered
        /// or named (its Watch row, its ring wedge, Alt), a cell agent in it or within 3 m of it heading in, an agent's path through it, Alert 2
        /// or more, or the watcher noticing someone now. The Scout's last Watch no longer keeps a cone strong once the Watch is done.</summary>
        void ShowSight()
        {
            if (Game == null || views == null) return;
            shownFans.Clear(); fanStrong.Clear();
            // Glyph's 016 review §5: the detail layer shows once Alt has been held 0.25 s, so an Alt chord (Alt + middle turns the view) or a quick
            // tap never flashes it; nothing shows while the view is being turned
            bool held = controls.Held(Keyboard.current, PlayAction.Peek); if (!held) peekHeldSince = -1f; else if (peekHeldSince < 0f) peekHeldSince = Time.unscaledTime;
            Peeking = (PeekLatch || held && Time.unscaledTime - peekHeldSince >= PeekDelay) && Ending.Length == 0 && !TurnDragging;
            FocusWatcher = null;
            Guard("sight", () =>
            {
                string focus = null;
                if (RingFocus is (string _, string rw) && rw.Length > 0) focus = rw; // a ring wedge naming a watcher
                else focus = WatcherOf(Hover) ?? WatcherOf(Panel?.T) ?? ScoutWatchingNow();
                if (focus != null && Game.Known(focus) is { Sight: true }) FocusWatcher = focus;
                var g = Game;
                foreach (var f in g.KnownFans())
                {
                    bool strong = Peeking || f.Watcher == FocusWatcher || HoldOn && WatchReach.HasValue && Hover != null || g.Alert >= 2 || noticingNow.Contains(f.Watcher) || FanMattersNow(f);
                    shownFans.Add(f); fanStrong.Add(strong);
                }
            });
            views.ShowFans(shownFans, fanStrong);
            // the mast's heads, numbered at their rims (Glyph §9): the farthest point of each head's polygon
            MastHeads.Clear();
            foreach (var f in shownFans)
            {
                if (f.More.Length == 0 || !(Peeking || f.Watcher == FocusWatcher)) continue;
                MastHeads.Add(Far(f, f.Points)); foreach (var m in f.More) MastHeads.Add(Far(f, m));
            }
        }
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
        string ScoutWatchingNow()
        {
            foreach (var w in CellOrder)
            { var sc = Game.Ops[(int)w]; if (sc.Busy != null && sc.Busy.Do == "watch" && Game.WatcherKind(sc.Busy.Target).Length > 0) return sc.Busy.Target; }
            return null;
        }

        /// <summary>Brief 057 §3 (2, 3): a cell agent on the street inside the fan, or within 3 m of it walking in, or an agent's walk crossing it.</summary>
        bool FanMatters(Fan f)
        {
            var g = Game;
            for (int k = 0; k < 4; k++)
            {
                var op = g.Ops[k]; if (op.Down || op.Inside || op.InCar) continue;
                if (InFan(f, op.Pos)) return true;
                var w = op.Walk; if (w == null || w.Points.Length < 2) continue;
                if (NearFan(f, op.Pos, 300) && InFan(f, w.End)) return true;
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
            Unknowns.Clear();
            List<Noticing> n = null; if (!Guard("detection", () => n = Game.Detection())) return;
            noticingNow.Clear(); foreach (var x in n) noticingNow.Add(x.Watcher); // brief 057 §3 (4): a watcher noticing someone draws strong
            foreach (var x in n) { if (x.Known) continue; var at = frame.World(x.From) + Vector3.up * 3.2f; if (!Unknowns.Contains(at)) Unknowns.Add(at); }
        }

        /// <summary>"seen by" for a quote, in words (rulebook sight.unknown: never more than the cell knows): "" when no one, "?" when only an
        /// unknown watcher could, else the known watchers that see it.</summary>
        public string SeenWords(SeenQuote q) => q == null || q.Seen == "no" ? "" : q.Seen == "?" ? "?" : string.Join(", ", q.SeenBy.ConvertAll(WatcherName));
    }
}
