using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Intel, redesigned (Glyph, briefs/014/glyph-less.md "Intel panel, redesigned"; Sky approved 2026-10-18; mock briefs/016/intel/).
    /// A dark panel sized to its icards, 26 px clear of the screws: one card a person the cell is after (Inez, the driver, the guard), 228 x 62,
    /// the role badge, then name · role on one line, then three fixed slots (need: a circle; temperament: a rounded square; ties or secret: a
    /// tab) and the next-step chip. ISlot rims say how it is known: solid learned, dashed told, dashed with "?" unknown; the facts are the
    /// engine's (Person.K*, Suspected, Secret and Lover), never the mock's. The chip is the agent who learns the next unknown slot and the
    /// verb (Watch for the routine, else Listen), checked with the engine's ScoutQuery so it never offers a refused order; turquoise when that
    /// agent is selected (a click gives the order: walk and act), cream when not (a click selects them). All learned and not turned: the
    /// Fixer's turn chip (a click opens his panel on the person). Turned: a ring and a tick on the badge, no chip. Hover: a learned or told
    /// slot shows its fragment, an unknown one the verb that learns it, the chip its verb, inline on line 1 in place of the role (Glyph r2).
    /// Sorted by the plan (the person the next timeline mark hangs on, then who a verb can teach, then the rest), re-sorted only when a fact
    /// is learned. Fragments are Fable's (text.json less.intel).</summary>
    public sealed partial class HudToolkit
    {
        const float IntelW = 280, CardW = 228, CardH = 62, CardGap = 6, IntelHead = 40, IntelPad = 26, IntelFoot = 26;
        static readonly string[] IntelRoles = { "waitress", "driver", "guard" };
        static readonly Color CardFill = new(0.086f, 0.259f, 0.251f), CardFillFirst = new(0.125f, 0.345f, 0.353f), CardRim = new(0.431f, 0.686f, 0.659f, 0.43f),
            SlotInk = new(0.039f, 0.149f, 0.141f), RoleInk = new(0.588f, 0.753f, 0.729f), TickTurq = new(0.376f, 0.878f, 0.918f);

        sealed class ICard
        {
            public VisualElement Root, Bar, Badge, BadgeRing, BadgeTick, Chip, ChipCameo, ChipGlyph, RoleGlyph; public Label Name, Role, Word;
            public readonly ISlot[] Slots = new ISlot[3]; public string Who = "";
        }
        sealed class ISlot { public VisualElement Hit, Frame, Glyph; public Label Q; public Paint Rim; public string State = "unk", Icon = "", Frag = "", Verb = ""; public int Kind; }
        VisualElement intelPanel, intelTip, intelTipGlyph; Label intelTipText; readonly List<ICard> icards = new();
        string[] intelOrder = (string[])IntelRoles.Clone(); string intelSig = ""; (int card, int slot) intelHover = (-1, -1);

        /// <summary>For scripted shots and checks: hover this card's slot (0-2) or chip (3) as the pointer would, else null.</summary>
        public static (int card, int slot)? IntelHoverForce;
        /// <summary>For scripted checks: the order of the cards (roles) and a card's chip clicked.</summary>
        public static string[] IntelOrderNow => live?.intelOrder ?? Array.Empty<string>();
        public static void IntelClick(int card) => live?.IntelChip(card);

        void BuildIntel()
        {
            intelPanel = E(intel, "s-panel_dark", pick: true); intelPanel.style.width = IntelW; intelPanel.style.marginTop = 0;
            var head = L(intelPanel, "section-label", 28, 14); head.text = "INTEL";
            var fold = E(intelPanel, null, IntelW - 28 - 14, 13, 14, 14, pick: true); Icon(fold, "chev_up", Cream);
            fold.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.IntelOpen = false; e.StopPropagation(); } });
            for (int i = 0; i < IntelRoles.Length; i++)
            {
                var c = new ICard(); int ci = i;
                c.Root = E(intelPanel, null, IntelPad, IntelHead + i * (CardH + CardGap), CardW, CardH, pick: true); Round(c.Root, 9); Border(c.Root, 1f, CardRim);
                c.Bar = E(c.Root, null, 0, 6, 3.4f, CardH - 12); c.Bar.style.backgroundColor = Turq; Round(c.Bar, 1.7f);
                c.Badge = E(c.Root, null, 26 - 18, 31 - 18, 36, 36, pick: true);
                c.BadgeRing = E(c.Root, null, 26 - 20, 31 - 20, 40, 40); Round(c.BadgeRing, 20); Border(c.BadgeRing, 2f, Turq);
                c.BadgeTick = E(c.Root, null, 26 + 7, 31 + 7, 14, 14); Round(c.BadgeTick, 7); c.BadgeTick.style.backgroundColor = SlotInk; Icon(E(c.BadgeTick, null, 1, 1, 12, 12), "check", TickTurq); // Glyph r2: 12 px, brighter turquoise, a 1 px ink outline
                c.Badge.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { FindPerson(icards[ci].Who); e.StopPropagation(); } });
                c.Name = L(c.Root, "verb-name", 52, 21 - 15); c.Name.style.fontSize = 13.5f; c.Name.pickingMode = PickingMode.Position;
                c.Name.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { FindPerson(icards[ci].Who); e.StopPropagation(); } });
                c.Role = L(c.Root, "panel-sub", 0, 21 - 13.5f); c.Role.style.fontSize = 12f; c.Role.style.color = RoleInk;
                c.RoleGlyph = E(c.Root, null, 0, 21 - 12.5f, 13, 13); Show(c.RoleGlyph, false); c.Word = L(c.Root, "verb-name", 0, 21 - 13.5f); c.Word.style.fontSize = 12f; Show(c.Word, false); // the hovered slot's glyph, inline on line 1 (Glyph r2: no floating tooltip)
                float[] cx = { 64 - 12, 94 - 12, 127 - 15 }; float[] w = { 24, 24, 30 };
                for (int s = 0; s < 3; s++)
                {
                    var sl = c.Slots[s] = new ISlot { Kind = s }; int si = s;
                    sl.Hit = E(c.Root, null, cx[s] - 2, 44 - 14, w[s] + 4, 28, pick: true); // the hit area, at least 28 px tall
                    sl.Frame = E(sl.Hit, null, 2, 2, w[s], 24); sl.Frame.style.backgroundColor = SlotInk; Round(sl.Frame, s == 0 ? 12 : s == 1 ? 6 : 5);
                    sl.Rim = P(sl.Hit, 2, 2, w[s], 24, (p, r) => SlotRim(p, r, sl, ci, si));
                    sl.Glyph = E(sl.Frame, null, (w[s] - 15) / 2, 4.5f, 15, 15);
                    sl.Q = L(sl.Frame, "verb-name", 0, 0, w[s], 24); sl.Q.text = "?"; sl.Q.style.fontSize = 12.5f; sl.Q.style.unityTextAlign = TextAnchor.MiddleCenter; sl.Q.style.color = new Color(Cream.r, Cream.g, Cream.b, 0.7f);
                    sl.Hit.RegisterCallback<PointerEnterEvent>(_ => intelHover = (ci, si)); sl.Hit.RegisterCallback<PointerLeaveEvent>(_ => { if (intelHover == (ci, si)) intelHover = (-1, -1); });
                    sl.Hit.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { IntelChip(ci); e.StopPropagation(); } }); // a slot: the same as its chip
                }
                c.Chip = E(c.Root, null, CardW - 10 - 52, 44 - 13, 52, 26, pick: true); Round(c.Chip, 13); c.Chip.style.backgroundColor = SlotInk; // Glyph r2: 8 px either side of the verb glyph
                c.ChipCameo = E(c.Chip, null, 7, 4, 18, 18); Round(c.ChipCameo, 9); Border(c.ChipCameo, 1f, Cream);
                c.ChipGlyph = E(c.Chip, null, 29.5f, 4.2f, 15, 15);
                c.Chip.RegisterCallback<PointerEnterEvent>(_ => intelHover = (ci, 3)); c.Chip.RegisterCallback<PointerLeaveEvent>(_ => { if (intelHover == (ci, 3)) intelHover = (-1, -1); });
                c.Chip.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { IntelChip(ci); e.StopPropagation(); } });
                icards.Add(c);
            }
            manifest.Add((intelPanel, "image", () => "intel")); // names and roles, facts as glyphs: no running text (the budget reads names as words)
            // the tooltip: above everything, opening above the slot
            intelTip = E(root, null, 0, 0, float.NaN, 24); intelTip.style.backgroundColor = new Color(0.047f, 0.157f, 0.153f, 0.97f); Border(intelTip, 1.3f, Cream); Round(intelTip, 8); intelTip.style.flexDirection = FlexDirection.Row; intelTip.style.alignItems = Align.Center; intelTip.style.paddingLeft = 8; intelTip.style.paddingRight = 10; // Glyph: a dark chip, cream 1.3 px rim, radius 8
            intelTipGlyph = E(intelTip, null, w: 13, h: 13); intelTipGlyph.style.marginRight = 6; intelTipText = L(intelTip, "verb-name"); intelTipText.style.fontSize = 12f;
            manifest.Add((intelTip, "image", () => "intel tip")); Show(intelTip, false);
        }

        /// <summary>The slot's rim: solid learned, dashed told or unknown; turquoise ring while hovered.</summary>
        void SlotRim(Painter2D p, Rect r, ISlot sl, int ci, int si)
        {
            float rad = sl.Kind == 0 ? 12 : sl.Kind == 1 ? 6 : 5; var c = Cream; bool dashed = sl.State != "learned";
            void Shape(float inset) { var rr = new Rect(r.x + inset, r.y + inset, r.width - 2 * inset, r.height - 2 * inset); float k = Mathf.Min(rad - inset, Mathf.Min(rr.width, rr.height) / 2); RoundRect(p, rr, k); }
            p.lineWidth = 1.3f; p.strokeColor = c;
            if (!dashed) { p.BeginPath(); Shape(0.65f); p.Stroke(); }
            else
            {   // Glyph r2 A: told = long dashes (3 px on, 2 off); unknown = fine dots (1 px on, 2 off), so the rim says which at a glance
                var pts = RoundRectPoints(new Rect(r.x + 0.65f, r.y + 0.65f, r.width - 1.3f, r.height - 1.3f), Mathf.Min(rad - 0.65f, (r.height - 1.3f) / 2), 240);
                float on = sl.State == "told" ? 3f : 1f, off = 2f, along = 0; bool draw = true; float run = 0;
                p.lineCap = sl.State == "told" ? LineCap.Butt : LineCap.Round; if (sl.State != "told") p.lineWidth = 1.4f;
                for (int i = 1; i < pts.Count; i++)
                {
                    float d = Vector2.Distance(pts[i - 1], pts[i]); run += d; along += d;
                    if (draw) { p.BeginPath(); p.MoveTo(pts[i - 1]); p.LineTo(pts[i]); p.Stroke(); }
                    if (run >= (draw ? on : off)) { draw = !draw; run = 0; }
                }
            }
            if ((IntelHoverForce ?? intelHover) == (ci, si)) { p.strokeColor = TurqLight; p.lineWidth = 1.6f; p.BeginPath(); var rr = new Rect(r.x - 2.4f, r.y - 2.4f, r.width + 4.8f, r.height + 4.8f); RoundRect(p, rr, rad + 2.4f); p.Stroke(); }
        }

        static void RoundRect(Painter2D p, Rect r, float k)
        {
            p.MoveTo(new Vector2(r.xMin + k, r.yMin)); p.LineTo(new Vector2(r.xMax - k, r.yMin)); p.ArcTo(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMin + k), k);
            p.LineTo(new Vector2(r.xMax, r.yMax - k)); p.ArcTo(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - k, r.yMax), k);
            p.LineTo(new Vector2(r.xMin + k, r.yMax)); p.ArcTo(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMax - k), k);
            p.LineTo(new Vector2(r.xMin, r.yMin + k)); p.ArcTo(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + k, r.yMin), k); p.ClosePath();
        }
        static List<Vector2> RoundRectPoints(Rect r, float k, int n)
        {
            // walk the perimeter at even arc length
            var segs = new List<Vector2>();
            void Arc(Vector2 c, float a0) { for (int i = 0; i <= 8; i++) { float a = (a0 + 90f * i / 8) * Mathf.Deg2Rad; segs.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * k); } }
            segs.Add(new Vector2(r.xMin + k, r.yMin)); segs.Add(new Vector2(r.xMax - k, r.yMin)); Arc(new Vector2(r.xMax - k, r.yMin + k), -90);
            segs.Add(new Vector2(r.xMax, r.yMax - k)); Arc(new Vector2(r.xMax - k, r.yMax - k), 0); segs.Add(new Vector2(r.xMin + k, r.yMax));
            Arc(new Vector2(r.xMin + k, r.yMax - k), 90); segs.Add(new Vector2(r.xMin, r.yMin + k)); Arc(new Vector2(r.xMin + k, r.yMin + k), 180);
            float total = 0; for (int i = 1; i < segs.Count; i++) total += Vector2.Distance(segs[i - 1], segs[i]);
            var outp = new List<Vector2>(); float step = total / n, acc = 0, next = 0; int j = 1;
            for (int s = 0; s < n; s++)
            {
                while (j < segs.Count && acc + Vector2.Distance(segs[j - 1], segs[j]) < next) { acc += Vector2.Distance(segs[j - 1], segs[j]); j++; }
                if (j >= segs.Count) break; float d = Vector2.Distance(segs[j - 1], segs[j]); outp.Add(Vector2.Lerp(segs[j - 1], segs[j], d < 1e-4f ? 0 : (next - acc) / d)); next += step;
            }
            return outp;
        }

        /// <summary>A fact's fragment from Fable's text.json less.intel ("group.value"); {name} is the lover as the HUD names them ("Inez", "the
        /// guard"). A missing key shows the raw value, so a gap is visible.</summary>
        string IntelFragment(string group, string value, string name = "")
        {
            var t = host.Text?.Intel; string key = group == "secret" ? (value == "affair" && name.Length > 0 ? "affair-named" : value.Length == 0 ? "none" : value) : value;
            if (t != null && t.TryGetValue(group + "." + key, out var w)) return w.Replace("{name}", name);
            return value.Length > 0 ? char.ToUpperInvariant(value[0]) + value[1..] : value;
        }
        /// <summary>The layout audit's worst cases (Fable's longest fragments).</summary>
        public static readonly string[] IntelWorst = { "Protection", "Promotion" };

        /// <summary>What teaches the next unknown slot, as (agent, verb), checked with the engine (never a refused order); the Fixer's turn when
        /// everything is learned and the person is not turned; null when there is nothing to do.</summary>
        (OpId by, string verb)? IntelNext(Person p, string role)
        {
            var g = host.Game; if (g == null || p.Turned) return null;
            bool needOpen = !p.KNeed, tempOpen = !p.KTemperament, tiesOpen = !(p.KSecret || (p.KNeed && p.KTemperament && p.Secret.Length == 0));
            bool Ok(string verb) { Answer a = default; try { a = g.ScoutQuery(verb, role); } catch { return false; } return a.ok || a.code is "too-far" or "agent-busy"; }
            if (!p.KRoutine && Ok("watch")) return (OpId.Scout, "watch");
            if ((needOpen || tempOpen || tiesOpen) && Ok("listen")) return (OpId.Scout, "listen");
            if ((needOpen || tempOpen || tiesOpen) && Ok("watch")) return (OpId.Scout, "watch");
            if (!needOpen && !tempOpen && role != "councillor") return (OpId.Fixer, "turn");
            return null;
        }

        /// <summary>The chip (or a slot) clicked: the agent selected first; selected, the order is given (walk, then act), or for the Fixer's
        /// turn his panel opens on the person.</summary>
        void IntelChip(int ci)
        {
            var g = host.Game; if (g == null || ci >= icards.Count) return; var role = icards[ci].Who; var p = g.PersonByRole(role);
            if (IntelNext(p, role) is not { } nx0) return; var (by, verb) = nx0;
            if (!host.Selected[(int)by] || host.Agent != by) { host.Select(by, false); return; }
            host.IntelOrder(role, verb);
        }

        void IntelFrame()
        {
            var g = host.Game; bool on = g != null && m.IntelOpen && !m.Ended && !m.BriefingOpen;
            if (intelPanel == null) BuildIntel();
            Show(intelOpen, false); Show(intelPanel, on); Show(intelTip, false); if (!on) return;
            intel.style.width = IntelW;
            // the order: re-sorted only when what the cell knows changes
            var sig = ""; foreach (var r in IntelRoles) { var p = g.PersonByRole(r); sig += $"{r}{(p.KRoutine ? 1 : 0)}{(p.KNeed ? 1 : 0)}{(p.KTemperament ? 1 : 0)}{(p.KSecret ? 1 : 0)}{(p.Turned ? 1 : 0)}|"; }
            if (sig != intelSig)
            {
                intelSig = sig; string markRole = "";
                var tl = host.Text?.Timeline; if (tl != null) foreach (var (at, label, known) in tl) { var pr = at.Split(':'); int tick = (int.Parse(pr[0]) * 60 + int.Parse(pr[1])) * Game.Hz; if (tick <= g.Tick) continue; markRole = known ? MarkPerson(label) : ""; break; }
                int Rank(string r) { var p = g.PersonByRole(r); return r == markRole && !p.Turned ? 0 : IntelNext(p, r) is { by: OpId.Scout } ? 1 : 2; }
                var list = new List<string>(IntelRoles); list.Sort((a, b) => { int c = Rank(a).CompareTo(Rank(b)); return c != 0 ? c : Array.IndexOf(IntelRoles, a).CompareTo(Array.IndexOf(IntelRoles, b)); });
                intelOrder = list.ToArray();
            }
            int n = intelOrder.Length; intelPanel.style.height = IntelHead + n * CardH + (n - 1) * CardGap + IntelFoot;
            bool stress = Stress;
            for (int i = 0; i < icards.Count; i++)
            {
                var c = icards[i]; var role = intelOrder[i]; c.Who = role; var p = g.PersonByRole(role);
                c.Root.style.backgroundColor = i == 0 ? CardFillFirst : CardFill; Show(c.Bar, i == 0);
                Bg(c.Badge, CastBadge(role), Color.white); Show(c.BadgeRing, p.Turned); Show(c.BadgeTick, p.Turned);
                string name = stress ? "Bartholomew" : host.Text?.TargetName(role, p.Name) ?? p.Name; if (name.Length > 0) name = char.ToUpperInvariant(name[0]) + name[1..]; string job = role switch { "waitress" => "café waitress", "driver" => "Vale's driver", "guard" => "door guard", _ => "" }; // "The driver", as the mock
                Text(c.Name, name); Text(c.Role, "·  " + job);
                float nw = c.Name.resolvedStyle.width; c.Role.style.left = 52 + (float.IsNaN(nw) ? 60 : nw) + 6;
                // the slots, from the engine's knowledge
                var nx = IntelNext(p, role);
                Fill(c.Slots[0], p.KNeed ? "learned" : p.Suspected.Length > 0 ? "told" : "unk", p.KNeed ? p.Need : p.Suspected, p.KNeed ? IntelFragment("need", p.Need) : p.Suspected.Length > 0 ? IntelFragment("need", p.Suspected) : "");
                Fill(c.Slots[1], p.KTemperament ? "learned" : "unk", p.Temperament, p.KTemperament ? IntelFragment("temperament", p.Temperament) : "");
                bool tiesKnown = p.KSecret && p.Secret.Length > 0, noneKnown = p.KNeed && p.KTemperament && p.Secret.Length == 0;
                string lover = p.Lover != null ? host.Text?.TargetName(p.Lover.Role, p.Lover.Name) ?? p.Lover.Name : ""; // "Loves Inez", "Loves the guard" (Aster)
                Fill(c.Slots[2], tiesKnown || noneKnown ? "learned" : "unk", tiesKnown ? (p.Secret == "affair" ? "kin" : "secret") : noneKnown ? "secret_none" : "", tiesKnown ? IntelFragment("secret", p.Secret, lover) : noneKnown ? IntelFragment("secret", "") : "");
                foreach (var sl in c.Slots) sl.Verb = sl.State == "unk" && nx is { by: OpId.Scout } ? nx.Value.verb : sl.State == "unk" ? "listen" : "";
                if (stress) foreach (var sl in c.Slots) { sl.Frag = IntelWorst[0]; }
                // the chip
                Show(c.Chip, nx.HasValue);
                if (nx is { } nxv)
                {
                    var (by, verb) = nxv; bool sel = host.Selected[(int)by] && host.Agent == by;
                    Bg(c.ChipCameo, $"portraits/cameo_{Op.Name(by)}_normal", Color.white); Border(c.Chip, 1.3f, sel ? Turq : Cream);
                    Icon(c.ChipGlyph, verb == "turn" ? "turn_person" : verb, sel ? TurqLight : Cream);
                }
                foreach (var sl in c.Slots) sl.Rim.MarkDirtyRepaint();
            }
            // hover, inline (Glyph r2): the hovered slot's fragment, or the verb that learns it, replaces the role on line 1; the slot keeps its ring
            var (hc, hs) = IntelHoverForce ?? intelHover;
            for (int i = 0; i < icards.Count; i++)
            {
                var c = icards[i]; string word = "", icon = "";
                if (i == hc && hs == 3) { var nx = IntelNext(g.PersonByRole(c.Who), c.Who); string v = nx?.verb ?? ""; word = v.Length > 0 ? IntelFragment("verb", v) : ""; icon = v == "turn" ? "turn_person" : v; }
                else if (i == hc && hs >= 0 && hs < 3) { var sl = c.Slots[hs]; if (sl.State == "unk") { word = sl.Verb.Length > 0 ? IntelFragment("verb", sl.Verb) : ""; icon = sl.Verb; } else { word = sl.Frag; icon = sl.Icon; } }
                float nw = c.Name.resolvedStyle.width; float x0 = 52 + (float.IsNaN(nw) ? 60 : nw) + 6;
                bool inline_ = word.Length > 0; Show(c.RoleGlyph, inline_ && icon.Length > 0); Show(c.Word, inline_);
                if (inline_)
                {   // "Inez · [glyph] Debt": the dot, the glyph, the word (Jost bold, turquoise for what to do, cream for a fact)
                    bool learn = hs == 3 || c.Slots[Math.Min(hs, 2)].State == "unk"; Color col = learn ? TurqLight : Cream;
                    Text(c.Role, "·"); c.Role.style.left = x0; c.Role.style.color = RoleInk;
                    float wx = x0 + 10; if (icon.Length > 0) { Icon(c.RoleGlyph, icon, col); c.RoleGlyph.style.left = wx; wx += 13 + 5; }
                    Text(c.Word, word); c.Word.style.left = wx; c.Word.style.color = col;
                }
                else { c.Role.style.color = RoleInk; }
            }
        }

        void Fill(ISlot sl, string state, string icon, string frag)
        {
            sl.State = state; sl.Icon = state == "unk" ? "" : icon; sl.Frag = frag;
            bool q = state == "unk"; Show(sl.Q, q); Show(sl.Glyph, !q && sl.Icon.Length > 0);
            if (!q && sl.Icon.Length > 0) { Icon(sl.Glyph, sl.Icon, Cream); sl.Glyph.style.opacity = state == "told" ? 0.8f : 1f; }
        }
    }
}
