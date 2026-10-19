// DP1, the recorded driven session (brief 025; game:check's replay proof). The session (Fixtures/drive-session.script.json) was played the way a
// host plays it: a float controller moved by camera-relative keys at a jittered 60 fps, its position rounded to centimetres and handed over as one
// drive sample a tick, pulled back to the engine's pose when corrected. It is saved with its record (Script.Write: inputs, engine, start hash,
// every checkpoint, outcome) and embedded in the DLL, so every runtime that runs the scenarios (the CLI, the Editor, the built player) replays it
// from its start state and order log and must match the record. Re-record it when the inputs or the engine move: suborned-sim drive-session.
using System;
using System.IO;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        public const string DriveSessionResource = "Suborned.Sim.Fixtures.drive-session.script.json";
        static Suborned.Sim.Script? driveSession; static string? driveSessionError;
        static Suborned.Sim.Script? DriveSession()
        {
            if (driveSession != null || driveSessionError != null) return driveSession;
            try
            {
                using var st = typeof(Scenarios).Assembly.GetManifestResourceStream(DriveSessionResource);
                if (st == null) { driveSessionError = "the recorded driven session is not embedded (Fixtures/drive-session.script.json)"; return null; }
                using var rd = new StreamReader(st); driveSession = Suborned.Sim.Script.Parse(rd.ReadToEnd());
            }
            catch (Exception e) { driveSessionError = e.GetType().Name + ": " + e.Message; }
            return driveSession;
        }
        static Start DrivenSessionStart() => DriveSession()?.Start ?? new Start();

        static void DrivenSessionProof(Run r)
        {
            var s = DriveSession();
            if (!r.Is(s != null && s.Expect != null, $"the recorded session loads with its record{(driveSessionError != null ? ": " + driveSessionError : "")}")) return;
            var g = r.G; var x = s!.Expect!;
            r.Eq(x.Engine, Engine.Version, "recorded on this engine (else re-record: suborned-sim drive-session)");
            r.Eq(Hash.Hex(g.StartHash), x.Start, "the same start state and inputs (else re-record: suborned-sim drive-session)");
            int drives = 0;
            foreach (var o in s.Orders)
            {
                if (o.Tick > s.Until) break;
                g.RunTo(o.Tick); if (g.Over && g.Tick != o.Tick) break;
                if (g.Tick == o.Tick) { g.Issue(o); if (o.Do == "drive") drives++; }
            }
            g.RunTo(s.Until); g.Finish();
            r.Is(drives >= Game.T(10), $"a driven session: {drives} drive samples");
            int n = Math.Min(x.Checkpoints.Count, g.Checkpoints.Count), bad = -1;
            for (int i = 0; i < n && bad < 0; i++) if (x.Checkpoints[i].tick != g.Checkpoints[i].tick || x.Checkpoints[i].hash != Hash.Hex(g.Checkpoints[i].hash)) bad = i;
            r.Is(bad < 0 && x.Checkpoints.Count == g.Checkpoints.Count, bad < 0 ? $"every checkpoint matches the record ({g.Checkpoints.Count})" : $"checkpoint {bad} differs at tick {g.Checkpoints[bad].tick}");
            r.Eq(g.Result.ToString().ToLowerInvariant(), x.Outcome, "the outcome matches the record");
        }
    }
}
