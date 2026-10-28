#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The boulevard's everyday kit, placed last (after the sky rail's pylons). The pavements were lamps, palms and
    /// benches on a fixed cadence, and read empty. Each promenade has three strips:
    ///   kerb (|x| ~9, between the palm pits): a programme repeating every 80 m (bins, bike racks, a scooter, an info
    ///     box, a phone booth, a hydrant), offset between the sides so they don't mirror across the road; signals at
    ///     Market Street and the circus mouths; a shelter and ticket machine at the tram stop
    ///   walk (|x| 10..16): kept clear but for the city's robots, going about their work where the shots can meet them:
    ///     a porter wheeling luggage to the tram, a butler at the hotel door, a nanny with her pram, waiters at the café
    ///     terraces, a shopkeeper waving from a shopfront
    ///   frontage (|x| ~17.5): urns flanking every fourth bay, a trough under the windows two bays on, topiary where
    ///     a run of bays ends
    /// and flat detail that obstructs nothing: drain grates in the gutters, manhole covers in the lanes and the walks.
    /// A candidate is kept only if its footprint clears everything standing there (manifest sizes as circles; trees
    /// by their trunks). The HQ forecourt north of the frontage stays ceremonial and bare.
    internal static class DecoKitStreetLife
    {
        const float KerbStrip = 9.2f, Front = L.FrontX, Gap = .2f, LastZ = P.NorthFacade - 6f;
        static readonly string[] Kerb = { "RecyclingBins", "BikeRack", "HoverScooter", "InfoLightbox", "RecyclingBins", "PhoneBooth", "BikeRack", "Hydrant" };
        // (robot, x, z, yaw): yaw 0 faces north. The west walk's porter and nanny stand short of the rail pylon at z ~105
        // (it hides the walk behind it from the promenade shot); waiters at the open ends of the east terraces (screens x 13.4)
        static readonly (string name, float x, float z, float yaw)[] Robots =
        {
            ("RobotPorter", -11.5f, 101f, 195f), ("RobotButler", -16.3f, 104.5f, 135f), ("RobotNanny", -14.8f, 102.5f, 165f),
            ("RobotShopkeeper", -16.4f, 140f, 120f), ("RobotWaiter", 15f, 49.5f, 200f), ("RobotWaiter", 14.6f, 258.5f, 210f),
            ("RobotShopkeeper", 16.4f, 288f, 240f), ("RobotNanny", 12.4f, 300f, 175f), ("RobotButler", 16.3f, 98.5f, 225f),
        };
        static readonly Regex Bay = new(@"^(StreamBay|DecoBay|PortholeBay|BalconyBay|ArcadeBay|HallBay)");
        // ground-standing kit that blocks a spot; facades, ground, sky, roofs and the tram in its lane don't
        static readonly HashSet<string> Standing = new() { "Street", "Furnish", "Booths", "Signals", "Structures", "Plants", "Plaza", "Signage", "Robots" };

        static Dictionary<string, DecoKitData.AssetRow> rows;
        static List<(Vector2 at, float r)> taken;
        static readonly Dictionary<string, int> placed = new();
        static int refused;

        internal static void Build(DecoKitPlacer p)
        {
            rows = DecoKitData.Assets().assets.ToDictionary(r => r.name);
            taken = p.Placed().Where(x => Blocks(x.name, x.pos)).Select(x => (new Vector2(x.pos.x, x.pos.z), Radius(x.name))).ToList();
            placed.Clear();
            refused = 0;
            Signals(p);
            TramStop(p);
            foreach (var s in new[] { 1f, -1f }) KerbProgramme(p, s);
            foreach (var s in new[] { 1f, -1f }) Frontage(p, s, BayCentres(p, s));
            foreach (var r in Robots) TryAlong(p, r.name, r.x, r.z, r.yaw);
            Flat(p);
            Debug.Log($"[DecoKit] street life: {string.Join(", ", placed.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value}"))}; {refused} spots refused");
        }

        static bool Blocks(string name, Vector3 pos) =>
            name.StartsWith("TreePit_") || name == "SkyRail_Pylon_Tall"
            || rows.TryGetValue(name, out var r) && Standing.Contains(r.category) && !Bay.IsMatch(name) && !name.StartsWith("Hall")
               && pos.y < L.Walk + .5f;  // wall signs and roof boards hang clear

        static float Radius(string name) =>
            name.StartsWith("TreePit_") ? 1f
            : !rows.TryGetValue(name, out var r) ? .5f
            : r.category == "Plants" ? .6f  // trunks: canopies are overhead
            : Mathf.Max(r.size[0], r.size[2]) * .5f;

        /// Places `name` if its footprint clears everything taken; the flat pieces take nothing.
        static bool Try(DecoKitPlacer p, string name, float x, float z, float yaw, float y = L.Walk, bool flat = false)
        {
            float r = Radius(name);
            var at = new Vector2(x, z);
            if (!flat && taken.Any(t => (t.at - at).magnitude < t.r + r + Gap)) { refused++; return false; }
            if (p.Put(name, new Vector3(x, y, z), yaw) == null) return false;
            if (!flat) taken.Add((at, r));
            placed[name] = placed.GetValueOrDefault(name) + 1;
            return true;
        }

        /// Takes a footprint for a later pass (DecoKitDistrictLife's people) if it clears everything standing there.
        internal static bool Claim(float x, float z, float r)
        {
            var at = new Vector2(x, z);
            if (taken.Any(t => (t.at - at).magnitude < t.r + r + Gap)) return false;
            taken.Add((at, r));
            return true;
        }

        /// The first spot within 3 m along the kerb that clears (off a lamp, a pit or a signal); refusals count once.
        static void TryAlong(DecoKitPlacer p, string name, float x, float z, float yaw)
        {
            int before = refused;
            foreach (var dz in new[] { 0f, 1.5f, -1.5f, 3f, -3f })
                if (Try(p, name, x, z + dz, yaw)) { refused = before; return; }
            refused = before + 1;
        }

        /// Signals face the traffic that meets them: at Market Street's mouth and where the boulevard enters the circus.
        static void Signals(DecoKitPlacer p)
        {
            foreach (var s in new[] { 1f, -1f })
            {
                TryAlong(p, "TrafficLight", s * 8.6f, L.SideZ.x - 3.7f, 180f);
                TryAlong(p, "TrafficLight", s * 8.6f, L.SideZ.y + 3.7f, 0f);
                TryAlong(p, "TrafficLight", s * 8.6f, P.CircusSouth - 6f, 180f);
                TryAlong(p, "TrafficLight", s * 8.6f, P.CircusNorth + 6f, 0f);
            }
        }

        /// The tram stop island's pavement end: a shelter facing the road between the palms, the ticket machine by it.
        static void TramStop(DecoKitPlacer p)
        {
            Try(p, "BusShelter", -11.4f, 116f, 90f);
            Try(p, "TicketMachine", -10.7f, 119.6f, 90f);
        }

        /// Between the palm pits (east pits at z 5 mod 10, west at 1 mod 10), the programme in order along each kerb.
        static void KerbProgramme(DecoKitPlacer p, float s)
        {
            int k = s > 0f ? 0 : 4;
            for (float z = s > 0f ? -30f : -34f; z < LastZ; z += 10f, k++)
            {
                float x = s * KerbStrip;
                if (!L.Paved(x, z) || s > 0f && L.InSide(z)) continue;
                var name = Kerb[k % Kerb.Length];
                float yaw = name switch
                {
                    "InfoLightbox" => 180f,                // panels to the walkers
                    "HoverScooter" => s > 0f ? -65f : 65f,  // nose to the road, angled
                    _ => s > 0f ? 90f : -90f,              // faces the walk
                };
                TryAlong(p, name, name == "Hydrant" ? s * 8.75f : x, z, yaw);
            }
        }

        /// Bay centres on this side's boulevard facade (pivots on the facade line), south to north.
        static List<float> BayCentres(DecoKitPlacer p, float s) =>
            p.Placed().Where(x => Bay.IsMatch(x.name) && Mathf.Abs(x.pos.x - s * Front) < .5f && x.pos.z > L.Z.x && x.pos.z < P.NorthFacade)
             .Select(x => x.pos.z).OrderBy(z => z).ToList();

        /// Along each unbroken run of bays: urns flanking bays 1, 5, 9.., a trough under bays 3, 7.., topiary at the ends.
        static void Frontage(DecoKitPlacer p, float s, List<float> bays)
        {
            float yaw = s > 0f ? -90f : 90f;  // faces the walk
            for (int i = 0; i < bays.Count;)
            {
                int j = i;
                while (j + 1 < bays.Count && bays[j + 1] - bays[j] < 6.5f) j++;
                for (int b = i; b <= j; b++)
                {
                    float z = bays[b];
                    if ((b - i) % 4 == 1)
                        foreach (var dz in new[] { -2.2f, 2.2f }) Try(p, "Planter_Urn", s * (Front - .45f), z + dz, yaw);
                    else if ((b - i) % 4 == 3) TryAlong(p, "Planter_Trough", s * (Front - .4f), z, yaw);
                }
                Try(p, "PottedTopiary", s * (Front - .6f), bays[i] - 2.2f, yaw);
                if (j > i) Try(p, "PottedTopiary", s * (Front - .6f), bays[j] + 2.2f, yaw);
                i = j + 1;
            }
        }

        /// Drain grates in the gutter every 20 m (the kerb's 0.375 m sett gutter, road deck at -0.002), manhole covers
        /// down the traffic lanes (clear of the tram rails and the island) and a few in the walks.
        static void Flat(DecoKitPlacer p)
        {
            foreach (var s in new[] { 1f, -1f })
            {
                for (float z = s > 0f ? -27f : -37f; z < L.Z.y; z += 20f)
                    if (L.Kerbed(s * L.KerbX, z) && !(s > 0f && L.InSide(z))) Try(p, "DrainGrate", s * 7.7f, z, 90f, .003f, true);
                float lane = s > 0f ? 4.2f : -6f;  // west: between the rails (x -2.5) and the kerb
                for (float z = s > 0f ? 15f : 38f; z < L.Z.y; z += 45f)
                    if (L.Kerbed(lane, z) && (s > 0f || z < 98f || z > 126f)) Try(p, "ManholeCover", lane, z, z * 7f, -.001f, true);
                for (float z = s > 0f ? 31f : 56f; z < LastZ; z += 50f)
                    if (L.Paved(s * 12.6f, z) && !taken.Any(t => (t.at - new Vector2(s * 12.6f, z)).magnitude < t.r + .5f))
                        Try(p, "ManholeCover", s * 12.6f, z, z * 7f, L.Walk + .001f, true);
            }
        }
    }
}
#endif
