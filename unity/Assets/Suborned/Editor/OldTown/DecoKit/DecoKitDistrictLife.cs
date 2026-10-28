#if UNITY_EDITOR
using UnityEngine;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// People and traffic north of Southside (which has its own), after the play-zoom shots read the boulevard's walks
    /// and lanes as empty: strollers down both promenades, the circus crescent, Orbit Gardens' walks and lawns, the
    /// harbour terrace and quay; cars down the boulevard's carriageways (the tram keeps its lane, the stop island
    /// closes the west lane), along Market Street and counter-clockwise round the circus ring. Runs after
    /// DecoKitStreetLife and keeps a person only where the footprint clears what stands there. Seeded.
    internal static class DecoKitDistrictLife
    {
        const float Body = .35f, Pair = .35f, Company = .65f;  // footprint radius; share walking in pairs; their spacing
        static readonly Vector2 Headway = new(7f, 16f);
        const float EastLane = 4.2f, WestLane = -6f;           // carriageway centres either side of the tram lane
        static readonly Vector2 Island = new(96f, 128f);       // the tram stop island's z, plus its tapers
        const float CanalX = -78f, PoolR = 12f;                // Orbit Gardens (DecoKitGardens)
        static System.Random rng;
        static DecoKitCitizens city;

        internal static void Build(Transform parent)
        {
            rng = new System.Random(1970);
            city = new DecoKitCitizens("District crowd and traffic", parent, 70);
            Promenades();
            Circus();
            Gardens();
            Harbour();
            Traffic();
            Debug.Log($"[DecoKit] district life: {city.People} people, {city.Cars} cars; {DecoKitCitizens.Tints} tinted materials");
        }

        static float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
        static float Either(float yaw) => (rng.NextDouble() < .5 ? yaw : yaw + 180f) + R(-12f, 12f);

        /// A person at (x, z) if the spot is clear; with `pair`, sometimes a companion `side` of them.
        static void Walker(float x, float z, float y, float yaw, Vector2 side = default)
        {
            if (!DecoKitStreetLife.Claim(x, z, Body)) return;
            city.Person(new Vector3(x, y, z), yaw);
            if (side == default || rng.NextDouble() >= Pair) return;
            var c = new Vector2(x, z) + side * Company;
            if (DecoKitStreetLife.Claim(c.x, c.y, Body)) city.Person(new Vector3(c.x, y, c.y), yaw + R(-6f, 6f));
        }

        /// The walk between the kerb strip's pits and the frontage's urns, both sides, clear of the circus and Market Street.
        static void Promenades()
        {
            foreach (var s in new[] { 1f, -1f })
                for (float z = L.Z.x + R(0f, 3f); z < P.NorthFacade - 6f; z += R(1f, 2.4f))
                {
                    float x = s * R(10.3f, 16.2f);
                    if (!L.Paved(x, z) || s > 0f && L.InSide(z)) continue;
                    Walker(x, z, L.Walk, Either(0f), new Vector2(Mathf.Abs(x) < 15.5f ? s : -s, 0f));
                }
        }

        /// The terrazzo ring under the crescents, off the boulevard's two openings.
        static void Circus()
        {
            for (int k = 0; k < 140; k++)
            {
                float phi = R(0f, 360f), r = R(P.RingR + 1.5f, P.CrescentR - 1.5f);
                var at = P.Circus + P.Dir(phi) * r;
                if (Mathf.Abs(at.x) < L.KerbX + 1.5f) continue;
                Walker(at.x, at.z, L.Walk, Either(phi + 90f), new Vector2(Mathf.Sin(phi * Mathf.Deg2Rad), Mathf.Cos(phi * Mathf.Deg2Rad)) * -1f);
            }
        }

        /// The canal walks (strolling its length), the gate promenade and the basin ring, and the lawns.
        static void Gardens()
        {
            float w = L.Walk;
            var pool = new Vector2(CanalX, P.Circus.z);
            foreach (var s in new[] { 1f, -1f })
                for (float z = P.WestLane.x - 4f; z < pool.y - PoolR - 5f; z += R(1.8f, 4f))
                    Walker(CanalX + s * R(4.3f, 6.6f), z, w, Either(0f), new Vector2(s, 0f));
            for (int k = 0; k < 120; k++)  // the gate promenade, east of the basin to the circus gate, and round the basin
            {
                var at = new Vector2(R(P.GardensX.x + 2f, P.GardensX.y - 1f), R(pool.y - 5f, pool.y + 5f));
                if ((at - pool).magnitude < PoolR + 1.2f) continue;
                Walker(at.x, at.y, w, Either(90f), new Vector2(0f, 1f));
            }
            for (int k = 0; k < 110; k++)  // the lawns: off the canal's walks and the basin
            {
                var at = new Vector2(R(P.GardensX.x + 2f, P.GardensX.y - 2f), R(P.GardensZ.x + 2f, P.GardensZ.y - 2f));
                if (Mathf.Abs(at.x - CanalX) < 10f && at.y < pool.y || (at - pool).magnitude < PoolR + 6f) continue;
                Walker(at.x, at.y, w + .004f, R(0f, 360f));
            }
        }

        /// The upper terrace (north of Southside, whose own terrace has the Harbour Green) and the quay promenade below.
        static void Harbour()
        {
            for (int k = 0; k < 320; k++)
                Walker(R(P.EastBack + 2f, P.StepsX - 2f), R(P.GardensZ.x, P.HarbourZ.y - 10f), L.Walk, Either(0f), new Vector2(1f, 0f));
            for (float z = P.HarbourZ.x + R(0f, 4f); z < P.HarbourZ.y - 10f; z += R(1.5f, 3.5f))
                Walker(R(P.PromenadeX + 1.5f, P.QuayX - 2f), z, P.PromenadeY, Either(0f), new Vector2(1f, 0f));
        }

        static void Traffic()
        {
            bool Road(Vector3 at) => !L.Kerbed(at.x, at.z);  // the circus: ring traffic only
            city.Lane(new(EastLane, L.Z.x + 2f), new(EastLane, P.NorthFacade), Headway, Road);
            city.Lane(new(WestLane, P.NorthFacade), new(WestLane, L.Z.x + 2f), Headway,
                      at => Road(at) || at.z > Island.x && at.z < Island.y);
            float mid = (L.SideZ.x + L.SideZ.y) * .5f;  // Market Street: eastbound on its south side
            city.Lane(new(L.KerbX + 1f, mid - 2.1f), new(L.SideEnd, mid - 2.1f), Headway, _ => false);
            city.Lane(new(L.SideEnd, mid + 2.1f), new(L.KerbX + 1f, mid + 2.1f), Headway, _ => false);
            foreach (var (r, n) in new[] { (P.IslandR + 3.5f, 5), (P.RingR - 3f, 8) })
            {
                float phi0 = R(0f, 360f);
                for (int k = 0; k < n; k++)
                {
                    float phi = phi0 - k * 360f / n + R(-8f, 8f);  // counter-clockwise, right-hand traffic
                    var at = P.Circus + P.Dir(phi) * r;
                    if (!city.NearCar(at)) city.Car(at, phi - 90f);
                }
            }
        }
    }
}
#endif
