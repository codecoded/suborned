using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using OpId = Suborned.Sim.OpId;
using Pt = Suborned.Sim.Pt;
using Target = Suborned.Play.HandoverHost.Target;
using static Suborned.Play.Proof085;

namespace Suborned.Play
{
    /// <summary>Sky on 0.2.212, by real input (Mouse2), on the patrol: "I didn't seem to have them all aiming" and "the action ring doesn't come up" in third person.
    ///  · r085ringFireMap, r085ringFireThird: a ring opened and its Fire wedge held with the squad selected: every able agent takes the order (the agent in hand too), the busy Scout says why, the wedge's faces and the right hold's are who fired, one card, nobody leaves the squad.
    ///  · r085tpLeftTap, r085tpRightTap: the Enforcer in hand, holstered and drawn: a left tap opens the ring, quick and with a trackpad's 10 px; a right tap opens nothing, as on the map (Sky 2026-11-02: "same"); r085tpHoldStays: a right press let go at 0.25 s still cancels the fire charge and opens nothing.
    ///  · r085tpFireFromRing: Fire taken from that ring. r085mapStays: the map's left click rings and its right tap does nothing.</summary>
    public static class Proof085Squad
    {
        static readonly Target Patrol = new Target { Kind = "patrol", Key = "patrol", Id = "patrol", Label = "the patrol" };

        /// <summary>A screen point that picks the patrol now (a camera's mast can stand in front of his head).</summary>
        static Vector2 OnPatrol(HandoverHost h)
        {
            var w = h.TargetWorld(Patrol) ?? Vector3.zero; Vector2 first = default; bool any = false;
            foreach (var up in new[] { 1f, 0.6f, 1.4f, 0.3f })
            {
                Vector2 s = h.View.WorldToScreenPoint(w + Vector3.up * up); if (!any) { first = s; any = true; }
                if (!HandoverHost.OverHud(s) && h.ProbeAt(s).StartsWith("left patrol/patrol")) return s;
            }
            return first;
        }

        /// <summary>A fresh run with the given agents walked up to the patrol (the Scout first sent to Watch Inez when `scoutBusy`), the clock stopped.</summary>
        static IEnumerator Close(HandoverHost h, bool scoutBusy, params int[] agents)
        {
            yield return Fresh(h); var g = h.Game; h.SetPaused(false); // running: paused, a walk only queues as a ghost
            if (scoutBusy) { h.GiveWatchScripted(OpId.Scout, "waitress"); for (int i = 0; i < 300 && g.Ops[0].Busy == null; i++) h.RunTo(g.Tick + 5); }
            for (int pass = 0; pass < 3; pass++)
            {
                var at = g.PosOf("patrol").Value; h.GroupWalk(new Pt(at.X + 500, at.Y + 300), false, null, new List<int>(agents));
                for (int i = 0; i < 120; i++) { bool on = false; foreach (var k in agents) on |= g.Ops[k].Walk != null; if (!on) break; h.RunTo(g.Tick + 5); }
            }
            h.SetPaused(true); h.LookAt(h.TargetWorld(Patrol) ?? Vector3.zero); yield return Frames(4);
        }

        /// <summary>A press and release of that long, with `move` px of travel half way; the pointer stays until the release is read.</summary>
        static IEnumerator Tap(Vector2 p, bool left, bool right, float s, float move = 0f)
        {
            Mouse2(p); yield return Frames(3); Mouse2(p, left, right); float t0 = Time.unscaledTime; bool moved = false; var q = p;
            while (Time.unscaledTime - t0 < s) { if (move > 0f && !moved && Time.unscaledTime - t0 > s / 2f) { moved = true; q = p + new Vector2(move, 0f); Mouse2(q, left, right); } yield return null; }
            Mouse2(q); yield return Frames(4);
        }

        /// <summary>The open ring's Fire wedge, pointer on it, held by the left button for its 0.5 s.</summary>
        static IEnumerator HoldFireWedge(HandoverHost h)
        {
            if (h.RingWedgeScreen("fire") is not Vector2 w) yield break;
            Mouse2(w); yield return Frames(4); Mouse2(w, left: true); yield return new WaitForSecondsRealtime(0.8f); Mouse2(w); yield return Frames(4);
        }

        static IEnumerator Squad(HandoverHost h, string name, bool third, System.Action<string, bool, string> row)
        {
            yield return Close(h, true, 1, 2, 3); var g = h.Game; h.SelectAll(); h.TakeInSquad(3);
            if (third) { h.SetPaused(false); yield return Press(Key.T); yield return new WaitForSecondsRealtime(0.7f); h.FollowFace(h.TargetWorld(Patrol) ?? Vector3.zero); yield return Frames(3); h.SetPaused(true); yield return Frames(3); }
            var p = OnPatrol(h); string probe = h.ProbeAt(p); int ord0 = g.Orders.Count, log0 = h.FireLog.FindAll(x => x.Contains("way-fire")).Count, card0 = h.Feed.FindAll(c => c.Title == "Going loud").Count;
            yield return Tap(p, true, false, 0.08f);
            string ring = h.RingWords; var faces = new List<int>(); if (h.Ring != null) foreach (var s in h.Ring.Segs) if (s.Row?.I?.Order?.Do == "fire") foreach (var k in s.By) faces.Add((int)k);
            var pill = h.FireShooters(Patrol); yield return HoldFireWedge(h);
            var took = new List<int>(); int fires = 0; for (int k = 0; k < 4; k++) if (g.Ops[k].Gun.Order == "patrol") took.Add(k);
            for (int i = ord0; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "fire") fires++;
            faces.Sort(); pill.Sort(); string scout = h.FireRefusalWord(0); int cards = h.Feed.FindAll(c => c.Title == "Going loud").Count - card0, ways = h.FireLog.FindAll(x => x.Contains("way-fire")).Count - log0;
            bool saidAll = true; string each = ""; for (int k = 0; k < 4; k++) { bool has = took.Contains(k); string w = h.FireRefusalWord(k); saidAll &= has || w.Length > 0; each += $"{k}:{(has ? "fires" : $"'{w}'/{h.FireRefusalOf(k).code}")}@{(g.PosOf("patrol") is Pt tp ? Suborned.Sim.G.Dist(g.Ops[k].Pos, tp) / 100 : -1)}m "; }
            bool want = string.Join(",", took) == "1,2,3" && fires == 3 && string.Join(",", faces) == "1,2,3" && string.Join(",", pill) == "1,2,3" && scout.Length > 0 && saidAll && cards <= 1 && ways == 1 && h.Loud == null && h.Selected[1] && h.Selected[2] && h.Selected[3] && (!third || h.Driving);
            row(name, want, $"ring [{ring}] at {probe}; faces {string.Join(",", faces)}, the hold's {string.Join(",", pill)}; took {string.Join(",", took)}, {fires} orders, {cards} card, {ways} give; {each}; driving {h.Driving}");
            if (h.Driving) h.DriveLetGoToMap(); yield return Frames(2);
        }

        public static IEnumerator Run(HandoverHost h, System.Action<string, bool, string> row)
        {
            yield return Squad(h, "r085ringFireMap", false, row);
            yield return Squad(h, "r085ringFireThird", true, row);

            // ---- the Enforcer in hand, on the patrol, as Sky stood
            yield return Close(h, false, 3); var g = h.Game; h.Select(OpId.Enforcer, false); h.SetPaused(false); yield return Press(Key.T); yield return new WaitForSecondsRealtime(0.7f); h.FollowFace(h.TargetWorld(Patrol) ?? Vector3.zero); yield return Frames(3); h.SetPaused(true); yield return Frames(3);
            bool inHand = h.Driving && h.DriveAgent == 3; var said = new List<string>();
            IEnumerator Case(bool left, float s, float move, bool[] ok)
            {
                var at = OnPatrol(h); yield return Tap(at, left, !left, s, move);
                ok[0] = left ? h.Ring != null && h.Ring.Plan.T.Kind == "patrol" && h.Ring.Segs.Exists(x => x.Row?.I?.Order?.Do == "fire") : h.Ring == null; said.Add($"{(left ? "left" : "right")} {s * 1000f:0} ms +{move:0}: [{h.RingWords}]"); h.CloseRing(); Mouse2(new Vector2(Screen.width * .5f, 6f)); yield return Frames(3);
            }
            var r = new bool[1]; bool lefts = inHand, rights = inHand; string state = "holstered";
            foreach (var stage in new[] { 0, 1 })
            {
                foreach (var (s, m) in new[] { (0.1f, 0f), (0.15f, 10f) }) { yield return Case(true, s, m, r); lefts &= r[0]; }
                foreach (var (s, m) in new[] { (0.1f, 0f), (0.17f, 0f), (0.1f, 10f) }) { yield return Case(false, s, m, r); rights &= r[0]; }
                if (stage == 0) { state = g.Ops[3].Gun.Drawn ? "drawn" : "holstered"; h.Issue(new Suborned.Sim.Order { Tick = g.Tick, Op = "enforcer", Do = "draw" }); h.SetPaused(false); yield return new WaitForSecondsRealtime(1f); h.SetPaused(true); yield return Frames(3); h.FollowFace(h.TargetWorld(Patrol) ?? Vector3.zero); yield return Frames(3); state += " then " + (g.Ops[3].Gun.Drawn ? "drawn" : "holstered"); }
            }
            row("r085tpLeftTap", lefts, $"{state}: {string.Join("; ", said.FindAll(x => x.StartsWith("left")))}");
            row("r085tpRightTap", rights, $"{state}: {string.Join("; ", said.FindAll(x => x.StartsWith("right")))}");
            { var at = OnPatrol(h); int c0 = h.HoldCancels; yield return Tap(at, false, true, 0.27f); row("r085tpHoldStays", h.Ring == null && h.HoldCancels == c0 + 1 && g.Ops[3].Gun.Order.Length == 0, $"ring {h.Ring != null}, cancels +{h.HoldCancels - c0}, status '{h.Status}'"); h.CloseRing(); yield return Frames(2); }
            { var at = OnPatrol(h); yield return Tap(at, true, false, 0.1f); string ways = h.RingWords; bool hasFire = h.RingWedgeScreen("fire").HasValue; yield return HoldFireWedge(h); row("r085tpFireFromRing", hasFire && g.Ops[3].Gun.Order == "patrol" && h.Driving && h.Ring == null, $"ring [{ways}], gun order '{g.Ops[3].Gun.Order}', driving {h.Driving}"); }
            if (h.Driving) h.DriveLetGoToMap(); yield return Frames(2);

            // ---- the map keeps its controls
            yield return Close(h, false, 3); h.Select(OpId.Enforcer, false); yield return Frames(2); var mp = OnPatrol(h); int rt0 = h.RightTaps; yield return Tap(mp, false, true, 0.1f); bool rightNone = h.Ring == null && h.RightTaps == rt0 + 1; yield return Tap(OnPatrol(h), true, false, 0.1f); bool leftRing = h.Ring != null && h.Ring.Plan.T.Kind == "patrol";
            row("r085mapStays", rightNone && leftRing, $"right tap ring none {rightNone}; left click [{h.RingWords}]"); h.CloseRing(); Keys(); Mouse2(new Vector2(Screen.width * .5f, Screen.height * .5f)); yield return Frames(2);
        }
    }
}
