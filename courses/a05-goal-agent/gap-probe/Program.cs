using System;
using System.Linq;
using System.Text.Json;
using Suborned.Sim;

// Diagnostic evidence for the report: fixture state, no production edits and no new acceptance claim.
var inputs = Engine.Load(args[0]);
var game = Engine.New(inputs);
var container = game.S.Stashes.First(s => s.Kind == "container");
var scout = game.OpOf(OpId.Scout);
scout.Pos = container.Stand;
scout.InCar = true;
var before = Hash.State(game);
var preview = game.LearnQuery("scout", "watch", container.Id);
var pure = Hash.State(game) == before;
var issued = game.Issue(new Order { Tick = game.Tick, Op = "scout", Do = "watch", Target = container.Id });
if (!preview.ok || issued.ok || issued.code != "in-car" || !pure)
    throw new Exception("The audited preview mismatch did not reproduce; revise the candidate report.");
Console.WriteLine(JsonSerializer.Serialize(new {
    diagnostic = "learn-query-in-car", reproduced = true,
    fixture = "Fresh Handover; Scout positioned at an unwatched container; InCar=true",
    preview = new { accepted = preview.ok, code = preview.code },
    issue = new { accepted = issued.ok, code = issued.code },
    previewIsPure = pure,
    expectedAfterRepair = "Both refuse in-car; preview remains pure",
    implementationChanged = false
}, new JsonSerializerOptions { WriteIndented = true }));
