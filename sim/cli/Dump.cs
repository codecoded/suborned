// suborned-sim dump <script.json> <out.json> [--crowd-step n] [--no-fans] (Splice, brief 081): a route script played through the real engine with every
// tick read out for the look tool (bun run simview; tools/simview). Read-only: it plays as Engine.Play does (same orders on the same ticks) and only reads between
// ticks; then it plays the script again untouched by reads and exits 1 unless every checkpoint matches. No rule, order or route changes here.
// Written once per tick (10 Hz) for the street's actors, every crowd-step ticks for the crowd (default 1), every 5 ticks for the sight fans.
// Format (suborned.simdump/1): positions in cm in the block frame, tracks as spans [startTick, x, y, dx, dy, dx, dy, ...] one pair per `step` ticks; anything that changes
// rarely as run-length keys [tick, value]. GunOf is never called for a crowd person (it would make their gun: state): their health comes from the shots.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;

static class DumpCli
{
    sealed class Track
    {
        public string Id = "", Label = "", Side = "", Cls = "", Detail = ""; public int Step = 1, HpMax;
        public readonly List<List<int>> Spans = new List<List<int>>(); List<int>? cur; int lx, ly, lastT = -1;
        public readonly List<(int t, int flags, string text)> St = new List<(int, int, string)>();
        public readonly List<(int t, int hp)> Hp = new List<(int, int)>();
        public readonly List<(int t, int deg)> Face = new List<(int, int)>();
        public Pt? Last;
        public void At(int t, Pt? p, int flags, string text, int hp, Pt? face)
        {
            if (p.HasValue) Last = p;
            if (St.Count == 0 || St[^1].flags != flags || St[^1].text != text) St.Add((t, flags, text));
            if (hp >= 0 && (Hp.Count == 0 || Hp[^1].hp != hp)) Hp.Add((t, hp));
            if (face is Pt f && (f.X != 0 || f.Y != 0))
            {
                int d = (int)Math.Round(Math.Atan2(f.Y, f.X) * 180 / Math.PI / 5) * 5; // 5 degree steps: facing is a read, not a rule
                if (Face.Count == 0 || Face[^1].deg != d) Face.Add((t, d));
            }
            if (!p.HasValue) { cur = null; return; }
            if (cur == null) { cur = new List<int> { t, p.Value.X, p.Value.Y }; Spans.Add(cur); lastT = t; lx = p.Value.X; ly = p.Value.Y; return; }
            if (t - lastT < Step) return;
            cur.Add(p.Value.X - lx); cur.Add(p.Value.Y - ly); lx = p.Value.X; ly = p.Value.Y; lastT = t;
        }
    }

    sealed class CarTrack { public string Id = ""; public readonly List<List<int>> Keys = new List<List<int>>(); public readonly List<(int t, string st, int hp)> St = new List<(int, string, int)>(); }

    static string Cl(int tick) => Report.Clock(tick);
    static void Ints(JWriter w, IEnumerable<int> xs) { w.Raw("[" + string.Join(",", xs.Select(x => x.ToString(CultureInfo.InvariantCulture))) + "]"); }

    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        if (args.Length < 3) { Console.Error.WriteLine("usage: dump <script.json> <out.json> [--crowd-step n] [--no-fans]"); return 2; }
        int cs = Array.IndexOf(args, "--crowd-step"); int crowdStep = cs > 0 ? int.Parse(args[cs + 1], CultureInfo.InvariantCulture) : 1;
        bool fans = Array.IndexOf(args, "--no-fans") < 0;
        var path = File.Exists(args[1]) ? args[1] : System.IO.Path.Combine(root, args[1]);
        var script = Script.Parse(File.ReadAllText(path));
        if (script.Mission != "handover") { Console.Error.WriteLine("dump plays the Handover only"); return 2; }
        return Scenarios.Bounded(() => Dump(inputs, script, path, args[2], crowdStep, fans));
    }

    static int Dump(Engine.Inputs inputs, Script script, string path, string outPath, int crowdStep, bool fans)
    {
        var g = Engine.New(inputs, script.Start);
        int until = script.Until <= 0 ? Game.T(inputs.SiteData.Over) : script.Until;
        var list = script.Orders.Select((o, k) => (k, o)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).Select(x => x.o).ToList();
        var tracks = new Dictionary<string, Track>(StringComparer.Ordinal); var order = new List<Track>();
        var shots = new JWriter().Arr(); var hud = new List<(int t, string v)>(); var windows = new List<(int t, string v)>();
        var fanKeys = new SortedDictionary<string, (string kind, List<(int t, string v)> keys)>(StringComparer.Ordinal);
        var cars = new Dictionary<string, CarTrack>(StringComparer.Ordinal);
        foreach (var c in new[] { "car:getaway", "car:official", "car:rivals" }) cars[c] = new CarTrack { Id = c };
        Track T(string id, string label, string side, string cls, int step = 1, string detail = "")
        {
            if (tracks.TryGetValue(id, out var t)) return t;
            t = new Track { Id = id, Label = label, Side = side, Cls = cls, Step = step, Detail = detail }; tracks[id] = t; order.Add(t); return t;
        }
        int oi = 0, last = 0;
        for (int t = 0; t <= until; t++)
        {
            if (t > 0) g.RunTo(t);
            if (g.Over && g.Tick != t) break;
            while (oi < list.Count && list[oi].Tick == t) g.Issue(list[oi++]);
            last = t;
            Sample(g, t, T, crowdStep);
            foreach (var s in g.ShotsThisTick)
            {
                shots.Arr().Num(s.Tick).Str(s.By).Str(s.Target).Num(s.From.X).Num(s.From.Y).Num(s.At.X).Num(s.At.Y).Num(s.Hit ? 1 : 0).Num(s.Damage).Num(s.Down ? 1 : 0).Num(s.Covered ? 1 : s.Covering ? 2 : 0).EndArr();
            }
            string h = string.Join(",", g.Alert, g.Trail, g.Cash, g.Standing, g.Gunfire, g.FightBranch);
            if (hud.Count == 0 || hud[^1].v != h) hud.Add((t, h));
            string win = string.Join(" ", g.Windows().Where(x => x.Kind != "seal" || x.Until < 100000).Select(x => $"{x.Kind}:{x.Thing}:{x.Until}"));
            if (windows.Count == 0 || windows[^1].v != win) windows.Add((t, win));
            SampleCars(g, t, cars);
            if (fans && t % 5 == 0) foreach (var id in g.WatcherIds())
            {
                var f = g.ConeFan(id, false);
                string v = f == null || f.Points.Length == 0 ? "" : (f.Live ? "1" : "0") + ";" + string.Join(",", f.Points.Select(p => $"{p.X / 10},{p.Y / 10}"));
                if (!fanKeys.TryGetValue(id, out var fk)) fanKeys[id] = fk = (f?.Kind ?? "", new List<(int, string)>());
                if (fk.kind == "" && f != null) fanKeys[id] = fk = (f.Kind, fk.keys);
                if (fk.keys.Count == 0 ? v != "" : fk.keys[^1].v != v) fk.keys.Add((t, v));
            }
            if (g.Over) break;
        }
        g.Finish();
        // the untouched game: reads changed nothing, or this exits 1
        var clean = Engine.Play(inputs, script.Start, script.Orders, script.Until);
        bool same = BatchCli.Same(g, clean) && g.Result == clean.Result && g.FailCode == clean.FailCode && g.Tick == clean.Tick;
        bool recordOk = script.Expect == null || script.Verify(g, inputs.Hash).Length == 0;

        var w = new JWriter().Obj().Prop("schema", "suborned.simdump/1").Prop("engine", Engine.Version).Prop("script", System.IO.Path.GetFileName(path)).Prop("hz", Game.Hz).Key("seed").Null().Prop("carLenCm", g.R.CarLenCm).Prop("carWCm", g.R.CarWCm).Prop("carBlastCm", g.R.CarBlastCm);
        w.Prop("inputsHash", Hash.Hex(inputs.Hash)).Prop("startHash", Hash.Hex(g.StartHash)).Prop("finalHash", Hash.Hex(g.Checkpoints[^1].hash)).Prop("cleanFinalHash", Hash.Hex(clean.Checkpoints[^1].hash)).Prop("readsChangedNothing", same);
        w.Prop("outcome", g.Result.ToString().ToLowerInvariant()).Prop("fail", g.FailCode).Prop("failText", g.FailText).Prop("tick", g.Tick).Prop("lastSample", last).Prop("until", until);
        if (g.Cause != null) w.Key("cause").Raw(g.Cause.Json());
        w.OpenArr("entities");
        foreach (var t in order)
        {
            w.Obj().Prop("id", t.Id).Prop("label", t.Label).Prop("side", t.Side).Prop("cls", t.Cls).Prop("detail", t.Detail).Prop("step", t.Step).Prop("hpMax", t.HpMax);
            w.OpenArr("spans"); foreach (var s in t.Spans) Ints(w, s); w.EndArr();
            w.OpenArr("st"); foreach (var (at, fl, tx) in t.St) w.Arr().Num(at).Num(fl).Str(tx).EndArr(); w.EndArr();
            w.Key("hp"); Ints(w, t.Hp.SelectMany(x => new[] { x.t, x.hp }));
            w.Key("face"); Ints(w, t.Face.SelectMany(x => new[] { x.t, x.deg }));
            w.EndObj();
        }
        w.EndArr().OpenArr("cars");
        foreach (var c in cars.Values)
        {
            w.Obj().Prop("id", c.Id).OpenArr("keys"); foreach (var k in c.Keys) Ints(w, k); w.EndArr();
            w.OpenArr("st"); foreach (var (at, st, hp) in c.St) w.Arr().Num(at).Str(st).Num(hp).EndArr(); w.EndArr().EndObj();
        }
        w.EndArr().Key("shots").Raw(shots.EndArr().ToString());
        w.OpenArr("hud"); foreach (var (at, v) in hud) w.Arr().Num(at).Str(v).EndArr(); w.EndArr();
        w.OpenArr("windows"); foreach (var (at, v) in windows) w.Arr().Num(at).Str(v).EndArr(); w.EndArr();
        w.OpenArr("fans"); foreach (var kv in fanKeys) { w.Obj().Prop("id", kv.Key).Prop("kind", kv.Value.kind).OpenArr("keys"); foreach (var (at, v) in kv.Value.keys) w.Arr().Num(at).Str(v).EndArr(); w.EndArr().EndObj(); } w.EndArr();
        w.OpenArr("cameras"); foreach (var c in g.S.Cameras) w.Obj().Prop("id", c.Id).Key("at").Raw($"[{c.At.X},{c.At.Y}]").EndObj(); w.EndArr();
        w.OpenArr("log"); foreach (var l in g.Log) w.Arr().Num(l.Tick).Str(l.Code).Str(l.Text).EndArr(); w.EndArr();
        w.OpenArr("orders"); foreach (var (o, ok, code, why) in g.Orders) w.Arr().Num(o.Tick).Raw(o.ToString()).Bool(ok).Str(code ?? "").Str(why ?? "").EndArr(); w.EndArr();
        w.OpenArr("ledger"); foreach (var (t, what, d, now, cause) in g.Ledger) w.Arr().Num(t).Str(what).Num(d).Num(now).Str(cause).EndArr(); w.EndArr();
        w.OpenArr("checkpoints"); foreach (var (t, h) in g.Checkpoints) w.Arr().Num(t).Str(Hash.Hex(h)).EndArr(); w.EndArr();
        w.EndObj();
        var dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(outPath)); if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(outPath, w.ToString());
        Console.WriteLine($"{System.IO.Path.GetFileName(path)}: {g.Result} {g.FailCode} t{g.Tick} final {Hash.Hex(g.Checkpoints[^1].hash)} clean {Hash.Hex(clean.Checkpoints[^1].hash)} {(same ? "reads changed nothing" : "READS CHANGED THE RUN")}");
        return same && recordOk ? 0 : 1;
    }

    const int Down = 1, Drawn = 2, Running = 4, Dead = 8, Turned = 16, Firing = 32, Moving = 64;

    static int GunFlags(Gun? g) => g == null ? 0 : (g.Down ? Down : 0) | (g.Drawn ? Drawn : 0);
    static string GunText(Gun? g)
    {
        if (g == null) return "";
        var s = new List<string>();
        if (g.St.Length > 0 && g.St != "routine") s.Add(g.St);
        if (g.Order.Length > 0) s.Add("firing at " + g.Order); else if (g.Drawn && g.Target.Length > 0) s.Add("aiming at " + g.Target); else if (g.Drawn) s.Add("weapon drawn");
        return string.Join(" · ", s);
    }
    static string Join(params string[] xs) => string.Join(" · ", xs.Where(x => x.Length > 0));
    static string Weapon(Gun? g) => g?.W == null ? "unarmed" : $"{g.W.Id} dmg {g.W.Damage} every {g.W.Every}t reach {g.W.MaxCm / 100} m";

    static void Sample(Game g, int t, Func<string, string, string, string, int, string, Track> T, int crowdStep)
    {
        foreach (var op in g.Ops)
        {
            var id = Op.Name(op.Id); var tr = T(id, id[0..1].ToUpperInvariant() + id[1..], "cell", id, 1, Weapon(op.Gun)); tr.HpMax = op.Gun.Max;
            int fl = GunFlags(op.Gun) | (op.Running && op.Walk != null ? Running : 0);
            string doing = op.Down ? "down" : op.Inside ? "inside the Office" : op.InCar ? "in the car" : op.Busy != null ? $"{op.Busy.Do} {op.Busy.Target}".Trim() + $" until {Cl(op.BusyEnd)}" : op.Act != null ? $"walking in to {op.Act.Do} {op.Act.Target}".Trim() : op.Walk != null ? (op.Running ? "running" : "walking") : "idle";
            string carry = op.CarriedBy.HasValue ? "carried by " + Op.Name(op.CarriedBy.Value) : op.Carrying.HasValue ? "carrying " + Op.Name(op.Carrying.Value) : op.CarryBody.Length > 0 ? "carrying " + op.CarryBody : "";
            Pt? p = op.Inside || op.InCar ? null : op.Pos;
            tr.At(t, p, fl, Join(doing, carry, GunText(op.Gun)), op.Gun.Health, null);
        }
        foreach (var per in g.People)
        {
            string role = per.Role, id = role, side = role == "councillor" ? "vale" : role == "guard" ? "guard" : "named", label = per.Name.Length > 0 ? per.Name : role;
            var tr = T(id, label, side, role, 1, Join(per.Job, per.Temperament, per.Need.Length > 0 ? "need: " + per.Need : "", per.Secret.Length > 0 && per.Secret != "none" ? "secret: " + per.Secret : ""));
            var gun = g.GunOf(id); if (gun != null) { tr.HpMax = gun.Max; if (tr.Detail.IndexOf("dmg", StringComparison.Ordinal) < 0 && gun.W != null) tr.Detail = Join(tr.Detail, Weapon(gun)); }
            var body = g.BodyOf(per); Pt? p; string doing = "";
            int fl = GunFlags(gun) | (per.Turned ? Turned : 0) | (per.Down ? Down : 0);
            if (body != null) { p = body.Open ? g.BodyPos(body) : null; doing = (body.Dead ? "dead" : body.Restrained ? "restrained" : "knocked out") + (body.Carrier.HasValue ? ", carried" : "") + (body.Open ? "" : " (stashed)"); fl |= Down | (body.Dead ? Dead : 0); }
            else
            {
                p = g.PosOf(id);
                if (role == "councillor") { doing = "Vale: " + g.Vale; if (g.Vale == ValeAt.InCar || g.Vale == ValeAt.Car) p = null; }
                else if (g.PersonPose(per) is { } pp && pp.phase.Length > 0) doing = pp.phase;
                else if (role == "guard" && g.GuardPose() is { } gp) doing = gp.phase;
            }
            if (!p.HasValue && gun != null && gun.Down && body == null) p = tr.Last;
            Pt? face = null; try { face = g.FacingNow(id); } catch (Exception) { } // the host guards this read the same way (HostViews)
            tr.At(t, p, fl, Join(doing, per.Turned ? "turned" : "", GunText(gun)), gun?.Health ?? -1, face);
        }
        Hostile(g, t, "patrol", "Patrol", "guard", "patrol", T, g.PatrolPose()?.facing);
        foreach (var sg in g.SecGuns) Hostile(g, t, sg.Id, sg.Id, "police", "officer", T, null);
        foreach (var m in g.Crew)
        {
            var tr = T(m.Id, m.Role.Length > 0 ? $"Rival {m.Role}" : m.Id, "rival", m.Role, 1, Weapon(m.Gun)); tr.HpMax = m.Gun.Max;
            var body = g.BodyOf(m.P); Pt? p; string doing = m.Mode.ToString().ToLowerInvariant();
            int fl = GunFlags(m.Gun);
            if (body != null) { p = body.Open ? g.BodyPos(body) : null; doing = body.Dead ? "dead" : "knocked out"; fl |= Down | (body.Dead ? Dead : 0); }
            else
            {
                p = m.Mode == CrewMode.Gone || m.Mode == CrewMode.Aboard ? null : g.PosOf(m.Id);
                if (m.Fight && m.H != null) { doing = m.H.Why; if (!m.H.Present || m.Gun.St == "gone") p = null; else if (!m.H.Down) p = m.H.Pos(t); }
                if (!p.HasValue && m.Gun.Down) p = tr.Last;
            }
            tr.At(t, p, fl, Join(m.Gun.Down ? "down" : doing, m.Goal.Length > 0 && !m.Fight && !m.Gun.Down ? "to " + m.Goal : "", GunText(m.Gun)), m.Gun.Health, null);
        }
        foreach (var h in g.Hostiles) if (!h.Id.StartsWith("rival", StringComparison.Ordinal) && g.CrewOf(h.Id) == null) Hostile(g, t, h.Id, h.Id, h.Id.StartsWith("security", StringComparison.Ordinal) || h.Id.StartsWith("officer", StringComparison.Ordinal) ? "police" : "guard", "officer", T, null);
        foreach (var u in g.Units)
        {
            if (u.Legacy || u.Patrol) continue;
            var tr = T(u.Id, (u.Robot ? "Robot " : "Unit ") + u.Id, u.Robot ? "robot" : "police", u.Robot ? "robot" : "officer", 1, Weapon(u.Gun)); tr.HpMax = u.Gun.Max;
            Pt? p = u.OnStreet(t) ? g.UnitPose(u)?.pos : null; if (!p.HasValue && u.Gun.Down) p = tr.Last;
            tr.At(t, p, GunFlags(u.Gun), Join(u.Gun.Down ? "down" : u.St, GunText(u.Gun)), u.Gun.Health, g.UnitPose(u)?.facing);
        }
        foreach (var w in g.Walkers)
        {
            var per = w.P; var tr = T(per.Id, per.Name.Length > 0 ? per.Name : per.Id, "civ", per.Job, crowdStep, Join(per.Job, per.Temperament, per.Need.Length > 0 ? "need: " + per.Need : "")); tr.HpMax = g.R.HealthCivilian;
            var body = g.BodyOf(per); Pt? p; int fl = per.Turned ? Turned : 0; string doing = w.St.Length > 0 ? w.St : "routine";
            if (body != null) { p = body.Open ? g.BodyPos(body) : null; doing = body.Dead ? "dead" : "knocked out"; fl |= Down | (body.Dead ? Dead : 0); }
            else { p = g.CrowdPos(w); if (w.St == "fleeing") fl |= Running; if (per.Down) { fl |= Down; doing = "down"; p = tr.Last; } }
            tr.At(t, p, fl, doing, -1, null);
        }
    }

    static void Hostile(Game g, int t, string id, string label, string side, string cls, Func<string, string, string, string, int, string, Track> T, Pt? face)
    {
        var gun = g.GunOf(id); var tr = T(id, label, side, cls, 1, Weapon(gun)); tr.HpMax = gun?.Max ?? 0;
        Pt? p = gun != null && gun.St == "gone" ? null : g.PosOf(id); if (!p.HasValue && gun != null && gun.Down) p = tr.Last;
        var h = g.Hostiles.Find(x => x.Id == id);
        tr.At(t, p, GunFlags(gun), Join(gun != null && gun.Down ? "down" : h != null && h.Present ? h.Why : "", GunText(gun)), gun?.Health ?? -1, face);
    }

    static void SampleCars(Game g, int t, Dictionary<string, CarTrack> cars)
    {
        var standing = g.StandingCars();
        void put(string id, Pt? at, Pt? head)
        {
            var c = cars[id]; var ct = g.CarOf(id); string st = ct == null ? "ok" : ct.Blown ? "blown" : ct.BurningAt >= 0 ? "burning" : ct.DisabledAt >= 0 ? "disabled" : "ok"; int hp = ct?.Gun.Health ?? -1;
            if (c.St.Count == 0 || c.St[^1].st != st || c.St[^1].hp != hp) c.St.Add((t, st, hp));
            var k = at.HasValue ? new List<int> { t, at.Value.X, at.Value.Y, head?.X ?? 0, head?.Y ?? 100 } : new List<int> { t };
            if (c.Keys.Count == 0 ? k.Count > 1 : !c.Keys[^1].Skip(1).SequenceEqual(k.Skip(1))) c.Keys.Add(k);
        }
        var sg = standing.FindIndex(x => x.Id == "car:getaway");
        if (g.GetawayDriving) put("car:getaway", g.GetawayCarPos(), g.GetawayHeading()); else if (sg >= 0) put("car:getaway", standing[sg].At, standing[sg].Heading); else put("car:getaway", null, null);
        put("car:official", g.CarPos(), g.CarHeading());
        var rp = g.RivalCarPose(); if (rp.HasValue) put("car:rivals", rp.Value.pos, rp.Value.heading); else put("car:rivals", null, null);
    }
}
