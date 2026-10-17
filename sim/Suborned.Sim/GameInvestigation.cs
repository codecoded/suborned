// What the city learns, and what happens before the next mission (brief 011 A, task 3).
// During the mission: every incident keeps its place, time and witnesses; the watchers (the two cameras and the patrol: §05 watchers)
// log any operative they see, with place and time, act or no act; a watcher that logs an operative within approach reach of a contact
// exposes the contact's job (rulebook exposure, Motive's review #12); in a Menace district a sighting counts as a witness report.
// After the mission, for the debrief: the investigation (rulebook investigation, investigator.question) and the between-missions
// events (rulebook phases.between-missions) run on copies of the people, so the mission's state and its checkpoint hashes are untouched.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class Incident { public int Tick; public string Kind = "", Cause = ""; public Pt At; public readonly List<(Person p, Pt at)> Witnesses = new List<(Person, Pt)>(); }
    public sealed class Sighting { public string Watcher = "", Op = ""; public int From, To; public Pt At; }
    public sealed class Finding { public string Who = "", Outcome = "", Lead = ""; public readonly List<string> Facts = new List<string>(); public string Cause = ""; public int Hop; }
    public sealed class Investigation
    {
        public int Investigators, Hops; public readonly List<Finding> Findings = new List<Finding>(); public readonly HashSet<string> Exposed = new HashSet<string>(StringComparer.Ordinal);
        public string NamedCause = "";
    }
    public sealed class BetweenLine { public string Line = "", Who = "", Detail = ""; }
    /// <summary>One district's Standing across the mission and the break (brief 014; rulebook standing, debrief.lines.standing): where it
    /// started, where the mission left it, the word, the drift, where the next mission starts, and the line's one cause.</summary>
    public sealed class StandingLine
    {
        public string District = "", Band = "", Cause = "", Who = "";
        public int From, End, Found, Word, WordUncapped, Drift, To; public bool Moved;
        /// <summary>Every move, in order: the mission's (seen, found), a killing found after it, then each teller's word before the cap.</summary>
        public readonly List<(int delta, string cause, string who)> Moves = new List<(int, string, string)>();
    }
    public sealed class Between
    {
        public readonly List<BetweenLine> Lines = new List<BetweenLine>(); public bool Ambush; public readonly List<string> AmbushBy = new List<string>();
        public readonly List<Person> Contacts = new List<Person>();
        // brief 013 (Sky 2026-10-16; Motive §2.5, §2.6): what the takedowns leave the campaign. No campaign layer runs yet: these are outputs.
        public int CampaignTrail, CampaignStanding, OpenCases, NextLevel;
        public readonly List<(string who, string kind, string place, int missions)> Cases = new List<(string, string, string, int)>(); // kind missing (open) or found-killing (closes)
        public readonly List<(string place, int guards, int cameras, int missions)> AreaWatch = new List<(string, int, int, int)>();
        /// <summary>brief 014: one Standing line per district (the Handover plays one) and the Standing the next mission starts at.</summary>
        public readonly List<StandingLine> Standing = new List<StandingLine>();
        public readonly List<(string who, string toward)> Word = new List<(string, string)>(); // standing.word's tellers, by id, and which way
        public readonly List<string> FoundAfter = new List<string>(); // killings found after the mission whose Standing moves now (bodies.surface)
    }

    /// <summary>The seeded streams (rulebook streams): FNV-1a 32 and mulberry32, integer draws; the same as contracts/rules/rulebook.ref.ts.</summary>
    public static class Streams
    {
        public static uint Fnv1a32(string key) { unchecked { uint h = 0x811c9dc5; foreach (var c in key) { h ^= c; h *= 0x01000193; } return h; } }
        public sealed class Mulberry
        {
            uint s;
            public Mulberry(string key) { s = Fnv1a32(key); if (s == 0) s = 1; }
            public uint Next()
            {
                unchecked
                {
                    s += 0x6d2b79f5; uint t = s;
                    t = (t ^ (t >> 15)) * (t | 1);
                    t ^= t + (t ^ (t >> 7)) * (t | 61);
                    return t ^ (t >> 14);
                }
            }
        }
        /// <summary>x = floor(u × total ÷ 2^32); walk the items in order subtracting each weight.</summary>
        public static T Draw<T>(string key, IReadOnlyList<(T item, int weight)> items)
        {
            if (items.Count == 0) throw new ArgumentException("nothing to draw");
            long total = 0; foreach (var i in items) { if (i.weight <= 0) throw new ArgumentException("weights are positive"); total += i.weight; }
            long x = (long)(((ulong)new Mulberry(key).Next() * (ulong)total) >> 32);
            foreach (var i in items) { x -= i.weight; if (x < 0) return i.item; }
            return items[items.Count - 1].item;
        }
    }

    public sealed partial class Game
    {
        public readonly List<Incident> Incidents = new List<Incident>();
        public readonly List<Sighting> Sightings = new List<Sighting>();
        public readonly SortedSet<string> ExposedByWatcher = new SortedSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, Sighting> openSighting = new Dictionary<string, Sighting>(StringComparer.Ordinal);

        Incident AddIncident(string kind, string cause, Pt at, IEnumerable<(Person p, Pt at)>? witnesses = null)
        {
            var i = new Incident { Tick = Tick, Kind = kind, Cause = cause, At = at };
            if (witnesses != null) i.Witnesses.AddRange(witnesses);
            Incidents.Add(i); return i;
        }
        List<(Person p, Pt at)> WitnessesWithPos(Pt at, Person? except)
        {
            var seen = new List<(Person, Pt)>();
            foreach (var (p, wp) in OnTheStreet()) { if (p == except) continue; if (G.Within(wp, at, witnessRange) && Sees(wp, at)) seen.Add((p, wp)); }
            return seen;
        }

        /// <summary>§05 watchers, once a second: each live watcher logs every operative in its sightline, as spans of place and time.</summary>
        void Watchers()
        {
            if (Tick % Hz != 0) return;
            var world = new List<string>(); foreach (var c in S.Cameras) if (c.World) world.Add(c.Id); // the city's own cameras (brief 013), after the mission's
            for (int w = 0; w < 3 + world.Count; w++)
            {
                string id = w == 0 ? "camera:entrance" : w == 1 ? "camera:pickup" : w == 2 ? "patrol" : "camera:" + world[w - 3];
                foreach (var op in Ops)
                {
                    if (op.Inside) continue;
                    bool sees = w == 0 ? CameraWatching("entrance", op.Pos) : w == 1 ? CameraWatching("pickup", op.Pos) : w == 2 ? PatrolWatching(op.Pos) : CameraWatching(world[w - 3], op.Pos);
                    string key = id + "/" + Sim.Op.Name(op.Id);
                    if (!sees) { openSighting.Remove(key); continue; }
                    foreach (var c in People) // a logged operative beside a contact, at any second of the sighting, ties the contact to the cell (rulebook exposure)
                    {
                        if (!c.Turned || c.Down) continue;
                        var cp = PersonPos(c); if (cp.HasValue && G.Within(cp.Value, op.Pos, R.ApproachReachCm)) ExposedByWatcher.Add(c.Role);
                    }
                    if (openSighting.TryGetValue(key, out var s) && s.To == Tick - Hz) { s.To = Tick; continue; }
                    var n = new Sighting { Watcher = id, Op = Sim.Op.Name(op.Id), From = Tick, To = Tick, At = op.Pos };
                    Sightings.Add(n); openSighting[key] = n;
                    if (R.BandOf(Standing) == Band.Menace) // §05: in a Menace district a sighting counts as a witness report
                    {
                        Reported++;
                        RaiseAlert(R.AlertByIncident[Math.Min(Reported, R.AlertByIncident.Length) - 1], "sighting:" + id);
                        AddIncident("report", "sighting:" + id, op.Pos);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ after the mission

        /// <summary>The investigation (rulebook investigation; investigator.question), worked from the leads at the end of the mission.</summary>
        public Investigation Investigate()
        {
            if (EndOpenFight()) fightClosedLate = true; // the mission is over: a fight still open ends with it (fight.public)
            var inv = new Investigation();
            if (Trail < R.TrailOpen) return inv;
            inv.Investigators = R.InvestigatorsBase + R.InvestigatorsPerAlert * Alert;
            inv.Hops = inv.Investigators * R.InvestigatorHops;
            // leads: incidents with their witnesses, and the watchers' sightings (witnesses: who stood within range of the place then)
            var leads = new List<(int tick, int seq, string what, Pt at, List<(Person p, Pt at)> who)>();
            int seq = 0;
            foreach (var i in Incidents) leads.Add((i.Tick, seq++, "incident:" + i.Cause, i.At, new List<(Person, Pt)>(i.Witnesses)));
            foreach (var s in Sightings) leads.Add((s.From, seq++, "sighting:" + s.Watcher + ":" + s.Op, s.At, PeopleNear(s.At, s.From)));
            var spent = new HashSet<int>(); var asked = new HashSet<Person>();
            for (int hop = 1; hop <= inv.Hops; hop++)
            {
                int pick = -1;
                for (int k = 0; k < leads.Count; k++)
                {
                    if (spent.Contains(leads[k].seq)) continue;
                    if (pick < 0 || leads[k].tick > leads[pick].tick || (leads[k].tick == leads[pick].tick && leads[k].seq > leads[pick].seq)) pick = k;
                }
                if (pick < 0) break;
                var lead = leads[pick];
                var who = lead.who.FindAll(x => !asked.Contains(x.p) && !x.p.Down);
                who.Sort((a, b) => { long da = G.D2(a.at, lead.at), db = G.D2(b.at, lead.at); return da != db ? da.CompareTo(db) : string.CompareOrdinal(a.p.Id, b.p.Id); });
                if (who.Count == 0) { spent.Add(lead.seq); inv.Findings.Add(new Finding { Hop = hop, Who = "", Outcome = "no-one-to-ask", Lead = lead.what }); continue; }
                if (who.Count == 1) spent.Add(lead.seq);
                var p = who[0].p; asked.Add(p);
                var f = Question(p, lead.what); f.Hop = hop;
                if (f.Outcome == "gives" || f.Outcome == "volunteers") // volunteers: a reporter (brief 013)
                {
                    foreach (var m in p.Memory) // each fact's place and time becomes a lead (one step up the chain, §02)
                    {
                        if (m.Act == "told") continue;
                        var at = PlaceAt(m.Place, p);
                        leads.Add((m.Tick, seq++, "told-by:" + f.Who + ":" + m.Act, at, PeopleNear(at, m.Tick)));
                    }
                    if (inv.NamedCause.Length == 0 && f.Facts.Contains(R.FaceFact.TryGetValue("fixer", out var face) ? face : "fixer-face")) inv.NamedCause = "named-witness:" + f.Who;
                }
                if (p.Turned && p.Role.Length > 0) inv.Exposed.Add(p.Role);
                inv.Findings.Add(f);
            }
            return inv;
        }

        /// <summary>What this person would tell an investigator now (rulebook investigator.question): the outcome, the facts and, for a
        /// Talker, the cause in words. Read-only; Investigate asks each person it reaches exactly this, so a scenario can test the Talker
        /// rule on a person whether or not the investigation's hops reach them.</summary>
        public Finding Question(Person p, string lead = "")
        {
            var ev = R.Ev("investigator.question");
            var outcome = FirstOutcome(ev, Ctx(p, ev)).Id; // holds-nothing, gives, lies, nothing: the rulebook's event decides
            var f = new Finding { Who = p.Role.Length > 0 ? p.Role : p.Id, Outcome = outcome, Lead = lead };
            if (outcome == "gives") { foreach (var fact in FactsOf(p)) f.Facts.Add(fact); f.Cause = TalkerCause(p); }
            else if (outcome == "volunteers") foreach (var fact in FactsOf(p)) f.Facts.Add(fact); // a reporter gives what they hold, no Talker test (brief 013)
            else if (outcome == "lies") f.Facts.Add(R.BandOf(Standing) == Band.Hero ? R.LieGivesHero : R.LieGivesOtherwise);
            return f;
        }

        /// <summary>§04 Talker test in words: "fear afraid 2 + resentment resentful 1 > bravery helper 1 + loyalty none 0" (Motive K7).</summary>
        string TalkerCause(Person p)
        {
            string side(int[] ix) { var parts = new List<string>(); foreach (var i in ix) parts.Add(Rules.ScaleNames[i] + " " + R.Step(i, p.Sc[i]) + " " + p.Sc[i].ToString(System.Globalization.CultureInfo.InvariantCulture)); return string.Join(" + ", parts); }
            return side(R.TalkerLeft) + " > " + side(R.TalkerRight);
        }
        public List<string> FactsOf(Person p)
        {
            var o = new List<string>();
            void add(string f) { if (!o.Contains(f)) o.Add(f); }
            foreach (var m in p.Memory) { if (m.Act == "told") continue; add("place-time"); foreach (var af in R.ActFacts[m.Act]) add(af); if (R.FaceFact.TryGetValue(m.By, out var ff)) add(ff); } // the act's facts (brief 013: enforcer-face), then the face of who did it
            foreach (var m in p.Memory) if (m.Act == "told" && m.Told != null) foreach (var f in m.Told.Split('|')) if (f.Length > 0) add(f);
            o.Sort((a, b) => R.Facts.IndexOf(a).CompareTo(R.Facts.IndexOf(b)));
            return o;
        }
        Pt PlaceAt(string place, Person p)
        {
            if (S.HasPlace(place)) return S.Stand(place);
            if (People2 != null && People2.Places.TryGetValue(place, out var c)) return c;
            return PersonPos(p) ?? S.Stand(S.GuardPost);
        }
        /// <summary>The simulated people within witness range of `at` at tick `t` on a clear sightline: the crowd where their routine had
        /// them, the cast where they stand for the mission.</summary>
        List<(Person p, Pt at)> PeopleNear(Pt at, int t)
        {
            var o = new List<(Person, Pt)>();
            foreach (var p in People) { var pp = PersonPos(p); if (pp.HasValue && G.Within(pp.Value, at, witnessRange) && Sees(pp.Value, at)) o.Add((p, pp.Value)); }
            if (People2 != null && Start.Crowd) foreach (var w in Walkers) { var wp = CrowdPos(w, t); if (wp.HasValue && G.Within(wp.Value, at, witnessRange) && Sees(wp.Value, at)) o.Add((w.P, wp.Value)); }
            return o;
        }

        /// <summary>The between-missions events (rulebook phases.between-missions), in their order, on copies of the people: what the next
        /// mission inherits, as debrief lines with causes (K7, §08). `mission` is this mission's number in the campaign (the Handover: 1).</summary>
        public Between BetweenMissions(Investigation inv, int mission = 1)
        {
            var b = new Between();
            var copies = new Dictionary<Person, Person>();
            Person C(Person p)
            {
                if (copies.TryGetValue(p, out var c)) return c;
                c = new Person { Role = p.Role, Id = p.Id, Name = p.Name, Temperament = p.Temperament, Need = p.Need, Job = p.Job, Turned = p.Turned, NeedMet = p.NeedMet, Down = p.Down, KRoutine = p.KRoutine, KNeed = p.KNeed, KTemperament = p.KTemperament, Place = p.Place, Suspected = p.Suspected, Record = p.Record, Size = p.Size, Holder = p.Holder };
                Array.Copy(p.Sc, c.Sc, 4); c.Memory.AddRange(p.Memory); foreach (var a in p.KnownRefused) c.KnownRefused.Add(a);
                copies[p] = c; return c;
            }
            var everyone = new List<Person>(People); foreach (var w in Walkers) everyone.Add(w.P);
            everyone.Sort((x, y) => string.CompareOrdinal(x.Id, y.Id));
            var byId = new Dictionary<string, Person>(StringComparer.Ordinal); foreach (var p in everyone) byId[p.Id] = p;
            void line(string l, Person who, string detail) => b.Lines.Add(new BetweenLine { Line = l, Who = who.Role.Length > 0 ? who.Role : who.Id, Detail = detail });
            string fmt(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // ladder.job: each job done this mission, exposed or not (rulebook exposure: questioned, or a watcher logged an operative beside them)
            foreach (var p in everyone)
            {
                if (!p.Turned || p.Down) continue;
                foreach (var m in p.Memory)
                {
                    if (m.Act != "job") continue;
                    var c = C(p); bool exposed = ExposedByWatcher.Contains(p.Role) || inv.Exposed.Contains(p.Role);
                    string move = exposed ? "job-exposed" : "job-unexposed";
                    var (k, by) = R.Ladders[move][0];
                    int was = c.Sc[k]; foreach (var (kk, bb) in R.Ladders[move]) c.Sc[kk] = R.Clamp(kk, c.Sc[kk] + bb);
                    line("ladder", p, $"{move}: {Rules.ScaleNames[k]} {R.Step(k, was)} → {R.Step(k, c.Sc[k])}");
                }
            }
            // contact.informs, mercenary.sells: one ambush in the next mission however many (Motive's review #4)
            foreach (var p in everyone)
            {
                if (!p.Turned || p.Down) continue;
                var c = C(p);
                if (c.Sc[Rules.Resentment] >= R.ScaleMax[Rules.Resentment]) { b.Ambush = true; b.AmbushBy.Add(c.Role.Length > 0 ? c.Role : c.Id); line("informer", p, "resentment " + R.Step(Rules.Resentment, c.Sc[Rules.Resentment]) + ": sells a route; an ambush is scheduled"); }
                else if (c.Temperament == "mercenary" && R.BandOf(Standing) == Band.Menace && c.Sc[Rules.Loyalty] <= R.ScaleMin[Rules.Loyalty]) { b.Ambush = true; b.AmbushBy.Add(c.Role.Length > 0 ? c.Role : c.Id); line("sold", p, "a Mercenary in a Menace district who owes the cell nothing sells a route"); }
            }
            // debtor.lapses: a met debt ends the ties that carried it
            var ties = People2?.Ties;
            foreach (var p in everyone)
            {
                if (!p.NeedMet || p.Need != "debt" || ties == null || !ties.TryGetValue(p.Id, out var ts)) continue;
                foreach (var (kind, with) in ts) if (kind == "debtor") line("tie-lapsed", p, "debtor tie to " + with + ": the debt is cleared");
            }
            // need.replaced (Sky Q2): from the person's own stream, {person}/need/{mission}
            foreach (var p in everyone)
            {
                if (!p.NeedMet) continue;
                var c = C(p); string old = c.Need;
                c.Need = ReplacementNeed(c, mission); c.NeedMet = false;
                if (People2 != null) { c.Record = People2.DefaultRecord(c.Need); c.Size = c.Record.Length > 0 ? People2.Records[c.Record].size : ""; c.Holder = c.Record.Length > 0 ? "beyond" : ""; } // brief 017 phase 1: the kind's record, no draw
                if (c.Turned) c.KNeed = true; else c.KNeed = false;
                line("need-replaced", p, $"{old} → {c.Need}{(c.Turned ? " (told free: a contact)" : " (Listen again)")}");
            }
            // word.passes (K3, Sky Q6): facts, never opinion, along family and workmate ties, one hop; an obliged contact keeps quiet
            if (ties != null)
                foreach (var p in everyone)
                {
                    var facts = FactsOfWitnessed(p);
                    if (facts.Count == 0 || C(p).Sc[Rules.Loyalty] >= R.WordQuietLoyalty || !ties.TryGetValue(p.Id, out var ts)) continue;
                    foreach (var (kind, with) in ts)
                    {
                        if ((kind != "family" && kind != "workmate") || !byId.TryGetValue(with, out var q)) continue;
                        var cq = C(q); var held = FactsOf(cq); var add = facts.FindAll(f => !held.Contains(f));
                        if (add.Count == 0) continue;
                        cq.Memory.Add(new Mem { Act = "told", By = p.Id, Place = cq.Place, Tick = Tick, Witnessed = false, Told = string.Join("|", add) });
                        line("word", p, $"tells {(q.Role.Length > 0 ? q.Role : q.Id)} ({kind}): {string.Join(", ", add)}");
                    }
                }
            // standing.word (brief 014): each person the cell helped, pressured or knocked out, alive, with a tie of a kind in standing.word.ties,
            // tells them how a stranger treated them: a feeling, never a fact, so no memory or knowledge moves; harm outranks help
            var word = new List<(int delta, string cause, string who)>();
            foreach (var p in everyone)
            {
                var done = DoneTo(p);
                if (done.Count == 0 || !Alive(p) || ties == null || !ties.TryGetValue(p.Id, out var wts) || !wts.Exists(t => R.WordTies.Contains(t.Item1))) continue;
                string how = done.Contains("pressured") ? "pressured" : done.Contains("knocked-out") ? "knocked-out" : "helped";
                int d = how == "helped" ? R.WordStandingStep : -R.WordStandingStep;
                word.Add((d, "word-" + how, Who(p))); b.Word.Add((Who(p), how == "helped" ? "hero" : "menace"));
            }
            // contact.introduces: who each contact could introduce (the pick is the player's order in the debrief)
            if (ties != null)
                foreach (var p in everyone)
                {
                    if (!p.Turned || p.Down || !ties.TryGetValue(p.Id, out var ts)) continue;
                    var can = new List<string>();
                    foreach (var (kind, with) in ts) if (R.TieIntroduces.TryGetValue(kind, out var intro) && intro && byId.TryGetValue(with, out var q) && !q.Turned && !q.Has(R.RefusedAct)) can.Add(with + " (" + kind + ")");
                    if (can.Count > 0) line("introduced", p, "can introduce " + string.Join(", ", can) + ", up to " + fmt(R.IntroductionsPerContact) + " (the player's pick)");
                }
            Takedowns(b, C, byId, line, fmt);
            StandingNext(b, word, fmt);
            foreach (var p in everyone) if (p.Turned) b.Contacts.Add(C(p));
            return b;
        }

        /// <summary>standing.done-to: what the cell did to the person this mission that their word carries (rulebook DoneTo): a Help that landed
        /// (memory help, any outcome), a Pressure that landed (memory pressure), a knockout that landed (a takedown ko, not a Take or a kill).</summary>
        List<string> DoneTo(Person p)
        {
            var o = new List<string>();
            if (p.Has(R.HelpedAct)) o.Add("helped");
            if (p.Has("pressure")) o.Add("pressured");
            if (Acts.Exists(a => a.P == p && !a.Take && !a.Kill && a.Lead != null)) o.Add("knocked-out");
            return o;
        }
        /// <summary>standing.alive: not killed by a takedown that landed (so not missing either) and not shot down; a knocked-out body lives.</summary>
        bool Alive(Person p)
        {
            if (Acts.Exists(a => a.P == p && !a.Take && a.Kill && a.Lead != null)) return false;
            if (!p.Down) return true;
            var body = BodyOf(p); return body != null && !body.Dead;
        }

        /// <summary>The district's Standing for the next mission (rulebook standing; standing.word, standing.drift) and its one debrief line.
        /// The Handover plays in one district (sim/data/handover.provisional.json district), so one line.</summary>
        void StandingNext(Between b, List<(int delta, string cause, string who)> word, Func<int, string> fmt)
        {
            var l = new StandingLine { District = S.District, From = StandingAtStart, End = Standing, Found = b.CampaignStanding };
            foreach (var m in StandingMoves) l.Moves.Add((m.delta, m.cause, m.who));
            foreach (var v in b.FoundAfter) l.Moves.Add((R.FoundKillStanding, "found-killing", v)); // a killing found after the mission (bodies.surface)
            int sum = 0; foreach (var w in word) { sum += w.delta; l.Moves.Add(w); }
            l.WordUncapped = sum; l.Word = Math.Max(-R.WordStandingCap, Math.Min(R.WordStandingCap, sum));
            l.Moved = l.Moves.Count > 0;
            int at = l.End + l.Found + l.Word;
            if (!l.Moved) { int gap = R.StandingStart - at; l.Drift = Math.Sign(gap) * Math.Min(R.StandingDrift, Math.Abs(gap)); }
            l.To = at + l.Drift;
            l.Band = Rules.BandId(R.BandOf(l.To));
            // the cause: the one that moved it most toward the net change (earliest on a tie); balanced when the moves cancel; else the drift
            int net = l.To - l.From - l.Drift;
            if (!l.Moved) { l.Cause = l.Drift < 0 ? "drift-down" : l.Drift > 0 ? "drift-up" : "steady"; }
            else if (net == 0) l.Cause = "balanced";
            else
            {
                var order = new List<string>(); var by = new Dictionary<string, int>(StringComparer.Ordinal); var first = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var (d, c, w) in l.Moves) { if (!by.ContainsKey(c)) { by[c] = 0; order.Add(c); first[c] = w; } by[c] += d; }
                int best = 0; foreach (var c in order) { int v = by[c] * Math.Sign(net); if (v > best) { best = v; l.Cause = c; l.Who = first[c]; } }
            }
            if (!R.StandingCauses.Contains(l.Cause)) throw new InvalidOperationException($"standing cause '{l.Cause}' is not in the rulebook");
            b.Standing.Add(l);
            string sg(int v) => (v > 0 ? "+" : "") + fmt(v);
            b.Lines.Add(new BetweenLine { Line = "standing", Who = l.Who, Detail = $"{l.District}: {sg(l.From)} → {sg(l.To)} {l.Band} ({l.Cause}{(l.Who.Length > 0 ? ": " + l.Who : "")}; mission {sg(l.End - l.From)}, found {sg(l.Found)}, word {sg(l.Word)}{(l.Word != l.WordUncapped ? " of " + sg(l.WordUncapped) : "")}, drift {sg(l.Drift)})" });
        }

        /// <summary>Brief 013's between-missions events, in the phase order (bodies.surface, body.grieved, missing.worried, area.hardens,
        /// cases.count): a hidden killing is a missing person, an open or found one a found killing; a knockout still down comes home and
        /// gives a statement; ties grieve or worry; the block hardens; the cases set the district's next level. On copies, as the rest.</summary>
        void Takedowns(Between b, Func<Person, Person> C, Dictionary<string, Person> byId, Action<string, Person, string> line, Func<int, string> fmt)
        {
            if (!Acts.Exists(x => !x.Take)) { b.NextLevel = S.SecurityLevel; return; } // a Take seen is an incident, never a body
            var found = new List<TdAct>(); var missing = new List<TdAct>();
            foreach (var a in Acts)
            {
                if (a.Take) continue;
                var body = BodyOf(a.P);
                string where = a.Scene.ToString();
                if (a.Kill)
                {
                    bool hidden = body != null && !body.Open && !a.Confirmed;
                    if (hidden) { missing.Add(a); b.CampaignTrail += R.MissingTrail; line("missing", a.P, $"{(body!.Stash)}: {fmt(R.MissingTrail)} T, no Menace; the case stays open until the body is found"); }
                    else
                    {
                        found.Add(a);
                        if (!a.ActTrail) { b.CampaignTrail += R.HitTrail + R.DownTrail + R.KillTrail; line("case", a.P, $"found after the mission where he lay: {fmt(R.HitTrail + R.DownTrail + R.KillTrail)} T"); }
                        if (!a.StandingPaid) { b.CampaignStanding += R.FoundKillStanding; b.FoundAfter.Add(Who(a.P)); }
                    }
                    continue;
                }
                if (body != null && body.P.Down && !a.Statement) // a knockout still down at the end: he comes home and gives a statement (Motive §3.2)
                {
                    var c = C(a.P);
                    c.Sc[Rules.Fear] = R.Clamp(Rules.Fear, c.Sc[Rules.Fear] + 1); if (a.Aware) c.Sc[Rules.Resentment] = R.Clamp(Rules.Resentment, c.Sc[Rules.Resentment] + 1);
                    if (!a.ActTrail) b.CampaignTrail += R.HitTrail + R.DownTrail;
                    line("statement", a.P, $"comes home: place and time{(a.Aware ? " and the Enforcer's face" : "")}; {fmt(a.ActTrail ? 0 : R.HitTrail + R.DownTrail)} T");
                }
            }
            // ties: grief for a found killing (a turned contact takes the saw-violence move; everyone else fear +1), worry for a missing person
            void ties(TdAct a, string what)
            {
                if (People2 == null || !People2.Ties.TryGetValue(a.P.Id, out var ts)) return;
                var ids = new List<string>(); foreach (var (k, id) in ts) if (R.CloseTies.Contains(k) && !ids.Contains(id)) ids.Add(id);
                ids.Sort(StringComparer.Ordinal);
                foreach (var id in ids)
                {
                    if (!byId.TryGetValue(id, out var q)) continue;
                    var c = C(q);
                    if (what == "grief" && q.Turned) { foreach (var (k, by) in R.Ladders["saw-violence"]) c.Sc[k] = R.Clamp(k, c.Sc[k] + by); line("grief", q, $"{Who(a.P)}: saw-violence"); }
                    else { c.Sc[Rules.Fear] = R.Clamp(Rules.Fear, c.Sc[Rules.Fear] + R.MissingTiesFear); line(what, q, $"{Who(a.P)}: fear +{fmt(R.MissingTiesFear)}"); }
                }
            }
            foreach (var a in found) ties(a, "grief");
            foreach (var a in missing) ties(a, "worry");
            foreach (var a in found) { b.AreaWatch.Add((a.Scene.ToString(), R.AreaWatchStep, R.AreaWatchStep, R.AreaWatchMissions)); b.Cases.Add((Who(a.P), "found-killing", a.Scene.ToString(), R.CaseCloseMissions)); }
            foreach (var a in missing) { b.AreaWatch.Add((a.Scene.ToString(), R.AreaWatchStep, R.AreaWatchStep, R.AreaWatchMissions)); b.Cases.Add((Who(a.P), "missing", a.Scene.ToString(), -1)); }
            foreach (var w in b.AreaWatch) b.Lines.Add(new BetweenLine { Line = "area-watch", Who = "", Detail = $"{w.place}: +{fmt(w.guards)} guard, +{fmt(w.cameras)} camera for {fmt(w.missions)} missions" });
            b.OpenCases = b.Cases.Count;
            b.NextLevel = Math.Min(4, Math.Min(S.SecurityBase + R.CaseLevelCap, S.SecurityBase + b.OpenCases / R.CasesPerLevel));
            foreach (var c in b.Cases) b.Lines.Add(new BetweenLine { Line = "case", Who = c.who, Detail = c.kind == "missing" ? "missing: open until the body is found" : $"found killing: closes after {fmt(c.missions)} missions" });
        }

        List<string> FactsOfWitnessed(Person p)
        {
            var o = new List<string>();
            foreach (var m in p.Memory) { if (m.Act == "told") continue; if (!o.Contains("place-time")) o.Add("place-time"); foreach (var af in R.ActFacts[m.Act]) if (!o.Contains(af)) o.Add(af); if (R.FaceFact.TryGetValue(m.By, out var f) && !o.Contains(f)) o.Add(f); }
            return o;
        }

        /// <summary>need.replaced's draw: people.rules needs in order, `requires` met, metBy ∩ respondsTo, never the need just met; equal weights
        /// until the people export carries each person's cast-row need weights (Splice; rulebook streams.weights).</summary>
        string ReplacementNeed(Person p, int mission)
        {
            var t = R.Temp(p.Temperament); var ties = People2?.Ties; var myTies = ties != null && ties.TryGetValue(p.Id, out var ts) ? ts : new List<(string, string)>();
            bool home = People2 != null && People2.HasHome.TryGetValue(p.Id, out var h) && h, work = People2 != null && People2.HasWork.TryGetValue(p.Id, out var wk) && wk;
            var cands = new List<(string item, int weight)>();
            foreach (var id in R.NeedIds)
            {
                if (id == p.Need) continue;
                var n = R.Needs[id];
                bool ok = n.Requires switch { "nothing" => true, "home" => home, "work" => work, "family-tie" => myTies.Exists(x => x.Item1 == "family"), "rival-tie" => myTies.Exists(x => x.Item1 == "rival"), _ => false };
                bool acts = false; foreach (var a in n.MetBy) if (t.RespondsTo.Contains(a)) acts = true;
                if (ok && acts) cands.Add((id, 1));
            }
            if (cands.Count == 0) return p.Need;
            var key = R.StreamKeys["need-replace"].Replace("{person}", p.Id).Replace("{mission}", mission.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return Streams.Draw(key, cands);
        }
    }
}
