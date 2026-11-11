using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 108 (Glyph's pages briefs/108/glyph/pages; Sky 2026-11-11: "Yes, much better and clearer"; "Doesn't need to cram onto one page for an intro mission"):
    /// the Job page as three calm pages, the same at the start and in play: 0 the mission, 1 the place, 2 look out for. At the start the book opens on the first and
    /// Begin sits on the last; in play the Job sign opens on the place page, where the ways' steps tick live behind their chips. Entering the place page runs the camera
    /// once round its places (a row lit as its place shows); a hover or a click on a row stops it and turns the street there. Back, the dots and Next are clicks; the
    /// arrow keys are an extra, and Space at the start is Next, then Begin. After a first play the mission page offers a quiet Skip to Begin. Reads only: nothing here
    /// gives an order or changes the run.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The Job page's step (0 the mission, 1 the place, 2 look out for).</summary>
        public int BookStep { get; private set; }
        /// <summary>The way whose steps are open on the place page ("" none).</summary>
        public string BookWayOpen { get; private set; } = "";
        /// <summary>The place tour: the row shown now (-1 when it isn't running), and when it showed.</summary>
        public int BookTourAt { get; private set; } = -1;
        float tourT; public const float TourStepS = 2.6f;

        const string BriefSeenPref = "suborned.brief.seen";
        static bool briefSeenSession;
        /// <summary>A shot's pin of "after a first play" (ShotRunner briefSeen); null: the player's own.</summary>
        public static bool? BriefSeenPin;
        static bool BriefPrefs => !(Application.isBatchMode || GameBoot.Automation && !Application.isEditor);
        /// <summary>Whether the player has begun a run before (this session, or ever on this machine): the mission page's Skip to Begin.</summary>
        public static bool BriefSeen => BriefSeenPin ?? (briefSeenSession || BriefPrefs && PlayerPrefs.GetInt(BriefSeenPref, 0) == 1);
        static void BriefMarkSeen() { briefSeenSession = true; if (BriefPrefs) { PlayerPrefs.SetInt(BriefSeenPref, 1); PlayerPrefs.Save(); } }

        /// <summary>A view the page can turn the street to, when it is in the street now (a click marker, "marker/door", always is). Null for none.</summary>
        public Transform BookView(string key)
        {
            var t = string.IsNullOrEmpty(key) ? null : views?.Find(key);
            return t && t.gameObject.activeInHierarchy ? t : null;
        }

        /// <summary>The page a step shows; the place page starts its tour.</summary>
        public void BookStepTo(int step)
        {
            if (!BookOpen || BookSectionNow != "job" || BookEnd || !BookWordsFile.Get().HasBrief) return;
            step = Mathf.Clamp(step, 0, 2); if (step == BookStep && BookPageNow?.Step == step) return;
            BookStep = step; BookWayOpen = ""; BookTourAt = -1; BookLookView = ""; bookLookPinned = false;
            BookShow("job");
            if (step == 1) TourLook(0);
        }
        public void BookStepBy(int d) => BookStepTo(BookStep + d);

        void TourLook(int i)
        {
            var pl = BookPageNow?.Places; if (pl == null || i < 0 || i >= pl.Count) { BookTourAt = -1; return; }
            BookTourAt = i; tourT = Time.unscaledTime; BookLookView = pl[i].View; bookLookPinned = false; BookShow("job");
        }

        /// <summary>From BookKeys, each frame the book is up: the tour's next place when its time is up; it ends on the last.</summary>
        void TourFrame()
        {
            if (BookTourAt < 0) return;
            if (!BookOpen || BookSectionNow != "job" || BookStep != 1 || BookPageNow?.Step != 1) { BookTourAt = -1; return; }
            if (Time.unscaledTime - tourT >= TourStepS) TourLook(BookTourAt + 1);
        }

        /// <summary>A row hovered (or left, null) on a briefing page: the tour stops, the street turns there.</summary>
        public void BookBriefHover(string view) { BookTourAt = -1; BookHover(string.IsNullOrEmpty(view) ? null : view); }

        /// <summary>A row clicked: the tour stops, the street turns there and stays (a second click lets go); the row lights.</summary>
        public void BookBriefPin(string view)
        {
            if (!BookOpen) return; BookTourAt = -1;
            if (string.IsNullOrEmpty(view)) return;
            BookPin(view); BookShow("job");
        }

        /// <summary>A way's chip: its steps open in the list's place (or shut), the street on its place.</summary>
        public void BookWayToggle(string way, string view)
        {
            if (!BookOpen || BookSectionNow != "job") return; BookTourAt = -1;
            BookWayOpen = BookWayOpen == way ? "" : way;
            if (BookWayOpen.Length > 0 && !string.IsNullOrEmpty(view)) { BookLookView = ""; bookLookPinned = false; BookPin(view); }
            BookShow("job");
        }

        /// <summary>Skip to Begin (the mission page, after a first play): the run begins as Begin does.</summary>
        public void BookSkip() { if (BookStart) BriefingBegin(); }

        /// <summary>The briefing pages' keys (from BookKeys): the arrows step back and on. True when one was taken.</summary>
        bool BookBriefKeys(Keyboard kb)
        {
            if (kb == null || !BookOpen || BookSectionNow != "job" || BookEnd || BookPageNow?.Step < 0) return false;
            if (kb.leftArrowKey.wasPressedThisFrame) { BookStepBy(-1); return true; }
            if (kb.rightArrowKey.wasPressedThisFrame) { BookStepBy(1); return true; }
            return false;
        }

        /// <summary>The page's data for its step (from BookJobPage): the title, the story, the places with the ways' chips, the dangers, the pager's words.</summary>
        void BookBrief(BookPage p, BookWordsFile bw)
        {
            int st = Mathf.Clamp(BookStep, 0, 2); p.Step = st;
            p.Kicker = (FrontEnd.LoadBrief().title ?? "").ToUpperInvariant(); p.Name = bw.BriefTitles[st]; p.Sub = "";
            p.NextWord = bw.BriefNext; p.BackWord = bw.BriefBack; p.SkipWord = bw.BriefSkip; p.Skip = st == 0 && BookStart && BriefSeen;
            if (st != 0) p.HeroFace = "";
            if (st == 0) p.Story.AddRange(bw.Story);
            if (st == 1) { p.CurveLeft = BookLayout.PlaceCurve; p.WayOpen = BookWayOpen; foreach (var r in bw.Places) p.Places.Add(Row(r)); }
            if (st == 2) foreach (var r in bw.Dangers) p.Dangers.Add(Row(r));
            BookPlace Row(BookWordsFile.BriefRow r) => new()
            {
                Id = r.Id, Icon = r.Icon, Face = r.Face, Badge = r.Face.StartsWith("badges/"), Word = r.Word, Line = r.Line, View = r.View, Way = r.Way,
                Chip = r.Chip.Length > 0 ? BookTimes(r.Chip) : "", When = r.When, On = r.View.Length > 0 && r.View == BookLookView,
            };
        }
    }
}
