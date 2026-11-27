// A bag or paper belongs to the person who stopped for it, including while the camera is elsewhere.
using System.Collections.Generic;

namespace Suborned.Sim
{
    public enum StreetPurchase : byte { None, Groceries, Newspaper }

    public sealed class DistrictShopping
    {
        const int ReachCm = 315;
        readonly List<(Pt at, StreetPurchase goods)> counters = new();
        readonly Dictionary<(int,int),List<int>> blocks = new();
        readonly int[] waiting, collected;
        readonly bool[] leftCounter;
        readonly StreetPurchase[] carried;

        internal DistrictShopping(DistrictMap map)
        {
            int n = map.People.Length;
            waiting = new int[n]; collected = new int[n]; leftCounter = new bool[n]; carried = new StreetPurchase[n];
            foreach (var venue in map.Ground.Venues)
            foreach (var f in venue.Fixtures)
            {
                var item = f.Kit == "ProduceStall" ? StreetPurchase.Groceries :
                    f.Kit == "Newsstand" ? StreetPurchase.Newspaper : StreetPurchase.None;
                if (item == StreetPurchase.None) continue;
                int index = counters.Count; counters.Add((f.At,item));
                for (int x = Block(f.At.X-ReachCm); x <= Block(f.At.X+ReachCm); x++)
                for (int y = Block(f.At.Y-ReachCm); y <= Block(f.At.Y+ReachCm); y++)
                {
                    if (!blocks.TryGetValue((x,y),out var list)) blocks[(x,y)] = list = new List<int>();
                    list.Add(index);
                }
            }
        }
        static int Block(int cm) => Grid.FloorDiv(cm,600);
        public StreetPurchase Carried(int person) => carried[person];

        internal void Step(DistrictRun district, int tick)
        {
            if (tick % 5 != 0) return;
            for (int i = 0; i < carried.Length; i++)
            {
                if (!district.Roams(i) || district.Network.Fallen[i] || district.Network.Away(i) || district.Meetings[i].HasValue)
                { waiting[i] = 0; continue; }
                var person = district.Map.People[i];
                var p = district.Pose(i,tick);
                if (carried[i] != StreetPurchase.None)
                {
                    if (!G.Within(p.at,counters[collected[i]].at,ReachCm+50)) leftCounter[i] = true;
                    if (leftCounter[i] && !p.moving && G.Within(p.at,person.Walk.Points[0],100))
                    { carried[i] = StreetPurchase.None; leftCounter[i] = false; }
                    continue;
                }
                int counter = -1;
                if (!p.moving && blocks.TryGetValue((Block(p.at.X),Block(p.at.Y)),out var nearby))
                    foreach (int k in nearby)
                    {
                        var c = counters[k];
                        if ((c.goods == StreetPurchase.Groceries && person.Job == "fruiterer") || !G.Within(p.at,c.at,ReachCm)) continue;
                        counter = k; break;
                    }
                // Two consecutive stops at one counter. Passing somebody else's stall is not a purchase.
                if (counter >= 0 && waiting[i] == counter+1)
                {
                    carried[i] = counters[counter].goods;
                    collected[i] = counter; leftCounter[i] = false; waiting[i] = 0;
                }
                else waiting[i] = counter+1;
            }
        }

        internal void Hash(Hash.H h)
        {
            for (int i=0; i<carried.Length; i++)
                if (waiting[i]!=0 || carried[i]!=StreetPurchase.None)
                    h.S("purchase").I(i).I(waiting[i]).I((int)carried[i]).I(carried[i]==StreetPurchase.None ? 0 : collected[i]).I(leftCounter[i] ? 1 : 0);
        }
    }
}
