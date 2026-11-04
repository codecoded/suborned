using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 096, the book's Journal (Sky 2026-11-04: "the logs should be in the book, given time pauses there"; Glyph's mock 3c-journal.png): the full log that the L
    /// history held, newest first, with time and place. Crucial rows are marked (the slim feed's cards, HostCrucial), unread rows dotted until the Journal has shown them;
    /// the filters are the history's (All, Crucial, Learned, Turned, City). A click opens a row to its lines and result and turns the street to where it happened; the face
    /// on the curve is who it was about. The HUD's Book key carries a dot while anything is unread. Reads only.</summary>
    public sealed partial class HandoverHost
    {
        int journalSeen;
        public int JournalOpenId { get; private set; } = -1;
        public int JournalFrom { get; private set; }
        public string JournalFilter { get; private set; } = "all";

        static bool Says(FeedCard c) => !c.Live && (c.Footer.Length > 0 || c.Lines.Count > 0);

        /// <summary>Rows not yet shown by the Journal: the Book key's dot. Allocation-free (asked every frame).</summary>
        public int JournalUnread
        {
            get
            {
                int n = 0;
                foreach (var c in Feed) if (c.Id > journalSeen && Says(c)) n++;
                foreach (var c in FeedHistory) if (c.Id > journalSeen && Says(c)) n++;
                foreach (var c in CrucialLog) if (c.Id > journalSeen) n++;
                return n;
            }
        }

        /// <summary>Whether a book section has anything in it (a dim sign until it does: People has no page yet, the Journal waits for its first row).</summary>
        public bool BookSectionLive(string id) => id != "journal" || Feed.Exists(Says) || FeedHistory.Exists(Says) || CrucialLog.Count > 0;

        void JournalSeenNow() { if (BookOpen && BookSectionNow == "journal") journalSeen = feedIds; }

        public void JournalFilterSet(string id) { JournalFilter = id; JournalFrom = 0; if (BookOpen) BookShow("journal"); }
        public void JournalScroll(int d) { int was = JournalFrom; JournalFrom = Math.Max(0, JournalFrom + d); if (JournalFrom != was && BookOpen) BookShow("journal"); }
        /// <summary>A row clicked: it opens (or shuts) and the street turns to where it happened.</summary>
        public void JournalRow(int id) { JournalOpenId = JournalOpenId == id ? -1 : id; if (BookOpen) BookShow("journal"); }

        static readonly HashSet<string> Faces = new() { "waitress", "driver", "councillor", "scout", "fixer", "tech", "enforcer" };
        static string FaceOf(string who) => Faces.Contains(who) ? $"portraits/cameo_{who}_normal" : who is "guard" or "patrol" or "security" or "rivals" ? HudModel.PersonBadge(who switch { "patrol" => "guard", _ => who }) : "";
        static string KindIcon(FeedCard c) => c.Kind switch
        {
            "learned" => c.Verb == "listen" ? "listen" : "watch", "approach" => c.Tone == Feedback.Ok ? "turned" : c.Tone == Feedback.Refused ? "refused" : "turn_person",
            "call" => "call", "warning" => "p_alert", "fight" => "fire", "act" => c.Verb is "loop" or "reroute" or "mislead" or "enter" or "take" ? c.Verb : "check", _ => "dot",
        };
        bool Passes(FeedCard c, bool crucial) => JournalFilter switch
        {
            "crucial" => crucial, "learned" => c.Kind == "learned", "turned" => c.Kind is "approach" or "call" && c.Tone == Feedback.Ok, "city" => c.Kind == "city", _ => true,
        };

        /// <summary>The crucial card a feed card is the same event as (its log code, or a turn landing on the same person), if any.</summary>
        CrucialCard CrucialOf(FeedCard c)
        {
            foreach (var k in CrucialLog)
            {
                if (k.Code.Length > 0 && c.Event == k.Code && Math.Abs(c.Started - k.Tick) <= Game.Hz) return k;
                if (k.Kind is "turned" or "refused" && c.Kind == "approach" && c.Target == k.Role && c.Ended >= 0 && Math.Abs(c.Ended - k.Tick) <= Game.Hz) return k;
            }
            return null;
        }

        public BookPage BookJournalPage()
        {
            var bw = BookWordsFile.Get(); var sec = BookSections?.Find(x => x.Id == "journal");
            var p = new BookPage { Section = "journal", Name = sec?.Word ?? "Journal" };
            var rows = new List<(int tick, BookRow row)>(); var merged = new HashSet<CrucialCard>();
            void Card(FeedCard c)
            {
                if (!Says(c)) return; var k = CrucialOf(c); if (k != null) merged.Add(k); bool crucial = k != null;
                if (!Passes(c, crucial)) return;
                var row = new BookRow { Id = c.Id, Time = MissionClock(c.Started), Title = c.Title, Where = c.Where, Result = c.Footer, Crucial = crucial, Unread = c.Id > journalSeen, Icon = crucial ? (k.Icon.Length > 0 ? k.Icon : KindIcon(c)) : KindIcon(c), Face = FaceOf(c.Target), View = c.PlaceKey };
                row.Place = PlaceNow(c); row.Open = c.Id == JournalOpenId;
                if (row.Open) foreach (var ln in c.Lines) row.Lines.Add((FaceOf(ln.Speaker), ln.Name.Length > 0 && FaceOf(ln.Speaker).Length == 0 ? $"{ln.Name}: {ln.Text}" : ln.Text));
                rows.Add((c.Started, row));
            }
            foreach (var c in Feed) Card(c); foreach (var c in FeedHistory) Card(c);
            foreach (var k in CrucialLog)
            {
                if (merged.Contains(k) || !(JournalFilter is "all" or "crucial")) continue;
                var row = new BookRow { Id = k.Id, Time = MissionClock(k.Tick), Title = k.Title, Where = CrucialLine(k), Crucial = true, Unread = k.Id > journalSeen, Icon = k.Icon.Length > 0 ? k.Icon : "p_alert", Face = k.Face.StartsWith("portraits/") ? k.Face : k.Role.Length > 0 ? FaceOf(k.Role) : "", View = k.View, Place = k.Place, Open = k.Id == JournalOpenId };
                if (row.Where.EndsWith(row.Time)) row.Where = row.Where[..^row.Time.Length].TrimEnd(' ', '·');
                rows.Add((k.Tick, row));
            }
            rows.Sort((a, b) => a.tick != b.tick ? b.tick.CompareTo(a.tick) : b.row.Id.CompareTo(a.row.Id));
            foreach (var (_, r) in rows) p.Rows.Add(r);
            if (JournalFilter == "all") p.Rows.Add(new BookRow { Id = 0, Time = MissionClock(0), Title = bw.JournalBegins, Icon = "take" });
            int unread = 0; foreach (var r in p.Rows) if (r.Unread) unread++;
            p.Sub = bw.JournalSub + (unread > 0 ? (bw.JournalSub.Length > 0 ? " · " : "") + bw.JournalNew.Replace("{n}", unread.ToString()) : "");
            foreach (var (id, word) in bw.JournalFilters) p.Filters.Add(new BookFilter { Id = id, Word = word, On = id == JournalFilter });
            JournalFrom = Mathf.Clamp(JournalFrom, 0, Math.Max(0, p.Rows.Count - 1)); p.RowsFrom = JournalFrom;
            // the face on the curve: who the open row is about, else the newest row's; the street on where the open row happened
            var open = p.Rows.Find(r => r.Open); var lead = open ?? p.Rows.Find(r => r.Face.StartsWith("portraits/"));
            if (lead != null && lead.Face.StartsWith("portraits/")) p.HeroFace = HeroOf(lead.Face); else p.HeroIcon = "papers";
            if (open != null)
            {
                var t = open.View.Length > 0 ? views?.Find(open.View) : null; bool live = t && t.gameObject.activeInHierarchy;
                p.Subject = live ? t.position : open.Place; p.Line = live && Faces.Contains(open.View.StartsWith("op/") ? open.View[3..] : open.View); p.SubjectView = live ? open.View : "";
            }
            return p;
        }

        /// <summary>The big face on the curve for a cameo: the kit's hero portrait (the target, the cell), else its painted face (Inez, the driver), else the cameo itself.</summary>
        static string HeroOf(string cameo)
        {
            string who = cameo.Replace("portraits/cameo_", "").Replace("_normal", "").Replace("_hurt", "");
            return who is "councillor" or "scout" or "fixer" or "tech" or "enforcer" ? "portraits/hero_" + who : who is "waitress" or "driver" ? "portraits/face_" + who : cameo;
        }
    }
}
