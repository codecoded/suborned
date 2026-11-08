// Brief 102 (Aster's default, after the Scout went quiet): when our car drives off, the three rivals posted round it going after it (engine rivals.follow-car, GameCrew
// FollowCar) are a news card, read from Fable's book.feed.cards.rivals-follow. Until those words land the card is not made at all: never the kind as its title, never
// words of the host's own. (No saved route drives our car off with the three still posted: route B leaves in Popovic's car since the lend, so the engine's own line is fed.)
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;

public class Follow102
{
    static HandoverHost Followed()
    {
        var h = Host.Fresh(); Host.Tick(h, 2);
        Host.Call(h, "CrucialFromLog", new LogLine { Tick = h.Game.Tick, Code = "rivals.follow-car", Text = "The rivals posted round our car go after it." });
        return h;
    }

    [Test] public void TheRivalsFollowingOurCarAreACardOnlyWithFablesWords()
    {
        var bw = BookWordsFile.Get(); bool had = bw.FeedCards.TryGetValue("rivals-follow", out var fable);
        try
        {
            bw.FeedCards.Remove("rivals-follow");
            var none = Followed();
            Assert.That(none.CrucialLog.Exists(c => c.Kind == "rivals-follow"), Is.False, "no words, no card (never the kind as its title)");
            Assert.That(none.CrucialNow, Is.Null, "and nothing in the lozenge");

            var words = had ? fable : new BookWordsFile.FeedCardWords { Title = "test title", Line = "test line" };
            bw.FeedCards["rivals-follow"] = words;
            var h = Followed(); var c = h.CrucialNow;
            Assert.That(c?.Kind, Is.EqualTo("rivals-follow")); Assert.That(c.Title, Is.EqualTo(words.Title)); Assert.That(c.Line, Is.EqualTo(words.Line));
            Assert.That(c.Face, Is.EqualTo("badges/signal_rival")); Assert.That(c.View, Is.EqualTo("car/getaway"));
            Assert.That(c.Rank, Is.EqualTo(System.Array.IndexOf(HandoverHost.CrucialOrder, "for-car")), "it ranks with the rivals for our car");
        }
        finally { if (had) bw.FeedCards["rivals-follow"] = fable; else bw.FeedCards.Remove("rivals-follow"); }
    }
}
