using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using Pt = Suborned.Sim.Pt;

namespace Suborned.Play
{
    /// <summary>Brief 101, the square's lamp mast (Sky's pick B; Vela's piece, HostViews.Mast): pictures for Sky and their checks, in r101 (not gated).
    ///  1 · the opening view at 0:00: the mast over the four with its "?" (not yet watched), its lenses away from them;
    ///  2 · the Scout watches it from her start, then the pointer on its pods: its three cones beside the cell, the cell behind the lenses;
    ///  3 · the Enforcer stands in its first head's view: that head's lens takes the cue (noticed, then wary), the other two as built; a close picture.</summary>
    public static class Proof101Mast
    {
        const string Id = "mast-z0", View = "camera/" + Id, Watcher = "camera:" + Id;
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Shoot(HandoverHost h, string dir, string name, List<string> log, string state)
        {
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), state)); yield return null; while (HudToolkit.ShotBusy) yield return null;
        }
        static bool OnScreen(HandoverHost h, Vector3 w) { var v = h.View.WorldToViewportPoint(w); return v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f; }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            void Check(string name, bool good, string words) { parts.Add($"\"{name}\": {{\"ok\": {B(good)}, \"words\": \"{J(words)}\"}}"); ok &= good; log.Add($"r101 mast {name}: {(good ? "ok" : "FAILED")}: {words}"); }
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return Frames(3); var g = h.Game;
            var cam = g.S.Cameras.Find(c => c.Id == Id);
            if (cam == null || !h.Views.IsMast(View)) { Check("mast", false, $"no lamp mast: camera {(cam == null ? "missing" : "found")}, piece {(h.Views.IsMast(View) ? "found" : "missing")} (worldMasts {h.worldMasts.Length})"); json[0] = $"{{\"ok\": false, {string.Join(", ", parts)}}}"; yield break; }

            // 1 · the opening view: the mast and its "?"
            h.Select(OpId.Scout, false); yield return new WaitForSecondsRealtime(0.4f);
            var top = h.Views.CamTop(cam); bool unwatched = g.Known(Watcher) is not { Sight: true };
            yield return Shoot(h, dir, "r101-mast-start", log, "rest");
            Check("start", unwatched && OnScreen(h, top), $"unwatched {unwatched}; its top {top} on screen {OnScreen(h, top)} at yaw 30, zoom {h.View.orthographicSize:0.#}");

            // 2 · watched from the Scout's start, then hovered: its cones beside the cell
            var o = new Order { Tick = g.Tick, Op = "scout", Do = "watch", Target = Watcher }; h.GiveScripted(o);
            for (int i = 0; i < 900 && g.Known(Watcher) is not { Sight: true } && !g.Over; i++) h.RunTo(g.Tick + 1);
            bool watched = g.Known(Watcher) is { Sight: true }; string walked = g.Ops[(int)OpId.Scout].Pos.ToString();
            var pods = h.Views.Find(View).position + Vector3.up * h.Views.PickUp(View);
            h.Select(OpId.Fixer, false); HandoverHost.PointerWorld = pods; yield return Frames(4); yield return new WaitForSecondsRealtime(0.3f);
            string focus = h.FocusWatcher ?? "", name = h.HoverName;
            yield return Shoot(h, dir, "r101-mast-cones", log, "active"); HandoverHost.PointerWorld = null;
            Check("cones", watched && focus == Watcher && name.Length > 0, $"watched at {HandoverHost.MissionClock(g.Tick)} (the Scout at {walked}); the pointer on the pods: hover '{name}', cones of '{focus}'");

            // 3 · the lens cue: the Enforcer stands in the first head's view, 6 m out
            Pt? spot = null; var f = cam.Facings[0]; double len = System.Math.Sqrt((double)f.X * f.X + (double)f.Y * f.Y);
            for (int d = 500; d <= 900 && spot == null; d += 50)
            {
                var p = g.S.Grid.NearestFree(cam.At + new Pt((int)(f.X * d / len), (int)(f.Y * d / len)));
                if (p is Pt q && g.InSight(Watcher, q, false)) spot = q;
            }
            int head = -1, cued = 0, stage = 0;
            if (spot is Pt at)
            {
                h.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "move", Point = at }); h.Select(OpId.Enforcer, false);
                for (int i = 0; i < 1500 && h.Cue(Watcher).stage < 1 && !g.Over; i++) { h.RunTo(g.Tick + 1); if (i % 20 == 0) yield return null; }
                yield return Frames(3); (head, cued) = h.Views.MastCue(View); stage = h.Cue(Watcher).stage;
                h.LookAt(h.Views.Find(View).position); yield return Frames(3);
                yield return Shoot(h, dir, "r101-mast-noticed", log, "rest");
                var d = h.View.transform.forward; var crown = h.Views.Find(View).position + Vector3.up * h.Views.PickUp(View); h.LookAt(crown + d * (crown.y / Mathf.Max(0.1f, -d.y))); // the ground point the view's ray through the pods meets: the pods at the centre
                float keep = h.View.orthographicSize; h.View.orthographicSize = 4f; yield return Frames(3);
                yield return Shoot(h, dir, "r101-mast-lens", log, "rest");
                for (int i = 0; i < 1500 && h.Cue(Watcher).stage < 2 && !g.Over; i++) { h.RunTo(g.Tick + 1); if (i % 20 == 0) yield return null; }
                yield return Frames(3); yield return Shoot(h, dir, "r101-mast-lens-wary", log, "rest");
                h.View.orthographicSize = keep;
            }
            Check("lens", spot.HasValue && stage >= 1 && head == 0 && cued == 1, $"the Enforcer at {spot?.ToString() ?? "no spot in head 0's view"}; cue stage {stage}; the head whose lens wears it {head}, lenses cued {cued} of {cam.Facings.Length}");

            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }
}
