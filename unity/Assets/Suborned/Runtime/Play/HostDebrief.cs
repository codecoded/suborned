using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 099, the debrief (Sky 2026-11-07: "that should be a like our book in terms of presentation, nice, visually appealing with explainers and stats", then
    /// "yeah, try A"): at every ending (a win, each fail cause, a withdrawal, the final fight's end) the book opens on its Job sign with the run's front page in place of the
    /// old end card, once the fail camera (if any) has played. It is the book's own page (BookPage slots, BookLayout, HudToolkit.Book): the start page's rows answered with when
    /// each came true, a fail's next time, five numbers that each open a fold, what the city does now, the run along the foot and the ways on (Watch the replay, Back to the
    /// start, Try again as a new start or the same). The other signs stay the book's (Cell, Journal, Menu). It reads the ended run only (the debrief the host builds, the
    /// ledger, brief 100's why rows, the log): opened or not, a saved run replays to the same hash. Esc opens the pause menu; the page comes back when it shuts.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The book is open as the debrief: Job is the run's front page, no Back, the ways on at the foot.</summary>
        public bool BookEnd { get; private set; }
        /// <summary>The number whose fold is open ("" none): time, cash, trail, alert, standing.</summary>
        public string DebriefFoldOpen { get; private set; } = "";
        /// <summary>Try again's choice: the same start (the seed and the dial's counts just played) or a new one (the default).</summary>
        public bool DebriefSameStart { get; private set; }
        /// <summary>For the proof: the page build's cost (ms) and the stand-in keys it read.</summary>
        public double DebriefBuildMs { get; private set; }

        /// <summary>The debrief is the end here (the old end card stays hidden): any play host but the fight range's.</summary>
        public bool DebriefOwnsEnd => !rangeMode && Fault == null;

        /// <summary>The run has ended and nothing else wants the screen (the fail camera, its replays, the pause menu): the debrief may open.</summary>
        bool BookEndMay => Ending.Length > 0 && Game != null && !FailCamOn && failPrep == null && !MenuOpen && !rangeMode && Fault == null;

        /// <summary>From BookKeys each frame the book is shut: the ended run opens the book on its debrief.</summary>
        bool BookOpenEnd()
        {
            if (!BookEndMay || BookOpen) return false;
            Paused = true; BookEnd = true; ClearHover(); if (Panel != null) ClosePanel();
            if (BookOpenNow("job")) return true;
            BookEnd = false; return false;
        }

        /// <summary>The debrief's keys while it is up (from BookKeys): Esc opens the pause menu over the stopped street (the page comes back when it shuts); the book key
        /// does nothing (the end has no play to go back to). True when one was taken.</summary>
        bool BookEndKeys(Keyboard kb)
        {
            if (!BookEnd || kb == null) return false;
            if (kb.escapeKey.wasPressedThisFrame) { if (DebriefFoldOpen.Length > 0) DebriefFold(null); else BookToMenu(); return true; }
            return controls.Down(kb, PlayAction.Book);
        }

        /// <summary>A number clicked: its fold opens (another's shuts); the same number, or null, shuts it.</summary>
        public void DebriefFold(string id)
        {
            if (!BookOpen || !BookEnd) return;
            DebriefFoldOpen = string.IsNullOrEmpty(id) || id == DebriefFoldOpen ? "" : id; BookShow("job");
        }

        /// <summary>Try again's choice between a new start and the same one.</summary>
        public void DebriefSame(bool same) { if (!BookOpen || !BookEnd || same == DebriefSameStart) return; DebriefSameStart = same; BookShow("job"); }

        /// <summary>A way on: watch the replay (the fail camera's, the run's own deciding moment), back to the start (the main menu, as the pause menu's Quit to menu),
        /// or try again (a new start rolls its seed; the same start keeps the seed and the dial's counts, HostSeed.BeginSameSeed). No order reaches the ended run.</summary>
        public void DebriefWay(string id)
        {
            if (!BookOpen || !BookEnd) return;
            switch (id)
            {
                case "replay": if (!FailCamAvailable) return; BookShut(false); FailCamStart(); return; // the page comes back when it has played (BookOpenEnd)
                case "start": BookShut(false); QuitToMenuFromMenu(); return;
                case "again": BookShut(false); if (DebriefSameStart) BeginSameSeed(); else Begin(); return;
            }
        }

        /// <summary>A new run (ResetCrucial, on every Begin): no fold open, a new start chosen, the rows' history cleared.</summary>
        void DebriefReset() { BookEnd = false; DebriefFoldOpen = ""; DebriefSameStart = false; NeedsReset(); }

        /// <summary>The debrief page: the head (today's end card words, the cause's), the way taken and its rows answered, a fail's next time, the numbers and an open fold,
        /// what the city does now, the run, the ways on. Built when it opens or a fold toggles, never per frame.</summary>
        public BookPage BookDebriefPage()
        {
            var t0 = System.Diagnostics.Stopwatch.GetTimestamp(); var g = Game; var dw = DebriefWords.Get(); bool win = g.Result == Outcome.Success; string clock = MissionClock(g.Tick);
            var p = new BookPage { Section = "job", NameFit = new[] { 72f, 64f, 56f, 50f }, NameW = BookLayout.DX1 - BookLayout.X, TrackMax = 8, KickerHot = !win };
            string head = win ? "win" : g.Result == Outcome.Withdrawn && dw.W("head", "withdraw").Length > 0 ? "withdraw" : "fail"; // a withdrawal's own words where written (the stand-ins'; Fable's schema has win and fail)
            p.Kicker = dw.F(new[] { "head", head }, false, ("clock", clock), ("title", FrontEnd.LoadBrief().title ?? "")).ToUpperInvariant();
            (p.Name, p.Sub) = DebriefHead(win);
            DebriefFace(p, win);
            NeedsAtEnd(); DebriefTicks(p, dw, win); DebriefNext(p, dw, win);
            DebriefStats(p, dw, win); if (DebriefFoldOpen.Length > 0) p.Fold = DebriefFoldOf(DebriefFoldOpen, dw, win);
            DebriefCity(p, dw); DebriefRun(p, dw, win); // with a fold open too: the layout measures the closed page so the fold moves nothing (BookLayout.Debrief)
            DebriefAgain(p, dw);
            DebriefBuildMs = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            return p;
        }

        /// <summary>The head and its line, as the end card worded them (brief 096): a win's headline and result (the lend's own when Popovic left in his own car); a fail's
        /// cause words (debrief.cause), else less.end.fail, else the briefing's fail line, else the debrief's fail headline; a withdrawal's the same way.</summary>
        (string head, string line) DebriefHead(bool win)
        {
            var g = Game; var dw = DebriefWords.Get(); var bw = BookWordsFile.Get();
            if (win) return ValeOut ? (bw.EndWinHead, WinLine()) : (dw.Debrief("headline", "win"), dw.Debrief("result", "win"));
            var cw = EndCauseWords(); string failLine = Text?.Fails.Find(f => f.code == g.FailCode).line ?? "";
            string head = cw.Head ?? (Text != null && Text.EndFail.TryGetValue(g.FailCode ?? "", out var ft) && ft.Length > 0 ? ft.TrimEnd('.') : failLine.Length > 0 ? failLine.Split(new[] { ". " }, System.StringSplitOptions.None)[0].TrimEnd('.') : dw.Debrief("headline", g.Result == Outcome.Withdrawn ? "withdraw" : "fail"));
            return (head, cw.Line ?? (failLine.Length > 0 ? failLine : g.FailText));
        }

        /// <summary>The face over the curve and what the street frames: Popovic (the job), or the cause's culprit where the kit has a painting of them; the line joins the face
        /// to them where they stand in view (a rival has no painting yet: Popovic stands in, briefs/099/latch/notes.md).</summary>
        void DebriefFace(BookPage p, bool win)
        {
            string who = "councillor";
            if (!win && Game.Cause is FailCause c && c.Culprits.Count > 0 && c.Culprits[0].Id is string id && (IsOpName(id) || id is "waitress" or "driver")) who = id;
            p.HeroFace = IsOpName(who) ? "portraits/hero_" + who : who == "councillor" ? "portraits/hero_councillor" : $"portraits/cameo_{who}_normal";
            string view = IsOpName(who) ? "op/" + who : who; var t = views?.Find(view);
            if (t && t.gameObject.activeInHierarchy) { p.Subject = t.position; p.Line = true; p.SubjectView = view; }
            else if (views?.Find("car/getaway") is Transform car && car) p.Subject = car.position;
        }

        /// <summary>The ways on: Watch the replay only where the run has a moment to replay (a fail with its culprit placed: the fail camera); Back to the start; Try again.</summary>
        void DebriefAgain(BookPage p, DebriefWords dw)
        {
            if (FailCamAvailable && dw.W("ways", "replay") is { Length: > 0 } rp) p.Again.Add(new BookAgain { Id = "replay", Icon = "loop", Word = rp });
            if (dw.W("ways", "start") is { Length: > 0 } st) p.Again.Add(new BookAgain { Id = "start", Icon = "papers", Word = st });
            if (dw.W("ways", "again") is { Length: > 0 } ag) p.Again.Add(new BookAgain { Id = "again", Icon = "chev_up", Word = ag, Main = true });
            p.NewWord = dw.W("ways", "new"); string same = dw.W("ways", "same"); p.SameWord = same.Length > 0 ? $"{same} · {Game.Seed}" : ""; p.SameOn = DebriefSameStart;
        }
    }
}
