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
            foreach (var w in new[] { OpId.Scout, OpId.Fixer, OpId.Tech, OpId.Enforcer }) // brief 031: every agent watches machines
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

        /// <summary>Per frame: which fans to draw (Alt: every learned one; else the focused watcher's, if its sight is learned).</summary>
        void ShowSight()
        {
            if (Game == null || views == null) return;
            shownFans.Clear();
            // Glyph's 016 review §5: the detail layer shows once Alt has been held 0.25 s, so an Alt chord (Alt + middle turns the view) or a quick
            // tap never flashes it; nothing shows while the view is being turned
            bool held = controls.Held(Keyboard.current, PlayAction.Peek); if (!held) peekHeldSince = -1f; else if (peekHeldSince < 0f) peekHeldSince = Time.unscaledTime;
            Peeking = (PeekLatch || held && Time.unscaledTime - peekHeldSince >= PeekDelay) && Ending.Length == 0 && !TurnDragging;
            FocusWatcher = null;
            Guard("sight", () =>
            {
                if (Peeking || HoldOn && WatchReach.HasValue && Hover != null) { shownFans.AddRange(Game.KnownFans()); return; } // brief 041: hovering a Watch shows the cones the cell has learned
                var w = WatcherOf(Hover) ?? WatcherOf(Panel?.T) ?? ScoutWatch();
                if (w == null || Game.Known(w) is not { Sight: true }) return;
                FocusWatcher = w; var f = Game.ConeFan(w); if (f != null) shownFans.Add(f);
            });
            views.ShowFans(shownFans);
            // the mast's heads, numbered at their rims (Glyph §9): the farthest point of each head's polygon
            MastHeads.Clear();
            foreach (var f in shownFans)
            {
                if (f.More.Length == 0) continue;
                MastHeads.Add(Far(f, f.Points)); foreach (var m in f.More) MastHeads.Add(Far(f, m));
            }
        }

        /// <summary>The mast heads' rim points (world), for their number plates; empty unless a mast's cone shows.</summary>
        public readonly List<Vector3> MastHeads = new();
        Vector3 Far(Fan f, Pt[] pts) { long best = -1; Pt at = f.At; foreach (var p in pts) { long d = G.D2(p, f.At); if (d > best) { best = d; at = p; } } return frame.World(at) + Vector3.up * 0.3f; }

        /// <summary>After each step: the watchers noticing an agent whose sight the cell doesn't know (the "?").</summary>
        void Notice()
        {
            Unknowns.Clear();
            List<Noticing> n = null; if (!Guard("detection", () => n = Game.Detection())) return;
            foreach (var x in n) { if (x.Known) continue; var at = frame.World(x.From) + Vector3.up * 3.2f; if (!Unknowns.Contains(at)) Unknowns.Add(at); }
        }

        /// <summary>"seen by" for a quote, in words (rulebook sight.unknown: never more than the cell knows): "" when no one, "?" when only an
        /// unknown watcher could, else the known watchers that see it.</summary>
        public string SeenWords(SeenQuote q) => q == null || q.Seen == "no" ? "" : q.Seen == "?" ? "?" : string.Join(", ", q.SeenBy.ConvertAll(WatcherName));
    }
}
