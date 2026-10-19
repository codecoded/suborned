// suborned-sim drive-session [out.json]: record the driven session DP1 replays (brief 025). A stand-in for the Unity host, deliberately
// host-shaped: a float controller (double metres) moved per frame at a jittered 60 fps by camera-relative keys (yaw 30, the opening view),
// walk 1.4 m/s or run 2.8 m/s, with no collision of its own (worse than Latch's grid-backed mover, so the engine corrects it often); each tick
// it hands the engine its position rounded to centimetres, one drive sample, then pulls itself back to the engine's pose if they differ by more
// than 5 cm. Floats never reach the engine: the record holds only the samples. Writes sim/Suborned.Sim/Fixtures/drive-session.script.json.
using System;
using System.IO;
using Suborned.Sim;

static class DriveSession
{
    public static int Record(Engine.Inputs inputs, string root, string? outPath)
    {
        var g = Engine.New(inputs, new Start());
        var id = OpId.Scout; var op = g.Ops[(int)id];
        double cx = op.Pos.X / 100.0, cy = op.Pos.Y / 100.0; // the controller, metres
        double yaw = 30 * Math.PI / 180, acc = 0, t = 0; uint seed = 2025;
        double Frame() { seed = seed * 1664525 + 1013904223; return 1.0 / 60 + ((int)((seed >> 8) % 7) - 3) / 1000.0; } // 13.7..19.7 ms
        // the keys, by mission second: (forward, right, shift)
        (int f, int rt, bool run) Keys(double s) =>
            s < 4 ? (1, 0, false) : s < 7 ? (1, 1, true) : s < 9 ? (0, -1, false) : s < 14 ? (-1, 0, true) : s < 15 ? (0, 0, false) : (0, 0, false);
        bool released = false, clicked = false;
        while (g.Tick < Game.T(30) && !g.Over)
        {
            double dt = Frame(); t += dt; acc += dt;
            var (f, rt, run) = Keys(g.Tick / 10.0);
            if (!released && (f != 0 || rt != 0))
            {
                // camera-relative: forward is the camera's heading on the ground, right its perpendicular (block frame: x south, y east)
                double fx = Math.Cos(yaw), fy = Math.Sin(yaw), rx = -fy, ry = fx;
                double vx = f * fx + rt * rx, vy = f * fy + rt * ry, len = Math.Sqrt(vx * vx + vy * vy), sp = run ? 2.8 : 1.4;
                cx += vx / len * sp * dt; cy += vy / len * sp * dt;
            }
            while (acc >= 0.1)
            {
                acc -= 0.1;
                if (g.Tick == Game.T(15) && !released) { g.Issue(g.DriveRelease(id)); released = true; }
                else if (!released)
                {
                    var at = new Pt((int)Math.Round(cx * 100), (int)Math.Round(cy * 100));
                    if (at != op.Pos || g.Tick == 0) g.Issue(g.DriveSample(id, at, run, (int)Math.Round((yaw * 180 / Math.PI) + (f < 0 ? 180 : 0) + 360) % 360));
                }
                if (released && !clicked && g.Tick == Game.T(16)) { g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "move", To = "d:Courtyard" }); clicked = true; }
                g.Step();
                var pose = g.DrivePoseOf(id);
                if (pose.Driven && Math.Abs(pose.Pos.X - cx * 100) + Math.Abs(pose.Pos.Y - cy * 100) > 5) { cx = pose.Pos.X / 100.0; cy = pose.Pos.Y / 100.0; } // pulled back
            }
        }
        g.Finish();
        int drives = 0, corrected = op.DriveHeld; foreach (var (o, _, _, _) in g.Orders) if (o.Do == "drive") drives++;
        var path = outPath ?? System.IO.Path.Combine(root, "sim/Suborned.Sim/Fixtures/drive-session.script.json");
        File.WriteAllText(path, Script.Write(g, inputs.Hash) + "\n");
        Console.WriteLine($"drive-session: {drives} drive orders, {corrected} corrected, {g.Checkpoints.Count} checkpoints, {g.Result} at {Report.Clock(g.Tick)} → {path}");
        return 0;
    }
}
