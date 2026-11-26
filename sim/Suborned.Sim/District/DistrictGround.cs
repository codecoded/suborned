using System;
using System.Linq;

namespace Suborned.Sim
{
    // Block coordinates are centimetres, X south and Y east. A and B are the southwest and
    // northeast corners. These same strips are the mesh vertices, picking floor and actor floor.
    public sealed class DistrictGround
    {
        public readonly struct Strip
        {
            public readonly Pt A, B;
            public readonly int WestCm, EastCm;
            public readonly string Id;
            public Strip(JNode n, bool platform)
            {
                A = Pt.Of(n["a"]); B = Pt.Of(n["b"]);
                Id = n[platform ? "id" : "quay"].Str();
                WestCm = n[platform ? "topCm" : "westCm"].Int();
                EastCm = n[platform ? "topCm" : "eastCm"].Int();
            }
            public bool Contains(Pt p) => p.X <= A.X && p.X >= B.X && p.Y >= A.Y && p.Y <= B.Y;
            public int Elevation(Pt p) => WestCm + (EastCm - WestCm) * (p.Y - A.Y) / Math.Max(1, B.Y - A.Y);
            public Pt Centre => new Pt((A.X + B.X) / 2, (A.Y + B.Y) / 2);
        }
        public sealed class Fixture
        {
            public readonly string Kit;
            public readonly Pt At;
            public readonly int Yaw;
            public Fixture(JNode n) { Kit = n["kit"].Str(); At = Pt.Of(n["at"]); Yaw = n["yaw"].Int(); }
        }
        public sealed class Venue
        {
            public readonly string Id, Name;
            public readonly Pt At;
            public readonly Fixture[] Fixtures;
            public Venue(JNode n) { Id = n["id"].Str(); Name = n["name"].Str(); At = Pt.Of(n["at"]); Fixtures = n["fixtures"].Items.Select(f => new Fixture(f)).ToArray(); }
        }

        readonly Grid grid;
        readonly bool[] water;
        public readonly Strip[] Waterfront, Platforms;
        public readonly Venue[] Venues;
        public DistrictGround(JNode data, Grid walk)
        {
            grid = walk;
            water = new bool[grid.Nx * grid.Ny];
            var rows = data["water"].Items;
            for (int i = 0; i < rows.Count; i++)
            {
                var runs = rows[i].Ints();
                for (int k = 0; k < runs.Length; k += 2)
                    for (int j = runs[k]; j < runs[k + 1]; j++) water[i * grid.Ny + j] = true;
            }
            Waterfront = data["waterfront"].Items.Select(s => new Strip(s, false)).ToArray();
            Platforms = data["platforms"].Items.Select(s => new Strip(s, true)).ToArray();
            Venues = data["venues"].Items.Select(v => new Venue(v)).ToArray();
        }
        public bool IsWater(Pt p) => grid.InGrid(p) && water[grid.CI(p.X) * grid.Ny + grid.CJ(p.Y)];
        public int ElevationCm(Pt p)
        {
            foreach (var s in Waterfront) if (s.Contains(p)) return s.Elevation(p);
            foreach (var s in Platforms) if (s.Contains(p)) return s.WestCm;
            return 0;
        }
    }
}
