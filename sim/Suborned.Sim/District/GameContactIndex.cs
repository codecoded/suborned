using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        // Presentation knowledge. A saved run rebuilds street encounters from the same positions
        // and orders. Browsing or reading a portrait never changes a simulation checkpoint.
        public int ContactCount => District == null ? 0 : District.Encountered.Length;
        public bool IsCivicContact(int i) => i >= District!.Map.People.Length;
        public Person CivicContact(int i) => People[i - District!.Map.People.Length];
        public string ContactId(int i) => IsCivicContact(i) ? CivicContact(i).Id : District!.Map.People[i].Id;
        public string ContactName(int i) => IsCivicContact(i) ? CivicContact(i).Name : District!.Map.People[i].Name;
        public string ContactJob(int i) => IsCivicContact(i) ? CivicContact(i).Job : District!.Map.People[i].Job;
        public Pt? ContactAt(int i) => IsCivicContact(i) ? PosOf(CivicContact(i).Role) : DistrictPersonAt(i);

        public bool ContactKnown(int i)
        {
            var d = District!;
            if (d.Encountered[i] || d.Introduced[i]) return true;
            if (!IsCivicContact(i))
                return d.Memory[i] != null || d.Network.AskedBy[i] != null || d.Trust[i] != 0;
            var p = CivicContact(i);
            return p.KRoutine || p.KNeed || p.KTemperament || p.KSecret || p.Turned;
        }

        public string ContactNewsKey(int i)
        {
            if (!ContactKnown(i)) return "";
            var d = District!;
            if (!IsCivicContact(i))
                return (d.Encountered[i] ? "met/" : "introduced/") + d.Trust[i] + "/" + d.Memory[i] + "/" + d.Network.AskedBy[i] + "/" + d.Network.Fallen[i];
            var p = CivicContact(i);
            return $"{d.Encountered[i]}/{p.KRoutine}/{p.KNeed}/{p.KSecret}/{p.KTemperament}/{p.KMood}/{p.Turned}/{p.Down}/{p.NeedMet}/{string.Join(",", p.KnownRefused)}";
        }

        void IndexStreetEncounters()
        {
            var d = District!;
            for (int i = 0; i < ContactCount; i++)
            {
                if (d.Encountered[i]) continue;
                if (IsCivicContact(i) && ContactKnown(i)) d.Introduced[i] = true;
                if (!IsCivicContact(i) && !d.Roams(i) && !d.Network.Away(i) && !HasCivicActor(d.Map.People[i].Id)) continue;
                if (IsCivicContact(i) && CivicContact(i).Down) continue;
                var at = ContactAt(i);
                if (!at.HasValue) continue;
                foreach (var o in Ops)
                    if (!o.Down && !o.LeftMap && !o.InCar && !o.Inside && G.Within(o.Pos, at.Value, 220))
                    {
                        d.Encountered[i] = true;
                        break;
                    }
            }
        }
    }
}
