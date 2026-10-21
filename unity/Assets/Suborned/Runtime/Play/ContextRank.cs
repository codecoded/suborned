using System.Collections.Generic;

namespace Suborned.Play
{
    /// <summary>Brief 030 (Glyph, final 2026-10-20; Sky: "right click should be kinda contextual"): which of the ways open on a target a quick
    /// right click takes, and the order the ring lists them in. Pure: it reads the rows the action panel already built (price, lock, outcome),
    /// so the hover chip and the click can never disagree, and it is unit-tested with no engine step (ContextRankTests).
    /// Rank: 1 the finishing acts (Take, Enter, Board, Go, and a turned contact's Call), 2 a free learn (Watch, then Listen), 3 a turn (the
    /// approach that fits the known temperament, then cheapest, then fewest Trail; Pressure last), 4 a machine act (Loop, Reroute, False tip),
    /// 5 body work (Carry, Hide, Tie, Put down), 6 a takedown, 7 Fire. A quick click takes the top way only when there is one clearly best way,
    /// it is no gamble (no "?" outcome) and it is not violent; otherwise the ring opens and nothing is taken.</summary>
    public static class ContextRank
    {
        /// <summary>One way on the target, for one or more selected agents.</summary>
        public sealed class Way
        {
            public string Do = "", Approach = "", How = "", Outcome = "";
            public bool Available, Mine;        // Mine: the primary selected agent can give it
            public long Usd, Trail, Orders = 1;
            public int Index;                   // the caller's row index
        }

        public enum Act { Default, Ring, Silent, Lock, Hint }

        public sealed class Choice
        {
            public Act Act; public int Pick = -1; public string Why = "";
            /// <summary>The available ways, best first (row indices): the ring's segments in clockwise order.</summary>
            public readonly List<int> Order = new();
        }

        public static int Tier(Way w) => w.Do switch
        {
            "take" or "enter" or "board" or "go" or "call" => 1,
            "watch" or "listen" => 2,
            "turn" => 3,
            "loop" or "reroute" or "mislead" => 4,
            "carry" or "stash" or "restrain" or "drop" => 5,
            "takedown" => 6,
            "fire" => 7,
            _ => 8,
        };

        static int Sub(Way w) => w.Do switch
        {
            "watch" => 0, "listen" => 1,
            "carry" => 0, "stash" => 1, "restrain" => 2, "drop" => 3,
            "takedown" => w.How == "kill" ? 1 : 0,
            "turn" => w.Approach == "pressure" ? 1 : 0, // Pressure is never a default: last of the turns
            _ => 0,
        };

        /// <summary>A turn's fit: turned in one order best, then the fewest orders (one step worse), then anything else, refused last.</summary>
        static long Fit(Way w) => w.Do != "turn" ? 0 : w.Outcome == "turned" ? w.Orders : w.Outcome == "refused" ? 1_000_000 : w.Outcome == "?" ? 500_000 : 100_000;

        public static bool Violent(Way w) => w.Do is "takedown" or "fire";
        public static bool Gamble(Way w) => w.Do == "turn" && w.Outcome == "?";
        static bool Refused(Way w) => w.Do == "turn" && w.Outcome == "refused";

        /// <summary>Best first. The turn's own keys (fit, $, Trail) order only turns; machine acts at one target tie on purpose (both open: the ring).</summary>
        public static int Compare(Way a, Way b)
        {
            int c = Tier(a).CompareTo(Tier(b)); if (c != 0) return c;
            c = Sub(a).CompareTo(Sub(b)); if (c != 0) return c;
            if (a.Do == "turn" && b.Do == "turn")
            {
                c = Fit(a).CompareTo(Fit(b)); if (c != 0) return c;
                c = a.Usd.CompareTo(b.Usd); if (c != 0) return c;
                c = a.Trail.CompareTo(b.Trail); if (c != 0) return c;
            }
            c = b.Mine.CompareTo(a.Mine); if (c != 0) return c;
            return a.Index.CompareTo(b.Index);
        }

        /// <summary>Equal rank: neither is clearly best.</summary>
        static bool Tie(Way a, Way b)
        {
            if (Tier(a) != Tier(b) || Sub(a) != Sub(b)) return false;
            if (a.Do == "turn" && b.Do == "turn") return Fit(a) == Fit(b) && a.Usd == b.Usd && a.Trail == b.Trail;
            return true; // two machine acts, two loops, two body acts of one kind: no order between them
        }

        /// <summary>What a quick click does on a target of `kind` with these ways. `silentKind`: a passer-by, a unit, an empty stash (the click
        /// does nothing; the hold still opens the ring when there are ways).</summary>
        public static Choice Choose(IList<Way> ways, string kind)
        {
            var ch = new Choice(); var avail = new List<Way>();
            foreach (var w in ways) if (w.Available) avail.Add(w);
            avail.Sort(Compare); foreach (var w in avail) ch.Order.Add(w.Index);
            if (kind is "crowd" or "unit") { ch.Act = Act.Silent; ch.Why = "passer-by"; return ch; } // never a plain click on a passer-by or a unit
            if (avail.Count == 0)
            {
                if (kind == "stash") { ch.Act = Act.Silent; ch.Why = "nothing carried"; return ch; }
                if (ways.Count > 0) { ch.Act = Act.Lock; ch.Pick = ways[0].Index; ch.Why = "locked"; return ch; }
                ch.Act = Act.Hint; ch.Why = "nothing here"; return ch;
            }
            // the candidates for a quick click: no violence, no Pressure, no refused approach; the primary agent's ways first
            var cands = avail.FindAll(w => !Violent(w) && Tier(w) <= 5 && !(w.Do == "turn" && w.Approach == "pressure") && !Refused(w));
            if (cands.Exists(w => w.Mine)) cands = cands.FindAll(w => w.Mine);
            if (cands.Count == 0) { ch.Act = Act.Ring; ch.Pick = avail[0].Index; ch.Why = avail.TrueForAll(Violent) ? "violent" : "no safe way"; return ch; } // only violence, Pressure or a refusal: the player chooses
            var top = cands[0];
            if (Gamble(top)) { ch.Act = Act.Ring; ch.Pick = top.Index; ch.Why = "gamble"; return ch; }
            if (cands.Count > 1 && Tie(top, cands[1])) { ch.Act = Act.Ring; ch.Pick = top.Index; ch.Why = "tie"; return ch; }
            ch.Act = Act.Default; ch.Pick = top.Index; ch.Why = "best"; return ch;
        }

        /// <summary>The first number in a price string ("$1,000–2,000" → 1000, "T +2" → 2, "A ?" → 0).</summary>
        public static long Num(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0; long n = 0; bool any = false;
            foreach (var c in s) { if (c >= '0' && c <= '9') { n = n * 10 + (c - '0'); any = true; } else if (c == ',' && any) continue; else if (any) break; }
            return n;
        }
    }
}
