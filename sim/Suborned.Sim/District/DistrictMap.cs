using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Suborned.Sim
{
    public sealed class DistrictResident
    {
        public string Id = "", Name = "", Job = "", Look = "", Need = "", Place = "", Errand = "";
        public Path Walk = null!;
        public int Phase;
        public readonly List<(int person, string kind)> Ties = new List<(int, string)>();
    }

    public sealed class DistrictLine
    {
        public string Id = "";
        public Path Path = null!;
    }

    public sealed class DistrictMap
    {
        static readonly Lazy<DistrictMap> shared = new Lazy<DistrictMap>(() => new DistrictMap());
        public static DistrictMap Harbour => shared.Value;
        public readonly Grid Walk, Roads;
        public readonly string Digest;
        public readonly DistrictResident[] People;
        public readonly DistrictLine[] Streets, Trams;
        public readonly Dictionary<string, int> Index = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly int WalkPiece, RoadPiece;

        DistrictMap()
        {
            using var stream = typeof(DistrictMap).Assembly.GetManifestResourceStream("Suborned.Harbour") ?? throw new InvalidDataException("Missing Harbour district");
            using var reader = new StreamReader(stream);
            var n = JNode.Parse(reader.ReadToEnd());
            if (n["schema"].Str() != "suborned.district/1") throw new InvalidDataException("Unknown district format");
            Digest = n["digest"].Str(); Walk = new Grid(n["grid"]); Roads = new Grid(n["roads"]);
            Streets = Lines(n["streets"]); Trams = Lines(n["tram"]);
            var junction = Streets.First(s => s.Id == "road/rocket-lane").Path.End;
            WalkPiece = Walk.PieceOf(Walk.NearestFree(junction)!.Value);
            RoadPiece = Roads.PieceOf(Roads.NearestFree(junction)!.Value);
            var records = n["residents"].Items;
            People = new DistrictResident[records.Count];
            for (int i = 0; i < People.Length; i++)
            {
                var r = records[i]; var stops = r["stops"].Items;
                // Each worker has an address from their actual timetable and a second stop on their round.
                // Some rounds take an errand out along a district street, away from the civic block.
                var home = stops[i % stops.Count]; var other = stops[(i + 1) % stops.Count];
                Pt a = Pt.Of(home["at"]), b = Pt.Of(other["at"]);
                string errand = other["place"].Str();
                if (a == b || i % 3 == 0)
                {
                    var street = Streets[i % Streets.Length];
                    b = street.Path.At(street.Path.Length * (2 + i % 7) / 10);
                    errand = StreetName(street.Id);
                }
                // Stagger neighbours at a shared doorway. Both ends must belong to the street network.
                var offset = new Pt((i % 5 - 2) * 118, (i % 7 - 3) * 106);
                var from = MeetingSite(a + offset); var to = MeetingSite(b + offset);
                var path = Walk.Route(from, to) ?? throw new InvalidDataException("Disconnected district round for " + r["name"].Str());
                var resident = new DistrictResident { Id = r["key"].Str(), Name = r["name"].Str(), Job = r["job"].Str(), Need = r["need"].Str(), Look = r["look"].Str(), Place = home["place"].Str(), Errand = errand, Walk = new Path(path), Phase = stops[0]["from"].Int() * 17 + i * 719 };
                People[i] = resident; Index.Add(resident.Id, i);
            }
            foreach (var t in n["ties"].Items)
            {
                int a = Index[t["a"].Str()], b = Index[t["b"].Str()]; string kind = t["kind"].Str();
                People[a].Ties.Add((b,kind)); if (t["mutual"].Bool()) People[b].Ties.Add((a,kind));
            }
        }
        static DistrictLine[] Lines(JNode n) => n.Items.Select(x => new DistrictLine { Id = x["id"].Str(), Path = new Path(x["points"].Items.Select(Pt.Of).ToArray()) }).ToArray();
        public static string StreetName(string id) { string s = id.Substring(id.IndexOf('/') + 1).Replace('-', ' '); return char.ToUpperInvariant(s[0]) + s.Substring(1); }
        public Pt MeetingSite(Pt p)
        {
            var a = Walk.NearestFree(p);
            if (a.HasValue && Walk.PieceOf(a.Value) == WalkPiece) return a.Value;
            return Walk.NearestFreeIn(p, WalkPiece) ?? throw new InvalidDataException("The district has no accessible meeting place");
        }
        public string StreetAt(Pt point)
        {
            long best=long.MaxValue; string name="Harbour district";
            foreach (var street in Streets)
            {
                var v=street.Path.Points;
                for(int i=1;i<v.Length;i++)
                {
                    var a=v[i-1]; var b=v[i]; long dx=b.X-a.X,dy=b.Y-a.Y,den=dx*dx+dy*dy;
                    if(den==0) continue;
                    long t=Math.Max(0,Math.Min(den,(point.X-a.X)*dx+(point.Y-a.Y)*dy));
                    long d=G.D2(point,new Pt(a.X+(int)(dx*t/den),a.Y+(int)(dy*t/den)));
                    if(d<best){best=d;name=StreetName(street.Id);}
                }
            }
            return name;
        }
        // A round rests at both ends. Integer motion makes schedules and traffic identical on reload.
        public static (Pt at, Pt facing, bool moving) Shuttle(Path path, int tick, int phase, int mmPerTick, int dwell)
        {
            long travel = Math.Max(1, (path.Length + mmPerTick - 1) / mmPerTick);
            long half = travel + dwell, clock = ((long)tick + phase) % (2 * half);
            bool back = clock >= half; long leg = clock % half;
            long mm = Math.Min(path.Length, Math.Max(0,leg-dwell)*mmPerTick);
            mm = back ? path.Length - mm : mm;
            var facing = path.Heading(mm);
            return (path.At(mm), back ? new Pt(-facing.X,-facing.Y) : facing, leg>=dwell && path.Length>0);
        }
    }
}
