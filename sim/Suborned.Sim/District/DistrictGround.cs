using System;
using System.Linq;
using System.Collections.Generic;

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
            public readonly Pt[] Court;
            public readonly Fixture[] Fixtures;
            public Venue(JNode n) { Id = n["id"].Str(); Name = n["name"].Str(); At = Pt.Of(n["at"]); Court = n["court"].Items.Select(Pt.Of).ToArray(); Fixtures = n["fixtures"].Items.Select(f => new Fixture(f)).ToArray(); }
        }

        public sealed class Court
        {
            public readonly string Name, Style;
            public readonly Pt[] Ring;
            public Court(string name, string style, Pt[] ring) { Name = name; Style = style; Ring = ring; }
        }
        public sealed class Building
        {
            public readonly Pt At;
            public readonly int Yaw, Width, Depth, Floors, Palette;
            public Building(JNode n)
            {
                At = Pt.Of(n["at"]); Yaw = n["yaw"].Int(); Width = n["width"].Int();
                Depth = n["depth"].Int(); Floors = n["floors"].Int(); Palette = n["palette"].Int();
            }
        }
        public readonly struct RoofTerrace
        {
            public readonly Pt A, B;
            public readonly int HeightCm;
            public RoofTerrace(JNode n) { A = Pt.Of(n["a"]); B = Pt.Of(n["b"]); HeightCm = n["heightCm"].Int(); }
        }
        public readonly RoofTerrace[] RoofTerraces;
        public readonly Court[] Courts;
        public readonly Building[] Buildings;
        public readonly struct Gallery
        {
            public readonly Pt At;
            public readonly int Yaw, WidthCm, Variant;
            public Gallery(JNode n) { At = Pt.Of(n["at"]); Yaw = n["yaw"].Int(); WidthCm = n["widthCm"].Int(); Variant = n["variant"].Int(); }
        }
        public readonly Gallery[] Galleries;
        public readonly Fixture[] StreetFixtures;
        readonly Dictionary<(int,int), List<Court>> courtCells = new();
        readonly Grid grid;
        readonly bool[] water, walls;
        public readonly Strip[] Waterfront, Platforms;
        public readonly Venue[] Venues;
        public DistrictGround(JNode data, Grid walk)
        {
            grid = walk;
            bool[] Cells(JNode source)
            {
                var result = new bool[grid.Nx * grid.Ny];
                var rows = source.Items;
                for (int i = 0; i < rows.Count; i++)
                {
                    var spans = rows[i].Ints();
                    for (int k = 0; k < spans.Length; k += 2)
                        for (int j = spans[k]; j < spans[k + 1]; j++) result[i * grid.Ny + j] = true;
                }
                return result;
            }
            water = Cells(data["water"]); walls = Cells(data["walls"]);
            Waterfront = data["waterfront"].Items.Select(s => new Strip(s, false)).ToArray();
            Platforms = data["platforms"].Items.Select(s => new Strip(s, true)).ToArray();
            Venues = data["venues"].Items.Select(v => new Venue(v)).ToArray();
            Courts = data["courts"].Items.Select(c => new Court(c["name"].Str(), c["style"].Str(), c["ring"].Items.Select(Pt.Of).ToArray()))
                .Concat(Venues.Where(v => v.Court.Length >= 3).Select(v => new Court(v.Name, "coffee", v.Court))).ToArray();
            StreetFixtures = data["streetFixtures"].Items.Select(x => new Fixture(x)).ToArray();
            Buildings = data["buildings"].Items.Select(x => new Building(x)).ToArray();
            RoofTerraces = data.Has("roofTerraces") ? data["roofTerraces"].Items.Select(x => new RoofTerrace(x)).ToArray() : Array.Empty<RoofTerrace>();
            Galleries = data["galleries"].Items.Select(x => new Gallery(x)).ToArray();
            foreach (var court in Courts)
            {
                int x0 = Grid.FloorDiv(court.Ring.Min(p => p.X), 2400), x1 = Grid.FloorDiv(court.Ring.Max(p => p.X), 2400);
                int y0 = Grid.FloorDiv(court.Ring.Min(p => p.Y), 2400), y1 = Grid.FloorDiv(court.Ring.Max(p => p.Y), 2400);
                for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++)
                {
                    if (!courtCells.TryGetValue((x,y), out var list)) courtCells[(x,y)] = list = new List<Court>();
                    list.Add(court);
                }
            }
        }
        public bool IsWall(Pt p) => grid.InGrid(p) && walls[grid.CI(p.X)*grid.Ny+grid.CJ(p.Y)];
        public bool IsWater(Pt p) => grid.InGrid(p) && water[grid.CI(p.X) * grid.Ny + grid.CJ(p.Y)];
        public int? TransportElevationCm(Pt p)
        {
            foreach (var s in Waterfront) if (s.Contains(p)) return s.Elevation(p);
            foreach (var s in Platforms) if (s.Contains(p)) return s.WestCm;
            return null;
        }
        public int ElevationCm(Pt p)
        {
            if (TransportElevationCm(p) is int elevation) return elevation;
            if (courtCells.TryGetValue((Grid.FloorDiv(p.X, 2400), Grid.FloorDiv(p.Y, 2400)), out var courts))
                foreach (var court in courts) if (G.InPoly(p, court.Ring)) return 18;
            return 0;
        }
    }
}
