using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using Camera = UnityEngine.Camera;
using Event = UnityEngine.Event;

namespace Suborned.Play
{
    /// <summary>The play HUD at round 4's v10 "signage hotbar" fidelity (Sky, 2026-10-15; briefs/011/ui/round4/v10-signage-hotbar*.jpg), simple and
    /// placeholder-iconed: the roster of four slots at the bottom (number, portrait, health pips, state), the 1–4 verb hotbar above it with the
    /// verb a right click gives lit, the top-right pill (mission clock, next event, $, Trail, Alert pips), number and verb markers on the pavement
    /// under the cell, at most two icons over a cast person, the price chip at the cursor ($ · time · Trail · Alert) before a right click commits
    /// (§02), and the end card with the debrief numbers. Transit-signage enamel: deep teal plates, cream type, thin brass-free borders. Plain
    /// IMGUI on its own component, reading HandoverHost only, so Glyph's art can replace it whole. Laid out at 1080p, scaled.</summary>
    [RequireComponent(typeof(HandoverHost))]
    public sealed class PlayHud : MonoBehaviour
    {
        // never kept over a domain reload (a kept style with its textures gone would draw nothing): built on the first OnGUI
        [System.NonSerialized] HandoverHost host; [System.NonSerialized] Texture2D plate, plateLit, edge, pipOn, pipOff, white, ring, refusedPlate, okPlate, cursorAct;
        [System.NonSerialized] bool cursorIsAct;
        [System.NonSerialized] GUIStyle txt, big, small, key, mid, smallC, icon, iconSmall, smallBright, txtSmallBold, smallWrap, wrapBright, wrapBold, wrapRefused; float s;
        [System.NonSerialized] Texture2D barOn, barOff, chipAgent, refusedText;
        // per-frame strings are rebuilt only when what they show changes (Sky's freeze review: the HUD allocated ~1,800 blocks a frame)
        [System.NonSerialized] string pillClock = "", pillNext = "", pillCash = "", pillTrail = "", pillState = "", pillCity = "", chipLine = "", tracker = "";
        [System.NonSerialized] readonly string[] slotState = new string[4];
        [System.NonSerialized] readonly (string a, string b)[] icons = new (string, string)[4];
        [System.NonSerialized] int pillKey = int.MinValue, chipKey; [System.NonSerialized] HandoverHost.Intent chipFor; [System.NonSerialized] Vector2 chipSize;
        static readonly string[] Nums = { "1", "2", "3", "4" };
        static readonly string[] Roles = { "waitress", "driver", "guard", "councillor" };
        static readonly List<Rect> hits = new();
        static readonly Color Teal = new(0.09f, 0.35f, 0.33f, 0.96f), TealLit = new(0.16f, 0.52f, 0.48f, 1f), Cream = new(0.96f, 0.93f, 0.85f), Dim = new(0.62f, 0.72f, 0.69f);
        static readonly string[] Names = { "Scout", "Fixer", "Tech", "Enforcer" };

        public static bool Blocks(Vector2 mouse) { var p = new Vector2(mouse.x, Screen.height - mouse.y); foreach (var r in hits) if (r.Contains(p)) return true; return false; }

        void Awake() => host = GetComponent<HandoverHost>();

        static Texture2D Tex(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

        /// <summary>A ring texture (placeholder until Glyph's kit): a circle outline, optionally with a centre dot (the action cursor).</summary>
        static Texture2D Ring(int n, int w, Color c, bool dot = false)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; float r = n / 2f - 1f;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - n / 2f + 0.5f) * (x - n / 2f + 0.5f) + (y - n / 2f + 0.5f) * (y - n / 2f + 0.5f));
                    bool on = Mathf.Abs(d - (r - w / 2f)) <= w / 2f || (dot && d <= 2.5f);
                    t.SetPixel(x, y, on ? c : new Color(0, 0, 0, 0));
                }
            t.Apply(); return t;
        }

        void Styles()
        {
            if (txt == null || plate == null)
            {
                plate = Tex(Teal); plateLit = Tex(TealLit); edge = Tex(Cream); pipOn = Tex(Cream); pipOff = Tex(new Color(1, 1, 1, 0.22f)); white = Tex(Color.white);
                refusedPlate = Tex(new Color(0.42f, 0.16f, 0.14f, 0.95f)); okPlate = Tex(new Color(0.12f, 0.42f, 0.32f, 0.95f));
                ring = Ring(64, 3, Cream); cursorAct = Ring(32, 3, Cream, true);
                txt = new GUIStyle(GUI.skin.label) { normal = { textColor = Cream }, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = false, clipping = TextClipping.Overflow };
                big = new GUIStyle(txt); small = new GUIStyle(txt) { fontStyle = FontStyle.Normal, normal = { textColor = Dim } };
                key = new GUIStyle(txt) { alignment = TextAnchor.MiddleCenter }; mid = new GUIStyle(txt) { alignment = TextAnchor.MiddleCenter };
                smallC = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter }; icon = new GUIStyle(mid); iconSmall = new GUIStyle(mid);
                smallBright = new GUIStyle(small) { normal = { textColor = Cream } }; txtSmallBold = new GUIStyle(txt); smallWrap = new GUIStyle(small) { wordWrap = true, alignment = TextAnchor.UpperLeft };
                wrapBright = new GUIStyle(smallWrap) { normal = { textColor = Cream } }; wrapBold = new GUIStyle(wrapBright) { fontStyle = FontStyle.Bold };
                wrapRefused = new GUIStyle(smallWrap) { normal = { textColor = new Color(1f, 0.72f, 0.62f) } };
                barOn = Tex(Cream); barOff = Tex(new Color(1, 1, 1, 0.18f)); chipAgent = Tex(new Color(0.85f, 0.8f, 0.62f, 1f)); refusedText = Tex(new Color(0.42f, 0.16f, 0.14f, 0.95f));
            }
            txt.fontSize = Mathf.RoundToInt(18 * s); big.fontSize = Mathf.RoundToInt(24 * s); small.fontSize = Mathf.RoundToInt(13 * s); key.fontSize = Mathf.RoundToInt(13 * s); mid.fontSize = Mathf.RoundToInt(15 * s);
            smallC.fontSize = small.fontSize; smallBright.fontSize = Mathf.RoundToInt(14 * s); smallWrap.fontSize = Mathf.RoundToInt(14 * s);
            wrapBright.fontSize = wrapRefused.fontSize = Mathf.RoundToInt(15 * s); wrapBold.fontSize = Mathf.RoundToInt(16 * s); txtSmallBold.fontSize = Mathf.RoundToInt(15 * s); icon.fontSize = Mathf.RoundToInt(14 * s); iconSmall.fontSize = Mathf.RoundToInt(10 * s);
        }

        void Plate(Rect r, bool lit = false)
        {
            GUI.DrawTexture(r, lit ? plateLit : plate);
            float b = Mathf.Max(1f, 1.5f * s);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, b), edge); GUI.DrawTexture(new Rect(r.x, r.yMax - b, r.width, b), edge);
            GUI.DrawTexture(new Rect(r.x, r.y, b, r.height), edge); GUI.DrawTexture(new Rect(r.xMax - b, r.y, b, r.height), edge);
        }

        void Pips(float x, float y, int on, int of, float d)
        {
            for (int i = 0; i < of; i++) GUI.DrawTexture(new Rect(x + i * d * 1.6f, y, d, d), i < on ? pipOn : pipOff);
        }

        void OnGUI()
        {
            if (host == null || host.Game == null) return;
            s = Screen.height / 1080f; Styles();
            if (Event.current.type == EventType.Layout) hits.Clear();
            var g = host.Game; var cam = host.View;
            if (host.Fault != null) { var fr = new Rect(20 * s, 20 * s, 1000 * s, 50 * s); Plate(fr); GUI.Label(new Rect(fr.x + 14 * s, fr.y, fr.width, fr.height), $"<color=#ff9a8a>{host.Status}</color>", txt); return; }

            // markers on the pavement under the cell: the agent's number and what it is doing
            if (cam && host.Views != null)
                for (int i = 0; i < 4; i++)
                {
                    var t = host.Views.Find("op/" + Op.Name((OpId)i)); if (!t || !t.gameObject.activeSelf) continue;
                    var p = cam.WorldToScreenPoint(t.position + Vector3.down * 0.15f); if (p.z <= 0) continue;
                    var op = g.Ops[i]; string verb = op.Busy != null ? HandoverHost.Glyph(op.Busy.Do, op.Busy.Approach) : op.Walk != null ? (op.Running ? "»" : "›") : "✓";
                    var r = new Rect(p.x - 26 * s, Screen.height - p.y + 2 * s, 52 * s, 20 * s);
                    if (host.Selected[i]) { GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height), plateLit); }
                    GUI.Label(new Rect(r.x + 4 * s, r.y, 18 * s, r.height), Nums[i], mid);
                    GUI.Label(new Rect(r.x + 20 * s, r.y, 32 * s, r.height), verb, mid);
                }

            // at most two icons over a cast person
            if (pillKey != Key(g)) Refresh(g);
            if (cam && host.Views != null)
                for (int k = 0; k < 4; k++)
                {
                    var t = host.Views.Find(Roles[k]); if (!t || !t.gameObject.activeSelf) continue;
                    var p = cam.WorldToScreenPoint(t.position + Vector3.up * 2.05f); if (p.z <= 0) continue;
                    var (a, b) = icons[k];
                    float w = 26 * s; var x = p.x - (b == null ? w / 2 : w + 1 * s); var y = Screen.height - p.y - w;
                    Icon(new Rect(x, y, w, w), a); if (b != null) Icon(new Rect(x + w + 2 * s, y, w, w), b);
                }

            // the price chip at the cursor
            var it = host.HoverIntent;
            if (it != null && host.Hover != null && host.Ending.Length == 0 && host.Panel == null)
            {
                if (it != chipFor || chipKey != txt.fontSize)
                {
                    chipFor = it; chipKey = txt.fontSize;
                    chipLine = !it.Actionable ? (it.Hint.Length > 0 ? it.Hint : it.Label) + (host.Hover.Kind == "crowd" ? "" : "   ·   right click: actions")
                        : $"{it.Label}   {it.Usd}   ·   {it.Time}   ·   {it.Trail}   ·   {it.Alert}{(it.Outcome.Length > 0 ? $"   ·   {it.Outcome}" : "")}{(host.Picked < 0 ? "   ·   right click: actions" : "")}";
                    chipSize = txt.CalcSize(new GUIContent(chipLine));
                }
                string line = chipLine; var size = chipSize;
                var r = new Rect(host.Mouse.x + 22 * s, host.Mouse.y + 6 * s, size.x + 24 * s, 34 * s);
                if (r.xMax > Screen.width) r.x = host.Mouse.x - r.width - 10 * s;
                Plate(r, it.Actionable); GUI.Label(new Rect(r.x + 12 * s, r.y, size.x, r.height), line, it.Actionable ? txt : small);
            }

            // top right pill: clock · next event · $ · Trail · Alert pips
            {
                var r = new Rect(Screen.width - 600 * s, 16 * s, 584 * s, 44 * s); Plate(r); hits.Add(r);
                float x = r.x + 14 * s;
                GUI.Label(new Rect(x, r.y, 90 * s, r.height), pillClock, big); x += 86 * s;
                GUI.Label(new Rect(x, r.y, 120 * s, r.height), pillNext, txt); x += 104 * s;
                GUI.Label(new Rect(x, r.y, 150 * s, r.height), pillCash, txt); x += 128 * s;
                GUI.Label(new Rect(x, r.y, 70 * s, r.height), pillTrail, txt); x += 64 * s;
                GUI.Label(new Rect(x, r.y, 24 * s, r.height), "A", txt); Pips(x + 20 * s, r.y + 16 * s, g.Alert, 3, 12 * s); x += 80 * s;
                GUI.Label(new Rect(x, r.y, 120 * s, r.height), pillState, txt);
                if (tracker.Length > 0) GUI.Label(new Rect(r.x + 4 * s, r.yMax + 4 * s, r.width, 22 * s), tracker, txtSmallBold);
                GUI.Label(new Rect(r.x + 4 * s, r.yMax + 26 * s, r.width, 18 * s), pillCity, small);
            }

            // roster: four slots (F-key, name, health pips, and what the agent is doing now: icon, label, progress and time left, the queued
            // act, an ✕ to cancel); the verb hotbar above it. Where they sit is one setting (controls.dock)
            var dock = host.controls.dock;
            float slotW = 214 * s, slotH = 98 * s, kw = 64 * s, kh = 50 * s;
            bool column = dock == RosterDock.Left;
            float rosterW = column ? slotW + 10 * s : slotW * 4 + 10 * s, rosterH = column ? slotH * 4 + 8 * s : slotH;
            var roster = dock switch
            {
                RosterDock.Left => new Rect(16 * s, Screen.height / 2f - rosterH / 2 - 60 * s, rosterW, rosterH),
                RosterDock.Corner => new Rect(16 * s, Screen.height - slotH - 16 * s, rosterW, rosterH),
                _ => new Rect(Screen.width / 2f - rosterW / 2 + 140 * s, Screen.height - slotH - 16 * s, rosterW, rosterH),
            };
            Plate(roster); hits.Add(roster);
            string tip = null; Rect tipAt = default;
            for (int i = 0; i < 4; i++)
            {
                var r = column ? new Rect(roster.x + 5 * s, roster.y + 4 * s + i * slotH, slotW - 4 * s, slotH - 8 * s) : new Rect(roster.x + 5 * s + i * slotW, roster.y + 4 * s, slotW - 4 * s, slotH - 8 * s);
                if (host.Selected[i]) GUI.DrawTexture(r, plateLit);
                var op = g.Ops[i]; var d = host.DoingOf(i);
                var x = new Rect(r.xMax - 26 * s, r.y + 28 * s, 22 * s, 22 * s); // the cancel ✕
                bool canX = d.CanCancel && host.Ending.Length == 0;
                if (canX && x.Contains(Event.current.mousePosition)) { tip = $"Cancel {d.CancelVerb} — {d.CancelText}"; tipAt = r; }
                if (canX && GUI.Button(x, GUIContent.none, GUIStyle.none)) host.CancelAgent(i, null);
                else if (GUI.Button(r, GUIContent.none, GUIStyle.none)) host.PortraitClick(i, Event.current.shift);
                GUI.Label(new Rect(r.x + 4 * s, r.y + 2 * s, 28 * s, 20 * s), host.AgentKeyLabel(i), key);
                GUI.Label(new Rect(r.x + 32 * s, r.y + 1 * s, 100 * s, 22 * s), Names[i], txtSmallBold);
                int hp = op.Gun.Health, hpMax = Mathf.Max(op.Gun.Max, op.Gun.Health);
                Pips(r.xMax - 6 * s - hpMax * 7 * s * 1.6f, r.y + 9 * s, hp, hpMax, 7 * s);
                // what they are doing
                var gl = new Rect(r.x + 4 * s, r.y + 28 * s, 34 * s, 22 * s);
                if (d.Busy) { GUI.DrawTexture(gl, plateLit); GUI.Label(gl, d.Phase is "walking" or "running" ? (d.Phase == "running" ? "»" : "›") : d.Glyph, iconSmall); }
                GUI.Label(new Rect(r.x + 42 * s, r.y + 28 * s, r.width - (canX ? 72 : 46) * s, 22 * s), d.Label, d.Busy ? smallBright : small);
                if (canX) { GUI.DrawTexture(x, x.Contains(Event.current.mousePosition) ? refusedPlate : plate); GUI.Label(x, "✕", mid); }
                if (d.Progress >= 0)
                {
                    var bar = new Rect(r.x + 6 * s, r.y + 56 * s, r.width - 58 * s, 6 * s);
                    GUI.DrawTexture(bar, barOff); GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * d.Progress / 1000f, bar.height), barOn);
                    GUI.Label(new Rect(bar.xMax + 4 * s, r.y + 49 * s, 46 * s, 18 * s), slotState[i], small);
                }
                string under = d.Queued.Length > 0 ? d.Queued : d.Busy && d.OnDone.Length > 0 ? "→ " + d.OnDone : "";
                if (under.Length > 0) GUI.Label(new Rect(r.x + 6 * s, r.y + 66 * s, r.width - 10 * s, 18 * s), under, small);
            }
            if (tip != null)
            {
                var size = smallBright.CalcSize(new GUIContent(tip)); size.x = Mathf.Min(size.x, 520 * s);
                var tr = new Rect(Mathf.Clamp(tipAt.x, 8 * s, Screen.width - size.x - 24 * s), tipAt.y - 34 * s, size.x + 20 * s, 26 * s);
                GUI.DrawTexture(tr, refusedPlate); GUI.Label(new Rect(tr.x + 10 * s, tr.y, tr.width, tr.height), tip, smallBright);
            }

            // the verb hotbar for the agent (keys 1–4), the lit key is what a right click gives
            var slots = HandoverHost.Slots[(int)host.Agent];
            var hb = column ? new Rect(roster.xMax + 10 * s, roster.y, kw * 4 + 12 * s, kh) : new Rect(roster.x + 6 * s, roster.y - kh - 10 * s, kw * 4 + 12 * s, kh);
            for (int i = 0; i < 4; i++)
            {
                var r = new Rect(hb.x + i * (kw + 4 * s), hb.y, kw, kh); hits.Add(r);
                bool has = i < slots.Length, lit = has && host.LitSlot == i;
                Plate(r, lit);
                if (has && GUI.Button(r, GUIContent.none, GUIStyle.none)) host.Pick(i);
                GUI.Label(new Rect(r.x, r.y + 2 * s, r.width, 26 * s), has ? slots[i].Glyph : "·", mid);
                GUI.Label(new Rect(r.x, r.yMax - 20 * s, r.width, 18 * s), host.VerbKey(i), key);
            }
            GUI.Label(new Rect(hb.xMax + 10 * s, hb.y, 420 * s, kh), $"{Names[(int)host.Agent]}: {(host.LitSlot >= 0 && host.LitSlot < slots.Length ? slots[host.LitSlot].Label : "")}", txt);

            // the dialogue log, docked beside the roster (bottom left)
            DialogueLog(dock == RosterDock.Corner ? new Rect(roster.xMax + 10 * s, 0, Mathf.Min(520 * s, Screen.width - roster.xMax - 640 * s), roster.yMax)
                : new Rect(16 * s, 0, column ? 520 * s : Mathf.Min(520 * s, roster.x - 32 * s), Screen.height - 16 * s));

            // the selection box being dragged
            if (host.BoxScreen is Rect box)
            {
                float b = Mathf.Max(1f, 1.5f * s);
                GUI.DrawTexture(new Rect(box.x, box.y, box.width, b), edge); GUI.DrawTexture(new Rect(box.x, box.yMax - b, box.width, b), edge);
                GUI.DrawTexture(new Rect(box.x, box.y, b, box.height), edge); GUI.DrawTexture(new Rect(box.xMax - b, box.y, b, box.height), edge);
            }

            // status line (top left)
            GUI.Label(new Rect(16 * s, 12 * s, Mathf.Max(400 * s, Screen.width - 660 * s), 22 * s), host.Status, small);

            Activities(cam);
            HoverRing(cam);
            Pulses(cam);
            if (host.Panel != null) PanelGui(g);
            Pops(cam);

            if (host.ShowBriefing && host.Ending.Length == 0) Briefing(g);
            if (host.Ending.Length > 0) EndCard(g);
        }

        /// <summary>Over each busy agent: the act's icon in a ring of progress dots; hovered, an ✕ that cancels (its consequence above). Walking
        /// to an act: the act's icon at the end of the walk line (HostViews draws the line).</summary>
        void Activities(Camera cam)
        {
            if (!cam || host.Views == null || host.Ending.Length > 0) return;
            var g = host.Game;
            for (int i = 0; i < 4; i++)
            {
                var d = host.DoingOf(i); if (!d.Busy || d.Phase == "down") continue;
                var t = host.Views.Find("op/" + Op.Name((OpId)i)); if (!t || !t.gameObject.activeSelf) continue;
                var p = cam.WorldToScreenPoint(t.position + Vector3.up * 2.35f); if (p.z <= 0) continue;
                var c = new Vector2(p.x, Screen.height - p.y); float rad = 17 * s, dot = 4 * s;
                var box = new Rect(c.x - rad - 4 * s, c.y - rad - 4 * s, 2 * rad + 8 * s, 2 * rad + 8 * s);
                bool walking = d.Phase is "walking" or "running";
                string glyph = walking ? (d.QueuedGlyph.Length > 0 ? d.QueuedGlyph : d.Phase == "running" ? "»" : "›") : d.Glyph;
                GUI.DrawTexture(new Rect(c.x - 13 * s, c.y - 11 * s, 26 * s, 22 * s), host.Selected[i] ? plateLit : plate);
                GUI.Label(new Rect(c.x - 20 * s, c.y - 11 * s, 40 * s, 22 * s), glyph, iconSmall);
                int on = d.Progress < 0 ? -1 : Mathf.RoundToInt(d.Progress * 24 / 1000f);
                for (int k = 0; k < 24; k++)
                {
                    float a = Mathf.PI / 2 - k * Mathf.PI * 2 / 24; // clockwise from the top
                    var dr = new Rect(c.x + rad * Mathf.Cos(a) - dot / 2, c.y - rad * Mathf.Sin(a) - dot / 2, dot, dot);
                    GUI.DrawTexture(dr, on < 0 ? (k % 2 == 0 ? pipOn : pipOff) : k < on ? pipOn : pipOff);
                }
                if (d.CanCancel && box.Contains(Event.current.mousePosition))
                {
                    var x = new Rect(box.xMax - 6 * s, box.y - 6 * s, 20 * s, 20 * s); hits.Add(box);
                    GUI.DrawTexture(x, refusedPlate); GUI.Label(x, "✕", iconSmall);
                    if (GUI.Button(box, GUIContent.none, GUIStyle.none)) host.CancelAgent(i, null);
                    var tip = $"Cancel {d.CancelVerb} — {d.CancelText}"; var size = smallBright.CalcSize(new GUIContent(tip));
                    var tr = new Rect(c.x - size.x / 2 - 8 * s, box.y - 32 * s, size.x + 16 * s, 24 * s);
                    GUI.DrawTexture(tr, refusedPlate); GUI.Label(new Rect(tr.x + 8 * s, tr.y, tr.width, tr.height), tip, smallBright);
                }
                if (d.ActAt.HasValue && walking && d.QueuedGlyph.Length > 0)
                {
                    var e = cam.WorldToScreenPoint(d.ActAt.Value + Vector3.up * 0.3f); if (e.z <= 0) continue;
                    var er = new Rect(e.x - 16 * s, Screen.height - e.y - 12 * s, 32 * s, 24 * s);
                    GUI.DrawTexture(er, plateLit); GUI.Label(er, d.QueuedGlyph, iconSmall);
                }
            }
        }

        /// <summary>The dialogue log: collapsed, its header and the last line; open (L, or click the header), the last nine. Each line has its
        /// speaker's chip (an agent's number, else the first letter); refusals red-brown.</summary>
        void DialogueLog(Rect area)
        {
            var talk = host.Talk; int show = host.LogOpen ? Mathf.Min(9, talk.Count) : Mathf.Min(1, talk.Count);
            float w = area.width, head = 24 * s, pad = 6 * s, chip = 22 * s; if (w < 200 * s) return;
            float textW = w - chip - 3 * pad;
            float h = head + pad;
            var heights = new float[show];
            for (int i = 0; i < show; i++) { var l = talk[talk.Count - show + i]; heights[i] = Mathf.Max(chip, wrapBright.CalcHeight(new GUIContent(Line(l)), textW)) + 4 * s; h += heights[i]; }
            if (show == 0) h += 24 * s;
            var r = new Rect(area.x, area.yMax - h, w, h); Plate(r); hits.Add(r);
            var hr = new Rect(r.x, r.y, w, head);
            if (GUI.Button(hr, GUIContent.none, GUIStyle.none)) host.LogOpen = !host.LogOpen;
            GUI.Label(new Rect(r.x + pad, r.y, w, head), $"{(host.LogOpen ? "▾" : "▸")} Dialogue  ({host.controls.Label(PlayAction.DialogueLog)})", small);
            float y = r.y + head;
            if (show == 0) GUI.Label(new Rect(r.x + pad, y, w - 2 * pad, 22 * s), "Orders, replies and what the cell learns appear here.", small);
            for (int i = 0; i < show; i++)
            {
                var l = talk[talk.Count - show + i];
                var cr = new Rect(r.x + pad, y + 1 * s, chip, chip);
                GUI.DrawTexture(cr, l.Agent >= 0 ? chipAgent : plateLit);
                GUI.Label(cr, l.Agent >= 0 ? $"<color=#1a2a28>{Nums[l.Agent]}</color>" : l.Name.Length > 0 ? l.Name[..1] : "·", iconSmall);
                GUI.Label(new Rect(cr.xMax + pad, y, textW, heights[i]), Line(l), l.Kind == HandoverHost.Feedback.Refused ? wrapRefused : wrapBright);
                y += heights[i];
            }
        }

        static string Line(HandoverHost.Said l) => $"<b>{HandoverHost.MissionClock(l.Tick)}</b>  {(l.Name.Length > 0 ? l.Name + ": " : "")}{l.Text}";

        /// <summary>The briefing card (open at the start; B): the job, objective, win, every way it fails, the clock and its close, the purse,
        /// the two known ways in as hints, and what each agent does.</summary>
        void Briefing(Game g)
        {
            var t = host.Text; if (t == null) return;
            float w = 860 * s, pad = 22 * s, iw = w - 2 * pad;
            var parts = new List<(string text, GUIStyle st)>
            {
                (t.Title.ToUpperInvariant(), big),
                (string.Join(" ", t.Card), wrapBright),
                ("<b>Objective</b>   " + t.Objective, wrapBright),
                ("<b>Win</b>   " + t.Win, wrapBright),
                ("<b>Fail if</b>   " + string.Join("  ·  ", t.Fails.FindAll(f => f.code is not ("engine-error" or "withdraw")).ConvertAll(f => f.line.Split(new[] { ". " }, System.StringSplitOptions.None)[0].TrimEnd('.'))), wrapBright),
                ("<b>Clock</b>   " + t.ClockLine, wrapBright),
                ("<b>Purse</b>   " + t.PurseLine, wrapBright),
                ("<b>Two ways in</b>", wrapBright),
            };
            foreach (var hnt in t.Hints) parts.Add(("   ▸ " + hnt, wrapBright));
            parts.Add(("<b>The cell</b>", wrapBright));
            foreach (var k in new[] { "scout", "fixer", "tech", "enforcer" }) if (t.Cell.TryGetValue(k, out var cl)) parts.Add(("   " + cl, smallWrap));
            var c = host.controls;
            parts.Add(($"{c.Label(PlayAction.Pause)}: start the clock   ·   {c.Label(PlayAction.Briefing)}: this briefing   ·   Esc: close   ·   right click a person or thing for actions   ·   text: {t.Source}", small));
            float h = 2 * pad; var hs = new float[parts.Count];
            for (int i = 0; i < parts.Count; i++) { hs[i] = parts[i].st.wordWrap ? parts[i].st.CalcHeight(new GUIContent(parts[i].text), iw) : 30 * s; hs[i] += 6 * s; h += hs[i]; }
            var r = new Rect(Screen.width / 2f - w / 2, Mathf.Max(70 * s, Screen.height / 2f - h / 2 - 70 * s), w, h); Plate(r); hits.Add(r);
            float y = r.y + pad;
            for (int i = 0; i < parts.Count; i++) { GUI.Label(new Rect(r.x + pad, y, iw, hs[i]), parts[i].text, parts[i].st); y += hs[i]; }
        }

        /// <summary>A ring at the feet of what the mouse is on, when the cell can act on it, and the action cursor; nothing on scenery or passers-by.</summary>
        void HoverRing(Camera cam)
        {
            var t = host.Hover; bool act = t != null && t.Kind != "crowd" && t.Kind != "op" && host.Ending.Length == 0;
            if (act != cursorIsAct) { Cursor.SetCursor(act ? cursorAct : null, act ? new Vector2(16, 16) : Vector2.zero, CursorMode.Auto); cursorIsAct = act; }
            if (!act || !cam) return;
            var w = host.TargetWorld(t); if (!w.HasValue) return;
            var p = cam.WorldToScreenPoint(w.Value); if (p.z <= 0) return;
            float rw = 64 * s * 14f / cam.orthographicSize, rh = rw * 0.62f; // the ground circle at pitch 38
            GUI.DrawTexture(new Rect(p.x - rw / 2, Screen.height - p.y - rh / 2, rw, rh), ring);
        }

        /// <summary>A taken order or a landing pulses on its target: a ring that grows and fades over 0.7 s.</summary>
        void Pulses(Camera cam)
        {
            if (!cam) return; float now = Time.unscaledTime;
            foreach (var (w, kind, born) in host.Pulses)
            {
                float a = (now - born) / 0.7f; if (a < 0 || a > 1) continue;
                var p = cam.WorldToScreenPoint(w); if (p.z <= 0) continue;
                float rw = (50 + 90 * a) * s * 14f / cam.orthographicSize, rh = rw * 0.62f;
                var c = GUI.color; GUI.color = new Color(kind == HandoverHost.Feedback.Refused ? 1f : 0.85f, kind == HandoverHost.Feedback.Refused ? 0.55f : 1f, 0.85f, 1f - a);
                GUI.DrawTexture(new Rect(p.x - rw / 2, Screen.height - p.y - rh / 2, rw, rh), ring); GUI.color = c;
            }
        }

        /// <summary>Pops: the answer to a click, at the cursor or over its target, for about 2.5 s (refusals red-brown, taken green, info teal).</summary>
        void Pops(Camera cam)
        {
            float now = Time.unscaledTime; float y0 = 0f;
            for (int i = host.Pops.Count - 1; i >= 0; i--)
            {
                var pp = host.Pops[i]; if (now > pp.Until) continue;
                Vector2 at;
                if (pp.World.HasValue && cam) { var p = cam.WorldToScreenPoint(pp.World.Value + Vector3.up * 2.6f); if (p.z <= 0) continue; at = new Vector2(p.x, Screen.height - p.y); }
                else if (pp.Screen.HasValue) at = pp.Screen.Value + new Vector2(18 * s, -40 * s);
                else continue;
                var size = small.CalcSize(new GUIContent(pp.Text)); size.x = Mathf.Min(size.x, 420 * s);
                var r = new Rect(Mathf.Clamp(at.x - size.x / 2, 8, Screen.width - size.x - 24 * s), at.y - y0, size.x + 16 * s, 24 * s);
                float fade = Mathf.Clamp01((pp.Until - now) / 0.4f);
                var c = GUI.color; GUI.color = new Color(1, 1, 1, fade);
                GUI.DrawTexture(r, pp.Kind == HandoverHost.Feedback.Refused ? refusedPlate : pp.Kind == HandoverHost.Feedback.Ok ? okPlate : plate);
                GUI.Label(new Rect(r.x + 8 * s, r.y, r.width - 12 * s, r.height), pp.Text, smallBright);
                GUI.color = c;
                if (!pp.World.HasValue) y0 += 26 * s;
            }
        }

        /// <summary>The action panel at the cursor, ~410 px at 1080p: one small card per action the selected agents have on the target. Each card:
        /// who and the action in plain words ("F2 Fixer · Pay Inez"), one line on what it does and its trade-off (Fable's text), the walk and
        /// the price ($ · time · Trail · Alert · outcome); a locked card is dimmed, says why, and what unlocks it. Click a card to give it (to
        /// that card's agent); hover one to see the walk.</summary>
        void PanelGui(Game g)
        {
            var pn = host.Panel; float w = 410 * s, head = 30 * s, pad = 10 * s, iw = w - 2 * pad - 8 * s;
            if (cardsFor != pn || cardsFont != wrapBright.fontSize)
            {
                cardsFor = pn; cardsFont = wrapBright.fontSize; cards.Clear();
                foreach (var row in pn.Rows)
                {
                    var it = row.I;
                    string title = $"{Names[(int)row.By]} · {row.Verb}";
                    string price = it.Move ? it.Time : $"{(it.Walk.Length > 0 ? it.Walk + " " : "")}{it.Usd} · {it.Time} · {it.Trail} · {it.Alert}{(it.Outcome.Length > 0 ? $" · {it.Outcome}" : "")}";
                    string lockLine = row.Available ? "" : $"Locked: {row.Reason}{(row.Unlock.Length > 0 ? $"\n{row.Unlock}" : "")}";
                    float h = 24 * s + (it.Does.Length > 0 ? wrapBright.CalcHeight(new GUIContent(it.Does), iw) : 0) + smallWrap.CalcHeight(new GUIContent(price), iw)
                        + (lockLine.Length > 0 ? wrapRefused.CalcHeight(new GUIContent(lockLine), iw) : 0) + 10 * s;
                    cards.Add((title, it.Does, price, lockLine, h));
                }
            }
            float aboutH = pn.About.Length > 0 ? smallWrap.CalcHeight(new GUIContent(pn.About), w - 2 * pad) + 4 * s : 0;
            float total = head + aboutH + (pn.Note.Length > 0 ? 44 * s : 0) + 6 * s; foreach (var cd in cards) total += cd.h + 4 * s;
            var r = new Rect(Mathf.Min(pn.Gui.x + 12 * s, Screen.width - w - 8 * s), Mathf.Clamp(pn.Gui.y - 12 * s, 8 * s, Mathf.Max(8 * s, Screen.height - total - 8 * s)), w, total);
            Plate(r); hits.Add(r);
            GUI.Label(new Rect(r.x + pad, r.y + 2 * s, w - 2 * pad, head), pn.T.Label, txt);
            float y = r.y + head;
            if (aboutH > 0) { GUI.Label(new Rect(r.x + pad, y - 4 * s, w - 2 * pad, aboutH), pn.About, smallWrap); y += aboutH; }
            if (pn.Note.Length > 0) { GUI.Label(new Rect(r.x + pad, y, w - 2 * pad, 42 * s), pn.Note, smallWrap); y += 44 * s; }
            int hover = -1;
            for (int i = 0; i < pn.Rows.Count && i < cards.Count; i++)
            {
                var row = pn.Rows[i]; var cd = cards[i]; var rr = new Rect(r.x + 4 * s, y, w - 8 * s, cd.h);
                bool mine = host.Selected[(int)row.By];
                GUI.DrawTexture(rr, rr.Contains(Event.current.mousePosition) ? plateLit : plate);
                if (rr.Contains(Event.current.mousePosition)) hover = i;
                GUI.DrawTexture(new Rect(rr.x, rr.y, 3 * s, rr.height), row.Available ? edge : refusedText);
                if (GUI.Button(rr, GUIContent.none, GUIStyle.none)) host.PanelDo(i);
                var c = GUI.color; if (!row.Available) GUI.color = new Color(1, 1, 1, 0.62f);
                float yy = rr.y + 3 * s;
                GUI.Label(new Rect(rr.x + 8 * s, yy, 30 * s, 20 * s), host.AgentKeyLabel((int)row.By), key);
                GUI.Label(new Rect(rr.x + 40 * s, yy, iw - 40 * s, 20 * s), cd.title, txtSmallBold);
                if (row.Slot >= 0 && mine) GUI.Label(new Rect(rr.xMax - 34 * s, yy, 30 * s, 20 * s), $"[{host.VerbKey(row.Slot)}]", key);
                yy += 22 * s;
                if (cd.does.Length > 0) { float dh = wrapBright.CalcHeight(new GUIContent(cd.does), iw); GUI.Label(new Rect(rr.x + pad, yy, iw, dh), cd.does, wrapBright); yy += dh; }
                float ph = smallWrap.CalcHeight(new GUIContent(cd.price), iw); GUI.Label(new Rect(rr.x + pad, yy, iw, ph), cd.price, smallWrap); yy += ph;
                GUI.color = c;
                if (cd.lockLine.Length > 0) GUI.Label(new Rect(rr.x + pad, yy, iw, rr.yMax - yy), cd.lockLine, wrapRefused);
                y += cd.h + 4 * s;
            }
            if (Event.current.type == EventType.Repaint) host.SetPanelHover(hover);
        }

        [System.NonSerialized] HandoverHost.ActionPanel cardsFor; [System.NonSerialized] int cardsFont;
        [System.NonSerialized] readonly List<(string title, string does, string price, string lockLine, float h)> cards = new();

        /// <summary>Two icons at most: what the cell knows of them, and their state toward the cell.</summary>
        static (string a, string b) Icons(Game g, string role)
        {
            var p = g.PersonByRole(role);
            if (role == "councillor") return ("★", null);
            if (role == "guard") return (g.GuardAtDoor ? "◆door" : "◆", p.Turned ? "✓" : null);
            string known = p.KTemperament ? p.Temperament[..1].ToUpperInvariant() : "?";
            string need = p.Turned ? "✓" : p.KNeed ? "$" : p.Suspected.Length > 0 ? "$?" : null;
            return (known, need);
        }

        void Icon(Rect r, string text)
        {
            GUI.DrawTexture(r, plate); GUI.Label(r, text, text.Length > 2 ? iconSmall : icon);
        }

        /// <summary>What the pill, the roster states and the cast icons show changes at most once a tick, on an order, or with pause and speed.</summary>
        int Key(Game g) => unchecked(g.Tick * 31 + g.Orders.Count * 7919 + (host.Paused ? 1 : 0) + host.Speed * 3 + g.Log.Count * 104729);

        void Refresh(Game g)
        {
            pillKey = Key(g);
            pillClock = HandoverHost.MissionClock(g.Tick); pillNext = "▶▶ " + HandoverHost.MissionClock(host.NextMark());
            pillCash = $"${g.Cash:N0}"; pillTrail = $"T {g.Trail}"; pillState = host.Paused ? "<color=#ffd27a>PAUSE</color>" : $"x{host.Speed}";
            var c = host.controls;
            pillCity = $"{host.CityClock()} · {c.Label(PlayAction.Pause)} pause · {c.Label(PlayAction.Briefing)} briefing · {c.Label(PlayAction.DialogueLog)} log · {c.Label(PlayAction.Cancel)} cancel · N next";
            tracker = host.Text == null ? "" : "▸ " + host.Text.Tracker.Replace("{close}", host.Text.Close);
            for (int i = 0; i < 4; i++)
            {
                var op = g.Ops[i];
                var d = host.DoingOf(i);
                slotState[i] = d.TicksLeft >= 0 && d.Busy ? $"{(d.TicksLeft + Game.Hz - 1) / Game.Hz} s" : "";
                icons[i] = Icons(g, Roles[i]);
            }
        }

        void EndCard(Game g)
        {
            var d = host.EndDebrief;
            var lines = new List<string>
            {
                g.Result == Outcome.Success ? "VALE IS IN THE CAR" : g.Result == Outcome.Withdrawn ? "WITHDRAWN" : g.FailCode == "vale-leaves" || g.FailCode == "rivals-reach-vale" ? "THE CLOSE" : "FAILED",
                g.FailText.Length > 0 ? g.FailText : g.Result == Outcome.Success ? "The crew escapes with Vale alive." : "",
                $"time {HandoverHost.MissionClock(g.Tick)}   ·   spent ${g.S.Purse - g.Cash:N0} of ${g.S.Purse:N0}   ·   Trail {g.Trail}   ·   Alert {g.Alert}   ·   shots {g.Gunfire}",
            };
            if (d != null)
            {
                lines.Add($"security: {d.PublicSecurity}   ·   investigation: {d.InvestigationState}{(d.InvestigationCause.Length > 0 ? $" ({d.InvestigationCause})" : "")}");
                if (d.Route.Count > 0) lines.Add("route: " + string.Join(", ", d.Route));
                if (d.FightBranch.Length > 0) lines.Add($"final fight: {d.FightBranch}, {d.FightHostiles} hostiles, {d.FightHits} hits taken");
                if (d.Assets.Count > 0) lines.Add("contacts kept: " + string.Join(", ", d.Assets));
            }
            lines.Add(host.Ending);
            lines.Add("Return: play again   ·   the session is saved in out/play/ and replays exactly");
            float w = 1100 * s, lh = 34 * s, h = 30 * s + lh * lines.Count;
            var r = new Rect(Screen.width / 2f - w / 2, Screen.height / 2f - h / 2 - 60 * s, w, h); Plate(r); hits.Add(r);
            for (int i = 0; i < lines.Count; i++) GUI.Label(new Rect(r.x + 24 * s, r.y + 14 * s + i * lh, w - 48 * s, lh), lines[i], i == 0 ? big : i == lines.Count - 1 || i == lines.Count - 2 ? small : txt);
        }
    }
}
