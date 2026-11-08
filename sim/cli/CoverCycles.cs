// Brief 098 (engine 0.50.0): the cover cycles a run shows, read from its log, for the dial sweep and the law that fighters hold cover
// (rulebook fight.cover). The log's ai.cover lines ("<id> takes cover at <cv> (...)") give each fighter's cover points in order. A return is
// a take of a point he took before with another point between (A, B, A: he went back to cover he had left), a run's cycles its returns.
// Also counted: a fighter who takes cover 6 times or more in a run (the 0.49.0 report's measure) and the advances (ai.advance). Read-only:
// nothing here moves a run, and it reads only the log, so the same file counts any engine's runs.
using System;
using System.Collections.Generic;
using System.Globalization;
using Suborned.Sim;

static class CoverCycles
{
    /// <summary>One run: cover lines, returns, the most returns and the most takes by one fighter (and who), advances.</summary>
    public readonly struct Run
    {
        public readonly int Takes, Returns, MaxReturns, MaxTakes, Advances; public readonly string Who;
        public Run(int takes, int returns, int maxReturns, int maxTakes, int advances, string who) { Takes = takes; Returns = returns; MaxReturns = maxReturns; MaxTakes = maxTakes; Advances = advances; Who = who; }
    }

    public static Run Of(Game g)
    {
        var seq = new Dictionary<string, List<string>>(StringComparer.Ordinal); int takes = 0, adv = 0;
        foreach (var l in g.Log)
        {
            if (l.Code == "ai.advance") { adv++; continue; }
            if (l.Code != "ai.cover") continue;
            int k = l.Text.IndexOf(" takes cover at ", StringComparison.Ordinal); if (k <= 0) continue;
            string who = l.Text.Substring(0, k), rest = l.Text.Substring(k + 16); int e = rest.IndexOf(' '); string cv = e < 0 ? rest.TrimEnd('.') : rest.Substring(0, e);
            if (!seq.TryGetValue(who, out var s)) seq[who] = s = new List<string>();
            s.Add(cv); takes++;
        }
        int ret = 0, maxRet = 0, maxTakes = 0; string whoMax = "";
        foreach (var kv in seq)
        {
            int r = 0; var seen = new HashSet<string>(StringComparer.Ordinal); string last = "";
            foreach (var cv in kv.Value) { if (cv != last && seen.Contains(cv)) r++; seen.Add(cv); last = cv; }
            ret += r;
            if (r > maxRet || (r == maxRet && kv.Value.Count > maxTakes)) { maxRet = r; whoMax = kv.Key; }
            if (kv.Value.Count > maxTakes) maxTakes = kv.Value.Count;
        }
        return new Run(takes, ret, maxRet, maxTakes, adv, whoMax);
    }

    /// <summary>A setting's runs together.</summary>
    public sealed class Tally
    {
        public int Runs, Takes, Returns, RunsWithReturn, RunsWith3, Runs6Takes, Advances, MaxReturns; public string MaxWhat = "";
        public void Add(Run r, string what)
        {
            Runs++; Takes += r.Takes; Returns += r.Returns; Advances += r.Advances;
            if (r.Returns > 0) RunsWithReturn++; if (r.MaxReturns >= 3) RunsWith3++; if (r.MaxTakes >= 6) Runs6Takes++;
            if (r.MaxReturns > MaxReturns) { MaxReturns = r.MaxReturns; MaxWhat = $"{r.Who} ({what})"; }
        }
        public void Write(JWriter w) => w.Open("cover").Prop("takes", Takes).Prop("returns", Returns).Prop("runsWithReturn", RunsWithReturn).Prop("runsWithAFighter3Returns", RunsWith3)
            .Prop("runsWithAFighter6Takes", Runs6Takes).Prop("advances", Advances).Prop("mostReturnsByOne", MaxReturns).Prop("mostReturnsBy", MaxWhat).EndObj();
        public string Line() => string.Create(CultureInfo.InvariantCulture,
            $"    cover: {Takes} takes, {Returns} returns to cover left before ({RunsWithReturn}/{Runs} runs; a fighter 3+ returns in {RunsWith3}, 6+ takes in {Runs6Takes}; most {MaxReturns}{(MaxWhat.Length > 0 ? ", " + MaxWhat : "")}), {Advances} advances");
    }
}
