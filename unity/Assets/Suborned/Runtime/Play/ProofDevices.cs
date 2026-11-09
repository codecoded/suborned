using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

namespace Suborned.Play
{
    /// <summary>Brief 101 (Aster, 2026-11-10: Sky shouldn't have to step away from the Studio for a test). While the proof's input checks run, its pointer and keys are
    /// the Input System's own virtual devices, the engine's standard test route (as InputTestFixture does): a Mouse and a Keyboard added with InputSystem.AddDevice and
    /// made current, fed only by the proofs' queued state events (InputSystem.QueueStateEvent). The OS's pointers and keyboards are disabled meanwhile, so Sky's hands on
    /// another window never reach the player and never move its pointer between a press and its release. Input is processed whatever the focus
    /// (InputSettings.BackgroundBehavior.IgnoreFocus, Application.runInBackground; editorInputBehaviorInPlayMode is the Editor's and the proof runs in the player).
    /// The HUD's clicks still go through a UI event system, as a real mouse's do: an EventSystem with the Input System's UI module, which UI Toolkit's panels take their
    /// pointer events from while it is up. UI Toolkit's own default event system skips every frame while the player is unfocused (Unity 6000.6 on macOS:
    /// DefaultEventSystem.Update returns at once unless Application.isFocused; the switch, UIElementsRuntimeUtility.eventSystemUpdateMode, is internal), and the
    /// Input System's UI module ignores focus under IgnoreFocus. Everything is put back after the checks. The focus is counted for the log: at the start, every
    /// change, and how many frames ran focused.</summary>
    public static class ProofDevices
    {
        static Mouse mouse; static Keyboard keys; static GameObject ui; static bool runKeep;
        static readonly List<InputDevice> off = new();
        static int frames, focusedFrames, changes; static bool focusedAtStart;

        /// <summary>What the checks run on, for the log's "input checks" line.</summary>
        public static string Said => mouse == null ? "OS devices" : $"virtual {mouse.name} and {keys.name} (current), {off.Count} OS device{(off.Count == 1 ? "" : "s")} off, UI events through {(ui ? "EventSystem + InputSystemUIInputModule" : "the scene's EventSystem")}";
        /// <summary>The focus over the last checks: written when they end (the log's "input checks end" line).</summary>
        public static string Last { get; private set; } = "";

        public static void Install()
        {
            if (mouse != null) return;
            runKeep = Application.runInBackground; Application.runInBackground = true;
            foreach (var d in InputSystem.devices) if (d.native && d.enabled && d is Pointer or Keyboard) off.Add(d);
            foreach (var d in off) InputSystem.DisableDevice(d);
            mouse = InputSystem.AddDevice<Mouse>("ProofMouse"); keys = InputSystem.AddDevice<Keyboard>("ProofKeyboard"); mouse.MakeCurrent(); keys.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) }); // where the OS pointer would rest: the middle, no button
            if (EventSystem.current == null) { ui = new GameObject("proof-ui-events"); Object.DontDestroyOnLoad(ui); ui.AddComponent<EventSystem>(); ui.AddComponent<InputSystemUIInputModule>(); } // the range's scene switch keeps it
            frames = focusedFrames = changes = 0; focusedAtStart = Application.isFocused;
            InputSystem.onAfterUpdate += Count; Application.focusChanged += Changed;
        }

        public static void Remove()
        {
            if (mouse == null) return;
            InputSystem.onAfterUpdate -= Count; Application.focusChanged -= Changed;
            Last = $"focused at the start {focusedAtStart}, now {Application.isFocused}; {changes} focus change{(changes == 1 ? "" : "s")}; {focusedFrames} of {frames} frames focused ({(frames == 0 ? 0 : 100f * focusedFrames / frames):0.#} %)";
            if (ui) Object.Destroy(ui); ui = null;
            InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keys); mouse = null; keys = null;
            foreach (var d in off) if (d.added) { InputSystem.EnableDevice(d); if (d is Mouse m) m.MakeCurrent(); else if (d is Keyboard k) k.MakeCurrent(); }
            off.Clear(); Application.runInBackground = runKeep;
        }

        static void Count() { if (InputState.currentUpdateType is InputUpdateType.Editor or InputUpdateType.BeforeRender) return; frames++; if (Application.isFocused) focusedFrames++; }
        static void Changed(bool focus) => changes++;
    }
}
