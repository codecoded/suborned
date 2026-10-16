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
            var sc = Game.Ops[(int)OpId.Scout];
            if (sc.Busy != null && sc.Busy.Do == "watch") return Game.WatcherKind(sc.Busy.Target).Length > 0 ? sc.Busy.Target : null;
            if (lastWatchOrders != Game.Orders.Count)
            {
                lastWatchOrders = Game.Orders.Count; lastWatch = null;
                for (int i = Game.Orders.Count - 1; i >= 0; i--)
                {
                    var (o, ok) = (Game.Orders[i].order, Game.Orders[i].ok);
                    if (!ok || o.Op != "scout") continue;
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
            Peeking = controls.Held(Keyboard.current, PlayAction.Peek) && Ending.Length == 0;
            FocusWatcher = null;
            Guard("sight", () =>
            {
                if (Peeking) { shownFans.AddRange(Game.KnownFans()); return; }
                var w = WatcherOf(Hover) ?? WatcherOf(Panel?.T) ?? ScoutWatch();
                if (w == null || Game.Known(w) is not { Sight: true }) return;
                FocusWatcher = w; var f = Game.ConeFan(w); if (f != null) shownFans.Add(f);
            });
            views.ShowFans(shownFans);
        }

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
