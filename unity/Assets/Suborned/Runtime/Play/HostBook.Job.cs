using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 096, the Job page as the start briefing too (Glyph's 1-start-job.png): the mission's title over the name, the purse and Heat beside the folded ways
    /// to fail, who each way runs through as faces, the shared end with the conditions every way shares under it, and the cell strip under the clock. All of it is
    /// the mission's own data (brief.json ways and contacts, book.job.needs, the run's purse and Heat) and book.start's words; the same page in play and at the start.</summary>
    public sealed partial class HandoverHost
    {
        static readonly string[] CellRoles = { "scout", "fixer", "tech", "enforcer" };

        void BookJobStart(BookPage p, BookWordsFile bw)
        {
            var g = Game;
            p.Kicker = (FrontEnd.LoadBrief().title ?? "").ToUpperInvariant();
            p.Purse = $"${g.Cash:N0}"; p.HeatWord = bw.HeatWord;
            string band = ""; Guard("heat", () => band = g.HeatBand); p.Heat = Text != null && Text.Heat.TryGetValue(band, out var hw) ? hw : band.Length > 0 ? char.ToUpperInvariant(band[0]) + band[1..] : "";
            // who each way runs through: its contact, then anyone its conditions name (the guard off the door, a turned role), each once
            foreach (var w in p.Ways)
            {
                var roles = new List<string>(); void Add(string r) { if (r is "waitress" or "driver" or "guard" && !roles.Contains(r)) roles.Add(r); }
                Add(w.Target?.Key ?? "");
                foreach (var n in p.Needs) if (n.Group == w.Verb && bw.Needs.TryGetValue(w.Verb, out var list) && list.Find(x => x.Id == n.Id) is { } nd)
                        foreach (var part in nd.Check.Split('|')) { var t = part.Trim(); if (t == "guard-away") Add("guard"); else if (t.StartsWith("turned:")) Add(t[7..]); }
                foreach (var r in roles) w.Faces.Add(BookFaceOf(r));
            }
            // the shared end: every condition all ways share after their first shared one sits once under the line (Group Tail), not under each way
            if (bw.Shared.Length > 0 && p.Ways.Count > 1)
            {
                p.Shared = bw.Shared; string first = null; var shared = new HashSet<string>();
                foreach (var n in p.Needs) if (n.Group == p.Ways[0].Verb && p.Ways.TrueForAll(w => p.Needs.Exists(x => x.Group == w.Verb && x.Id == n.Id))) { if (first == null) first = n.Id; else shared.Add(n.Id); }
                var kept = new HashSet<string>(); var drop = new List<BookNeed>();
                foreach (var n in p.Needs) if (shared.Contains(n.Id)) { if (kept.Add(n.Id)) n.Group = BookLayout.Tail; else drop.Add(n); }
                foreach (var n in drop) p.Needs.Remove(n);
            }
            // the cell strip: face, name, job word and one line each; a click opens their Cell page
            for (int k = 0; k < 4; k++)
            {
                string r = CellRoles[k], line = bw.StartCell.TryGetValue(r, out var l) ? l : Text != null && Text.PageCell.TryGetValue(r, out var pl) ? pl : "";
                p.Crew.Add(new BookCrew { Agent = k, Face = $"portraits/cameo_{r}_{(g.Ops[k].Down ? "hurt" : "normal")}", Name = AgentName(k), Job = AgentJob(k), Line = line });
            }
        }

        /// <summary>A person's face for the page: their cameo where the kit has one, else their badge; their name; the view a hover turns to.</summary>
        BookFace BookFaceOf(string role)
        {
            bool cameo = role is "waitress" or "driver" or "councillor"; // the kit's cameos (portraits/cameo_*_normal); the guard has a badge only
            return new BookFace { Face = cameo ? $"portraits/cameo_{role}_normal" : HudModel.PersonBadge(role), Badge = !cameo, Word = Cap(Name(role)), View = role };
        }
    }
}
