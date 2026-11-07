// Brief 098 step 1b, jams in the host (HostTrafficBoxed, HostTraffic; Sky 2026-11-07: "should be opposite - shot cars can get past, others block"): a Go or a Drive
// refused car-boxed by a passing car names it plainly and says shooting it clears the way; the rival car's own line stays; a passing car boxing us in or making room
// says so on its hover; a name says the car's colour. Same harness as HostLogic.cs: the real engine and text.json, no views (the passing cars are put in the views'
// list here, as TrafficViews puts them; the player probe, ProofJam, plays a real jam).
using System;
using System.Reflection;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;

public class Jam098
{
    static TrafficView Car(string id, string model = "sedan", string boxes = "", string room = "", bool shot = false, string state = "stopped") =>
        new TrafficView { Id = id, Model = model, State = state, Mode = shot ? "abandoned" : "drive", DriverIn = !shot, Standing = true, Boxes = boxes, MakingRoom = room, Shot = shot };
    /// <summary>A game with passing cars (the dial's Cars row, as Sky sets it): the views are silent here, so names come from the engine's own read of each car.</summary>
    static HandoverHost WithCars(int n) { var h = Host.Fresh(); Host.Set(h, "ShowBriefing", true); h.DialStep(Population.Cars, n); h.DialPlay(); Assert.That(h.Game.TrafficOn, h.Status); return h; }
    static string Name(TrafficView v) => v.Model switch { "van" => "the van", "scooter" => "the scooter", _ => "the passing car" }; // no paint without the views: the plain fallback

    [Test] public void BoxedInByTrafficNamesTheCarAndSaysShootingItClearsTheWay()
    {
        var h = WithCars(8); var cars = h.Game.TrafficNow(); Assert.That(cars.Count, Is.GreaterThanOrEqualTo(2)); var c1 = cars[0]; var c2 = cars[1];
        var one = new Answer { ok = false, code = "car-boxed", why = $"boxed in: {c1.Id} is in the way", ids = c1.Id };
        Assert.That(h.BoxedLine(one), Is.EqualTo($"Boxed in: {Name(c1)} is in our way out. Shoot it and we can shove it aside."));
        Assert.That(h.BoxedShort(one), Is.EqualTo("Boxed in · " + Name(c1).Substring(4)));
        var two = new Answer { ok = false, code = "drive-boxed", why = "", ids = c1.Id + "," + c2.Id };
        Assert.That(h.BoxedLine(two), Is.EqualTo($"Boxed in: {Name(c1)} and {Name(c2)} are in our way out. Shoot them and we can shove them aside."));
        Assert.That(h.BoxedShort(two), Is.EqualTo("Boxed in · 2 cars"));
        var mixed = new Answer { ok = false, code = "car-boxed", why = "", ids = "car:rivals," + c2.Id };
        var n2 = Name(c2); Assert.That(h.BoxedLine(mixed), Does.StartWith(h.Text.LockByCode("car-boxed").Line).And.EndWith($"{char.ToUpperInvariant(n2[0])}{n2.Substring(1)} is in the way too: shoot it and we can shove it aside."), "the rival car's own line stays (brief 096: it has to drive off or be blown up)");
        var rivals = new Answer { ok = false, code = "car-boxed", why = "", ids = "car:rivals" };
        Assert.That(h.BoxedLine(rivals), Is.Null, "no passing car: the text's own lock line"); Assert.That(h.BoxedShort(rivals), Is.Null);
        Assert.That(h.LockLine(new Order { Op = "tech", Do = "go" }, one), Is.EqualTo(h.BoxedLine(one)), "the refusal's long line is the traffic one");
        Assert.That(h.BoxedLine(new Answer { ok = false, code = "car-held", ids = c1.Id }), Is.Null, "only a box names the car in the way");
        foreach (var a in new[] { one, two, mixed }) { Host.NoTokens(a.ids, h.BoxedLine(a)); Assert.That(h.BoxedLine(a), Does.Not.Contain("car:"), "never an engine id"); }
        var none = Host.Fresh(); Assert.That(none.BoxedLine(rivals), Is.Null); Assert.That(none.MissionHeldWords("car:getaway"), Is.EqualTo(""), "no traffic: nothing holds a mission car");
    }

    [Test] public void APassingCarIsNamedByItsColour()
    {
        var m = typeof(HandoverHost).GetMethod("TrafficColour", BindingFlags.NonPublic | BindingFlags.Static); Assert.That(m, Is.Not.Null);
        string C(string paint) => (string)m.Invoke(null, new object[] { paint });
        Assert.That(C("matador-red"), Is.EqualTo("red")); Assert.That(C("canary-yellow"), Is.EqualTo("yellow")); Assert.That(C("cream-turquoise"), Is.EqualTo("cream"));
        Assert.That(C("harbor-blue"), Is.EqualTo("blue")); Assert.That(C("larkspur-blue"), Is.EqualTo("pale blue")); Assert.That(C("onyx"), Is.EqualTo("black"));
        Assert.That(C("orchid"), Is.EqualTo("lilac")); Assert.That(C("seafoam-green"), Is.EqualTo("green")); Assert.That(C(""), Is.EqualTo(""), "no paint: no colour word");
        Assert.That(C("sunset-gold"), Is.EqualTo("sunset"), "a paint the table does not know: its first word");
    }

    /// <summary>The hover's words for one car's read (HostTraffic.TrafficWordsOf): boxing a mission car in, making room for one, a wreck being pushed aside.</summary>
    [Test] public void TheHoverSaysACarBoxesUsInOrMakesRoom()
    {
        var h = Host.Fresh(); var m = typeof(HandoverHost).GetMethod("TrafficWordsOf", BindingFlags.NonPublic | BindingFlags.Instance); Assert.That(m, Is.Not.Null);
        string W(TrafficView v) => (string)m.Invoke(h, new object[] { v });
        Assert.That(W(Car("car:t-001-01", boxes: "car:getaway")), Is.EqualTo("boxing our car in"));
        Assert.That(W(Car("car:t-002-01", room: "car:getaway")), Is.EqualTo("making room for our car"));
        Assert.That(W(Car("car:t-003-01", boxes: "car:official")), Does.StartWith("boxing ").And.EndWith(" in").And.Not.Contain("our car"), "Popovic's car by its name");
        Assert.That(W(Car("car:t-004-01", shot: true, state: "disabled")), Is.EqualTo("wrecked, the driver ran"), "a wreck boxes nobody in");
        var pushed = Car("car:t-005-01", shot: true, state: "disabled"); pushed.ShovedAt = h.Game.Tick; pushed.ShovedBy = "car:getaway"; pushed.Shoved = 150;
        Assert.That(W(pushed), Is.EqualTo("wrecked, the driver ran · being pushed aside"));
        pushed.ShovedAt = h.Game.Tick - 30; Assert.That(W(pushed), Is.EqualTo("wrecked, the driver ran"), "once it stands still again it is just a wreck");
        Assert.That(W(null), Is.EqualTo(""));
    }
}
