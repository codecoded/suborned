using System;
using System.Linq;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        Stand? secondStreet;
        Path? secondRoute;
        long[]? secondProfile;
        int secondStart;
        public bool DistrictSecondDriving => secondRoute != null;
        public Pt? SecondCarAt => secondRoute != null ? secondRoute.At(SecondMm()) : secondStreet?.CarAt;
        public Pt? SecondCarHeading => secondRoute != null ? secondRoute.Heading(SecondMm()) : secondStreet?.Facing;
        long SecondMm() => secondProfile![Math.Min(Tick-secondStart,secondProfile.Length-1)];
        string DistrictStandName(Stand stand) => District!=null && stand.Id.StartsWith("street/") ? District.Map.StreetAt(stand.CarAt) : stand.Id;
        public Pt DistrictDoor(Pt at, Pt facing)
        {
            var side=CarGeo.Ahead(at,new Pt(-facing.Y,facing.X),260);
            return DistrictWalkPoint(side) ?? District!.Map.MeetingSite(side);
        }
        public Pt DistrictExitDoor(Op op) => op.InLent ? LentSideOut : GetawaySeat;
        public Pt? DistrictRoadPoint(Pt point)
        {
            if (District==null) return null;
            var grid = S.Drive.InGrid(point) ? S.Drive : District.Map.Roads;
            var road = grid.NearestFree(point);
            return road.HasValue && G.Within(road.Value,point,1200) ? road : null;
        }
        Stand? StreetStand(Pt point)
        {
            var at=DistrictRoadPoint(point); return at.HasValue ? StreetStandAt(at.Value,new Pt(100,0)) : null;
        }
        Stand StreetStandAt(Pt at,Pt facing)
        {
            var template=CurStand;
            return new Stand { Id=$"street/{at.X}/{at.Y}", Car=template.Car, CarAt=at, CarCell=at,
                Facing=facing, Exit=new[]{at,CarGeo.Ahead(at,facing,700)}, ExitHalfCm=template.ExitHalfCm,
                Ambush=template.Ambush, Police=template.Police, RivalStop=template.RivalStop };
        }
        Pt[]? DistrictCarRoute(Pt from,Pt to)
        {
            if(S.Drive.InGrid(from)&&S.Drive.InGrid(to)) {
                var field=TrafficField(); return S.Drive.CarRoute(from,to,S.DriveLaneCm,TrafficStillMask(),field?.fx,field?.fy);
            }
            var roads=District!.Map.Roads;
            var a=RoadPortal(from); var b=RoadPortal(to); if(!a.HasValue||!b.HasValue)return null;
            var trunk=roads.Route(a.Value,b.Value); if(trunk==null)return null;
            var start=S.Drive.InGrid(from)?S.Drive.CarRoute(from,a.Value,S.DriveLaneCm):new[]{from};
            var end=S.Drive.InGrid(to)?S.Drive.CarRoute(b.Value,to,S.DriveLaneCm):new[]{to};
            return start==null||end==null?null:start.Concat(trunk).Concat(end).ToArray();
        }
        Pt? RoadPortal(Pt at)
        {
            var roads=District!.Map.Roads;
            if(roads.Free(at)&&roads.PieceOf(at)==District.Map.RoadPiece)return at;
            if(!S.Drive.InGrid(at)) return null;
            var x=roads.CI(at.X); var y=roads.CJ(at.Y);
            for(int r=1;r<=9;r++) for(int i=-r;i<=r;i++) for(int j=-r;j<=r;j++) {
                if(Math.Abs(i)!=r&&Math.Abs(j)!=r)continue;
                var p=roads.Centre(x+i,y+j); if(roads.Free(p)&&roads.PieceOf(p)==District.Map.RoadPiece&&S.Drive.Free(p)&&S.Drive.Route(at,p)!=null) return p;
            }
            return null;
        }
        string DriveSecondCar(Op op,Order o)
        {
            var st=StreetStand(o.Point!.Value); if(st==null)return No("no-walk","Choose a connected street.");
            if(lentLeft||!Lent)return No("car-left","That car has left.");
            foreach(var x in Ops) if(x.InLent && x.Busy!=null && x.Busy.Do!="ride" && x.Busy.Do!="drive-car")return No("drive-busy","A passenger is still busy.");
            if(secondRoute==null && (LentHolders().Count>0 || LentBoxers().Count>0)) return No("drive-held","The car is blocked.");
            var points=DistrictCarRoute(LentPos(),st.CarAt); if(points==null)return No("no-walk","No road connects these places.");
            var path=new Path(points); if(path.Length<50)return No("drive-here","Already there.");
            secondStreet=StreetStandAt(st.CarAt,path.Heading(path.Length)); secondRoute=path; secondProfile=CarProfile(path); secondStart=Tick;
            foreach(var x in Ops) if(x.InLent && !x.LeftMap) {
                x.Busy=x==op?o:new Order{Op=Sim.Op.Name(x.Id),Do="ride",Tick=Tick}; x.BusyStart=Tick; x.BusyEnd=Tick+Math.Max(1,secondProfile.Length-1);
            }
            Note("district.drive",$"The {Sim.Op.Name(op.Id)} drives to {DistrictStandName(secondStreet)}."); return "";
        }
        void LandSecondCar()
        {
            if(secondStreet==null)return;
            secondRoute=null;secondProfile=null;takenAt=secondStreet.CarAt;takenHead=secondStreet.Facing;
            lentLane=secondStreet.Exit;lentStand=secondStreet;liveCoverKey=-1;
            foreach(var x in Ops)if(x.InLent) x.Pos=secondStreet.CarAt;
            Note("district.parked",$"Parked on {DistrictStandName(secondStreet)}.");
        }
        void DistrictDeparture(bool success)
        {
            if (!HandoverOpen) return;
            // The crew delivers the passenger. The same people, car, standing and witness record carry on.
            foreach(var o in Ops) if(o.LeftMap && !Captured.Contains(Sim.Op.Name(o.Id))) {o.LeftMap=false;o.InCar=false;o.InLent=false;o.Pos=GetawaySeat;}
            if(success) Chain.Add("car-leaves-with-vale"); EndOpenFight();
            CloseHandover(success?Outcome.Success:Outcome.Withdrawn,success?"The councillor made it out. The Handover is complete.":"The cell left the Handover behind.");
        }
        internal void HashDistrictDrive(Hash.H h)
        {
            if(secondStreet!=null) h.S(secondStreet.Id).I(secondStart).Z(secondRoute!=null);
        }
    }
}
