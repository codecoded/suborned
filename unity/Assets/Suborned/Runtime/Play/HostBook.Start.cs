using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 096 (Sky 2026-11-04: the start briefing becomes the book's Job page; Glyph's mock briefs/096/glyph/1-start-job.png, approved "all. recommended"):
    /// a new run opens the book on Job with the clock stopped at 0:00 and Begin where Back is. One page and one data source with the book in play, so the start
    /// can't fall out of date. No stills and no drawn plan: the frozen street behind the page is the plan, and hovering a way or a face turns it to the place
    /// (a click keeps it there). Begin closes the book onto the opening view; the roster and the clock come in on the start sequence's settle, then the clock
    /// runs. Esc, B or J closes it without starting (the clock stays stopped, as the old page's Esc did). `ShowBriefing` stays the one flag the rest of the
    /// host reads ("the start is up"); the book opens on it on the next frame, once the run's camera stands on the opening view. Reads only: replays are untouched.</summary>
    public sealed partial class HandoverHost
    {
        bool startWanted;
        /// <summary>The start briefing is up (the book on Job in start mode), or about to open. Setting it false closes the start book and leaves the clock as it is.</summary>
        public bool ShowBriefing { get => startWanted; set { startWanted = value; if (!value && BookStart) BookShut(true); } }
        /// <summary>The book is open as the start briefing (Begin on its pages).</summary>
        public bool BookStart { get; private set; }

        /// <summary>The start book, when wanted and nothing stops it (from BookKeys, each frame the book is shut).</summary>
        bool BookOpenStart()
        {
            if (!startWanted || BookOpen || Game == null || Game.Over || Game.Tick > 0 && !Paused) return false;
            BookStart = true; if (BookOpenNow("job")) return true;
            BookStart = false; return false;
        }

        /// <summary>Begin (the page's button, Space): the start book closes onto the opening view; the settle (the four rings, the roster) and then the clock.</summary>
        public void BriefingBegin()
        {
            if (!ShowBriefing) return;
            if (DialPending) { DialPlay(); return; } // brief 098: numbers changed on the dial: the street is built with them first, then this again (HostDial)
            if (!cam || Game == null || Game.Over) { StartClock(); return; }
            ShowBriefing = false; Paused = true; ClearHover(); Status = "";
            StartT = BriefExitS + BriefGlideS; glideFrom = glideTo = focus; glideYawFrom = glideYawTo = yawNow; // no glide: the view behind the page is where play starts
        }

        /// <summary>The view a hover or a click on the page turns the street to (a person's view key); "" for the page's own subject. A click pins it.</summary>
        public string BookLookView { get; private set; } = "";
        bool bookLookPinned;

        /// <summary>A way or a face hovered (or left, with null): the street turns to them unless a click has pinned another.</summary>
        public void BookHover(string view)
        {
            if (!BookOpen || bookLookPinned) return;
            BookLookAt(view ?? "");
        }

        /// <summary>A way or a face clicked: the street turns there and stays (a second click on it lets go).</summary>
        public void BookPin(string view)
        {
            if (!BookOpen) return;
            if (bookLookPinned && BookLookView == view) { bookLookPinned = false; BookLookAt(""); return; }
            bookLookPinned = !string.IsNullOrEmpty(view); BookLookAt(view ?? "");
        }

        void BookLookAt(string view)
        {
            if (view == BookLookView || BookPageNow == null) return;
            BookLookView = view;
            var t = view.Length > 0 ? views?.Find(view) : null;
            if (t && t.gameObject.activeInHierarchy) BookGlide(t.position, BookPageNow.SubjectYaw);
            else BookGlide(BookPageNow.Subject, BookPageNow.SubjectYaw);
        }

        /// <summary>The Menu sign on the book's rail (Sky's call 6: no HUD button): the book shuts and the pause menu opens over the stopped street.</summary>
        public void BookToMenu()
        {
            if (!BookOpen) return;
            if (BookStart) ShowBriefing = false; else BookClose();
            OpenMenu();
        }

        /// <summary>The start's keys while its book is up (from BookKeys): Space begins; Esc, B or J closes the start, the clock still stopped. True when one was taken.</summary>
        bool BookStartKeys(Keyboard kb)
        {
            if (!BookStart || kb == null) return false;
            if (controls.Down(kb, PlayAction.Pause)) { BriefingBegin(); return true; }
            if (controls.Down(kb, PlayAction.Book) || kb.escapeKey.wasPressedThisFrame) { ShowBriefing = false; return true; }
            return false;
        }

        /// <summary>The door's outward side for the Job page's view: from the target's exit toward where his car stands (the site's vale exit and car), as a quarter step
        /// from the opening yaw; brief.camera.yaw without them. (Brief 070 read it from the stills step, retired with the stills.)</summary>
        float BriefYaw()
        {
            var s = Game?.S; if (s == null) return Brief?.YawFromOpening ?? 0f;
            Vector3 a, b; try { a = frame.World(s.At(s.ValeExit)); b = frame.World(s.At(s.ValeCar)); } catch (System.Exception) { return Brief?.YawFromOpening ?? 0f; }
            var wn = b - a; wn.y = 0f; if (wn.sqrMagnitude < 0.01f) return Brief?.YawFromOpening ?? 0f; wn.Normalize();
            float best = 0f, bestDot = float.NegativeInfinity;
            for (int k = 0; k < 4; k++) { float y = (baseYaw + 90f * k) * Mathf.Deg2Rad; float d = Vector3.Dot(new Vector3(Mathf.Sin(y), 0f, Mathf.Cos(y)), -wn); if (d > bestDot + 1e-4f) { bestDot = d; best = 90f * k; } }
            return best;
        }
    }
}
