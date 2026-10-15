using System.Linq;
using NUnit.Framework;
using Suborned.Play;

namespace Suborned.Play.Tests
{
    /// <summary>§11 (brief 011 D): the engine DLL Unity loads plays every scenario to the headless CLI's checkpoint hashes, and replays each
    /// identically. Needs the bundle from `bun tools/sim/unity.ts`. Run: Window/General/Test Runner (EditMode), or the run_tests tool.</summary>
    public sealed class SimScenarioTests
    {
        [Test]
        public void EveryScenarioMatchesHeadlessAndReplays()
        {
            var b = SimBundle.Load();
            var rows = SimCheck.RunAll(b.Inputs, SimBundle.Headless());
            Assert.That(rows.Count, Is.GreaterThanOrEqualTo(28), "the 28 scenarios and the N-cases");
            var bad = rows.Where(r => !r.Match || !r.ReplayIdentical || r.ChecksFailed > 0 || r.Why.Length > 0).Select(r => $"{r.Id}: {r.Why} (replay {r.ReplayIdentical}, checks failed {r.ChecksFailed})").ToList();
            Assert.That(bad, Is.Empty, string.Join("\n", bad));
        }
    }
}
