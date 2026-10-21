using System;
using NUnit.Framework;
using In = Suborned.Play.HandoverHost.TpIn;
using St = Suborned.Play.HandoverHost.TpState;
using Do = Suborned.Play.HandoverHost.TpDo;

namespace Suborned.Play.Tests
{
    /// <summary>Brief 024 revision, Glyph's done-check 1: one meaning per input per state. The third-person input map (HandoverHost.TpResolve,
    /// the table the host follows) gives exactly one result for every input in every state, and the rules that make it safe hold: the left
    /// button fires only while aiming, the right hold aims except over a ring or walking Vale, the middle drag always looks.</summary>
    public sealed class TpInputMapTests
    {
        [Test]
        public void EveryInputHasOneMeaningInEveryState()
        {
            foreach (In i in Enum.GetValues(typeof(In)))
                foreach (St s in Enum.GetValues(typeof(St)))
                    Assert.That(Enum.IsDefined(typeof(Do), HandoverHost.TpResolve(i, s)), $"{i} in {s}");
        }

        [Test]
        public void NoHipFire()
        {
            foreach (St s in Enum.GetValues(typeof(St)))
            {
                if (s == St.Aiming) continue;
                Assert.AreNotEqual(Do.Fire, HandoverHost.TpResolve(In.LeftTap, s), $"left tap in {s}");
                Assert.AreNotEqual(Do.Fire, HandoverHost.TpResolve(In.LeftHold, s), $"left hold in {s}");
                Assert.AreEqual(Do.Nothing, HandoverHost.TpResolve(In.LeftAiming, s), $"left 'aiming' in {s}");
            }
            Assert.AreEqual(Do.Fire, HandoverHost.TpResolve(In.LeftTap, St.Aiming));
        }

        [Test]
        public void TheRowsOfTheBrief()
        {
            Assert.AreEqual(Do.Act, HandoverHost.TpResolve(In.ETap, St.Free));
            Assert.AreEqual(Do.Ring, HandoverHost.TpResolve(In.EHold, St.Free));
            Assert.AreEqual(Do.Act, HandoverHost.TpResolve(In.LeftTap, St.Walking));
            Assert.AreEqual(Do.Orbit, HandoverHost.TpResolve(In.LeftDrag, St.Running));
            Assert.AreEqual(Do.RingAtTarget, HandoverHost.TpResolve(In.RightTap, St.Free));
            Assert.AreEqual(Do.Aim, HandoverHost.TpResolve(In.RightHold, St.Running));
            Assert.AreEqual(Do.Nothing, HandoverHost.TpResolve(In.RightHold, St.CarryingVale));
            Assert.AreEqual(Do.TakeLit, HandoverHost.TpResolve(In.ETap, St.RingOpen));
            foreach (St s in Enum.GetValues(typeof(St))) Assert.AreEqual(Do.Look, HandoverHost.TpResolve(In.MiddleDrag, s));
        }
    }
}
