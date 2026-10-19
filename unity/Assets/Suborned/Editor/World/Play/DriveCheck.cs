using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.World.Play
{
    /// <summary>Brief 025's controls test for the driven agent (Latch), run in Play against the live host (no engine step, nothing sent: the
    /// controller only, through HandoverHost.DriveProbe). At camera yaw 30, 120, 210 and 300 (the follow camera turned there): W walks the
    /// way the camera looks (the angle between the walk and the camera's ground forward); a diagonal (W+D) is as fast as a straight (W); and
    /// against a wall a diagonal push slides along it (the walk grid, x then y, as the engine resolves a step). Writes
    /// briefs/025/latch/controls.json and returns the same text.</summary>
    public static class DriveCheck
    {
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        public static string Controls()
        {
            var h = Object.FindAnyObjectByType<HandoverHost>();
            if (h == null || h.Game == null) return "no host (enter Play first)";
            var g = h.Game; var grid = g.S.Grid;
            if (!h.Driving) { h.Select(OpId.Scout, false); h.DriveToggle(); }
            if (!h.Driving) return "could not take control";
            bool Free(int i, int j) => grid.FreeCell(i, j);
            var me = g.Ops[h.DriveAgent].Pos; int ci = grid.CI(me.X), cj = grid.CJ(me.Y);
            // an open spot: every cell within 6.5 m free (a 2 s run is 5.6 m), nearest the agent
            Vector2? open = null; float best = float.MaxValue;
            for (int i = ci - 80; i <= ci + 80; i++) for (int j = cj - 80; j <= cj + 80; j++)
            {
                bool ok = true; for (int a = -13; a <= 13 && ok; a++) for (int b = -13; b <= 13 && ok; b++) ok = Free(i + a, j + b);
                if (!ok) continue; float d = (i - ci) * (i - ci) + (j - cj) * (j - cj);
                if (d < best) { best = d; open = new Vector2(grid.X0 + i * grid.Cell + grid.Cell / 2, grid.Y0 + j * grid.Cell + grid.Cell / 2); }
            }
            if (open == null) return "no open spot within 40 m";
            float pace = g.R.CellSpeed * Game.Hz / 10f; // cm/s at a walk
            var sb = new StringBuilder(); sb.Append("{\n  \"schema\": \"suborned.drive-controls/1\",\n  \"walkCmPerS\": ").Append(F(pace)).Append(",\n  \"open\": [").Append(F(open.Value.x)).Append(", ").Append(F(open.Value.y)).Append("],\n  \"yaws\": [\n");
            bool pass = true; var cam = h.View; var lines = new List<string>();
            foreach (float yaw in new[] { 30f, 120f, 210f, 300f })
            {
                // the follow camera turned to look along this yaw (FollowFace places it at once)
                var ag = h.Views.Find("op/" + Op.Name((OpId)h.DriveAgent)).position;
                h.FollowFace(ag + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 10f);
                var cf = cam.transform.forward; cf.y = 0f; cf.Normalize();
                float cy = h.DriveCameraYaw;
                var (wEnd, wDir) = h.DriveProbe(open.Value, new Vector2(0, 1), false, 2f, cy);
                var (dEnd, _) = h.DriveProbe(open.Value, new Vector2(1, 1).normalized, false, 2f, cy);
                var (dEnd2, _) = h.DriveProbe(open.Value, new Vector2(1, 1), false, 2f, cy); // raw keys W+D (not normalised)
                var (sEnd, sDir) = h.DriveProbe(open.Value, new Vector2(0, -1), false, 2f, cy);
                var (rEnd, _) = h.DriveProbe(open.Value, new Vector2(0, 1), true, 2f, cy);
                float ang = Vector3.Angle(wDir, cf), back = Vector3.Angle(sDir, -cf);
                float ws = (wEnd - open.Value).magnitude, ds = (dEnd2 - open.Value).magnitude, rs = (rEnd - open.Value).magnitude;
                bool ok = ang < 2f && back < 2f && Mathf.Abs(ds / ws - 1f) < 0.01f && Mathf.Abs(Mathf.DeltaAngle(cy, yaw)) < 0.5f && Mathf.Abs(rs / ws - 2f) < 0.05f;
                pass &= ok;
                lines.Add($"    {{\"yaw\": {F(yaw)}, \"cameraYaw\": {F(Mathf.Repeat(cy, 360f))}, \"wVsCameraDeg\": {F(ang)}, \"sVsBehindDeg\": {F(back)}, \"w2sCm\": {F(ws)}, \"diagonal2sCm\": {F(ds)}, \"diagonalOverStraight\": {F(ds / ws)}, \"run2sCm\": {F(rs)}, \"runOverWalk\": {F(rs / ws)}, \"pass\": {(ok ? "true" : "false")}}}");
            }
            sb.Append(string.Join(",\n", lines)).Append("\n  ],\n");
            // the wall: a free column with a wall on its +x side for 4 m; a diagonal push (+x, +y in the block frame) slides along it
            (int i, int j)? wall = null; best = float.MaxValue;
            for (int i = ci - 80; i <= ci + 80; i++) for (int j = cj - 80; j <= cj + 80; j++)
            {
                bool ok = true; for (int b = -1; b <= 9 && ok; b++) ok = Free(i, j + b) && Free(i - 1, j + b) && !Free(i + 1, j + b);
                if (!ok) continue; float d = (i - ci) * (i - ci) + (j - cj) * (j - cj);
                if (d < best) { best = d; wall = (i, j); }
            }
            if (wall is (int wi, int wj))
            {
                var from = new Vector2(grid.X0 + wi * grid.Cell + grid.Cell / 2, grid.Y0 + wj * grid.Cell + grid.Cell / 2);
                float yaw = h.CameraYawFor(new Vector2(1, 1));
                var (end, _) = h.DriveProbe(from, new Vector2(0, 1), false, 2f, yaw);
                float wallX = grid.X0 + (wi + 1) * grid.Cell; float slid = end.y - from.y, stopped = wallX - end.x;
                bool ok = stopped > 0f && stopped < 3f && slid > pace * 2f * 0.7071f * 0.85f; // stops at the wall; keeps the push's y share (eased in)
                pass &= ok;
                sb.Append($"  \"wall\": {{\"from\": [{F(from.x)}, {F(from.y)}], \"wallX\": {F(wallX)}, \"end\": [{F(end.x)}, {F(end.y)}], \"cmShortOfWall\": {F(stopped)}, \"slidCm\": {F(slid)}, \"expectedYShareCm\": {F(pace * 2f * 0.7071f)}, \"pass\": {(ok ? "true" : "false")}}},\n");
            }
            else { sb.Append("  \"wall\": null,\n"); pass = false; }
            sb.Append($"  \"pass\": {(pass ? "true" : "false")}\n}}\n");
            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "briefs", "025", "latch")); Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "controls.json"), sb.ToString());
            return sb.ToString();
        }
    }
}
