// Brief 096, Redline's render audit item 2 option B (Sky 2026-11-05: "looks worth it"): the play camera's stand-off from the tallest geometry in its
// frame column (StandOffField, PlayStandOff). The field's need against the exact answer for a lone tower, at the play pitch 38 and both played yaws.
using NUnit.Framework;
using Suborned;
using UnityEngine;

public class StandOff096
{
    const float Pitch = 38f;
    static float Sin => Mathf.Sin(Pitch * Mathf.Deg2Rad);
    static float Cos => Mathf.Cos(Pitch * Mathf.Deg2Rad);
    static StandOffField Tower(Vector3 at, float height, float side = 8f)
    {
        var f = new StandOffField(8f, -1000f, -1000f, 1000f, 1000f);
        f.AddBox(new Bounds(new Vector3(at.x, height / 2f, at.z), new Vector3(side, height, side)));
        return f;
    }

    [Test] public void FlatGroundNeedsOnlyTheFramesLowerEdge()
    {
        var f = new StandOffField(8f, -500f, -500f, 500f, 500f);
        float need = f.Need(Vector3.zero, 30f, Pitch, 17.8f, 10f, 0f);
        Assert.That(need, Is.EqualTo(10f * Cos / Sin).Within(1e-3f), "the ground at the frame's lower edge");
    }

    [Test] public void ATowerOnTheFocusNeedsItsRoofInFrame()
    {
        // a 40 m tower on the focus: its highest point in frame is where the view ray through the frame's top edge meets it (y_cam = halfH)
        var at = new Vector3(4f, 0f, 4f); var f = Tower(at, 40f, 4f); // one 8 m cell, centred on the focus
        float need = f.Need(at, 210f, Pitch, 17.8f, 10f, 0f);
        float rho = 8f * 0.70711f, hs = Mathf.Min(40f, (10f + rho * Sin) / Cos), exact = hs * Sin + rho * Cos;
        Assert.That(need, Is.EqualTo(exact).Within(0.05f));
        Assert.That(need, Is.LessThan(80f), "the Handover zone's roofs: well short of the old 160 m");
    }

    [Test] public void SpaceHqTowardTheCameraIsCountedAndAwayFromItIsNot()
    {
        // 250 m tall, 320 m toward the camera at yaw 30: its roof projects onto the frame (y_cam = 250 cos - 320 sin is about 0), so the camera must stand
        // past it (about 250 / sin 38 = 406 m); the same tower 320 m beyond the focus is above the frame and needs nothing
        float yaw = 30f; var fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
        float toward = Tower(-fwd * 320f, 250f).Need(Vector3.zero, yaw, Pitch, 17.8f, 10f, 0f);
        Assert.That(toward, Is.GreaterThan(395f), $"SpaceHQ in the column: {toward:0.0} m");
        float beyond = Tower(fwd * 320f, 250f).Need(Vector3.zero, yaw, Pitch, 17.8f, 10f, 0f);
        Assert.That(beyond, Is.EqualTo(10f * Cos / Sin).Within(1e-3f), "beyond the frame's top: the ground alone");
        float aside = Tower(new Vector3(fwd.z, 0f, -fwd.x) * 120f - fwd * 320f, 250f).Need(Vector3.zero, yaw, Pitch, 17.8f, 10f, 0f);
        Assert.That(aside, Is.EqualTo(10f * Cos / Sin).Within(1e-3f), "outside the frame's width: the ground alone");
    }

    [Test] public void TheLookAheadCountsATowerBeforeItEntersTheFrame()
    {
        // a 120 m tower 24 m off the frame's right edge: not in the column now, in it with a 30 m look-ahead (its height in frame then reaches the grown top edge)
        float yaw = 210f; var right = new Vector3(Mathf.Cos(yaw * Mathf.Deg2Rad), 0f, -Mathf.Sin(yaw * Mathf.Deg2Rad));
        var f = Tower(right * (17.8f + 24f), 120f, 4f);
        float now = f.Need(Vector3.zero, yaw, Pitch, 17.8f, 10f, 0f), ahead = f.Need(Vector3.zero, yaw, Pitch, 17.8f, 10f, 30f);
        Assert.That(now, Is.LessThan(30f), $"now {now:0.0}");
        Assert.That(ahead, Is.GreaterThan(now + 20f), $"now {now:0.0}, ahead {ahead:0.0}");
    }
}
