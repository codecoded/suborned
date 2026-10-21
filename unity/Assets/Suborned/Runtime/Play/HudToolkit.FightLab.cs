using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>Brief 027, Glyph's finished fight HUD (briefs/027/glyph/hud.html §2; EXPERIMENTAL, with the lab preset): health bars over heads
    /// (8 × 5 px segments of 10 health, a side-coloured frame, cream fill), the hovered target's next hit and target mode's volley hatched on the
    /// bar, forecast pips above it ("🚶 4 s · ●○●" when Keel's walk-in walks first), a mark on each hostile that can fire on the walk, the freeze
    /// rim and the one-glyph mode pill, the queue markers by each portrait, the portraits' segment bars and turquoise charge, and the F-number
    /// badges at our lines' ends (the lines themselves are on the signal layer, HostFightLab). Each §3 choice is a P panel switch
    /// (FightHudLook). Read only: it never changes the run.</summary>
    public sealed partial class HudToolkit
    {
        Paint flOver; // above the panels: the rim and the portraits' bars
        readonly List<Action<Painter2D>> flPaints = new();
        VisualElement flPill, flPillGlyph, flPillTrig; Paint flPillCross; readonly List<(VisualElement cap, Label key, Label word)> flKeys = new();
        readonly List<(VisualElement root, VisualElement act, VisualElement coin, Label text, Paint dash, VisualElement walk, Label walkS, Paint exp, Label expN)> flQueue = new();
        VisualElement flGo; Label flGoKey; readonly List<bool>[] flQPips = { new(), new(), new(), new() }; readonly Paint[] flQPipEl = new Paint[4];
        readonly List<(VisualElement root, Label text)> flBadges = new();
        readonly List<(VisualElement root, Label text)> flWalks = new();
        readonly List<(VisualElement root, Label text)> flKites = new();
        readonly Dictionary<string, (int hp, float at, int from)> flHp = new();
        float flFrozenAt = -1f; bool flWasFrozen; Paint flCoinRing; string flCoinBand = "";

        /// <summary>Bars drawn this frame over the range's cars and bystanders (the range proof reads it).</summary>
        public static int RangeBarsDrawn { get; private set; }
        /// <summary>The goal line's words now ("" hidden; the range proof reads it).</summary>
        public static string GoalNow => live != null ? live.goalText : "";
        static readonly Color FlGround = new(0.035f, 0.09f, 0.09f, 0.88f);
        /// <summary>The frame of a range car's or bystander's bar: nobody's side (the range panel's pale line onto a bystander).</summary>
        static readonly Color RangeNeutral = new(0.92f, 0.92f, 0.88f);

        bool FlOn => host.Game != null && host.Game.Lab;

        void FlBuild()
        {
            if (flOver != null) return;
            flOver = P(root, 0, 0, 0, 0, (p, r) => { foreach (var f in flPaints) f(p); });
            flOver.style.left = 0; flOver.style.top = 0; flOver.style.right = 0; flOver.style.bottom = 0; flOver.style.width = StyleKeyword.Auto; flOver.style.height = StyleKeyword.Auto;
            // the mode pill: top centre, 28 px, a glyph and keycaps (Glyph frame A)
            flPill = E(root, "s-plate_dark"); flPill.style.position = Position.Absolute; flPill.style.top = 14; flPill.style.height = ChipH; flPill.style.minWidth = 96;
            flPill.style.flexDirection = FlexDirection.Row; flPill.style.alignItems = Align.Center; flPill.style.paddingLeft = ChipPad; flPill.style.paddingRight = ChipPad;
            flPillGlyph = E(flPill, null, w: Glyph, h: Glyph); flPillGlyph.style.flexShrink = 0;
            flPillCross = P(flPill, 0, 0, Glyph, Glyph, (p, r) => Crosshair(p, new Vector2(Glyph / 2, Glyph / 2), 5.6f, Cream)); flPillCross.style.position = Position.Relative; flPillCross.style.left = StyleKeyword.Auto; flPillCross.style.top = StyleKeyword.Auto;
            flPillTrig = E(flPill, null, w: Glyph, h: Glyph); flPillTrig.style.marginLeft = 6; flPillTrig.style.flexShrink = 0;
            for (int i = 0; i < 3; i++)
            {
                var cap = E(flPill, "s-plate_light"); cap.style.height = 20; cap.style.minWidth = 22; cap.style.marginLeft = 8; cap.style.paddingLeft = 5; cap.style.paddingRight = 5; cap.style.justifyContent = Justify.Center; cap.style.flexDirection = FlexDirection.Row; cap.style.alignItems = Align.Center;
                var k = L(cap, "keycap"); k.style.fontSize = 14; k.style.unityTextAlign = TextAnchor.MiddleCenter;
                var w = L(flPill, "verb-name"); w.style.fontSize = 14; w.style.marginLeft = 4;
                flKeys.Add((cap, k, w));
            }
            // the queue markers: one per portrait, 150 wide, the HUD's 36 px chip (Glyph's 28 leaves no room for a 14 px glyph 4 px clear of the
            // plate's rim: Sky's layout audit), 14 px type: action glyph, target coin, [footsteps + walk seconds], pips and damage, [exposure mark + count]
            for (int i = 0; i < 4; i++)
            {
                var r = E(root, null, 0, 0, float.NaN, ChipH); r.style.minWidth = 150; r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.paddingLeft = ChipPad; r.style.paddingRight = ChipPad;
                var dash = P(r, 0, 0, 150, ChipH, (p, rc) => DashedPill(p, new Rect(4, 5, rc.width - 8, rc.height - 10), new Color(Cream.r, Cream.g, Cream.b, 0.7f))); dash.style.right = 0; dash.style.bottom = 0; dash.style.width = StyleKeyword.Auto; dash.style.height = StyleKeyword.Auto;
                var act = E(r, null, w: Glyph, h: Glyph); act.style.flexShrink = 0;
                var coin = E(r, null, w: Glyph, h: Glyph); coin.style.marginLeft = 6; coin.style.flexShrink = 0;
                var walk = E(r, null, w: Glyph, h: Glyph); walk.style.marginLeft = 6; walk.style.flexShrink = 0; Icon(walk, "s_moving", Cream);
                var walkS = L(r, "chip-value"); walkS.style.fontSize = 14; walkS.style.marginLeft = 3; walkS.style.marginRight = 8;
                // the pips are dots, not type (Glyph frame A; the budget reads a row of type glyphs as running text): filled cream, outlined
                int qi = i; var pipsEl = P(r, 0, 0, 40, Glyph, (p, rc) => { var l = flQPips[qi]; for (int j = 0; j < l.Count; j++) { var c = new Vector2(3.6f + j * 9.5f, rc.height / 2); p.BeginPath(); p.Arc(c, 3.5f, Deg(0), Deg(360)); if (l[j]) { p.fillColor = Cream; p.Fill(); } else { p.strokeColor = Cream; p.lineWidth = 1.1f; p.Stroke(); } } });
                pipsEl.style.position = Position.Relative; pipsEl.style.left = StyleKeyword.Auto; pipsEl.style.top = StyleKeyword.Auto; pipsEl.style.marginLeft = 6; pipsEl.style.flexShrink = 0; flQPipEl[i] = pipsEl;
                var t = L(r, "chip-value"); t.style.fontSize = 14; t.style.marginLeft = 14; // groups (walk · pips · damage · exposure) 14 px apart, as the chip's dividers: they read as marks, not a sentence
                var exp = P(r, 0, 0, Glyph, Glyph, (p, rc) => Exposed(p, new Vector2(rc.width / 2, rc.height / 2 + 0.5f), Cream)); exp.style.position = Position.Relative; exp.style.left = StyleKeyword.Auto; exp.style.top = StyleKeyword.Auto; exp.style.marginLeft = 14; exp.style.flexShrink = 0;
                var expN = L(r, "chip-value"); expN.style.fontSize = 14; expN.style.marginLeft = 3;
                flQueue.Add((r, act, coin, t, dash, walk, walkS, exp, expN));
            }
            flGo = E(root, "s-plate_dark", 0, 0, float.NaN, ChipH); flGo.style.flexDirection = FlexDirection.Row; flGo.style.alignItems = Align.Center; flGo.style.paddingLeft = ChipPad; flGo.style.paddingRight = ChipPad;
            var gl = L(flGo, "chip-value"); gl.text = "Go"; gl.style.fontSize = 14;
            var gc = E(flGo, "s-plate_light"); gc.style.height = 20; gc.style.minWidth = 22; gc.style.marginLeft = 6; gc.style.paddingLeft = 5; gc.style.paddingRight = 5; gc.style.justifyContent = Justify.Center;
            flGoKey = L(gc, "keycap"); flGoKey.style.fontSize = 14; flGoKey.text = "⏎"; flGoKey.style.unityTextAlign = TextAnchor.MiddleCenter;
            // the range state on the chip's side coin (filled ring effective, half long, hollow out)
            flCoinRing = P(chipLead, -3, -3, 0, 0, (p, r) =>
            {
                var c = new Vector2(r.width / 2, r.height / 2); float rad = Mathf.Min(r.width, r.height) / 2 - 1;
                if (flCoinBand is "eff" or "point") RingArc(p, c, rad, 1f, Cream, 1.6f);
                else if (flCoinBand == "long") { RingArc(p, c, rad, 1f, new Color(Cream.r, Cream.g, Cream.b, 0.3f), 1.6f); p.strokeColor = Cream; p.lineWidth = 1.6f; p.BeginPath(); p.Arc(c, rad, Deg(90), Deg(270)); p.Stroke(); }
                else if (flCoinBand == "out") Dashed(p, c, rad, new Color(Cream.r, Cream.g, Cream.b, 0.8f), 1.2f, 8);
            });
            flCoinRing.style.right = -3; flCoinRing.style.bottom = -3; flCoinRing.style.width = StyleKeyword.Auto; flCoinRing.style.height = StyleKeyword.Auto;
        }

        /// <summary>Each frame after the street marks: the whole fight layer, or hide it.</summary>
        void FightLabLayer(bool alt, Camera cam)
        {
            if (root == null || root.panel == null) return;
            FlBuild(); flPaints.Clear();
            var g = host.Game; var look = host.Fights.Hud;
            bool live = FlOn && !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None && cam;
            bool frozen = live && host.Frozen;
            if (frozen && !flWasFrozen) flFrozenAt = Time.unscaledTime; flWasFrozen = frozen;
            // the portraits keep today's pips without the lab; with it, segment bars (and the charge) replace them
            for (int i = 0; i < 4; i++) foreach (var w in new[] { colWins[i], rowWins[i] }) if (w?.Pips != null) w.Pips.style.visibility = FlOn ? Visibility.Hidden : Visibility.Visible;
            Show(flPill, frozen); Show(flGo, frozen && look.ladder); foreach (var q in flQueue) Show(q.root, frozen && look.ladder);
            int badges = 0, walks = 0, kites = 0;
            Show(flCoinRing, live && host.AimNow is { Lab: true }); flCoinBand = host.AimNow?.Band ?? ""; flCoinRing.MarkDirtyRepaint();
            if (live)
            {
                float now = Time.unscaledTime;
                // ---- what each target will take: the hover's next hit (real time) or the queued volleys / fire (a freeze)
                var after = new Dictionary<string, int>(); var forecasts = new Dictionary<string, List<(int k, VolleyPreview v)>>(); var exposed = new Dictionary<string, int>();
                if (frozen)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        string tgt = host.VolleyShots[k] > 0 ? host.VolleyTarget[k] : host.Queued[k] is { Do: "fire" } q ? q.Target : null; if (string.IsNullOrEmpty(tgt)) continue;
                        if (host.VolleyShots[k] > 0)
                        {
                            VolleyPreview v = null; try { v = g.PreviewVolley((OpId)k, tgt, host.VolleyShots[k]); } catch { }
                            if (v == null || v.Refused.Length > 0) continue;
                            var gun = g.GunOf(tgt); int hp0 = after.TryGetValue(tgt, out var a0) ? a0 : gun?.Health ?? 0; after[tgt] = Mathf.Max(0, hp0 - ((gun?.Health ?? 0) - v.HealthAfter));
                            if (!forecasts.TryGetValue(tgt, out var lst)) forecasts[tgt] = lst = new List<(int, VolleyPreview)>(); lst.Add((k, v));
                            if (v.Walk) foreach (var x in v.WalkExposedTo) exposed[x] = exposed.TryGetValue(x, out var c) ? c + 1 : 1;
                        }
                        else
                        {
                            FirePreview p = null; try { p = g.PreviewFire((OpId)k, tgt, host.Queued[k].Force); } catch { }
                            if (p == null || p.Refused.Length > 0) continue;
                            int hp0 = after.TryGetValue(tgt, out var a0) ? a0 : p.Health; after[tgt] = Mathf.Max(0, hp0 - p.Damage);
                            if (p.Walk) foreach (var x in p.WalkExposedTo) exposed[x] = exposed.TryGetValue(x, out var c) ? c + 1 : 1;
                        }
                    }
                }
                else if (host.LabChunk is { } ch && ch.max > 0)
                {
                    after[ch.id] = Mathf.Max(0, ch.health - ch.damage);
                    if (host.LabFire is { Walk: true } lf) foreach (var x in lf.WalkExposedTo) exposed[x] = 1;
                }
                FlCoverMarks(g, now); // brief 027 B5: cover marks under the bars (HudToolkit.Cover.cs)
                // ---- health bars over every fighter (the bar is the one icon of the two-icon budget; an exposure mark the second)
                var seen = new HashSet<string>();
                var fighters = new List<(string id, Gun gun)>(host.Fighters()); var have = new HashSet<string>(); foreach (var f in fighters) have.Add(f.id);
                if (g.FightOn) foreach (var cid in g.Combatants()) if (!have.Contains(cid) && g.GunOf(cid) is Gun cg && !cg.Down) fighters.Add((cid, cg)); // Keel: Combatants() is whom the bars go over (plus the cell)
                // placement: bars first, then the forecast rows over them, then the badges; each steps clear of what is already placed (two
                // fighters a body apart would otherwise draw their bars, rows and badges on top of each other)
                // brief 029 (Sky on 0.2.34): the range's cars and bystanders wear the same bar (the engine's RangeCars / RangeBystanders health), neutral-framed
                var bars = new List<(string id, int hp, int max, bool neutral)>(); foreach (var (fid, fg) in fighters) bars.Add((fid, fg.Health, fg.Max, false));
                if (host.rangeMode)
                {
                    foreach (var c in g.RangeCars()) if (c.State != "wrecked" && c.Max > 0 && !have.Contains(c.Id)) bars.Add((c.Id, c.Health, c.Max, true));
                    foreach (var b in g.RangeBystanders()) if (!b.Down && b.State != "safe" && b.Max > 0 && !have.Contains(b.Id)) bars.Add((b.Id, b.Health, b.Max, true));
                }
                int rangeBars = 0; var placed = new List<Rect>(); var barsAt = new List<(string id, Rect bar, Vector2 q)>();
                Rect Place(Rect r, float dy) { for (int i = 0; i < 8; i++) { bool hit = false; foreach (var u in placed) if (u.Overlaps(r)) { hit = true; break; } if (!hit) break; r.y += dy; } placed.Add(r); return r; }
                bars.Sort((a2, b2) => { var wa = host.DrawnAt(a2.id); var wb = host.DrawnAt(b2.id); float ya = wa.HasValue && Proj(wa.Value, out var qa) ? qa.y : 0, yb = wb.HasValue && Proj(wb.Value, out var qb) ? qb.y : 0; return yb.CompareTo(ya); }); // nearest the camera first: it keeps its place
                foreach (var (id, ghp, gmax, neutral) in bars)
                {
                    seen.Add(id); if (gmax <= 0) continue;
                    if (!flHp.TryGetValue(id, out var st)) st = (ghp, -9f, ghp);
                    if (ghp < st.hp) st = (ghp, now, st.hp); else if (ghp > st.hp) st = (ghp, -9f, ghp);
                    flHp[id] = st;
                    var w = host.DrawnAt(id); if (!w.HasValue || !Proj(w.Value + Vector3.up * 2.1f, out var q)) continue;
                    var side = neutral ? RangeNeutral : HandoverHost.SideColour(id); int hp = ghp, max = Mathf.Max(gmax, ghp);
                    int aft = after.TryGetValue(id, out var av) ? av : hp; float flashT = now - st.at; int flashFrom = flashT < 0.25f ? st.from : hp;
                    bool hatch = look.hatched; Color fill = look.creamFill ? Cream : side;
                    var top = new Vector2(q.x, q.y - 10); var barRect = BarRect(top, max, 8f, 5f, 2f);
                    { var outer = new Rect(barRect.xMin - 3, barRect.yMin - 3, barRect.width + 6 + (exposed.ContainsKey(id) ? 18 : 0), barRect.height + 6); var cl = Place(outer, -11f); barRect.y += cl.y - outer.y; }
                    flPaints.Add(pp => SegBar(pp, barRect, hp, max, aft, flashFrom, side, fill, 8f, 2f, hatch, now));
                    if (exposed.ContainsKey(id)) { var at = new Vector2(barRect.xMax + 9, barRect.center.y); flPaints.Add(pp => Exposed(pp, at, side)); }
                    barsAt.Add((id, barRect, q)); if (neutral) rangeBars++;
                }
                foreach (var (id, barRect, q) in barsAt)
                {
                    if (forecasts.TryGetValue(id, out var fl))
                    {
                        float y = barRect.yMin - 11;
                        foreach (var (k, v) in fl)
                        {   // a row per agent: [🚶 4 s ·] ●○●, dotted-outlined (a forecast, never a result)
                            int n = v.Shots.Count; float pitch = 10f, rowW = n * pitch; float x0 = q.x - rowW / 2 + pitch / 2;
                            if (v.Walk)
                            {
                                var we = FlWalk(walks++); bool mine = host.Selected[k]; Show(we.text, mine); we.root.style.backgroundColor = mine ? FlGround : Color.clear; // the other agents' rows read secondary: the footsteps alone, no pill Text(we.text, mine ? $"{HandoverHost.WalkSeconds(v.WalkTicks)} s" : ""); // the selected agent's walk in seconds; the others the footsteps alone (their seconds are on the ladder): two stacked rows of type read as a text block
                                float ww = float.IsNaN(we.root.resolvedStyle.width) ? 52 : we.root.resolvedStyle.width; x0 += (ww + 4) / 2;
                                var row = Place(new Rect(x0 - pitch / 2 - 4 - ww, y - 10, ww + 4 + rowW + 4, 20), -20f); y = row.y + 10;
                                we.root.style.left = x0 - pitch / 2 - 4 - ww; we.root.style.top = y - 10;
                            }
                            else { var row = Place(new Rect(x0 - pitch / 2 - 4, y - 10, rowW + 8, 20), -20f); y = row.y + 10; }
                            var shots = new List<bool>(); foreach (var s in v.Shots) shots.Add(s.hit); float cx = x0, cy = y;
                            flPaints.Add(pp => ForecastPips(pp, cx, cy, pitch, shots));
                            { var from = new Vector2(cx - pitch / 2 - 2, cy + 7.5f); var to = new Vector2(barRect.center.x, barRect.yMin - 2.5f); if (to.y - from.y > 3f) flPaints.Add(pp => { pp.strokeColor = Cream; pp.lineWidth = 1f; pp.lineCap = LineCap.Butt; pp.BeginPath(); pp.MoveTo(from); pp.LineTo(new Vector2(from.x, to.y)); pp.LineTo(to); pp.Stroke(); }); } // Glyph: a 1 px cream tick from a stacked row down to its own bar
                            y -= 20;
                        }
                    }
                }
                RangeBarsDrawn = rangeBars;
                var gone = new List<string>(); foreach (var kv in flHp) if (!seen.Contains(kv.Key)) gone.Add(kv.Key); foreach (var x in gone) flHp.Remove(x);
                // ---- the F-number badges at our lines' target ends (HostFightLab's signal lines)
                foreach (var (text, at) in host.LineBadges)
                {   // beside the body under the chest, clear of the bars, the rows and the other badges (a second badge on the same target steps under it)
                    if (!Proj(at, out var q)) continue; var b = FlBadge(badges++); Text(b.text, text);
                    float bw = float.IsNaN(b.root.resolvedStyle.width) ? 54 : b.root.resolvedStyle.width;
                    var br = Place(new Rect(q.x + 12, q.y + 14, bw, 20), 23f); b.root.style.left = br.x; b.root.style.top = br.y;
                }
                // ---- the kite's answers: seconds over each hostile ghost (HostFightLab draws the ghost and its line)
                foreach (var (side, at, secs) in host.KiteBadges)
                {
                    if (!Proj(at + Vector3.up * 2.1f, out var kq)) continue; var kc = FlKite(kites++, side == "rivals" ? HandoverHost.RivalViolet : HandoverHost.PoliceAmber); Text(kc.text, $"{secs} s");
                    float kw = float.IsNaN(kc.root.resolvedStyle.width) ? 44 : kc.root.resolvedStyle.width; var kr = Place(new Rect(kq.x - kw / 2, kq.y - 24, kw, 20), -23f); kc.root.style.left = kr.x; kc.root.style.top = kr.y;
                }
                // ---- portraits: segment bars and the charge (frame B, C)
                if (m.Dock == RosterDock.Left || true)
                {
                    var wins = m.Dock == RosterDock.Left ? colWins : rowWins;
                    for (int k = 0; k < 4; k++)
                    {
                        var w = wins[k]; if (w?.Pips == null) continue; var op = g.Ops[k]; if (op.Gun.W == null) continue;
                        var wb = w.Pips.worldBound; if (wb.width <= 0 || float.IsNaN(wb.width)) continue;
                        int hp = op.Gun.Health, max = Mathf.Max(op.Gun.Max, op.Gun.Health); int segs = Mathf.Max(1, (max + 9) / 10);
                        float sw = Mathf.Min(6f, (40f - (segs - 1) * 1.2f) / segs); var br = BarRect(new Vector2(wb.center.x, wb.center.y + 2.5f), max, sw, 5f, 1.2f);
                        int flashFrom = flHp.TryGetValue(Op.Name((OpId)k), out var st) && now - st.at < 0.25f ? st.from : hp; bool hatch = look.hatched; Color fill = look.creamFill ? Cream : CellTurquoiseC;
                        flPaints.Add(pp => SegBar(pp, br, hp, max, hp, flashFrom, CellTurquoiseC, fill, sw, 1.2f, hatch, now));
                        if (look.chargeOnPortrait && g.Labr != null && g.Labr.Charge)
                        {
                            int c = g.ChargeOf((OpId)k), cmax = Mathf.Max(1, g.Labr.ChargeMax), spend = 0;
                            if (frozen && host.VolleyShots[k] > 0) { try { var v = g.PreviewVolley((OpId)k, host.VolleyTarget[k], host.VolleyShots[k]); if (v.Refused.Length == 0) spend = v.ChargeCost; } catch { } }
                            else if (!frozen && host.LabFireAgent == k && host.LabFire != null) spend = host.LabFire.ChargePerShot;
                            var cr = new Rect(br.xMin, br.yMax + 2.5f, br.width, 3f); int cs = c, csp = spend;
                            flPaints.Add(pp => ChargeBar(pp, cr, cs, cmax, csp, hatch));
                        }
                    }
                }
                // ---- the freeze: the rim, the pill, the queue markers
                if (frozen)
                {
                    float W = root.panel.visualTree.layout.width, H = root.panel.visualTree.layout.height;
                    float pulse = 0.6f * (0.55f + 0.45f * Mathf.Sin((now - flFrozenAt) * Mathf.PI * 2f / 1.2f));
                    var rimC = new Color(CellTurquoiseC.r, CellTurquoiseC.g, CellTurquoiseC.b, pulse);
                    flPaints.Insert(0, pp => { pp.strokeColor = rimC; pp.lineWidth = 3f; pp.BeginPath(); pp.MoveTo(new Vector2(1.5f, 1.5f)); pp.LineTo(new Vector2(W - 1.5f, 1.5f)); pp.LineTo(new Vector2(W - 1.5f, H - 1.5f)); pp.LineTo(new Vector2(1.5f, H - 1.5f)); pp.ClosePath(); pp.Stroke(); });
                    FlPill(W, now, look);
                    if (look.ladder) FlLadder(g, exposed);
                    if (look.ladder) for (int i = 0; i < 4; i++) Show(busy[i].root, false); // the markers stand where the busy chips stand
                }
            }
            for (int i = badges; i < flBadges.Count; i++) Show(flBadges[i].root, false);
            for (int i = walks; i < flWalks.Count; i++) Show(flWalks[i].root, false);
            for (int i = kites; i < flKites.Count; i++) Show(flKites[i].root, false);
            flOver.BringToFront(); if (frozen) { flPill.BringToFront(); foreach (var q in flQueue) q.root.BringToFront(); flGo.BringToFront(); }
            flOver.MarkDirtyRepaint();
        }

        /// <summary>Brief 027 B5 (HudToolkit.Cover.cs, with Keel's cover snap): the cover marks near the selected drawn agents. A no-op without it.</summary>
        partial void FlCoverMarks(Game g, float now);

        static readonly Color CellTurquoiseC = new(0x4f / 255f, 0xe3 / 255f, 0xe8 / 255f);

        void FlPill(float W, float now, FightHudLook look)
        {
            bool target = host.TargetFreeze; string why = host.FrozenWhy;
            Show(flPillGlyph, !target); Show(flPillCross, target); if (!target) Icon(flPillGlyph, "pause", Cream);
            string trig = why switch { "fired on" => "fire", "spotted" => "eye_open", "the fight springs" => "bang", "going loud" => "bang", "reinforcements" => "units", "one of the cell down" => "s_downed", _ => "" };
            Show(flPillTrig, trig.Length > 0); if (trig.Length > 0) { Icon(flPillTrig, trig, Cream); float t = now - flFrozenAt; float sc = t < 0.6f ? 1f + 0.35f * Mathf.Sin(t / 0.6f * Mathf.PI) : 1f; flPillTrig.style.scale = new Scale(new Vector3(sc, sc, 1)); } // an auto freeze pulses its trigger once
            string[] keys = target ? new[] { "+", "−", host.controls.Label(PlayAction.TargetMode) + " ➤" } : new[] { "⏎", "Esc", "" };
            string[] words = target ? new[] { "add", "remove", "fire" } : new[] { "go", "leave", "" };
            for (int i = 0; i < flKeys.Count; i++) { var (cap, k, w) = flKeys[i]; bool on = keys[i].Length > 0; Show(cap, on); Text(k, keys[i]); Show(w, on && !look.keysOnly); Text(w, words[i]); }
            float pw = float.IsNaN(flPill.resolvedStyle.width) ? 96 : flPill.resolvedStyle.width; flPill.style.left = W / 2 - pw / 2;
        }

        /// <summary>The queue markers beside each portrait (where the busy chips stand): action glyph, target coin, pips and damage, or the volley's
        /// forecast with Keel's walk-in ("🚶 4 s") and the exposure count ("⚠2"); empty = a dashed outline. "Go ⏎" under the last.</summary>
        void FlLadder(Game g, Dictionary<string, int> exposedAll)
        {
            int a = m.Agent; float colW = 109 * RosterScale; bool left = m.Dock == RosterDock.Left;
            float WinMid(int i) => left ? ColY + ColumnBezels[i].y * RosterScale : 14 + 92 + 14 + ChipH / 2 + i * (ChipH + 4);
            float plateX = ColX + colW + 4; bool folded = PlateFolded;
            float lastY = 0, lastX = 0;
            for (int k = 0; k < 4; k++)
            {
                var (r, act, coin, text, dash, walk, walkS, exp, expN) = flQueue[k]; Show(walk, false); Show(walkS, false); Show(exp, false); Show(expN, false); var qp = flQPips[k]; qp.Clear(); Show(flQPipEl[k], false);
                float x = !left ? 14 : k == a ? SelectedChipX(plateX, folded && PlateOn) : plateX; // brief 030 (no plate): the ladder docks beside the portraits r.style.left = x; r.style.top = WinMid(k) - ChipH / 2; lastY = WinMid(k) + ChipH / 2; lastX = plateX;
                var q = host.Queued[k]; int shots = host.VolleyShots[k]; string tgt = shots > 0 ? host.VolleyTarget[k] : q?.Target ?? "";
                bool empty = q == null && shots <= 0; Show(dash, empty); r.EnableInClassList("s-plate_dark", !empty);
                bool armed = g.Ops[k].Gun.W != null && !g.Ops[k].Down;
                r.style.opacity = empty && !armed ? 0.45f : 1f;
                if (empty) { Show(act, false); Show(coin, false); Text(text, ""); r.style.minWidth = 150; continue; }
                string glyph = shots > 0 || q.Do == "fire" ? "fire" : q.Do switch { "move" => "s_moving", "stance" => q.Stance == "hold" ? "lock" : "fire", "draw" => "fire", "holster" => "lock", "panic" => "run", _ => HudModel.AtlasIcon(q.Do, q.Approach ?? "", "") };
                Icon(act, glyph, Cream);
                bool hostile = tgt.Length > 0 && !(tgt is "scout" or "fixer" or "tech" or "enforcer");
                Show(coin, hostile); if (hostile) Bg(coin, tgt.StartsWith("rival") ? "badges/signal_rival" : "badges/signal_police", Color.white);
                string s = "";
                if (shots > 0)
                {
                    VolleyPreview v = null; try { v = g.PreviewVolley((OpId)k, tgt, shots); } catch { }
                    if (v != null && v.Refused.Length == 0)
                    {
                        foreach (var sh in v.Shots) qp.Add(sh.hit);
                        s = v.Down ? "✕" : $"−{(g.GunOf(tgt)?.Health ?? 0) - v.HealthAfter}"; WalkExp(v.Walk, v.WalkTicks, v.Walk ? v.WalkExposedTo.Count : 0);
                    }
                    else s = $"×{shots}";
                }
                else if (q.Do == "fire" && g.Lab)
                {
                    FirePreview p = null; try { p = g.PreviewFire((OpId)k, tgt, q.Force); } catch { }
                    if (p != null && p.Refused.Length == 0) { int pips = Mathf.Clamp(p.Pips, 0, 4); for (int j = 0; j < 4; j++) qp.Add(j < pips); s = $"−{p.Damage} ×{p.HitsToDown}"; WalkExp(p.Walk, p.WalkTicks, p.Walk ? p.WalkExposedTo.Count : 0); }
                }
                Text(text, s);
                r.style.minWidth = s.Length == 0 && !hostile ? ChipH : 150; // a glyph alone (hold, holster) sits in a slot-sized plate, not on an empty bar
                var pe = flQPipEl[k]; Show(pe, qp.Count > 0); pe.style.width = qp.Count > 0 ? qp.Count * 9.5f - 2f : 0; pe.MarkDirtyRepaint();
                void WalkExp(bool walks, int ticks, int exposedN)
                {   // Keel's walk-in: the footsteps glyph and its seconds before the pips; the hostiles that can fire on the way: a cream mark and their count
                    Show(walk, walks); Show(walkS, walks); if (walks) Text(walkS, $"{HandoverHost.WalkSeconds(ticks)} s");
                    Show(exp, exposedN > 0); Show(expN, exposedN > 0); if (exposedN > 0) Text(expN, exposedN.ToString());
                }
            }
                        flGo.style.left = lastX; flGo.style.top = lastY + 8;
        }

        (VisualElement root, Label text) FlBadge(int i)
        {
            while (flBadges.Count <= i)
            {
                var r = E(marks, null, 0, 0, float.NaN, 20); r.style.backgroundColor = Ink; Round(r, 10); Border(r, 1.4f, CellTurquoiseC); r.style.paddingLeft = 6; r.style.paddingRight = 6; r.style.justifyContent = Justify.Center;
                var t = L(r, "chip-value"); t.style.fontSize = 14; t.style.color = Cream; flBadges.Add((r, t));
            }
            Show(flBadges[i].root, true); return flBadges[i];
        }

        /// <summary>The kite's answer (Keel's PreviewAnswer): a clock glyph and the seconds until that hostile stands at its ghost, framed in its
        /// side colour. No words: the ghost says who and where, the seconds say when.</summary>
        (VisualElement root, Label text) FlKite(int i, Color side)
        {
            while (flKites.Count <= i)
            {
                var r = E(marks, null, 0, 0, float.NaN, 20); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.backgroundColor = FlGround; Round(r, 10); r.style.paddingLeft = 5; r.style.paddingRight = 6;
                var ic = E(r, null, w: 13, h: 13); Icon(ic, "p_time", Cream); var t = L(r, "chip-value"); t.style.fontSize = 14; t.style.marginLeft = 3; flKites.Add((r, t));
            }
            Border(flKites[i].root, 1.4f, side); Show(flKites[i].root, true); return flKites[i];
        }

        (VisualElement root, Label text) FlWalk(int i)
        {
            while (flWalks.Count <= i)
            {
                var r = E(marks, null, 0, 0, float.NaN, 20); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.backgroundColor = FlGround; Round(r, 10); r.style.paddingLeft = 5; r.style.paddingRight = 6;
                var ic = E(r, null, w: 14, h: 14); Icon(ic, "s_moving", Cream); var t = L(r, "chip-value"); t.style.fontSize = 14; t.style.marginLeft = 3; flWalks.Add((r, t));
            }
            Show(flWalks[i].root, true); return flWalks[i];
        }

        // ---------------------------------------------------------------- painting

        static Rect BarRect(Vector2 centreBottom, int max, float segW, float segH, float gap)
        {
            int segs = Mathf.Max(1, (max + 9) / 10); float w = segs * segW + (segs - 1) * gap;
            return new Rect(centreBottom.x - w / 2, centreBottom.y - segH, w, segH);
        }

        static void Box(Painter2D p, Rect r) { p.BeginPath(); p.MoveTo(r.min); p.LineTo(new Vector2(r.xMax, r.yMin)); p.LineTo(r.max); p.LineTo(new Vector2(r.xMin, r.yMax)); p.ClosePath(); }
        static void FillBox(Painter2D p, Rect r, Color c) { p.fillColor = c; Box(p, r); p.Fill(); }
        static void StrokeBox(Painter2D p, Rect r, Color c, float w) { p.strokeColor = c; p.lineWidth = w; Box(p, r); p.Stroke(); }

        /// <summary>Diagonal hatching inside r (45°, clipped to the rect by x; y stays inside for t in 0..1).</summary>
        static void Hatch(Painter2D p, Rect r, Color c, float step = 2.6f, float w = 1.1f)
        {
            p.strokeColor = c; p.lineWidth = w; p.lineCap = LineCap.Butt; float h = r.height;
            for (float o = -h; o < r.width; o += step)
            {
                var a = new Vector2(r.xMin + o, r.yMax); float t0 = Mathf.Max(0f, (r.xMin - a.x) / h), t1 = Mathf.Min(1f, (r.xMax - a.x) / h); if (t1 <= t0) continue;
                p.BeginPath(); p.MoveTo(new Vector2(a.x + t0 * h, a.y - t0 * h)); p.LineTo(new Vector2(a.x + t1 * h, a.y - t1 * h)); p.Stroke();
            }
        }

        /// <summary>Glyph frame B's bar: dark ground, a 1 px side-coloured frame, one segment per 10. Full = fill; just lost = white for 0.25 s;
        /// the preview's chunk hatched, pulsing at 1.2 s; empty = dark with an outline (it still reads inside a red cone); a preview that empties
        /// the bar = every remaining segment hatched and a small cross at the right end.</summary>
        static void SegBar(Painter2D p, Rect r, int hp, int max, int after, int flashFrom, Color side, Color fill, float segW, float gap, bool hatched, float now)
        {
            int segs = Mathf.Max(1, (max + 9) / 10);
            var outer = new Rect(r.xMin - 2f, r.yMin - 2f, r.width + 4f, r.height + 4f); FillBox(p, outer, FlGround); StrokeBox(p, outer, side, 1f);
            float pulse = 0.65f + 0.35f * Mathf.Sin(now * Mathf.PI * 2f / 1.2f); bool down = after <= 0 && after < hp;
            for (int i = 0; i < segs; i++)
            {
                int lo = i * 10; var s = new Rect(r.xMin + i * (segW + gap), r.yMin, segW, r.height);
                if (hp > lo && after <= lo) // the preview's chunk
                {
                    if (hatched) { FillBox(p, s, new Color(fill.r, fill.g, fill.b, 0.18f)); Hatch(p, s, new Color(fill.r, fill.g, fill.b, pulse)); }
                    else FillBox(p, s, new Color(fill.r, fill.g, fill.b, 0.45f * pulse));
                }
                else if (hp > lo) FillBox(p, s, fill);
                else if (flashFrom > lo) FillBox(p, s, Color.white);
                else StrokeBox(p, new Rect(s.xMin + 0.5f, s.yMin + 0.5f, s.width - 1f, s.height - 1f), new Color(Cream.r, Cream.g, Cream.b, 0.4f), 1f);
            }
            if (down)
            {
                var c = new Vector2(outer.xMax + 6f, outer.center.y); p.strokeColor = Cream; p.lineWidth = 1.8f; p.lineCap = LineCap.Round;
                p.BeginPath(); p.MoveTo(c + new Vector2(-3, -3)); p.LineTo(c + new Vector2(3, 3)); p.Stroke(); p.BeginPath(); p.MoveTo(c + new Vector2(3, -3)); p.LineTo(c + new Vector2(-3, 3)); p.Stroke();
            }
        }

        /// <summary>The portrait's charge: 3 px turquoise, one segment per 10, the spend hatched.</summary>
        static void ChargeBar(Painter2D p, Rect r, int charge, int max, int spend, bool hatched)
        {
            int segs = Mathf.Max(1, max / 10); float gap = 0.8f, sw = (r.width - (segs - 1) * gap) / segs;
            FillBox(p, new Rect(r.xMin - 1, r.yMin - 1, r.width + 2, r.height + 2), FlGround);
            for (int i = 0; i < segs; i++)
            {
                int lo = i * 10; var s = new Rect(r.xMin + i * (sw + gap), r.yMin, sw, r.height);
                if (charge > lo && charge - spend <= lo) { if (hatched) { FillBox(p, s, new Color(CellTurquoiseC.r, CellTurquoiseC.g, CellTurquoiseC.b, 0.3f)); Hatch(p, s, Cream, 2f, 0.8f); } else FillBox(p, s, new Color(1, 1, 1, 0.7f)); }
                else if (charge > lo) FillBox(p, s, CellTurquoiseC);
                else FillBox(p, s, new Color(1, 1, 1, 0.12f));
            }
        }

        /// <summary>Target mode's forecast: one 7 px dot per shot, filled cream = hit, outlined = miss, each ringed dotted (never a result).</summary>
        static void ForecastPips(Painter2D p, float x0, float y, float pitch, List<bool> hits)
        {
            for (int i = 0; i < hits.Count; i++)
            {
                var c = new Vector2(x0 + i * pitch, y);
                p.fillColor = FlGround; p.BeginPath(); p.Arc(c, 5.5f, Deg(0), Deg(360)); p.Fill();
                if (hits[i]) { p.fillColor = Cream; p.BeginPath(); p.Arc(c, 3.6f, Deg(0), Deg(360)); p.Fill(); }
                else { p.strokeColor = Cream; p.lineWidth = 1f; p.BeginPath(); p.Arc(c, 3.6f, Deg(0), Deg(360)); p.Stroke(); } // a 6 px hole: it reads as hollow at 1:1
            }
            if (hits.Count > 0) DashedPill(p, new Rect(x0 - 7.5f, y - 7.5f, (hits.Count - 1) * pitch + 15f, 15f), new Color(Cream.r, Cream.g, Cream.b, 0.75f)); // the row dotted-outlined: a forecast, never a result
        }

        /// <summary>The exposure mark (Keel: WalkExposedTo): a small triangle in the hostile's side colour with a cream rim and an ink "!".</summary>
        static void Exposed(Painter2D p, Vector2 c, Color side)
        {
            p.fillColor = side; p.strokeColor = Cream; p.lineWidth = 1.2f; p.lineJoin = LineJoin.Round;
            p.BeginPath(); p.MoveTo(c + new Vector2(0, -6.5f)); p.LineTo(c + new Vector2(6.5f, 5f)); p.LineTo(c + new Vector2(-6.5f, 5f)); p.ClosePath(); p.Fill(); p.Stroke();
            p.strokeColor = Ink; p.lineWidth = 1.6f; p.lineCap = LineCap.Round; p.BeginPath(); p.MoveTo(c + new Vector2(0, -2.5f)); p.LineTo(c + new Vector2(0, 1f)); p.Stroke();
            p.fillColor = Ink; p.BeginPath(); p.Arc(c + new Vector2(0, 3.2f), 0.9f, Deg(0), Deg(360)); p.Fill();
        }

        static void Crosshair(Painter2D p, Vector2 c, float r, Color col)
        {
            p.strokeColor = col; p.lineWidth = 1.6f; p.lineCap = LineCap.Round; p.BeginPath(); p.Arc(c, r * 0.7f, Deg(0), Deg(360)); p.Stroke();
            foreach (var d in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right }) { p.BeginPath(); p.MoveTo(c + d * r * 0.35f); p.LineTo(c + d * r * 1.15f); p.Stroke(); }
        }

        static void DashedPill(Painter2D p, Rect r, Color c)
        {
            p.strokeColor = c; p.lineWidth = 1.2f; p.lineCap = LineCap.Butt; float rad = r.height / 2, straight = r.width - 2 * rad;
            float per = 2 * straight + 2 * Mathf.PI * rad; int n = Mathf.Max(8, (int)(per / 9f));
            Vector2 At(float d)
            {
                d = Mathf.Repeat(d, per);
                if (d < straight) return new Vector2(r.xMin + rad + d, r.yMin); d -= straight;
                if (d < Mathf.PI * rad) { float a = -90 + d / rad * Mathf.Rad2Deg; return new Vector2(r.xMax - rad, r.center.y) + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * rad; } d -= Mathf.PI * rad;
                if (d < straight) return new Vector2(r.xMax - rad - d, r.yMax); d -= straight;
                { float a = 90 + d / rad * Mathf.Rad2Deg; return new Vector2(r.xMin + rad, r.center.y) + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * rad; }
            }
            float step = per / n;
            for (int i = 0; i < n; i++) { p.BeginPath(); for (int s = 0; s <= 4; s++) { var v = At(i * step + s / 4f * step * 0.55f); if (s == 0) p.MoveTo(v); else p.LineTo(v); } p.Stroke(); }
        }
    }
}
