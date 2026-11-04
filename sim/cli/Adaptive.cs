// Adaptive plans (brief 080, Waymark; Sky 2026-10-30: "let the game play organically", "checks shouldn't be so rigid"). A person doesn't give an order at a tick: they
// give it when what they can see says so ("when the rivals' car has parked, Mislead the driver"; "Go when the car is free, retry if refused"). A plan
// (contracts/missions/<mission>/adaptive/<id>.adaptive.json, suborned.mission.adaptive/1) is a list of such rules, played on one seed by this driver, so a seed that moves
// the rivals 15 s moves the plan's orders with them, as it would a player's.
//
// The driver only calls Game.Issue at a tick, as any host does. It draws nothing from the engine's streams and writes nothing to it: what it decides is the orders, and a
// run replays exactly from seed + those orders (Engine.Replay; `batch` checks it unless --no-replay, and bands.ts checks saved runs through `play`).
//
// What a plan may read (Sees below), and nothing else. Every condition names something a player has on screen: the clock; their own four (where, how hurt, whether busy,
// the Enforcer's warning); people and rivals on the street (the map shows them, walking, standing, down); Alert; the tape's time left; and the lines of VisibleLog (each with
// how the player sees it). A plan that names any other log code or condition is rejected at load (unknown = reject): no plan can read a rival's next roll, a cone's phase
// or a hidden timetable, and a Watch's knowledge is only used by plans that did the Watch (the `after` on a Watch rule; the engine refuses what the cell hasn't learned).
//
// The human model (tools/qa/human-profile.json, Suture, measured from Sky's saved sessions): one input at a time. Between two inputs lies a gap (values.gapS, lognormal);
// from the moment a rule's conditions hold to its order lies a reaction (measured.firstOrderS, the time from a standing start to an order). `pace` in a plan scales these:
//   human       the profile as measured, no lapses: a person playing their plan
//   distracted  the profile with its idle lapses (values.idleProb, idleS): a person whose attention wanders
//   sharp       half the profile: the fully informed player, for the windows' other end
// `react`: "expected" on a rule halves its reaction (the player is waiting for this: the ambush they planned for). `with` gives a rule as one input with another (a squad's
// orders: measured.squadShareOfDecisions, F on the four). Gaps and reactions stop at 15 s, as the profile's gapS does (longer is an idle lapse, the `distracted` pace's). Every draw is from a stream keyed on seed + plan + what it's for, so one rule added never reshuffles the others'
// luck more than its own inputs do.
//
// A rule: { id, op, do, to, target, approach, run, force, how, stance  (the order, in the engine's words, parsed by Order.Parse: unknown = reject);
//   when: [conditions], all must hold · after: [rule ids], each accepted · unless: [conditions], any stops the rule for good · by: "m:ss", it lapses ·
//   retry: seconds before trying again after a refusal (default 3) · repeat: seconds until it is given again while `when` holds (default: once) · tries: refusals before giving up (default 40) ·
//   interrupt: true, given over the op's act (default: waits for the op to finish; a walk is never waited for) · late: seconds of extra delay (the window check's knob) · with · react · why }
// Conditions (a leading ! negates): t>=m:ss · t<m:ss · done:rule · fin:rule (accepted, and its op is free again) · since:rule:s (accepted s ago) · log:code · sincelog:code:s ·
//   free:op · left<=:op:s (the op's act has s or less to run: its ring) · drawn:op · up:op · down:op · hp<=:op:n · alert>=:n · near:a:b:m (a, b: an op, a person's id or role, a rival, "x,y" cm, or a place id, % for its colon) · seen:who · hostiles>=:n ·
//   hostilesnear:a:m:n · blocker · loopleft>=:s · nobody:who:m (no passer-by on the street within m) · clear:who:how (the takedown card shows nobody who'd see it)
// Targets "$blocker" (the hostile the car's mark names) and "$nearest:op" (the nearest hostile up) are resolved when the order is given.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Suborned.Sim;

static class Adaptive
{
    /// <summary>The log lines a player sees, with how. Anything else (ai.*, attention.*, witness.*, entry.*) is the engine talking to itself or to the debrief: not a plan's to read.</summary>
    static readonly Dictionary<string, string> VisibleLog = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["rivals.coming"] = "the car turning in, and its card", ["rivals.drop"] = "three men getting out by your car", ["rivals.arrive"] = "the car parked across from Vale's, and its card",
        ["rivals.misled"] = "the rival car driving off, and its card", ["rivals.late"] = "the rival car pulling up", ["rivals.retreat"] = "the crew running for their car, and its card", ["rivals.down"] = "bodies on the street",
        ["loop.on"] = "the Tech's card; the camera's lens mark", ["loop.ends"] = "the card; the lens mark", ["patrol.rerouted"] = "the robot walking off; the card", ["patrol.returns"] = "the card",
        ["call.waitress"] = "the call's card", ["guard.at-cafe"] = "the guard beside Inez; the card", ["guard.back"] = "the guard at his door; the card",
        ["enter.in"] = "the Enforcer gone in", ["enter.out"] = "the Enforcer and Vale at the service door", ["take.done"] = "the Take's card",
        ["car.parks"] = "Vale's car at the service bay", ["car.leaves"] = "Vale's car leaving", ["chime"] = "the chime and its card", ["vale.exits"] = "Vale on the steps",
        ["fight.safe"] = "the fight's card", ["fight.loud"] = "the fight's card and the police", ["fight.ambush"] = "the first shots, or Inez's warning", ["fight.down"] = "a man falling", ["fight.over"] = "the card; the street goes quiet",
        ["fight.blocker"] = "the mark on the car", ["blocker.out"] = "the mark on the car clears", ["car.free"] = "the car's mark clears; the card",
        ["board.in"] = "the Board's card", ["enforcer.warning"] = "the roster flash and his line, brief 080",
        ["scout.watch.watched"] = "the Watch's card", ["scout.listen.heard"] = "the Listen's card", ["turn.help.turned"] = "the Turn's card", ["turn.pressure.turned"] = "the Turn's card", ["turn.pay.turned"] = "the Turn's card", ["turn.help.not-yet"] = "the Turn's card",
        ["takedown.out"] = "the body on the ground", ["takedown.fails"] = "the struggle", ["takedown.seen"] = "the card", ["guard.down"] = "the guard falling", ["patrol.down"] = "the robot falling", ["security.down"] = "officers falling",
        ["alarm.dispatches"] = "the alarm's card", ["seal.starts"] = "the seal clock on the HUD", ["gunfire"] = "the shots", ["report"] = "the Alert pip and the card", ["camera.dark"] = "the lens mark",
        ["disable.on"] = "the Tech's card; the lens goes dark", ["close.warning"] = "the clock turns and its line (fight.close, 6:30)",
        ["camera.record-lands"] = "the card and the Alert pip (brief 096)", ["car.report-lands"] = "the card and the Alert pip (brief 096)", ["car.kerbside"] = "Vale's car pulling in at the kerb (brief 096)",
    };

    public sealed class Pace { public double GapP50, GapP90, ReactP50, ReactP90, IdleProb, IdleP50, IdleP90; }
    internal sealed class Rule
    {
        public string Id = "", Why = "", OrderText = "", Op = "", Do = "", TargetVar = "", With = "";
        public Func<Ctx, bool>[] When = Array.Empty<Func<Ctx, bool>>(), Unless = Array.Empty<Func<Ctx, bool>>();
        public int By = int.MaxValue, RetryT = 30, RepeatT, Tries = 40, LateT; public bool Interrupt, Expected;
    }
    public sealed class Plan { internal List<Rule> Rules = new List<Rule>(); public string Id = "", Style = ""; public int Until; public Pace Pace = new Pace(); internal Dictionary<string, int> Index = new Dictionary<string, int>(StringComparer.Ordinal); internal JNode? StartNode; }
    /// <summary>What a band reads beside the game: never hashed, never read by the game.</summary>
    public sealed class Extra
    {
        public string Plan = "", Style = ""; public int IdleMaxT, IdleAtT, Refused, Inputs, Unpriced, GiveUps; public int FirstShotT = -1;
        public readonly SortedDictionary<string, int> Refusals = new SortedDictionary<string, int>(StringComparer.Ordinal), Given = new SortedDictionary<string, int>(StringComparer.Ordinal);
        public readonly List<string> Never = new List<string>();
    }

    /// <summary>What the rules read: only the Game's public, on-screen facts (the file's header).</summary>
    internal sealed class Ctx
    {
        public Game G = null!; public int T; public readonly Dictionary<string, int> First = new Dictionary<string, int>(StringComparer.Ordinal); public int LogAt; public int[] DoneAt = Array.Empty<int>(); public bool[] Fin = Array.Empty<bool>();
        public Op Op(string name) { var id = Suborned.Sim.Op.Parse(name); foreach (var o in G.Ops) if (o.Id == id) return o; throw new FormatException(name); }
        public void Drain() { var l = G.Log; for (; LogAt < l.Count; LogAt++) if (!First.ContainsKey(l[LogAt].Code)) First[l[LogAt].Code] = l[LogAt].Tick; }
        public static bool Free(Op o) => !o.Down && o.Busy == null && o.Act == null && o.Walk == null && !o.Inside;
        public Hostile? Blocker() { foreach (var h in G.Hostiles) if (h.Present && !h.Down && h.Blocker) return h; return null; }
        public IEnumerable<(string id, Pt at)> Foes()
        {
            foreach (var m in G.Crew) if (G.PosOf(m.Id) is Pt p) yield return (m.Id, p);
            foreach (var h in G.Hostiles) if (h.Present && !h.Down) yield return (h.Id, h.Pos(T));
        }
    }

    static Func<Ctx, Pt?> Where(Game? _, string spot)
    {
        var who = spot.Replace('%', ':'); // a place id's colon ("d:Getaway bay") is written % in a condition
        var c = who.Split(',');
        if (c.Length == 2 && int.TryParse(c[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var x) && int.TryParse(c[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var y)) { var pt = new Pt(x, y); return _ => pt; }
        return k => k.G.S.HasPlace(who) ? k.G.S.Stand(who) : k.G.PosOf(who);
    }
    static int Clock(string s, string where)
    {
        var p = s.Split(':'); if (p.Length != 2) throw new FormatException($"{where}: '{s}' is no m:ss");
        return (int.Parse(p[0], CultureInfo.InvariantCulture) * 60 + int.Parse(p[1], CultureInfo.InvariantCulture)) * Game.Hz;
    }
    static Func<Ctx, bool> Cond(string text, Plan plan, string where)
    {
        if (text.StartsWith('!')) { var f = Cond(text.Substring(1), plan, where); return k => !f(k); }
        var a = text.Split(':');
        string A(int i) => i < a.Length ? a[i] : throw new FormatException($"{where}: condition '{text}' wants more");
        int N(int i) => int.Parse(A(i), CultureInfo.InvariantCulture);
        int RuleIx(string id) => plan.Index.TryGetValue(id, out var ix) ? ix : throw new FormatException($"{where}: no rule '{id}' (unknown = reject)");
        string Vis(string code) => VisibleLog.ContainsKey(code) ? code : throw new FormatException($"{where}: log code '{code}' isn't one a player sees (Adaptive.VisibleLog; unknown = reject)");
        string OpN(string s) { Op.Parse(s); return s; }
        switch (a[0])
        {
            case "t>=": { int v = Clock(A(1) + ":" + A(2), where); return k => k.T >= v; }
            case "t<": { int v = Clock(A(1) + ":" + A(2), where); return k => k.T < v; }
            case "done": { int r = RuleIx(A(1)); return k => k.DoneAt[r] >= 0; }
            case "fin": { int r = RuleIx(A(1)); return k => k.Fin[r]; }
            case "since": { int r = RuleIx(A(1)), s = N(2) * Game.Hz; return k => k.DoneAt[r] >= 0 && k.T - k.DoneAt[r] >= s; }
            case "log": { var c = Vis(A(1)); return k => k.First.ContainsKey(c); }
            case "sincelog": { var c = Vis(A(1)); int s = N(2) * Game.Hz; return k => k.First.TryGetValue(c, out var at) && k.T - at >= s; }
            case "free": { var o = OpN(A(1)); return k => Ctx.Free(k.Op(o)); }
            case "drawn": { var o = OpN(A(1)); return k => k.Op(o).Gun.Drawn; }
            case "left<=": { var o = OpN(A(1)); int n = N(2) * Game.Hz; return k => k.Op(o).Busy != null && k.Op(o).BusyEnd - k.T <= n; } // the act's progress ring
            case "up": { var o = OpN(A(1)); return k => !k.Op(o).Down; }
            case "down": { var o = OpN(A(1)); return k => k.Op(o).Down; }
            case "hp<=": { var o = OpN(A(1)); int n = N(2); return k => k.Op(o).Gun.Health <= n; }
            case "alert>=": { int n = N(1); return k => k.G.Alert >= n; }
            case "near": { var x = Where(null, A(1)); var y = Where(null, A(2)); long m = (long)(double.Parse(A(3), CultureInfo.InvariantCulture) * 100); return k => x(k) is Pt p && y(k) is Pt q && G.D2(p, q) <= m * m; }
            case "seen": { var x = Where(null, A(1)); return k => x(k).HasValue; }
            case "hostiles>=": { int n = N(1); return k => { int c = 0; foreach (var _ in k.Foes()) c++; return c >= n; }; }
            case "hostilesnear": { var x = Where(null, A(1)); long m = N(2) * 100L; int n = N(3); return k => { if (!(x(k) is Pt p)) return false; int c = 0; foreach (var (_, at) in k.Foes()) if (G.D2(p, at) <= m * m) c++; return c >= n; }; }
            case "blocker": return k => k.Blocker() != null;
            case "nobody": { var x = Where(null, A(1)); long m = N(2) * 100L; return k => { if (!(x(k) is Pt p)) return false; foreach (var w in k.G.Walkers) if (k.G.CrowdPos(w) is Pt wp && G.D2(p, wp) <= m * m) return false; return true; }; } // the map shows them
            case "clear": { var who = A(1); var how = A(2); return k => { var q = k.G.QuoteTakedown(who, how); if (q.Aware == "yes") return false; foreach (var w in q.People) if (w.verb == "radio") return false; foreach (var w in q.Coming) if (w.verb == "radio") return false; return true; }; } // the takedown's card: nobody it names sees it (GDD T15 "will they see it?")
            case "loopleft>=": { int s = N(1) * Game.Hz; return k => k.G.LoopOn && k.G.LoopUntil - k.T >= s; }
            default: throw new FormatException($"{where}: unknown condition '{text}' (unknown = reject)");
        }
    }

    static readonly string[] OrderKeys = { "op", "do", "to", "target", "approach", "run", "force", "how", "stance" };
    static readonly string[] RuleKeys = { "id", "op", "do", "to", "target", "approach", "run", "force", "how", "stance", "when", "after", "unless", "by", "retry", "repeat", "tries", "interrupt", "late", "with", "react", "why" };
    static void Value(JWriter w, JNode v)
    {
        switch (v.Kind)
        {
            case JKind.Str: w.Str(v.Str()); break;
            case JKind.Bool: w.Bool(v.Bool()); break;
            case JKind.Num: w.Raw(v.NumberText); break;
            case JKind.Arr: w.Arr(); foreach (var x in v.Items) Value(w, x); w.EndArr(); break;
            default: throw new FormatException($"{v.Path}: not an order's value");
        }
    }

    static readonly ConcurrentDictionary<string, Plan> Cache = new ConcurrentDictionary<string, Plan>(StringComparer.Ordinal);
    static (double p50, double p90) Dist(JNode n) => (double.Parse(n["p50"].NumberText, CultureInfo.InvariantCulture), double.Parse(n["p90"].NumberText, CultureInfo.InvariantCulture));
    static Pace PaceOf(string name, string root)
    {
        var f = System.IO.Path.Combine(root, "tools/qa/human-profile.json");
        var p = JNode.Parse(File.ReadAllText(f), "human-profile"); var v = p["values"]; var m = p["measured"];
        var (gp50, gp90) = Dist(v["gapS"]); var (rp50, rp90) = Dist(m["firstOrderS"]); var (ip50, ip90) = Dist(v["idleS"]);
        var o = new Pace { GapP50 = gp50, GapP90 = gp90, ReactP50 = rp50, ReactP90 = rp90, IdleP50 = ip50, IdleP90 = ip90 };
        switch (name)
        {
            case "human": return o;
            case "distracted": o.IdleProb = double.Parse(v["idleProb"].NumberText, CultureInfo.InvariantCulture); return o;
            case "sharp": o.GapP50 /= 2; o.GapP90 /= 2; o.ReactP50 /= 2; o.ReactP90 /= 2; return o;
            default: throw new FormatException($"pace '{name}': human, distracted or sharp (unknown = reject)");
        }
    }

    public static Plan Load(string path, string root) => Cache.GetOrAdd(path, _ => Parse(File.ReadAllText(path), root, System.IO.Path.GetFileName(path)));
    public static Plan Parse(string text, string root, string name)
    {
        var n = JNode.Parse(text, name);
        n.Only("schema", "mission", "id", "style", "note", "pace", "start", "until", "rules");
        if (n["schema"].Str() != "suborned.mission.adaptive/1") throw new FormatException($"{name}: schema is not suborned.mission.adaptive/1");
        if (n["mission"].Str() != "handover") throw new FormatException($"{name}: only the Handover plays");
        var plan = new Plan { Id = n["id"].Str(), Style = n["style"].Str(), Until = n.Opt("until")?.Int() ?? 0, Pace = PaceOf(n.Opt("pace")?.Str() ?? "human", root), StartNode = n.Opt("start") };
        var nodes = n["rules"].Items;
        foreach (var r in nodes)
        {
            var id = r["id"].Str(); if (plan.Index.ContainsKey(id)) throw new FormatException($"{name}: rule '{id}' twice");
            plan.Index[id] = plan.Rules.Count; plan.Rules.Add(new Rule { Id = id });
        }
        for (int i = 0; i < nodes.Count; i++)
        {
            var r = nodes[i]; var rule = plan.Rules[i]; var where = $"{name} rule {rule.Id}";
            r.Only(RuleKeys);
            var w = new JWriter().Obj().Key("tick").Raw("__T__");
            foreach (var k in OrderKeys) if (r.Opt(k) is JNode v) { if (k == "target" && v.Str().StartsWith('$')) { rule.TargetVar = v.Str(); w.Prop(k, "__TARGET__"); continue; } w.Key(k); Value(w, v); }
            rule.OrderText = w.EndObj().ToString();
            Order.Parse(JNode.Parse(rule.OrderText.Replace("__T__", "0", StringComparison.Ordinal).Replace("__TARGET__", "x", StringComparison.Ordinal), where)); // an order the engine can't parse is rejected at load
            rule.Op = r.Opt("op")?.Str() ?? ""; rule.Do = r["do"].Str(); rule.Why = r.Opt("why")?.Str() ?? ""; rule.With = r.Opt("with")?.Str() ?? "";
            if (rule.With.Length > 0 && !plan.Index.ContainsKey(rule.With)) throw new FormatException($"{where}: with '{rule.With}': no such rule");
            var when = new List<Func<Ctx, bool>>();
            if (r.Opt("after") is JNode af) foreach (var x in af.Strs()) when.Add(Cond("done:" + x, plan, where));
            if (r.Opt("when") is JNode wn) foreach (var x in wn.Strs()) when.Add(Cond(x, plan, where));
            rule.When = when.ToArray();
            if (r.Opt("unless") is JNode un) { var l = new List<Func<Ctx, bool>>(); foreach (var x in un.Strs()) l.Add(Cond(x, plan, where)); rule.Unless = l.ToArray(); }
            if (r.Opt("by") is JNode by) rule.By = Clock(by.Str(), where);
            if (r.Opt("retry") is JNode rt) rule.RetryT = Tenths(rt);
            if (r.Opt("repeat") is JNode rp) rule.RepeatT = Tenths(rp);
            if (r.Opt("tries") is JNode tr) rule.Tries = tr.Int();
            if (r.Opt("interrupt") is JNode ip) rule.Interrupt = ip.Bool();
            if (r.Opt("late") is JNode lt) rule.LateT = Tenths(lt);
            if (r.Opt("react") is JNode rc) rule.Expected = rc.Str() == "expected" ? true : throw new FormatException($"{where}: react: expected");
        }
        return plan;
    }
    static int Tenths(JNode v) => (int)Math.Round(double.Parse(v.NumberText, CultureInfo.InvariantCulture) * Game.Hz);

    /// <summary>The seeded draws (FNV-1a key, mulberry32, as tools/qa/human.ts's streams): the driver's own, apart from the engine's.</summary>
    sealed class Draws
    {
        uint a;
        public Draws(int seed, string plan, string name) { uint h = 2166136261; foreach (var ch in $"{seed}:{plan}:{name}") { h ^= ch; h *= 16777619; } a = h == 0 ? 1 : h; }
        public double U() { unchecked { a += 0x6d2b79f5; uint t = a; t = (t ^ (t >> 15)) * (t | 1); t ^= t + (t ^ (t >> 7)) * (t | 61); return (t ^ (t >> 14)) / 4294967296.0; } }
        public int Time(double p50, double p90, double scale = 1, double cap = 15)
        {
            double sigma = p90 > p50 && p50 > 0 ? Math.Log(p90 / p50) / 1.2815516 : 0, u1 = Math.Max(1e-9, U()), u2 = U(), z = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
            return (int)Math.Round(Math.Min(cap, Math.Max(0.1, p50 * scale * Math.Exp(sigma * z))) * Game.Hz);
        }
    }

    /// <summary>Plays a plan on a seed: the whole driver. Mirrors Engine.Play's order of things (run to the tick, give the tick's orders), so Engine.Replay lands on the same checkpoints.</summary>
    public static Game Play(Engine.Inputs inputs, Plan plan, int seed, out Extra extra)
    {
        var start = Start.Parse(plan.StartNode); start.Seed = seed;
        var g = Engine.New(inputs, start); int until = plan.Until > 0 ? plan.Until : Game.T(inputs.SiteData.Over);
        var x = new Extra { Plan = plan.Id, Style = plan.Style }; extra = x;
        var rules = plan.Rules; int n = rules.Count;
        var k = new Ctx { G = g, DoneAt = new int[n], Fin = new bool[n] };
        var armed = new int[n]; var due = new int[n]; var retryAt = new int[n]; var refused = new int[n]; var dead = new bool[n]; var nextRepeat = new int[n];
        for (int i = 0; i < n; i++) { k.DoneAt[i] = -1; armed[i] = -1; }
        var withOf = new List<int>[n]; for (int i = 0; i < n; i++) if (rules[i].With.Length > 0) (withOf[plan.Index[rules[i].With]] ??= new List<int>()).Add(i);
        var gaps = new Draws(seed, plan.Id, "gap"); var reacts = new Draws(seed, plan.Id, "react"); var lapses = new Draws(seed, plan.Id, "idle");
        int nextInput = 0, idleRun = 0;

        bool Give(int i, Rule r, int t)
        {
            var txt = r.OrderText.Replace("__T__", t.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
            if (r.TargetVar.Length > 0)
            {
                string? who = null;
                if (r.TargetVar == "$blocker") who = k.Blocker()?.Id;
                else if (r.TargetVar.StartsWith("$nearest:", StringComparison.Ordinal))
                {
                    var o = k.Op(r.TargetVar.Substring(9)); long best = long.MaxValue;
                    foreach (var (id, at) in k.Foes()) { var d = G.D2(o.Pos, at); if (d < best) { best = d; who = id; } }
                }
                else throw new FormatException($"rule {r.Id}: target {r.TargetVar}: $blocker or $nearest:op");
                if (who == null) return false;
                txt = txt.Replace("__TARGET__", who, StringComparison.Ordinal);
            }
            var a = g.Issue(Order.Parse(JNode.Parse(txt, r.Id)));
            x.Inputs++;
            if (a.ok)
            {
                k.DoneAt[i] = t; retryAt[i] = 0; x.Given[r.Id] = t; if (r.RepeatT > 0) nextRepeat[i] = t + r.RepeatT;
                if (r.Op.Length > 0 && r.Op != "cell" && k.Op(r.Op).Busy is Order b && k.Op(r.Op).BusyQuote == null && Priced(b.Do)) x.Unpriced++;
                if (r.Do == "fire" && x.FirstShotT < 0) x.FirstShotT = t;
            }
            else
            {
                x.Refused++; var key = r.Do + ":" + a.code; x.Refusals.TryGetValue(key, out var c); x.Refusals[key] = c + 1;
                if (++refused[i] >= r.Tries) { dead[i] = true; x.GiveUps++; }
                retryAt[i] = t + r.RetryT;
            }
            return a.ok;
        }

        for (int t = 0; t <= until; t++)
        {
            if (t > 0) g.RunTo(t);
            if (g.Over && g.Tick != t) break;
            k.T = t; k.Drain();
            // the whole cell idle: nobody walking, acting, inside or firing, nobody up in the car waiting on Go (a measure, never read by a rule)
            if (!g.Over)
            {
                bool idle = true; foreach (var o in g.Ops) if (!o.Down && (!Ctx.Free(o) || o.Gun.Order.Length > 0 || o.InCar)) { idle = false; break; }
                if (idle) { if (++idleRun > x.IdleMaxT) { x.IdleMaxT = idleRun; x.IdleAtT = t; } } else idleRun = 0;
            }
            for (int i = 0; i < n; i++)
            {
                var r = rules[i];
                if (k.DoneAt[i] >= 0 && !k.Fin[i] && (r.Op.Length == 0 || r.Op == "cell" || Ctx.Free(k.Op(r.Op)) || t > k.DoneAt[i] && k.Op(r.Op).Busy == null && k.Op(r.Op).Act == null && r.Do != "move")) k.Fin[i] = true;
                if (dead[i] || r.With.Length > 0) continue;
                if (k.DoneAt[i] >= 0 && (r.RepeatT == 0 || t < nextRepeat[i])) continue;
                if (t > r.By) { dead[i] = true; continue; }
                bool stop = false; foreach (var u in r.Unless) if (u(k)) { stop = true; break; }
                if (stop) { dead[i] = true; continue; }
                bool ok = true; foreach (var w in r.When) if (!w(k)) { ok = false; break; }
                if (!ok) { if (r.LateT == 0) armed[i] = -1; continue; } // `late`: the delay counts from the first time the rule could fire, as a person's would
                if (armed[i] < 0)
                {
                    armed[i] = t; due[i] = t + r.LateT + reacts.Time(plan.Pace.ReactP50, plan.Pace.ReactP90, r.Expected ? 0.5 : 1);
                }
                if (t < due[i] || t < nextInput || t < retryAt[i] || g.Over) continue;
                if (!r.Interrupt && r.Op.Length > 0 && r.Op != "cell") { var o = k.Op(r.Op); if (o.Busy != null || o.Act != null || o.Down) continue; }
                bool given = Give(i, r, t);
                if (given && withOf[i] != null) foreach (var j in withOf[i]) { if (!g.Over) Give(j, rules[j], t); }
                nextInput = t + gaps.Time(plan.Pace.GapP50, plan.Pace.GapP90);
                if (plan.Pace.IdleProb > 0 && lapses.U() < plan.Pace.IdleProb) nextInput += lapses.Time(plan.Pace.IdleP50, plan.Pace.IdleP90, 1, 60);
                armed[i] = given ? -1 : armed[i];
                k.Drain();
                if (g.Over) break;
            }
            if (g.Over) break;
        }
        if (!g.Over) g.RunTo(until);
        g.Finish();
        for (int i = 0; i < n; i++) if (k.DoneAt[i] < 0) x.Never.Add(rules[i].Id);
        return g;
    }
    /// <summary>The verbs the engine itself holds a quote for while they run (Game.Quote): the rest of the price lines are the host's, from text.json less.price (bands.ts R-4).</summary>
    static bool Priced(string verb) => verb is "listen" or "turn";

    /// <summary>One line a band reads (batch --digest): the run without its log, for 200 seeds of a plan to stay small.</summary>
    public static string Digest(string id, Game g, Extra? x, bool same, bool replayed)
    {
        var w = new JWriter().Obj().Prop("id", id).Prop("plan", x?.Plan ?? "").Prop("style", x?.Style ?? "").Prop("seed", g.Seed).Prop("outcome", g.Result.ToString().ToLowerInvariant()).Prop("fail", g.FailCode).Prop("tick", g.Tick);
        w.Prop("cash", g.Cash).Prop("trail", g.Trail).Prop("alert", g.Alert).Prop("gunfire", g.Gunfire).Prop("standing", g.Standing);
        w.Prop("startHash", Hash.Hex(g.StartHash)).Prop("finalHash", Hash.Hex(g.Checkpoints[g.Checkpoints.Count - 1].hash));
        if (replayed) w.Prop("replayIdentical", same);
        w.Open("stats").Prop("fightS", g.CellFightS).Prop("shots", g.CellShots).Prop("hits", g.CellHits).Prop("hpLost", g.CellHpLost).Prop("down", g.CellDownCount).EndObj();
        if (g.Result == Outcome.Success)
        {
            var d = Debrief.Of(g);
            w.Prop("campaignTrail", d.Next.CampaignTrail).Prop("campaignStanding", d.Next.CampaignStanding).Prop("openCases", d.Next.OpenCases);
        }
        if (g.Cause != null) { w.Key("cause"); g.Cause.Write(w); }
        var marks = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var l in g.Log) if (!marks.ContainsKey(l.Code) && Marks.Contains(l.Code)) marks[l.Code] = l.Tick;
        w.Open("marks"); foreach (var kv in marks) w.Prop(kv.Key, kv.Value); w.EndObj();
        w.OpenArr("held"); foreach (var l in g.Log) if (l.Code.StartsWith("car.held.", StringComparison.Ordinal)) w.Obj().Prop("tick", l.Tick).Prop("how", l.Code.Substring(9)).Prop("who", l.Text.Split(' ')[0]).EndObj(); w.EndArr(); // MC-11 reads them
        // full health to down (design §4: nobody under 5 s, the Enforcer under 7 s): each op's first hit to the line that says he fell; a measure only
        var hit = new Dictionary<string, int>(StringComparer.Ordinal); w.OpenArr("fullToDown");
        foreach (var l in g.Log)
        {
            foreach (var op in OpNames)
            {
                if (l.Code.StartsWith(op + ".hit", StringComparison.Ordinal) && !hit.ContainsKey(op)) hit[op] = l.Tick;
                else if (hit.TryGetValue(op, out var h) && (l.Code == op + ".down" || (op == "enforcer" && (l.Code == "fail.enforcer-down" || (l.Code == "fail.rivals-reach-vale" && l.Text.StartsWith("The Enforcer is down", StringComparison.Ordinal)))))) { w.Obj().Prop("op", op).Prop("s10", l.Tick - h).EndObj(); hit[op] = int.MaxValue / 2; }
            }
        }
        w.EndArr();
        if (x != null)
        {
            w.Prop("idleMaxS", x.IdleMaxT / Game.Hz).Prop("idleAt", x.IdleAtT).Prop("inputs", x.Inputs).Prop("refused", x.Refused).Prop("unpriced", x.Unpriced).Prop("giveUps", x.GiveUps).Prop("firstShot", x.FirstShotT);
            w.Open("refusals"); foreach (var kv in x.Refusals) w.Prop(kv.Key, kv.Value); w.EndObj();
            w.Open("given"); foreach (var kv in x.Given) w.Prop(kv.Key, kv.Value); w.EndObj();
            w.OpenArr("never"); foreach (var s in x.Never) w.Str(s); w.EndArr();
        }
        return w.EndObj().ToString();
    }
    static readonly string[] OpNames = { "scout", "fixer", "tech", "enforcer" };
    static readonly HashSet<string> Marks = new HashSet<string>(StringComparer.Ordinal) { "enter.in", "enter.out", "take.done", "fight.safe", "fight.loud", "fight.ambush", "blocker.out", "car.free", "board.in", "rivals.arrive", "rivals.coming", "attention.reporting", "seal.starts", "captured", "rivals.wiped", "enforcer.warning", "loop.on", "loop.ends", "guard.at-cafe", "guard.back", "call.waitress", "takedown.out", "takedown.seen", "takedown.fails", "fight.over", "camera.recorded", "camera.record-lands", "car.reported", "car.report-lands", "car.kerbside" };
}
