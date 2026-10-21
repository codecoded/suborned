// A public fight (Sky 2026-10-16, via Aster: "it should be per fight, and depends who you're fighting - a rival gang that has menace
// level authority is a +1"; Motive, briefs/014/motive-standing.md, a public fight; rulebook fight.public). It replaces engine 0.6.1's
// per-target gunfire step (gunfire.heard, retired with the cause seen-gunfire).
//  · One fight: the final fight is one fight, from its first shot by or at the cell to its end (FightOverTick) or the mission's. Outside
//    it, a fight starts at the first shot by or at the cell and ends crowd.quiet after the last such shot. Gunfire still open when the
//    final fight starts is the final fight. A fight still open when the mission ends ends with it.
//  · Public: a person of the city who is not security stands on the street within crowd.scatter of the shooter of any of its shots,
//    line or no line. Outside the final fight its shots are those by or at the cell; in it, every shot.
//  · When it ends, a public fight moves Standing once, by fight.public's first outcome that holds (the rulebook's order, read at load):
//    the cell fired on security −1; fired first on rivals the public doesn't fear −1; drove off rivals the district fears +1; else 0.
//    Untraced shots count (Standing is the public's judgement, not evidence). A seen act: outside the word's cap, a move for the drift.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Suborned.Sim
{
    /// <summary>One fight as fight.public judges it (Sky 2026-10-16). Output and hashed state, never read by a host to decide anything.</summary>
    public sealed class PublicFight
    {
        public int Start, Last, End = -1;            // ticks: its first shot, its last shot by or at the cell, when it ended
        public bool Final, Public;                   // the final fight; a person of the city heard it
        public bool OnSecurity, OnRivals;            // the cell fired on security, on rivals
        public string FirstSecurity = "";            // the first security body the cell fired on (the move's who)
        public string RivalsFirst = "";              // who fired the first shot between the cell and the rivals: "cell", "rivals" or ""
        public int CrewBroken = -1;                  // the rival crew was driven back in this fight and has not fired on the cell since
        public readonly List<string> Rivals = new List<string>(); // the rival bodies the cell fought, in the order met
        public string Outcome = "", Cause = "";      // fight.public's outcome ("unheard" when nobody heard it) and its Standing cause
        public int Delta;
    }

    public sealed partial class Game
    {
        /// <summary>The fights of this mission that have ended, in order (fight.public).</summary>
        public readonly List<PublicFight> Fights = new List<PublicFight>();
        /// <summary>The fight on now, if any.</summary>
        public PublicFight? OpenFight { get; private set; }
        bool finalFightJudged, fightClosedLate;

        /// <summary>How the public sees this mission's rivals: a start that fixes it, else the mission's word (site rivals.public), else the rulebook's default.</summary>
        public string RivalsPublic => Start.RivalsPublic ?? (S.RivalsPublic.Length > 0 ? S.RivalsPublic : R.RivalsPublicDefault);

        static bool IsRivalId(string id) => id == "rivals" || id.StartsWith("rival-", StringComparison.Ordinal);
        /// <summary>Authority: the guard, the patrol, the response unit's officers, the response's robots and police, the final fight's officers.</summary>
        bool IsSecurityId(string id) => id == "guard" || id == "patrol" || SecIndex(id) >= 0 || UnitOf(id) != null || (IsFightHostile(id) && !IsRivalId(id));
        bool FinalFightOn => FightBranch.Length > 0 && FightOverTick == INF && !finalFightJudged;

        /// <summary>A shot lands (Resolve): it opens or extends the fight, and the people within reach of its shooter hear it.</summary>
        void FightShot(string by, string t, Pt from)
        {
            if (Range) return; // brief 029: the range has no public, no Standing
            bool cellBy = IsOpId(by), cellAt = IsOpId(t);
            var f = OpenFight;
            if (!cellBy && !cellAt) { if (f != null && f.Final) Hear(f, from); return; } // in the final fight every shot is the fight's
            if (f != null && !f.Final && Tick - f.Last >= R.FightQuietTicks) { EndFight(); f = null; }
            if (f == null) { f = new PublicFight { Start = Tick, Final = FinalFightOn }; OpenFight = f; }
            else if (!f.Final && FinalFightOn) f.Final = true;
            f.Last = Tick;
            Hear(f, from);
            if (cellBy)
            {
                if (IsSecurityId(t)) { f.OnSecurity = true; if (f.FirstSecurity.Length == 0) f.FirstSecurity = t; }
                else if (IsRivalId(t)) { f.OnRivals = true; if (!f.Rivals.Contains(t)) f.Rivals.Add(t); if (f.RivalsFirst.Length == 0) f.RivalsFirst = "cell"; }
            }
            else if (IsRivalId(by))
            {
                if (!f.Rivals.Contains(by)) f.Rivals.Add(by);
                if (f.RivalsFirst.Length == 0) f.RivalsFirst = "rivals";
                if (by == "rivals") f.CrewBroken = -1; // back and firing: not broken
            }
        }

        /// <summary>Public: a person of the city who is not security, on the street within the reach of the shooter, line or no line.</summary>
        void Hear(PublicFight f, Pt from)
        {
            if (f.Public) return;
            foreach (var (p, wp) in OnTheStreet()) if (p.Civilian && G.Within(wp, from, R.FightPublicCm)) { f.Public = true; return; }
        }

        /// <summary>The rival crew is driven back (Damage): it broke in this fight.</summary>
        void CrewBroken() { if (OpenFight != null) OpenFight.CrewBroken = Tick; }

        /// <summary>The final fight starts (StartFight): gunfire still open is the final fight, else it closes first.</summary>
        void FightJoinsFinal()
        {
            var f = OpenFight; if (f == null || f.Final) return;
            if (Tick - f.Last >= R.FightQuietTicks) EndFight(); else f.Final = true;
        }

        /// <summary>End of a tick: a fight outside the final fight ends quiet after its last shot; the final fight at its end; any fight with the mission.</summary>
        void FightEnds()
        {
            var f = OpenFight; if (f == null) return;
            if (Over) EndFight();
            else if (f.Final) { if (FightOverTick != INF) EndFight(); }
            else if (Tick - f.Last >= R.FightQuietTicks) EndFight();
        }

        /// <summary>The run ends with a fight open (Finish, Investigate): it ends now. True when one did.</summary>
        bool EndOpenFight() { if (OpenFight == null) return false; EndFight(); return true; }

        void EndFight()
        {
            var f = OpenFight!; OpenFight = null; f.End = Tick;
            autoHolsterAt = Tick + T(R.AutoHolsterS); // brief 030 (weapons.autoHolster)
            if (f.Final) finalFightJudged = true;
            f.Outcome = "unheard";
            if (f.Public)
            {
                f.Outcome = "none";
                foreach (var o in R.FightOutcomes)
                {
                    bool all = true; foreach (var c in o.When) if (!FightHolds(c, f)) { all = false; break; }
                    if (!all) continue;
                    f.Outcome = o.Id; f.Delta = o.Delta; f.Cause = o.Cause; break;
                }
            }
            Fights.Add(f);
            string span = $"{Sim.Report.Clock(f.Start)}-{Sim.Report.Clock(f.Last)}";
            if (f.Delta != 0)
            {
                string who = f.OnSecurity ? f.FirstSecurity : "rivals"; // authority outranks (the rulebook's first outcome)
                MoveStanding(f.Delta, $"fight:{span}:{f.Outcome}", f.Cause, who);
            }
            Note("fight.judged", f.Public ? $"The fight ({span}{(f.Final ? ", the final fight" : "")}) is over: the street judges it {f.Outcome} ({(f.Delta > 0 ? "+" : "")}{f.Delta.ToString(CultureInfo.InvariantCulture)} Standing)." : $"The fight ({span}) is over. Nobody heard it.");
        }

        bool FightHolds(Cond c, PublicFight f)
        {
            switch (c.K)
            {
                case "fired-on": foreach (var x in c.L("in")) if ((x == "security" && f.OnSecurity) || (x == "rivals" && f.OnRivals)) return true; return false;
                case "rivals-public": return c.L("in").Contains(RivalsPublic);
                case "fired-first": return (f.RivalsFirst == "cell") == c.B("is");
                case "rivals-broken": return RivalsBroken(f) == c.B("is");
            }
            throw new InvalidOperationException($"fight.public: '{c.K}' is not a fight's condition"); // Rules rejects it at load
        }

        /// <summary>Every rival the cell fought in this fight (in the final fight, every rival of the ambush too) broke, was driven back
        /// or is down when it ends. The crew absorbed into the ambush is judged by the ambush.</summary>
        bool RivalsBroken(PublicFight f)
        {
            var set = new List<string>(f.Rivals);
            if (f.Final) foreach (var h in Hostiles) if (IsRivalId(h.Id) && h.Present && !set.Contains(h.Id)) set.Add(h.Id);
            int judged = 0;
            foreach (var id in set)
            {
                if (id == "rivals")
                {
                    if (RivalsAbsorbed) continue;
                    if (f.CrewBroken < 0) return false;
                    judged++; continue;
                }
                var h = HostileOf(id);
                if (h != null && !h.Down && h.Gun.St != "break" && h.Gun.St != "gone") return false;
                judged++;
            }
            return judged > 0;
        }
    }
}
