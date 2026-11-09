// Brief 101 (Sky's pick B, the square's lamp mast): what the host makes of the mast with no scene. The Scout watches it from her start, and the done chip says
// what the Watch taught (its cone) and stands on the mast (camera/mast-z0), as on any camera: the engine's line names it "the mast-z0 camera", hyphen and all.
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using Order = Suborned.Sim.Order;

public class Mast101
{
    [Test] public void WatchingTheMastPutsItsConeOnTheMast()
    {
        var h = Host.Fresh(); var g = h.Game; Host.Call(h, "DoneFrame"); // the done moment's first frame: it reads the log from here on
        var mast = g.S.Cameras.Find(c => c.Id == "mast-z0"); Assert.That(mast != null && mast.World && mast.Facings.Length == 3, "the square's lamp mast, three heads");
        h.Issue(new Order { Op = "scout", Do = "watch", Target = "camera:mast-z0" });
        Assert.That(g.Orders.Last().ok, $"the Scout watches it from her start: {g.Orders.Last().code} {g.Orders.Last().why}");
        Assert.That(Host.Until(h, () => g.Known("camera:mast-z0") is { Sight: true }, 600), "its sight is learned");
        Host.Call(h, "DoneFrame");
        var chip = h.DoneChips.LastOrDefault(c => c.Do == "watch"); Assert.That(chip, Is.Not.Null, "a done chip for the Watch");
        Assert.That(chip.AnchorKey, Is.EqualTo("camera/mast-z0"), "it stands on the mast, not on the Scout");
        Assert.That(chip.Lines.Select(l => l.Icon), Does.Contain("cone"), "what it taught: " + string.Join(", ", chip.Lines.Select(l => l.Word)));
    }
}
