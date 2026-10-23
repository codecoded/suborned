using System;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 033 (Sky 2026-10-21; Forma's stills, Glyph's cue board briefs/033/cues.html): the roster portraits change face with what
    /// happens to that agent. One state per agent, from the sim's own reads, priority top to bottom, the first true wins:
    /// downed (op.Down), badly hurt (health at most a third), under fire (shot at in the last 2 s: Gun.ShotByTick), hurt (under two thirds, or
    /// a hit in the last 3 s), firing (drawn with a live fire order on a target still up, or a shot in the last 1 s: Gun.NextShot moved),
    /// noticed (the displayed attention toward the agent, HostAttention's Cue: the same instant as the attention inset, never a cone test),
    /// success (a 2 s beat on a landed act of the agent's, only from calm or focused), focused (Watch, Listen, a turn or a call under way, or
    /// any timed act running), calm. A state holds 1 s before a lower one may replace it; a higher one takes over at once.
    /// Success needs the act's agent. The log line carries no actor field (LogLine is tick, code, text), but every landing code names its
    /// agent or belongs to one agent's verb: &lt;agent&gt;.watch.watched (brief 031), scout.listen.* and rivals.misled (the Scout's), turn.*.turned
    /// and call.&lt;role&gt; (the Fixer's), loop.on and patrol.rerouted (the Tech's), takedown.out / takedown.dead (unseen), take.done and
    /// enter.in (the Enforcer's). If a verb is ever shared, Keel adds the actor to the line. View only: nothing here reaches the engine.</summary>
    public sealed partial class HandoverHost
    {
        public enum Face { Calm, Focused, Noticed, UnderFire, Firing, Hurt, BadlyHurt, Downed, Success }
        /// <summary>The stills' file names (briefs/033/portraits/final/&lt;agent&gt;_&lt;state&gt;.png), by Face.</summary>
        public static readonly string[] FaceFiles = { "calm", "focused", "noticed", "under_fire", "firing", "hurt", "badly_hurt", "downed", "success" };

        /// <summary>Brief 033's priority: downed, badly hurt, under fire, hurt, firing, noticed, success, focused, calm.</summary>
        public static int FaceRank(Face f) => f switch
        {
            Face.Downed => 8, Face.BadlyHurt => 7, Face.UnderFire => 6, Face.Hurt => 5, Face.Firing => 4, Face.Noticed => 3, Face.Success => 2, Face.Focused => 1, _ => 0,
        };

        public const float FaceHoldS = 1f, SuccessS = 2f, HitFlashS = 0.6f;

        sealed class FaceState
        {
            public Face Shown = Face.Calm; public float Since = -10f;
            public int LastHp = int.MinValue, HitTick = -100000, LastNext = int.MinValue, ShotTick = -100000, Eye, LostPip = -1;
            public float SuccessUntil = -10f, HitAt = -10f, EyeUpAt = -10f;
        }
        readonly FaceState[] faces = { new(), new(), new(), new() };
        Suborned.Sim.Game faceGame; int faceLog;

        /// <summary>For scripted shots only: a face held whatever the run says (null: the run decides). Never saved.</summary>
        public static readonly Face?[] FaceForce = new Face?[4];

        /// <summary>The face the roster shows for agent k now, and since when (unscaled seconds).</summary>
        public Face FaceOf(int k) => FaceForce[k] ?? faces[k].Shown;
        public float FaceSince(int k) => faces[k].Since;
        /// <summary>The highest displayed attention stage toward agent k (0 calm .. 3 reporting), and when it last stepped up.</summary>
        public int EyeStage(int k) => FaceForce[k] == Face.Noticed && faces[k].Eye == 0 ? 1 : faces[k].Eye;
        public float EyeUpAt(int k) => faces[k].EyeUpAt;
        /// <summary>The last hit on agent k (unscaled seconds) and the pip it cost (its index, -1 none).</summary>
        public float HitAt(int k) => faces[k].HitAt;
        public int LostPip(int k) => faces[k].LostPip;

        /// <summary>Once a frame after the attention cues: each agent's face from the run.</summary>
        void FacesFrame()
        {
            var g = Game; if (g == null) return; // brief 041: the range too (the under-fire cue is tested there)
            float now = Time.unscaledTime;
            if (!ReferenceEquals(g, faceGame)) { faceGame = g; faceLog = g.Log.Count; foreach (var f in faces) { f.Shown = Face.Calm; f.Since = now; f.LastHp = int.MinValue; f.HitTick = -100000; f.LastNext = int.MinValue; f.ShotTick = -100000; f.Eye = 0; f.SuccessUntil = -10f; f.HitAt = -10f; f.EyeUpAt = -10f; f.LostPip = -1; } }
            // the landings since the last frame: a success beat for the act's agent
            if (Ending.Length == 0)
                for (int i = faceLog; i < g.Log.Count; i++) { int who = SuccessAgent(g, g.Log[i]); if (who >= 0) faces[who].SuccessUntil = now + SuccessS; }
            faceLog = g.Log.Count;
            // the displayed attention toward each agent (HostAttention's cue: up at once, down after its 2 s)
            Span<int> eye = stackalloc int[4];
            if (!g.Range) foreach (var (_, _, stage, op) in Cues()) { if (stage <= 0 || !IsOpName(op)) continue; int k = (int)Op.Parse(op); if (stage > eye[k]) eye[k] = stage; }
            for (int k = 0; k < 4; k++)
            {
                var f = faces[k]; var op = g.Ops[k]; var gun = op.Gun;
                if (eye[k] > f.Eye) f.EyeUpAt = now; f.Eye = eye[k];
                int hp = gun.Health;
                if (f.LastHp != int.MinValue && hp < f.LastHp) { f.HitTick = g.Tick; f.HitAt = now; f.LostPip = Mathf.Max(0, Seg(hp)); }
                f.LastHp = hp;
                if (f.LastNext != int.MinValue && gun.NextShot != f.LastNext && gun.Fired) f.ShotTick = g.Tick;
                f.LastNext = gun.NextShot;
                var want = FaceNow(g, k, f, now);
                if (want == f.Shown) continue;
                if (FaceRank(want) > FaceRank(f.Shown) || now - f.Since >= FaceHoldS) { f.Shown = want; f.Since = now; }
            }
        }

        Face FaceNow(Suborned.Sim.Game g, int k, FaceState f, float now)
        {
            var op = g.Ops[k]; var gun = op.Gun; int max = Mathf.Max(gun.Max, gun.Health);
            if (op.Down || gun.Down) return Face.Downed;
            if (max > 0 && gun.Health * 3 <= max) return Face.BadlyHurt;
            if (UnderFireNow[k].Count > 0) return Face.UnderFire; // engine 0.19 UnderFireOf: a shooter's spell on this agent, still up, within 3 s
            if (max > 0 && (gun.Health * 3 < max * 2 || g.Tick - f.HitTick <= 3 * Suborned.Sim.Game.Hz)) return Face.Hurt;
            bool targetUp = gun.Target.Length > 0 && !(g.GunOf(gun.Target) is Gun tg && tg.Down);
            if (gun.Drawn && gun.Order.Length > 0 && targetUp || g.Tick - f.ShotTick <= Suborned.Sim.Game.Hz) return Face.Firing;
            if (f.Eye >= 1) return Face.Noticed;
            var d = doing[k];
            bool focused = op.Busy != null && op.Busy.Do is "watch" or "listen" or "turn" or "call" || d.Busy && d.Progress >= 0 && d.Phase == "acting";
            if (now < f.SuccessUntil) return Face.Success; // only from calm or focused: every higher state returned above
            return focused ? Face.Focused : Face.Calm;
        }

        /// <summary>Whose landed act a log line is (the agent's index), or -1: see the class note for the codes.</summary>
        static int SuccessAgent(Suborned.Sim.Game g, LogLine l)
        {
            var land = LandingOf(g, l); // brief 043: one landing detector for the face, the tick, the plate, the chip and the row
            if (land.Agent < 0 || land.Outcome is "not-yet" or "refused" or "suspicious" || land.Do is "carry" or "stash" or "restrain" or "drop" or "board" || land.Code == "enter.out") return -1;
            if (land.Do == "takedown") { foreach (var x in g.Log) if (x.Tick == l.Tick && x.Code == "takedown.seen") return -1; } // seen: not a quiet win
            return land.Agent;
        }
    }
}
