// Personal space on the wider district grid. Each pedestrian reserves one actual grounded position.
// A tick tests both the neighbours who have stepped and those who are about to step.
using System;

namespace Suborned.Sim
{
    internal sealed class DistrictPedestrians
    {
        const int Shoulders = 58;
        readonly Grid grid;
        readonly int[] first, next, prev, cell;
        readonly Pt[] positions;

        internal DistrictPedestrians(Grid grid,int count)
        {
            this.grid=grid; first=new int[grid.Nx*grid.Ny]; next=new int[count]; prev=new int[count];
            cell=new int[count]; positions=new Pt[count]; Reset();
        }
        internal void Reset() { Array.Fill(first,-1); Array.Fill(cell,-1); }
        internal void Remove(int who)
        {
            if (cell[who]<0) return;
            if (prev[who]<0) first[cell[who]]=next[who]; else next[prev[who]]=next[who];
            if (next[who]>=0) prev[next[who]]=prev[who];
            cell[who]=-1;
        }
        internal void Add(int who,Pt at)
        {
            int x=grid.CI(at.X),y=grid.CJ(at.Y);
            if ((uint)x>=(uint)grid.Nx || (uint)y>=(uint)grid.Ny) return;
            int k=x*grid.Ny+y, before=first[k];
            cell[who]=k; prev[who]=-1; next[who]=before;
            if (before>=0) prev[before]=who;
            first[k]=who; positions[who]=at;
        }

        int Nearest(Pt at,int reach)
        {
            int nearest=-1, minX=Math.Max(0,grid.CI(at.X-reach)), maxX=Math.Min(grid.Nx-1,grid.CI(at.X+reach));
            int minY=Math.Max(0,grid.CJ(at.Y-reach)),maxY=Math.Min(grid.Ny-1,grid.CJ(at.Y+reach));
            long best=(long)reach*reach;
            for (int x=minX;x<=maxX;x++)
            for (int y=minY;y<=maxY;y++)
            for (int i=first[x*grid.Ny+y];i>=0;i=next[i])
            {
                long d=G.D2(at,positions[i]);
                if (d<best || d==best && (nearest<0 || i<nearest)) { best=d; nearest=i; }
            }
            return nearest;
        }
        internal bool Clear(Pt p) => Nearest(p,Shoulders)<0;

        internal Pt Pass(Pt from,Pt goal)
        {
            if (G.Within(from,goal,2)) return goal;
            var h=CarGeo.Ahead(default,goal-from,100);
            var look=CarGeo.Ahead(from,h,56);
            int other=Nearest(look,74);
            if (other<0) return goal;
            var offset=positions[other]-from;
            int along=(offset.X*h.X+offset.Y*h.Y)/100;
            if (along<0 || along>125) return goal;
            var right=new Pt(h.Y,-h.X);
            int side=(offset.X*right.X+offset.Y*right.Y)/100;
            if (Math.Abs(side)>=72) return goal;
            int sign=side>9 ? -1 : 1;
            // Short local steps can take somebody round a stationary queue even when their
            // timetable is already several metres ahead on the same walk.
            var basePoint=G.Within(from,goal,74) ? goal : CarGeo.Ahead(from,h,74);
            var aim=CarGeo.Ahead(basePoint,right,sign*(77-Math.Abs(side)));
            return grid.LineFree(from,aim) ? aim : goal;
        }
    }
}
