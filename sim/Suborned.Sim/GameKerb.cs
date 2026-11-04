// The official car's drive round and where it stops (brief 059 K2, K5; brief 096, engine 0.40.0: rulebook car.kerb and car.box; Motive's
// rules page). Moved here from Game.cs with the kerbside stop.
//  · The turned driver drives Vale round from the pickup when Vale gets in, unless a standing car boxes his way out from the pickup: then he
//    waits there with Vale aboard and tries again each second (car.box; in the Handover nothing can stand there, but the rule is every car's).
//  · As he comes up to his kerbside stop (Site.Ground.Kerb, worked out at load), a getaway car standing at a stand on his drive, or driving
//    there, takes the drive: he pulls in at the kerb short of it, nose the way he was driving (car.kerbside); another standing car there sends
//    him back along his route until his footprint is clear of it. Taken only after he has passed that point, he stops where he is at the
//    drive's mouth (car.blocked, the old stop: a tick's window).
//  · At the close the turned driver gives up and drives Vale on, unless his way out (out of the service bay, or on from the kerb) is boxed: then
//    he stays, and the verdict is closed-in, cause boxed.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Brief 096 (car.kerb): the official car pulled in at the kerb short of a taken drive (CarAtMouth is set too: stopped short).</summary>
        public bool CarAtKerb;
        Pt kerbHeading; bool kerbDecided; int pickupBoxedSince = -1;

        /// <summary>The turned driver starts his drive round with Vale aboard, or, his way out from the pickup boxed, waits (car.box).</summary>
        void StartValeDrive()
        {
            var bx = OfficialBoxers();
            if (bx.Count > 0)
            {
                if (pickupBoxedSince < 0) Note("car.boxed-in", $"{Cap(ValeName)}'s car is boxed in at the pickup by {string.Join(", ", bx)}: the driver waits.");
                pickupBoxedSince = Tick; return;
            }
            pickupBoxedSince = -1;
            valeDriveStart = Tick; carToService = new Path(S.CarServicePath); carDrive = CarProfile(carToService); carMouthMm = MouthMm(carToService);
            Note("car.leaves", $"{Cap(ValeName)} gets into his car. The turned driver drives him round to the service bay.");
            ValeCarMoves(); // F2: the rival car follows rivals.follow later
        }

        /// <summary>Brief 059 K2, K5 and brief 096 (car.kerb): the turned driver's drive round with Vale in the back. As he comes up to his kerbside
        /// stop, a getaway car on his drive (standing at a stand on it, or driving there) makes him pull in at the kerb short of it; taken only
        /// after he has passed that point, he stops at the drive's mouth on Apron Road. The car parks (car.parks) at the end of its drive.</summary>
        void CarTimeline()
        {
            if (valeDriveStart < 0 && pickupBoxedSince >= 0 && Vale == ValeAt.InCar && Tick % Hz == 0 && !Driver.Down && !OfficialOut) { StartValeDrive(); return; } // car.box: boxed in at the pickup, he tries again each second
            if (valeDriveStart < 0 || carDrive == null || carStoppedAt.HasValue) return;
            int kNow = Tick - valeDriveStart;
            if (!kerbDecided && kNow >= 0 && kNow + 1 < carDrive.Length && KerbFirst() is Site.KerbStop first && carDrive[kNow + 1] > first.Mm) // he is coming up to the kerbside stop: decide now
            {
                kerbDecided = true;
                var st = getawayPath != null ? GetawayDest : CurStand;
                if (st != null && S.GroundOf(R).Kerb.TryGetValue(st.Id, out var k)) PullIn(k, carDrive[kNow]);
            }
            if (!mouthDecided && carMouthMm >= 0 && kNow >= 0 && kNow + 1 < carDrive.Length && carDrive[kNow + 1] > carMouthMm) // its next tick would pass the mouth: decide now, still on the road
            {
                mouthDecided = true;
                if (!CarAtKerb && DriveTaken())
                {
                    int k = Math.Max(0, Math.Min(kNow, carDrive.Length - 1));
                    var cut = new long[k + 1]; Array.Copy(carDrive, cut, k + 1); carDrive = cut; CarAtMouth = true;
                    Note("car.blocked", "Our car is on the service drive: the driver stops at its mouth on Apron Road.");
                }
            }
            if (Tick == CarInBayTick)
            {
                PickupService = Vale == ValeAt.InCar || Vale == ValeAt.Taking;
                Note("car.parks", CarAtKerb ? $"{Cap(ValeName)}'s car pulls in at the kerb short of the service drive. Take him from the back seat." : CarAtMouth ? $"{Cap(ValeName)}'s car stops at the mouth of the service drive. Take him from the back seat." : $"{Cap(ValeName)}'s car parks in the service bay. Take him from the back seat.");
                SawGetaway(InCarName()); // Motive M059-2.4: our car already behind his
            }
        }
        /// <summary>The earliest kerbside stop along his route (by stand order), or null: where he decides.</summary>
        Site.KerbStop? KerbFirst()
        {
            Site.KerbStop? k = null; var kerb = S.GroundOf(R).Kerb;
            foreach (var st in S.Stands) if (kerb.TryGetValue(st.Id, out var x) && (k == null || x.Mm < k.Mm)) k = x;
            return k;
        }
        /// <summary>car.kerb: he pulls in at the stop (or, another standing car there, back along his route a walk cell at a time until his footprint
        /// is clear of it, never behind where he is now): his route is cut there and ends at the stop, nose the way he was driving.</summary>
        void PullIn(Site.KerbStop k, long nowMm)
        {
            var off = k.At - carToService!.At(k.Mm); long mm = k.Mm; Pt at = k.At;
            long hl = R.CarLenCm / 2, hw = R.CarWCm / 2;
            while (mm > nowMm)
            {
                var me = CarGeo.Rect(at, k.Heading, hl, hw); bool clear = true;
                foreach (var c in StandingCars()) if (c.Id != "car:official" && !(CarOf(c.Id) is CarTarget t && t.Blown) && CarGeo.PolysMeet(me, CarGeo.Rect(c.At, c.Heading, hl, hw))) { clear = false; break; }
                if (clear) break;
                mm = Math.Max(nowMm, mm - 500); at = carToService.At(mm) + off;
            }
            var pts = new List<Pt>(carToService.Cut(mm).Points); if (pts[pts.Count - 1] != at) pts.Add(at);
            carToService = new Path(pts.ToArray()); carDrive = CarProfile(carToService);
            CarAtKerb = true; CarAtMouth = true; kerbHeading = k.Heading;
            Note("car.kerbside", $"Our car is on the service drive: the driver pulls in at the kerb short of it ({Sim.Report.Clock(CarInBayTick)}).");
        }
        /// <summary>K5: the service drive is taken: the getaway car stands at its service-drive stand, or is driving there.</summary>
        bool DriveTaken()
        {
            var st = getawayPath != null ? GetawayDest : CurStand; if (st == null || carToService == null || carMouthMm < 0) return false;
            for (long at = carMouthMm; at <= carToService.Length; at += 100) if (G.Within(carToService.At(at), st.CarCell, 200)) return true; // a stand on the drive the driver must use
            return false;
        }
        /// <summary>The mm along the route to the service bay where it leaves the carriageway for the drive (the drive's mouth: the last road
        /// point after the official bay's own cells), or -1.</summary>
        long MouthMm(Path p)
        {
            bool road = false;
            for (long at = 0; at <= p.Length; at += 50)
            {
                bool r = S.Drive.SurfaceAt(p.At(at)) == 'r';
                if (r) road = true; else if (road) return Math.Max(0, at - 50);
            }
            return -1;
        }

        /// <summary>Brief 059 K4 at the close: the turned driver gives up and drives Vale on (vale-leaves), unless his car is boxed in (car.box):
        /// then he stays, and the verdict is closed-in, cause boxed, naming the car in his way.</summary>
        void GivesUp()
        {
            if (Vale == ValeAt.InCar && OfficialBoxers() is var bx && bx.Count > 0)
            {
                Note("car.boxed-in", $"The driver would give up and drive {ValeName} on, but {string.Join(", ", bx)} boxes his car in.");
                Fail("closed-in", $"The district closes: {ValeName}'s car is boxed in by {string.Join(", ", bx)}.", () => Because("boxed", true, Locate(LocateCar(bx[0]), "closed-in"), Locate("car", "victim")));
                return;
            }
            if (Vale == ValeAt.InCar) Note("car.leaves", $"The driver gives up waiting and drives {ValeName} away.");
            Fail("vale-leaves", $"{Cap(ValeName)} leaves the district in the official car.", () => Because("too-late", true, Locate("car", "left"), Locate("councillor", "left")));
        }
        /// <summary>A car id as Locate names it (the official car "car", the getaway car "getaway-car", the rival car its own id).</summary>
        static string LocateCar(string id) => id == "car:official" ? "car" : id == "car:getaway" ? "getaway-car" : id;
    }
}
