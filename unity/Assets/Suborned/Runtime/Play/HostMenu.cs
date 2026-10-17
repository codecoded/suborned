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
