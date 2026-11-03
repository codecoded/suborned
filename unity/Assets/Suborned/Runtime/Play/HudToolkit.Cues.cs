using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 084, the cue switches on the HUD (FightCues; numbers from briefs/084/src/cues.js): the hot flash, the white spark and the dust
    /// puff; the corners on every target with the shooters' faces; the red dart on an agent's ring pointing at each shooter. Off, nothing here runs
    /// and nothing is built. All read from the host; view only. Glyph's orange is a lab value until Sky keeps it.</summary>
    public sealed partial class HudToolkit
    {
        static readonly Color CueHot = Hex(0xff7a1a), CueHotCore = Hex(0xfff3c4), CueWhite = Color.white;

        /// <summary>Body scale: 1 at play zoom, so a mark keeps to the figure as the wheel moves.</summary>
        float CueZoom => 1f / (Mathf.Max(1e-4f, GroundPxM) * 54f);

        /// <summary>Called from FireHud each frame.</summary>
        void CuesFrame(bool on, float now, float W, float H)
        {
            FightCues.Sync(host.rangeMode); if (!FightCues.Any && !cueBuilt) return;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp(); cueBuilt = true;
            cueOff.Clear();
            CueTargets(on && FightCues.Target, now);
            CueEdges(on && FightCues.Edge, now, W, H);
            CueBlindTips(on && FightCues.Blind);
            if (FightCues.Any) { FightCues.CostTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0; FightCues.CostFrames++; }
        }
        bool cueBuilt;

        // ---------------------------------------------------------------- shots

        /// <summary>In place of ShotFx: orange means fired, white means hit. The flash lies along the shot, a dark edge under it; a hit's six white rays; a
        /// miss's dust puff, shorter than a hit's spark and no line; cover's chips as built.</summary>
        void CueShotFx(float now)
        {
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var v = FightCues.V; float flash = v.flashTime, spark = v.sparkTime, zk = CueZoom, longest = Mathf.Max(flash, Mathf.Max(spark, Mathf.Max(CueMissS, 0.2f)));
            foreach (var s in host.ShotFx)
            {
                float age = now - s.born; if (age > longest) continue;
                bool cell = s.side == "cell"; if (!Proj(s.from, out var a)) continue; bool toOn = Proj(s.to, out var b);
                var wd = s.to - s.from; Vector2 d = toOn ? b - a : wd.sqrMagnitude > 1e-4f && Proj(s.from + wd.normalized * 0.5f, out var ah) ? ah - a : Vector2.right;
                var u = d.sqrMagnitude > 1f ? d.normalized : Vector2.right; var n = new Vector2(-u.y, u.x);
                if (age < flash)
                {
                    ShotFlashes++; if (!cell) ShotFlashesHostile++;
                    float k = 1f - age / flash, r = (cell ? 24f : 21f) * (0.65f + 0.35f * k) * v.flashSize * zk; var m = a + u * (r * 0.35f + 1f); /* its short end at the muzzle, so it reads as the gun's and not as a dart flying off */
                    firePaints.Add(pp => { CueFlame(pp, m, u, n, r + 3f, new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.7f)); CueFlame(pp, m, u, n, r, CueHot); CueFlame(pp, m, u, n, r * 0.55f, CueHotCore); });
                }
                if (!toOn) continue;
                if (s.hit && age < spark)
                {
                    float t = age / spark, r0 = (6f + 12f * t) * zk, r1 = (18f + 18f * t) * zk, wI = 6f * zk, wW = 3.6f * zk;
                    firePaints.Add(pp =>
                    {
                        pp.lineCap = LineCap.Round;
                        for (int i = 0; i < 6; i++)
                        {
                            float ang = (i * 60f + 15f) * Mathf.Deg2Rad; var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                            pp.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.5f * (1f - t)); pp.lineWidth = wI; pp.BeginPath(); pp.MoveTo(b + dir * r0); pp.LineTo(b + dir * r1); pp.Stroke();
                            pp.strokeColor = new Color(1f, 1f, 1f, 1f - t); pp.lineWidth = wW; pp.BeginPath(); pp.MoveTo(b + dir * r0); pp.LineTo(b + dir * r1); pp.Stroke();
                        }
                    });
                }
                else if (s.covered && age < 0.2f)
                {
                    float t = age / 0.2f, seed = s.born * 997f % 30f;
                    firePaints.Add(pp =>
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            float ang = (i * 72f + 40f + seed) * Mathf.Deg2Rad; var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) - 0.6f); var q = b + dir * (4f + 14f * t) + new Vector2(0, 10f * t * t);
                            pp.fillColor = new Color(Ink.r, Ink.g, Ink.b, 0.55f * (1f - t)); pp.BeginPath(); pp.Arc(q, 2.4f, Deg(0), Deg(360)); pp.Fill();
                            pp.fillColor = new Color(Cream.r, Cream.g, Cream.b, 1f - t); pp.BeginPath(); pp.Arc(q, 1.5f, Deg(0), Deg(360)); pp.Fill();
                        }
                    });
                }
                else if (!s.hit && !s.covered && age < CueMissS)
                {
                    float t = age / CueMissS; var q0 = b + u * (18f * zk);
                    firePaints.Add(pp => { for (int i = 0; i < 3; i++) { var q = q0 + new Vector2((i - 1) * (3f + 6f * t) * zk, (-2f * t - (i == 1 ? 2f : 0f)) * zk); pp.fillColor = new Color(Smoke.r, Smoke.g, Smoke.b, 0.6f * (1f - t)); pp.BeginPath(); pp.Arc(q, (3.5f + 8f * t) * zk, Deg(0), Deg(360)); pp.Fill(); } });
                }
            }
            FightCues.CostTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        }
        const float CueMissS = 0.22f;

        /// <summary>One layer of the flash: long along the shot, waisted, short behind the gun.</summary>
        static void CueFlame(Painter2D p, Vector2 m, Vector2 u, Vector2 n, float r, Color c)
        {
            p.fillColor = c; p.BeginPath(); p.MoveTo(m + u * (r * 1.9f)); p.LineTo(m + n * (r * 0.35f)); p.LineTo(m + n * (r * 0.85f)); p.LineTo(m - u * (r * 0.35f) + n * (r * 0.2f));
            p.LineTo(m - u * (r * 0.35f) - n * (r * 0.2f)); p.LineTo(m - n * (r * 0.85f)); p.LineTo(m - n * (r * 0.35f)); p.ClosePath(); p.Fill();
        }

        // ---------------------------------------------------------------- the target corners

        readonly Dictionary<string, List<int>> cueBy = new(); readonly List<VisualElement> cueFaces = new();
        /// <summary>This frame's targets off screen, for the edge list.</summary>
        readonly List<(string id, Vector3 world, List<int> shooters)> cueOff = new();

        /// <summary>A place on the street the player can see: inside the frame and clear of the roster's column (the page's rule).</summary>
        bool CueOnScreen(Vector2 s, float W, float H)
        {
            if (s.x <= 20f || s.x >= W - 30f || s.y <= 60f || s.y >= H - 10f) return false;
            if (column != null && column.resolvedStyle.display != DisplayStyle.None) { var cb = column.worldBound; if (s.x < cb.xMax && s.y < cb.yMax) return false; }
            return true;
        }

        void CueTargets(bool on, float now)
        {
            int used = 0;
            foreach (var l in cueBy.Values) l.Clear();
            if (on)
            {
                var g = host.Game;
                for (int k = 0; k < 4; k++)
                {
                    var fs = host.FireNow[k]; if (g.Ops[k].Down || string.IsNullOrEmpty(fs.Target) || string.IsNullOrEmpty(fs.Mode)) continue; // FireStateOf names a self-picked target too (weapons-free): no new read
                    if (!cueBy.TryGetValue(fs.Target, out var list)) cueBy[fs.Target] = list = new List<int>();
                    list.Add(k);
                }
                var size = root.panel.visualTree.layout.size; float zk0 = CueZoom;
                foreach (var kv in cueBy)
                {
                    if (kv.Value.Count == 0 || host.DrawnAt(kv.Key) is not Vector3 w) continue;
                    if (!Proj(w, out var s) || !CueOnScreen(s, size.x, size.y)) { cueOff.Add((kv.Key, w, kv.Value)); continue; }
                    bool sel = false; foreach (var k in kv.Value) sel |= host.Selected[k];
                    var ls = host.CueShotOn.TryGetValue(kv.Key, out var lv) ? lv : (at: -9f, hit: false); float age = now - ls.at;
                    float bw = 25f, bh = 54f, alpha = 1f; var col = Cream;
                    if (age < 0.35f) { float q = 1f - age / 0.35f; if (ls.hit) { bw -= 6f * q; bh -= 8f * q; col = CueWhite; } else { bw += 7f * q * (1f + Mathf.Sin(age * 90f) * 0.4f); bh += 6f * q; alpha = 0.75f; } }
                    var c = new Vector2(s.x, s.y - 42f * zk0); float W2 = bw * zk0, H2 = bh * zk0, L = 11f * zk0, thick = (sel ? 2.8f : 2f) * Mathf.Sqrt(zk0);
                    firePaints.Add(pp =>
                    {
                        pp.lineCap = LineCap.Butt;
                        for (int pass = 0; pass < 2; pass++)
                        {
                            pp.strokeColor = pass == 0 ? new Color(InkDeep.r, InkDeep.g, InkDeep.b, alpha) : new Color(col.r, col.g, col.b, alpha); pp.lineWidth = pass == 0 ? thick + 2.2f : thick;
                            for (int sx = -1; sx <= 1; sx += 2) for (int sy = -1; sy <= 1; sy += 2)
                            { var e = c + new Vector2(sx * W2, sy * H2); pp.BeginPath(); pp.MoveTo(e - new Vector2(sx * L, 0)); pp.LineTo(e); pp.LineTo(e - new Vector2(0, sy * L)); pp.Stroke(); }
                        }
                    });
                    // the shooters' faces above: above the lock disc when he is the focus
                    float top = s.y - (42f + 54f + 16f) * zk0;
                    if (reticles.TryGetValue(kv.Key, out var rt) && rt.OnScreen && rt.Gone < 0f) top = Mathf.Min(top, rt.At.y - ReticleD - 6f - 16f);
                    int n = Mathf.Min(4, kv.Value.Count);
                    for (int i = 0; i < n; i++)
                    {
                        while (cueFaces.Count <= used) { var f = Cameo(marks, 18f, 2f); f.style.position = Position.Absolute; f.pickingMode = PickingMode.Ignore; cueFaces.Add(f); }
                        var cf = cueFaces[used++]; Show(cf, true); CameoOf(cf, kv.Value[i]); cf.style.backgroundColor = host.Selected[kv.Value[i]] ? TurqLight : Chrome;
                        cf.style.left = s.x + (i - (n - 1) / 2f) * 28f - 12f; cf.style.top = top - 12f; cf.BringToFront();
                    }
                }
            }
            for (int i = used; i < cueFaces.Count; i++) Show(cueFaces[i], false);
        }

        // ---------------------------------------------------------------- on the ground

        /// <summary>The ground batch's part: the darts and the hollow cones, before GroundFlush.</summary>
        void CueGround()
        {
            if (!FightCues.Any || host.Game == null || m.Ended || m.BriefingOpen) return;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            if (FightCues.Incoming) CueDarts();
            if (FightCues.Blind) CueHollow();
            FightCues.CostTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        }

        /// <summary>Incoming: the agent keeps its turquoise ring; a red dart on it points at each shooter (never a red ring: in the build that is your
        /// named target). It lengthens for 0.45 s on a shot.</summary>
        void CueDarts()
        {
            var g = host.Game;
            for (int k = 0; k < 4; k++)
            {
                var op = g.Ops[k]; if (op.Down || op.Inside || op.InCar) continue;
                var t = host.Views?.Find("op/" + Op.Name((OpId)k)); if (!t || !t.gameObject.activeInHierarchy) continue;
                int shown = 0;
                foreach (var u in host.UnderFireNow[k])
                {
                    if (shown++ >= 4 || host.DrawnAt(u.By) is not Vector3 by) continue;
                    var d = by - t.position; d.y = 0f; if (d.sqrMagnitude < 0.01f) continue; d.Normalize();
                    bool hot = (g.Tick - u.Last) / (float)Game.Hz < 0.45f; const float R = 0.56f, Wing = 0.34f; float len = hot ? 0.63f : 0.52f, lift = t.position.y + GroundLift + 0.004f;
                    Vector3 Rot(Vector3 v, float a) => new(v.x * Mathf.Cos(a) - v.z * Mathf.Sin(a), 0f, v.x * Mathf.Sin(a) + v.z * Mathf.Cos(a));
                    var c = new Vector3(t.position.x, lift, t.position.z); Vector3 tip = c + d * (R + len), b0 = c + Rot(d, -Wing) * R, b1 = c + Rot(d, Wing) * R;
                    Vector3 grow = d * 0.05f; float e = hot ? 0.05f : 0.035f;
                    Quad(GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.9f), true), tip + grow, tip + grow, b1 + Rot(d, Mathf.PI / 2f) * e, b0 - Rot(d, Mathf.PI / 2f) * e);
                    Quad(GroundOf(RingThreat, false), tip, tip, b1, b0);
                }
            }
        }
    }
}
