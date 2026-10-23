// Sky 2026-10-22 (build 0.2.48, engine 0.20.0, third person): "I couldn't fire on the robot". The Scout stood on the road at the kerb in front
// of the Records Office's public door, the patrol on its sweep about 10 m ahead, nothing between them. The engine's side of it: the line is
// clear (no kerb, bin, lamp or cabinet is a sight occluder: the site's occluders are buildings and the cover blockers at eye height), the
// going-loud card has nothing to refuse, and the fire order the host sends (TpFireLock: the driven agent, Force false) is taken and fires.
// The fault was the host's (briefs/041/next-batch.md, Keel 2026-10-22): its third-person pill offered a left-hold Fire the left hold never gives.
using System;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static void FireLineCases()
        {
            Add("FL-KERB", "—", "Sky 2026-10-22 (build 0.2.48): the Scout, driven, on the road at the kerb before the Records Office's public door, fires on the patrol sweeping the forecourt 10 m ahead: the line is clear (a kerb, a bin, a lamp or a service cabinet blocks no line between two standing people), the going-loud card refuses nothing, and the host's third-person fire order (driven, not forced) is taken, needs no walk-in and fires within the aim time", r =>
            {
                var g = r.G;
                var kerb = new Pt(6800, -5075); // on the carriageway 75 cm out from the forecourt's kerb (the drive grid's road edge, y -50), 4 m south of the door axis (x 64)
                r.To(Math.Max(r.SendTo(Scout, kerb), T(75)));
                var sc = r.Op(Scout); var pat = g.PosOf("patrol");
                r.Is(pat.HasValue && g.PatrolPose()!.Value.pos == pat.Value && g.PatrolAwayUntil <= g.Tick && !g.PatrolChasing, $"the patrol is on its sweep ({pat})");
                r.Eq(sc.Pos, kerb, "the Scout stands on the road at the kerb");
                long d = G.Isqrt(G.D2(sc.Pos, pat!.Value));
                r.Is(d >= 900 && d <= 1200 && d <= sc.Gun.W!.MaxCm, $"the patrol is about 10 m ahead, inside her pistol's reach ({d} cm, max {sc.Gun.W!.MaxCm})");
                r.Is(g.S.Sight(sc.Pos, pat.Value, g.R.Eye), "the engine's line is clear at eye height");
                r.Is(!g.CrowdBlocks(sc.Pos, pat.Value), "and nobody on the street cuts it");
                var dv = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "drive", Point = sc.Pos });
                r.Is(dv.ok && sc.Driven, $"third person: the Scout is driven ({dv.code})");
                var card = g.GoingLoud(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = "patrol" });
                r.Is(card.Loud && card.Refused.Length == 0 && card.Fails.Length == 0, $"the going-loud card: loud, nothing refused, nothing failed ({card.Why})");
                var pv = g.PreviewFire(OpId.Scout, "patrol");
                r.Is(pv.Refused.Length == 0 && !pv.Walk, $"the preview: no refusal, no walk-in ('{pv.Refused}')");
                int t0 = g.Tick;
                r.Fire(Scout, "patrol"); // as TpFireLock sends it: the driven agent, the patrol, not forced
                r.Is(r.Until(() => r.Shots().Exists(s => s.by == "scout" && s.target == "patrol"), T(3)), $"she fires on the patrol within 3 s ({g.Tick - t0} ticks)");
                r.Is(g.Gunfire > 0 && g.Alert >= g.R.GunfireAlert, $"gunfire, Alert {g.Alert}");
                r.Is(!g.Log.Exists(l => l.Tick >= t0 && (l.Code == "fire.lapses" || l.Code == "fire.out-of-range")), "the order never lapses");
            });
        }
    }
}
