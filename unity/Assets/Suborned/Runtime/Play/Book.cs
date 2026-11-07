using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 088, the book's page as data: named slots in a fixed order, no positions. Glyph's rule (briefs/085/glyph/book/look3/notes.md, look4/notes.md):
    /// empty slots take no room, and the layout never knows what the content is. `Gear` and `Scales` carry our own agents' kit and stats (Sky, 2026-11-02), so wounds,
    /// gear and what a person gives need no redesign: more rows, the same code.</summary>
    public enum BookSlot { Hero, Name, Sub, State, FactsHead, Facts, Gear, Weapon, Ways, Needs, Scales, Knows, Can, Track, Others, Kicker, Purse, Faces, Shared, Crew, Begin, Filters, Rows, WayIn, Ticks, Next, Stats, Fold, City, Again }

    public sealed class BookFact { public string Icon = "", Word = "", Note = ""; public bool Unknown; }
    public sealed class BookField { public string Icon = "", Text = ""; }
    /// <summary>A way on a page: only where the mission bounds them (the job's ways in). It gives no order: a click opens the ring on `Target`, so the price shows first.</summary>
    public sealed class BookWay
    {
        public string Icon = "", Word = "", Verb = ""; public bool Shut; public HandoverHost.Target Target;
        public readonly List<BookField> Sub = new();
        /// <summary>Brief 096: who the way runs through, a face cameo each (the contact and the people its conditions name); a hover turns the street to them.</summary>
        public readonly List<BookFace> Faces = new();
        public string SubText { get { var p = new List<string>(); foreach (var f in Sub) p.Add(f.Text); return string.Join(" · ", p); } }
    }
    /// <summary>What someone can do in general: a verb and its one line. No target, price, time or lock, and no click: the ring is the only place to act. The rows depend only on who it is.</summary>
    public sealed class BookCan { public string Id = "", Icon = "", Word = "", Line = ""; }
    /// <summary>A weapon at a glance (Sky on 0.2.226: "too specific"): its word, drawn or not, and a few ratings as pips, each the engine's value against a scale held as
    /// data (book.cell.weapon.stats). The fire chip gives a real shot's exact chance; the book only compares. Any weapon fits by data.</summary>
    public sealed class BookWeapon
    {
        public string Id = "", Word = "", Note = "";
        public readonly List<(string id, string word, int pips, int of, float value)> Stats = new();
    }
    /// <summary>A condition a way in needs (book.job.needs, data): its words, the way it belongs to, and its state now: Met, not yet, or Unknown ("?") while the cell hasn't learned what it rests on.
    /// It names no method: any act that makes it true ticks it.</summary>
    public sealed class BookNeed { public string Id = "", Group = "", Words = ""; public bool Met, Unknown; }
    public sealed class BookScale { public string Word = ""; public int Value, Max; }
    public sealed class BookKnow { public string Icon = "", Face = "", Word = ""; }
    /// <summary>A stop on the track. Brief 099: the debrief's run strip gives a cell act its agent's face, and Hot marks the end that lost it (the book's orange); Rank is the order
    /// the run gives its stops up in when the page shows fewer (0 stays longest: the end; equal ranks keep the latest).</summary>
    public sealed class BookStop { public string Icon = "", Word = "", Time = "", Face = ""; public bool Past, Hot; public int Rank; }
    public sealed class BookOther { public string Face = "", Word = ""; public int Agent = -1; }
    /// <summary>Brief 096: a face under a way (Glyph's 1-start-job): its picture (a cameo, or a badge for one with none), its name, and the view a hover turns the street to.</summary>
    public sealed class BookFace { public string Face = "", Word = "", View = ""; public bool Badge; }
    /// <summary>Brief 096: one of the cell in the strip under the clock: face, name, job word, one line; a click opens their Cell page.</summary>
    public sealed class BookCrew { public int Agent; public string Face = "", Name = "", Job = "", Line = ""; }
    public sealed class BookFilter { public string Id = "", Word = ""; public bool On; }
    /// <summary>Brief 096: a Journal row: time, icon or face, what happened, where; crucial rows marked, unread dotted. Open, its lines (a face and words each) and its result.</summary>
    public sealed class BookRow
    {
        public int Id; public string Time = "", Icon = "", Face = "", Title = "", Where = "", Result = ""; public bool Unread, Crucial, Open; public Vector3? Place; public string View = "";
        public readonly List<(string face, string text)> Lines = new();
    }

    /// <summary>Brief 099 (the debrief, concept A): a start-page row answered: met with the time it came true and how, never, or the one that lost it (Lost, the orange).</summary>
    public sealed class BookTick { public string Words = "", Note = ""; public bool Met, Lost, Unknown, Spare; } // Spare: after the row that lost it, the first to give way when the page is full
    /// <summary>A fail's way on: an agent's face, the way's icon and word, and who (the cell's own act on the culprit; no order: the page gives none).</summary>
    public sealed class BookChip { public string Face = "", Icon = "", Word = "", Who = ""; }
    /// <summary>One of the run's numbers: its icon and label, the value, one line why. Hot: the book's orange (a raised Alert, a Menace band). A click opens its fold.</summary>
    public sealed class BookStat { public string Id = "", Icon = "", Label = "", Value = "", Why = ""; public bool Hot; }
    /// <summary>A number's explainer, as the book's fold: its head, a line on what it means, a bar of every point (Pips: a run of n, hot or not), rows biggest first, the rule under them.</summary>
    public sealed class BookFoldView
    {
        public string Id = "", Title = "", Head = "", Means = "", Law = "";
        /// <summary>The words for the rows past what fits, summed into one last row ("" for a fold whose values don't add up, such as times).</summary>
        public string Rest = "";
        public readonly List<(int n, bool hot)> Pips = new();
        public readonly List<BookFoldRow> Rows = new();
    }
    /// <summary>A fold row; Keep: shown whatever else gives way (the Time fold's end).</summary>
    public sealed class BookFoldRow { public string Icon = "", Value = "", Words = "", When = ""; public bool Hot, Keep; public readonly List<string> Faces = new(); }
    /// <summary>What the city does now: an icon, a head and its line.</summary>
    public sealed class BookCity { public string Icon = "", Head = "", Line = ""; }
    /// <summary>A way on at the end: an id the host acts on, its icon and word; Main is the book's orange pill.</summary>
    public sealed class BookAgain { public string Id = "", Icon = "", Word = ""; public bool Main; }

    public sealed class BookPage
    {
        public string Section = "", HeroFace = "", HeroIcon = "", Name = "", NameTag = "", Sub = "", State = "", FactsHead = "";
        public readonly List<BookFact> Facts = new(), Gear = new();
        public readonly List<BookWay> Ways = new();
        public readonly List<BookCan> Can = new();
        public BookWeapon Weapon;
        public readonly List<BookNeed> Needs = new();
        /// <summary>The facts fold under their head: shut, the head alone says how many (FactsCount) and opens them.</summary>
        public bool FactsFolded; public int FactsCount;
        public readonly List<BookScale> Scales = new();
        public readonly List<BookKnow> Knows = new();
        public readonly List<BookStop> Track = new();
        public readonly List<BookOther> Others = new();
        /// <summary>What the frozen street shows beside the page, its world point; Line: the thin line from the face to that person (people, never a place they aren't).</summary>
        public Vector3? Subject; public bool Line; public float? SubjectYaw; public string SubjectView = "";
        // brief 096 (Glyph's mocks briefs/096/glyph): the start's and the Journal's slots
        /// <summary>The small spaced capitals over the name (the mission's title).</summary>
        public string Kicker = "";
        /// <summary>The purse and Heat, right of the folded ways to fail.</summary>
        public string Purse = "", HeatWord = "", Heat = "";
        /// <summary>The line under the ways where both end (the getaway), and the shared conditions under it (Needs with Group "*").</summary>
        public string Shared = "";
        public readonly List<BookCrew> Crew = new();
        /// <summary>The start: Begin where Back is.</summary>
        public bool Begin;
        public readonly List<BookFilter> Filters = new();
        public readonly List<BookRow> Rows = new();
        /// <summary>The Journal's first row shown (the wheel scrolls it).</summary>
        public int RowsFrom;
        // brief 099, the debrief (concept A, briefs/099/glyph): the start page's rows answered, a fail's next time, the numbers and a fold, the city, the run, the ways on
        /// <summary>The kicker in the orange (a lost run); the name's sizes to try (null: the book's own), and its width.</summary>
        public bool KickerHot; public float[] NameFit; public float NameW;
        /// <summary>The way taken over its rows: its icon, word and the soft note after it ("your way in").</summary>
        public string WayIcon = "", WayWord = "", WayNote = "";
        public readonly List<BookTick> Ticks = new();
        /// <summary>A fail's next time: the small caps, the line, the cell's own ways on the culprit.</summary>
        public string NextHead = "", NextLine = ""; public readonly List<BookChip> NextWays = new();
        public readonly List<BookStat> Stats = new();
        /// <summary>The open number's explainer (null: none open; the city and the run show instead).</summary>
        public BookFoldView Fold;
        public readonly List<BookCity> City = new();
        /// <summary>The track's most stops (the book's clock: 6; the debrief's run: 8).</summary>
        public int TrackMax = 6;
        /// <summary>The ways on, and Try again's choice: a new start or the same (its seed shown after the word).</summary>
        public readonly List<BookAgain> Again = new(); public string NewWord = "", SameWord = ""; public bool SameOn;
    }

    /// <summary>One section sign: an entry in the list is all a section takes (HandoverHost.BookSections); no Page: a dim sign with no page.</summary>
    public sealed class BookSection
    {
        public readonly string Id, Icon, Word; public readonly Func<HandoverHost, BookPage> Page;
        public BookSection(string id, string icon, string word, Func<HandoverHost, BookPage> page = null) { Id = id; Icon = icon; Word = word; Page = page; }
    }
}
