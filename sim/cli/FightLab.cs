// suborned-sim fight-lab (brief 027 §6.5): the balance matrix. Each run of §6.1 (loud L1–L4 from 0:01, prepared P-A, P-G, P-B), scripted as the
// perfect player, under each preset (briefs/027/presets: today, lab; the lab also without cover snap, with a player that seeks the cover marks,
// with charge off and with the field on), one row per run ×
// preset with §6.2/§6.3's verdict and §6.4's triggers, plus §6.4.7's lethality ceiling. Writes briefs/027/keel/fight-lab.json and the two
// checkpoints (briefs/027/checkpoints/cp-loud.script.json, cp-safe.script.json). Every run is replayed from its order log and must match.
//   fight-lab [--runs L1,P-A] [--presets today,lab] [--scripts <dir>] [--rulebook <file>]   (--scripts: write each run as a replayable script; --rulebook: probe another rulebook, writes nothing)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;
using Path = System.IO.Path;

static class FightLabCli
{
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        string? Arg(string k) { int i = Array.IndexOf(args, k); return i > 0 && i + 1 < args.Length ? args[i + 1] : null; }
        var runs = (Arg("--runs") ?? string.Join(",", Scenarios.LabRuns.Concat(Scenarios.LabAdversarial))).Split(','); // the matrix, then the adversarial rows (tuning.md §6.8)
        if (Arg("--rulebook") is string rbPath) // a probe of proposed numbers: another rulebook text, the rest of the inputs as they are (nothing is written)
        {
            string rd(string f) => File.ReadAllText(Path.Combine(root, f)); string? opt(string f) => File.Exists(Path.Combine(root, f)) ? rd(f) : null;
            inputs = Engine.FromTexts(File.ReadAllText(rbPath), rd(Engine.PeopleRulesFile), rd(Engine.SiteFile), rd(Engine.PeopleFile), opt(Engine.ProvisionalFile), opt(Engine.TakedownFile), opt(Engine.RoundsFile));
        }
        bool probe = Arg("--rulebook") != null;
        var routes = new Dictionary<string, string>();
        foreach (var n in new[] { "route-a-board", "route-g-fast-board", "route-b-board" }) routes[n] = File.ReadAllText(Path.Combine(root, "contracts/missions/handover/routes", n + ".json"));
        Start Preset(string file, Action<Start>? f = null)
        {
            var n = JNode.Parse(File.ReadAllText(Path.Combine(root, "briefs/027/presets", file + ".json")), file);
            var s = Start.Parse(n["start"]); f?.Invoke(s); return s;
        }
        var coverPlayer = new HashSet<string> { "lab-cover" }; // B5 probe: the lab (snap on) with a player that walks to the marks under fire (Run.CoverPlayer)
        var presets = new List<(string name, Func<Start> start)>
        {
            ("today", () => Preset("today")),
            ("lab", () => Preset("lab")),
            ("lab-snap-off", () => Preset("lab", s => s.LabCoverSnap = false)), // B5: the lab without cover snap (and its player without the marks): the rows before B5
            ("lab-cover", () => Preset("lab")),
            ("lab-charge-off", () => Preset("lab", s => s.LabCharge = false)),
            ("lab-field-on", () => Preset("lab", s => s.LabField = true)),
            ("lab-kite-off", () => Preset("lab", s => s.LabKite = false)), // brief 027 F2: the lab without Pulse's answer to fire from beyond reach
        };
        var want = Arg("--presets")?.Split(',');
        if (want != null) presets = presets.Where(p => want.Contains(p.name)).ToList();

        var all = new Dictionary<string, List<LabRow>>(); bool replayOk = true;
        var w = new JWriter().Obj().Prop("schema", "suborned.fightlab.matrix/1").Prop("engine", Engine.Version).Prop("inputs", Hash.Hex(inputs.Hash)).OpenArr("presets");
        Console.WriteLine($"brief 027 fight-lab matrix · {Engine.Version} · inputs {Hash.Hex(inputs.Hash)}");
        Console.WriteLine("preset          run  result            end      T(m/tot) A  St(s→e) pub     inv     cases down capt hp-lost hits foes shots free/side  contact verdict");
        foreach (var (name, mk) in presets)
        {
            var rows = new List<LabRow>();
            w.Obj().Prop("name", name).OpenArr("rows");
            foreach (var id in runs)
            {
                var r = Scenarios.PlayLabRun(inputs, id, mk(), routes, coverPlayer.Contains(name));
                var replay = Engine.Replay(inputs, r.G);
                bool same = replay.Checkpoints.Count == r.G.Checkpoints.Count && replay.Checkpoints.SequenceEqual(r.G.Checkpoints) && replay.Result == r.G.Result;
                replayOk &= same;
                var row = Scenarios.Row(id, name, r.G); rows.Add(row);
                if (Arg("--scripts") is string dir) { Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, $"{name}.{id}.script.json"), Script.Write(r.G, inputs.Hash) + "\n"); } // each run as a replayable script (bun run sim play <file>)
                string res = row.Result + (row.Fail.Length > 0 ? " " + row.Fail : "");
                Console.WriteLine(FormattableString.Invariant($"{name,-15} {id,-4} {res,-17} {Report.Clock(row.EndTick),-8} {row.TrailMission,2}/{row.TrailTotal,-3}   {row.Alert} {row.StandingStart,2}→{row.StandingEnd,-3} {row.PublicSecurity,-7} {row.Investigation,-7} {row.OpenCases,5} {row.OpsDown,4} {row.OpsCaptured,4} {row.HealthLostPct,5} % {row.CellHits,4} {row.FoesDown,4} {row.CellShots,5} {row.FreeDowns,4}/{row.FreeSide,-4} c{row.ContactHad}/{row.ContactSeen} s{row.ShotsAtCellAfterVale,-3} {row.Verdict}{(row.Why.Count > 0 ? " — " + string.Join("; ", row.Why) : "")}{(same ? "" : "  REPLAY DIFFERS")}"));
                w.Obj().Prop("run", id).Prop("result", row.Result).Prop("fail", row.Fail).Prop("end", Report.Clock(row.EndTick)).Prop("endTick", row.EndTick)
                    .Prop("trailMission", row.TrailMission).Prop("trailTotal", row.TrailTotal).Prop("alert", row.Alert).Prop("standingStart", row.StandingStart).Prop("standingEnd", row.StandingEnd)
                    .Prop("publicSecurity", row.PublicSecurity).Prop("investigation", row.Investigation).Prop("openCases", row.OpenCases).Prop("opsDown", row.OpsDown).Prop("opsCaptured", row.OpsCaptured)
                    .Prop("cellHealth", row.CellHealth).Prop("cellMax", row.CellMax).Prop("healthLostPct", row.HealthLostPct).Prop("cellHitsTaken", row.CellHits).Prop("foesDown", row.FoesDown).Prop("cellShots", row.CellShots).Prop("freeDowns", row.FreeDowns).Prop("freeDownsBy", row.FreeDownsBy).Prop("freeSide", row.FreeSide).Prop("freeSideBy", row.FreeSideBy).Prop("contactSeen", row.ContactSeen).Prop("contactHad", row.ContactHad).Prop("shotsAtCellAfterVale", row.ShotsAtCellAfterVale)
                    .Prop("verdict", row.Verdict).Prop("replayIdentical", same).OpenArr("why"); foreach (var x in row.Why) w.Str(x); w.EndArr().EndObj();
            }
            w.EndArr();
            all[name] = rows;
            var trig = Scenarios.Triggers(rows, name == "today" ? null : all.TryGetValue("today", out var t) ? t : null);
            var leth = Scenarios.Lethality(Engine.New(inputs, mk()));
            Console.WriteLine(FormattableString.Invariant($"{name,-15} 6.4 triggers: {(trig.Count == 0 ? "none" : string.Join(" | ", trig))}"));
            Console.WriteLine(FormattableString.Invariant($"{name,-15} 6.4.7 lethality (two trained shooters at the sidearm's effective ring, open ground): an op down in {leth.op:0.0} s, the Enforcer in {leth.enforcer:0.0} s{(leth.op < 2.5 ? "  TRIGGER (< 2.5 s)" : "")}"));
            var lg = Engine.New(inputs, mk()); var lethCell = Scenarios.LethalityCell(lg); // §6.4.7b is a lab line (tuning.md §6.2): no trigger with the lab off
            Console.WriteLine(FormattableString.Invariant($"{name,-15} 6.4.7b cell → hostile (three trained sidearms at their effective ring on an uncovered rival): down {lethCell:0.0} s after the first shot{(lg.Lab && lethCell < 1.0 ? "  TRIGGER (< 1.0 s)" : lg.Lab ? "" : "  (today: no line)")}"));
            w.OpenArr("triggers"); foreach (var x in trig) w.Str(x); w.EndArr();
            w.Key("lethalityS").Raw(FormattableString.Invariant($"{{\"op\":{leth.op:0.0},\"enforcer\":{leth.enforcer:0.0},\"cellOnRival\":{lethCell:0.0}}}"));
            w.EndObj();
        }
        w.EndArr().EndObj();
        if (want == null || want.Contains("lab")) // Suture's micro probes (balance.md F4, F5; tuning.md §6.3, §6.4), on the rulebook's lab defaults
        {
            Console.WriteLine("micro  " + Scenarios.PanicSpam(inputs, 30, 10, 5));
            Console.WriteLine("micro  " + Scenarios.VolleyCharge(inputs, 1, 30, false));
            Console.WriteLine("micro  " + Scenarios.VolleyCharge(inputs, 1, 30, true));
            Console.WriteLine("micro  " + Scenarios.VolleyCharge(inputs, 6, 30, false));
        }
        if (want == null && Arg("--runs") == null && !probe)
        {
            File.WriteAllText(Path.Combine(root, "briefs/027/keel/fight-lab.json"), w.ToString() + "\n");
            var (loud, safe) = Scenarios.Checkpoints(inputs);
            File.WriteAllText(Path.Combine(root, "briefs/027/checkpoints/cp-loud.script.json"), Script.Write(loud, inputs.Hash) + "\n");
            File.WriteAllText(Path.Combine(root, "briefs/027/checkpoints/cp-safe.script.json"), Script.Write(safe, inputs.Hash) + "\n");
            Console.WriteLine($"wrote briefs/027/keel/fight-lab.json; checkpoints CP-LOUD at {Report.Clock(loud.Tick)}, CP-SAFE at {Report.Clock(safe.Tick)} (briefs/027/checkpoints)");
        }
        Console.WriteLine(replayOk ? "every run replays identically from its order log" : "A RUN DOES NOT REPLAY");
        return replayOk ? 0 : 1;
    }
}
