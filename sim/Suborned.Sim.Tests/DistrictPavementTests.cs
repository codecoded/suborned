using System;
using System.Collections.Generic;
using System.Linq;
using Suborned.Sim;
using Xunit;

public sealed class DistrictPavementTests
{
    static Game Harbour(int seed=9) => Engine.New(Repo.Inputs,new Start { District=true, Seed=seed });
    static int Clearance(StreetRobot role) => role switch {
        StreetRobot.Porter => 151, StreetRobot.Shopkeeper => 106, StreetRobot.Sweeper => 89,
        StreetRobot.CapsuleRunner => 71, _ => 101
    };

    [Theory]
    [InlineData(9)]
    [InlineData(37)]
    public void ResidentsPassWorkingMachinesAndBothKeepTravelling(int seed)
    {
        var g=Harbour(seed); var d=g.District!; var traffic=d.Traffic;
        var publicResidents=Enumerable.Range(0,d.Map.People.Length).Where(d.Roams).ToArray();
        var modes=new HashSet<StreetRobot>(); int passes=0;
        for (int k=1;k<=800;k++)
        {
            g.Step();
            if (k%4!=0 && k!=579) continue;
            var rs=Enumerable.Range(0,traffic.RobotCount).Select(i=>traffic.Robot(i,k)).ToArray();
            foreach (int i in publicResidents)
            {
                if (g.DistrictPersonAt(i) is not Pt at) continue;
                for (int r=0;r<rs.Length;r++)
                {
                    var bot=rs[r];
                    if (Math.Abs(at.X-bot.At.X)>195 || Math.Abs(at.Y-bot.At.Y)>195) continue;
                    var role=traffic.RobotKind(r);
                    Assert.False(G.Within(at,bot.At,Clearance(role)-1),$"{d.Map.People[i].Name} crossed {role} {r} at tick {k}");
                    if (bot.Moving) modes.Add(role);
                    else if (d.Pose(i,k).moving) passes++;
                }
            }
        }
        Assert.Contains(StreetRobot.Sweeper,modes);
        Assert.Contains(StreetRobot.CapsuleRunner,modes);
        Assert.True(passes>20);
    }

    [Fact]
    public void ServiceMachinesResumeTheirRoundsAfterTheOpeningQueues()
    {
        var g=Harbour(); var traffic=g.District!.Traffic;
        var start=Enumerable.Range(0,traffic.RobotCount).Select(i=>traffic.ServiceMechanism(i,0).wheelMm).ToArray();
        g.RunTo(2000);
        for (int i=0;i<start.Length;i++)
            Assert.True(traffic.ServiceMechanism(i,g.Tick).wheelMm-start[i]>4000,$"{traffic.RobotKind(i)} {i} stayed in its opening queue");
    }

    [Fact]
    public void ShoppingRequiresADwellAndPurchasesTravelWithTheirOwner()
    {
        var g=Harbour();var d=g.District!;
        var fixtures=d.Map.Ground.Venues.SelectMany(v=>v.Fixtures).ToArray();
        var held=new Dictionary<int,(StreetPurchase goods,Pt pickedUp)>();
        var travelled=new HashSet<StreetPurchase>(); var delivered=new HashSet<StreetPurchase>();
        for(int k=0;k<=1600;k++)
        {
            if(k>0)g.Step();
            if(k%5!=0)continue;
            for(int i=0;i<d.Map.People.Length;i++)
            {
                var now=d.Shopping.Carried(i);
                var p=d.Pose(i,k);
                if(held.TryGetValue(i,out var last))
                {
                    if(now==StreetPurchase.None)
                    {
                        Assert.True(G.Within(p.at,d.Map.People[i].Walk.Points[0],100));
                        delivered.Add(last.goods); held.Remove(i);
                    }
                    else if (!G.Within(p.at,last.pickedUp,400)) travelled.Add(now);
                }
                else if(now!=StreetPurchase.None)
                {
                    Assert.False(p.moving);
                    Assert.Contains(fixtures,f=>f.Kit==(now==StreetPurchase.Groceries ? "ProduceStall" : "Newsstand") && G.Within(f.At,p.at,315));
                    held[i]=(now,p.at);
                }
            }
        }
        Assert.Contains(StreetPurchase.Groceries,travelled);
        Assert.Contains(StreetPurchase.Newspaper,travelled);
        Assert.NotEmpty(delivered);
    }

    [Fact]
    public void ACellMemberCanFinishTheWalkToTheLanternNewsstandContact()
    {
        var g=Harbour();var d=g.District!;
        const string person="p-z-harbour-lantern-papers-04";
        Assert.True(g.Issue(new Order { Op="scout",Do="district-visit",Target=person }).ok);
        var scout=g.Ops[0]; var goal=scout.Walk!.End;
        while(g.Tick<2100 && scout.Walk!=null) g.Step();
        Assert.Null(scout.Walk);
        Assert.True(G.Within(scout.Pos,goal,25));
    }

    [Fact]
    public void PointAndClickPassesAnOperatingMarketSweeper()
    {
        var g=Harbour(); var d=g.District!;
        var market=d.Map.Ground.Venues.Single(v=>v.Id=="lantern-market");
        var scout=g.Ops[0]; var delta=new Pt(265,0); var goal=market.At+delta;
        Assert.True(g.Issue(new Order { Op="scout",Do="move",Point=market.At-delta }).ok);
        while(g.Tick<2500 && scout.Walk!=null) g.Step();
        Assert.Null(scout.Walk);
        int sweeper=Enumerable.Range(0,d.Traffic.RobotCount).Where(i=>d.Traffic.RobotKind(i)==StreetRobot.Sweeper)
            .OrderBy(i=>G.D2(market.At,d.Traffic.Robot(i,g.Tick).At)).First();
        while(g.Tick<4000 && (!G.Within(market.At,d.Traffic.Robot(sweeper,g.Tick).At,20)
            || d.Traffic.ServiceMechanism(sweeper,g.Tick).lift<250)) g.Step();
        Assert.True(g.Tick<4000);
        Assert.True(d.Map.Walk.LineFree(scout.Pos,goal));
        Assert.True(g.Issue(new Order { Tick=g.Tick,Op="scout",Do="move",Point=goal }).ok);
        int near=0, end=g.Tick+120;
        while(g.Tick<end && scout.Walk!=null)
        {
            g.Step();
            for(int i=0;i<d.Traffic.RobotCount;i++)
            {
                var p=d.Traffic.Robot(i,g.Tick).At;
                if(!G.Within(p,scout.Pos,250)) continue;
                near++;
                Assert.False(G.Within(p,scout.Pos,Clearance(d.Traffic.RobotKind(i))-1));
            }
        }
        Assert.True(near>0);
        Assert.Null(scout.Walk);
        Assert.True(G.Within(scout.Pos,goal,25));
    }
}
