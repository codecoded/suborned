// Brief 059 (engine 0.32.0): the rival crew's rules as the rulebook states them (rulebook `rivals`; Pulse briefs/059/pulse/rival-routine.md,
// Motive briefs/059/motive/rulings.json). Read strictly at load: unknown = reject.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>One beat of a posted body's idle business: a pose name (the host's), its seconds, the yaw off the base facing (0, ±30, ±60),
    /// and for the lookout's pace beats the walk along the kerb (Move +1 out, −1 back; AtPace: standing at the paced end).</summary>
    public sealed class CrewBeat { public string Pose = ""; public int S, Yaw, Move; public bool AtPace; }

    /// <summary>A beat loop and each member's phase in seconds.</summary>
    public sealed class CrewLoop
    {
        public readonly List<CrewBeat> Beats = new List<CrewBeat>();
        public readonly Dictionary<string, int> PhaseS = new Dictionary<string, int>(StringComparer.Ordinal);
        public int PeriodS;
    }

    public sealed class CrewRules
    {
        public readonly int Size, Posted, SpeedCmS, FollowCmS, DropS, OutS, StepS, FollowS, LookoutPaceCm;
        /// <summary>Engine 0.41.0 (brief 096, rivals.block): the block point's stand-off from the cell's car (cm) and the wheelman's step out (s);
        /// engine 0.42.0 (Motive F1, F2): the wheelman's gunfire ring is retired (he stays at the wheel), a broken man's run off the map (cm/s).</summary>
        public readonly int BlockCm, WheelmanOutS, BrokenCmS;
        /// <summary>Engine 0.41.0 (rivals.lookout): his run after the target's car (cm/s), and how long a misled member watches where he was sent (s).</summary>
        public readonly int LookoutRunCmS, MisledS;
        /// <summary>Engine 0.41.0 (rivals.turn): Pay's price, time and Trail; the cap on one temperament (per cent of the crew); the kinds, in draw order.</summary>
        public readonly int PayUsd, PayS, PayTrail, TemperMaxPct; public readonly string[] Kinds;
        /// <summary>Engine 0.33.0 (rivals.free, Motive M059-3.4): a crewmate within this many cm of a tied member unties him.</summary>
        public readonly int FreeCm;
        public readonly CrewLoop PostedLoop, LookoutLoop, WheelmanLoop;

        public CrewRules(JNode n, Func<JNode, int> ri, int safeHostiles)
        {
            n.Only("crew", "posted", "speed", "followPace", "drop", "out", "step", "follow", "lookoutPace", "free", "peopleRule", "block", "lookout", "turn", "beats", "rule", "tag", "src", "proposed", "reason");
            FreeCm = ri(n.Opt("free") ?? throw new FormatException("rivals.free is missing: engine 0.33.0 frees a tied rival by a crewmate within it (Motive M059-3.4)"));
            if (n.Opt("peopleRule") is not JNode pr || pr.Str().Length == 0) throw new FormatException("rivals.peopleRule is missing: engine 0.33.0 takes the crew as people for the takedown and the body orders (Motive M059-3.1)");
            if (n["rule"].Str().Length == 0) throw new FormatException("rivals.rule is empty");
            Size = ri(n["crew"]); Posted = ri(n["posted"]); SpeedCmS = ri(n["speed"]); FollowCmS = ri(n["followPace"]);
            DropS = ri(n["drop"]); OutS = ri(n["out"]); StepS = ri(n["step"]); FollowS = ri(n["follow"]);
            LookoutPaceCm = ri(n["lookoutPace"]);
            // engine 0.41.0 (brief 096; Motive rivals.json; required: a rulebook without them is pinned before 0.41.0, unknown = reject)
            var bk = n.Opt("block") ?? throw new FormatException("rivals.block is missing: engine 0.41.0 runs the block on arrival (brief 096)");
            bk.Only("standOff", "wheelmanOut", "brokenPace", "rule", "tag", "src", "proposed", "reason"); if (bk.Opt("brokenPace") == null) throw new FormatException("rivals.block.brokenPace is missing: engine 0.42.0 runs a broken man off the map (brief 096, Motive F2)"); if (bk["rule"].Str().Length == 0) throw new FormatException("rivals.block.rule is empty");
            BlockCm = ri(bk["standOff"]) * 100; WheelmanOutS = ri(bk["wheelmanOut"]); BrokenCmS = ri(bk["brokenPace"]);
            var lk = n.Opt("lookout") ?? throw new FormatException("rivals.lookout is missing: engine 0.41.0 makes the lookout the cell's job (brief 096)");
            lk.Only("follow", "misled", "rule", "tag", "src", "proposed", "reason"); if (lk["rule"].Str().Length == 0) throw new FormatException("rivals.lookout.rule is empty");
            LookoutRunCmS = ri(lk["follow"]); MisledS = ri(lk["misled"]);
            var tn = n.Opt("turn") ?? throw new FormatException("rivals.turn is missing: engine 0.41.0 gives the crew temperaments (brief 096, Motive Q3 a)");
            tn.Only("pay", "s", "trail", "maxShare", "kinds", "rule", "tag", "src", "proposed", "reason"); if (tn["rule"].Str().Length == 0) throw new FormatException("rivals.turn.rule is empty");
            PayUsd = ri(tn["pay"]); PayS = ri(tn["s"]); PayTrail = ri(tn["trail"]); TemperMaxPct = ri(tn["maxShare"]);
            var kinds = new List<string>(); foreach (var k in tn["kinds"].Items) kinds.Add(k.Str()); Kinds = kinds.ToArray();
            if (Kinds.Length != 2 || Kinds[0] != "mercenary" || Kinds[1] != "loyal") throw new FormatException("rivals.turn.kinds: the engine draws mercenary or loyal (Motive Q3 a)");
            if (BlockCm < 100 || BrokenCmS < 1 || WheelmanOutS < 1 || LookoutRunCmS < 1 || MisledS < 1 || PayUsd < 1 || PayS < 1 || PayTrail < 0 || TemperMaxPct < 50 || TemperMaxPct > 100) throw new FormatException("rivals.block, lookout, turn: positive values; maxShare 50..100 %");
            if (Size != 5) throw new FormatException("rivals.crew: the engine's crew is five (rival-1..3 posted, rival-4 the lookout, rival-5 the wheelman; GDD §07)");
            if (Posted != 3) throw new FormatException("rivals.posted: the engine posts three (rival-1..3)");
            if (safeHostiles != Size) throw new FormatException("fight.safe.hostiles is the crew's size (rivals.crew)");
            if (DropS <= StepS * Posted) throw new FormatException("rivals.drop is longer than the posted three's steps out");
            var b = n["beats"]; b.Only("maxS", "minBeats", "posted", "lookout", "wheelman", "rule"); b["rule"].Str();
            int maxS = b["maxS"].Int(), minBeats = b["minBeats"].Int();
            if (maxS < 1 || minBeats < 1) throw new FormatException("rivals.beats.maxS and minBeats are at least 1");
            PostedLoop = Loop(b["posted"], maxS, minBeats, new[] { "rival-1", "rival-2", "rival-3" }, false);
            LookoutLoop = Loop(b["lookout"], maxS, minBeats, new[] { "rival-4" }, true);
            WheelmanLoop = Loop(b["wheelman"], maxS, minBeats, new[] { "rival-5" }, false);
        }

        static CrewLoop Loop(JNode n, int maxS, int minBeats, string[] who, bool pace)
        {
            n.Only("phase", "loop");
            var l = new CrewLoop();
            foreach (var kv in n["phase"].Entries)
            {
                if (Array.IndexOf(who, kv.Key) < 0) throw new FormatException($"{n.Path}.phase: '{kv.Key}' is not one of {string.Join(", ", who)}");
                int ph = kv.Value.Int(); if (ph < 0) throw new FormatException($"{kv.Value.Path} is not negative");
                l.PhaseS[kv.Key] = ph;
            }
            foreach (var w in who) if (!l.PhaseS.ContainsKey(w)) throw new FormatException($"{n.Path}.phase has no '{w}'");
            CrewBeat? prev = null; int run = 0; string runPose = "";
            foreach (var x in n["loop"].Items)
            {
                x.Only("pose", "s", "yaw", "move", "at");
                var bt = new CrewBeat { Pose = x["pose"].Str(), S = x["s"].Int(), Yaw = x["yaw"].Int() };
                if (bt.Pose.Length == 0 || bt.S < 1 || bt.S > maxS) throw new FormatException($"{x.Path}: a beat has a pose and 1..{maxS} s");
                if (bt.Yaw != 0 && bt.Yaw != 30 && bt.Yaw != -30 && bt.Yaw != 60 && bt.Yaw != -60) throw new FormatException($"{x.Path}.yaw is 0, ±30 or ±60");
                if (x.Opt("move") is JNode mv) { if (!pace) throw new FormatException($"{x.Path}.move: only the lookout paces"); var m = mv.Str(); bt.Move = m == "out" ? 1 : m == "back" ? -1 : throw new FormatException($"{mv.Path} is out or back"); }
                if (x.Opt("at") is JNode at) { if (!pace || at.Str() != "pace") throw new FormatException($"{at.Path}: only the lookout stands at 'pace'"); bt.AtPace = true; }
                if (prev != null && prev.Pose == bt.Pose && prev.Yaw == bt.Yaw) throw new FormatException($"{x.Path}: two beats in a row with one pose and yaw");
                run = bt.Pose == runPose ? run + bt.S : bt.S; runPose = bt.Pose;
                if (run > maxS) throw new FormatException($"{x.Path}: one pose runs longer than {maxS} s");
                l.Beats.Add(bt); l.PeriodS += bt.S; prev = bt;
            }
            if (l.Beats.Count < minBeats) throw new FormatException($"{n.Path}.loop has fewer than {minBeats} beats");
            return l;
        }

        /// <summary>The beat a member plays `ticks` after his post began (whole loop, 10 Hz), and the ticks into it.</summary>
        public static (CrewBeat beat, int into) BeatAt(CrewLoop l, string id, int ticks)
        {
            int period = l.PeriodS * Game.Hz, t = ((ticks + l.PhaseS[id] * Game.Hz) % period + period) % period;
            foreach (var b in l.Beats) { int len = b.S * Game.Hz; if (t < len) return (b, t); t -= len; }
            return (l.Beats[l.Beats.Count - 1], 0);
        }
    }
}
