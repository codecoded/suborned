using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 027 playground (EXPERIMENTAL; IMGUI, the B0–B2 debug layer, now behind the P panel switches: debugLayer, and ladder off for the strip; Glyph's finished HUD is HudToolkit.FightLab): the freeze banner and queue
    /// strip (one slot per agent: its queued action and price, from PreviewFire / PreviewVolley; the charge bar from ChargeOf), target mode's
    /// forecast under each targeted combatant (●○●● from PreviewVolley, the chunk it takes off the bar, DOWN), and the lab's damage chunk on
    /// the hovered target (one hit's PreviewFire damage against its bar). Read only: it never changes the run.</summary>
    public sealed class FightLabHud : MonoBehaviour
    {
        HandoverHost host;
        static GUIStyle big, small, mid;
        static readonly Color Ink = new(0.96f, 0.94f, 0.88f), Dim = new(0.06f, 0.1f, 0.1f, 0.86f), Hit = new(0.97f, 0.42f, 0.3f), Lit = new(0.36f, 0.8f, 0.62f), Empty = new(0.2f, 0.24f, 0.24f, 0.9f), Charge = new(0.98f, 0.82f, 0.36f);

        void Awake() => host = GetComponent<HandoverHost>();

        void OnGUI()
        {
            if (host == null || host.Game == null || host.Ending.Length > 0 || host.View == null) return;
            var look = host.Fights.Hud; if (!look.debugLayer && look.ladder) return; // Glyph's finished HUD (HudToolkit.FightLab) draws instead
            big ??= new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            mid ??= new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter, richText = true };
            small ??= new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.UpperLeft, richText = true, wordWrap = true };
            float s = Mathf.Max(1f, Screen.height / 1080f); var m = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s;
            var g = host.Game;
            try
            {
                if (host.Frozen && !look.debugLayer) Strip(g, W); // switch #2 off: the strip across the top instead of the roster markers
                else if (host.Frozen)
                {
                    var r = new Rect(W / 2 - 260, 88, 520, 30); Fill(r, Dim);
                    GUI.Label(r, host.TargetFreeze ? $"<color=#ffd27a>TARGET</color> · {host.FrozenWhy} · click a combatant: +1 shot · right: −1 · Y fires" : $"<color=#8fe3ff>FROZEN</color> · {host.FrozenWhy} · one action each · Return releases · Esc leaves", mid);
                    Strip(g, W);
                    Forecasts(g, s);
                }
                else if (g.Lab && host.LabChunk is { } ch && ch.max > 0 && host.WorldOf(ch.id) is Vector3 w) Chunk(w, ch.health, ch.max, ch.damage, s);
            }
            finally { GUI.matrix = m; }
        }

        /// <summary>The queue strip: one slot per agent with its queued action, target and price, and the charge bar.</summary>
        void Strip(Game g, float W)
        {
            float x0 = W / 2 - 4 * 130 + 0, y = 122;
            for (int k = 0; k < 4; k++)
            {
                var r = new Rect(x0 + k * 260, y, 252, 54); Fill(r, Dim);
                string name = Op.Name((OpId)k), what = "—", price = "";
                var q = host.Queued[k];
                if (host.VolleyShots[k] > 0)
                {
                    var v = Safe(() => g.PreviewVolley((OpId)k, host.VolleyTarget[k], host.VolleyShots[k]));
                    what = $"volley ×{host.VolleyShots[k]} → {host.VolleyTarget[k]}";
                    if (v != null) price = v.Refused.Length > 0 ? $"<color=#ff8a7a>{v.Refused}</color>" : $"{Dots(v)} {(v.Down ? "<b>DOWN</b>" : $"→ {v.HealthAfter}")} · ▮{v.ChargeCost}";
                }
                else if (q != null)
                {
                    what = $"{q.Do} {(q.Target.Length > 0 ? q.Target : q.To.Length > 0 ? q.To : q.Point.HasValue ? "there" : q.Stance)}";
                    if (q.Do == "fire" && g.Lab) { var p = Safe(() => g.PreviewFire((OpId)k, q.Target, q.Force)); if (p != null) price = p.Refused.Length > 0 ? $"<color=#ff8a7a>{p.Refused}</color>" : $"{Pips(p.Pips)} −{p.Damage} ×{p.HitsToDown}{(p.Walk ? $" · walks {p.WalkS} s" : "")}"; }
                }
                GUI.Label(new Rect(r.x + 6, r.y + 2, r.width - 12, 18), $"<b>{name}</b>  {what}", small);
                GUI.Label(new Rect(r.x + 6, r.y + 18, r.width - 12, 18), price, small);
                if (g.Lab && g.Labr != null && g.Labr.Charge)
                {
                    int c = g.ChargeOf((OpId)k), mx = Mathf.Max(1, g.Labr.ChargeMax);
                    var b = new Rect(r.x + 6, r.y + 40, r.width - 12, 7); Fill(b, Empty); Fill(new Rect(b.x, b.y, b.width * Mathf.Clamp01(c / (float)mx), b.height), Charge);
                }
            }
            if (host.QueuedCell != null) { var r = new Rect(W / 2 - 100, y + 58, 200, 20); Fill(r, Dim); GUI.Label(r, $"cell: {host.QueuedCell.Do}", mid); }
        }

        /// <summary>Target mode: under each targeted combatant, the shots as they will land if it stands (●○●●), the chunk, DOWN.</summary>
        void Forecasts(Game g, float s)
        {
            if (!host.TargetFreeze) return;
            for (int k = 0; k < 4; k++)
            {
                if (host.VolleyShots[k] <= 0 || host.VolleyTarget[k] == null) continue;
                var v = Safe(() => g.PreviewVolley((OpId)k, host.VolleyTarget[k], host.VolleyShots[k])); var gun = g.GunOf(host.VolleyTarget[k]);
                if (v == null || gun == null || !(host.WorldOf(host.VolleyTarget[k]) is Vector3 w)) continue;
                var sp = host.View.WorldToScreenPoint(w + Vector3.down * 0.2f); if (sp.z <= 0) continue;
                float x = sp.x / s, y = (Screen.height - sp.y) / s + 8 + k * 30;
                var r = new Rect(x - 80, y, 160, 26); Fill(r, Dim);
                GUI.Label(new Rect(r.x, r.y - 1, r.width, 16), $"{Op.Name((OpId)k)} {Dots(v)} {(v.Down ? "<b>DOWN</b>" : "")}", mid);
                Bar(new Rect(r.x + 6, r.y + 17, r.width - 12, 6), gun.Health, gun.Max, gun.Health - v.HealthAfter);
            }
        }

        void Chunk(Vector3 w, int hp, int max, int dmg, float s)
        {
            var sp = host.View.WorldToScreenPoint(w + Vector3.down * 0.15f); if (sp.z <= 0) return;
            var r = new Rect(sp.x / s - 50, (Screen.height - sp.y) / s + 10, 100, 8); Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), Dim);
            Bar(r, hp, max, Mathf.Min(hp, dmg));
        }

        /// <summary>A segmented bar (one segment per 10): health lit, the chunk about to go in the hit colour.</summary>
        static void Bar(Rect r, int hp, int max, int chunk)
        {
            int segs = Mathf.Max(1, (max + 9) / 10); float sw = r.width / segs;
            for (int i = 0; i < segs; i++)
            {
                int lo = i * 10, hi = lo + 10; var c = hp <= lo ? Empty : hp - chunk <= lo ? Hit : Lit;
                Fill(new Rect(r.x + i * sw + 0.5f, r.y, sw - 1f, r.height), c);
            }
        }

        static string Pips(int n) => n <= 0 ? "walk" : new string('●', n) + new string('○', Mathf.Max(0, 4 - n));
        static string Dots(VolleyPreview v) { var sb = new System.Text.StringBuilder(); foreach (var x in v.Shots) sb.Append(x.hit ? '●' : '○'); return sb.ToString(); }
        static T Safe<T>(System.Func<T> f) where T : class { try { return f(); } catch { return null; } }
        static void Fill(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = o; }
    }
}
