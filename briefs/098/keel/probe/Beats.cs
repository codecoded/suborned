// Kept here as the evidence's source (brief 098, engine 0.49.0, Keel; briefs/098/keel/walk-law.json); not built from here: built in a scratch copy of sim/cli (out/scratch/keel/probe) with its case added to Dial.cs, as Vantage's regions probe.
// Keel's scratch probe (brief 098, engine 0.49.0): the dial's beat points (door, post, call-box) and the doors they stand by. dial beats
using System;
using System.Linq;
using Suborned.Sim;
static class BeatsCli
{
    public static int Run(Engine.Inputs inputs, string root, string[] args)
    {
        var s = inputs.SiteData; var pop = inputs.Population!;
        var pts = Population.PointsOf(s, pop, inputs.Rules.BodiesOn ? inputs.Rules.BodySpaceCm : 0);
        foreach (var e in s.Entrances) Console.WriteLine($"entrance {e.id} {e.building} {e.kind} at ({e.at.X},{e.at.Y}) stand ({e.stand.X},{e.stand.Y})");
        foreach (var r in s.Refuges.Where(r => r.kind == "door")) Console.WriteLine($"door {r.id} at ({r.at.X},{r.at.Y})");
        foreach (var p in pts.Posts) Console.WriteLine($"post {p.id} at ({p.at.X},{p.at.Y}) face ({p.face.X},{p.face.Y})");
        return 0;
    }
}
