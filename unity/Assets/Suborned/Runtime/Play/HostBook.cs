using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 088, the book: one key opens a full-screen page over the real street, stopped (the live 3D scene, not a picture). Opening stops the clock as the pause
    /// path does, closing puts it back as it was, a stopped clock staying stopped. It gives no orders and changes no game state: a saved run replays to the same hash with
    /// the book opened and shut. A way on a page closes it and opens that thing's ring, so the price shows first. In the map view the camera moves to the page's subject and
    /// comes back exactly (the player's own zoom, yaw and place); in third person the view stays. While it is up Update stops here (MenuKeys calls BookKeys), so nothing
    /// reaches the street (ModalGate.Book); while it is shut this costs one key read a frame and the page's elements aren't in the tree (HudToolkit.Book).</summary>
    public sealed partial class HandoverHost
    {
        public bool BookOpen { get; private set; }
        /// <summary>The section lit and the agent its page is on (the cell page).</summary>
        public string BookSectionNow { get; private set; } = "cell";
        public int BookAgent { get; private set; }
        /// <summary>The page up now, built when it opens or changes, never per frame.</summary>
        public BookPage BookPageNow { get; private set; }
        /// <summary>Counted for the proof: what the page build took last (ms).</summary>
        public double BookBuildMs { get; private set; }
        /// <summary>The sections, in order: one entry each (BookWordsFile.Sections; a test section is one more entry). Null until the first open.</summary>
        public List<BookSection> BookSections { get; private set; }

        bool pausedBeforeBook; int bookClosedFrame = -1;
        /// <summary>The camera the book came from (map view only), and its glide to the page's subject.</summary>
        bool bookCamSaved; Vector3 bookFocus; float bookYaw, bookZoom; Vector3 bookFrom, bookTo; float bookYawFrom, bookYawTo, bookGlideAt = -1f; const float BookGlideS = 0.45f;

        /// <summary>Whether the book may open now: only with no modal up, no scene or fail camera playing, no start sequence.</summary>
        bool BookMayOpen => Game != null && Fault == null && !rangeMode && (BookEnd ? BookEndMay : !Game.Over && Ending.Length == 0 && (Modal == ModalGate.Modal.None || startWanted && Modal == ModalGate.Modal.Briefing) && !Starting && !ConvoOn && convoWant == null && !FailCamOn && Loud == null && !Frozen) && !CameraLab.Typing; // brief 099: the ended run's debrief (HostDebrief)

        public bool BookOpenNow(string section = null, int agent = -1)
        {
            if (BookOpen || !BookMayOpen) return false;
            BookSections ??= BookWordsFile.Get().Sections();
            pausedBeforeBook = Paused; if (!Paused) { Paused = true; Save(); } // as the pause path does
            if (Ring != null) CloseRing(); if (Panel != null) ClosePanel();
            CancelHold(); lp = default; rp = default; ClearHover();
            BookOpen = true; BookAgent = agent >= 0 ? agent : (int)Agent; BookSectionNow = section ?? BookSectionNow; BookLookView = ""; bookLookPinned = false;
            bookCamSaved = !Following && cam; if (bookCamSaved) { bookFocus = focus; bookYaw = yawNow; bookZoom = cam.orthographicSize; }
            BookShow(BookSectionNow, BookAgent); Status = "Paused.";
            return true;
        }

        public void BookClose() { if (BookStart) ShowBriefing = false; else if (BookEnd) BookShow("job"); else BookShut(false); } // brief 099: at the end, Back on another page returns to the debrief

        /// <summary>The book shuts: the camera back exactly; the clock as it was before it opened, or (the start, `keepClock`) left as it is now.</summary>
        void BookShut(bool keepClock)
        {
            if (!BookOpen) return;
            JournalSeenNow(); BookStart = false; BookEnd = false; BookLookView = ""; bookLookPinned = false;
            BookOpen = false; BookPageNow = null; bookClosedFrame = Time.frameCount; bookGlideAt = -1f; if (!keepClock) Paused = pausedBeforeBook; lp = default; rp = default;
            if (bookCamSaved && cam) { yawNow = yawFrom = yawTo = bookYaw; turnStart = -1f; cam.transform.rotation = Quaternion.Euler(pitch, yawNow, 0f); cam.orthographicSize = bookZoom; CamProjection(); focus = bookFocus; PlaceCamera(); } bookCamSaved = false;
            Status = Paused ? "Paused." : "Running."; intentKey = "";
        }

        /// <summary>A section or an agent's page: the page built and the view on its subject. A sign with no page does nothing.</summary>
        public void BookShow(string section, int agent = -1)
        {
            var s = BookSections?.Find(x => x.Id == section); if (s?.Page == null || !BookOpen) return;
            if (BookSectionNow == "journal" && section != "journal") JournalSeenNow(); // leaving the Journal: what it showed is read
            if (section != BookSectionNow || agent >= 0 && agent != BookAgent) { BookLookView = ""; bookLookPinned = false; }
            BookSectionNow = section; if (agent >= 0) BookAgent = agent;
            var t0 = System.Diagnostics.Stopwatch.GetTimestamp(); BookPage pg = null; Guard("book", () => pg = s.Page(this)); if (pg == null) return;
            pg.Begin = BookStart; // brief 096: the start's pages carry Begin where Back is
            BookPageNow = pg; BookBuildMs = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            var look = BookLookView.Length > 0 ? views?.Find(BookLookView) : null;
            BookGlide(look && look.gameObject.activeInHierarchy ? look.position : pg.Subject, pg.SubjectYaw);
        }

        /// <summary>The camera glides to frame a world point beside the page (Glyph's frame); with none, back to where the book came from (brief 096: a hover let go).</summary>
        void BookGlide(Vector3? to, float? yaw)
        {
            if (!bookCamSaved || !cam) return;
            if (to is not Vector3 at) { bookFrom = focus; bookTo = bookFocus; bookYawFrom = yawNow; bookYawTo = bookYaw; bookZoomFrom = cam.orthographicSize; bookZoomTo = bookZoom; bookGlideAt = Time.unscaledTime; return; }
            var pg = new BookPage { SubjectYaw = yaw };
            // where the subject stands left of the curve (Glyph's frame: 300 of 1920 across, 46 % down): worked out on the camera at its goal, then put back, nothing drawn between
            float yawGoal = pg.SubjectYaw ?? yawNow, playZoom = Mathf.Clamp(Cam.overhead.zoom, zoomMin, zoomMax);
            var keepF = focus; float keepY = yawNow, keepZ = cam.orthographicSize; var keepR = cam.transform.rotation;
            cam.orthographicSize = playZoom; CamProjection(); cam.transform.rotation = Quaternion.Euler(pitch, yawGoal, 0f); focus = new Vector3(at.x, 0f, at.z); PlaceCamera();
            Vector2 sp = cam.WorldToScreenPoint(at), want = new(cam.pixelWidth * 300f / 1920f, cam.pixelHeight * (1f - 0.46f));
            var d = (sp - want) * (2f * playZoom / cam.pixelHeight); // px to metres at this zoom (orthographic)
            var right = cam.transform.right; right.y = 0f; right.Normalize(); var up = cam.transform.forward; up.y = 0f; up.Normalize();
            var goal = focus + right * d.x + up * d.y / Mathf.Sin(cam.transform.eulerAngles.x * Mathf.Deg2Rad); goal.y = 0f;
            cam.orthographicSize = keepZ; CamProjection(); cam.transform.rotation = keepR; focus = keepF; PlaceCamera();
            bookFrom = keepF; bookTo = goal; bookYawFrom = keepY; bookYawTo = yawGoal; bookGlideAt = Time.unscaledTime; bookZoomFrom = keepZ; bookZoomTo = playZoom;
        }
        float bookZoomFrom, bookZoomTo;

        void BookCamera()
        {
            if (bookGlideAt < 0f || !cam || !bookCamSaved) return;
            float u = Mathf.Clamp01((Time.unscaledTime - bookGlideAt) / BookGlideS), e = u * u * (3f - 2f * u);
            yawNow = yawFrom = yawTo = bookYawFrom + Mathf.DeltaAngle(bookYawFrom, bookYawTo) * e; turnStart = -1f; cam.transform.rotation = Quaternion.Euler(pitch, yawNow, 0f);
            cam.orthographicSize = Mathf.Lerp(bookZoomFrom, bookZoomTo, e); CamProjection();
            focus = Vector3.Lerp(bookFrom, bookTo, e); PlaceCamera();
            if (u >= 1f) bookGlideAt = -1f;
        }

        /// <summary>From MenuKeys, before anything else in Update: the key that opens the book; while it is up, its frame and nothing else (true).</summary>
        bool BookKeys(Keyboard kb)
        {
            if (!BookOpen)
            {
                if (Time.frameCount == bookClosedFrame) return true; // the frame a page's click closed it: that click reaches nothing under it
                if (BookOpenStart() || BookOpenEnd()) return true; // brief 096: a new run's start opens the book on Job; brief 099: an ended run, on its debrief
                if (controls.Down(kb, PlayAction.Book) && BookOpenNow(JournalUnread > 0 ? "journal" : null)) return true;
                return false;
            }
            ClearHover();
            if (BookStartKeys(kb) || BookEndKeys(kb)) return true;
            if (controls.Down(kb, PlayAction.Book) || kb != null && kb.escapeKey.wasPressedThisFrame) { BookClose(); return true; }
            if (BookSectionNow == "journal" && UnityEngine.InputSystem.Mouse.current is { } wm && wm.scroll.ReadValue().y is float wy && wy != 0f) JournalScroll(wy > 0f ? -1 : 1);
            if (controls.Down(kb, PlayAction.CycleAgent)) BookNext();
            if (BookSectionNow == "cell") for (int i = 0; i < 4; i++) if (controls.Down(kb, PlayAction.Agent1 + i)) BookShow("cell", i);
            BookCamera();
            views?.Frame(1f); Guard("car fx", CarFxFrame); FollowFrame(); FadePoints();
            return true;
        }

        /// <summary>The next section that has a page.</summary>
        public void BookNext()
        {
            if (BookSections == null) return; int i = BookSections.FindIndex(x => x.Id == BookSectionNow);
            for (int k = 1; k <= BookSections.Count; k++) { var s = BookSections[(i + k) % BookSections.Count]; if (s.Page != null) { BookShow(s.Id); return; } }
        }

        /// <summary>A way clicked: the book closes and the thing's ring opens for the selection as it is. No order is given.</summary>
        public void BookTakeWay(BookWay w)
        {
            if (!BookOpen || w == null) return; BookClose();
            var t = w.Target; if (t == null) return;
            var world = TargetWorld(t); Vector2 screen = new(Screen.width / 2f, Screen.height / 2f);
            if (world is Vector3 at) { if (!Following) LookAt(at); if (cam) { var sp = cam.WorldToScreenPoint(at + Vector3.up * 1f); if (sp.z > 0f) screen = sp; } }
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, screen)) || plan == null) return;
            OpenRing(plan, screen, false); if (Ring != null) { Ring.Sticky = true; Ring.Always = false; Ring.FromHold = false; }
        }

        /// <summary>Brief 101 round 3 (Glyph: "what you click is a round tab, or a capsule if it has a word"): a new way's capsule clicked, on a conversation's slip or a
        /// done chip: the view on the thing, and its ring opened for the selection as it is, that way lit when the ring offers it (by its pictogram or its word). No order
        /// is given, as the Book's ways (BookTakeWay): the ring is the one place an order is taken.</summary>
        public void WayClick(string anchor, string icon, string word)
        {
            if (Game == null || views == null || string.IsNullOrEmpty(anchor) || !ModalGate.AllowsClicks(Modal)) return;
            var tr = views.Find(anchor); if (!tr || !tr.gameObject.activeInHierarchy) return;
            var t = TargetOf(tr); if (t == null) return;
            var world = TargetWorld(t) ?? tr.position; if (!Following) LookAt(world);
            Vector2 screen = new(Screen.width / 2f, Screen.height / 2f);
            if (cam) { var sp = cam.WorldToScreenPoint(world + Vector3.up * 1f); if (sp.z > 0f) screen = sp; }
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, screen)) || plan == null) return;
            OpenRing(plan, screen, false); if (Ring == null) return;
            Ring.Sticky = true; Ring.Always = false; Ring.FromHold = false;
            int i = Ring.Segs.FindIndex(s => !s.More && !s.Shut && !s.Harmful && (s.Icon == icon || string.Equals(s.Word, word, System.StringComparison.OrdinalIgnoreCase)));
            if (i >= 0) Ring.Lit = i;
        }

        /// <summary>The proof: the same checks as a frame of the shut book, N times, with what they cost (ms a call, bytes in all).</summary>
        public (double msEach, long bytes) BookShutProbe(int n)
        {
            var kb = Keyboard.current; long b0 = GC.GetAllocatedBytesForCurrentThread(); var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            for (int i = 0; i < n; i++) { if (BookOpen) break; _ = Time.frameCount == bookClosedFrame; _ = controls.Down(kb, PlayAction.Book); }
            double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency / Math.Max(n, 1);
            return (ms, GC.GetAllocatedBytesForCurrentThread() - b0);
        }
    }
}
