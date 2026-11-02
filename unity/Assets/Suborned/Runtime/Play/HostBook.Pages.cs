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
        public readonly List<(string id, string icon, string word)> SectionRows = new();
        public readonly List<End> Ends = new();
        public readonly Dictionary<string, Stop> Stops = new();
        public readonly Dictionary<string, string> Weapons = new();

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
                var c = b["cell"]; one.Health = S(c, "health"); one.Reach = S(c, "reach"); one.Drawn = S(c, "drawn"); one.Holstered = S(c, "holstered");
                foreach (var kv in c["weapons"].Entries) one.Weapons[kv.Key] = kv.Value.Str();
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

        /// <summary>The cell page: an agent as the game tracks them: what they're doing, health, weapon and whether it's drawn, how far they watch, what they can do now and what's locked.</summary>
        public BookPage BookCellPage(int k)
        {
            var bw = BookWordsFile.Get(); var g = Game; var op = g.Ops[k]; string role = Op.Name((OpId)k);
            string name = Text != null && Text.Speakers.TryGetValue(role, out var sp) ? sp : role;
            var p = new BookPage { Section = "cell", HeroFace = "portraits/hero_" + role, Name = name, Sub = Text != null && Text.PageCell.TryGetValue(role, out var cl) ? cl : "" };
            var d = DoingOf(k); p.Facts.Add(new BookFact { Icon = op.Down ? "s_downed" : d.Phase is "walking" or "running" ? "s_moving" : HudModel.AtlasIcon(d.Verb, op.Busy?.Approach ?? "", d.Phase), Word = op.Down ? (Text != null && Text.Activity.TryGetValue("down", out var dw) ? dw : "Down") : d.Label });
            if (bw.Reach.Length > 0) p.Facts.Add(new BookFact { Icon = "watch", Word = bw.Reach.Replace("{m}", (g.R.WatchReachCm((OpId)k) / 100).ToString()) });
            if (op.Gun.W != null) p.Gear.Add(new BookFact { Icon = "fire", Word = bw.Weapons.TryGetValue(op.Gun.W.Id, out var wn) ? wn : op.Gun.W.Id, Note = Drawn(k) ? bw.Drawn : bw.Holstered });
            p.Scales.Add(new BookScale { Word = bw.Health, Value = op.Gun.Health, Max = op.Gun.Max });
            foreach (var w in BookWaysOf(k)) p.Ways.Add(w);
            for (int i = 0; i < 4; i++) if (i != k) { var r = Op.Name((OpId)i); p.Others.Add(new BookOther { Agent = i, Face = $"portraits/portrait_{r}_{(g.Ops[i].Down ? "down" : "normal")}", Word = Text != null && Text.Speakers.TryGetValue(r, out var on) ? on : r }); }
            var vt = views?.Find("op/" + role); bool shown = vt && vt.gameObject.activeInHierarchy;
            p.Subject = shown ? vt.position : frame.World(op.Pos); p.Line = shown; p.SubjectView = "op/" + role;
            return p;
        }

        /// <summary>The things a way can be on, as the pointer would name them (TargetOf): the cast, the door, the cabinet, the cameras, the patrol, the cars, the rivals.</summary>
        List<Target> BookTargets()
        {
            var l = new List<Target>();
            foreach (var r in new[] { "waitress", "driver", "guard", "councillor" }) l.Add(new Target { Kind = "person", Key = r, Id = r, Label = Name(r) });
            l.Add(new Target { Kind = "door", Key = "door", Label = "the public door" }); l.Add(new Target { Kind = "cabinet", Key = "cabinet", Label = "the control cabinet" });
            foreach (var c in Game.S.Cameras) l.Add(new Target { Kind = "camera", Key = c.Id, Label = CameraName(c.Id) });
            l.Add(new Target { Kind = "patrol", Key = "patrol", Label = "the patrol" });
            l.Add(new Target { Kind = "car", Key = "official", Label = Text?.TargetName("car-official", "Vale's car") ?? "Vale's car" }); l.Add(new Target { Kind = "car", Key = "getaway", Label = "the getaway car" });
            l.Add(WatcherTarget("car:rivals"));
            return l;
        }

        /// <summary>What an agent can do now, one sign a way, each read from the same rows the ring ranks (ContextFor, BuildPanel), with the selection on that agent alone for the read and put back
        /// before it returns, so the page can't disagree with the ring. Each way lands on the first thing it's open on, else the first where it's locked (its short lock). Fire and the body ways
        /// aren't listed: they belong to a moment, not an agent (the gear slot says the weapon).</summary>
        public List<BookWay> BookWaysOf(int k)
        {
            var keepSel = (bool[])Selected.Clone(); var keepAgent = Agent; var byWord = new Dictionary<string, BookWay>(); var order = new List<string>();
            try
            {
                Array.Clear(Selected, 0, 4); Selected[k] = true; Agent = (OpId)k;
                Vector2 mid = new(Screen.width / 2f, Screen.height / 2f);
                foreach (var t in BookTargets())
                {
                    ContextPlan plan = null; if (!Guard("book", () => plan = ContextFor(t, mid)) || plan?.Pn == null) continue;
                    foreach (var r in plan.Pn.Rows)
                    {
                        var o = r.I?.Order; if (o == null || r.By != (OpId)k || o.Do is "fire" or "carry" or "restrain" or "drop" or "stash" or "go" or "board" or "drive-car") continue;
                        var word = RingWord(r.I); byWord.TryGetValue(word, out var have); if (have != null && (!have.Shut || !r.Available) && !(o.Do == "takedown" && t.Key == "guard" && r.Available)) continue; /* a takedown lands on the guard, never first on a bystander in the cast */
                        if (have == null) order.Add(word);
                        byWord[word] = BookWayFrom(r, t, k, word);
                    }
                }
            }
            finally { Array.Copy(keepSel, Selected, 4); Agent = keepAgent; }
            var l = new List<BookWay>(); foreach (var w in order) l.Add(byWord[w]); return l;
        }

        /// <summary>A sign from a ring's row: the icon, the word, the lock (ShutCalmSegs's ShortOf) or the price (the wedge's PriceLine).</summary>
        BookWay BookWayFrom(PanelRow r, Target t, int k, string word)
        {
            var o = r.I.Order; var w = new BookWay { Word = word, Verb = o.Do + "|" + (o.Approach ?? "") + "|" + (o.How ?? ""), Icon = r.I.Lead.Length > 0 ? r.I.Lead : HudModel.AtlasIcon(o.Do, o.Approach, ""), By = k, Shut = !r.Available, Target = t, LockCode = r.Lock.code ?? "" };
            if (!r.Available) { w.Sub.Add(new BookField { Text = BookLockText(r) }); return w; }
            w.Named = TitleName(t);
            foreach (var f in HudModel.PriceLine(this, r.I)) if (!string.IsNullOrEmpty(f.Text)) w.Sub.Add(new BookField { Icon = f.Icon ?? "", Text = f.Text });
            return w;
        }

        /// <summary>A locked row's words, as the ring's shut wedge prints them.</summary>
        public string BookLockText(PanelRow r) => ShortOf(r.I.Order, r.Lock) ?? r.Reason;
        /// <summary>The proof: a price as the ring's pill prints it.</summary>
        public string BookPriceText(PanelRow r) { var p = new List<string>(); foreach (var f in HudModel.PriceLine(this, r.I)) if (!string.IsNullOrEmpty(f.Text)) p.Add(f.Text); return string.Join(" · ", p); }

        static string TitleName(Target t) { var s = t?.Label ?? ""; if (s.StartsWith("the ", StringComparison.Ordinal)) s = s[4..]; return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s[1..] : s; }
    }
}
