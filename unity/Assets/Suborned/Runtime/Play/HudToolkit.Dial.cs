using System;
using System.Collections.Generic;
using System.Text;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 098 step 0, the dial's dev panel on the start page (HostDial; Sky 2026-11-07: "add 3 more guards, take some away, bring in more cars etc - the game
    /// then just does that"). Bottom left, on the street beside the book's page, only while the start book is up; its plate on the grid's 48 px margin line, so all of it sits
    /// in the page's body (the book's rule B3, tools/hud-budget/book.ts; at 24 its words stood 4 px into the margin). Per kind (guards, people, and cars since step 1: passing traffic, today 0): −N, −, the number, +, +N
    /// (clicks; no key does any of it; N the kind's big step, HostDial.DialBigStep: people and guards 5, cars 4), the kind's name over what it counts (since step 1b, Sky 2026-11-07
    /// "people counter is for street": "People / on the street", "Guards / in the zone", "Cars / on the roads"), with today's number, the floor (the cast never leave) and the most beside it. Today puts every kind back; Play builds the
    /// street with those numbers and begins (the page's Begin does the same once a number changed). The line under them says what the street behind the page plays.
    /// A dev panel, not final UI: the book's own colours and Jost, for Glyph to style. Named elements ("dial-guards-plus", "dial-people-plus-big", "dial-play") for a scripted click.</summary>
    public sealed partial class HudToolkit
    {
        const float DlW = 560f, DlX = 48f, DlLineW = 400f, DlRow0 = 74f, DlRowH = 46f;
        /// <summary>The panel's height for its rows (one a kind the dial builds: 198 px for two, 244 for three), and where the line under them sits.</summary>
        float DlH => DlLineY + 32f; float DlLineY => DlRow0 + DlRowH * Mathf.Max(1, host.DialToday?.Count ?? 0);
        static readonly Color DlRim = Hex(0xcbc6c3);
        VisualElement dl; readonly Dictionary<string, Label> dlNum = new(); Label dlLine; string dlLineText = "";
        /// <summary>The probe (ProofDial): the panel's element by name ("dial", "dial-guards-plus", "dial-play"), while the panel is up; else null.</summary>
        public static VisualElement DialElement(string name) { var d = live?.dl; if (d == null || d.panel == null) return null; if (d.name == name) return d; return d.Q<VisualElement>(name); }
        /// <summary>The probe: the panel's line as drawn.</summary>
        public static string DialLineNow => live?.dlLine?.text ?? "";

        /// <summary>From BookHud, each frame the book is up: the panel while it is the start, its numbers and its line kept current.</summary>
        void DialHud()
        {
            bool want = host.BookStart && host.Game != null && host.DialToday != null && bkPage != null;
            if (!want || !DialOpen) { if (dl != null) { dl.RemoveFromHierarchy(); dl = null; } }
            if (!want || DialOpen) { if (dlChip != null) { dlChip.RemoveFromHierarchy(); dlChip = null; } }
            if (!want) return;
            if (!DialOpen) { if (dlChip == null || dlChip.parent != bkPage) DialChipBuild(); return; } // brief 108: folded to its chip (HudToolkit.DialFold)
            if (dl == null || dl.parent != bkPage) DialBuild();
            foreach (var k in Population.Kinds)
            {
                if (!dlNum.TryGetValue(k, out var l) || host.DialTodayOf(k) is not { } t) continue;
                int n = host.DialShown(k); Text(l, n.ToString()); var c = n != t.Today ? TurqLight : Cream; if (l.style.color.value != c) l.style.color = c;
            }
            var s = DialLine(); if (s != dlLineText) { dlLineText = s; Text(dlLine, s); float sz = 15f; while (sz > 11f && BkTw(s, sz, "Jost-Medium") > DlLineW) sz -= 0.5f; dlLine.style.fontSize = sz; }
        }

        /// <summary>What the street behind the page plays (the census when the dial is on), or that the panel's numbers are not built yet.</summary>
        string DialLine()
        {
            if (host.DialPending) return "Changed: Play builds this street";
            var g = host.Game; if (g == null || !g.DialOn) return "Now: today's world";
            var sb = new StringBuilder("Now: ");
            for (int i = 0; i < g.Census.Count; i++)
            {
                var c = g.Census[i]; int d = c.Count - c.Today; if (i > 0) sb.Append(" · ");
                sb.Append(c.Kind).Append(' ').Append(c.Count); if (d != 0) sb.Append(" (").Append(d > 0 ? "+" : "−").Append(Math.Abs(d)).Append(')');
            }
            return sb.ToString();
        }

        void DialBuild()
        {
            dl?.RemoveFromHierarchy(); dlNum.Clear(); dlLineText = "";
            dl = E(bkPage, null, DlX, BookLayout.GridH - 24f - DlH, DlW, DlH, true); dl.name = "dial";
            Round(dl, 16f); dl.style.backgroundColor = new Color(BkBody.r, BkBody.g, BkBody.b, 0.94f); Border(dl, 1.5f, DlRim);
            PgText(dl, 18f, 12f, "Jost-SemiBold", 14f, BkDimHex, "The dial · dev", 0.22f, true);
            DlButton("dial-fold", DlW - 16f - 86f - 10f - 34f, 25f, 34f, 30f, "−", 18f, BkBody, () => DialUnfold(false)); // brief 108: folds back to its chip
            DlButton("dial-today", DlW - 16f - 86f, 25f, 86f, 30f, "Today", 15f, BkDeep, host.DialReset);
            float cy = DlRow0;
            foreach (var k in Population.Kinds)
            {
                var kind = k; var t = host.DialTodayOf(k); if (t == null) continue; int big = host.DialBigStep(k);
                BkPut(dl, 18f, cy - 8f, char.ToUpperInvariant(k[0]) + k.Substring(1), 19f, "Jost-SemiBold", BkCreamHex); // brief 098 step 1b: what the number counts under the kind ("People / on the street")
                var m = BkPut(dl, 18f, cy + 11f, HandoverHost.DialMeasureWords(t), 13f, "Jost-Medium", BkDimHex); m.name = $"dial-{k}-measure";
                DlButton($"dial-{k}-minus-big", 112f, cy, 46f, 34f, "−" + big, 16f, BkDeep, () => host.DialStep(kind, -big));
                DlButton($"dial-{k}-minus", 162f, cy, 36f, 34f, "−", 20f, BkDeep, () => host.DialStep(kind, -1));
                var n = BkPut(dl, 200f, cy, "", 25f, "Jost-Bold", BkCreamHex); n.style.width = 68f; n.style.unityTextAlign = TextAnchor.MiddleCenter; dlNum[k] = n;
                DlButton($"dial-{k}-plus", 270f, cy, 36f, 34f, "+", 20f, BkDeep, () => host.DialStep(kind, 1));
                DlButton($"dial-{k}-plus-big", 310f, cy, 46f, 34f, "+" + big, 16f, BkDeep, () => host.DialStep(kind, big));
                BkPut(dl, 370f, cy, $"today {t.Today} · {t.Floor}–{t.Max}", 15f, "Jost-Medium", BkDimHex);
                cy += DlRowH;
            }
            dlLine = BkPut(dl, 18f, DlLineY, "", 15f, "Jost-Medium", BkCreamHex);
            DlButton("dial-play", DlW - 16f - 112f, DlLineY - 2f, 112f, 38f, "Play ›", 19f, BkOrange, host.DialPlay);
        }

        /// <summary>One of the panel's buttons: a rounded plate (the book's deep teal, or its orange for Play) with a chrome rim, the word centred; a hover lights the rim.</summary>
        VisualElement DlButton(string name, float x, float cy, float w, float h, string word, float size, Color body, Action click)
        {
            var e = E(dl, null, x, cy - h / 2f, w, h, true); e.name = name; Round(e, h / 2f); e.style.backgroundColor = body; Border(e, 1.5f, DlRim);
            BkPut(e, (w - BkTw(word, size, "Jost-Bold")) / 2f, h / 2f, word, size, "Jost-Bold", BkCreamHex);
            e.RegisterCallback<PointerEnterEvent>(_ => Border(e, 1.5f, TurqLight)); e.RegisterCallback<PointerLeaveEvent>(_ => Border(e, 1.5f, DlRim));
            e.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); click(); });
            return e;
        }
    }
}
