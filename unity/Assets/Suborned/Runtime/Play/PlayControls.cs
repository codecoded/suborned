using System;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    public enum PlayAction
    {
        PanUp, PanDown, PanLeft, PanRight,
        Verb1, Verb2, Verb3, Verb4,
        Agent1, Agent2, Agent3, Agent4, CycleAgent,
        Pause, Faster, Slower, NextEvent, Restart, Debug, AddToGroup,
        Cancel, Briefing, DialogueLog,
    }

    public enum RosterDock { Bottom, Left, Corner }

    /// <summary>The play controls in one table (Sky, 2026-10-15, "everything but rotation": the camera keeps yaw 30), and the HUD's settings.
    /// Every key the host reads comes from `keys`; the mouse buttons are fixed by the scheme (left select and move, right act, middle pan,
    /// wheel zoom). Remapping is editing this table; a settings screen or the dock option later reads and writes the same object.</summary>
    [Serializable]
    public sealed class PlayControls
    {
        [Serializable]
        public struct Binding { public PlayAction action; public Key[] keys; public string label; }

        public Binding[] keys = Defaults();

        public static Binding[] Defaults() => new Binding[]
        {
            new() { action = PlayAction.PanUp, keys = new[] { Key.W, Key.UpArrow }, label = "W" },
            new() { action = PlayAction.PanDown, keys = new[] { Key.S, Key.DownArrow }, label = "S" },
            new() { action = PlayAction.PanLeft, keys = new[] { Key.A, Key.LeftArrow }, label = "A" },
            new() { action = PlayAction.PanRight, keys = new[] { Key.D, Key.RightArrow }, label = "D" },
            new() { action = PlayAction.Verb1, keys = new[] { Key.Digit1, Key.Numpad1 }, label = "1" },
            new() { action = PlayAction.Verb2, keys = new[] { Key.Digit2, Key.Numpad2 }, label = "2" },
            new() { action = PlayAction.Verb3, keys = new[] { Key.Digit3, Key.Numpad3 }, label = "3" },
            new() { action = PlayAction.Verb4, keys = new[] { Key.Digit4, Key.Numpad4 }, label = "4" },
            new() { action = PlayAction.Agent1, keys = new[] { Key.F1 }, label = "F1" },
            new() { action = PlayAction.Agent2, keys = new[] { Key.F2 }, label = "F2" },
            new() { action = PlayAction.Agent3, keys = new[] { Key.F3 }, label = "F3" },
            new() { action = PlayAction.Agent4, keys = new[] { Key.F4 }, label = "F4" },
            new() { action = PlayAction.CycleAgent, keys = new[] { Key.Tab }, label = "Tab" },
            new() { action = PlayAction.Pause, keys = new[] { Key.Space }, label = "Space" },
            new() { action = PlayAction.Faster, keys = new[] { Key.Equals, Key.NumpadPlus }, label = "=" },
            new() { action = PlayAction.Slower, keys = new[] { Key.Minus, Key.NumpadMinus }, label = "−" },
            new() { action = PlayAction.NextEvent, keys = new[] { Key.N }, label = "N" },
            new() { action = PlayAction.Restart, keys = new[] { Key.Enter, Key.NumpadEnter }, label = "Return" },
            new() { action = PlayAction.Debug, keys = new[] { Key.F12 }, label = "F12" },
            new() { action = PlayAction.AddToGroup, keys = new[] { Key.LeftShift, Key.RightShift }, label = "Shift" },
            new() { action = PlayAction.Cancel, keys = new[] { Key.C }, label = "C" },
            new() { action = PlayAction.Briefing, keys = new[] { Key.B }, label = "B" },
            new() { action = PlayAction.DialogueLog, keys = new[] { Key.L }, label = "L" },
        };
        static readonly Binding[] defaults = Defaults();

        [UnityEngine.Tooltip("Pan when the mouse is at the screen's edge (off by default).")]
        public bool edgeScroll = false;
        [UnityEngine.Tooltip("Edge band for edge scrolling, in pixels at 1080p.")]
        public float edgePixels = 12f;
        [UnityEngine.Tooltip("Where the roster and the verb hotbar sit (Glyph's placements: bottom, left column, corner).")]
        public RosterDock dock = RosterDock.Bottom;
        [UnityEngine.Tooltip("Pause the clock while the action panel is open (off: real time, Sky's lean).")]
        public bool pauseWithPanel = false;
        [UnityEngine.Tooltip("Two presses or clicks within this many seconds count as a double (centre on the agent, run).")]
        public float doubleSeconds = 0.35f;

        /// <summary>The binding for an action: this table's, else the default's (a table saved in a scene before an action existed keeps working).</summary>
        Binding Of(PlayAction a) { if (keys != null) foreach (var b in keys) if (b.action == a) return b; foreach (var b in defaults) if (b.action == a) return b; return default; }

        public bool Down(Keyboard kb, PlayAction a) { if (kb == null) return false; var b = Of(a); if (b.keys == null) return false; foreach (var k in b.keys) if (kb[k].wasPressedThisFrame) return true; return false; }
        public bool Held(Keyboard kb, PlayAction a) { if (kb == null) return false; var b = Of(a); if (b.keys == null) return false; foreach (var k in b.keys) if (kb[k].isPressed) return true; return false; }
        public string Label(PlayAction a) => Of(a).label ?? "";
    }
}
