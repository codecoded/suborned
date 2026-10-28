#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;
using Q = Suborned.OldTown.Editor.DecoKit.DecoKitSouthside;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Southside's life, for the 2.5D look test: what stands on the pavements (palms or cypresses in the pits, a lamp
    /// every other pit, a kerb programme), parked and moving cars, the square's pavilion, fountains, kiosks and café
    /// terraces, the greens (a rocket monument, the Comet Diner), robots at work, rooftop billboards and a skypad, and a
    /// crowd (DecoKitCitizens). Static: a still of a busy afternoon, ~0.12 people per m2 of walk (the play-zoom shots
    /// read 0.04 and 0.07 as empty). Seeded, so rebuilds match.
    internal static class DecoKitSouthsideLife
    {
        static readonly Vector2 Headway = new(9f, 18f);  // moving cars' spacing per lane
        const double Parking = .8, Pair = .35;          // kerb gaps with a car; walkers with company
        static readonly string[] Kerb = { "RecyclingBins", "BikeRack", "HoverScooter", "InfoLightbox", "Hydrant", "PhoneBooth" };
        static readonly HashSet<string> Cypress = new() { "RL1", "RL2" };  // Rocket Lane: a cypress avenue

        // moving traffic: lane centre lines (from, to), right-hand traffic both ways
        static readonly (Vector2 a, Vector2 b)[] Roads =
        {
            (new(0f, -46f + 0f), new(0f, -98f)),           // Comet Row
            (new(-148f, -46f), new(44f, -46f)),            // Sunset Way
            (new(0f, -152f), new(0f, -296f)),              // Satellite Street
            (new(-94f, -52f), new(-94f, -104f)),           // Garden Row
            (new(-48f, -106f), new(-80f, -107f)),          // Orbit Walk, then west past W2
            (new(-86f, -114.5f), new(-146f, -114.5f)),
            (new(-86f, -253f), new(40f, -253f)),           // Apollo Way, Rocket Lane to Quay Road
            (new(-87f, -118f), new(-87f, -158f)),          // Pylon Row, between W1 and W2
            (new(42f, -154f), new(42f, -296f)),            // Quay Road
        };

        static DecoKitCitizens city;
        static System.Random rng;

        internal static void Build(DecoKitPlacer p, Transform root, List<(Vector3 at, Vector3 inward, DecoKitBlock b)> pits,
                                   List<Vector3> deck)
        {
            rng = new System.Random(1962);
            city = new DecoKitCitizens("Southside crowd and traffic", root, 62);
            Pavements(p, pits);
            Square(p);
            Greens(p, root);
            Robots(p);
            Roofs(p);
            Traffic();
            Crowd(pits);
            foreach (var at in deck) city.Person(at, R(0f, 360f));  // the podium decks' loungers and strollers
            Debug.Log($"[DecoKit] Southside life: {city.People} people, {city.Cars} cars");
        }

        static float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
        static Vector3 W(float x, float z, float y = L.Walk) => new(x, y, z);

        /// Pits: a tree each; a lamp between every other pair, 0.6 m off the kerb; the kerb programme every third;
        /// a car parked against the kerb in most gaps (not across a block end).
        static void Pavements(DecoKitPlacer p, List<(Vector3 at, Vector3 inward, DecoKitBlock b)> pits)
        {
            int i = 0;
            foreach (var (at, inward, b) in pits)
            {
                var s = at + Vector3.up * L.Walk;
                p.Put(Cypress.Contains(b.Name) ? "Cypress" : "CoconutPalm", s, R(0f, 360f));
                float u = Vector3.Dot(at - b.At(0f, 0f), b.Axis);
                if (i % 2 == 0 && u + 5f < b.W * .5f - 2f) p.Put("Streetlamp", s + b.Axis * 5f - inward * .4f, P.Yaw(-inward));
                if (i % 3 == 1) p.Put(Kerb[i / 3 % Kerb.Length], s - b.Axis * 3f - inward * .3f, P.Yaw(inward) + 90f);
                var bay = at + b.Axis * 5f - inward * 2.3f;  // kerbside, unless the face looks onto a green
                if (u + 5f < b.W * .5f - 3f && rng.NextDouble() < Parking && !OnGreen(bay))
                    city.Car(bay, P.Yaw(inward) - 90f);
                i++;
            }
        }

        static void Square(DecoKitPlacer p)
        {
            var c = Q.Square.center;
            p.Put("DecoPavilion", W(c.x, c.y), 0f);
            foreach (var dx in new[] { -26f, 26f }) p.Put("PlazaFountain", W(c.x + dx, c.y), 90f);
            p.Put("StreetClock", W(0f, Q.Square.yMax - 3f), 180f);
            p.Put("CurvedKiosk", W(-14f, -106f), 200f);
            p.Put("CurvedKiosk", W(15f, -144f), 20f);
            p.Put("CoffeeCart", W(10f, -106.5f), 180f);
            p.Put("AdColumn", W(-34f, -104f));
            p.Put("AdColumn", W(36f, -146f));
            for (int k = 0; k < 6; k++)
            {
                float a = k * 60f + 30f;
                p.Put("RoundBench", W(c.x + Mathf.Sin(a * Mathf.Deg2Rad) * 12f, c.y + Mathf.Cos(a * Mathf.Deg2Rad) * 9f), a);
            }
            // café terraces under the two arcades (W1 faces east at x -50, E1 west at x 46)
            foreach (var (x, face) in new[] { (-44.5f, 90f), (42.5f, -90f) })
                for (int k = 0; k < 4; k++)
                {
                    p.Put(k % 2 == 0 ? "CafeSet_Tulip" : "BistroSet", W(x + (k / 2) * (face > 0 ? 2.6f : -2.6f), -133f + (k % 2) * 3.4f - 1.7f), R(0f, 90f));
                    if (k < 2) p.Put("TerraceScreen", W(x + (face > 0 ? 4.3f : -4.3f), -136.5f + k * 7f), face);
                }
            foreach (var (x, z) in new[] { (-30f, -112f), (30f, -112f), (-30f, -138f), (30f, -138f), (-8f, -146f), (8f, -104f) })
                p.Put("FanPalm", W(x, z), R(0f, 360f));
            foreach (var (x, z) in new[] { (-38f, -104f), (38f, -104f), (-38f, -147f), (-20f, -147f) })
                p.Put("PottedPalm_Small", W(x, z), R(0f, 360f));
            p.Put("DrinkingFountain", W(-6f, -118f), 90f);
            p.Put("InfoLightbox", W(6f, -103f), 180f);
            p.Put("SandwichBoard", W(-47.8f, -121f), 90f);
            p.Put("VendingMachine_Cola", W(-12f, -148.9f), 0f);
        }

        static void Greens(DecoKitPlacer p, Transform root)
        {
            // Rocket Green: the monument (the sky's rocket at 45%, nose up; the flame below its base stays inside the
            // stepped limestone plinth) in cypress and topiary
            var m = new Vector2(-17f, -174f);
            new DecoKitGroundMesh().Block(new Rect(m.x - 3f, m.y - 3f, 6f, 6f), L.Walk, L.Walk + .5f)
                                   .Block(new Rect(m.x - 2f, m.y - 2f, 4f, 4f), L.Walk + .5f, L.Walk + 1.3f)
                                   .Emit("Rocket plinth", DecoKitGroundMesh.Mat("DK_Limestone_Tex"), true, root);
            p.Put("RocketShip", W(m.x, m.y, L.Walk + 1.3f), 35f, scale: .45f);
            foreach (var (x, z) in new[] { (-10f, -158f), (-26f, -186f), (-8f, -188f), (-12f, -170f) }) p.Put("Cypress", W(x, z), R(0f, 360f));
            foreach (var (x, z) in new[] { (-21f, -166f), (-13f, -181f), (-23f, -180f), (-7f, -164f) }) p.Put("TopiaryCone", W(x, z), 0f);
            foreach (var (x, z) in new[] { (-6.5f, -176f), (-19f, -188.5f) }) p.Put("FlowerBed", W(x, z), 90f);
            p.Put("Bench", W(-6f, -170f), -90f);
            // Starline Green: the Comet Diner (the Googie pavilion) with its terrace, palms, beds, deckchairs
            p.Put("DecoPavilion", W(21f, -172f), 180f);
            foreach (var (x, z) in new[] { (15f, -163f), (27f, -163f), (21f, -160f) }) p.Put("CafeSet", W(x, z), R(0f, 90f));
            foreach (var (x, z) in new[] { (8f, -156f), (34f, -156f), (8f, -186f), (34f, -186f), (21f, -184f) }) p.Put("FanPalm", W(x, z), R(0f, 360f));
            foreach (var (x, z) in new[] { (10f, -170f), (32f, -174f), (14f, -182f) }) p.Put("ShrubMound", W(x, z), R(0f, 360f));
            foreach (var (x, z) in new[] { (21f, -154.5f), (28f, -188.5f) }) p.Put("FlowerBed", W(x, z), 0f);
            // Launch Green: a fountain garden in the lane's wedge, a cypress screen along the lane, beds and benches
            p.Put("PlazaFountain", W(-53f, -234f), 0f);
            foreach (var t in new[] { 58f, 70f, 82f, 94f, 106f })
            {
                var c = Q.Lane(t, -7.5f);  // 2.5 m in from the kerb
                p.Put("Cypress", W(c.x, c.y), R(0f, 360f));
            }
            foreach (var (x, z) in new[] { (-42.5f, -207f), (-44f, -244f), (-62f, -245f) }) p.Put("FanPalm", W(x, z), R(0f, 360f));
            foreach (var (x, z) in new[] { (-46f, -222f), (-60f, -227f) }) p.Put("FlowerBed", W(x, z), 0f);
            foreach (var (x, z) in new[] { (-45f, -214f), (-66f, -240f) }) p.Put("RoundBench", W(x, z), 0f);
            foreach (var (x, z) in new[] { (-44f, -230f), (-52f, -244f) }) p.Put("ShrubMound", W(x, z), R(0f, 360f));
            // Harbour Green on the terrace: palms, deckchairs toward the sea, telescopes on the step edge
            var h = Q.HarbourGreen;
            for (float z = h.yMin + 4f; z < h.yMax; z += 9f) p.Put("CoconutPalm", W(h.xMin + 3f, z), R(0f, 360f));
            for (float z = h.yMin + 7f; z < h.yMax - 4f; z += 6f)
                foreach (var dx in new[] { 0f, 1.3f }) p.Put("Deckchair", W(h.xMax - 4f + dx, z), 90f + R(-15f, 15f));
            foreach (var z in new[] { -168f, -192f }) p.Put("CoinTelescope", W(P.StepsX - .9f, z), 90f);
            p.Put("LifebuoyStand", W(P.StepsX - .8f, -180f), 90f);
        }

        // (robot, x, z, yaw): yaw 0 faces north
        static readonly (string name, float x, float z, float yaw)[] Crew =
        {
            ("RobotWaiter", -41f, -131f, 110f), ("RobotWaiter", 39.5f, -134.5f, 250f), ("RobotPorter", 44.5f, -112f, 200f),
            ("RobotNanny", 16f, -178f, 150f), ("RobotButler", -12.5f, -70f, 90f), ("RobotShopkeeper", -6.4f, -212f, 80f),
            ("RobotPorter", 5.6f, -236f, 190f), ("RobotButler", -20f, -112f, 160f), ("RobotNanny", -9f, -182f, 30f),
            ("RobotShopkeeper", 12.6f, -84f, 260f), ("RobotWaiter", 57f, -163f, 60f), ("RobotButler", -60f, -52.6f, 200f),
        };

        static void Robots(DecoKitPlacer p)
        {
            foreach (var r in Crew) p.Put(r.name, W(r.x, r.z), r.yaw);
        }

        /// Aircars in the streets' air (the low hover lane at 9-14 m); the roofs are DecoKitDecks'.
        static void Roofs(DecoKitPlacer p)
        {
            p.Put("AirCar_Teal", W(-1.5f, -176f, 11f), 0f);
            p.Put("AirCar_Coral", W(-60f, -46.5f, 13.5f), 90f);
            p.Put("AirCar_Saucer", W(-70f, -104f, 18f), 0f);
        }

        static void Traffic()
        {
            foreach (var (a, b) in Roads.Append((Q.Lane(2f, 0f), Q.Lane(160f, 0f))).Append((Q.Lane(44f, 45f), Q.Lane(172f, 45f))))  // + the mews
            {
                var d = (b - a).normalized;
                var right = new Vector2(d.y, -d.x) * 2.1f;
                city.Lane(a + right, b + right, Headway, OnWalk);
                city.Lane(b - right, a - right, Headway, OnWalk);
            }
        }

        /// People along every pavement (in the facade half, clear of the pits and lamps), strolling either way along
        /// it, some in pairs; groups and passers-by in the square and the greens.
        static void Crowd(List<(Vector3 at, Vector3 inward, DecoKitBlock b)> pits)
        {
            foreach (var b in Q.Built)  // a reserved parcel's crowd is the world plan's
            {
                float hw = b.W * .5f, hd = b.D * .5f;
                var faces = new (float walk, float len, System.Func<float, float, Vector3> at, Vector3 dir)[]
                {
                    (b.WalkR, b.W, (t, o) => b.At(t - hw, hd + o), b.Axis), (b.WalkL, b.W, (t, o) => b.At(t - hw, -hd - o), b.Axis),
                    (b.WalkH, b.D, (t, o) => b.At(hw + o, t - hd), b.Side), (b.WalkT, b.D, (t, o) => b.At(-hw - o, t - hd), b.Side),
                };
                foreach (var (walk, len, at, dir) in faces)
                {
                    if (walk < 4f) continue;
                    float reach = walk > 4f ? walk - 2.2f : 1.9f;  // the facade row; a wide promenade short of its pits
                    for (float t = R(0f, 2f); t < len; t += R(1.2f, 3f))
                    {
                        float o = R(.8f, reach), yaw = P.Yaw(rng.NextDouble() < .5 ? dir : -dir) + R(-12f, 12f);
                        city.Person(at(t, o) + Vector3.up * L.Walk, yaw);
                        if (rng.NextDouble() < Pair) city.Person(at(t, o + .65f > reach ? o - .65f : o + .65f) + Vector3.up * L.Walk, yaw + R(-6f, 6f));
                    }
                }
            }
            var sq = Q.Square;
            for (int k = 0; k < 50; k++)  // groups of two to four, facing in
            {
                var c = new Vector2(R(sq.xMin + 3f, sq.xMax - 3f), R(sq.yMin + 3f, sq.yMax - 3f));
                if (Mathf.Abs(c.x - sq.center.x) < 7f && Mathf.Abs(c.y - sq.center.y) < 7f) continue;
                int n = rng.Next(2, 5);
                for (int j = 0; j < n; j++)
                {
                    float a = j * 360f / n + R(-20f, 20f);
                    var at = c + new Vector2(Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad)) * .75f;
                    city.Person(W(at.x, at.y), a + 180f);
                }
            }
            for (int k = 0; k < 140; k++)  // passers-by: mostly on the Comet Row - Satellite Street desire line
            {
                bool line = rng.NextDouble() < .6;
                var at = line ? new Vector2(R(-7f, 7f), R(sq.yMin, sq.yMax)) : new Vector2(R(sq.xMin + 2f, sq.xMax - 2f), R(sq.yMin + 2f, sq.yMax - 2f));
                if (Mathf.Abs(at.x - sq.center.x) < 6f && Mathf.Abs(at.y - sq.center.y) < 6f) continue;
                city.Person(W(at.x, at.y), (line ? (rng.NextDouble() < .5 ? 0f : 180f) : R(0f, 360f)) + R(-15f, 15f));
            }
            foreach (var (poly, n) in new[] { (Q.RocketGreen, 24), (Q.StarlineGreen, 42), (Q.LaunchGreen, 33) })
            {
                Vector2 lo = new(poly.Min(q => q.x), poly.Min(q => q.y)), hi = new(poly.Max(q => q.x), poly.Max(q => q.y));
                for (int k = 0; k < n; k++)
                {
                    var at = new Vector2(R(lo.x + 1.5f, hi.x - 1.5f), R(lo.y + 1.5f, hi.y - 1.5f));
                    if (Inside(poly, at)) city.Person(W(at.x, at.y, L.Walk + .004f), R(0f, 360f));
                }
            }
        }

        static bool OnWalk(Vector3 at) => Q.Paved.Any(k => k.Paved(at, 1.2f));

        static bool OnGreen(Vector3 q) => new[] { Q.RocketGreen, Q.StarlineGreen, Q.LaunchGreen }.Any(g => Inside(g, new Vector2(q.x, q.z)));

        static bool Inside(Vector2[] poly, Vector2 q)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > q.y) != (poly[j].y > q.y) && q.x < (poly[j].x - poly[i].x) * (q.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }
    }
}
#endif
