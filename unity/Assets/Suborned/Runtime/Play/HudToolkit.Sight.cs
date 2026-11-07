using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The cones round (Sky 2026-11-07, playing: "every rival having a cone - dominates the game"; never key-only):
    ///  · the eye button on the scope's top rim, between the turn buttons (the kit's disc_dark_24 and eye_open, as they are): a click latches what Alt
    ///    shows of the cones, every learned one in the merged look (HandoverHost.ConesAll); lit turquoise-light with a 1.5 px ring while on;
    ///  · a crew counts once: beside a selected agent two or more learned watchers see now (HandoverHost.SeenBy), a small plate as the window
    ///    kill plate's and the unlock pills' (kit plate_dark, 30 px at slice 0.3, a 14 px glyph): the takedown's "he sees you" eye (eye_open, red) and the count in cream, left of the head, clear of the
    ///    act disc and its seconds on the right. Each watcher keeps its own head cue (the attention cues, the "?"). Sourced colours only; Glyph may restyle.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement conesBtn, conesIcon; bool? conesLit; static readonly int[] seenDrawn = new int[4];
        readonly List<(VisualElement root, VisualElement glyph, Label count)> seenMarks = new();
        /// <summary>The proof reads them: whether the eye button is latched, and the counts drawn this frame ("fixer:3").</summary>
        public static bool ConesLatched => live?.host != null && live.host.ConesAll;
        public static string SeenShown { get { var l = new List<string>(); for (int k = 0; k < 4; k++) if (seenDrawn[k] > 0) l.Add($"{Op.Name((OpId)k)}:{seenDrawn[k]}"); return string.Join(" ", l); } } // Redline 064 #6: built when a proof reads it
        /// <summary>The proof reads it: where the eye button is on screen (pixels, origin bottom left), null when hidden.</summary>
        public static Vector2? ConesBtnScreen { get { var b = live?.conesBtn; if (b == null || b.resolvedStyle.display == DisplayStyle.None || b.worldBound.width <= 0) return null; var c = b.worldBound.center; var sz = b.panel.visualTree.layout.size; return new Vector2(c.x / sz.x * Screen.width, Screen.height - c.y / sz.y * Screen.height); } }

        void BuildConesButton()
        {
            var b = E(scope, "s-disc_dark_24", ScopeW / 2f - 15f, -12f, 30f, 30f, pick: true); b.name = "cones-button"; Round(b, 15f);
            conesIcon = E(b, null, 7f, 7f, 16f, 16f); Icon(conesIcon, "eye_open", Cream);
            b.RegisterCallback<PointerEnterEvent>(_ => b.style.unityBackgroundImageTintColor = new Color(0.75f, 1f, 1f)); b.RegisterCallback<PointerLeaveEvent>(_ => b.style.unityBackgroundImageTintColor = Color.white);
            b.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ConesAllToggle(); e.StopPropagation(); } });
            conesBtn = b; manifest.Add((b, "image", () => "cones button"));
        }

        /// <summary>Per frame with the pill (between the street marks' reset and their end): the button's state and the seen counts.</summary>
        void SightFrame(bool ended)
        {
            if (conesBtn != null && conesLit != host.ConesAll) { bool on = host.ConesAll; conesLit = on; Icon(conesIcon, "eye_open", on ? TurqLight : Cream); Border(conesBtn, on ? 1.5f : 0f, TurqLight); }
            int used = 0; System.Array.Clear(seenDrawn, 0, 4);
            var g = host.Game;
            if (g != null && !ended && !host.Following && marks != null)
                for (int k = 0; k < 4; k++)
                {
                    int n = host.SeenBy[k];
                    if (n < 2 || !host.Selected[k] || g.Ops[k].Down || g.Ops[k].Inside || g.Ops[k].InCar || m.Marks[k].World is not Vector3 at || !Proj(at + Vector3.up * 1.95f, out var q)) continue; // the drawn figure (HudModel.Marks)
                    var u = SeenMark(used++); Show(u.root, true); float w = u.root.resolvedStyle.width; if (float.IsNaN(w) || w < 1f) w = 46f;
                    u.root.style.left = q.x - 16f - w; u.root.style.top = q.y - 15f; // its right edge 16 px left of the head
                    Text(u.count, Counts[System.Math.Min(n, Counts.Length - 1)]); seenDrawn[k] = n;
                }
            for (int i = used; i < seenMarks.Count; i++) Show(seenMarks[i].root, false);
        }

        static readonly string[] Counts = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "9+" };
        (VisualElement root, VisualElement glyph, Label count) SeenMark(int i)
        {
            while (seenMarks.Count <= i)
            {
                var r = PlateDark(marks, 30f, 0.3f); r.style.position = Position.Absolute; r.style.paddingLeft = 9; r.style.paddingRight = 10; // the kill plate's size: a glyph 4 px clear of the rim
                var gl = E(r, null, w: 14, h: 14); gl.style.flexShrink = 0; Icon(gl, "eye_open", Red);
                var t = Words(r, 13.5f); t.style.marginLeft = 5; // cream, the kit's tag size
                seenMarks.Add((r, gl, t)); manifest.Add((r, "image", () => "seen count"));
            }
            return seenMarks[i];
        }
    }
}
