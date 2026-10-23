using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using Op = Suborned.Sim.Op;

namespace Suborned.Play
{
    /// <summary>Brief 049 round 2, Glyph's 0.2.79 review #2: the plain target rings in the Handover, where no hostile aims at the cell before a
    /// fight (the range's hostiles always do, so its rings are red): amber on the guard and the patrol with the cell drawn (`ring-plain-amber`),
    /// dashed for one agent whose weapon cannot reach them (`ring-plain-out`), violet on the rival crew for an agent the crew cannot see
    /// (`ring-plain-rival`). Each frame logs every ring's state (id:side:reach:flags). The cream rings (a passer-by under the cursor, the cell's
    /// ticks on a passer-by) are the range's (ProofHold.Range, section 2). Gated: the amber frame (deterministic); the dashed and violet frames
    /// say what they saw.</summary>
    public static class ProofRings
    {
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Shoot(string dir, string name, List<string> log)
        {
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null;
        }

        /// <summary>This frame's rings, one token each: id:side:reach and :aim :fire :ours :hover when set.</summary>
        public static string RingsNow(HandoverHost h) => string.Join(",", h.TargetRings.Select(r =>
            $"{r.Id}:{r.Side}:{r.Reach}{(r.Aim ? ":aim" : "")}{(r.Fire ? ":fire" : "")}{(r.Ours ? ":ours" : "")}{(r.Hover ? ":hover" : "")}"));

        static Vector3? World(HandoverHost h, string kind, string key) => h.TargetWorld(new HandoverHost.Target { Kind = kind, Key = key });

        /// <summary>Draws every armed agent in `who` (a draw order each, the engine stepped until drawn, 3 s at most).</summary>
        static void DrawAll(HandoverHost h, IEnumerable<int> who)
        {
            var g = h.Game; var list = who.Where(k => g.Ops[k].Gun.W != null && !g.Ops[k].Down).ToList();
            foreach (int k in list) if (!g.Ops[k].Gun.Drawn) h.Issue(new Order { Op = Op.Name((OpId)k), Do = "draw" });
            int lim = g.Tick + Suborned.Sim.Game.T(3); while (g.Tick < lim && list.Any(k => !g.Ops[k].Gun.Drawn)) h.RunTo(g.Tick + 1);
        }

        public static IEnumerator Handover(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            HandoverHost.PointerWorld = null; h.ClearHover();
            // A · amber: the whole cell drawn, the guard and the patrol in frame
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; var g = h.Game;
            if (h.View) h.View.orthographicSize = 14f;
            h.SelectAll(); DrawAll(h, Enumerable.Range(0, 4));
            var guardAt = World(h, "person", "guard"); if (guardAt.HasValue) h.LookAt(guardAt.Value);
            yield return Frames(4); string ringsA = RingsNow(h);
            bool aOk = h.TargetRings.Any(r => r.Side == HandoverHost.RingSide.Guard && !r.Aim && !r.Fire);
            yield return Shoot(dir, "ring-plain-amber", log);
            parts.Add($"\"amber\": {{\"ok\": {B(aOk)}, \"drawn\": \"{string.Join(",", Enumerable.Range(0, 4).Where(k => g.Ops[k].Gun.Drawn))}\", \"rings\": \"{J(ringsA)}\"}}"); ok &= aOk;
            log.Add($"rings amber: {(aOk ? "ok" : "FAILED")}: {ringsA}");
            // B · out of reach: only the armed agent farthest past its own max ring from the guard (or the patrol) selected
            int outK = -1; string outOf = ""; float worst = 0f;
            foreach (var (id, kind) in new[] { ("guard", "person"), ("patrol", "patrol") })
            {
                var at = g.PosOf(id); if (!at.HasValue) continue;
                for (int k = 0; k < 4; k++)
                {
                    var op = g.Ops[k]; if (op.Gun.W == null || op.Down || op.Inside) continue;
                    float d = Mathf.Sqrt((float)((long)(op.Pos.X - at.Value.X) * (op.Pos.X - at.Value.X) + (long)(op.Pos.Y - at.Value.Y) * (op.Pos.Y - at.Value.Y))), over = d - op.Gun.W.MaxCm;
                    if (over > worst) { worst = over; outK = k; outOf = id; }
                }
            }
            bool bSeen = false; string ringsB = "";
            if (outK >= 0)
            {
                h.Select((OpId)outK, false); var ot = outOf == "guard" ? World(h, "person", "guard") : World(h, "patrol", "patrol"); if (ot.HasValue) h.LookAt(ot.Value);
                yield return Frames(4); ringsB = RingsNow(h);
                bSeen = h.TargetRings.Any(r => r.Id == outOf && r.Reach == HandoverHost.RingReach.Out && !r.Aim && !r.Fire);
                yield return Shoot(dir, "ring-plain-out", log);
            }
            parts.Add($"\"out\": {{\"seen\": {B(bSeen)}, \"agent\": \"{(outK >= 0 ? Op.Name((OpId)outK) : "")}\", \"of\": \"{outOf}\", \"pastMaxM\": {(worst / 100f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}, \"rings\": \"{J(ringsB)}\"}}");
            log.Add($"rings out of reach: {(bSeen ? "seen" : "not seen")}: agent {outK} of {outOf}: {ringsB}");
            // C · violet: a fresh run to the rival crew's arrival, then one armed agent the crew cannot see, drawn
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; g = h.Game;
            if (h.View) h.View.orthographicSize = 14f;
            { int lim = g.RivalAt + Suborned.Sim.Game.T(10); while (g.Tick < lim && !g.Over) h.RunTo(System.Math.Min(lim, g.Tick + 50)); }
            bool cSeen = false; string ringsC = ""; int vk = -1;
            var rv = g.PosOf("rivals");
            if (rv.HasValue && !g.Over)
            {
                for (int k = 0; k < 4 && vk < 0; k++)
                {
                    var op = g.Ops[k]; if (op.Gun.W == null || op.Down || op.Inside || op.InCar) continue;
                    bool sees = false; try { sees = g.S.Sight(rv.Value, op.Pos, g.R.Eye); } catch (System.Exception) { sees = true; }
                    if (!sees) vk = k;
                }
                if (vk >= 0)
                {
                    h.Select((OpId)vk, false); DrawAll(h, new[] { vk });
                    var rw = World(h, "rivals", "rivals"); if (rw.HasValue) h.LookAt(rw.Value);
                    yield return Frames(4); ringsC = RingsNow(h);
                    cSeen = h.TargetRings.Any(r => r.Side == HandoverHost.RingSide.Rival && !r.Aim && !r.Fire);
                    yield return Shoot(dir, "ring-plain-rival", log);
                }
            }
            parts.Add($"\"rival\": {{\"seen\": {B(cSeen)}, \"agent\": \"{(vk >= 0 ? Op.Name((OpId)vk) : "")}\", \"rivalsOn\": {B(rv.HasValue)}, \"over\": {B(g.Over)}, \"rings\": \"{J(ringsC)}\"}}");
            log.Add($"rings violet: {(cSeen ? "seen" : "not seen")}: agent {vk}, rivals {(rv.HasValue ? "on" : "off")}: {ringsC}");
            h.SetPaused(false);
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }
}
