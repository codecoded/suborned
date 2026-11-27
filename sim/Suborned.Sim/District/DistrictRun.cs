using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public sealed class DistrictJob
    {
        public int A, B, C, Stage, Series;
        public bool Bluff;
        public string FirstTie = "", SecondTie = "";
        public int Contact => Stage == 0 ? A : Stage == 1 ? B : C;
        public string Key => Series.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public sealed class DistrictRun
    {
        public readonly DistrictMap Map = DistrictMap.Harbour;
        public Outcome Handover;
        public string HandoverNews = "The Handover is open at the Records Office.";
        public readonly DistrictJob[] Jobs = new DistrictJob[3];
        public readonly DistrictTraffic Traffic;
        public readonly DistrictShopping Shopping;
        public readonly DistrictNetwork Network;
        public readonly int[] Trust;
        public readonly bool[] Encountered, Introduced;
        public readonly string[] Memory;
        public readonly Pt? [] Meetings;
        readonly int[] meetingSince, heldTicks;
        readonly bool[] firstBluff = new bool[3];
        public int Completed;
        public string News = "";
        readonly int seed;
        public DistrictRun(int seed)
        {
            this.seed = seed;
            Traffic = new DistrictTraffic(Map);
            Shopping = new DistrictShopping(Map);
            Network = new DistrictNetwork(this);
            Trust = new int[Map.People.Length];
            Encountered = new bool[Map.People.Length + 4];
            Introduced = new bool[Encountered.Length];
            Memory = new string[Trust.Length];
            Meetings = new Pt? [Trust.Length];
            meetingSince = new int[Trust.Length];
            heldTicks = new int[Trust.Length];
            for (int i = 0; i < Jobs.Length; i++)
                Jobs[i] = Next(i);
            Traffic.Pavement.Open(this,0);
        }

        // The civic shift keeps its established rounds. The other residents work the wider streets.
        public bool Roams(int person) => Map.People[person].Local || person % 3 != 1;
        DistrictJob Next(int series, Func<int, bool>? available = null)
        {
            var occupied = new HashSet<int>();
            foreach (var j in Jobs)
                if (j != null && j != Jobs[series % Jobs.Length])
                {
                    occupied.Add(j.A);
                    occupied.Add(j.B);
                    occupied.Add(j.C);
                }

            var paths = new List<DistrictJob>();
            for (int a = 0; a < Map.People.Length; a++)
            {
                if (!Roams(a) || occupied.Contains(a) || available?.Invoke(a) == false)
                    continue;
                foreach (var first in Map.People[a].Ties)
                {
                    int b = first.person;
                    if (!Roams(b) || occupied.Contains(b) || available?.Invoke(b) == false)
                        continue;
                    foreach (var second in Map.People[b].Ties)
                    {
                        int c = second.person;
                        if (c == a || !Roams(c) || occupied.Contains(c) || available?.Invoke(c) == false)
                            continue;
                        paths.Add(new DistrictJob { A = a, B = b, C = c, Series = series, FirstTie = first.kind, SecondTie = second.kind });
                    }
                }
            }

            if (paths.Count == 0 && available != null)
                return Jobs[series % Jobs.Length];
            if (paths.Count == 0)
                throw new InvalidOperationException("The Harbour graph has too few independent introductions");
            if (series < 6)
            {
                string anchor = new[]
                {
                    "p-pylon-row-east-03",
                    "p-cafe-03",
                    "p-t1-rocket-kiosk-01"
                }[series % 3];
                var authored = paths.FindAll(j => Map.People[j.A].Id == anchor);
                if (authored.Count > 0)
                    paths = authored;
            }

            return paths[(int)(((long)seed * 8191 + (long)series * 3571 + 131) % paths.Count)];
        }

        public void Finish(DistrictJob job)
        {
            if (job.Series < 3)
                firstBluff[job.Series] = job.Bluff;
            Completed++;
            Jobs[job.Series % Jobs.Length] = Next(job.Series + Jobs.Length);
        }

        public bool Replace(DistrictJob job, Func<int, bool> available)
        {
            var next = Next(job.Series + 3, available);
            Jobs[job.Series % 3] = next;
            return next != job;
        }

        internal void Meet(int who, Pt place, int tick)
        {
            if (!Meetings[who].HasValue)
            {
                meetingSince[who] = tick;
                Meetings[who] = place;
            }
        }

        internal void Release(int who, int tick)
        {
            if (Meetings[who].HasValue)
            {
                heldTicks[who] += tick - meetingSince[who];
                Meetings[who] = null;
            }
        }

        public (Pt at, Pt facing, bool moving) Pose(int person, int tick)
        {
            if (!Network.Away(person) && Meetings[person] is Pt meeting) return (meeting,new Pt(0,100),false);
            if (Traffic.Pavement.TryPerson(person,tick,out var p)) return (p.At,p.Facing,p.Moving);
            return ScheduledPose(person,tick);
        }

        internal (Pt at, Pt facing, bool moving) ScheduledPose(int person, int tick)
        {
            var r = Map.People[person];
            if (Network.TryPose(person, tick, out var outing)) return outing;
            if (Meetings[person] is Pt p)
                return (p, new Pt(0, 100), false);
            int elapsed=tick-heldTicks[person];
            if (!r.Local) return DistrictMap.Shuttle(r.Walk, elapsed, r.Phase + seed % 1709, 112, 850);
            // Local workers alternate walking and dwelling at their own frontage. A regular starts
            // the longer daily visit at the work end of the round and returns there before resuming.
            int pace=97+person%31, dwell=48+person%109;
            int phase = r.Phase + seed % 1709;
            if (!(r.Delivery is Path delivery))
                return DistrictMap.Shuttle(r.Walk, elapsed, phase, pace, dwell);

            long half = Math.Max(1, (r.Walk.Length + pace - 1) / pace) + dwell;
            long visit = 2 * (Math.Max(1, (delivery.Length + pace - 1) / pace) + dwell);
            long rounds = Math.Max(1, (24L * 60 * 60 * Game.Hz - visit) / (2 * half));
            long period = rounds * 2 * half + visit;
            long start = Math.Min(rounds - 1, r.DeliveryStart / (2 * half)) * 2 * half + half;
            long local = ((long)elapsed + phase) % period;
            if (local >= start && local < start + visit)
                return DistrictMap.Shuttle(delivery, (int)(local - start), 0, pace, dwell);
            long round = local < start ? local : local - visit;
            return DistrictMap.Shuttle(r.Walk, (int)round, 0, pace, dwell);
        }

        public DistrictJob? Available(int person) => Array.Find(Jobs, j => j.Contact == person);
        public string Title(DistrictJob j) => j.Series >= 3 && j.Series < 6 ? new[]
        {
            "A name inside the lid",
            "The garden encore",
            "A seat for the night shift"
        }[j.Series % 3] : new[]
        {
            "The unclaimed parcel",
            "One more song",
            "The last strip of tickets"
        }[j.Series % 3];
        static string TieWord(string k) => k == "family" ? "relative" : k == "regular" ? "regular contact" : "workmate";
        static string First(string name) => name.Split(' ')[0];
        public string Speaker(DistrictJob j)
        {
            var b = First(Map.People[j.B].Name);
            var c = First(Map.People[j.C].Name);
            if (j.Series >= 3 && j.Series < 6)
                return SecondChapter(j, b, c);
            if (j.Stage == 0)
                return (j.Series % 3) switch
                {
                    0 => $"There's a parcel downstairs with my mother's handwriting on it. No claim slip. My {TieWord(j.FirstTie)} {b} knows who signed for it. Will you take my note?",
                    1 => $"I promised I'd play at the garden tonight. My shift keeps moving. My {TieWord(j.FirstTie)} {b} can arrange cover if somebody asks in person.",
                    _ => $"They've pulled the cheap tram tickets from the kiosk. My {TieWord(j.FirstTie)} {b} still has the old book. Get me a strip for the people on my floor?"};
            if (j.Stage == 1)
                return (j.Series % 3) switch
                {
                    0 => $"I know my {TieWord(j.SecondTie)} {c}'s writing. I can ask for the duplicate slip. Or I could say the desk has already cleared the parcel.",
                    1 => $"My {TieWord(j.SecondTie)} {c} keeps the rota. I could trade my free evening. Or tell them the band was booked by the office.",
                    _ => $"My {TieWord(j.SecondTie)} {c} works the tray. I could ask them to honour the old fare. Or tell them a refund has been signed."};
            if (j.Bluff)
                return $"{b} gave me their word. I released it before I checked the book. Take it. Next time I want to see the paperwork myself.";
            return (j.Series % 3) switch
            {
                0 => $"{b}'s note checks out. Look under that brown paper. One parcel, and a thank-you from the family.",
                1 => $"{b} took my Sunday. I've written the change in my own hand. Tell your musician to play us something good.",
                _ => $"I kept the old fare for the whole building. Here's the strip. {b} said you could be trusted with the change."};
        }

        string SecondChapter(DistrictJob j, string b, string c)
        {
            if (j.Stage == 0)
            {
                string opening = firstBluff[j.Series % 3] ? "The story we told has come back to me. I'd like to put it right." : "You kept your word last time. I can ask you this now.";
                return opening + " " + ((j.Series % 3) switch
                {
                    0 => $"There was a name inside the parcel lid. {b} might help me find the person who packed it.",
                    1 => $"Someone left an old score on the piano. {b} knows who wrote it. I'd like their permission to play it.",
                    _ => $"The night cleaners missed out on those fares. {b} could put aside some seats for their ride home."});
            }

            if (j.Stage == 1)
                return (j.Series % 3) switch
                {
                    0 => $"{c} has the signing book. I'll ask them to pass a letter along, or I can say the writer asked for a copy.",
                    1 => $"{c} keeps the music. Shall I ask to borrow the score, or tell them the composer promised it to us?",
                    _ => $"{c} has the seat list. I can ask for the empty places, or claim the seats were paid for."};
            return j.Bluff ? $"I did what {b} asked. The ledger tells a different story. Take this. I'll remember whose word I took." : (j.Series % 3) switch
            {
                0 => "I'll pass the letter on. If they want to answer they'll know where to find her.",
                1 => "Yes, play it. My brother wrote that tune. Leave two chairs by the piano for us.",
                _ => "Four seats, every night. I've written the cleaners' names on my own list."
            };
        }

        public string Choice(DistrictJob j, bool bluff = false) => j.Series >= 3 && j.Series < 6 ? (j.Stage == 0 ? "Take the request" : j.Stage == 2 ? "Bring back their answer" : bluff ? "Pass on a false assurance" : "Ask in their own name") : j.Stage == 0 ? new[]
        {
            "Carry the note",
            "Ask about a shift swap",
            "Find the ticket book"
        }[j.Series % 3] : j.Stage == 2 ? new[]
        {
            "Collect the parcel",
            "Collect the signed rota",
            "Collect the ticket strip"
        }[j.Series % 3] : bluff ? new[]
        {
            "Say the desk cleared it",
            "Say the office booked the band",
            "Say the refund is signed"
        }[j.Series % 3] : new[]
        {
            "Ask for a duplicate slip",
            "Arrange an honest swap",
            "Ask them to honour the fare"
        }[j.Series % 3];
        public string Lead(DistrictJob j) => j.Stage == 0 ? $"Hear {Map.People[j.A].Name} out." : j.Stage == 1 ? $"{Map.People[j.A].Name} sent you to their {TieWord(j.FirstTie)}, {Map.People[j.B].Name}." : $"{Map.People[j.B].Name} has spoken to {Map.People[j.C].Name}. Collect their answer.";
        internal void Hash(Hash.H h)
        {
            h.S("harbour").S(Map.Digest).I((int)Handover).S(HandoverNews).I(Completed).S(News);
            Traffic.Hash(h);
            Shopping.Hash(h);
            Network.Hash(h);
            foreach (bool bluff in firstBluff)
                h.Z(bluff);
            foreach (var j in Jobs)
                h.I(j.Series).I(j.A).I(j.B).I(j.C).I(j.Stage).Z(j.Bluff);
            for (int i = 0; i < Trust.Length; i++)
                if (Trust[i] != 0 || Memory[i] != null || Meetings[i].HasValue || heldTicks[i] != 0)
                {
                    h.I(i).I(heldTicks[i]).I(meetingSince[i]).I(Trust[i]).S(Memory[i] ?? "").Z(Meetings[i].HasValue);
                    if (Meetings[i] is Pt p)
                        h.I(p.X).I(p.Y);
                }
        }
    }
}
