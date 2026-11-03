using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using Pt = Suborned.Sim.Pt;

namespace Suborned.Play
{
    /// <summary>Brief 055 step 1 (Sky, the door guard sunk to the hips in the Office landing: "need to work on the levels"): people stand on the
    /// floor that is drawn (the walk file, PlayInstall.WalkFloor). Checks: the host's ground is the walk file's 0.25 m raster; the door guard
    /// at the Office public entrance stands on the landing (0.93 m drawn, 0.75 m above the pavement; 0.18 m before), his figure there too; the four
    /// crew walk to the entrance and climb the steps (their figures rise past 0.5 m on the way, never sink under the floor). Pictures at yaw 210
    /// (the Office front): `levels-guard-landing`, `levels-crew-steps`.</summary>
    public static class ProofLevels
    {
        static string B(bool b) => b ? "true" : "false";
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Shoot(HandoverHost h, string dir, string name, List<string> log)
        {
            bool was = h.Paused; h.SetPaused(true);
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null;
            log.Add(Proof118.Uncut(h, name, count: false)); // Glint 056 (2026-10-26): the silhouette people with no cut (pass: none), the points against the cap
            h.SetPaused(was);
        }

        public static IEnumerator Handover(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var keepConvo = h.controls.conversations; h.controls.conversations = ConversationScenes.Never;
            if (h.Following) h.FollowToggle();
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; var g = h.Game; var f = h.frame;
            bool fine = f.cell <= 0.26f && f.heights.Length == f.nx * f.ny && f.nx > 0;
            // 1 · the door guard on the landing
            var gp = g.GuardPose()?.pos; float floor = gp.HasValue ? f.World(gp.Value).y : -1f;
            h.TurnSnap(2); var gt = h.Views.Find("guard"); if (gt) h.LookAt(gt.position); yield return Frames(6);
            float fig = gt ? gt.position.y : -1f;
            bool guardOk = fine && Mathf.Abs(floor - 0.93f) <= 0.05f && Mathf.Abs(fig - floor) <= 0.02f;
            log.Add($"levels guard: {(guardOk ? "ok" : "FAILED")}: ground {f.nx}x{f.ny} at {F(f.cell)} m; floor under the guard {F(floor)} m (drawn landing 0.93), figure at {F(fig)} m");
            yield return Shoot(h, dir, "levels-guard-landing", log);
            // 2 · the four crew to the entrance, up the steps
            Pt door; try { door = g.S.At("d:Office entrance"); } catch (System.FormatException) { door = new Pt(6400, -4125); }
            for (int k = 0; k < 4; k++)
            {
                var want = new Pt(door.X - 120 + 80 * k, door.Y + 60); var to = g.S.Grid.Free(want) ? want : g.S.Grid.NearestFree(want) ?? want;
                h.Issue(new Order { Op = Suborned.Sim.Op.Name((OpId)k), Do = "move", Point = to, Run = true });
            }
            float[] top = new float[4], low = new float[4]; for (int k = 0; k < 4; k++) { top[k] = 0f; low[k] = 9f; }
            int ticks = 0, onSteps = -1; float sunk = 0f;
            // stepped until the first agent is 6 m from the door (fast: not timed), then at real time over the steps
            while (ticks++ < 1200 && !g.Over)
            {
                h.RunTo(g.Tick + 5); bool near = false;
                for (int k = 0; k < 4; k++) { var p = g.Ops[k].Pos; long dx = p.X - door.X, dy = p.Y - door.Y; if (dx * dx + dy * dy < 600L * 600L) near = true; }
                if (near) break;
            }
            h.SetPaused(false); float t0 = Time.realtimeSinceStartup; bool shot = false; HostViews.FeetRaisedMax = 0f;
            while (Time.realtimeSinceStartup - t0 < 12f && !g.Over)
            {
                yield return null;
                for (int k = 0; k < 4; k++)
                {
                    var t = h.Views.Find("op/" + Suborned.Sim.Op.Name((OpId)k)); if (!t || !t.gameObject.activeSelf) continue;
                    var bp = f.ToBlock(new Vector2(t.position.x, t.position.z)); float y = t.position.y, under = f.Height(bp.x, bp.y); top[k] = Mathf.Max(top[k], y); low[k] = Mathf.Min(low[k], y);
                    sunk = Mathf.Max(sunk, under - y - 0.12f); // Splice (0.2.113): the floor at the figure's own drawn place, not under its engine position a tick ahead (0.12 m slack kept)
                    if (!shot && y > 0.4f && y < 0.85f) { onSteps = k; }
                }
                if (!shot && onSteps >= 0) { shot = true; var t = h.Views.Find("op/" + Suborned.Sim.Op.Name((OpId)onSteps)); if (t) h.LookAt(t.position); yield return Frames(3); yield return Shoot(h, dir, "levels-crew-steps", log); }
                bool all = true; for (int k = 0; k < 4; k++) all &= top[k] > 0.85f; if (all && shot) break;
            }
            if (!shot) yield return Shoot(h, dir, "levels-crew-steps", log);
            h.SetPaused(true);
            int climbed = 0; for (int k = 0; k < 4; k++) if (top[k] > 0.85f) climbed++;
            bool crewOk = fine && climbed >= 3 && sunk <= 0.05f;
            log.Add($"levels crew: {(crewOk ? "ok" : "FAILED")}: {climbed} of 4 reached the landing (highest {F(top[0])} {F(top[1])} {F(top[2])} {F(top[3])} m), deepest under the floor {F(Mathf.Max(0f, sunk))} m past the slack, a figure on the steps shot: {B(shot)}");
            json[0] = $"{{\"ok\": {B(guardOk && crewOk)}, \"cell\": {F(f.cell)}, \"nx\": {f.nx}, \"ny\": {f.ny}, \"guardFloor\": {F(floor)}, \"guardFigure\": {F(fig)}, \"climbed\": {climbed}, \"tops\": [{F(top[0])}, {F(top[1])}, {F(top[2])}, {F(top[3])}], \"sunkM\": {F(Mathf.Max(0f, sunk))}, \"easeLiftM\": {F(HostViews.FeetRaisedMax)}}}"; // easeLiftM: how far the plain ease would have put a figure under the floor at its own place (lifted onto it since 0.2.114)
            h.TurnSnap(0); h.controls.conversations = keepConvo; h.Begin(); yield return null;
        }
    }
}
