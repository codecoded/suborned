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
        [System.NonSerialized] GUIStyle txt, big, small, key, mid, smallC, icon, iconSmall, smallBright, txtSmallBold, smallWrap; float s;
        // per-frame strings are rebuilt only when what they show changes (Sky's freeze review: the HUD allocated ~1,800 blocks a frame)
        [System.NonSerialized] string pillClock = "", pillNext = "", pillCash = "", pillTrail = "", pillState = "", pillCity = "", chipLine = "";
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
            }
            txt.fontSize = Mathf.RoundToInt(18 * s); big.fontSize = Mathf.RoundToInt(24 * s); small.fontSize = Mathf.RoundToInt(13 * s); key.fontSize = Mathf.RoundToInt(13 * s); mid.fontSize = Mathf.RoundToInt(15 * s);
            smallC.fontSize = small.fontSize; smallBright.fontSize = Mathf.RoundToInt(14 * s); smallWrap.fontSize = Mathf.RoundToInt(14 * s); txtSmallBold.fontSize = Mathf.RoundToInt(15 * s); icon.fontSize = Mathf.RoundToInt(14 * s); iconSmall.fontSize = Mathf.RoundToInt(10 * s);
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
                    var op = g.Ops[i]; string verb = op.Busy != null ? Glyph(op.Busy.Do, op.Busy.Approach) : op.Walk != null ? (op.Running ? "»" : "›") : "✓";
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
                GUI.Label(new Rect(r.x, r.yMax + 2 * s, r.width, 18 * s), pillCity, small);
            }

            // roster: four slots (F-key, portrait, health pips, state) and the verb hotbar beside it; where they sit is one setting (controls.dock)
            var dock = host.controls.dock;
            float slotW = 84 * s, slotH = 74 * s, kw = 64 * s, kh = 50 * s;
            bool column = dock == RosterDock.Left;
            float rosterW = column ? slotW + 10 * s : slotW * 4 + 10 * s, rosterH = column ? slotH * 4 + 8 * s : slotH;
            var roster = dock switch
            {
                RosterDock.Left => new Rect(16 * s, Screen.height / 2f - rosterH / 2, rosterW, rosterH),
                RosterDock.Corner => new Rect(16 * s, Screen.height - slotH - 48 * s, rosterW, rosterH),
                _ => new Rect(Screen.width / 2f - rosterW / 2, Screen.height - slotH - 16 * s, rosterW, rosterH),
            };
            Plate(roster); hits.Add(roster);
            for (int i = 0; i < 4; i++)
            {
                var r = column ? new Rect(roster.x + 5 * s, roster.y + 4 * s + i * slotH, slotW - 4 * s, slotH - 8 * s) : new Rect(roster.x + 5 * s + i * slotW, roster.y + 4 * s, slotW - 4 * s, slotH - 8 * s);
                if (host.Selected[i]) GUI.DrawTexture(r, plateLit);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) host.PortraitClick(i, Event.current.shift);
                var op = g.Ops[i];
                GUI.Label(new Rect(r.x + 4 * s, r.y, 24 * s, 18 * s), host.AgentKeyLabel(i), key);
                GUI.Label(new Rect(r.x, r.y + 12 * s, r.width, 30 * s), Names[i], mid);
                int hp = i == 3 ? g.EnforcerHealth : 4, hpMax = i == 3 ? Mathf.Max(g.EnforcerHealth, g.R.EnforcerHealth) : 4;
                Pips(r.x + 10 * s, r.y + 44 * s, hp, hpMax, 7 * s);
                GUI.Label(new Rect(r.x, r.y + 50 * s, r.width, 16 * s), slotState[i], smallC);
            }

            // the verb hotbar for the agent (keys 1–4), the lit key is what a right click gives
            var slots = HandoverHost.Slots[(int)host.Agent];
            var bar = column ? new Rect(roster.xMax + 10 * s, roster.y, kw * 4 + 12 * s, kh) : new Rect(roster.x + 6 * s, roster.y - kh - 10 * s, kw * 4 + 12 * s, kh);
            for (int i = 0; i < 4; i++)
            {
                var r = new Rect(bar.x + i * (kw + 4 * s), bar.y, kw, kh); hits.Add(r);
                bool has = i < slots.Length, lit = has && host.LitSlot == i;
                Plate(r, lit);
                if (has && GUI.Button(r, GUIContent.none, GUIStyle.none)) host.Pick(i);
                GUI.Label(new Rect(r.x, r.y + 2 * s, r.width, 26 * s), has ? slots[i].Glyph : "·", mid);
                GUI.Label(new Rect(r.x, r.yMax - 20 * s, r.width, 18 * s), host.VerbKey(i), key);
            }
            GUI.Label(new Rect(bar.xMax + 10 * s, bar.y, 420 * s, kh), $"{Names[(int)host.Agent]}: {(host.LitSlot >= 0 && host.LitSlot < slots.Length ? slots[host.LitSlot].Label : "")}", txt);

            // the selection box being dragged
            if (host.BoxScreen is Rect box)
            {
                float b = Mathf.Max(1f, 1.5f * s);
                GUI.DrawTexture(new Rect(box.x, box.y, box.width, b), edge); GUI.DrawTexture(new Rect(box.x, box.yMax - b, box.width, b), edge);
                GUI.DrawTexture(new Rect(box.x, box.y, b, box.height), edge); GUI.DrawTexture(new Rect(box.xMax - b, box.y, b, box.height), edge);
            }

            // status line
            GUI.Label(new Rect(16 * s, Screen.height - 34 * s, Mathf.Max(400 * s, Screen.width / 2f - 360 * s), 24 * s), host.Status, small);

            HoverRing(cam);
            Pulses(cam);
            if (host.Panel != null) PanelGui(g);
            Pops(cam);

            if (host.Ending.Length > 0) EndCard(g);
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

        /// <summary>The action panel at the cursor (≤ 280 px at 1080p): every action on the target, by agent, the selected agent's first; available
        /// rows first, locked rows dimmed with their reason; click a row to give it (to that row's agent); hover a row to see the walk.</summary>
        void PanelGui(Game g)
        {
            var pn = host.Panel; float w = 380 * s, head = 26 * s, rh = 56 * s;
            float h = head + (pn.Note.Length > 0 ? 42 * s : 0) + rh * pn.Rows.Count + 6 * s;
            var r = new Rect(Mathf.Min(pn.Gui.x + 12 * s, Screen.width - w - 8 * s), Mathf.Clamp(pn.Gui.y - 12 * s, 8 * s, Screen.height - h - 8 * s), w, h);
            Plate(r); hits.Add(r);
            GUI.Label(new Rect(r.x + 10 * s, r.y + 2 * s, w - 20 * s, head), pn.T.Label, txt);
            float y = r.y + head;
            if (pn.Note.Length > 0) { GUI.Label(new Rect(r.x + 10 * s, y, w - 20 * s, 40 * s), pn.Note, smallWrap); y += 42 * s; }
            int hover = -1;
            for (int i = 0; i < pn.Rows.Count; i++)
            {
                var row = pn.Rows[i]; var rr = new Rect(r.x + 4 * s, y + i * rh, w - 8 * s, rh - 2 * s);
                bool mine = host.Selected[(int)row.By];
                if (rr.Contains(Event.current.mousePosition)) { hover = i; GUI.DrawTexture(rr, plateLit); }
                else if (mine) GUI.DrawTexture(new Rect(rr.x, rr.y, 3 * s, rr.height), edge);
                if (GUI.Button(rr, GUIContent.none, GUIStyle.none)) host.PanelDo(i);
                var c = GUI.color; if (!row.Available) GUI.color = new Color(1, 1, 1, 0.5f);
                GUI.Label(new Rect(rr.x + 8 * s, rr.y + 1 * s, 30 * s, 20 * s), host.AgentKeyLabel((int)row.By), key);
                GUI.Label(new Rect(rr.x + 38 * s, rr.y, rr.width - 44 * s, 20 * s), $"{Names[(int)row.By]} · {row.Verb}{(row.Slot >= 0 && mine ? $"  [{host.VerbKey(row.Slot)}]" : "")}", txtSmallBold);
                var it = row.I;
                string line = !row.Available ? row.Reason : it.Move ? it.Time : $"{(it.Walk.Length > 0 ? it.Walk + " " : "")}{it.Usd} · {it.Time} · {it.Trail} · {it.Alert}{(it.Outcome.Length > 0 ? $" · {it.Outcome}" : "")}";
                GUI.Label(new Rect(rr.x + 38 * s, rr.y + 18 * s, rr.width - 44 * s, 36 * s), line, smallWrap);
                GUI.color = c;
            }
            if (Event.current.type == EventType.Repaint) host.SetPanelHover(hover);
        }

        static string Glyph(string verb, string approach) => verb switch
        {
            "watch" => "eye", "listen" => "ear", "turn" => approach == "pay" ? "$" : approach, "call" => "call", "loop" => "loop", "reroute" => "route",
            "mislead" => "tip", "enter" => "in", "take" => "take", "extract" => "car", "fire" => "fire", _ => verb,
        };

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
            pillCity = $"{host.CityClock()} · Space pause · = − speed · N next event · F12 debug";
            for (int i = 0; i < 4; i++)
            {
                var op = g.Ops[i];
                slotState[i] = op.Inside ? "inside" : op.Busy != null ? $"{Glyph(op.Busy.Do, op.Busy.Approach)} {HandoverHost.MissionClock(op.BusyEnd)}" : op.Walk != null ? (op.Running ? "running" : "walking") : "ready";
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
