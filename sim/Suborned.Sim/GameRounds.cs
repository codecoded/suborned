// The living café (brief 022, Pulse; Sky 2026-10-18: "have the waitress also walk around the cafe tables, and when guard comes, they stand
// together"). The round is baked at build time (tools/routines/rounds.ts → sim/data/handover.rounds.json); the engine reads it and plays it on a
// round clock that stops while she is held (rulebook rounds). This is tools/routines/rounds.ts poseAt in integers, piece for piece:
//  · free: the round clock runs with the mission; she rests at her post and goes out to a table (to table, serving, from table)
//  · held: an order of a verb in rounds.holds taken on her, from the tick it is taken to the tick it lands, plus rounds.linger; she stands
//    where she is, turned to the operative; an order taken inside the linger is the same hold, longer. Watch holds nothing.
//  · called: the summons; she walks back to her post the way she came, waits, stands together with him from his arrival to his turn plus
//    rounds.part, and the round resumes at the start of its next rest
//  · ended: at the first of rounds.ends she walks back and stands her post for the rest of the mission
// Every reader of her position (reach, witnesses, sight, the summons' lover-within) reads PersonPos, which is the round's.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>The baked rounds (suborned.sim.rounds/1), read strictly: unknown = reject.</summary>
    public sealed class RoundsData
    {
        public sealed class Exc { public string Table = ""; public Pt At, Face; public int Start, Out, Serve, Back; public Path Path = null!; }
        public sealed class Server { public string Id = "", Role = "", Post = ""; public Pt At, Face; public int From, To, Origin; public readonly List<Exc> Exc = new List<Exc>(); }
        public readonly Dictionary<string, Server> Servers = new Dictionary<string, Server>(StringComparer.Ordinal);
        public int LingerS, CloseCm, ClearCm, PartS, StayS, PaceCmPerS;
        public Pt GuardStopAt, GuardFace; public int GuardWalkCm, GuardStopWalkCm;
        /// <summary>Where the summoned partner may stand, in the file's order (ring of CloseCm round her post, free ground, most lateral first, her left first): the engine picks, never computes.</summary>
        public readonly List<Pt> GuardStops = new List<Pt>();
        /// <summary>The reference poses (tick, x, y, facing x, facing y, phase) by server and kind: free, held, called, ended (tests read them).</summary>
        public readonly Dictionary<string, Dictionary<string, List<(int tick, Pt pos, Pt face, string phase)>>> Vectors = new Dictionary<string, Dictionary<string, List<(int, Pt, Pt, string)>>>(StringComparer.Ordinal);

        public static RoundsData Parse(string json, Rules r, Site site)
        {
            var n = JNode.Parse(json, "rounds"); var d = new RoundsData();
            n.Only("schema", "from", "frame", "rules", "servers", "meetings", "guard", "vectors");
            if (n["schema"].Str() != "suborned.sim.rounds/1") throw new FormatException("rounds schema is not suborned.sim.rounds/1");
            var ru = n["rules"]; ru.Only("file", "sha256", "paceCmPerS", "lingerS", "closeCm", "clearCm", "partS", "settleS", "stayS", "chanceStayS");
            d.LingerS = ru["lingerS"].Int(); d.CloseCm = ru["closeCm"].Int(); d.ClearCm = ru["clearCm"].Int(); d.PartS = ru["partS"].Int(); d.StayS = ru["stayS"].Int(); d.PaceCmPerS = ru["paceCmPerS"].Int();
            // the baked numbers are the rulebook's (Motive confirms them there): a file baked from other numbers is rejected
            if (d.LingerS != r.RoundsLingerS || d.CloseCm != r.PartnersCloseCm || d.ClearCm != r.PartnersClearCm || d.PartS != r.PartnersPartS || d.StayS != r.SummonStayS || d.PaceCmPerS != r.CellSpeed)
                throw new FormatException("rounds.rules (lingerS, closeCm, clearCm, partS, stayS, paceCmPerS) are not the rulebook's rounds.linger, rounds.close, rounds.clear, rounds.part, summon.stay and cell.speed: rebake (bun run rounds)");
            if (n["meetings"].Items.Count > 0) throw new FormatException("rounds.meetings: chance meetings are not played yet (brief 022 §2.6): unknown = reject");
            foreach (var kv in n["servers"].Entries)
            {
                var v = kv.Value; v.Only("name", "post", "at", "face", "spans");
                var role = site.Cast.Find(c => c.Id == kv.Key)?.Role ?? throw new FormatException($"rounds.servers.{kv.Key} is not cast: only the cast is played by the engine (unknown = reject)");
                var sv = new Server { Id = kv.Key, Role = role, Post = v["post"].Str(), At = Pt.Of(v["at"]), Face = Pt.Of(v["face"]) };
                var spans = v["spans"].Items; if (spans.Count != 1) throw new FormatException($"rounds.servers.{kv.Key}: one span is played (brief 022), not {spans.Count}");
                var sp = spans[0]; sp.Only("place", "from", "to", "origin", "excursions");
                if (sp["place"].Str() != sv.Post) throw new FormatException($"rounds.servers.{kv.Key}: the span's place is the post");
                sv.From = sp["from"].Int(); sv.To = sp["to"].Int(); sv.Origin = sp["origin"].Int();
                if (sv.Origin != Math.Max(0, sv.From) || sv.To <= Math.Max(0, sv.From)) throw new FormatException($"rounds.servers.{kv.Key}: origin is max(0, from) and the span is not empty");
                int last = -1;
                foreach (var e in sp["excursions"].Items)
                {
                    e.Only("table", "at", "face", "start", "out", "serve", "back", "m", "path");
                    var pts = new List<Pt>(); foreach (var q in e["path"].Items) pts.Add(Pt.Of(q));
                    var x = new Exc { Table = e["table"].Str(), At = Pt.Of(e["at"]), Face = Pt.Of(e["face"]), Start = e["start"].Int(), Out = e["out"].Int(), Serve = e["serve"].Int(), Back = e["back"].Int(), Path = new Path(pts.ToArray()) };
                    if (pts.Count < 2 || pts[0] != sv.At || pts[pts.Count - 1] != x.At) throw new FormatException($"{e.Path}.path runs from the post to the service point");
                    if (x.Out < 1 || x.Serve < 1 || x.Back < 1 || x.Start <= last) throw new FormatException($"{e.Path}: legs are whole seconds, excursions in order");
                    last = x.Start + x.Out + x.Serve + x.Back - 1;
                    sv.Exc.Add(x);
                }
                d.Servers[sv.Id] = sv;
                var vec = new Dictionary<string, List<(int, Pt, Pt, string)>>(StringComparer.Ordinal);
                if (n["vectors"].Opt(kv.Key) is JNode vs)
                    foreach (var k in vs.Entries)
                    {
                        var rows = new List<(int, Pt, Pt, string)>();
                        foreach (var row in k.Value.Items) { var it = row.Items; rows.Add((it[0].Int(), new Pt(it[1].Int(), it[2].Int()), new Pt(it[3].Int(), it[4].Int()), it[5].Str())); }
                        vec[k.Key] = rows;
                    }
                d.Vectors[sv.Id] = vec;
            }
            var g = n["guard"]; g.Only("from", "post", "walkCm", "ringCm", "stopAt", "face", "stops");
            d.GuardStopAt = Pt.Of(g["stopAt"]); d.GuardFace = Pt.Of(g["face"]); d.GuardWalkCm = g["walkCm"].Int();
            if (g["ringCm"].Int() != d.CloseCm) throw new FormatException("rounds.guard.ringCm is rules.closeCm");
            foreach (var st in g["stops"].Items) { st.Only("at", "face", "deg", "walkCm"); if (d.GuardStops.Count == 0) d.GuardStopWalkCm = st["walkCm"].Int(); d.GuardStops.Add(Pt.Of(st["at"])); }
            if (d.GuardStops.Count > 0 && d.GuardStops[0] != d.GuardStopAt) throw new FormatException("rounds.guard.stopAt is stops[0]");
            return d;
        }
    }

    /// <summary>A server's pose: where she is, which way she faces, and what she is doing (at post, to table, serving, from table, held, to
    /// post, together).</summary>
    public struct RoundPose { public Pt Pos, Face; public string Phase; }

    /// <summary>What the mission did to a server's round, in ticks: the holds (taken, landed; the linger is added when played), the call, the end.</summary>
    public sealed class RoundEvents
    {
        public sealed class Hold { public int From, To; public Pt Face; public OpId By; }
        public readonly List<Hold> Holds = new List<Hold>();
        public int CallAt = -1, CallArrive, CallUntil; public Pt CallStop;
        public int EndAt = -1; public string EndWhy = "";
        public int Version;
    }

    public sealed partial class Game
    {
        /// <summary>Brief 022: the round is played (the mission's rounds file is loaded and the start doesn't switch it off).</summary>
        public bool RoundsOn => S.Rounds != null && (Start.Rounds ?? true);
        readonly Dictionary<string, RoundEvents> roundEv = new Dictionary<string, RoundEvents>(StringComparer.Ordinal);
        readonly Dictionary<string, (int version, List<Piece> pieces)> roundCache = new Dictionary<string, (int, List<Piece>)>(StringComparer.Ordinal);

        RoundsData.Server? ServerOf(Person p) => RoundsOn && S.Rounds!.Servers.TryGetValue(p.Id, out var sv) ? sv : null;
        RoundEvents EvOf(RoundsData.Server sv) { if (!roundEv.TryGetValue(sv.Id, out var e)) roundEv[sv.Id] = e = new RoundEvents(); return e; }
        readonly RoundEvents noEvents = new RoundEvents();
        /// <summary>The events for reading only (the HUD, PersonPos, the hash): never creates state, so a read can't change a run.</summary>
        RoundEvents EvRead(RoundsData.Server sv) => roundEv.TryGetValue(sv.Id, out var e) ? e : noEvents;

        /// <summary>Where a person stands and faces now, and their phase: a server's from her round; anyone else from PersonPos (phase "").</summary>
        public (Pt pos, Pt facing, string phase)? PersonPose(Person p)
        {
            if (p.Down) return null;
            var sv = ServerOf(p);
            if (sv == null) { var at = PersonPos(p); return at.HasValue ? (at.Value, new Pt(0, 0), "") : ((Pt, Pt, string)?)null; }
            var q = RoundPoseAt(sv, EvRead(sv), Tick); return (q.Pos, q.Face, q.Phase);
        }
        /// <summary>Where a server will be `ticks` from now if nothing more is done to her (the HUD; Preview aims there). Others: as now.</summary>
        public (Pt pos, Pt facing, string phase)? PoseAhead(Person p, int ticks)
        {
            if (p.Down) return null;
            var sv = ServerOf(p); if (sv == null) return PersonPose(p);
            var q = RoundPoseAt(sv, EvRead(sv), Tick + Math.Max(0, ticks)); return (q.Pos, q.Face, q.Phase);
        }
        /// <summary>Ticks until a server is next at her post, standing (0 if she is now); -1 if she has no round (or never, within 5 minutes).</summary>
        public int RoundNext(Person p)
        {
            var sv = ServerOf(p); if (sv == null || p.Down) return -1;
            var ev = EvRead(sv);
            for (int k = 0; k <= T(300); k++) { var q = RoundPoseAt(sv, ev, Tick + k); if (q.Phase == "at post" || q.Phase == "together") return k; }
            return -1;
        }

        /// <summary>An order of a verb in rounds.holds is taken on a server: she stands still until it lands (plus the linger), turned to the operative.</summary>
        void RoundHold(Person p, Op op, string verb, int landsAt)
        {
            var sv = ServerOf(p); if (sv == null || !R.RoundHolds.Contains(verb)) return;
            var ev = EvOf(sv); var me = RoundPoseAt(sv, ev, Tick).Pos;
            var f = op.Pos - me; if (f == new Pt(0, 0)) f = sv.Face;
            ev.Holds.Add(new RoundEvents.Hold { From = Tick, To = landsAt, Face = f, By = op.Id }); ev.Version++;
        }
        /// <summary>An operative's order ends early (cancelled, or the operative down): a hold it made ends now (its linger still follows).</summary>
        void RoundHoldCut(Op op)
        {
            foreach (var ev in roundEv.Values)
                foreach (var h in ev.Holds) if (h.By == op.Id && h.To > Tick) { h.To = Tick; ev.Version++; }
        }
        /// <summary>The summoned partner's stop (brief 022 addendum, partners.summoned.side): the first stop in the file with nobody within partners-clear-cm of it at the tick of the call
        /// (the operatives not down and not inside, the patrol, and everyone on the street by the engine's own OnTheStreet: the cast by role, the crowd when Start.Crowd;
        /// not the server and not the summoned); if every stop is taken, the one whose nearest person is farthest, the earlier on a tie; integer distances, a fixed order:
        /// the same people give the same stop. Null when the file holds no stop (the old stop: on his route, close short of her post).</summary>
        Pt? PartnerStop(Person server, Person lover)
        {
            var stops = S.Rounds!.GuardStops; if (stops.Count == 0) return null;
            var people = new List<Pt>();
            foreach (var op in Ops) if (!op.Down && !op.Inside) people.Add(op.Pos);
            if (!PatrolDown && PatrolPose() is var pp && pp.HasValue) people.Add(pp.Value.pos);
            foreach (var (who, at) in OnTheStreet()) if (who != server && who != lover) people.Add(at);
            Pt best = stops[0]; long bestD = -1;
            foreach (var st in stops)
            {
                long near = long.MaxValue; foreach (var q in people) near = Math.Min(near, G.Dist(st, q));
                if (near >= R.PartnersClearCm) return st;
                if (near > bestD) { bestD = near; best = st; }
            }
            return best;
        }
        /// <summary>The summons reaches a server (brief 022): she walks back to her post, and they stand together while he stays.</summary>
        void RoundCall(Person p, int arrive, int until, Pt stop)
        {
            var sv = ServerOf(p); if (sv == null) return;
            var ev = EvOf(sv); if (ev.CallAt >= 0) return;
            ev.CallAt = Tick; ev.CallArrive = arrive; ev.CallUntil = until; ev.CallStop = stop; ev.Version++;
        }
        /// <summary>The round ends for the rest of the mission (rounds.ends: alert-2, gunfire, body-found, final-fight): she walks back and stands her post.</summary>
        void RoundsEnd(string why)
        {
            if (!RoundsOn || !R.RoundEnds.Contains(why)) return;
            foreach (var sv in S.Rounds!.Servers.Values)
            {
                var ev = EvOf(sv); if (ev.EndAt >= 0) continue;
                ev.EndAt = Tick; ev.EndWhy = why; ev.Version++;
                Note("round.ends", $"{why}: {(PersonById(sv.Id)?.Name ?? sv.Id)} walks back to her post and stays there.");
            }
        }

        // ---------------------------------------------------------------- the pose (tools/routines/rounds.ts poseAt, in integers)

        sealed class Piece
        {
            public int From, To; public string Kind = ""; // run, fixed, walk
            public int C0; public Pt Pos, Face; public string Phase = "";
            public Path? WPath; public long StartMm; public int Dur;
        }

        static long RoundHalf(long num, long den) => den <= 0 ? 0 : (2 * num + den) / (2 * den); // Math.round(num / den) for num, den ≥ 0

        /// <summary>The nominal round, on its clock in ticks from the origin: at post, or an excursion's legs.</summary>
        static RoundPose BodyPose(RoundsData.Server sv, int tr)
        {
            foreach (var x in sv.Exc)
            {
                int t0 = x.Start * Hz, t1 = t0 + x.Out * Hz, t2 = t1 + x.Serve * Hz, t3 = t2 + x.Back * Hz;
                if (tr < t0 || tr >= t3) continue;
                if (tr < t1) { long mm = RoundHalf(x.Path.Length * (tr - t0), t1 - t0); return new RoundPose { Pos = x.Path.At(mm), Face = x.Path.Heading(mm), Phase = "to table" }; }
                if (tr < t2) return new RoundPose { Pos = x.At, Face = x.Face, Phase = "serving" };
                long m2 = RoundHalf(x.Path.Length * (t3 - tr), t3 - t2); var h = x.Path.Heading(m2);
                return new RoundPose { Pos = x.Path.At(m2), Face = new Pt(-h.X, -h.Y), Phase = "from table" };
            }
            return new RoundPose { Pos = sv.At, Face = sv.Face, Phase = "at post" };
        }
        /// <summary>The walk home from round-clock tick tr: the path, where on it she is, and how long it takes (rounds.ts walkBack).</summary>
        static (Path path, long startMm, int dur)? WalkBack(RoundsData.Server sv, int tr)
        {
            foreach (var x in sv.Exc)
            {
                int t0 = x.Start * Hz, t1 = t0 + x.Out * Hz, t2 = t1 + x.Serve * Hz, t3 = t2 + x.Back * Hz;
                if (tr < t0 || tr >= t3) continue;
                if (tr < t1) return (x.Path, RoundHalf(x.Path.Length * (tr - t0), t1 - t0), tr - t0);
                if (tr < t2) return (x.Path, x.Path.Length, t3 - t2);
                return (x.Path, RoundHalf(x.Path.Length * (t3 - tr), t3 - t2), t3 - tr);
            }
            return null;
        }
        static int Resume(RoundsData.Server sv, int tr)
        {
            foreach (var x in sv.Exc) { int t0 = x.Start * Hz, t3 = t0 + (x.Out + x.Serve + x.Back) * Hz; if (tr >= t0 && tr < t3) return t3; }
            return tr;
        }

        List<Piece> Pieces(RoundsData.Server sv, RoundEvents ev)
        {
            if (roundCache.TryGetValue(sv.Id, out var c) && c.version == ev.Version) return c.pieces;
            const int MAX = int.MaxValue;
            int ling = T(S.Rounds!.LingerS), part = T(S.Rounds.PartS), origin = T(sv.Origin);
            var pieces = new List<Piece>(); int cur = origin, clock = 0;
            void run(int to, int c0, int from) => pieces.Add(new Piece { From = from, To = to, Kind = "run", C0 = c0 });
            Piece walkTo(int from, int tr)
            {
                var w = WalkBack(sv, tr);
                if (w == null) return new Piece { From = from, To = from, Kind = "fixed", Pos = sv.At, Face = sv.Face, Phase = "to post" };
                return new Piece { From = from, To = from + w.Value.dur, Kind = "walk", WPath = w.Value.path, StartMm = w.Value.startMm, Dur = w.Value.dur };
            }
            // an order taken before the last one's linger is over is the same hold, longer
            var holds = new List<(int from, int to, Pt face)>();
            var sorted = StableByFrom(ev.Holds); // by the tick taken, ties in the order taken
            foreach (var h in sorted)
            {
                if (holds.Count > 0 && h.From <= holds[holds.Count - 1].to + ling) { var l = holds[holds.Count - 1]; holds[holds.Count - 1] = (l.from, Math.Max(l.to, h.To), h.Face); }
                else holds.Add((h.From, h.To, h.Face));
            }
            var events = new List<(string kind, int at, int ix)>();
            for (int i = 0; i < holds.Count; i++) events.Add(("hold", holds[i].from, i));
            if (ev.CallAt >= 0) events.Add(("call", ev.CallAt, -1));
            if (ev.EndAt >= 0) events.Add(("end", ev.EndAt, -1));
            events = StableByAt(events);
            foreach (var e in events)
            {
                if (e.at < cur)
                {
                    if (e.kind == "hold") continue;
                    if (pieces.Count > 0 && pieces[pieces.Count - 1].To > e.at) pieces[pieces.Count - 1].To = e.at;
                    cur = e.at;
                }
                if (e.at > cur) { run(e.at, clock, cur); clock += e.at - cur; cur = e.at; }
                if (e.kind == "hold")
                {
                    var h = holds[e.ix]; var was = BodyPose(sv, clock); int to = h.to + ling;
                    pieces.Add(new Piece { From = cur, To = to, Kind = "fixed", Pos = was.Pos, Face = h.face, Phase = "held" });
                    cur = to;
                }
                else if (e.kind == "call")
                {
                    var w = walkTo(cur, clock); int arrive = w.To; pieces.Add(w);
                    int wait = Math.Max(arrive, ev.CallArrive);
                    pieces.Add(new Piece { From = arrive, To = wait, Kind = "fixed", Pos = sv.At, Face = sv.Face, Phase = "at post" });
                    var gf = ev.CallStop - sv.At;
                    int end = Math.Max(wait, ev.CallUntil) + part;
                    pieces.Add(new Piece { From = wait, To = end, Kind = "fixed", Pos = sv.At, Face = gf, Phase = "together" });
                    cur = end; clock = Resume(sv, clock);
                }
                else
                {
                    var w = walkTo(cur, clock); pieces.Add(w);
                    pieces.Add(new Piece { From = w.To, To = MAX, Kind = "fixed", Pos = sv.At, Face = sv.Face, Phase = "at post" });
                    cur = MAX; break;
                }
            }
            if (cur < MAX) run(MAX, clock, cur);
            roundCache[sv.Id] = (ev.Version, pieces);
            return pieces;
        }
        static List<RoundEvents.Hold> StableByFrom(List<RoundEvents.Hold> l) { var o = new List<RoundEvents.Hold>(l); var ix = new Dictionary<RoundEvents.Hold, int>(); for (int i = 0; i < o.Count; i++) ix[o[i]] = i; o.Sort((a, b) => a.From != b.From ? a.From.CompareTo(b.From) : ix[a].CompareTo(ix[b])); return o; }
        static List<(string kind, int at, int ix)> StableByAt(List<(string kind, int at, int ix)> l) { var o = new List<(string kind, int at, int ix, int n)>(); for (int i = 0; i < l.Count; i++) o.Add((l[i].kind, l[i].at, l[i].ix, i)); o.Sort((a, b) => a.at != b.at ? a.at.CompareTo(b.at) : a.n.CompareTo(b.n)); return o.ConvertAll(x => (x.kind, x.at, x.ix)); }

        /// <summary>A server's pose at a mission tick, given what the mission did to her (rounds.ts poseAt).</summary>
        RoundPose RoundPoseAt(RoundsData.Server sv, RoundEvents ev, int tick)
        {
            if (tick < T(sv.Origin)) return new RoundPose { Pos = sv.At, Face = sv.Face, Phase = "at post" };
            var pieces = Pieces(sv, ev);
            Piece? p = null; foreach (var x in pieces) if (tick >= x.From && tick < x.To) { p = x; break; }
            p ??= pieces[pieces.Count - 1];
            switch (p.Kind)
            {
                case "run": return BodyPose(sv, p.C0 + (tick - p.From));
                case "walk":
                    {
                        int k = Math.Min(p.Dur, tick - p.From); long mm = p.Dur == 0 ? 0 : RoundHalf(p.StartMm * (p.Dur - k), p.Dur); var h = p.WPath!.Heading(mm);
                        return new RoundPose { Pos = p.WPath.At(mm), Face = new Pt(-h.X, -h.Y), Phase = "to post" };
                    }
                default: return new RoundPose { Pos = p.Pos, Face = p.Face, Phase = p.Phase };
            }
        }

        /// <summary>The reference function for tests (brief 022 vectors): a server's pose at a tick under the given events, without touching the run.</summary>
        public RoundPose RoundReference(string serverId, int tick, (int from, int to, Pt face)[] holds, (int at, int arrive, int until, Pt stop)? call, int endAt)
        {
            var sv = S.Rounds!.Servers[serverId]; var ev = new RoundEvents();
            foreach (var h in holds) ev.Holds.Add(new RoundEvents.Hold { From = h.from, To = h.to, Face = h.face });
            if (call.HasValue) { ev.CallAt = call.Value.at; ev.CallArrive = call.Value.arrive; ev.CallUntil = call.Value.until; ev.CallStop = call.Value.stop; }
            ev.EndAt = endAt; ev.Version = -1;
            var saved = roundCache.TryGetValue(serverId, out var c) ? c : ((int, List<Piece>)?)null;
            roundCache.Remove(serverId);
            var q = RoundPoseAt(sv, ev, tick);
            if (saved.HasValue) roundCache[serverId] = saved.Value; else roundCache.Remove(serverId);
            return q;
        }

        /// <summary>The round's state for the checkpoint hash: no bytes unless the round is played.</summary>
        internal void HashRounds(Hash.H h)
        {
            if (!RoundsOn) return;
            var ids = new List<string>(roundEv.Keys); ids.Sort(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                var ev = roundEv[id]; h.S(id).I(ev.Holds.Count);
                foreach (var x in ev.Holds) h.I(x.From).I(x.To).P(x.Face);
                h.I(ev.CallAt).I(ev.CallArrive).I(ev.CallUntil).P(ev.CallStop).I(ev.EndAt).S(ev.EndWhy);
            }
            foreach (var sv in S.Rounds!.Servers.Values) { var p = PersonById(sv.Id); if (p != null && !p.Down) { var q = RoundPoseAt(sv, EvRead(sv), Tick); h.P(q.Pos).S(q.Phase); } }
        }
    }
}
