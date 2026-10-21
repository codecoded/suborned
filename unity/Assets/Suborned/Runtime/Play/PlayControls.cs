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
        Cancel, Briefing, DialogueLog, Draw, Cease, Stance, Force, Hints, Acknowledge, AllStrips, HudView, Intel, Peek, Response, Gait,
        TurnLeft, TurnRight, TurnDrag,
        FollowView,
        CameraLab,
        TargetMode,
        StepTarget,
        Act,
    }

    /// <summary>Brief 024 revision (action-game controls): the right button aims while held (the default) or toggles the aim (for the trackpad's two-finger click).</summary>
    public enum AimMode { Hold, Toggle }

    public enum RosterDock { Bottom, Left, Corner }

    /// <summary>Brief 032's setting: the conversation scene plays always, the first time with each person (the default), or never (bubbles only).</summary>
    public enum ConversationScenes { Always, FirstTime, Never }

    /// <summary>Which HUD draws: Glyph's kit on UI Toolkit (HudToolkit, its pieces as they land) or the IMGUI placeholder (PlayHud).</summary>
    public enum HudView { Kit, Imgui }

    /// <summary>The play controls in one table (Sky, 2026-10-15; the view turns since brief 016, Sky 2026-10-17: Q / E a quarter round, Alt +
    /// middle-drag freely, snapping to a quarter), and the HUD's settings. Every key the host reads comes from `keys`; the mouse buttons are
    /// fixed by the scheme (left select and move, right act, middle pan, with TurnDrag's key held middle turns, wheel zoom). Remapping is editing this table; a settings screen or the dock option later reads and writes the same object.</summary>
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
            new() { action = PlayAction.Verb1, keys = new[] { Key.Digit1, Key.Numpad1 }, label = "⇧1" }, // brief 030 §8 (Sky 2026-10-20): Shift + the digit picks the action; the plain digit selects the agent
            new() { action = PlayAction.Verb2, keys = new[] { Key.Digit2, Key.Numpad2 }, label = "⇧2" },
            new() { action = PlayAction.Verb3, keys = new[] { Key.Digit3, Key.Numpad3 }, label = "⇧3" },
            new() { action = PlayAction.Verb4, keys = new[] { Key.Digit4, Key.Numpad4 }, label = "⇧4" },
            new() { action = PlayAction.Agent1, keys = new[] { Key.Digit1, Key.Numpad1, Key.F1 }, label = "1" }, // F1–F4: hidden aliases for an external keyboard (Sky uses them as Mac function keys)
            new() { action = PlayAction.Agent2, keys = new[] { Key.Digit2, Key.Numpad2, Key.F2 }, label = "2" },
            new() { action = PlayAction.Agent3, keys = new[] { Key.Digit3, Key.Numpad3, Key.F3 }, label = "3" },
            new() { action = PlayAction.Agent4, keys = new[] { Key.Digit4, Key.Numpad4, Key.F4 }, label = "4" },
            new() { action = PlayAction.CycleAgent, keys = new[] { Key.Tab }, label = "Tab" }, // brief 030 §8: Tab steps the selected agent's actions (the name is kept for saved tables)
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
            new() { action = PlayAction.Draw, keys = new[] { Key.F }, label = "F" },
            new() { action = PlayAction.Cease, keys = new[] { Key.X }, label = "X" },
            new() { action = PlayAction.Stance, keys = new[] { Key.V }, label = "V" },
            new() { action = PlayAction.Force, keys = new[] { Key.LeftCtrl, Key.RightCtrl }, label = "Ctrl" },
            new() { action = PlayAction.Hints, keys = new[] { Key.H }, label = "H" },
            new() { action = PlayAction.Acknowledge, keys = new[] { Key.K }, label = "K" },
            new() { action = PlayAction.AllStrips, keys = new[] { Key.Backquote }, label = "`" },
            new() { action = PlayAction.HudView, keys = new[] { Key.F11 }, label = "F11" },
            new() { action = PlayAction.Intel, keys = new[] { Key.I }, label = "I" },
            new() { action = PlayAction.Peek, keys = new[] { Key.LeftAlt, Key.RightAlt }, label = "Alt" },
            new() { action = PlayAction.Response, keys = new[] { Key.R }, label = "R" }, // brief 013: the response pill's list (Glyph §8)
            new() { action = PlayAction.Gait, keys = new[] { Key.G }, label = "G" }, // Sky 2026-10-16: a moving agent between walk and run (R is the response list; G is free)
            new() { action = PlayAction.TurnLeft, keys = new[] { Key.Q }, label = "Q" }, // brief 016: the view a quarter round left (Q and E were free)
            new() { action = PlayAction.TurnRight, keys = new[] { Key.E }, label = "E" },
            new() { action = PlayAction.TurnDrag, keys = new[] { Key.LeftAlt, Key.RightAlt }, label = "Alt" }, // held with the middle button: turn freely (Alt alone is Peek; the detail layer stays off while turning)
            new() { action = PlayAction.FollowView, keys = new[] { Key.T }, label = "T" }, // Sky 2026-10-18, a test: the third-person follow view (V is Stance; T was free)
            new() { action = PlayAction.CameraLab, keys = new[] { Key.P }, label = "P" },
            new() { action = PlayAction.TargetMode, keys = new[] { Key.Y }, label = "Y" },
            new() { action = PlayAction.StepTarget, keys = new[] { Key.Z }, label = "Z" }, // brief 030 §8: step the target (Tab's old overlap and target uses; Z was free) // brief 027: target mode (Y was free; Motive proposed it) // brief 025: the camera lab (P was free; no fn needed on a MacBook)
            new() { action = PlayAction.Act, keys = new[] { Key.E }, label = "E" }, // brief 024 revision: third person's act key (tap acts, hold the options); E is the view's turn on the map, so the two never meet in one mode
        };
        static readonly Binding[] defaults = Defaults();

        [UnityEngine.Tooltip("Pan when the mouse is at the screen's edge (off by default).")]
        public bool edgeScroll = false;
        [UnityEngine.Tooltip("WASD, arrows and edge pan speed in m/s at play zoom 14; it scales with the zoom, so the screen moves at the same rate at any zoom.")]
        public float keyPanSpeed = 18f;
        [UnityEngine.Tooltip("Middle-drag: 1 = grab the map (the ground point under the cursor stays under it, 1:1 at any zoom); above 1 moves the map faster than the mouse.")]
        public float dragPan = 1f;
        [UnityEngine.Tooltip("Edge band for edge scrolling, in pixels at 1080p.")]
        public float edgePixels = 12f;
        [UnityEngine.Tooltip("Where the roster and the verb hotbar sit (Glyph's placements: bottom, left column, corner).")]
        public RosterDock dock = RosterDock.Left; // the kit's default (Glyph round 6: the left column)
        [UnityEngine.Tooltip("Kit: Glyph's HUD on UI Toolkit (falls back to IMGUI where a piece has not landed, or when the kit is not imported). F11 switches.")]
        public HudView view = HudView.Kit;
        [UnityEngine.Tooltip("Pause the clock while the action panel is open (off: real time, Sky's lean).")]
        public bool pauseWithPanel = false;
        [UnityEngine.Tooltip("Brief 030: a right click opens the full action panel as before (off: the contextual act and the ring).")]
        public bool panelOnRight = false;
        [UnityEngine.Tooltip("Two presses or clicks within this many seconds count as a double (centre on the agent, run).")]
        public float doubleSeconds = 0.35f;
        [UnityEngine.Tooltip("Brief 030 (Sky 2026-10-21, option A): the action plate beside the selected portrait. Off by default: the armed verb rides the cursor and the portrait, Tab or Shift shows the strip. On brings the plate back (pause menu, Action plate).")]
        public bool actionPlate = false;
        [UnityEngine.Tooltip("Brief 032: when an act's opening lines play as a close two-shot with the clock held (pause menu, Conversations). Never: bubbles only, as before.")]
        public ConversationScenes conversations = ConversationScenes.FirstTime;
        [UnityEngine.Tooltip("Sky 2026-10-21 (after 0.2.19): off (the default), a right click (or E in third person) opens the ring at the target with the likely way lit, and a second click takes it. On: the quick action of brief 030 (a click takes the likely way at once, a hold opens the ring). Pause menu, Right-click.")]
        public bool quickAction = false;
        [UnityEngine.Tooltip("Brief 024 revision (action-game controls): in third person a left click on the prompted target does what E does (tap acts, hold 0.22 s opens the options). Off: E only. Pause menu, Left click acts.")]
        public bool clickActs = true;
        [UnityEngine.Tooltip("Brief 024 revision: the right button aims while held (Hold, the default) or a hold toggles the aim on and a tap lowers it (Toggle, for a trackpad).")]
        public AimMode aimMode = AimMode.Hold;
        [UnityEngine.Tooltip("Brief 024 Firing (commander view): off (the default), a right click on a hostile gives a lasting fire order; on, it fires only while the right button is held (Syndicate-style), and release ceases.")]
        public bool commanderHoldFire = false;
        [UnityEngine.Tooltip("Brief 024 (Sky on firing): the follow setting. Off (the default), agents near the driven one fire at its target while its trigger is held; on, they lock its target and keep firing. Sent to the engine as the logged follow order (op cell).")]
        public bool followLock = false;

        const string PlatePref = "Suborned.Controls.ActionPlate", ConvoPref = "Suborned.Controls.Conversations", QuickPref = "Suborned.Controls.QuickAction", ClickPref = "Suborned.Controls.ClickActs", AimPref = "Suborned.Controls.AimMode", HoldFirePref = "Suborned.Controls.CommanderHoldFire", FollowPref = "Suborned.Controls.FollowLock";
        /// <summary>The two menu settings from the player's profile (PlayerPrefs), when saved; the table's own values otherwise.</summary>
        public void LoadSettings()
        {
            if (UnityEngine.PlayerPrefs.HasKey(PlatePref)) actionPlate = UnityEngine.PlayerPrefs.GetInt(PlatePref) == 1;
            if (UnityEngine.PlayerPrefs.HasKey(ConvoPref)) conversations = (ConversationScenes)Math.Clamp(UnityEngine.PlayerPrefs.GetInt(ConvoPref), 0, 2);
            if (UnityEngine.PlayerPrefs.HasKey(QuickPref)) quickAction = UnityEngine.PlayerPrefs.GetInt(QuickPref) == 1;
            if (UnityEngine.PlayerPrefs.HasKey(ClickPref)) clickActs = UnityEngine.PlayerPrefs.GetInt(ClickPref) == 1;
            if (UnityEngine.PlayerPrefs.HasKey(AimPref)) aimMode = (AimMode)Math.Clamp(UnityEngine.PlayerPrefs.GetInt(AimPref), 0, 1);
            if (UnityEngine.PlayerPrefs.HasKey(HoldFirePref)) commanderHoldFire = UnityEngine.PlayerPrefs.GetInt(HoldFirePref) == 1;
            if (UnityEngine.PlayerPrefs.HasKey(FollowPref)) followLock = UnityEngine.PlayerPrefs.GetInt(FollowPref) == 1;
        }
        public void SaveSettings()
        {
            UnityEngine.PlayerPrefs.SetInt(PlatePref, actionPlate ? 1 : 0); UnityEngine.PlayerPrefs.SetInt(ConvoPref, (int)conversations); UnityEngine.PlayerPrefs.SetInt(QuickPref, quickAction ? 1 : 0);
            UnityEngine.PlayerPrefs.SetInt(ClickPref, clickActs ? 1 : 0); UnityEngine.PlayerPrefs.SetInt(AimPref, (int)aimMode); UnityEngine.PlayerPrefs.SetInt(HoldFirePref, commanderHoldFire ? 1 : 0); UnityEngine.PlayerPrefs.SetInt(FollowPref, followLock ? 1 : 0); UnityEngine.PlayerPrefs.Save();
        }

        /// <summary>The binding for an action: this table's, else the default's (a table saved in a scene before an action existed keeps working).</summary>
        /// <summary>A table saved in a scene before brief 030 (agents on F1–F4 only) takes the new defaults: the digits select, Shift + digit picks.</summary>
        public void Migrate() { var a = Of(PlayAction.Agent1); if (keys == null || a.keys == null || Array.IndexOf(a.keys, Key.Digit1) < 0) keys = Defaults(); }

        Binding Of(PlayAction a) { if (keys != null) foreach (var b in keys) if (b.action == a) return b; foreach (var b in defaults) if (b.action == a) return b; return default; }

        public bool Down(Keyboard kb, PlayAction a) { if (kb == null) return false; var b = Of(a); if (b.keys == null) return false; foreach (var k in b.keys) if (kb[k].wasPressedThisFrame) return true; return false; }
        public bool Held(Keyboard kb, PlayAction a) { if (kb == null) return false; var b = Of(a); if (b.keys == null) return false; foreach (var k in b.keys) if (kb[k].isPressed) return true; return false; }
        public string Label(PlayAction a) => Of(a).label ?? "";
    }
}
