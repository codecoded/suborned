using NUnit.Framework;
using UnityEngine;

namespace Suborned.Play.Tests
{
    /// <summary>Brief 077 (Latch; Glint, light.json night.emissive globeOpal): LightPhase drives a glow at an absolute level per phase (day 0.06, evening
    /// 0.06, night 2.5), _EmissionColor = colour x level with alpha = level, and puts the day level back.</summary>
    public sealed class LightPhaseGlow077Tests
    {
        [Test]
        public void GlobeOpalAtItsAbsoluteLevels()
        {
            var go = new GameObject("phase"); var lp = go.AddComponent<LightPhase>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            var hue = new Color(1f, .88f, .64f, 1f);
            lp.glows = new[] { new LightPhase.Glow { material = mat, colour = hue, day = .06f, evening = .06f, night = 2.5f } };
            try
            {
                lp.Set(true); var c = mat.GetColor("_EmissionColor");
                Assert.That(c.a, Is.EqualTo(2.5f).Within(1e-5f), "night level"); Assert.That(c.r, Is.EqualTo(2.5f).Within(1e-4f)); Assert.That(mat.IsKeywordEnabled("_EMISSION"), Is.True);
                lp.SetEvening(); Assert.That(mat.GetColor("_EmissionColor").a, Is.EqualTo(.06f).Within(1e-5f), "evening level");
                lp.Set(false); c = mat.GetColor("_EmissionColor");
                Assert.That(c.a, Is.EqualTo(.06f).Within(1e-5f), "day level"); Assert.That(c.g, Is.EqualTo(.88f * .06f).Within(1e-5f), "the hue kept");
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(mat); }
        }
    }
}
