// Why a hostile fires on an operative (engine 0.25.0; Sky playing 0.2.56: "Enforcer was shot by the door guard - I couldn't tell if that's
// because weapon was out, carrying Vale or coz they shot police etc?"). A read-only host read and one log line; no rule changes.
//  · The causes are the ones the hostile's own targeting reads (FoesInReach): before the final fight, and in it with ai.intent.final agent,
//    the operative's own causes by the hostile's intent kind (Provoked: police and robots drawn, fired, vale, violence and the known face from
//    their Alert rung; rivals drawn, vale, and once alerted seen); a final-fight body under final every fires on every operative ("final");
//    the fight range's bodies always do ("range").
//  · Fixed order, the strongest first (FireCauseOrder): range, final (the rule, not the agent), then fired (it has fired: the targeting's
//    class 1 is a shooter), vale (it holds Vale: class 2), violence (security saw it do violence), drawn (a weapon out in sight: class 3),
//    face (security knows its face), seen (the rival crew saw it act). Memory comes after the live act.
//  · ai.why is logged once a pair, on the tick a hostile first takes an operative as its target (its Target becomes the operative), with
//    the causes it had then. The log is hashed by tick and code, so a run's hashes move from the first such tick and nowhere else.
//  · The code is the AI's (ai.), not fire.: fire.* lines are the cell's fire orders, and the mission check counts them as shots before the
//    final fight (tools/missions/check/judge.ts), which a hostile's line must not inflate.
//  · Nothing here is hashed: the pairs already logged are what the log already says.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    /// <summary>One hostile on an operative (Game.FireWhyOf): who, the causes it has for him now (strongest first; empty if none is live any
    /// more, e.g. its last shots after he holstered), the causes it had when it first took him, that tick, and whether it aims at him or
    /// has fired on him within weapons.lineLapse. Read-only.</summary>
    public struct FireWhy
    {
        public string By; public string[] Causes, First; public int Since; public bool Aiming, Firing;
        /// <summary>The strongest cause now, else the strongest it had when it took him, else "".</summary>
        public string Main => Causes.Length > 0 ? Causes[0] : First.Length > 0 ? First[0] : "";
    }

    public sealed partial class Game
    {
        /// <summary>The causes in their fixed order, the strongest first.</summary>
        public static readonly string[] FireCauseOrder = { "range", "final", "fired", "vale", "violence", "drawn", "face", "seen" };

        readonly Dictionary<string, (int tick, string[] causes)> fireWhyFirst = new Dictionary<string, (int, string[])>(StringComparer.Ordinal); // "<hostile>/<agent>": output only, not hashed (the log says it)

        /// <summary>Every armed hostile body by id, in a fixed order: the guard, the patrol, the officers, the rival crew, the final fight's
        /// bodies, the response's units. Reads nothing that changes state.</summary>
        IEnumerable<string> HostileIds()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            IEnumerable<string> All()
            {
                yield return "guard"; yield return "patrol";
                foreach (var g in SecGuns) yield return g.Id;
                yield return "rivals";
                foreach (var h in Hostiles) yield return h.Id;
                foreach (var u in Units) yield return u.Id;
            }
            foreach (var id in All()) if (seen.Add(id)) yield return id;
        }

        /// <summary>The causes hostile `by` has to fire on operative `id` now, strongest first (FireCauseOrder); empty when it has none. Its
        /// sight and reach are not read (they are the targeting's, not a cause). Read-only.</summary>
        public string[] FireCauses(string by, OpId id)
        {
            if (IsFightHostile(by) && (R.Intent.FinalEvery || Range)) return new[] { Range ? "range" : "final" }; // FoesInReach's anyOp
            var op = OpOf(id); var ir = R.Intent; string kind = IntentKind(by);
            string[] foes = kind == "police" ? ir.PoliceFoes : kind == "robots" ? ir.RobotFoes : RivalsAlert ? ir.RivalsAlerted : ir.RivalsCalm;
            var have = new HashSet<string>(StringComparer.Ordinal);
            foreach (var f in foes) if (HasFoe(f, op)) have.Add(f);
            if (Alert >= FaceAt(kind) && faces.Contains(Sim.Op.Name(id))) have.Add("face"); // as Provoked
            var l = new List<string>(); foreach (var c in FireCauseOrder) if (have.Contains(c)) l.Add(c);
            return l.ToArray();
        }

        /// <summary>Every hostile aiming at this operative or that has fired on it within weapons.lineLapse, in id order, with its causes.
        /// Empty when none is. Read-only.</summary>
        public List<FireWhy> FireWhyOf(OpId id)
        {
            var l = new List<FireWhy>(); string me = Sim.Op.Name(id); var d = underFire[(int)id];
            foreach (var by in HostileIds())
            {
                var g = GunOf(by); if (g == null || g.Down) continue;
                bool aiming = g.Target == me, firing = d.TryGetValue(by, out var x) && Tick - x.last <= T(R.LineLapseS);
                if (!aiming && !firing) continue;
                var first = fireWhyFirst.TryGetValue(by + "/" + me, out var fw) ? fw : (-1, Array.Empty<string>());
                l.Add(new FireWhy { By = by, Causes = FireCauses(by, id), First = first.Item2, Since = first.Item1, Aiming = aiming, Firing = firing });
            }
            l.Sort((a, b) => string.CompareOrdinal(a.By, b.By));
            return l;
        }

        /// <summary>The causes hostile `by` had when it first took operative `id` as its target, and that tick; (-1, empty) if it never has.</summary>
        public (int tick, string[] causes) FireWhyFirst(string by, OpId id) => fireWhyFirst.TryGetValue(by + "/" + Sim.Op.Name(id), out var x) ? x : (-1, Array.Empty<string>());

        /// <summary>Once a fight tick, after the bodies have aimed and before the shots land (Combat): a hostile whose target is an operative
        /// it has never had logs ai.why once, with the causes it has for him (the state its choice read).</summary>
        void FireWhyStep()
        {
            foreach (var by in HostileIds())
            {
                var g = GunOf(by); if (g == null || g.Down || !IsOpId(g.Target)) continue;
                string key = by + "/" + g.Target; if (fireWhyFirst.ContainsKey(key)) continue;
                var causes = FireCauses(by, Sim.Op.Parse(g.Target));
                fireWhyFirst[key] = (Tick, causes);
                Note("ai.why", $"{by} takes the {g.Target} as its target: {(causes.Length > 0 ? string.Join(", ", causes) : "no cause")}.");
            }
        }
    }
}
