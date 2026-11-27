using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    internal sealed partial class DistrictPavement
    {
        // A coat and a foot in mid-stride. The three extra cm cover rounding between sim ticks.
        const int PersonCm = 36;
        static readonly (int cos,int sin)[] Turns = { (100,0), (87,50), (50,87), (0,100), (-50,87), (-87,50), (-100,0) };
        static long Dot(Pt a, Pt b) => (long)a.X*b.X + (long)a.Y*b.Y;

        bool Clear(Pt at, List<int>? nearby)
        {
            if (nearby == null) return true;
            foreach (int r in nearby)
                if (G.Within(machines[r].At,at,radius[r]+PersonCm)) return false;
            return true;
        }

        static Pt Rotate(Pt h, int cos, int sin) =>
            new Pt((h.X*cos-h.Y*sin)/100,(h.X*sin+h.Y*cos)/100);

        Pt Place(Pt origin, int id, Grid floor)
        {
            if (Clear(origin,Near(origin))) return origin;
            // At district opening, choose a vacant patch around a shared doorway before showing anyone.
            for (int distance = 35; distance <= 700; distance += 35)
            foreach (var turn in Turns)
            for (int side = -1; side <= 1; side += 2)
            {
                var direction = Rotate((id&1)==0 ? new Pt(100,0) : new Pt(0,100),turn.cos,side*turn.sin);
                var candidate = CarGeo.Ahead(origin,direction,distance);
                if (floor.LineFree(origin,candidate) && Clear(candidate,Near(candidate))) return candidate;
            }
            return origin;
        }

        Pt Walk(Pt from, Pt goal, Pt facing, bool walking, int stride, int id, ref int pass, Grid floor, bool arriving = false)
        {
            var nearby = Near(from);
            var atGoal = Near(goal);
            if (nearby == null && atGoal == null)
            {
                pass = 0;
                if (G.Within(from,goal,stride)) return goal;
            }
            var aim = goal;
            // On the last few steps a safe clicked spot takes precedence over the passing lane.
            bool approach = arriving && Clear(goal,atGoal) && G.Within(from,goal,180);
            if (atGoal != null && !approach) aim = PassingPoint(from,goal,facing,walking,id,ref pass,atGoal,floor);
            else pass = 0;
            if (from == aim) return from;
            var delta = aim-from;
            int length = G.Dist(from,aim);
            var straight = length <= stride ? aim : G.Lerp(from,aim,stride,length);
            bool Safe(Pt p) => floor.LineFree(from,p) && Clear(p,Near(p)) && Clear(G.Lerp(from,p,1,2),nearby);
            if (Safe(straight)) return straight;

            // A narrow pavement or a second machine can close one side. Try the other, then wait.
            var heading = CarGeo.Ahead(default,delta,100);
            int first = pass != 0 ? Math.Sign(pass) : ((id&1)==0 ? 1 : -1);
            for (int i = 1; i < Turns.Length-1; i++)
            {
                var turn = Turns[i];
                for (int n=0; n<2; n++)
                {
                    var direction = Rotate(heading,turn.cos,turn.sin*(n==0 ? first : -first));
                    var point = CarGeo.Ahead(from,direction,Math.Min(length,stride));
                    if (G.D2(point,aim) < G.D2(from,aim) && Safe(point)) return point;
                }
            }
            return from;
        }

        Pt PassingPoint(Pt from, Pt goal, Pt heading, bool walking, int id, ref int pass, List<int> nearby, Grid floor)
        {
            var forward = CarGeo.Ahead(default,heading,100);
            int best = -1, weight = 0, across = 0, gap = 0;
            Pt normal = default;
            foreach (int r in nearby)
            {
                var localForward = walking ? forward : CarGeo.Ahead(default,machines[r].Facing,100);
                var side = new Pt(-localForward.Y,localForward.X);
                var offset = goal-machines[r].At;
                int along = Math.Abs((int)(Dot(offset,localForward)/100));
                int lateral = (int)(Dot(offset,side)/100);
                int space = radius[r]+PersonCm+30;
                int full = space*3/4;
                // Ease into the passing line about two metres ahead, then ease back onto the route.
                int strength = Math.Clamp((space+170-along)*100/(space+170-full),0,100);
                int score = (space-Math.Abs(lateral))*strength;
                if (score <= weight) continue;
                best=r; weight=score; across=lateral; gap=space; normal=side;
            }
            if (best<0) { pass=0; return goal; }
            int preferred = Math.Sign(across);
            if (Math.Abs(across)<14) preferred = ((id+best)&1)==0 ? 1 : -1;
            if (Math.Abs(pass)==best+1) preferred=Math.Sign(pass);
            else
            {
                long stood = Dot(from-machines[best].At,normal);
                if (Math.Abs(stood)>1800) preferred=Math.Sign(stood);
            }
            int influence = weight/Math.Max(1,gap-Math.Abs(across));
            for (int attempt = 0; attempt < 2; attempt++)
            {
                int sign=attempt==0 ? preferred : -preferred;
                var target = CarGeo.Ahead(goal,normal,(sign*gap-across)*influence/100);
                if (!floor.Free(target) || !floor.LineFree(goal,target)) continue;
                pass = sign*(best+1);
                return target;
            }
            return goal;
        }
    }
}
