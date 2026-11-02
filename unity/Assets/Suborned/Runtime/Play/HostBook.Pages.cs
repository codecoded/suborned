using System;
using System.Collections.Generic;
using File = System.IO.File;
using Path = System.IO.Path;
using Directory = System.IO.Directory;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 088: the book's words (text.json's `book` block, draft for Fable), read once at the first open: nothing here costs a frame while the book is shut.</summary>
    public sealed class BookWordsFile
    {
        public sealed class End { public string Icon = "", Word = ""; public readonly List<string> Fails = new(); }
        public sealed class Stop { public string Icon = "", Log = ""; }
        public string Back = "Back", JobName = "", JobSub = "", EndsHead = "", Health = "Health", Reach = "", Drawn = "", Holstered = "";
        public readonly List<(string id, string word, float full, int pips)> WeaponStats = new();
        public readonly List<(string id, string icon, string word)> SectionRows = new();
        public readonly List<End> Ends = new();
        public readonly Dictionary<string, Stop> Stops = new();
        public readonly Dictionary<string, string> Weapons = new();
        public readonly Dictionary<string, (string word, string icon, string line)> Does = new();

        static BookWordsFile one;
        public static BookWordsFile Get()
        {
            if (one != null) return one; one = new BookWordsFile();
            try
            {
                var path = Path.Combine(SimBundle.Dir, "text.json"); if (!File.Exists(path)) return one;
                var b = JNode.Parse(File.ReadAllText(path), "text.json").Opt("book"); if (b == null) return one;
                string S(JNode n, string k) => n?.Opt(k) is JNode v && v.Kind == JKind.Str ? v.Str() : "";
                one.Back = S(b, "back");
                foreach (var x in b["sections"].Items) one.SectionRows.Add((S(x, "id"), S(x, "icon"), S(x, "word")));
                var j = b["job"]; one.JobName = S(j, "name"); one.JobSub = S(j, "sub"); one.EndsHead = S(j, "endsHead");
                foreach (var x in j["ends"].Items) { var e = new End { Icon = S(x, "icon"), Word = S(x, "word") }; foreach (var f in x["fails"].Items) e.Fails.Add(f.Str()); one.Ends.Add(e); }
                foreach (var x in j["stops"].Items) one.Stops[S(x, "at")] = new Stop { Icon = S(x, "icon"), Log = S(x, "log") };
                var c = b["cell"]; one.Health = S(c, "health"); one.Reach = S(c, "reach"); one.Drawn = S(c, "drawn"); one.Holstered = S(c, "holstered"); foreach (var x in c["weapon"]["stats"].Items) one.WeaponStats.Add((S(x, "id"), S(x, "word"), float.Parse(x["full"].NumberText, System.Globalization.CultureInfo.InvariantCulture), x["pips"].Int()));
                foreach (var kv in c["weapons"].Entries) one.Weapons[kv.Key] = kv.Value.Str();
                foreach (var kv in c["does"].Entries) one.Does[kv.Key] = (S(kv.Value, "word"), S(kv.Value, "icon"), S(kv.Value, "line"));
            }
            catch (Exception e) { Debug.LogWarning($"Book: text.json's book block not read ({e.Message})"); }
            return one;
        }

        /// <summary>The list: one entry a section. Which have pages is this table; a sign with none is dim and has no page.</summary>
        public List<BookSection> Sections()
        {
            var pages = new Dictionary<string, Func<HandoverHost, BookPage>> { ["cell"] = h => h.BookCellPage(h.BookAgent), ["job"] = h => h.BookJobPage() };
            var l = new List<BookSection>(); foreach (var (id, icon, word) in SectionRows) l.Add(new BookSection(id, icon, word, pages.TryGetValue(id, out var p) ? p : null)); return l;
        }
    }

    public sealed partial class HandoverHost
    {
        /// <summary>The job page: from the briefing's own data (the target, the ways, the clock) and the book's list of what ends it. The clock's times are the run's, read
        /// from the log once a beat has happened; a beat the cell can't know yet stays "?" until it has.</summary>
        public BookPage BookJobPage()
        {
            var bw = BookWordsFile.Get(); var g = Game; var p = new BookPage { Section = "job", HeroFace = "portraits/hero_councillor", Name = bw.JobName, Sub = bw.JobSub, FactsHead = bw.EndsHead };
            foreach (var e in bw.Ends) p.Facts.Add(new BookFact { Icon = e.Icon, Word = e.Word });
            var mission = BriefScreen.MissionFile("brief.json");
            if (mission?.Opt("ways") is JNode ways && ways.Kind == JKind.Arr)
                foreach (var w in ways.Items)
                {
                    string id = w.Opt("id")?.Str() ?? "", contact = w.Opt("contact")?.Str() ?? "";
                    var way = new BookWay { Icon = id == "pickup" ? "car" : "enter", Verb = id, Word = Text?.BriefWord("ways." + id, null) ?? id, Target = new Target { Kind = "person", Key = contact, Id = contact, Label = Name(contact) } };
                    if (Text != null && Text.PageWays.TryGetValue(id, out var pw) && pw.when.Length > 0) way.Sub.Add(new BookField { Text = pw.when });
                    p.Ways.Add(way);
                }
            if (Text != null)
                foreach (var at in Text.PageClock)
                {
                    var (_, label, known) = Text.Timeline.Find(x => x.at == at); bw.Stops.TryGetValue(at, out var st);
                    int logged = -1; if (st != null && st.Log.Length > 0) for (int i = 0; i < g.Log.Count; i++) if (g.Log[i].Code == st.Log) { logged = g.Log[i].Tick; break; }
                    var parts = at.Split(':'); int tick = (int.Parse(parts[0]) * 60 + int.Parse(parts[1])) * Game.Hz;
                    bool past = logged >= 0 || known && g.Tick >= tick && (st == null || st.Log.Length == 0); bool open = known || logged >= 0;
                    string word = open ? (Text.TimelineShort.TryGetValue(at, out var sh) ? sh : label ?? at) : "?";
                    p.Track.Add(new BookStop { Icon = st?.Icon ?? "nextevent", Word = word, Time = logged >= 0 ? MissionClock(logged) : known ? at : "?", Past = past });
                }
            if (Brief?.TargetAt is Vector3 door) { p.Subject = door; p.SubjectYaw = baseYaw + BriefYaw(); }
            return p;
        }

        /// <summary>The cell page: an agent as the game tracks them: what they're doing, health, weapon and whether it's drawn, how far they watch, and what they can do in general, never on whom (Sky on 0.2.221: "can't scale").</summary>
        public BookPage BookCellPage(int k, Weapon weapon = null)
        {
            var bw = BookWordsFile.Get(); var g = Game; var op = g.Ops[k]; string role = Op.Name((OpId)k);
            string name = Text != null && Text.Speakers.TryGetValue(role, out var sp) ? sp : role;
            var p = new BookPage { Section = "cell", HeroFace = "portraits/hero_" + role, Name = name, Sub = Text != null && Text.PageCell.TryGetValue(role, out var cl) ? cl : "" };
            var d = DoingOf(k); p.Facts.Add(new BookFact { Icon = op.Down ? "s_downed" : d.Phase is "walking" or "running" ? "s_moving" : HudModel.AtlasIcon(d.Verb, op.Busy?.Approach ?? "", d.Phase), Word = op.Down ? (Text != null && Text.Activity.TryGetValue("down", out var dw) ? dw : "Down") : d.Label });
            if (bw.Reach.Length > 0) p.Facts.Add(new BookFact { Icon = "watch", Word = bw.Reach.Replace("{m}", (g.R.WatchReachCm((OpId)k) / 100).ToString()) });
            p.Weapon = BookWeaponOf(k, weapon);
            p.Scales.Add(new BookScale { Word = bw.Health, Value = op.Gun.Health, Max = op.Gun.Max });
            p.Can.AddRange(BookCanOf(k));
            for (int i = 0; i < 4; i++) if (i != k) { var r = Op.Name((OpId)i); p.Others.Add(new BookOther { Agent = i, Face = $"portraits/portrait_{r}_{(g.Ops[i].Down ? "down" : "normal")}", Word = Text != null && Text.Speakers.TryGetValue(r, out var on) ? on : r }); }
            var vt = views?.Find("op/" + role); bool shown = vt && vt.gameObject.activeInHierarchy;
            p.Subject = shown ? vt.position : frame.World(op.Pos); p.Line = shown; p.SubjectView = "op/" + role;
            return p;
        }

        /// <summary>The weapon block: the weapon (the agent's, or any weapon given: a future one is data) and its ratings, each an engine value set against book.cell.weapon.stats
        /// (a value for a full row and how many pips). The values: damage a hit; rate in shots a minute (from the gap between shots); range to its last metre; the agent's aim (the stat sheet).</summary>
        public BookWeapon BookWeaponOf(int k, Weapon w = null)
        {
            var g = Game; var op = g.Ops[k]; w ??= op.Gun.W; if (w == null) return null; var bw = BookWordsFile.Get();
            var b = new BookWeapon { Id = w.Id, Word = bw.Weapons.TryGetValue(w.Id, out var wn) ? wn : char.ToUpperInvariant(w.Id[0]) + w.Id[1..], Note = w == op.Gun.W ? (Drawn(k) ? bw.Drawn : bw.Holstered) : "" };
            foreach (var (id, word, full, pips) in bw.WeaponStats) { float v = BookStatValue(id, k, w); b.Stats.Add((id, word, BookPips(v, full, pips), pips, v)); }
            return b;
        }
        /// <summary>An engine value for a rating, by its id in the data; NaN for an id the engine has no value for.</summary>
        public float BookStatValue(string id, int k, Weapon w) => id switch
        {
            "damage" => w.Damage, "rate" => w.Every > 0 ? 60f * Game.Hz / w.Every : 0f, "range" => w.MaxCm / 100f, "aim" => Game.Ops[k].Gun.Aim, _ => float.NaN,
        };
        /// <summary>The pips for a value: its share of the full value, rounded, at least one for anything above nothing, never past the row.</summary>
        public static int BookPips(float v, float full, int pips) => float.IsNaN(v) || v <= 0f || full <= 0f ? 0 : Mathf.Clamp((int)Math.Round(v / full * pips, MidpointRounding.AwayFromZero), 1, pips);

        /// <summary>An agent's own verbs, the table the hotbar's plate starts from (Slots: PlateOf only swaps knobs while carrying, so reading the table keeps the rows the same at any place and
        /// time), each with its word and one line from actions.*, the takedown one verb, whatever the ring calls its ways. A verb with no line of its own takes book.cell.does.</summary>
        public List<BookCan> BookCanOf(int k)
        {
            var l = new List<BookCan>(); var bw = BookWordsFile.Get();
            foreach (var s in Slots[k])
            {
                PlayText.ActionText a = null; Text?.Actions.TryGetValue(s.Id, out a); bw.Does.TryGetValue(s.Id, out var own);
                l.Add(new BookCan { Id = s.Id, Icon = !string.IsNullOrEmpty(own.icon) ? own.icon : HudModel.AtlasIcon(s.Id, s.Id, ""), Word = !string.IsNullOrEmpty(own.word) ? own.word : a != null && a.Label.Length > 0 ? a.Label : s.Label, Line = !string.IsNullOrEmpty(own.line) ? own.line : a?.Does ?? "" });
            }
            return l;
        }
    }
}
