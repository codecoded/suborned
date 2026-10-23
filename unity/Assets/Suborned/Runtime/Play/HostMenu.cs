using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>The pause menu (Aster, 2026-10-17, the game build): Esc, when it has nothing nearer to close (a panel, the briefing, a picked
    /// verb, the history), opens a small modal menu: Resume, Restart mission, Quit. The clock stops under it and starts again as it was on
    /// Resume (Esc again resumes). Restart is Begin (a new run from the start state; the run left is saved at its last tick). Quit saves and
    /// leaves the app (in the Editor: leaves Play). Nothing reaches the street, the camera or the keys while it is up (ModalGate.Menu).</summary>
    public sealed partial class HandoverHost
    {
        public bool MenuOpen { get; private set; }
        bool pausedBeforeMenu;
        int menuOpenedFrame = -1;

        public void OpenMenu()
        {
            if (MenuOpen || Game == null) return;
            MenuOpen = true; menuOpenedFrame = Time.frameCount; pausedBeforeMenu = Paused; Paused = true;
            ClearHover(); if (Panel != null) ClosePanel();
        }

        public void ResumeFromMenu() { if (!MenuOpen) return; MenuOpen = false; Paused = pausedBeforeMenu; }

        public void RestartFromMenu() { MenuOpen = false; Begin(); }

        /// <summary>Brief 051: the pause menu's Quit to menu: the run is saved at its last tick (as Quit does), the clock stays stopped, and the main menu
        /// comes up over it (Continue returns to it).</summary>
        public void QuitToMenuFromMenu()
        {
            MenuOpen = false; Paused = true;
            if (Game != null && Fault == null) Save();
            FrontEnd.QuitToMenu(this);
        }

        /// <summary>Brief 051: this run was continued from a saved session (Continue on the main menu).</summary>
        public bool Continued { get; private set; }
        string continueScript;
        /// <summary>Continue: a run rebuilt from a saved session (its start state and every order, replayed to its last tick, as a checkpoint loads),
        /// the clock stopped, no briefing. The session goes on in a new file with the same start and orders.</summary>
        public void ContinueFrom(string path)
        {
            continueScript = path; Begin();
            if (Continued) { ShowBriefing = false; Status = "Continued."; }
        }
        Suborned.Sim.Game ContinueGame()
        {
            var path = continueScript; continueScript = null; Continued = false;
            if (path == null || rangeMode) return null;
            var sc = Suborned.Sim.Script.Parse(System.IO.File.ReadAllText(path));
            var g = Suborned.Sim.Engine.New(PlayInputs, sc.Start);
            var list = new System.Collections.Generic.List<(int i, Suborned.Sim.Order o)>(); for (int i = 0; i < sc.Orders.Count; i++) list.Add((i, sc.Orders[i]));
            list.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.i.CompareTo(b.i));
            foreach (var (_, o) in list) { g.RunTo(o.Tick); if (g.Over) break; g.Issue(o); }
            if (!g.Over) g.RunTo(sc.Until);
            lastLog = g.Log.Count; Continued = true;
            Debug.Log($"Play: continued {System.IO.Path.GetFileName(path)} at tick {g.Tick} ({sc.Orders.Count} orders)");
            return g;
        }

        public void QuitFromMenu()
        {
            MenuOpen = false;
            if (Game != null && Fault == null) Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(0);
#endif
        }

        /// <summary>The menu's keys, before anything else in Update: Esc resumes. True while the menu is up (the frame stops there).</summary>
        bool MenuKeys(Keyboard kb)
        {
            if (!MenuOpen) return false;
            ClearHover();
            if (kb != null && kb.escapeKey.wasPressedThisFrame && Time.frameCount != menuOpenedFrame) ResumeFromMenu();
            return true;
        }
    }
}
