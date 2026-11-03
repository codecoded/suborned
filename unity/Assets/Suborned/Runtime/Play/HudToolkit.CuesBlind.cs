using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 084, the watchers that can't see you: a dashed, hollow cone (the engine's own polygon, Fan.Live false: a looped or blocked camera, a
    /// rerouted patrol) that fills back red from the watcher out over its window's last 10 s. No plate over anything that moves (the window marks for
    /// these kinds stand down: HudToolkit.Done); the time left on hover (a line) and Alt (the seconds) only. Route lines on hover only, never in a fight
    /// (HudToolkit.Reroute). A guard called away can still see: his cone stays live and walks with him; only his plate goes, the question Sky must try.</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>The window kinds whose plate stands down.</summary>
        public static bool CueNoPlate(string kind) => FightCues.Blind && kind is "reroute" or "loop" or "call" or "mislead";
        readonly List<Vector3> cueOutline = new(); readonly List<(VisualElement plate, Label text)> cueTips = new();
        /// <summary>The proof reads it: the hollow cones drawn now, and the tips up.</summary>
        public static string BlindShown { get; private set; } = "";

        int? CueLeft(string watcher)
        {
            var g = host.Game;
            foreach (var w in g.Windows())
            {
                bool mine = w.Kind == "reroute" && watcher == "patrol" || w.Kind == "loop" && watcher == "camera:" + w.Thing;
                if (mine) return w.Until < 0 ? (int?)null : Mathf.Max(0, (w.Until - g.Tick + Game.Hz - 1) / Game.Hz);
            }
            return null;
        }

        void CueHollow()
        {
            var g = host.Game; var fr = host.frame; var bits = new List<string>();
            foreach (var f in host.CueFans)
            {
                if (f.Live || f.Points.Length < 3 || host.Views.CueCameraDead(f.Watcher)) continue;
                cueOutline.Clear(); foreach (var p in f.Points) cueOutline.Add(fr.World(p)); cueOutline.Add(cueOutline[0]);
                GroundPath(cueOutline, Cream, 3f, true, 0.8f); /* 2 px at 0.55 was lost on pale paving (0.2.175 shot) */
                bits.Add(f.Watcher);
                if (CueLeft(f.Watcher) is int left && left < 10)
                {   // the last 10 s: the live cone comes back from the watcher out (the sight polygon is a star about the watcher, so each edge clamps to the circle)
                    var apex = fr.World(f.At); float R0 = 40f * (1f - left / 10f);
                    Vector3 Clamp(Vector3 q) { var d = q - apex; d.y = 0f; return d.magnitude <= R0 ? q : apex + d.normalized * R0; }
                    var fill = GroundOf(new Color(Red.r, Red.g, Red.b, 0.18f), true);
                    for (int i = 0; i + 1 < cueOutline.Count; i++)
                    {
                        var a = Clamp(cueOutline[i]); var b = Clamp(cueOutline[i + 1]); var lift = Vector3.up * GroundLift;
                        Quad(fill, apex + lift, apex + lift, a + lift, b + lift);
                        EdgedLine(a, b, Red, SecRim, SecEdge, SecEdgeA, 1f);
                    }
                }
            }
            BlindShown = string.Join(" ", bits);
        }

        /// <summary>The time left, on hover and Alt only: "Can't see you, back in 18 s" (a guard called away: "Can see you. Back at the door in 18 s"), at the feet.</summary>
        void CueBlindTips(bool on)
        {
            int used = 0;
            if (on)
                foreach (var w in host.WindowMarks)
                {
                    if (!CueNoPlate(w.Kind)) continue;
                    string watcher = w.Kind switch { "reroute" => "patrol", "call" => "guard", "loop" => "camera:" + w.Thing, _ => "" };
                    bool hover = watcher.Length > 0 ? host.CueHovers(watcher) : host.Hover?.Kind == "rivals"; bool alt = host.Peeking; if (!hover && !alt) continue;
                    if (host.AnchorWorld(w.Key) is not Vector3 at || !Proj(at, out var q)) continue;
                    while (cueTips.Count <= used) { var pl = PlateDark(marks, 26f, 0.3f); pl.style.position = Position.Absolute; pl.style.paddingLeft = 11; pl.style.paddingRight = 11; pl.pickingMode = PickingMode.Ignore; cueTips.Add((pl, Words(pl, 13.5f))); }
                    var (plate, text) = cueTips[used++]; string count = w.Left >= 0 ? $"{w.Left} s" : "";
                    string say = !hover ? count : w.Kind == "call" ? $"Can see you. Back at the door in {count}" : w.Kind == "mislead" ? $"Rivals misled, back in {count}" : w.Left >= 0 ? $"Can't see you, back in {count}" : "Can't see you";
                    if (say.Length == 0) { used--; continue; }
                    Show(plate, true); Text(text, say); float pw = float.IsNaN(plate.resolvedStyle.width) ? 60f : plate.resolvedStyle.width; plate.style.left = q.x - pw / 2f; plate.style.top = q.y + 22f; plate.BringToFront();
                }
            for (int i = used; i < cueTips.Count; i++) Show(cueTips[i].plate, false);
        }
    }
}
