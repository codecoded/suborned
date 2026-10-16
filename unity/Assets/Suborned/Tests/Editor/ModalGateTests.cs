using System;
using NUnit.Framework;
using Suborned.Play;

namespace Suborned.Play.Tests
{
    /// <summary>Sky 2026-10-16: "i'm on this screen yet when I move mouse everything is showing up as label, and responding". Under a modal (the
    /// briefing until Begin, the end card) no hover reaches the street and no click or key does, except Begin, the briefing key, restart on
    /// the end card, debug, and the camera. The live check (the host with the briefing up, the pointer over the street) is Drive.ModalCheck in
    /// out/less/Drive.cs, run against the play build.</summary>
    public sealed class ModalGateTests
    {
        [Test]
        public void NoHoverOrClickUnderAModal()
        {
            foreach (var m in new[] { ModalGate.Modal.Briefing, ModalGate.Modal.EndCard })
            {
                Assert.That(ModalGate.AllowsHover(m), Is.False, $"{m}: hover");
                Assert.That(ModalGate.AllowsClicks(m), Is.False, $"{m}: clicks");
                Assert.That(ModalGate.AllowsCamera(m), Is.True, $"{m}: the camera may look round");
            }
            Assert.That(ModalGate.AllowsHover(ModalGate.Modal.None) && ModalGate.AllowsClicks(ModalGate.Modal.None), Is.True, "no modal: the street answers");
        }

        [Test]
        public void OnlyBeginAndTheCameraKeysUnderTheBriefing()
        {
            var allowed = new[] { PlayAction.Pause, PlayAction.Briefing, PlayAction.Debug, PlayAction.PanUp, PlayAction.PanDown, PlayAction.PanLeft, PlayAction.PanRight };
            foreach (PlayAction a in Enum.GetValues(typeof(PlayAction)))
                Assert.That(ModalGate.Allows(ModalGate.Modal.Briefing, a), Is.EqualTo(Array.IndexOf(allowed, a) >= 0), $"briefing: {a}");
        }

        [Test]
        public void OnlyRestartAndTheCameraKeysOnTheEndCard()
        {
            var allowed = new[] { PlayAction.Restart, PlayAction.Debug, PlayAction.PanUp, PlayAction.PanDown, PlayAction.PanLeft, PlayAction.PanRight };
            foreach (PlayAction a in Enum.GetValues(typeof(PlayAction)))
                Assert.That(ModalGate.Allows(ModalGate.Modal.EndCard, a), Is.EqualTo(Array.IndexOf(allowed, a) >= 0), $"end card: {a}");
            foreach (PlayAction a in Enum.GetValues(typeof(PlayAction))) Assert.That(ModalGate.Allows(ModalGate.Modal.None, a), Is.True, $"no modal: {a}");
        }
    }
}
