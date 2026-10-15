using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using Camera = UnityEngine.Camera;
using Event = UnityEngine.Event;

namespace Suborned.Play
{
    /// <summary>The play HUD at round 4's v10 "signage hotbar" fidelity (Sky, 2026-10-15; briefs/011/ui/round4/v10-signage-hotbar*.jpg), simple and
    /// placeholder-iconed: the roster of four slots at the bottom (number, portrait, health pips, state), the Q/W/E/R verb hotbar above it with the
    /// verb a right click gives lit, the top-right pill (mission clock, next event, $, Trail, Alert pips), number and verb markers on the pavement
    /// under the cell, at most two icons over a cast person, the price chip at the cursor ($ · time · Trail · Alert) before a right click commits
    /// (§02), and the end card with the debrief numbers. Transit-signage enamel: deep teal plates, cream type, thin brass-free borders. Plain
    /// IMGUI on its own component, reading HandoverHost only, so Glyph's art can replace it whole. Laid out at 1080p, scaled.</summary>
    [RequireComponent(typeof(HandoverHost))]
    public sealed class PlayHud : MonoBehaviour
    {
        // never kept over a domain reload (a kept style with its textures gone would draw nothing): built on the first OnGUI
        [System.NonSerialized] HandoverHost host; [System.NonSerialized] Texture2D plate, plateLit, edge, pipOn, pipOff, white;
        [System.NonSerialized] GUIStyle txt, big, small, key, mid; float s;
        static readonly List<Rect> hits = new();
        static readonly Color Teal = new(0.09f, 0.35f, 0.33f, 0.96f), TealLit = new(0.16f, 0.52f, 0.48f, 1f), Cream = new(0.96f, 0.93f, 0.85f), Dim = new(0.62f, 0.72f, 0.69f);
        static readonly string[] Names = { "Scout", "Fixer", "Tech", "Enforcer" };

        public static bool Blocks(Vector2 mouse) { var p = new Vector2(mouse.x, Screen.height - mouse.y); foreach (var r in hits) if (r.Contains(p)) return true; return false; }

        void Awake() => host = GetComponent<HandoverHost>();

        static Texture2D Tex(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

        void Styles()
        {
            if (txt == null || plate == null)
            {
                plate = Tex(Teal); plateLit = Tex(TealLit); edge = Tex(Cream); pipOn = Tex(Cream); pipOff = Tex(new Color(1, 1, 1, 0.22f)); white = Tex(Color.white);
                txt = new GUIStyle(GUI.skin.label) { normal = { textColor = Cream }, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = false, clipping = TextClipping.Overflow };
                big = new GUIStyle(txt); small = new GUIStyle(txt) { fontStyle = FontStyle.Normal, normal = { textColor = Dim } };
                key = new GUIStyle(txt) { alignment = TextAnchor.MiddleCenter }; mid = new GUIStyle(txt) { alignment = TextAnchor.MiddleCenter };
            }
            txt.fontSize = Mathf.RoundToInt(18 * s); big.fontSize = Mathf.RoundToInt(24 * s); small.fontSize = Mathf.RoundToInt(13 * s); key.fontSize = Mathf.RoundToInt(13 * s); mid.fontSize = Mathf.RoundToInt(15 * s);
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
                    var op = g.Ops[i]; string verb = op.Busy != null ? Glyph(op.Busy.Do, op.Busy.Approach) : op.Walk != null ? "›" : "✓";
                    var r = new Rect(p.x - 26 * s, Screen.height - p.y + 2 * s, 52 * s, 20 * s);
                    if (host.Selected[i]) { GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height), plateLit); }
                    GUI.Label(new Rect(r.x + 4 * s, r.y, 18 * s, r.height), $"{i + 1}", mid);
                    GUI.Label(new Rect(r.x + 20 * s, r.y, 32 * s, r.height), verb, mid);
                }

            // at most two icons over a cast person
            if (cam && host.Views != null)
                foreach (var role in new[] { "waitress", "driver", "guard", "councillor" })
                {
                    var t = host.Views.Find(role); if (!t || !t.gameObject.activeSelf) continue;
                    var p = cam.WorldToScreenPoint(t.position + Vector3.up * 2.05f); if (p.z <= 0) continue;
                    var (a, b) = Icons(g, role);
                    float w = 26 * s; var x = p.x - (b == null ? w / 2 : w + 1 * s); var y = Screen.height - p.y - w;
                    Icon(new Rect(x, y, w, w), a); if (b != null) Icon(new Rect(x + w + 2 * s, y, w, w), b);
                }

            // the price chip at the cursor
            var it = host.HoverIntent;
            if (it != null && host.Hover != null && host.Ending.Length == 0)
            {
                string line = it.Move ? $"{it.Label}   ·   {it.Time}" : $"{it.Label}   {it.Usd}   ·   {it.Time}   ·   {it.Trail}   ·   {it.Alert}{(it.Outcome.Length > 0 ? $"   ·   {it.Outcome}" : "")}";
                var size = txt.CalcSize(new GUIContent(line));
                var r = new Rect(host.Mouse.x + 22 * s, host.Mouse.y + 6 * s, size.x + 24 * s, 34 * s);
                if (r.xMax > Screen.width) r.x = host.Mouse.x - r.width - 10 * s;
                Plate(r, !it.Move); GUI.Label(new Rect(r.x + 12 * s, r.y, size.x, r.height), line, txt);
            }

            // top right pill: clock · next event · $ · Trail · Alert pips
            {
                int next = host.NextMark();
                string clock = HandoverHost.MissionClock(g.Tick), nx = HandoverHost.MissionClock(next);
                var r = new Rect(Screen.width - 600 * s, 16 * s, 584 * s, 44 * s); Plate(r); hits.Add(r);
                float x = r.x + 14 * s;
                GUI.Label(new Rect(x, r.y, 90 * s, r.height), clock, big); x += 86 * s;
                GUI.Label(new Rect(x, r.y, 120 * s, r.height), $"▶▶ {nx}", txt); x += 104 * s;
                GUI.Label(new Rect(x, r.y, 150 * s, r.height), $"${g.Cash:N0}", txt); x += 128 * s;
                GUI.Label(new Rect(x, r.y, 70 * s, r.height), $"T {g.Trail}", txt); x += 64 * s;
                GUI.Label(new Rect(x, r.y, 24 * s, r.height), "A", txt); Pips(x + 20 * s, r.y + 16 * s, g.Alert, 3, 12 * s); x += 80 * s;
                GUI.Label(new Rect(x, r.y, 120 * s, r.height), host.Paused ? "<color=#ffd27a>PAUSE</color>" : $"x{host.Speed}", txt);
                GUI.Label(new Rect(r.x, r.yMax + 2 * s, r.width, 18 * s), $"{host.CityClock()} · Space pause · = − speed · N next event · F1 debug", small);
            }

            // roster: four slots, number, portrait, health pips, state
            float slotW = 84 * s, slotH = 74 * s, rosterW = slotW * 4 + 10 * s;
            var roster = new Rect(Screen.width / 2f - rosterW / 2, Screen.height - slotH - 16 * s, rosterW, slotH);
            Plate(roster); hits.Add(roster);
            for (int i = 0; i < 4; i++)
            {
                var r = new Rect(roster.x + 5 * s + i * slotW, roster.y + 4 * s, slotW - 4 * s, slotH - 8 * s);
                if (host.Selected[i]) GUI.DrawTexture(r, plateLit);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) host.Select((OpId)i, Event.current.shift);
                var op = g.Ops[i];
                GUI.Label(new Rect(r.x + 4 * s, r.y, 16 * s, 18 * s), $"{i + 1}", key);
                GUI.Label(new Rect(r.x, r.y + 12 * s, r.width, 30 * s), Names[i], mid);
                int hp = i == 3 ? g.EnforcerHealth : 4, hpMax = i == 3 ? Mathf.Max(g.EnforcerHealth, g.R.EnforcerHealth) : 4;
                Pips(r.x + 10 * s, r.y + 44 * s, hp, hpMax, 7 * s);
                string state = op.Inside ? "inside" : op.Busy != null ? $"{Glyph(op.Busy.Do, op.Busy.Approach)} {HandoverHost.MissionClock(op.BusyEnd)}" : op.Walk != null ? "walking" : "ready";
                GUI.Label(new Rect(r.x, r.y + 50 * s, r.width, 16 * s), state, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            }

            // the Q/W/E/R hotbar for the agent, the lit key is what a right click gives
            var slots = HandoverHost.Slots[(int)host.Agent];
            float kw = 64 * s, kh = 50 * s;
            var bar = new Rect(roster.x + 6 * s, roster.y - kh - 10 * s, kw * 4 + 12 * s, kh);
            for (int i = 0; i < 4; i++)
            {
                var r = new Rect(bar.x + i * (kw + 4 * s), bar.y, kw, kh); hits.Add(r);
                bool has = i < slots.Length, lit = has && host.LitSlot == i;
                Plate(r, lit);
                if (has && GUI.Button(r, GUIContent.none, GUIStyle.none)) host.Pick(i);
                GUI.Label(new Rect(r.x, r.y + 2 * s, r.width, 26 * s), has ? slots[i].Glyph : "·", mid);
                GUI.Label(new Rect(r.x, r.yMax - 20 * s, r.width, 18 * s), HandoverHost.Keys[i], key);
            }
            GUI.Label(new Rect(bar.xMax + 10 * s, bar.y, 420 * s, kh), $"{Names[(int)host.Agent]}: {(host.LitSlot >= 0 && host.LitSlot < slots.Length ? slots[host.LitSlot].Label : "")}", txt);

            // status line
            GUI.Label(new Rect(16 * s, Screen.height - 34 * s, Screen.width / 2f - rosterW / 2 - 24 * s, 24 * s), host.Status, small);

            if (host.Ending.Length > 0) EndCard(g);
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
            GUI.DrawTexture(r, plate); GUI.Label(r, text, new GUIStyle(mid) { fontSize = Mathf.RoundToInt((text.Length > 2 ? 10 : 14) * s) });
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
