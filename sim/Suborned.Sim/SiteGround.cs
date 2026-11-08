// The site's car ground (brief 096, engine 0.40.0, Keel; Motive's rules page; site `ground`, tools/sim/ground.ts from civic-block.json): the
// bays with their use (Vantage's) and the drives with their kerbs. From them and the drive grid the engine works out, once per rulebook (the
// widths are the rulebook's), what the getaway rules read:
//  · each stand's way out (rulebook car.box): the drive grid's car route from its car cell to the first point of its exit lane, then that lane;
//  · whether each stand is private ground (attention.parked): its car on a drive, or in a bay whose use the rulebook names;
//  · the official car's ways out: from the pickup along its route to the service bay, and out of the service bay along that route backwards,
//    each to the carriageway and then fight-block-m on;
//  · the turned driver's kerbside stop (car.kerb) for each stand that takes the drive he uses.
// A stand whose way out has no route, or a taken drive with no legal kerbside stop, rejects the site (unknown = reject). Derived once, read-only.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Suborned.Sim
{
    public sealed partial class Site
    {
        public sealed class Bay { public string Id = "", Use = ""; public Pt[] Poly = Array.Empty<Pt>(); }
        public sealed class DriveWay { public string Id = "", Road = "", To = ""; public Pt[] Kerb = Array.Empty<Pt>(), Poly = Array.Empty<Pt>(); }
        /// <summary>The block's bays (site ground.bays: id, use public/official/service, polygon) and drives (ground.drives: kerb, polygon, the bay it reaches).</summary>
        public readonly List<Bay> Bays = new List<Bay>();
        public readonly List<DriveWay> Drives = new List<DriveWay>();

        /// <summary>The turned driver's kerbside stop for one taken stand: where his car stands, its heading, and how far along his route (mm) he pulls in.</summary>
        public sealed class KerbStop { public Pt At, Heading; public long Mm; public string Drive = ""; }
        /// <summary>What the getaway rules read from the ground, worked out for one rulebook.</summary>
        public sealed class Ground
        {
            /// <summary>Each stand's way out (car.box), by stand id: a polyline, swept at Half each side.</summary>
            public readonly Dictionary<string, Pt[]> Way = new Dictionary<string, Pt[]>(StringComparer.Ordinal);
            /// <summary>Each stand on private ground (attention.parked), by stand id.</summary>
            public readonly Dictionary<string, bool> Private = new Dictionary<string, bool>(StringComparer.Ordinal);
            /// <summary>The kerbside stop for each stand that takes the turned driver's drive, by stand id.</summary>
            public readonly Dictionary<string, KerbStop> Kerb = new Dictionary<string, KerbStop>(StringComparer.Ordinal);
            /// <summary>The official car's way out from the pickup (its route to the service bay) and out of the service bay (that route backwards).</summary>
            public Pt[] PickupWay = Array.Empty<Pt>(), ServiceWay = Array.Empty<Pt>();
            public long Half, BlockCm;
        }

        void ReadGround(JNode r)
        {
            var gn = r.Opt("ground");
            if (gn == null) { if (!IsRange) throw new FormatException("site.ground is missing: engine 0.40.0 reads the bays' use and the drives' kerbs (brief 096; bun run sim:site)"); return; }
            gn.Only("bays", "drives", "src"); gn["src"].Str();
            foreach (var b in gn["bays"].Items)
            {
                b.Only("id", "use", "polygon");
                var bay = new Bay { Id = b["id"].Str(), Use = b["use"].Str(), Poly = Poly(b["polygon"]) };
                if (bay.Use != "public" && bay.Use != "official" && bay.Use != "service") throw new FormatException($"{b.Path}.use '{bay.Use}' is not public, official or service (brief 096; unknown = reject)");
                if (Bays.Exists(x => x.Id == bay.Id)) throw new FormatException($"{b.Path}: bay {bay.Id} repeats");
                Bays.Add(bay);
            }
            foreach (var d in gn["drives"].Items)
            {
                d.Only("id", "road", "to", "kerb", "polygon");
                var dw = new DriveWay { Id = d["id"].Str(), Road = d["road"].Str(), To = d["to"].Str(), Kerb = Poly(d["kerb"]), Poly = Poly(d["polygon"]) };
                if (dw.Kerb.Length != 2 || dw.Kerb[0] == dw.Kerb[1]) throw new FormatException($"{d.Path}.kerb is two points, the kerb line the drive crosses");
                if (!Bays.Exists(x => x.Id == dw.To)) throw new FormatException($"{d.Path}.to: no bay '{dw.To}'");
                Drives.Add(dw);
            }
        }
        static Pt[] Poly(JNode n) { var l = new List<Pt>(); foreach (var q in n.Items) l.Add(Pt.Of(q)); if (l.Count < 2) throw new FormatException($"{n.Path}: two points at least"); return l.ToArray(); }

        readonly ConditionalWeakTable<Rules, Ground> grounds = new ConditionalWeakTable<Rules, Ground>();
        /// <summary>The ground as this rulebook reads it (car.box widths, car.kerb clearances, attention.parked's private ground), worked out once.</summary>
        public Ground GroundOf(Rules r) => grounds.GetValue(r, k => Derive(k));

        Ground Derive(Rules R)
        {
            var g = new Ground { Half = R.BoxHalfCm, BlockCm = R.Num("v:fight-block-m") * 100 };
            if (IsRange) return g; // the range: no stands that leave, no drive, no private ground
            if (Grid.HasFine && Grid.Body != R.WalkClearCm) throw new FormatException($"site grid.body is {Grid.Body} cm but the rulebook's walk clearance is {R.WalkClearCm} (bodies.radius + bodies.margin): rebuild the site (bun run sim:site; unknown = reject)"); // engine 0.49.0
            foreach (var st in Stands)
            {
                var start = Drive.NearestFree(st.Exit[0]) ?? throw new FormatException($"getaway.stands {st.Id}: its exit lane's first point has no drivable cell (car.box)");
                var route = Drive.CarRoute(st.CarCell, start, DriveLaneCm) ?? throw new FormatException($"getaway.stands {st.Id}: its way out has no route on the drive grid from its car to its exit lane (rulebook car.box; unknown = reject)");
                var way = new List<Pt>(route);
                for (int i = 1; i < st.Exit.Length; i++) if (st.Exit[i] != way[way.Count - 1]) way.Add(st.Exit[i]);
                g.Way[st.Id] = way.ToArray();
                g.Private[st.Id] = PrivateAt(st.CarAt, R);
            }
            if (CarServicePath.Length < 2) return g;
            var path = new Path(CarServicePath);
            var back = new Pt[CarServicePath.Length]; for (int i = 0; i < back.Length; i++) back[i] = CarServicePath[CarServicePath.Length - 1 - i];
            g.PickupWay = WayAlong(path, g.BlockCm); g.ServiceWay = WayAlong(new Path(back), g.BlockCm);
            foreach (var st in Stands)
                foreach (var d in Drives)
                {
                    if (!CarGeo.Inside(st.CarAt, d.Poly)) continue;
                    var k = KerbFor(path, d, g.Way[st.Id], R, g.Half);
                    if (k == null) { if (Enters(path, d) >= 0) throw new FormatException($"car.kerb: no kerbside stop short of drive {d.Id} clear of it and of stand {st.Id}'s way out on the turned driver's route (unknown = reject)"); continue; }
                    g.Kerb[st.Id] = k;
                }
            return g;
        }
        /// <summary>A car's spot is private ground: on a drive, or in a bay whose use the rulebook's attention.parked.ground names.</summary>
        public bool PrivateAt(Pt p, Rules R)
        {
            foreach (var d in Drives) if (CarGeo.Inside(p, d.Poly)) return R.ParkedGround.Contains("drive");
            foreach (var b in Bays) if (CarGeo.Inside(p, b.Poly)) return R.ParkedGround.Contains(b.Use);
            return false;
        }
        /// <summary>A path to where it first reaches a carriageway, then blockCm on along it while it stays on the carriageway (the whole path if it
        /// never reaches one): "its drive to the road, then 30 m of road".</summary>
        Pt[] WayAlong(Path p, long blockCm)
        {
            long r0 = -1; for (long mm = 0; mm <= p.Length; mm += 100) if (Drive.SurfaceAt(p.At(mm)) == 'r') { r0 = mm; break; }
            if (r0 < 0) return p.Points;
            long end = Math.Min(p.Length, r0 + blockCm * 10);
            for (long mm = r0 + 1000; mm <= end; mm += 100) if (Drive.SurfaceAt(p.At(mm)) != 'r') { end = mm; break; }
            return CarGeo.Upto(p, end);
        }
        /// <summary>The first mm of the path inside the drive, or -1.</summary>
        static long Enters(Path p, DriveWay d) { for (long mm = 0; mm <= p.Length; mm += 100) if (CarGeo.Inside(p.At(mm), d.Poly)) return mm; return -1; }

        /// <summary>Rulebook car.kerb: along the route, back from where it enters the drive a centimetre at a time, the first point whose footprint
        /// grown by kerb.clear meets neither the drive nor the way out; moved square across to the kerb line until its edge is kerb.edge off it;
        /// held to the same test. Null when the route does not use the drive, or no point is clear.</summary>
        static KerbStop? KerbFor(Path route, DriveWay d, Pt[] way, Rules R, long half)
        {
            long enter = Enters(route, d); if (enter < 0) return null;
            long hl = R.CarLenCm / 2, hw = R.CarWCm / 2, cl = R.KerbClearCm;
            bool Clear(Pt c, Pt h) { var grown = CarGeo.Rect(c, h, hl + cl, hw + cl); return !CarGeo.PolysMeet(grown, d.Poly) && !CarGeo.OnWay(grown, way, half); }
            Pt k0 = d.Kerb[0], k1 = d.Kerb[1]; long kx = k1.X - k0.X, ky = k1.Y - k0.Y, kl = Math.Max(1, G.Isqrt(kx * kx + ky * ky));
            for (long mm = enter; mm >= 0; mm -= 100)
            {
                Pt c = route.At(mm), h = route.Heading(mm);
                if (h.X == 0 && h.Y == 0) continue;
                if (!Clear(c, h)) continue;
                long sd = (kx * (c.Y - k0.Y) - ky * (c.X - k0.X)) / kl; // signed cm from the kerb line along its normal (-ky, kx)
                long hn = Math.Max(1, G.Isqrt((long)h.X * h.X + (long)h.Y * h.Y));
                long ext = (Math.Abs(hl * (h.X * -ky + h.Y * kx)) + Math.Abs(hw * (-h.Y * -ky + h.X * kx))) / (hn * kl); // the footprint's reach along the normal
                long move = Math.Abs(sd) - (ext + R.KerbEdgeCm), sign = sd >= 0 ? 1 : -1;
                var at = new Pt((int)(c.X - sign * -ky * move / kl), (int)(c.Y - sign * kx * move / kl));
                if (!Clear(at, h)) continue;
                return new KerbStop { At = at, Heading = h, Mm = mm, Drive = d.Id };
            }
            return null;
        }
    }
}
