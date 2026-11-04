using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 088, the book's page as data: named slots in a fixed order, no positions. Glyph's rule (briefs/085/glyph/book/look3/notes.md, look4/notes.md):
    /// empty slots take no room, and the layout never knows what the content is. `Gear` and `Scales` carry our own agents' kit and stats (Sky, 2026-11-02), so wounds,
    /// gear and what a person gives need no redesign: more rows, the same code.</summary>
    public enum BookSlot { Hero, Name, Sub, State, FactsHead, Facts, Gear, Weapon, Ways, Needs, Scales, Knows, Can, Track, Others, Kicker, Purse, Faces, Shared, Crew, Begin, Filters, Rows }

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
    public sealed class BookStop { public string Icon = "", Word = "", Time = ""; public bool Past; }
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
    }

    /// <summary>One section sign: an entry in the list is all a section takes (HandoverHost.BookSections); no Page: a dim sign with no page.</summary>
    public sealed class BookSection
    {
        public readonly string Id, Icon, Word; public readonly Func<HandoverHost, BookPage> Page;
        public BookSection(string id, string icon, string word, Func<HandoverHost, BookPage> page = null) { Id = id; Icon = icon; Word = word; Page = page; }
    }
}
