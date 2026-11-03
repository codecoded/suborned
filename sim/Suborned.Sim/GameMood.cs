// What a Watch shows you (brief 054 §2, §4, Motive rev 4; Sky 2026-10-23/24; engine 0.28.0; rulebook mood).
//  · MoodNow(p): a person's mood toward the cell now, the first row that holds (scared, uneasy, friendly, neutral), from any cause, their own
//    life included (Sky's (B): the Fearful temperament's fear 1 reads uneasy). The fit reads it (Rules.FitWithMood): one step at most.
//  · WatchTeaches(p): a Watch on this person would show something the cell can act on: their mood (not yet known, not neutral, not the one
//    last seen, the temperament not known), or for security their sight and marks until watched. Off (rulebook without mood): as 0.27.0.
//  · KnowsGate(p, what): "routine known" for Listen and the approaches that require it: watched, or no Watch to give.
//  · Host reads: MoodOf(role) (the mood the cell knows), WatchOffered(role), the card's Quote.Mood / Likely / Hurt. Read-only.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>A person's mood now (rulebook mood; "neutral" with the mood off).</summary>
        string MoodNow(Person p)
        {
            if (!R.MoodOn) return "neutral";
            bool Any(HashSet<string> acts) { foreach (var m in p.Memory) if (acts.Contains(m.Act)) return true; return false; }
            if (p.Sc[Rules.Fear] >= R.MoodScaredFear || Any(R.MoodScaredActs)) return "scared";
            if (p.Sc[Rules.Fear] >= R.MoodUneasyFear || p.Sc[Rules.Resentment] >= R.MoodUneasyResentment || Any(R.MoodUneasyActs)) return "uneasy";
            if (R.MoodWary && p == GuardP) foreach (var op in Ops) if (AttentionWary("guard", Sim.Op.Name(op.Id))) return "uneasy"; // a security watcher's wary stage
            if (p.Sc[Rules.Loyalty] >= R.MoodFriendlyLoyalty || Any(R.MoodFriendlyActs)) return "friendly";
            return "neutral";
        }

        /// <summary>Brief 054 §2.1: would a Watch on p teach something the cell reads (presence aside)?</summary>
        bool WatchTeaches(Person p)
        {
            if (p.Down) return false;
            // brief 054 §9.2 (engine 0.29.0): a Watch never teaches a secret (only Listen does), so the affair offers no Watch
            if (p == GuardP && !(Known("guard")?.Sight ?? true)) return true; // the door guard's sight, until known
            if (p.Turned) return false;
            if (IsStaff(p)) return !p.KRoutine; // security: their sight and check-in marks, until watched
            if (p.KTemperament) return false;    // after Listen the card is exact: a mood adds nothing
            var m = MoodNow(p); return m != "neutral" && m != p.KMood;
        }
        /// <summary>The refusal a Watch that teaches nothing gets: already-watched once the cell has watched them (the row greyed, ✓), else nothing-to-see (no row).</summary>
        string WatchRefusal(Person p) => !R.WatchOffers || p.KRoutine ? "already-watched" : "nothing-to-see";

        /// <summary>Brief 054 §2.2: a fact the cell knows, with "routine" read as the Watch gate: watched, or no Watch offered for this person.</summary>
        bool KnowsGate(Person p, string what)
        {
            if (what == "routine" && R.WatchOffers) return p.KRoutine || !WatchTeaches(p);
            return p.Knows(what);
        }

        /// <summary>Brief 054 §4.3: the mood the card reads and the approach it favours or hurts.</summary>
        void QuoteMood(Quote q, Event ev, Person p)
        {
            if (!R.MoodOn) return;
            q.Mood = p.KTemperament ? MoodNow(p) : p.KMood;
            if (q.Mood.Length == 0 || q.Mood == "neutral" || !ev.Id.StartsWith("turn.", StringComparison.Ordinal)) return;
            int step = R.MoodStep(q.Mood, ev.Id.Substring(5));
            q.Likely = step > 0; q.Hurt = step < 0;
        }

        // ---------------------------------------------------------------- host reads (brief 054), nothing hashed

        /// <summary>The mood the cell knows for a cast role: what its last Watch saw, the true mood once the temperament is known (Listen), else "".</summary>
        public string MoodOf(string role)
        {
            var p = PersonByRole(role);
            return !R.MoodOn ? "" : p.KTemperament ? MoodNow(p) : p.KMood;
        }
        /// <summary>Brief 054 §2.1: is a Watch on this role on the order list now (it would teach something, and they are on the street, not down)?</summary>
        public bool WatchOffered(string role)
        {
            var p = PersonByRole(role);
            if (!R.WatchOffers) return true;
            return PersonPos(p).HasValue && WatchTeaches(p);
        }
    }
}
