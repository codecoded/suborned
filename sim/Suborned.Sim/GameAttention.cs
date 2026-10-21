// Watchers notice loiterers (brief 031 §2, Keel; Sky 2026-10-20: "Standing in an observed area for a while without moving causes alert. So
// standing behind a camera's LoS is fine … same for a robot (hence the digital face that changes expression on them)"; L1-L3). The rules are
// the rulebook's `attention` section:
//  · every security watcher on the street (a live camera, the door guard, the patrol robot, the response's officers and robots) keeps one
//    clock per operative; civilians and rivals keep none;
//  · in its sight, standing still, idle or Watching: +1 tick a tick; in sight in any other act: held; moving or out of sight: -1, to 0, or
//    to the wary mark once wary;
//  · in sight: a machine by its cone only (L2); a person (the guard, the officers) also within attention.near on a clear line, the posted
//    guard included (for the clock only);
//  · the marks are the Heat band's (Heat at the start: Start.Heat, else the site's mission start.heat, else 0): noticed (a warning), wary
//    (for the mission; a wary guard is aware of the Enforcer at a takedown), report (once per operative per watcher: a camera's at its
//    watcher's next scan + call, a person's or robot's by radio after call; a found fault: Alert at least attention.alert, no Trail, a
//    sighting, the dial's dispatch).
// Integers only; watchers in WatcherIds order, operatives in OpId order; reports in the order they were made. Hashed only once a clock has
// moved, so a run in which no watcher ever saw an operative stand still hashes as before.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>One watcher's clocks, one per operative (ticks), its wary marks, its reports made, and the highest stage logged.</summary>
        sealed class AttnRow { public readonly int[] C = new int[4]; public readonly bool[] Wary = new bool[4], Reported = new bool[4]; public readonly int[] Noted = new int[4]; }
        /// <summary>A loiterer's report on its way to the desk.</summary>
        sealed class AttnRep { public string W = ""; public int Op, From, LandAt; public Pt At; public bool Done, Stopped; }

        readonly SortedDictionary<string, AttnRow> attn = new SortedDictionary<string, AttnRow>(StringComparer.Ordinal);
        readonly List<AttnRep> attnReps = new List<AttnRep>();
        /// <summary>Which operatives each watcher had in its sight on the last tick (derived from positions: not hashed). For the cues.</summary>
        readonly Dictionary<string, int> attnSeen = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>The cue stages, in order (brief 031 §3): calm, noticed, wary, reporting (until the report lands).</summary>
        public static readonly string[] AttentionStages = { "calm", "noticed", "wary", "reporting" };

        /// <summary>The cell's Heat for this mission (campaign Trail + Menace), fixed at the start: the start state's, else the mission's.</summary>
        public int Heat => Start.Heat ?? S.Heat;
        /// <summary>The Heat band's id (rulebook attention.bands: unknown, noticed, wanted, infamous): the briefing's word is Fable's for it.</summary>
        public string HeatBand => R.AttnBand(Heat).id;
        /// <summary>The Heat band's marks in seconds (noticed, wary, report), for the HUD and the scenarios.</summary>
        public (int notice, int wary, int report) AttentionMarks { get { var b = R.AttnBand(Heat); return (b.noticeS, b.waryS, b.reportS); } }

        /// <summary>Does this watcher keep an attention clock (a security watcher: not the rivals)?</summary>
        bool AttnWatcher(string w) { var k = WatcherKind(w); return k == "camera" || k == "guard" || k == "patrol" || k == "security"; }
        /// <summary>Is this watcher a person (the guard, an officer: all-round near sense) rather than a machine (a camera, the patrol, a robot)?</summary>
        bool AttnPerson(string w)
        {
            switch (WatcherKind(w))
            {
                case "guard": return R.AttnGuardNear;
                case "security": return UnitOf(w) is Unit u ? !u.Robot && R.AttnOfficerNear : R.AttnOfficerNear;
                default: return false;
            }
        }
        /// <summary>Is the operative in this watcher's sight for the clock: its live sight now (SeesNow), and for a person anyone within
        /// attention.near on a clear line, whatever his facing.</summary>
        bool AttnSees(string w, Shape s, Op op)
        {
            if (SeesNow(w, op.Pos)) return true;
            return AttnPerson(w) && G.Within(s.At, op.Pos, R.AttnNearCm) && Sees(s.At, op.Pos) && !CrowdBlocks(s.At, op.Pos);
        }
        /// <summary>Is the operative in an act other than a Watch (the clock holds): busy, carrying a body or an operative, holding Vale.</summary>
        bool AttnInAct(Op op) => (op.Busy != null && op.Busy.Do != "watch") || op.CarryBody.Length > 0 || op.Carrying.HasValue
            || (op.Id == OpId.Enforcer && (Vale == ValeAt.WithEnforcer || Vale == ValeAt.Taking));

        /// <summary>Once a tick (brief 031 §2): every security watcher's clock on every operative, its stages, and the reports it makes.</summary>
        void AttentionStep()
        {
            attnSeen.Clear();
            var band = R.AttnBand(Heat); int nT = T(band.noticeS), wT = T(band.waryS), rT = T(band.reportS);
            foreach (var w in WatcherIds())
            {
                if (!AttnWatcher(w)) continue;
                var sh = ShapeOf(w); bool live = sh.HasValue && sh.Value.Live;
                attn.TryGetValue(w, out var row);
                if (!live && row == null) continue;
                int seenMask = 0;
                for (int k = 0; k < 4; k++)
                {
                    var op = Ops[k];
                    bool present = !op.Inside && !op.InCar && !op.Down && !op.CarriedBy.HasValue;
                    bool seen = live && present && AttnSees(w, sh!.Value, op);
                    if (seen) seenMask |= 1 << k;
                    if (row == null) { if (!seen) continue; attn[w] = row = new AttnRow(); }
                    bool moving = op.Walk != null || (op.Driven && op.DriveMoved);
                    int c = row.C[k];
                    if (seen && AttnInAct(op)) { } // an act holds it (L1): the act's own seen-rules apply
                    else if (seen && !moving) c++;
                    else c = Math.Max(row.Wary[k] ? wT : 0, c - 1);
                    row.C[k] = c;
                    if (c >= wT) row.Wary[k] = true;
                    int st = c >= rT ? 3 : c >= wT ? 2 : c >= nT ? 1 : 0;
                    if (st > row.Noted[k])
                    {
                        string who = Sim.Op.Name(op.Id), by = w.Replace("camera:", "the camera ");
                        if (row.Noted[k] < 1) Note("attention.noticed", $"{by} notices the {who} standing still in its sight.");
                        if (st >= 2 && row.Noted[k] < 2) Note("attention.wary", $"{by} is wary of the {who} for the rest of the mission.");
                        row.Noted[k] = st;
                    }
                    if (c >= rT && !row.Reported[k])
                    {
                        row.Reported[k] = true;
                        int land;
                        if (WatcherKind(w) == "camera") { int period = T(R.AttnScanS), phase = (int)(Streams.Fnv1a32(w) % (uint)period); land = Tick + ((phase - Tick % period) % period + period) % period + T(R.AttnCallS); }
                        else land = Tick + T(R.AttnCallS);
                        attnReps.Add(new AttnRep { W = w, Op = k, From = Tick, LandAt = land, At = op.Pos });
                        Note("attention.reporting", $"{w.Replace("camera:", "the camera ")} reports the {Sim.Op.Name(op.Id)} loitering (lands {Sim.Report.Clock(land)}).");
                    }
                }
                if (seenMask != 0) attnSeen[w] = seenMask;
            }
            AttentionReports();
        }

        /// <summary>Whether the watcher that made a report is down or gone before it lands (a camera's report is on its tape: it lands).</summary>
        bool AttnReporterDown(string w)
        {
            switch (WatcherKind(w))
            {
                case "camera": return false;
                case "guard": return GuardDown;
                case "patrol": return PatrolDown;
                case "security":
                    if (UnitOf(w) is Unit u) return u.Gun.Down;
                    { int k = SecIndex(w); return k >= 0 && SecGuns[k].Down; }
                default: return true;
            }
        }

        /// <summary>The loiterers' reports that land now, in the order made: a found fault (Alert at least attention.alert, no Trail), a sighting,
        /// the dial's dispatch (an alarm with no body). A reporter downed first drops his.</summary>
        void AttentionReports()
        {
            foreach (var r in attnReps)
            {
                if (r.Done || r.LandAt > Tick) continue;
                r.Done = true;
                string who = Sim.Op.Name((OpId)r.Op);
                if (AttnReporterDown(r.W)) { r.Stopped = true; Note("attention.stopped", $"{r.W.Replace("camera:", "the camera ")}'s report of the {who} never lands."); continue; }
                Sightings.Add(new Sighting { Watcher = r.W, Op = who, From = r.From, To = Tick, At = r.At });
                RaiseAlert(Math.Max(Alert, R.AttnAlert), $"loiter:{r.W}:{who}");
                Note("attention.landed", $"{r.W.Replace("camera:", "the camera ")}'s report lands: the {who} loitering. Alert {Alert}.");
                Alarm(null, r.At, null, "");
                if (Over) return;
            }
        }

        // ---------------------------------------------------------------- the host's reads (brief 031 §3: the cues)

        /// <summary>One watcher's stage toward one operative now (0 calm, 1 noticed, 2 wary, 3 reporting): calm unless the operative stood in its
        /// sight on the last tick, so a cue never shows a sight the cell hasn't seen. Read-only.</summary>
        public int AttentionStage(string watcher, string op)
        {
            int k = (int)Sim.Op.Parse(op);
            if (!attnSeen.TryGetValue(watcher, out var mask) || (mask & (1 << k)) == 0 || !attn.TryGetValue(watcher, out var row)) return 0;
            foreach (var r in attnReps) if (!r.Done && r.W == watcher && r.Op == k) return 3;
            if (row.Wary[k]) return 2;
            return row.C[k] >= T(R.AttnBand(Heat).noticeS) ? 1 : 0;
        }
        /// <summary>A watcher's cue (brief 031 §3): its highest stage toward any operative in its sight now, and that operative (the higher
        /// clock on a tie, then the operative order); (0, "") when calm. Read-only.</summary>
        public (int stage, string op) Attention(string watcher)
        {
            int best = 0, bc = -1; string who = "";
            attn.TryGetValue(watcher, out var row);
            for (int k = 0; k < 4; k++)
            {
                var name = Sim.Op.Name((OpId)k); int st = AttentionStage(watcher, name); if (st == 0) continue;
                int c = row?.C[k] ?? 0;
                if (st > best || (st == best && c > bc)) { best = st; bc = c; who = name; }
            }
            return (best, who);
        }
        /// <summary>A watcher's clock on an operative, in ticks (0 when it has none). Read-only.</summary>
        public int AttentionTicks(string watcher, string op) => attn.TryGetValue(watcher, out var row) ? row.C[(int)Sim.Op.Parse(op)] : 0;
        /// <summary>Whether a watcher is wary of an operative (for the rest of the mission). Read-only.</summary>
        public bool AttentionWary(string watcher, string op) => attn.TryGetValue(watcher, out var row) && row.Wary[(int)Sim.Op.Parse(op)];
        /// <summary>Whether a watcher has reported this operative as a loiterer this mission (made, landed or not). Read-only.</summary>
        public bool AttentionReported(string watcher, string op) => attn.TryGetValue(watcher, out var row) && row.Reported[(int)Sim.Op.Parse(op)];
        /// <summary>The loiterer reports made so far, in order: watcher, operative, when made, when it lands, and whether it landed or was stopped.</summary>
        public List<(string watcher, string op, int from, int landAt, string state)> AttentionReportsMade()
        {
            var l = new List<(string, string, int, int, string)>();
            foreach (var r in attnReps) l.Add((r.W, Sim.Op.Name((OpId)r.Op), r.From, r.LandAt, r.Stopped ? "stopped" : r.Done ? "landed" : "pending"));
            return l;
        }

        /// <summary>The clocks and reports for the checkpoint hash: no bytes until a clock has moved.</summary>
        internal void HashAttention(Hash.H h)
        {
            if (attn.Count == 0 && attnReps.Count == 0) return;
            h.S("attention").I(Heat);
            foreach (var kv in attn)
            {
                h.S(kv.Key);
                for (int k = 0; k < 4; k++) h.I(kv.Value.C[k]).Z(kv.Value.Wary[k]).Z(kv.Value.Reported[k]).I(kv.Value.Noted[k]);
            }
            foreach (var r in attnReps) h.S(r.W).I(r.Op).I(r.From).I(r.LandAt).P(r.At).Z(r.Done).Z(r.Stopped);
        }
    }
}
