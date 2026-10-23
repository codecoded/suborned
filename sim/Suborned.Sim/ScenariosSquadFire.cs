// Engine 0.26.0: the squad fire order, agent by agent (Sky playing 0.2.65: "I had the whole squad selected but some of my agents weren't
// firing at the target e.g. the Scout"; briefs/041/next-batch.md, item "squad fire"). The host builds a squad fire order (HostHold.Shooters):
// every selected armed agent who is drawn, else all of them; so with any agent drawn, a holstered one gets no order and, holstered, never
// fires (weapons free never draws). The engine takes a fire order from a holstered agent (it draws), out of reach (it walks in) and with
// no line (it walks to one). FireQuery says, per agent, what Issue would answer, so the host can give the order to everyone who takes it
// and show why on the plate of everyone who doesn't. Ids avoid the S prefix (the 28).
//    QF-HOST     the host's rule today: Enforcer and Fixer drawn, Scout and Tech holstered, all four selected: only the drawn two get the
//                order, and the Scout and Tech never fire
//    QF-ALL      the proposed rule: the order to all four: the holstered Scout draws and fires, the Tech out of pistol range walks in and
//                fires; FireQuery agrees with Issue for each, and says which walk in
//    QF-LINE     a holstered agent with no line inside its ring: taken, it walks to a line and fires
//    QF-CANT     Arms 1: the Fixer and the Tech carry no weapon: FireQuery and Issue both say no-weapon; the holstered Scout still draws
//    QF-FOCUS    the focus click in a fight: a focus out of reach is taken but never walks in, so the Scout 20 m off never fires and the
//                lock lapses (tuning.md §14 as built); a plain fire order in its place walks in
//    QF-BUSY     the Handover: a Scout busy watching answers agent-busy, from FireQuery as from Issue (its plate's reason)
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>HostHold.Shooters (Unity, build 0.2.65) for the map (not driven, no portrait locks): every selected armed agent (a weapon,
        /// not down, not inside), never the Enforcer walking Vale; the drawn ones if any is drawn, else all.</summary>
        static List<string> QfHostShooters(Run r, IEnumerable<string> selected)
        {
            var g = r.G; var able = new List<string>();
            foreach (var n in selected)
            {
                var o = r.Op(n);
                if (o.Gun.W != null && !o.Down && !o.Inside && !(o.Id == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer)) able.Add(n);
            }
            var drawn = able.Where(n => { var gg = r.Op(n).Gun; return gg.Drawn && gg.HolsterAt < 0; }).ToList();
            return drawn.Count > 0 ? drawn : able;
        }

        /// <summary>Ask FireQuery, then give the order: the two answers agree (taken, code, words). Returns Issue's answer and FireQuery's walk.</summary>
        static (Answer a, bool walk) QfGive(Run r, string op, string t, bool focus = false, bool force = false)
        {
            var g = r.G; var q = g.FireQuery(Sim.Op.Parse(op), t, force, focus, out bool walk);
            var a = g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = t, Focus = focus, Force = force });
            r.Is(q.ok == a.ok && q.code == a.code && q.why == a.why && q.m == a.m && q.tick == a.tick, $"{op}: FireQuery agrees with Issue ({(a.ok ? "taken" : a.code)}{(q.ok == a.ok && q.code == a.code ? "" : $"; query said {(q.ok ? "taken" : q.code)}")})");
            if (a.ok && !focus) r.Is(walk == r.Op(op).CloseWalk, $"{op}: FireQuery's walk ({walk}) is the walk in Issue started");
            return (a, walk);
        }

        /// <summary>The first shot `op` fires on `t` from `from`, or -1.</summary>
        static int QfFirst(Run r, string op, string t, int from) { foreach (var s in r.Shots()) if (s.by == op && s.target == t && s.tick >= from) return s.tick; return -1; }

        /// <summary>The four on posts around the range's car slot 1 (spawned): Enforcer, Fixer and Scout 10 m off, the Tech `techCm` off (22 m:
        /// beyond the 16 m sidearm, inside the 30 m carbine), all on the side the cell starts. Enforcer and Fixer drawn. Returns the car's id.</summary>
        static string QfCarPosts(Run r, int techCm = 2200)
        {
            var g = r.G;
            r.Is(SpawnCar(r, "car", 1).ok, "a car spawned on slot 1");
            var car = g.S.At("spawn:car:1"); var from = r.Op("enforcer").Pos;
            double deg = Math.Atan2(from.Y - car.Y, from.X - car.X) * 180 / Math.PI;
            var posts = new (string op, double d, int cm)[] { ("enforcer", -12, 1000), ("fixer", -4, 1000), ("scout", 4, 1000), ("tech", 12, techCm) };
            int at = 0;
            foreach (var p in posts)
            {
                // the first ray from the post's angle outward (2° steps, either side, all the way round) whose point is free, reachable and at its distance
                Pt to = Ray(r, car, deg + p.d, p.cm);
                for (int k = 0; k < 180; k++)
                {
                    var q = Ray(r, car, deg + p.d + (k % 2 == 0 ? 1 : -1) * 2 * ((k + 1) / 2), p.cm);
                    if (g.S.Grid.Free(q) && Math.Abs(G.Isqrt(G.D2(q, car)) - p.cm) <= 100 && g.S.Grid.Route(r.Op(p.op).Pos, q) != null) { to = q; break; }
                }
                at = Math.Max(at, r.SendTo(p.op, to));
            }
            r.To(at);
            foreach (var p in posts) { long d = G.Isqrt(G.D2(r.Op(p.op).Pos, car)); r.Is(Math.Abs(d - p.cm) <= 150, $"{p.op} stands {d / 100.0:0.0} m from the car"); }
            r.Do("enforcer", "draw"); r.Do("fixer", "draw"); r.Wait(Game.T(2));
            r.Is(r.Op("enforcer").Gun.Drawn && r.Op("fixer").Gun.Drawn && !r.Op("scout").Gun.Drawn && !r.Op("tech").Gun.Drawn, "Enforcer and Fixer drawn; Scout and Tech holstered");
            return "car:range-1";
        }

        static void SquadFireRange()
        {
            AddRange("QF-HOST", "today", "open", "Sky playing 0.2.65: the host's squad fire order goes only to the drawn agents, so the holstered Scout and Tech never fire", r =>
            {
                var g = r.G; var car = QfCarPosts(r);
                var who = QfHostShooters(r, Cell4R);
                r.Eq(string.Join(",", who), "enforcer,fixer", "the host's rule picks only the drawn agents of the four selected");
                int t0 = g.Tick; foreach (var n in who) r.Is(QfGive(r, n, car).a.ok, $"{n}: its order taken");
                r.Wait(Game.T(8));
                foreach (var n in Cell4R) r.Is(true, $"{n}: first shot {(QfFirst(r, n, car, t0) is int s && s >= 0 ? $"{(s - t0) / (double)Game.Hz:0.0} s" : "none")}, {r.Shots().Count(x => x.by == n && x.tick >= t0)} shots");
                r.Is(QfFirst(r, "enforcer", car, t0) >= 0 && QfFirst(r, "fixer", car, t0) >= 0, "the drawn Enforcer and Fixer fire");
                r.Is(QfFirst(r, "scout", car, t0) < 0 && QfFirst(r, "tech", car, t0) < 0, "the holstered Scout and Tech never fire: they got no order");
                r.Is(r.Op("scout").Gun.Order.Length == 0 && !r.Op("scout").Gun.Drawn, "the Scout stays holstered with no order");
            });

            AddRange("QF-ALL", "today", "open", "the proposed rule: every selected agent who can fire gets the order; the holstered draw, the one out of range walks in", r =>
            {
                var g = r.G; var car = QfCarPosts(r);
                int t0 = g.Tick; var walk = new Dictionary<string, bool>();
                foreach (var n in Cell4R) { var (a, w) = QfGive(r, n, car); r.Is(a.ok, $"{n}: taken"); walk[n] = w; }
                r.Is(!walk["enforcer"] && !walk["fixer"] && !walk["scout"], "the three at 10 m fire from where they stand");
                r.Is(walk["tech"] && r.Op("tech").CloseWalk, "the Tech beyond its 16 m sidearm walks in");
                r.Is(r.Until(() => Cell4R.All(n => QfFirst(r, n, car, t0) >= 0), Game.T(15)), "all four fire");
                foreach (var n in Cell4R) r.Is(true, $"{n}: first shot {(QfFirst(r, n, car, t0) - t0) / (double)Game.Hz:0.0} s");
                int sd = QfFirst(r, "scout", car, t0) - t0;
                r.Is(sd >= g.R.DrawTicks, $"the Scout draws first ({sd / (double)Game.Hz:0.0} s, draw {g.R.DrawTicks / (double)Game.Hz:0.0} s)");
            });

            AddRange("QF-LINE", "today", "open", "a holstered agent with no line inside its ring: the fire order is taken, it walks to a line and fires", r =>
            {
                var g = r.G;
                r.Is(SpawnCar(r, "car", 1).ok, "a car spawned on slot 1"); const string car = "car:range-1";
                var at = g.PosOf(car)!.Value; var t = r.Op("tech");
                // a free point inside the sidearm's 12 m with no line to the car (behind cover), reachable on the grid
                Pt? post = null; long best = long.MaxValue;
                for (int dx = -1200; dx <= 1200; dx += 50) for (int dy = -1200; dy <= 1200; dy += 50)
                {
                    var q = new Pt(at.X + dx, at.Y + dy); long d = G.D2(q, at);
                    if (d < 400L * 400 || d > 1200L * 1200 || !g.S.Grid.Free(q) || g.S.Sight(q, at, g.R.Eye)) continue;
                    long dt = G.D2(q, t.Pos); if (dt < best && g.S.Grid.Route(t.Pos, q) != null) { best = dt; post = q; }
                }
                if (!r.Is(post.HasValue, "a post with no line to the car within 12 m")) return;
                r.To(r.SendTo("tech", post!.Value));
                r.Is(!g.S.Sight(t.Pos, at, g.R.Eye) && G.Within(t.Pos, at, t.Gun.W!.MaxCm), $"the Tech stands {G.Isqrt(G.D2(t.Pos, at)) / 100.0:0.0} m off with no line, holstered");
                int t0 = g.Tick; var (a, w) = QfGive(r, "tech", car);
                r.Is(a.ok && w, "taken: it walks to a line");
                r.Is(r.Until(() => QfFirst(r, "tech", car, t0) >= 0, Game.T(15)), $"it fires ({(QfFirst(r, "tech", car, t0) - t0) / (double)Game.Hz:0.0} s)");
            });

            var cant = AddRange("QF-CANT", "today", "open", "Arms 1: the Fixer and the Tech carry no weapon: the order's answer says so for each (no-weapon); the holstered Scout draws", r =>
            {
                var g = r.G;
                r.Is(r.Op("fixer").Gun.W == null && r.Op("tech").Gun.W == null && r.Op("scout").Gun.W != null, "Arms 1: the Scout's sidearm only (and the Enforcer's carbine)");
                r.Is(SpawnCar(r, "car", 1).ok, "a car spawned on slot 1"); const string car = "car:range-1";
                var at = g.S.At("spawn:car:1"); var from = r.Op("enforcer").Pos; double deg = Math.Atan2(from.Y - at.Y, from.X - at.X) * 180 / Math.PI;
                int arrive = 0; foreach (var (n, k) in Cell4R.Select((n, k) => (n, k))) arrive = Math.Max(arrive, r.SendTo(n, Ray(r, at, deg - 12 + 8 * k, 1000)));
                r.To(arrive); r.Do("enforcer", "draw"); r.Wait(Game.T(2));
                r.Eq(string.Join(",", QfHostShooters(r, Cell4R)), "enforcer", "the host's rule: only the drawn Enforcer (the holstered Scout is skipped, the unarmed are never asked)");
                int t0 = g.Tick;
                foreach (var n in Cell4R)
                {
                    var (a, _) = QfGive(r, n, car);
                    if (n is "fixer" or "tech") r.Is(!a.ok && a.code == "no-weapon", $"{n}: no-weapon");
                    else r.Is(a.ok, $"{n}: taken");
                }
                r.Is(r.Until(() => QfFirst(r, "scout", car, t0) >= 0, Game.T(10)), "the Scout draws and fires");
            });
            cant.Start.Arms = 1;

            AddRange("QF-FOCUS", "today", "lane", "the focus click in a fight: a focus out of reach is taken and never walks in, so the Scout 20 m off never fires and its lock lapses; a fire order walks in (the others hold, so officer-2 stays up)", r =>
            {
                var g = r.G; var h = g.RangeSlot("near", 1);
                var posts = new (string op, double deg, int cm)[] { ("enforcer", -10, 1200), ("fixer", -4, 1200), ("scout", 2, 2000), ("tech", 8, 1200) };
                int at = 0; foreach (var p in posts) at = Math.Max(at, r.SendTo(p.op, Ray(r, h, p.deg, p.cm)));
                r.To(at); foreach (var p in posts) r.Do(p.op, "draw"); r.Wait(Game.T(2));
                foreach (var n in new[] { "enforcer", "fixer", "tech" }) Hold(r, n); // only the orders fire, so officer-2 stays up past the lapse
                Spawned(r, "police", "near", 2);
                r.Is(FireOn(r, "enforcer", "officer-1").ok, "the opening shot's order");
                r.Is(r.Until(() => g.FreeFightOn, Game.T(6)), "a fight is on");
                var s = r.Op("scout"); var o2 = H(r, "officer-2");
                long d = G.Isqrt(G.D2(s.Pos, o2.Pos(g.Tick)));
                r.Is(d > s.Gun.W!.MaxCm, $"the Scout is {d / 100.0:0.0} m from officer-2, beyond its {s.Gun.W.MaxCm / 100} m sidearm");
                int t0 = g.Tick;
                r.Is(QfGive(r, "scout", "officer-2", focus: true).a.ok, "scout: the focus is taken out of reach");
                r.Is(!s.CloseWalk && s.Walk == null, "the Scout does not walk in (a focus never does)");
                r.Is(r.Until(() => s.Gun.Order != "officer-2" || s.Down, Game.T(g.R.LineLapseS + 2)), $"the Scout's lock ends ({Report.Clock(g.Tick)})");
                r.Is(QfFirst(r, "scout", "officer-2", t0) < 0, "and it never fired on officer-2");
                r.Is(g.Log.Exists(l => l.Code == "fire.lapses" && l.Tick > t0 && l.Text.StartsWith("scout", StringComparison.Ordinal)), "its lock lapsed (fire.lapses)");
                if (!s.Down && !H(r, "officer-2").Gun.Down)
                {
                    var (a, w) = QfGive(r, "scout", "officer-2");
                    r.Is(a.ok && w && s.CloseWalk, "a plain fire order in its place walks in");
                }
            });
        }

        static void SquadFireCases()
        {
            Add("QF-BUSY", "—", "a Scout busy watching answers agent-busy to a fire order, from FireQuery as from Issue (the plate's reason)", r =>
            {
                var g = r.G; var o = r.Op(Scout);
                r.To(r.Send(Scout, "beside-inez"));
                r.Is(r.Do(Scout, "watch", Inez), "the Scout watches Inez");
                r.Is(r.Until(() => o.Busy != null, Game.T(10)), "and is busy");
                var (a, _) = QfGive(r, Scout, "guard");
                r.Is(!a.ok && a.code == "agent-busy", $"the fire order: agent-busy ({a.code})");
                r.Is(!o.Gun.Drawn && o.Busy != null, "nothing changed: holstered, still watching");
            });
        }
    }
}
