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
        public readonly int[] Trust;
        public readonly string[] Memory;
        public readonly Pt?[] Meetings;
        public int Completed;
        public string News = "";
        readonly int seed;
        public DistrictRun(int seed)
        {
            this.seed = seed; Trust = new int[Map.People.Length]; Memory = new string[Trust.Length]; Meetings = new Pt?[Trust.Length];
            for (int i=0;i<Jobs.Length;i++) Jobs[i] = Next(i);
        }
        // The civic shift keeps its established rounds. The other residents work the wider streets.
        public bool Roams(int person) => person % 3 != 1;
        DistrictJob Next(int series)
        {
            var occupied = new HashSet<int>();
            foreach (var j in Jobs) if (j != null && j != Jobs[series%Jobs.Length]) { occupied.Add(j.A); occupied.Add(j.B); occupied.Add(j.C); }
            var paths = new List<DistrictJob>();
            for (int a=0;a<Map.People.Length;a++)
            {
                if (!Roams(a) || occupied.Contains(a)) continue;
                foreach (var first in Map.People[a].Ties)
                {
                    int b = first.person; if (!Roams(b) || occupied.Contains(b)) continue;
                    foreach (var second in Map.People[b].Ties)
                    {
                        int c=second.person; if(c==a || !Roams(c) || occupied.Contains(c)) continue;
                        paths.Add(new DistrictJob { A=a, B=b, C=c, Series=series, FirstTie=first.kind, SecondTie=second.kind });
                    }
                }
            }
            if (paths.Count == 0) throw new InvalidOperationException("The Harbour graph has too few independent introductions");
            return paths[(int)(((long)seed*8191+(long)series*3571+131)%paths.Count)];
        }
        public void Finish(DistrictJob job) { Completed++; Jobs[job.Series % Jobs.Length] = Next(job.Series + Jobs.Length); }
        public (Pt at, Pt facing, bool moving) Pose(int person,int tick)
        {
            var r=Map.People[person]; if(Meetings[person] is Pt p) return (p,new Pt(0,100),false);
            return DistrictMap.Shuttle(r.Walk,tick,r.Phase+seed%1709,112,850);
        }
        public DistrictJob? Available(int person) => Array.Find(Jobs,j=>j.Contact==person);
        public string Title(DistrictJob j) => new[]{"The unclaimed parcel", "One more song", "The last strip of tickets"}[j.Series%3];
        static string TieWord(string k) => k=="family"?"relative":k=="regular"?"regular contact":"workmate";
        static string First(string name) => name.Split(' ')[0];
        public string Speaker(DistrictJob j)
        {
            var b=First(Map.People[j.B].Name); var c=First(Map.People[j.C].Name);
            if(j.Stage==0) return (j.Series%3) switch {
                0 => $"There's a parcel downstairs with my mother's handwriting on it. No claim slip. My {TieWord(j.FirstTie)} {b} knows who signed for it. Will you take my note?",
                1 => $"I promised I'd play at the garden tonight. My shift keeps moving. My {TieWord(j.FirstTie)} {b} can arrange cover if somebody asks in person.",
                _ => $"They've pulled the cheap tram tickets from the kiosk. My {TieWord(j.FirstTie)} {b} still has the old book. Get me a strip for the people on my floor?" };
            if(j.Stage==1) return (j.Series%3) switch {
                0 => $"I know my {TieWord(j.SecondTie)} {c}'s writing. I can ask for the duplicate slip. Or I could say the desk has already cleared the parcel.",
                1 => $"My {TieWord(j.SecondTie)} {c} keeps the rota. I could trade my free evening. Or tell them the band was booked by the office.",
                _ => $"My {TieWord(j.SecondTie)} {c} works the tray. I could ask them to honour the old fare. Or tell them a refund has been signed." };
            if(j.Bluff) return $"{b} gave me their word. I released it before I checked the book. Take it. Next time I want to see the paperwork myself.";
            return (j.Series%3) switch { 0 => $"{b}'s note checks out. Look under that brown paper. One parcel, and a thank-you from the family.", 1 => $"{b} took my Sunday. I've written the change in my own hand. Tell your musician to play us something good.", _ => $"I kept the old fare for the whole building. Here's the strip. {b} said you could be trusted with the change." };
        }
        public string Choice(DistrictJob j,bool bluff=false) => j.Stage==0 ? new[]{"Carry the note", "Ask about a shift swap", "Find the ticket book"}[j.Series%3] : j.Stage==2 ? new[]{"Collect the parcel", "Collect the signed rota", "Collect the ticket strip"}[j.Series%3] : bluff ? new[]{"Say the desk cleared it", "Say the office booked the band", "Say the refund is signed"}[j.Series%3] : new[]{"Ask for a duplicate slip", "Arrange an honest swap", "Ask them to honour the fare"}[j.Series%3];
        public string Lead(DistrictJob j) => j.Stage==0 ? $"Hear {Map.People[j.A].Name} out." : j.Stage==1 ? $"{Map.People[j.A].Name} sent you to their {TieWord(j.FirstTie)}, {Map.People[j.B].Name}." : $"{Map.People[j.B].Name} has spoken to {Map.People[j.C].Name}. Collect their answer.";
        internal void Hash(Hash.H h)
        {
            h.S("harbour").I((int)Handover).S(HandoverNews).I(Completed).S(News);
            foreach(var j in Jobs) h.I(j.Series).I(j.A).I(j.B).I(j.C).I(j.Stage).Z(j.Bluff);
            for(int i=0;i<Trust.Length;i++) if(Trust[i]!=0 || Memory[i]!=null || Meetings[i].HasValue) {h.I(i).I(Trust[i]).S(Memory[i]??"").Z(Meetings[i].HasValue); if(Meetings[i] is Pt p) h.I(p.X).I(p.Y);}
        }
    }
}
