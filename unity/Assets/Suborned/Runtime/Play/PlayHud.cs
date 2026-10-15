using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using Camera = UnityEngine.Camera;
using Event = UnityEngine.Event;

namespace Suborned.Play
{
    /// <summary>The IMGUI view of the HUD model (HudModel): the fallback and debug view once the UI Toolkit view lands. The play HUD at round 4's v10 "signage hotbar" fidelity (Sky, 2026-10-15; briefs/011/ui/round4/v10-signage-hotbar*.jpg), simple and
    /// placeholder-iconed: the roster of four slots at the bottom (number, portrait, health pips, state), the 1–4 verb hotbar above it with the
    /// verb a right click gives lit, the top-right pill (mission clock, next event, $, Trail, Alert pips), number and verb markers on the pavement
    /// under the cell, at most two icons over a cast person, the price chip at the cursor ($ · time · Trail · Alert) before a right click commits
    /// (§02), and the end card with the debrief numbers. Transit-signage enamel: deep teal plates, cream type, thin brass-free borders. Plain
    /// IMGUI on its own component, reading HandoverHost only, so Glyph's art can replace it whole. Laid out at 1080p, scaled.</summary>
    [RequireComponent(typeof(HandoverHost))]
    public sealed class PlayHud : MonoBehaviour
    {
        // never kept over a domain reload (a kept style with its textures gone would draw nothing): built on the first OnGUI
        [System.NonSerialized] HandoverHost host; [System.NonSerialized] Texture2D plate, plateLit, edge, pipOn, pipOff, white, ring, refusedPlate, okPlate, cursorAct, cursorAim, cursorNoLine;
        [System.NonSerialized] int cursorIs; // 0 default, 1 act, 2 crosshair, 3 crosshair with no line
        [System.NonSerialized] GUIStyle txt, big, small, key, mid, smallC, icon, iconSmall, smallBright, txtSmallBold, smallWrap, wrapBright, wrapBold, wrapRefused; float s;
        [System.NonSerialized] Texture2D barOn, barOff, chipAgent, refusedText;
        // the model this view draws (HudModel: plain data the host fills; the UI Toolkit view will read the same); clicks call the host
        [System.NonSerialized] readonly HudModel m = new();
        public HudModel Model => m;
        [System.NonSerialized] string chipLine = ""; [System.NonSerialized] int chipKey; [System.NonSerialized] Vector2 chipSize;
        static readonly string[] Nums = { "1", "2", "3", "4" };
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

        /// <summary>The crosshair cursor (placeholder for Glyph's): a ring with four ticks and a gap at the centre.</summary>
        static Texture2D Crosshair(int n, Color c)
        {
            var t = Ring(n, 2, c);
            for (int i = 0; i < n; i++)
            {
                bool tick = (i < n / 2 - 4 || i > n / 2 + 3) && (i < 4 || i > n - 5 || (i > n / 2 - 11 && i < n / 2 + 10));
                if (!tick) continue;
                t.SetPixel(i, n / 2, c); t.SetPixel(i, n / 2 - 1, c); t.SetPixel(n / 2, i, c); t.SetPixel(n / 2 - 1, i, c);
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
                cursorAim = Crosshair(32, Cream); cursorNoLine = Crosshair(32, new Color(1f, 0.6f, 0.5f));
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
            if (Event.current.type == EventType.Layout) { hits.Clear(); m.Frame(host); } // the model, once a frame
            var cam = host.View; bool kit = HudToolkit.Active; // the kit view draws the pill, roster, strips, verb plate, ticker and feed
            if (m.Fault != null) { var fr = new Rect(20 * s, 20 * s, 1000 * s, 50 * s); Plate(fr); GUI.Label(new Rect(fr.x + 14 * s, fr.y, fr.width, fr.height), $"<color=#ff9a8a>{m.Fault}</color>", txt); return; }

            // markers on the pavement under the cell: the agent's number and what it is doing
            if (cam)
                for (int i = 0; i < 4; i++)
                {
                    var mk = m.Marks[i]; if (!mk.World.HasValue) continue;
                    var p = cam.WorldToScreenPoint(mk.World.Value + Vector3.down * 0.15f); if (p.z <= 0) continue;
                    var r = new Rect(p.x - 26 * s, Screen.height - p.y + 2 * s, 52 * s, 20 * s);
                    if (mk.Selected) GUI.DrawTexture(r, plateLit);
                    GUI.Label(new Rect(r.x + 4 * s, r.y, 18 * s, r.height), mk.Num, mid);
                    GUI.Label(new Rect(r.x + 20 * s, r.y, 32 * s, r.height), mk.Verb, mid);
                }

            // at most two icons over a cast person
            if (cam)
                for (int k = 0; k < 4; k++)
                {
                    var ci = m.Cast[k]; if (!ci.World.HasValue) continue;
                    var p = cam.WorldToScreenPoint(ci.World.Value + Vector3.up * 2.05f); if (p.z <= 0) continue;
                    float w = 26 * s; var x = p.x - (ci.B == null ? w / 2 : w + 1 * s); var y = Screen.height - p.y - w;
                    Icon(new Rect(x, y, w, w), ci.A); if (ci.B != null) Icon(new Rect(x + w + 2 * s, y, w, w), ci.B);
                }

            // the chip at the cursor: the crosshair's or the walk-or-run chip, else the price
            if (m.Chip != HudModel.ChipKind.None)
            {
                if (m.ChipText != chipLine || chipKey != txt.fontSize) { chipLine = m.ChipText; chipKey = txt.fontSize; chipSize = txt.CalcSize(new GUIContent(chipLine)); }
                var r = new Rect(m.Mouse.x + 22 * s, m.Mouse.y + 6 * s, chipSize.x + 24 * s, 34 * s);
                if (r.xMax > Screen.width) r.x = m.Mouse.x - r.width - 10 * s;
                switch (m.Chip)
                {
                    case HudModel.ChipKind.Price: Plate(r, true); break;
                    case HudModel.ChipKind.Hint: Plate(r, false); break;
                    default: GUI.DrawTexture(r, m.Chip == HudModel.ChipKind.AimWarn ? refusedPlate : plate); break;
                }
                var st = m.Chip is HudModel.ChipKind.Price or HudModel.ChipKind.Aim or HudModel.ChipKind.AimWarn ? txt : m.Chip == HudModel.ChipKind.Move ? smallBright : small;
                GUI.Label(new Rect(r.x + 12 * s, r.y, chipSize.x, r.height), chipLine, st);
            }

            // top right pill: clock · next on the timeline · $ · Trail · Alert pips
            if (!kit)
            {
                var r = new Rect(Screen.width - 760 * s, 16 * s, 744 * s, 44 * s); Plate(r); hits.Add(r);
                float x = r.x + 14 * s;
                GUI.Label(new Rect(x, r.y, 90 * s, r.height), m.Clock, big); x += 86 * s;
                GUI.Label(new Rect(x, r.y, 270 * s, r.height), m.Next, txtSmallBold); x += 264 * s;
                GUI.Label(new Rect(x, r.y, 150 * s, r.height), m.Cash, txt); x += 128 * s;
                GUI.Label(new Rect(x, r.y, 70 * s, r.height), m.Trail, txt); x += 64 * s;
                GUI.Label(new Rect(x, r.y, 24 * s, r.height), "A", txt); Pips(x + 20 * s, r.y + 16 * s, m.Alert, 3, 12 * s); x += 80 * s;
                GUI.Label(new Rect(x, r.y, 120 * s, r.height), m.State, txt);
                if (m.Tracker.Length > 0) GUI.Label(new Rect(r.x + 4 * s, r.yMax + 4 * s, r.width, 22 * s), m.Tracker, txtSmallBold);
                GUI.Label(new Rect(r.x + 4 * s, r.yMax + 26 * s, r.width, 18 * s), m.City, small);
            }

            // roster: four slots (F-key, name, health pips, and what the agent is doing now: icon, label, progress and time left, the queued
            // act, an ✕ to cancel); the verb hotbar above it. Where they sit is one setting (controls.dock)
            if (kit) { KitRest(cam); return; }
            var dock = m.Dock;
            float slotW = 214 * s, slotH = 98 * s, kw = 64 * s, kh = 50 * s;
            bool column = dock == RosterDock.Left;
            float rosterW = column ? slotW + 10 * s : slotW * 4 + 10 * s, rosterH = column ? slotH * 4 + 8 * s : slotH;
            var roster = dock switch
            {
                RosterDock.Left => new Rect(16 * s, Screen.height / 2f - rosterH / 2 - 60 * s, rosterW, rosterH),
                RosterDock.Corner => new Rect(16 * s, Screen.height - slotH - 16 * s, rosterW, rosterH),
                _ => new Rect(Screen.width / 2f - rosterW / 2, Screen.height - slotH - 16 * s, rosterW, rosterH),
            };
            Plate(roster); hits.Add(roster);
            string tip = null; Rect tipAt = default;
            for (int i = 0; i < 4; i++)
            {
                var r = column ? new Rect(roster.x + 5 * s, roster.y + 4 * s + i * slotH, slotW - 4 * s, slotH - 8 * s) : new Rect(roster.x + 5 * s + i * slotW, roster.y + 4 * s, slotW - 4 * s, slotH - 8 * s);
                var sl = m.Slots[i];
                if (sl.Selected) GUI.DrawTexture(r, plateLit);
                if (sl.HintRing) // the hint rings this portrait
                {
                    float b = 3 * s; var cc = GUI.color; GUI.color = new Color(1f, 0.85f, 0.45f, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 5f));
                    GUI.DrawTexture(new Rect(r.x, r.y, r.width, b), white); GUI.DrawTexture(new Rect(r.x, r.yMax - b, r.width, b), white);
                    GUI.DrawTexture(new Rect(r.x, r.y, b, r.height), white); GUI.DrawTexture(new Rect(r.xMax - b, r.y, b, r.height), white); GUI.color = cc;
                }
                var x = new Rect(r.xMax - 26 * s, r.y + 28 * s, 22 * s, 22 * s); // the cancel ✕
                bool canX = sl.CanCancel;
                if (canX && x.Contains(Event.current.mousePosition)) { tip = sl.CancelTip; tipAt = r; }
                if (canX && GUI.Button(x, GUIContent.none, GUIStyle.none)) host.CancelAgent(i, null);
                else if (GUI.Button(r, GUIContent.none, GUIStyle.none)) host.PortraitClick(i, Event.current.shift);
                GUI.Label(new Rect(r.x + 4 * s, r.y + 2 * s, 28 * s, 20 * s), sl.Key, key);
                if (sl.Armed) GUI.Label(new Rect(r.x + 2 * s, r.y + 15 * s, 32 * s, 14 * s), $"{(sl.Drawn ? "•" : "")}{sl.Stance}", iconSmall); // stance; a dot when drawn
                GUI.Label(new Rect(r.x + 32 * s, r.y + 1 * s, 100 * s, 22 * s), sl.Name, txtSmallBold);
                Pips(r.xMax - 6 * s - sl.HpMax * 7 * s * 1.6f, r.y + 9 * s, sl.Hp, sl.HpMax, 7 * s);
                // what they are doing
                var gl = new Rect(r.x + 4 * s, r.y + 28 * s, 34 * s, 22 * s);
                if (sl.Busy) { GUI.DrawTexture(gl, plateLit); GUI.Label(gl, sl.Glyph, iconSmall); }
                GUI.Label(new Rect(r.x + 42 * s, r.y + 28 * s, r.width - (canX ? 72 : 46) * s, 22 * s), sl.Label, sl.Busy ? smallBright : small);
                if (canX) { GUI.DrawTexture(x, x.Contains(Event.current.mousePosition) ? refusedPlate : plate); GUI.Label(x, "✕", mid); }
                if (sl.Progress >= 0)
                {
                    var bar = new Rect(r.x + 6 * s, r.y + 56 * s, r.width - 58 * s, 6 * s);
                    GUI.DrawTexture(bar, barOff); GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * sl.Progress / 1000f, bar.height), barOn);
                    GUI.Label(new Rect(bar.xMax + 4 * s, r.y + 49 * s, 46 * s, 18 * s), sl.Left, small);
                }
                if (sl.Down) { var cc = GUI.color; GUI.color = new Color(0, 0, 0, 0.45f); GUI.DrawTexture(r, white); GUI.color = cc; }
                if (sl.Under.Length > 0) GUI.Label(new Rect(r.x + 6 * s, r.y + 66 * s, r.width - 10 * s, 18 * s), sl.Under, small);
            }
            if (tip != null)
            {
                var size = smallBright.CalcSize(new GUIContent(tip)); size.x = Mathf.Min(size.x, 520 * s);
                var tr = new Rect(Mathf.Clamp(tipAt.x, 8 * s, Screen.width - size.x - 24 * s), tipAt.y - 34 * s, size.x + 20 * s, 26 * s);
                GUI.DrawTexture(tr, refusedPlate); GUI.Label(new Rect(tr.x + 10 * s, tr.y, tr.width, tr.height), tip, smallBright);
            }

            // the verb hotbar for the agent (keys 1–4), the lit key is what a right click gives
            var hb = column ? new Rect(roster.xMax + 10 * s, roster.y, kw * 4 + 12 * s, kh) : new Rect(roster.x + 6 * s, roster.y - kh - 10 * s, kw * 4 + 12 * s, kh);
            for (int i = 0; i < 4; i++)
            {
                var r = new Rect(hb.x + i * (kw + 4 * s), hb.y, kw, kh); hits.Add(r);
                bool has = m.VerbHas[i], lit = has && m.VerbLit == i;
                Plate(r, lit);
                if (has && GUI.Button(r, GUIContent.none, GUIStyle.none)) host.Pick(i);
                GUI.Label(new Rect(r.x, r.y + 2 * s, r.width, 26 * s), m.VerbGlyph[i], mid);
                GUI.Label(new Rect(r.x, r.yMax - 20 * s, r.width, 18 * s), m.VerbKey[i], key);
            }
            GUI.Label(new Rect(hb.xMax + 10 * s, hb.y, 420 * s, kh), m.VerbCaption, txt);

            // the scene feed (bottom right; lifted above the roster when the roster reaches the corner)
            {
                float fw = 360 * s; float fx = Screen.width - fw - 16 * s; float fb = Screen.height - 16 * s;
                if (roster.xMax + 10 * s > fx && dock != RosterDock.Left) fb = roster.y - (column ? 0 : kh + 20 * s);
                FeedGui(new Rect(fx, 0, fw, fb));
            }

            // the ticker (Glyph round 6b: the old dialogue log's place): the cell's last word, one line, fading after a few seconds
            Ticker(column ? new Rect(roster.x, roster.yMax + 8 * s, roster.width + 240 * s, 26 * s) : new Rect(roster.x, hb.y - 34 * s, Mathf.Min(620 * s, roster.width), 26 * s));

            // the selection box being dragged
            if (m.Box is Rect box)
            {
                float b = Mathf.Max(1f, 1.5f * s);
                GUI.DrawTexture(new Rect(box.x, box.y, box.width, b), edge); GUI.DrawTexture(new Rect(box.x, box.yMax - b, box.width, b), edge);
                GUI.DrawTexture(new Rect(box.x, box.y, b, box.height), edge); GUI.DrawTexture(new Rect(box.xMax - b, box.y, b, box.height), edge);
            }

            // status line (top left)
            GUI.Label(new Rect(16 * s, 12 * s, Mathf.Max(400 * s, Screen.width - 820 * s), 22 * s), m.Status, small);

            Activities(cam);
            FightMarks(cam);
            HoverRing(cam);
            Pulses(cam);
            if (m.PanelOpen) PanelGui();
            if (m.LoudOpen) LoudCardGui();
            Pops(cam);

            HintGui(cam);
            if (m.BriefingOpen) Briefing();
            if (m.Ended) EndCard();
        }

        /// <summary>With the kit view on: what it does not draw yet (steps 3 and 4) — the world marks, the chips, the cards, the hint, the
        /// history (L) above the kit's feed bar, and the ring in the street for the feed card under the mouse.</summary>
        void KitRest(Camera cam)
        {
            if (m.HistoryOpen) History(new Rect(Screen.width - (14 + 384) * s, 0, 384 * s, Screen.height - (14 + 32 + 6) * s));
            if (m.Box is Rect box)
            {
                float b = Mathf.Max(1f, 1.5f * s);
                GUI.DrawTexture(new Rect(box.x, box.y, box.width, b), edge); GUI.DrawTexture(new Rect(box.x, box.yMax - b, box.width, b), edge);
                GUI.DrawTexture(new Rect(box.x, box.y, b, box.height), edge); GUI.DrawTexture(new Rect(box.xMax - b, box.y, b, box.height), edge);
            }
            if (cam && Event.current.type == EventType.Repaint && HudToolkit.HoverPlace is Vector3 at)
            {
                var p = cam.WorldToScreenPoint(at); if (p.z > 0) { float rw = 80 * s * 14f / cam.orthographicSize, rh = rw * 0.62f; GUI.DrawTexture(new Rect(p.x - rw / 2, Screen.height - p.y - rh / 2, rw, rh), ring); }
            }
            Activities(cam); FightMarks(cam); HoverRing(cam); Pulses(cam);
            if (m.PanelOpen) PanelGui();
            if (m.LoudOpen) LoudCardGui();
            Pops(cam); HintGui(cam);
            if (m.BriefingOpen) Briefing();
            if (m.Ended) EndCard();
        }

        /// <summary>Over each busy agent: the act's icon in a ring of progress dots; hovered, an ✕ that cancels (its consequence above). Walking
        /// to an act: the act's icon at the end of the walk line (HostViews draws the line).</summary>
        void Activities(Camera cam)
        {
            if (!cam) return;
            for (int i = 0; i < 4; i++)
            {
                var rg = m.Rings[i]; if (!rg.Show) continue;
                var p = cam.WorldToScreenPoint(rg.World + Vector3.up * 2.35f); if (p.z <= 0) continue;
                var c = new Vector2(p.x, Screen.height - p.y); float rad = 17 * s, dot = 4 * s;
                var box = new Rect(c.x - rad - 4 * s, c.y - rad - 4 * s, 2 * rad + 8 * s, 2 * rad + 8 * s);
                GUI.DrawTexture(new Rect(c.x - 13 * s, c.y - 11 * s, 26 * s, 22 * s), rg.Selected ? plateLit : plate);
                GUI.Label(new Rect(c.x - 20 * s, c.y - 11 * s, 40 * s, 22 * s), rg.Glyph, iconSmall);
                int on = rg.Progress < 0 ? -1 : Mathf.RoundToInt(rg.Progress * 24 / 1000f);
                for (int k = 0; k < 24; k++)
                {
                    float a = Mathf.PI / 2 - k * Mathf.PI * 2 / 24; // clockwise from the top
                    var dr = new Rect(c.x + rad * Mathf.Cos(a) - dot / 2, c.y - rad * Mathf.Sin(a) - dot / 2, dot, dot);
                    GUI.DrawTexture(dr, on < 0 ? (k % 2 == 0 ? pipOn : pipOff) : k < on ? pipOn : pipOff);
                }
                if (rg.CanCancel && box.Contains(Event.current.mousePosition))
                {
                    var x = new Rect(box.xMax - 6 * s, box.y - 6 * s, 20 * s, 20 * s); hits.Add(box);
                    GUI.DrawTexture(x, refusedPlate); GUI.Label(x, "✕", iconSmall);
                    if (GUI.Button(box, GUIContent.none, GUIStyle.none)) host.CancelAgent(i, null);
                    var size = smallBright.CalcSize(new GUIContent(rg.Tip));
                    var tr = new Rect(c.x - size.x / 2 - 8 * s, box.y - 32 * s, size.x + 16 * s, 24 * s);
                    GUI.DrawTexture(tr, refusedPlate); GUI.Label(new Rect(tr.x + 8 * s, tr.y, tr.width, tr.height), rg.Tip, smallBright);
                }
                if (rg.ActAt.HasValue)
                {
                    var e = cam.WorldToScreenPoint(rg.ActAt.Value + Vector3.up * 0.3f); if (e.z <= 0) continue;
                    var er = new Rect(e.x - 16 * s, Screen.height - e.y - 12 * s, 32 * s, 24 * s);
                    GUI.DrawTexture(er, plateLit); GUI.Label(er, rg.ActGlyph, iconSmall);
                }
            }
        }

        /// <summary>The hint (Glyph round 6 frame 02's place: top centre, "NEXT STEP", an ✕, H): its line, and a ring pulsing on its target in
        /// the street. Below it, a toast (the run-seen line) for a few seconds.</summary>
        void HintGui(Camera cam)
        {
            float w = 620 * s, pad = 12 * s, y = 70 * s;
            if (m.Hint != null)
            {
                float th = wrapBright.CalcHeight(new GUIContent(m.Hint), w - 2 * pad - 40 * s);
                var r = new Rect(Screen.width / 2f - w / 2, y, w, th + 34 * s); Plate(r); hits.Add(r);
                GUI.Label(new Rect(r.x + pad, r.y + 4 * s, 200 * s, 18 * s), "NEXT STEP", small);
                GUI.Label(new Rect(r.x + pad, r.y + 24 * s, w - 2 * pad - 40 * s, th), m.Hint, wrapBright);
                var x = new Rect(r.xMax - 30 * s, r.y + 6 * s, 22 * s, 22 * s); GUI.DrawTexture(x, plateLit); GUI.Label(x, "✕", mid);
                if (GUI.Button(x, GUIContent.none, GUIStyle.none)) host.DismissHint();
                GUI.Label(new Rect(r.xMax - 30 * s, r.y + 30 * s, 22 * s, 16 * s), m.HintKey, key);
                y = r.yMax + 8 * s;
                if (cam && m.HintRing.HasValue)
                {
                    var p = cam.WorldToScreenPoint(m.HintRing.Value); if (p.z > 0)
                    {
                        float k = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f), rw = 90 * s * k * 14f / cam.orthographicSize, rh = rw * 0.62f;
                        var cc = GUI.color; GUI.color = new Color(1f, 0.85f, 0.45f, 0.95f);
                        GUI.DrawTexture(new Rect(p.x - rw / 2, Screen.height - p.y - rh / 2, rw, rh), ring); GUI.color = cc;
                    }
                }
            }
            if (m.Toast != null)
            {
                var size = smallBright.CalcSize(new GUIContent(m.Toast));
                var r = new Rect(Screen.width / 2f - size.x / 2 - 12 * s, y, size.x + 24 * s, 28 * s);
                GUI.DrawTexture(r, refusedPlate); GUI.Label(new Rect(r.x + 12 * s, r.y, r.width, r.height), m.Toast, smallBright);
            }
        }

        /// <summary>In a fight: health pips over everyone fighting (the cell when a fight is on, drawn or hurt; anyone who has fired or aims;
        /// the fight's hostiles), and a DOWN plate with the carry hint over a downed agent.</summary>
        void FightMarks(Camera cam)
        {
            if (!cam || Event.current.type != EventType.Repaint) return;
            foreach (var f in m.Fighters)
            {
                var p = cam.WorldToScreenPoint(f.World + Vector3.up * 2.05f); if (p.z <= 0) continue;
                float d = 5 * s; float width = f.Max * d * 1.6f;
                var bg = new Rect(p.x - width / 2 - 3 * s, Screen.height - p.y - 3 * s, width + 4 * s, d + 6 * s);
                GUI.DrawTexture(bg, f.Cell ? plate : refusedPlate);
                Pips(bg.x + 3 * s, bg.y + 3 * s, f.Hp, f.Max, d);
            }
            foreach (var dn in m.Down)
            {
                var p = cam.WorldToScreenPoint(dn.World + Vector3.up * 1.0f); if (p.z <= 0) continue;
                var size = smallBright.CalcSize(new GUIContent(dn.Line));
                var r = new Rect(p.x - size.x / 2 - 8 * s, Screen.height - p.y - 12 * s, size.x + 16 * s, 24 * s);
                GUI.DrawTexture(r, refusedPlate); GUI.Label(new Rect(r.x + 8 * s, r.y, r.width, r.height), dn.Line, smallBright);
            }
        }

        /// <summary>The going-loud card at the cursor (motive-fights.md §2.6): what this order sets off, read from the engine (Game.GoingLoud),
        /// with the commit ring that fills while the same input is held. The clock keeps running.</summary>
        void LoudCardGui()
        {
            float w = 440 * s, pad = 12 * s; var lines = m.LoudLines;
            float h = pad * 2; var hs = new float[lines.Count];
            for (int i = 0; i < lines.Count; i++) { hs[i] = wrapBright.CalcHeight(new GUIContent(lines[i]), w - 2 * pad - 50 * s) + 2 * s; h += hs[i]; }
            var r = new Rect(Mathf.Min(m.LoudAt.x + 24 * s, Screen.width - w - 8 * s), Mathf.Clamp(m.LoudAt.y - h - 16 * s, 8 * s, Screen.height - h - 8 * s), w, h);
            GUI.DrawTexture(r, refusedPlate); hits.Add(r);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && r.Contains(Event.current.mousePosition)) { host.GateHoldByCard(); Event.current.Use(); }
            float y = r.y + pad;
            for (int i = 0; i < lines.Count; i++) { GUI.Label(new Rect(r.x + pad, y, w - 2 * pad - 50 * s, hs[i]), lines[i], i == 0 ? wrapBold : wrapBright); y += hs[i]; }
            // the commit ring: 24 dots, filled as the hold runs
            var cc = new Vector2(r.xMax - 30 * s, r.y + 30 * s); float rad = 16 * s, dot = 4 * s; int on = Mathf.RoundToInt(m.LoudProgress * 24);
            for (int k = 0; k < 24; k++)
            {
                float a = Mathf.PI / 2 - k * Mathf.PI * 2 / 24;
                GUI.DrawTexture(new Rect(cc.x + rad * Mathf.Cos(a) - dot / 2, cc.y - rad * Mathf.Sin(a) - dot / 2, dot, dot), k < on ? pipOn : pipOff);
            }
        }

        // Glyph's round 6b feed colours (feed6.py, ΔE-checked against the signals): header tints by kind (cream text), history accent bars,
        // the result and live footers, the NEW pill, the tick button, the card ivory
        static readonly Dictionary<string, Color> KindTint = new()
        {
            ["learned"] = Hex(0x1f6c67), ["approach"] = Hex(0x244c48), ["call"] = Hex(0x306064), ["city"] = Hex(0x607064), ["warning"] = Hex(0x283436), ["fight"] = Hex(0x0e1a1b),
        };
        static readonly Dictionary<string, Color> KindAccent = new()
        {
            ["learned"] = Hex(0x65b2ad), ["approach"] = Hex(0x6a928e), ["call"] = Hex(0x76a6aa), ["city"] = Hex(0xa6b6aa), ["warning"] = Hex(0x6e7a7c), ["fight"] = Hex(0x546061),
        };
        static readonly Color CardIvory = Hex(0xfce4cc), FooterSea = Hex(0xbae2d6), FooterLine = Hex(0x629e92), LiveFooter = Hex(0xd6deda), LiveLine = Hex(0x96aaa6),
            Ink = Hex(0x17302f), Stripe = Hex(0xece9e0), NewPill = Hex(0x1cb9c6), CheckDisc = Hex(0x1f6c67);
        static Color Hex(int rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        [System.NonSerialized] readonly Dictionary<string, Texture2D> tints = new();
        Texture2D TintTex(string kind) { if (!tints.TryGetValue(kind, out var t) || !t) tints[kind] = t = Tex(KindTint.TryGetValue(kind, out var c) ? c : KindTint["city"]); return t; }
        [System.NonSerialized] Texture2D ivoryTex, footerTex, liveTex, pillTex, discTex, stripeTex;
        [System.NonSerialized] readonly Dictionary<string, Texture2D> accents = new();
        Texture2D AccentTex(string kind) { if (!accents.TryGetValue(kind, out var t) || !t) accents[kind] = t = Tex(KindAccent.TryGetValue(kind, out var c) ? c : KindAccent["city"]); return t; }
        void Outline(Rect r, Color c, float px) { var cc = GUI.color; GUI.color = c; GUI.DrawTexture(new Rect(r.x, r.y, r.width, px), white); GUI.DrawTexture(new Rect(r.x, r.yMax - px, r.width, px), white); GUI.DrawTexture(new Rect(r.x, r.y, px, r.height), white); GUI.DrawTexture(new Rect(r.xMax - px, r.y, px, r.height), white); GUI.color = cc; } [System.NonSerialized] GUIStyle inkWrap, inkBold, inkSmall, headTxt;

        void FeedStyles()
        {
            if (ivoryTex == null) { ivoryTex = Tex(CardIvory); footerTex = Tex(FooterSea); liveTex = Tex(LiveFooter); pillTex = Tex(NewPill); discTex = Tex(CheckDisc); stripeTex = Tex(Stripe); }
            if (inkWrap == null)
            {
                inkWrap = new GUIStyle(smallWrap) { normal = { textColor = Ink }, richText = true };
                inkBold = new GUIStyle(inkWrap) { fontStyle = FontStyle.Bold, wordWrap = false };
                inkSmall = new GUIStyle(inkWrap) { normal = { textColor = new Color(0.32f, 0.39f, 0.37f) }, wordWrap = false };
                headTxt = new GUIStyle(txtSmallBold) { normal = { textColor = Cream }, clipping = TextClipping.Clip };
            }
            inkWrap.fontSize = Mathf.RoundToInt(14 * s); inkBold.fontSize = Mathf.RoundToInt(14 * s); inkSmall.fontSize = Mathf.RoundToInt(13 * s); headTxt.fontSize = Mathf.RoundToInt(15 * s);
        }

        /// <summary>A speaker chip (placeholder for Forma's portraits and Glyph's badges): the cell's number on cream; Inez a cup, the guard an
        /// amber shield, the driver a wheel, Vale a star, a rival a violet diamond, a watcher a camera, anyone else a plain figure.</summary>
        void Chip(Rect r, string speaker)
        {
            int k = System.Array.IndexOf(new[] { "scout", "fixer", "tech", "enforcer" }, speaker);
            if (k >= 0) { GUI.DrawTexture(r, chipAgent); GUI.Label(r, $"<color=#1a2a28>{Nums[k]}</color>", iconSmall); return; }
            var (bg, glyph) = speaker switch
            {
                "waitress" => (Hex(0x8fb9b0), "☕"), "guard" => (Hex(0xf0a020), "◆"), "driver" => (Hex(0x8fb9b0), "◎"), "councillor" => (Hex(0x8fb9b0), "★"),
                "rivals" => (Hex(0x8a4fd0), "◆"), "watcher" => (Hex(0x8fb9b0), "▣"), "patrol" or "security" => (Hex(0xf0a020), "◆"), _ => (Hex(0xb9c4bf), "•"),
            };
            var c = GUI.color; GUI.color = bg; GUI.DrawTexture(r, white); GUI.color = c; GUI.Label(r, $"<color=#17302f>{glyph}</color>", iconSmall);
        }

        /// <summary>The scene feed (Glyph's round 6b structure, placeholder art): at most three cards above the bar, newest on top and open, the
        /// other two folded to header and footer, "+N more" for the rest; in a fight every card folds to its header (hover opens it). A card:
        /// a header in its kind's tint (kind and verb icons, title, where, clock, NEW), its lines with speaker chips (a "…" for the next speaker
        /// while it plays), and the highlighted result footer with the tick (✓ K). A click on the body pans to its place; the tick acknowledges.
        /// The bar: "SCENES n new", the keys, and the history (L) with unread first, Read all and filter chips.</summary>
        void FeedGui(Rect area)
        {
            FeedStyles();
            float w = area.width, pad = 8 * s, head = 26 * s, chip = 18 * s; var feed = m.Feed; bool fight = m.FeedFold;
            // the bar
            var bar = new Rect(area.x, area.yMax - 26 * s, w, 26 * s); Plate(bar); hits.Add(bar);
            int unread = m.Unread;
            GUI.Label(new Rect(bar.x + pad, bar.y, 90 * s, bar.height), "SCENES", txtSmallBold);
            if (unread > 0) { var nb = new Rect(bar.x + 78 * s, bar.y + 4 * s, 60 * s, 18 * s); GUI.DrawTexture(nb, ivoryTex); GUI.Label(nb, $"<color=#17302f>{unread} new</color>", smallC); }
            GUI.Label(new Rect(bar.xMax - 190 * s, bar.y, 182 * s, bar.height), $"{m.AckKey} ack · {m.HistoryKey} history {(m.HistoryOpen ? "▾" : "▴")}", small);
            if (GUI.Button(new Rect(bar.xMax - 110 * s, bar.y, 110 * s, bar.height), GUIContent.none, GUIStyle.none)) host.FeedHistoryOpen = !host.FeedHistoryOpen;
            float y = bar.y - 6 * s;
            if (m.HistoryOpen) { History(new Rect(area.x, 0, w, y)); return; }
            if (feed.Count > 3) { var mr = new Rect(area.xMax - 90 * s, y - 20 * s, 90 * s, 20 * s); Plate(mr); GUI.Label(mr, $"+{feed.Count - 3} more", smallC); hits.Add(mr); y = mr.y - 4 * s; }
            // oldest of the three nearest the bar, newest on top
            if (Event.current.type == EventType.Repaint) { hoverCard = hoverNow; hoverNow = -1; }
            int first = Mathf.Max(0, feed.Count - 3);
            for (int i = first; i < feed.Count; i++)
            {
                var c = feed[i]; bool newest = i == feed.Count - 1;
                float iw = w - 2 * pad - chip - 6 * s;
                // fold: the older two show header and footer; in a fight every card shows only its header unless hovered
                bool open = fight ? c.Id == hoverCard : newest; // in a fight, cards fold to their header; hovering one opens it
                var shown = new List<HandoverHost.FeedLine>();
                for (int j = 0; j < Mathf.Min(c.Revealed, c.Lines.Count); j++) shown.Add(c.Lines[j]);
                float linesH = 0; if (open) foreach (var l in shown) linesH += Mathf.Max(chip, inkWrap.CalcHeight(new GUIContent($"<b>{l.Name}</b>  {l.Text}"), iw)) + 3 * s;
                if (open && c.Typing) linesH += chip;
                bool footer = (c.Live || c.Footer.Length > 0) && (!fight || open || newest);
                float bodyH = (open ? linesH + 6 * s : 0) + (c.Live && c.Progress >= 0 ? 10 * s : 0) + (footer ? 26 * s : 0);
                var r = new Rect(area.x, y - head - bodyH, w, head + bodyH);
                if (Event.current.type == EventType.Repaint) { if (r.Contains(Event.current.mousePosition)) hoverNow = c.Id; }
                y = r.y - 6 * s;
                hits.Add(r);
                // header
                var hr = new Rect(r.x, r.y, w, head); GUI.DrawTexture(hr, TintTex(c.Kind));
                if (c.Kind == "warning") for (float sx = hr.x; sx < hr.xMax; sx += 14 * s) { GUI.DrawTexture(new Rect(sx, hr.y, 6 * s, 2 * s), stripeTex); GUI.DrawTexture(new Rect(sx, hr.yMax - 2 * s, 6 * s, 2 * s), stripeTex); }
                float x = hr.x + pad;
                GUI.Label(new Rect(x, hr.y, 22 * s, head), c.Icon, iconSmall); x += 24 * s;
                var title = new GUIContent(c.Title); float tw = headTxt.CalcSize(title).x;
                GUI.Label(new Rect(x, hr.y, Mathf.Min(tw, w * 0.5f), head), title, headTxt); x += Mathf.Min(tw, w * 0.5f) + 8 * s;
                GUI.Label(new Rect(x, hr.y + 1 * s, hr.xMax - x - 100 * s, head), c.Where, small);
                bool isNew = !c.Live && !c.Read;
                if (isNew) { var nr = new Rect(hr.xMax - 98 * s, hr.y + 5 * s, 38 * s, 16 * s); GUI.DrawTexture(nr, pillTex); GUI.Label(nr, "<color=#17302f>NEW</color>", iconSmall); }
                GUI.Label(new Rect(hr.xMax - 54 * s, hr.y, 50 * s, head), HandoverHost.MissionClock(c.Started), txtSmallBold);
                if (r.height <= head + 0.5f) { HoverPan(c, r); continue; }
                // body
                var br = new Rect(r.x, hr.yMax, w, r.height - head); GUI.DrawTexture(br, ivoryTex);
                float yy = br.y + 3 * s;
                if (c.Live && c.Progress >= 0)
                {
                    var pb = new Rect(br.x + pad, yy + 2 * s, w - 2 * pad - 50 * s, 4 * s); GUI.DrawTexture(pb, barOff); var cc = GUI.color; GUI.color = CheckDisc; GUI.DrawTexture(new Rect(pb.x, pb.y, pb.width * c.Progress / 1000f, pb.height), white); GUI.color = cc;
                    if (c.TicksLeft >= 0) GUI.Label(new Rect(pb.xMax + 4 * s, yy - 6 * s, 46 * s, 16 * s), $"{(c.TicksLeft + Game.Hz - 1) / Game.Hz} s", inkSmall);
                    yy += 10 * s;
                }
                if (open)
                {
                    foreach (var l in shown)
                    {
                        var content = new GUIContent($"<b>{l.Name}</b>  {l.Text}"); float lh = Mathf.Max(chip, inkWrap.CalcHeight(content, iw));
                        Chip(new Rect(br.x + pad, yy, chip, chip), l.Speaker);
                        GUI.Label(new Rect(br.x + pad + chip + 6 * s, yy, iw, lh), content, inkWrap); yy += lh + 3 * s;
                    }
                    if (c.Typing && c.Revealed < c.Lines.Count) { Chip(new Rect(br.x + pad, yy, chip, chip), c.Lines[c.Revealed].Speaker); GUI.Label(new Rect(br.x + pad + chip + 6 * s, yy, iw, chip), "…", inkBold); yy += chip; }
                }
                if (footer)
                {
                    var fr = new Rect(br.x + 4 * s, br.yMax - 26 * s, w - 8 * s, 22 * s);
                    if (c.Live) { GUI.DrawTexture(fr, liveTex); Outline(fr, LiveLine, Mathf.Max(1f, s)); GUI.Label(new Rect(fr.x + 8 * s, fr.y, fr.width - 16 * s, fr.height), "The result lands when the act finishes.", inkSmall); }
                    else
                    {
                        GUI.DrawTexture(fr, footerTex); Outline(fr, FooterLine, Mathf.Max(1f, 1.2f * s));
                        var disc = new Rect(fr.x + 5 * s, fr.y + 4 * s, 14 * s, 14 * s); GUI.DrawTexture(disc, discTex); GUI.Label(disc, $"<color=#f7f4ec>{(c.Tone == HandoverHost.Feedback.Refused ? "✕" : "✓")}</color>", iconSmall);
                        GUI.Label(new Rect(fr.x + 24 * s, fr.y, fr.width - 88 * s, fr.height), c.Footer, inkBold);
                        var tick = new Rect(fr.xMax - 58 * s, fr.y + 2 * s, 54 * s, 18 * s); GUI.DrawTexture(tick, white); Outline(tick, CheckDisc, Mathf.Max(1f, 1.6f * s)); GUI.Label(tick, $"<color=#17302f>✓ {m.AckKey}</color>", iconSmall);
                        if (!c.Read) { var dot = new Rect(tick.xMax - 4 * s, tick.y - 4 * s, 8 * s, 8 * s); GUI.DrawTexture(dot, white); GUI.DrawTexture(new Rect(dot.x + 2 * s, dot.y + 2 * s, 4 * s, 4 * s), pillTex); }
                        if (GUI.Button(tick, GUIContent.none, GUIStyle.none)) { host.Acknowledge(c, false); continue; }
                    }
                }
                HoverPan(c, r);
            }
        }

        [System.NonSerialized] int hoverCard = -1, hoverNow = -1;

        /// <summary>Hovering a card rings its place in the street; a click on it pans there.</summary>
        void HoverPan(HandoverHost.FeedCard c, Rect r)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            if (hover && Event.current.type == EventType.Repaint && host.View && host.PlaceNow(c) is Vector3 at)
            {
                var p = host.View.WorldToScreenPoint(at); if (p.z > 0) { float rw = 80 * s * 14f / host.View.orthographicSize, rh = rw * 0.62f; GUI.DrawTexture(new Rect(p.x - rw / 2, Screen.height - p.y - rh / 2, rw, rh), ring); }
            }
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) host.Acknowledge(c, true);
        }

        /// <summary>The history (L): the unread first with Read all, then the acknowledged, newest first; filter chips All, Scenes, Cell (the cell's
        /// own chatter, the old dialogue log's lines). Each line pans to its place.</summary>
        void History(Rect area)
        {
            float w = area.width, pad = 8 * s, lh = 20 * s; var rows = m.History;
            float h = 30 * s + rows.Count * lh + 8 * s;
            var r = new Rect(area.x, area.yMax - h, w, h); GUI.DrawTexture(r, ivoryTex); hits.Add(r);
            float x = r.x + pad;
            foreach (var f in new[] { "all", "scenes", "cell" })
            {
                var fr = new Rect(x, r.y + 5 * s, 64 * s, 20 * s); GUI.DrawTexture(fr, m.Filter == f ? plateLit : plate); GUI.Label(fr, f == "all" ? "All" : f == "scenes" ? "Scenes" : "Cell", smallC);
                if (GUI.Button(fr, GUIContent.none, GUIStyle.none)) host.FeedFilter = f; x += 70 * s;
            }
            var ra = new Rect(r.xMax - 84 * s, r.y + 5 * s, 78 * s, 20 * s); GUI.DrawTexture(ra, plate); GUI.Label(ra, "Read all", smallC);
            if (GUI.Button(ra, GUIContent.none, GUIStyle.none)) host.ReadAll();
            float y = r.y + 30 * s;
            foreach (var (text, card, unread) in rows)
            {
                var lr = new Rect(r.x + pad, y, w - 2 * pad, lh);
                if (card != null) GUI.DrawTexture(new Rect(r.x + 2 * s, y + 3 * s, 4 * s, lh - 6 * s), AccentTex(card.Kind));
                GUI.Label(lr, text, unread ? inkBold : inkSmall);
                if (card != null && GUI.Button(lr, GUIContent.none, GUIStyle.none) && host.PlaceNow(card) is Vector3 at) host.LookAt(at);
                y += lh;
            }
        }

        /// <summary>The ticker: the cell's last word (an order taken, an arrival) for four seconds, then it fades. One line, no stack, no tick.</summary>
        void Ticker(Rect r)
        {
            var last = m.Ticker; if (last == null) return;
            float age = m.TickerAge; if (age > 5f) return;
            var c = GUI.color; GUI.color = new Color(1, 1, 1, Mathf.Clamp01((5f - age) / 1f));
            string line = $"{last.Name}: {last.Text}"; var size = smallBright.CalcSize(new GUIContent(line));
            var rr = new Rect(r.x, r.y, Mathf.Min(r.width, size.x + 40 * s), r.height); GUI.DrawTexture(rr, last.Kind == HandoverHost.Feedback.Refused ? refusedPlate : plate);
            Chip(new Rect(rr.x + 4 * s, rr.y + 4 * s, 18 * s, 18 * s), last.Speaker);
            GUI.Label(new Rect(rr.x + 28 * s, rr.y, rr.width - 32 * s, rr.height), line, smallBright);
            GUI.color = c;
        }

        /// <summary>The briefing card (open at the start; B): the job, objective, win, every way it fails, the clock and its close, the purse,
        /// the two known ways in as hints, and what each agent does.</summary>
        void Briefing()
        {
            if (m.Briefing.Count == 0) return;
            float w = 860 * s, pad = 22 * s, iw = w - 2 * pad;
            var parts = new List<(string text, GUIStyle st)>(m.Briefing.Count);
            foreach (var (text, part) in m.Briefing) parts.Add((text, part switch { HudModel.Part.Title => big, HudModel.Part.Small => smallWrap, HudModel.Part.Footer => small, _ => wrapBright }));
            float h = 2 * pad; var hs = new float[parts.Count];
            for (int i = 0; i < parts.Count; i++) { hs[i] = parts[i].st.wordWrap ? parts[i].st.CalcHeight(new GUIContent(parts[i].text), iw) : 30 * s; hs[i] += 6 * s; h += hs[i]; }
            var r = new Rect(Screen.width / 2f - w / 2, Mathf.Max(70 * s, Screen.height / 2f - h / 2 - 70 * s), w, h); Plate(r); hits.Add(r);
            float y = r.y + pad;
            for (int i = 0; i < parts.Count; i++) { GUI.Label(new Rect(r.x + pad, y, iw, hs[i]), parts[i].text, parts[i].st); y += hs[i]; }
        }

        /// <summary>A ring at the feet of what the mouse is on, when the cell can act on it, and the action cursor; nothing on scenery or passers-by.</summary>
        void HoverRing(Camera cam)
        {
            int want = m.Cursor;
            if (want != cursorIs) { var tex = want == 3 ? cursorNoLine : want == 2 ? cursorAim : want == 1 ? cursorAct : null; Cursor.SetCursor(tex, tex ? new Vector2(16, 16) : Vector2.zero, CursorMode.Auto); cursorIs = want; }
            if (!m.HoverRing.HasValue || !cam) return;
            var p = cam.WorldToScreenPoint(m.HoverRing.Value); if (p.z <= 0) return;
            float rw = 64 * s * 14f / cam.orthographicSize, rh = rw * 0.62f; // the ground circle at pitch 38
            GUI.DrawTexture(new Rect(p.x - rw / 2, Screen.height - p.y - rh / 2, rw, rh), ring);
        }

        /// <summary>A taken order or a landing pulses on its target: a ring that grows and fades over 0.7 s.</summary>
        void Pulses(Camera cam)
        {
            if (!cam) return; float now = Time.unscaledTime;
            foreach (var (w, kind, born) in m.Pulses)
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
            for (int i = m.Pops.Count - 1; i >= 0; i--)
            {
                var pp = m.Pops[i]; if (now > pp.Until) continue;
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
        void PanelGui()
        {
            float w = 410 * s, head = 30 * s, pad = 10 * s, iw = w - 2 * pad - 8 * s; var cards = m.PanelCards;
            if (heightsFor != cards.Count + (cards.Count > 0 ? cards[0].Title.GetHashCode() : 0) || heightsFont != wrapBright.fontSize)
            {
                heightsFor = cards.Count + (cards.Count > 0 ? cards[0].Title.GetHashCode() : 0); heightsFont = wrapBright.fontSize; heights.Clear();
                foreach (var cd in cards)
                    heights.Add(24 * s + (cd.Does.Length > 0 ? wrapBright.CalcHeight(new GUIContent(cd.Does), iw) : 0) + smallWrap.CalcHeight(new GUIContent(cd.Price), iw)
                        + (cd.Lock.Length > 0 ? wrapRefused.CalcHeight(new GUIContent(cd.Lock), iw) : 0) + 10 * s);
            }
            float aboutH = m.PanelAbout.Length > 0 ? smallWrap.CalcHeight(new GUIContent(m.PanelAbout), w - 2 * pad) + 4 * s : 0;
            float total = head + aboutH + (m.PanelNote.Length > 0 ? 44 * s : 0) + 6 * s; foreach (var hh in heights) total += hh + 4 * s;
            var r = new Rect(Mathf.Min(m.PanelAt.x + 12 * s, Screen.width - w - 8 * s), Mathf.Clamp(m.PanelAt.y - 12 * s, 8 * s, Mathf.Max(8 * s, Screen.height - total - 8 * s)), w, total);
            Plate(r); hits.Add(r);
            GUI.Label(new Rect(r.x + pad, r.y + 2 * s, w - 2 * pad, head), m.PanelTitle, txt);
            float y = r.y + head;
            if (aboutH > 0) { GUI.Label(new Rect(r.x + pad, y - 4 * s, w - 2 * pad, aboutH), m.PanelAbout, smallWrap); y += aboutH; }
            if (m.PanelNote.Length > 0) { GUI.Label(new Rect(r.x + pad, y, w - 2 * pad, 42 * s), m.PanelNote, smallWrap); y += 44 * s; }
            int hover = -1;
            for (int i = 0; i < cards.Count && i < heights.Count; i++)
            {
                var cd = cards[i]; var rr = new Rect(r.x + 4 * s, y, w - 8 * s, heights[i]);
                GUI.DrawTexture(rr, rr.Contains(Event.current.mousePosition) ? plateLit : plate);
                if (rr.Contains(Event.current.mousePosition)) hover = i;
                GUI.DrawTexture(new Rect(rr.x, rr.y, 3 * s, rr.height), cd.Available ? edge : refusedText);
                if (GUI.Button(rr, GUIContent.none, GUIStyle.none)) host.PanelDo(i);
                var c = GUI.color; if (!cd.Available) GUI.color = new Color(1, 1, 1, 0.62f);
                float yy = rr.y + 3 * s;
                GUI.Label(new Rect(rr.x + 8 * s, yy, 30 * s, 20 * s), cd.Key, key);
                GUI.Label(new Rect(rr.x + 40 * s, yy, iw - 40 * s, 20 * s), cd.Title, txtSmallBold);
                if (cd.Verb.Length > 0 && cd.Mine) GUI.Label(new Rect(rr.xMax - 34 * s, yy, 30 * s, 20 * s), $"[{cd.Verb}]", key);
                yy += 22 * s;
                if (cd.Does.Length > 0) { float dh = wrapBright.CalcHeight(new GUIContent(cd.Does), iw); GUI.Label(new Rect(rr.x + pad, yy, iw, dh), cd.Does, wrapBright); yy += dh; }
                float ph = smallWrap.CalcHeight(new GUIContent(cd.Price), iw); GUI.Label(new Rect(rr.x + pad, yy, iw, ph), cd.Price, smallWrap); yy += ph;
                GUI.color = c;
                if (cd.Lock.Length > 0) GUI.Label(new Rect(rr.x + pad, yy, iw, rr.yMax - yy), cd.Lock, wrapRefused);
                y += heights[i] + 4 * s;
            }
            if (Event.current.type == EventType.Repaint) host.SetPanelHover(hover);
        }

        [System.NonSerialized] int heightsFor = -1, heightsFont; [System.NonSerialized] readonly List<float> heights = new();

        void Icon(Rect r, string text)
        {
            GUI.DrawTexture(r, plate); GUI.Label(r, text, text.Length > 2 ? iconSmall : icon);
        }

        void EndCard()
        {
            var lines = m.EndLines;
            float w = 1100 * s, lh = 34 * s, h = 30 * s + lh * lines.Count;
            var r = new Rect(Screen.width / 2f - w / 2, Screen.height / 2f - h / 2 - 60 * s, w, h); Plate(r); hits.Add(r);
            for (int i = 0; i < lines.Count; i++) GUI.Label(new Rect(r.x + 24 * s, r.y + 14 * s + i * lh, w - 48 * s, lh), lines[i], i == 0 ? big : i == lines.Count - 1 || i == lines.Count - 2 ? small : txt);
        }
    }
}
