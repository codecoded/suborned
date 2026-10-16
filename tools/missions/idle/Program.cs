// suborned-idle <script.json>…: replays each route script the way Engine.Play does, but one tick at a time, and samples every
// operative's Game.ActivityOf (pure: changes nothing) after the tick's orders are issued. Prints one JSON line per script:
// { id, outcome, fail, end, hashMatch, segments: { scout: [[from, to, phase, verb, target], …], … }, log: [[tick, code, text]], orders: [[tick, op, do, target]] }
// hashMatch = this stepped run's final checkpoint equals Engine.Play's, so the sampling did not change the run.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Suborned.Sim;

static class Program
{
    static string Esc(string s) { var b = new StringBuilder("\""); foreach (var c in s) { if (c == '"' || c == '\\') b.Append('\\').Append(c); else if (c < ' ') b.Append(' '); else b.Append(c); } return b.Append('"').ToString(); }

    static int Main(string[] args)
    {
        var root = Engine.FindRoot(Directory.GetCurrentDirectory());
        var inputs = Engine.Load(root);
        foreach (var path in args)
        {
            var script = Script.Parse(File.ReadAllText(path));
            var reference = Engine.Play(inputs, script.Start, script.Orders, script.Until);
            var g = Engine.New(inputs, script.Start);
            int until = script.Until > 0 ? script.Until : Game.T(inputs.SiteData.Over);
            var list = new List<(int idx, Order o)>(); for (int k = 0; k < script.Orders.Count; k++) list.Add((k, script.Orders[k]));
            list.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.idx.CompareTo(b.idx));
            var segs = new List<(int from, int to, string phase, string verb, string target)>[4];
            for (int i = 0; i < 4; i++) segs[i] = new List<(int, int, string, string, string)>();
            int next = 0;
            void Sample()
            {
                for (int i = 0; i < 4; i++)
                {
                    var a = g.ActivityOf((OpId)i); var op = g.OpOf((OpId)i);
                    string phase = a.Phase, verb = a.Verb, target = a.Target;
                    if (phase == "idle" && op.Gun.Drawn) { phase = "armed"; verb = "stance:" + op.Gun.Stance; }   // weapon out, firing at will or ready
                    if (phase == "running") phase = "walking";
                    if (phase == "walking") target = "";
                    var l = segs[i];
                    if (l.Count > 0 && l[^1].phase == phase && l[^1].verb == verb && l[^1].target == target && l[^1].to == g.Tick) l[^1] = (l[^1].from, g.Tick + 1, phase, verb, target);
                    else l.Add((g.Tick, g.Tick + 1, phase, verb, target));
                }
            }
            bool stopped = false;
            while (!stopped)
            {
                while (next < list.Count && list[next].o.Tick == g.Tick) { if (list[next].o.Tick <= until) g.Issue(list[next].o); next++; }
                if (g.Over || g.Tick >= until) break;
                Sample();
                g.Step();
                while (next < list.Count && list[next].o.Tick < g.Tick) next++;  // never: orders are on ticks the loop visits
            }
            g.Finish();
            string h(Game x) => Hash.Hex(x.Checkpoints[x.Checkpoints.Count - 1].hash);
            var sb = new StringBuilder();
            sb.Append("{\"id\":").Append(Esc(System.IO.Path.GetFileName(path))).Append(",\"outcome\":").Append(Esc(g.Result.ToString())).Append(",\"fail\":").Append(Esc(g.FailCode))
              .Append(",\"end\":").Append(g.Tick).Append(",\"hashMatch\":").Append(h(g) == h(reference) ? "true" : "false").Append(",\"segments\":{");
            for (int i = 0; i < 4; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Esc(Op.Name((OpId)i))).Append(":[");
                for (int k = 0; k < segs[i].Count; k++) { var s = segs[i][k]; if (k > 0) sb.Append(','); sb.Append('[').Append(s.from).Append(',').Append(s.to).Append(',').Append(Esc(s.phase)).Append(',').Append(Esc(s.verb)).Append(',').Append(Esc(s.target)).Append(']'); }
                sb.Append(']');
            }
            sb.Append("},\"log\":[");
            for (int k = 0; k < g.Log.Count; k++) { var l = g.Log[k]; if (k > 0) sb.Append(','); sb.Append('[').Append(l.Tick).Append(',').Append(Esc(l.Code)).Append(',').Append(Esc(l.Text)).Append(']'); }
            sb.Append("],\"orders\":[");
            for (int k = 0; k < list.Count; k++) { var o = list[k].o; if (k > 0) sb.Append(','); sb.Append('[').Append(o.Tick).Append(',').Append(Esc(o.Op)).Append(',').Append(Esc(o.Do)).Append(',').Append(Esc(o.Target.Length > 0 ? o.Target : o.To)).Append(']'); }
            sb.Append("]}");
            Console.WriteLine(sb.ToString());
        }
        return 0;
    }
}
