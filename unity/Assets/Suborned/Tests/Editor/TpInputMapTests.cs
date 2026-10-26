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

        /// <summary>The brief's rows as the table has them now, in both presets (HandoverHost.HoldRing = the Built preset): brief 041 (Double click:
        /// no hold opens a ring, a held E acts as a tap on release) and item 33 (Sky 2026-10-24: a left click on the prompted target opens its ring;
        /// a right tap does nothing, the right button's hold aims). The rows of 0.2.43 (E hold rings, left tap acts, right tap rings at the target)
        /// were stale since then; ProofFire checks the same rows in the player.</summary>
        [Test]
        public void TheRowsOfTheBrief()
        {
            bool was = HandoverHost.HoldRing;
            try
            {
                foreach (bool built in new[] { false, true })
                {
                    HandoverHost.HoldRing = built; string p = built ? "Built" : "hold preset";
                    Assert.AreEqual(Do.Act, HandoverHost.TpResolve(In.ETap, St.Free), p);
                    Assert.AreEqual(built ? Do.Ring : Do.Act, HandoverHost.TpResolve(In.EHold, St.Free), p);
                    Assert.AreEqual(built ? Do.Ring : Do.Act, HandoverHost.TpResolve(In.LeftHold, St.Free), p);
                    Assert.AreEqual(Do.Ring, HandoverHost.TpResolve(In.LeftTap, St.Walking), p);
                    Assert.AreEqual(Do.Orbit, HandoverHost.TpResolve(In.LeftDrag, St.Running), p);
                    Assert.AreEqual(Do.Nothing, HandoverHost.TpResolve(In.RightTap, St.Free), p);
                    Assert.AreEqual(Do.LowerAim, HandoverHost.TpResolve(In.RightTap, St.Aiming), p);
                    Assert.AreEqual(Do.Aim, HandoverHost.TpResolve(In.RightHold, St.Running), p);
                    Assert.AreEqual(Do.Nothing, HandoverHost.TpResolve(In.RightHold, St.CarryingVale), p);
                    Assert.AreEqual(Do.TakeLit, HandoverHost.TpResolve(In.ETap, St.RingOpen), p);
                    foreach (St s in Enum.GetValues(typeof(St))) Assert.AreEqual(Do.Look, HandoverHost.TpResolve(In.MiddleDrag, s), p);
                }
            }
            finally { HandoverHost.HoldRing = was; }
        }
    }
}
