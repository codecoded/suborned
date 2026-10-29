using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Suborned.Play.Tests
{
    /// <summary>Brief 077: runs KitLods077Tests synchronously inside one batch step (unityrun.ts passes -quit, which -runTests does not take), the
    /// results to -testOut (JSON lines: name, result, message).</summary>
    public static class Run077Tests
    {
        sealed class Cb : ICallbacks
        {
            public readonly List<string> Lines = new();
            public void RunStarted(ITestAdaptor t) { }
            public void RunFinished(ITestResultAdaptor r) { }
            public void TestStarted(ITestAdaptor t) { }
            public void TestFinished(ITestResultAdaptor r)
            {
                if (r.Test.IsSuite) return;
                Lines.Add($"{{\"test\": \"{r.Test.Name}\", \"result\": \"{r.TestStatus}\", \"message\": {Json(r.Message ?? "")}}}");
            }
            static string Json(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "") + "\"";
        }

        public static void Run()
        {
            var args = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(args, "-testOut");
            var file = i >= 0 ? args[i + 1] : "out/scratch/latch/077build/tests.jsonl";
            var api = ScriptableObject.CreateInstance<TestRunnerApi>(); var cb = new Cb(); api.RegisterCallbacks(cb);
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, groupNames = new[] { "^Suborned\\.Play\\.Tests\\.(KitLods077Tests|LightPhaseGlow077Tests)" } }) { runSynchronously = true });
            var path = Path.Combine(Suborned.World.HarbourWorldBuilder.RepoRoot, file); Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, cb.Lines);
            Debug.Log($"[077 tests] {cb.Lines.Count} tests: {cb.Lines.Count(l => l.Contains("\"Passed\""))} passed -> {file}");
        }
    }
}
