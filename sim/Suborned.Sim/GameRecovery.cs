// Recovery is measured in mission ticks. Removing a casualty from the street never removes the incident.
using System;
using System.Collections.Generic;
namespace Suborned.Sim
{
    public sealed partial class Game
    {
        const int RecoverySeconds = 15;
        readonly SortedDictionary<string, int> recoverySince = new SortedDictionary<string, int>(StringComparer.Ordinal);

        bool ReadyToCollect(string key, bool available)
        {
            if (!available) { recoverySince.Remove(key); return false; }
            if (!recoverySince.TryGetValue(key, out int since)) { recoverySince[key] = Tick; return false; }
            if (Tick - since < T(RecoverySeconds)) return false;
            recoverySince.Remove(key); return true;
        }

        bool ResponderAt(Pt casualty)
        {
            bool Reaches(Pt p) => G.Within(p, casualty, 200) && Sees(p, casualty);
            for (int i = 0; i < SecGuns.Length; i++)
                if (!SecGuns[i].Down && SecOfficerPose(i) is { } officer && Reaches(officer.pos)) return true;
            foreach (var u in Units)
                if (!u.Robot && !u.Legacy && u.Gun != null && !u.Gun.Down && UnitPose(u) is { } pose && Reaches(pose.pos)) return true;
            return false;
        }

        void RecoveryStep()
        {
            if (!Start.RecoverCasualties || Tick % Hz != 0) return;
            bool quiet = LastShotTick < 0 || Tick - LastShotTick >= T(R.CrowdQuietS);
            foreach (var b in Bodies)
            {
                if (!b.Dead || b.Recovered) continue;
                // A human responder must still be able to see and reach the body after the scene quiets.
                if (!ReadyToCollect("body/" + b.Id, quiet && b.Open && !b.Seizing && !b.Carrier.HasValue && ResponderAt(b.At))) continue;
                b.Recovered = true;
                Note("body.recovered", $"The response removes {Who(b.P)} from the scene.");
            }
            foreach (var m in Crew)
            {
                if (!m.Gun.Down || m.Mode == CrewMode.Gone || BodyOf(m.P) != null) continue;
                // A downed fighter may still be alive. They leave only when responders can actually reach them.
                if (!ReadyToCollect("crew/" + m.Id, quiet && ResponderAt(MemberPos(m, Tick)))) continue;
                m.Mode = CrewMode.Gone; m.ModeTick = Tick;
                if (m.H != null) m.H.Present = false;
                Note("rivals.evacuated", $"Responders evacuate {m.Id} from the street.");
            }
        }

        internal void HashRecovery(Hash.H h)
        {
            foreach (var e in recoverySince) h.S("recovery").S(e.Key).I(e.Value);
            foreach (var b in Bodies) if (b.Recovered) h.S("recovered").S(b.Id);
        }
    }
}
