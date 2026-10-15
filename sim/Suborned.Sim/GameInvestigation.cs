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
    public sealed class Between
    {
        public readonly List<BetweenLine> Lines = new List<BetweenLine>(); public bool Ambush; public readonly List<string> AmbushBy = new List<string>();
        public readonly List<Person> Contacts = new List<Person>();
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
            for (int w = 0; w < 3; w++)
            {
                string id = w == 0 ? "camera:entrance" : w == 1 ? "camera:pickup" : "patrol";
                foreach (var op in Ops)
                {
                    if (op.Inside) continue;
                    bool sees = w == 0 ? CameraWatching("entrance", op.Pos) : w == 1 ? CameraWatching("pickup", op.Pos) : PatrolWatching(op.Pos);
                    string key = id + "/" + Sim.Op.Name(op.Id);
                    if (!sees) { openSighting.Remove(key); continue; }
                    if (openSighting.TryGetValue(key, out var s) && s.To == Tick - Hz) { s.To = Tick; continue; }
                    var n = new Sighting { Watcher = id, Op = Sim.Op.Name(op.Id), From = Tick, To = Tick, At = op.Pos };
                    Sightings.Add(n); openSighting[key] = n;
                    foreach (var c in People) // a logged operative beside a contact ties the contact to the cell (rulebook exposure)
                    {
                        if (!c.Turned || c.Down) continue;
                        var cp = PersonPos(c); if (cp.HasValue && G.Within(cp.Value, op.Pos, R.ApproachReachCm)) ExposedByWatcher.Add(c.Role);
                    }
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
            var ev = R.Ev("investigator.question");
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
                var x = Ctx(p, ev);
                var outcome = FirstOutcome(ev, x).Id; // holds-nothing, gives, lies, nothing: the rulebook's event decides
                var f = new Finding { Hop = hop, Who = p.Role.Length > 0 ? p.Role : p.Id, Outcome = outcome, Lead = lead.what };
                if (outcome == "gives")
                {
                    foreach (var fact in FactsOf(p)) f.Facts.Add(fact);
                    f.Cause = TalkerCause(p);
                    foreach (var m in p.Memory) // each fact's place and time becomes a lead (one step up the chain, §02)
                    {
                        if (m.Act == "told") continue;
                        var at = PlaceAt(m.Place, p);
                        leads.Add((m.Tick, seq++, "told-by:" + f.Who + ":" + m.Act, at, PeopleNear(at, m.Tick)));
                    }
                    if (inv.NamedCause.Length == 0 && f.Facts.Contains(R.FaceFact.TryGetValue("fixer", out var face) ? face : "fixer-face")) inv.NamedCause = "named-witness:" + f.Who;
                }
                else if (outcome == "lies") f.Facts.Add(R.BandOf(Standing) == Band.Hero ? R.LieGivesHero : R.LieGivesOtherwise);
                if (p.Turned && p.Role.Length > 0) inv.Exposed.Add(p.Role);
                inv.Findings.Add(f);
            }
            return inv;
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
            foreach (var m in p.Memory) { if (m.Act == "told") continue; add("place-time"); if (R.FaceFact.TryGetValue(m.By, out var ff)) add(ff); }
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
                c = new Person { Role = p.Role, Id = p.Id, Name = p.Name, Temperament = p.Temperament, Need = p.Need, Job = p.Job, Turned = p.Turned, NeedMet = p.NeedMet, Down = p.Down, KRoutine = p.KRoutine, KNeed = p.KNeed, KTemperament = p.KTemperament, Place = p.Place, Suspected = p.Suspected };
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
            // contact.introduces: who each contact could introduce (the pick is the player's order in the debrief)
            if (ties != null)
                foreach (var p in everyone)
                {
                    if (!p.Turned || p.Down || !ties.TryGetValue(p.Id, out var ts)) continue;
                    var can = new List<string>();
                    foreach (var (kind, with) in ts) if (R.TieIntroduces.TryGetValue(kind, out var intro) && intro && byId.TryGetValue(with, out var q) && !q.Turned && !q.Has(R.RefusedAct)) can.Add(with + " (" + kind + ")");
                    if (can.Count > 0) line("introduced", p, "can introduce " + string.Join(", ", can) + ", up to " + fmt(R.IntroductionsPerContact) + " (the player's pick)");
                }
            foreach (var p in everyone) if (p.Turned) b.Contacts.Add(C(p));
            return b;
        }

        List<string> FactsOfWitnessed(Person p)
        {
            var o = new List<string>();
            foreach (var m in p.Memory) { if (m.Act == "told") continue; if (!o.Contains("place-time")) o.Add("place-time"); if (R.FaceFact.TryGetValue(m.By, out var f) && !o.Contains(f)) o.Add(f); }
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
