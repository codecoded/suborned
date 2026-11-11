using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 110 (Aster, after an hour lost: the proof ran from a scratch build one folder deeper than out/build/play, ProofBook.Route found no
    /// route file, its exception ended the proof's coroutine chain, and the player waited with nothing written; Sky's rule for orders, "no hidden waits",
    /// holds for the proof too). The proof fails fast, never waits:
    ///  · before any section: the inputs the sections read from the repo (the routes, the mission, briefs/027) must be where the proof will look; any
    ///    missing, proof.json says `error` and names every path it looked for, and the player quits (exit 2), within seconds of the start;
    ///  · during: an exception thrown out of a proof section (a stack through Suborned.Play.Proof*, GameProof, Shots*) has ended its coroutine chain, so
    ///    the proof can't finish: proof.json says `error` with the message and its first frames, and the player quits (exit 2) at once.
    /// Sections catch nothing on purpose (no proof logs an exception it means to survive), so any such exception is a death, not noise.</summary>
    public sealed partial class GameProof
    {
        static readonly string[] Needs = { "contracts/missions/handover/routes/route-a.json", "contracts/missions/handover/mission.json", "briefs/027" };
        bool died;

        bool FailFast()
        {
            Application.logMessageReceived += Died;
            var root = Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))) ?? ""; var missing = new List<string>();
            foreach (var n in Needs) { var p = Path.Combine(root, n); if (!File.Exists(p) && !Directory.Exists(p)) missing.Add(p); }
            if (missing.Count == 0) { log.Add($"inputs: the repo at {root}"); return true; }
            var why = $"proof inputs missing: looked for {string.Join(", ", missing)} (the repo is found by walking up from {Application.dataPath} to a folder holding briefs/027)";
            log.Add(why); Debug.LogError("GameProof: " + why); Write(null, "error"); Application.Quit(2); return false;
        }

        void Died(string message, string stack, LogType type)
        {
            if (died || type != LogType.Exception || stack == null) return;
            if (!(stack.Contains("Suborned.Play.Proof") || stack.Contains("Suborned.Play.GameProof") || stack.Contains("Suborned.Play.Shots"))) return;
            died = true; var ours = new List<string>(); foreach (var l in stack.Split('\n')) if (l.Contains("Suborned.") && ours.Count < 3) ours.Add(l.Trim()); var at = string.Join(" | ", ours);
            log.Add($"proof died: {message} at {at}"); Debug.LogError("GameProof: a section threw; its chain has ended, so the proof stops now (proof.json: error)");
            try { Write(null, "error"); } catch (System.Exception) { }
            Application.Quit(2);
        }

        void OnDestroy() => Application.logMessageReceived -= Died;
    }
}
