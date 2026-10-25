// Brief 054 §3.3 (engine 0.28.0; its Done-check scenario, added with engine 0.31.0): a stop on the street with no facing is rejected at load
// (unknown = reject): the people file's faces emptied, the game refuses to start, naming the stop.
using System;
using System.Text.Json.Nodes;
using Suborned.Sim;
using Xunit;

public class StopFacingTests
{
    [Fact]
    public void AStopWithNoFacingIsRejectedAtLoad()
    {
        var I = Repo.Inputs;
        var people = JsonNode.Parse(I.People)!.AsObject();
        Assert.True(people["faces"]!.AsObject().Count > 0);
        people["faces"] = new JsonObject();
        var bare = Engine.FromTexts(I.Rulebook, I.PeopleRules, I.Site, people.ToJsonString(), I.Provisional.Length > 0 ? I.Provisional : null, I.Takedown.Length > 0 ? I.Takedown : null, I.Rounds.Length > 0 ? I.Rounds : null);
        var e = Assert.Throws<FormatException>(() => Engine.New(bare));
        Assert.Contains("has no facing", e.Message);
        Engine.New(I); // the real file starts
    }
}
