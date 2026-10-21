using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 031 (Sky 2026-10-20/21; Glyph's cue board briefs/031/cues.html, option A): the watchers' attention on the street. The engine
    /// keeps a clock per (security watcher, agent) and reads its stage out as Game.Attention(watcher): calm, noticed, wary, reporting, only
    /// while the agent stands in the watcher's sight now, so no cue ever draws a cone the cell hasn't learned. This host shows it:
    ///  · the displayed stage rises at once and falls one stage only after the engine's stage has stayed under it for 2 s of mission time
    ///    (board §1 rule 3: stepping across an edge paints no edge; display only, the sim is untouched);
    ///  · each stage-up of a watcher toward an agent is a callout (board §9): noticed once per watcher per mission, wary and reporting once
    ///    each, at most one per agent and one per watcher, the higher stage first, then the newest; the rest queue behind (a "+1");
    ///  · H19, the first-noticed hint (Fable's text, shown once).
    /// The views draw the cues (HostViews.Attention: lens, faces, heads); the HUD draws the callouts (HudToolkit.Attention). Nothing here is an
    /// order or reaches the engine, so every replay is unchanged.</summary>
    public sealed partial class HandoverHost
    {
        sealed class AttnShown { public int Stage, LowFrom = -1; public string Op = ""; }
        readonly Dictionary<string, AttnShown> attnShown = new(StringComparer.Ordinal);
        readonly HashSet<string> calloutSeen = new(StringComparer.Ordinal);
        int attnTick = -1;

        /// <summary>One close-up callout (board §9): the watcher, its view key, the agent it has, the stage it stepped up to, when it opened and
        /// until when it stays (real time: it does not stop for pause), and its order of claim (newest last).</summary>
        public sealed class Callout { public string Watcher = "", View = "", Op = ""; public int Stage, Seq; public float Born, Until; public bool Queued; }
        /// <summary>The callouts up now (at most one per agent and one per watcher), and the ones queued behind them.</summary>
        public readonly List<Callout> Callouts = new(), CalloutQueue = new();
        int calloutSeq;
        /// <summary>Board §9: the display delay on a falling stage (s of mission time) and the callouts' holds (s).</summary>
        public const float AttnFallS = 2f, CalloutNoticedS = 2f, CalloutWaryS = 3f, CalloutReportCapS = 6f, CalloutAfterS = 1f, CalloutQueuedS = 2f;

        void ResetAttention() { attnShown.Clear(); calloutSeen.Clear(); Callouts.Clear(); CalloutQueue.Clear(); attnTick = -1; calloutSeq = 0; views?.ResetAttention(); }

        /// <summary>The view a watcher is drawn by: camera/&lt;id&gt;, guard, patrol, unit/&lt;id&gt; (the dial's robots and police), security/0..1 (the
        /// legacy officers); "" for one with no view (the rivals keep no clock).</summary>
        public string AttnView(string w)
        {
            switch (Game.WatcherKind(w))
            {
                case "camera": return "camera/" + w.Substring(7);
                case "guard": return "guard";
                case "patrol": return "patrol";
                case "security":
                    foreach (var u in Game.Units) if (u.Id == w) return u.Patrol ? "patrol" : "unit/" + u.Id;
                    for (int i = 0; i < Game.SecGuns.Length; i++) if (Game.SecGuns[i].Id == w) return "security/" + Math.Min(i, 1);
                    return "";
            }
            return "";
        }

        /// <summary>A watcher's displayed cue now (board §1): its stage (0 calm .. 3 reporting) and the agent it has ("" when calm).</summary>
        public (int stage, string op) Cue(string watcher) => attnShown.TryGetValue(watcher, out var s) ? (s.Stage, s.Op) : (0, "");
        /// <summary>Every watcher with a cue above calm, with its view key.</summary>
        public IEnumerable<(string watcher, string view, int stage, string op)> Cues()
        {
            foreach (var kv in attnShown) if (kv.Value.Stage > 0) yield return (kv.Key, AttnView(kv.Key), kv.Value.Stage, kv.Value.Op);
        }

        /// <summary>Once a frame, after the steps: on a new tick each watcher's displayed stage follows the engine's (up at once, down a stage
        /// after 2 s under it); stage-ups open callouts. Every frame the callouts expire and the queue moves.</summary>
        void AttentionFrame()
        {
            var g = Game; if (g == null || g.Range) return;
            if (g.Tick != attnTick)
            {
                attnTick = g.Tick; int fall = Mathf.RoundToInt(AttnFallS * Game.Hz);
                foreach (var w in g.WatcherIds())
                {
                    var kind = Game.WatcherKind(w); if (kind is "rivals" or "") continue;
                    (int stage, string op) raw = (0, ""); if (!Guard("attention", () => raw = g.Attention(w))) return;
                    if (!attnShown.TryGetValue(w, out var s)) { if (raw.stage == 0) continue; attnShown[w] = s = new AttnShown(); }
                    if (raw.stage >= s.Stage)
                    {
                        int was = s.Stage; string wasOp = s.Op;
                        s.Stage = raw.stage; s.LowFrom = -1; if (raw.stage > 0) s.Op = raw.op;
                        if (raw.stage > was || (raw.stage > 0 && raw.op != wasOp)) StageUp(w, raw.op, raw.stage, raw.stage > was ? was : 0);
                    }
                    else
                    {
                        if (s.LowFrom < 0) s.LowFrom = g.Tick;
                        if (g.Tick - s.LowFrom >= fall) { s.Stage--; s.LowFrom = s.Stage > raw.stage ? g.Tick : -1; if (s.Stage == 0) s.Op = ""; }
                    }
                }
            }
            CalloutFrame();
        }

        /// <summary>A watcher stepped up toward an agent: a callout for each stage newly reached (noticed once per watcher per mission; wary and
        /// reporting once per watcher and agent, as the engine makes them).</summary>
        void StageUp(string w, string op, int stage, int from)
        {
            for (int st = Math.Max(1, from + 1); st <= stage; st++)
            {
                string key = st == 1 ? $"{w}|1" : $"{w}|{op}|{st}";
                if (!calloutSeen.Add(key)) continue;
                if (st < stage) continue; // a jump over a stage: only the highest opens (it replaces the lower at once anyway)
                OpenCallout(new Callout { Watcher = w, View = AttnView(w), Op = op, Stage = st, Seq = ++calloutSeq, Born = Time.unscaledTime, Until = Time.unscaledTime + HoldOf(w, op, st) });
            }
        }

        float HoldOf(string w, string op, int stage)
        {
            if (stage == 1) return CalloutNoticedS;
            if (stage == 2) return CalloutWaryS;
            float left = CalloutReportCapS; // reporting: until the report lands (6 s cap), plus 1 s
            try { foreach (var r in Game.AttentionReportsMade()) if (r.watcher == w && r.op == op && r.state == "pending") left = Mathf.Min(left, (r.landAt - Game.Tick) / (float)Game.Hz / Mathf.Max(1, Speed)); } catch (Exception) { }
            return Mathf.Max(0.5f, left) + CalloutAfterS;
        }

        /// <summary>Board §9 stacking: one callout per agent and one per watcher; a higher stage replaces (the one it replaces queues with its
        /// time left), a lower or equal older one queues behind; the newest wins a tie.</summary>
        void OpenCallout(Callout c)
        {
            if (c.View.Length == 0) return;
            bool blocked = false;
            foreach (var a in Callouts) if ((a.Op == c.Op || a.Watcher == c.Watcher) && a.Stage > c.Stage) { blocked = true; break; }
            if (blocked) { c.Queued = true; CalloutQueue.Add(c); return; }
            for (int i = Callouts.Count - 1; i >= 0; i--)
            {
                var a = Callouts[i]; if (a.Op != c.Op && a.Watcher != c.Watcher) continue;
                Callouts.RemoveAt(i);
                if (a.Watcher != c.Watcher && a.Until > Time.unscaledTime) { a.Queued = true; CalloutQueue.Add(a); } // the same watcher: the higher stage is its news
            }
            Callouts.Add(c);
            while (Callouts.Count > 4) { var o = Callouts[0]; Callouts.RemoveAt(0); o.Queued = true; CalloutQueue.Add(o); }
        }

        void CalloutFrame()
        {
            float now = Time.unscaledTime;
            for (int i = Callouts.Count - 1; i >= 0; i--)
            {
                var c = Callouts[i];
                if (c.Stage == 3 && Game.AttentionReportsMade().Exists(r => r.watcher == c.Watcher && r.op == c.Op && r.state != "pending")) c.Until = Mathf.Min(c.Until, now + CalloutAfterS); // landed: one more second
                if (c.Until > now) continue;
                Callouts.RemoveAt(i);
                // the queued one behind it shows for its remaining time, at most 2 s, if its watcher still has the agent
                int qi = CalloutQueue.FindIndex(q => (q.Op == c.Op || q.Watcher == c.Watcher) && Cue(q.Watcher).stage >= 1 && q.Until > now);
                if (qi >= 0) { var q = CalloutQueue[qi]; CalloutQueue.RemoveAt(qi); q.Queued = false; q.Until = Mathf.Min(q.Until, now + CalloutQueuedS); q.Born = now; if (!Callouts.Exists(a => a.Op == q.Op || a.Watcher == q.Watcher)) Callouts.Add(q); }
            }
            CalloutQueue.RemoveAll(q => q.Until <= now);
        }

        /// <summary>The "+1" on a callout: how many wait behind it (same agent or same watcher), at most 3.</summary>
        public int QueuedBehind(Callout c) { int n = 0; foreach (var q in CalloutQueue) if (q.Op == c.Op || q.Watcher == c.Watcher) n++; return Math.Min(n, 3); }

        /// <summary>A callout clicked (board §9): the camera centres on its watcher; never an order.</summary>
        public void CalloutClick(Callout c) { if (views?.Find(c.View) is Transform t && t) LookAt(t.position); }

        /// <summary>H19's trigger (Fable's text, brief 031): a security watcher's displayed attention at noticed or more toward any agent; rings
        /// nothing (the watcher's own cue shows it), names the agent's portrait.</summary>
        bool AttnHint(out Vector3? ring, out int portrait)
        {
            ring = null; portrait = -1;
            foreach (var (w, view, stage, op) in Cues())
            {
                if (stage < 1 || op.Length == 0) continue;
                portrait = (int)Op.Parse(op); return true;
            }
            return false;
        }
    }
}
