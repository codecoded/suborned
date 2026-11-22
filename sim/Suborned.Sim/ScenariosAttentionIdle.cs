using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static void PublicIdle(Run r, string watcher)
        {
            var spot = watcher == GuardR ? BesideGuard : CameraSpot(r, Enf, true);
            if (!r.Is(spot.HasValue, "reachable public ground in the watcher's view")) return;
            r.To(r.SendTo(Enf, spot!.Value));
            int alert = r.G.Alert, trail = r.G.Trail;
            r.Wait(T(65));
            r.Eq(r.G.AttentionTicks(watcher, Enf), 0, "ordinary public presence keeps no attention clock");
            r.Is(!r.G.AttentionWary(watcher, Enf) && !r.G.AttentionReported(watcher, Enf), "public waiting makes no wary mark or report");
            r.Is(!r.G.Ledger.Any(l => l.cause.StartsWith("loiter:")), "public waiting creates no loiter incident");
            r.Eq((r.G.Alert, r.G.Trail), (alert, trail), "public waiting leaves Alert and Trail unchanged");
        }
    }
}
