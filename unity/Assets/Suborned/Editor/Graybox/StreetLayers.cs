using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Street surfaces: roads (kerb, carriageway, tram reservation), medians, foot malls and walks, Crown Circus.
    /// Everything is drawn straight from the exported polygons; nothing is re-derived.
    /// Carriageways sit at 0; the kerb strip lies 1 cm under them; raised surfaces stand to the pavement height.</summary>
    public static class StreetLayers
    {
        const float KerbLevel = -0.01f, ReservationLevel = 0.005f;
        public const float GardenRise = 0.2f;   // garden bed above the island

        /// <summary>Kerb strip, carriageway and tram reservation per road. A closed road (the Crown Circus gyratory) is an
        /// annulus: its outer rings minus the exported `hole` rings.</summary>
        public static void Roads(Transform root, object world)
        {
            foreach (var r in J.A(world, "roads"))
            {
                string id = J.S(r, "id");
                var g = Nodes.Group(root, id);
                var hole = J.Get(r, "hole");
                if (J.B(r, "closed") != (hole != null)) throw new System.IO.InvalidDataException($"Graybox: road {id}: closed and hole disagree");
                Nodes.Part(g, "kerb", Surface(r, hole, "kerb", KerbLevel), "kerb");
                Nodes.Part(g, "carriageway", Surface(r, hole, "carriageway", 0), "carriageway");
                var res = J.Get(r, "reservation");
                if (res != null) Nodes.Part(g, "reservation", new MeshKit().Flat(J.Poly(res, "polygon"), ReservationLevel), "reservation");
            }
        }

        static MeshKit Surface(object road, object hole, string key, float h) =>
            hole == null ? new MeshKit().Flat(J.Poly(road, key), h) : new MeshKit().Flat(J.Poly(road, key), J.Poly(hole, key), h);

        public static void Medians(Transform root, object world, float pavement)
        {
            foreach (var m in J.A(world, "medians"))
                Nodes.Part(root, J.S(m, "id"), new MeshKit().Prism(J.Poly(m, "polygon"), 0, pavement), "median");
        }

        public static void Foot(Transform root, object world, float pavement)
        {
            foreach (var f in J.A(world, "foot"))
                Nodes.Part(root, J.S(f, "id"), new MeshKit().Prism(J.Poly(f, "polygon"), 0, pavement), "foot");
        }

        /// <summary>Outer pavement as exported: one simple polygon per sector between road entries (the roads stay open), then
        /// island and garden. The gyratory carriageway itself is the crown-circus road.</summary>
        public static void Circus(Transform root, object world, float pavement)
        {
            var c = J.O(world)["circus"];
            int i = 0;
            foreach (var p in J.Polys(c, "pavement")) Nodes.Part(root, "pavement-" + i++, new MeshKit().Prism(p, 0, pavement), "pavement");
            Nodes.Part(root, "island", new MeshKit().Prism(J.Poly(c, "island"), 0, pavement), "circus/island");
            Nodes.Part(root, "garden", new MeshKit().Prism(J.Poly(c, "garden"), pavement, pavement + GardenRise), "circus/garden");
        }
    }
}
