using System;
using System.Linq;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        public readonly DistrictRun? District;
        public bool HandoverOpen => District == null || District.Handover == Outcome.None;
        bool AbleCell() => Array.Exists(Ops,o=>!o.Down && !o.LeftMap && !Captured.Contains(Sim.Op.Name(o.Id)));
        void CloseHandover(Outcome result,string news)
        {
            if (District == null || !HandoverOpen) return;
            District.Handover=result; District.HandoverNews=news; District.News=news;
            if (Vale!=ValeAt.Dead) Vale=ValeAt.Out;
            EntryInProgress=false; FightStart=INF;
            Note("district.handover",news+" The cell can keep working the district.");
        }
        void DistrictEndStep()
        {
            District!.Traffic.Step(this,District);
            if (Tick%Hz==0) foreach(var job in District.Jobs) {
                if (ContactLost(job.Contact)) {
                    District.Release(job.Contact,Tick);
                    District.Replace(job,id=>!ContactLost(id));
                    District.News="Word came back that a contact was incapacitated. A new lead is in the district book.";
                    Note("district.new-lead",District.News);
                }
            }
            if (SecondCarAt is Pt car) foreach (var o in Ops) if (o.InLent && !o.LeftMap) o.Pos=car;
            if (Result==Outcome.Success || Result==Outcome.Withdrawn)
            {
                var result=Result; Result=Outcome.None; CloseHandover(result,result==Outcome.Success?"The Handover succeeded. The councillor made it out.":"The cell withdrew from the Handover.");
            }
            if (Result==Outcome.None && !AbleCell()) { Result=Outcome.Failure; FailCode="cell-down"; FailText="Every operative is incapacitated or in custody."; Note("fail.cell-down",FailText); }
        }
        public Pt? DistrictWalkPoint(Pt p) => S.Grid.InGrid(p) ? Walk.Free(p) ? p : Walk.NearestFree(p) : District?.Map.Walk.NearestFree(p);
        public Pt[]? StreetWalk(Pt from,Pt to)
        {
            if (District==null || (S.Grid.InGrid(from) && S.Grid.InGrid(to))) return Walk.Route(from,to);
            var grid=District.Map.Walk;
            // Use the fine civic mesh up to the point where it meets the wider road mesh.
            var a=Portal(from); var b=Portal(to); if(!a.HasValue||!b.HasValue) return null;
            var trunk=grid.Route(a.Value,b.Value); if(trunk==null)return null;
            var start=S.Grid.InGrid(from)?Walk.Route(from,a.Value):new[]{from}; var end=S.Grid.InGrid(to)?Walk.Route(b.Value,to):new[]{to};
            return start==null||end==null?null:start.Concat(trunk).Concat(end).ToArray();
        }
        Pt? Portal(Pt from)
        {
            var map=District!.Map; var g=map.Walk;
            if(!S.Grid.InGrid(from)) return g.Free(from) ? from : (Pt?)null;
            if(g.Free(from) && g.PieceOf(from)==map.WalkPiece && Walk.Free(from)) return from;
            int x=g.CI(from.X),y=g.CJ(from.Y);
            for(int r=0;r<=8;r++) for(int i=-r;i<=r;i++) for(int j=-r;j<=r;j++)
            {
                if(Math.Abs(i)!=r&&Math.Abs(j)!=r) continue;
                var q=g.Centre(x+i,y+j); if(g.Free(q)&&g.PieceOf(q)==map.WalkPiece&&Walk.Free(q)&&Walk.Route(from,q)!=null)return q;
            }
            return null;
        }
        public Pt? DistrictPersonAt(int who)
        {
            var d=District!; var key=d.Map.People[who].Id;
            if (districtWalkers.TryGetValue(key,out var w))
            {
                if (w.P.Down) return null;
                if (w.St.Length>0) return CrowdPos(w);
            }
            return d.Pose(who,Tick).at;
        }
        bool ContactLost(int who) => districtWalkers.TryGetValue(District!.Map.People[who].Id,out var w) && w.P.Down;
        readonly System.Collections.Generic.Dictionary<string,Walker> districtWalkers = new System.Collections.Generic.Dictionary<string,Walker>();
        void BindDistrictCast() { if (District != null) foreach(var w in Walkers) districtWalkers[w.P.Id]=w; }
        public bool HasCivicActor(string id) => districtWalkers.ContainsKey(id);
        string DistrictOrder(Op op,Order o)
        {
            var d=District!; if(!d.Map.Index.TryGetValue(o.Target,out int who)) return No("target-unknown","There is no resident by that name.");
            var person=d.Map.People[who]; var at=DistrictPersonAt(who);
            if (!at.HasValue) return No("person-away","This contact is off the street.");
            var place=at.Value;
            if(o.Do=="district-visit")
            {
                if(op.InCar) return No("in-car","Step out to meet someone on foot.");
                var route=StreetWalk(op.Pos,place); if(route==null) return No("no-walk","There is no foot route to this meeting place.");
                var travel=Move(op,new Order {Op=o.Op,Do="move",Point=place,Run=o.Run}); if(travel.Length>0)return travel;
                d.Meet(who,place,Tick); return "";
            }
            if(!G.Within(op.Pos,place,650)) return No("too-far",$"Meet {person.Name} on foot within six metres.");
            if(o.Do=="district-back" || o.Do=="district-refer")
            {
                string line;
                if(o.Do=="district-back") {
                    if(d.Trust[who]>0) return No("already-turned","They already trust the cell.");
                    if(Cash<15) return No("no-cash","You need $15 to cover the favour.",usd:15);
                    Cash-=15;d.Trust[who]++;line=$"{person.Name} accepted help with {person.Need}.";
                } else {
                    if(d.Trust[who]<1)return No("not-a-contact","Earn their trust first.");
                    if(!d.Map.Index.TryGetValue(o.To,out int friend) || !person.Ties.Exists(t=>t.person==friend))return No("no-leverage","They do not know that person.");
                    if(d.Trust[friend]>0)return No("already-turned","That contact already trusts the cell.");
                    d.Trust[friend]++;line=$"{person.Name} vouched for the cell with {d.Map.People[friend].Name}.";
                    d.Memory[friend]=line;
                }
                d.News=line;d.Memory[who]=line;d.Release(who,Tick);Note("district.introduction",line);return "";
            }
            var job=d.Available(who);
            if(job==null || o.To!=job.Key) return No("target-unknown","That errand has moved on. Check the district book for its next contact.");
            bool bluff=o.Do=="district-bluff";
            if(bluff && job.Stage!=1) return No("no-leverage","There is no message to pass on here.");
            if(bluff && (d.Trust[job.B]<0 || d.Trust[job.C]<0)) return No("refusal-remembered","They remember the last false promise. An honest request can still earn their help.");
            string news;
            if(job.Stage==0) {news=$"{person.Name} vouched for the cell with {d.Map.People[job.B].Name}.";job.Stage=1;}
            else if(job.Stage==1) {job.Bluff=bluff; news=bluff?$"{person.Name} passed your false assurance to {d.Map.People[job.C].Name}.":$"{person.Name} asked {d.Map.People[job.C].Name} for a favour on your behalf.";job.Stage=2;}
            else
            {
                int pay=job.Bluff?70:35; Cash+=pay; int change=job.Bluff?-1:1;
                d.Trust[job.B]=Math.Clamp(d.Trust[job.B]+change,-3,3); d.Trust[who]=Math.Clamp(d.Trust[who]+change,-3,3);
                news=job.Bluff?$"{person.Name} paid ${pay}, then found the discrepancy. Both contacts will check your word.":$"{person.Name} paid ${pay}. Both contacts will speak for the cell.";
                if(job.Bluff) AddTrail(1,"district-false-assurance");
                d.Memory[job.A]=news; d.Memory[job.B]=news; d.Finish(job);
            }
            d.Memory[who]=news; d.News=news; d.Release(who,Tick); Note("district.favour",news); return "";
        }
    }
}
