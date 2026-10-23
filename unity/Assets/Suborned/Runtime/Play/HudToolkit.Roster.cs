using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 046 (Sky's picks, 2026-10-22): the roster column with the icons under each portrait. Layout `under` (one slim row under
    /// the ring: weapon, status, dots), the steel divider without screws between portraits, the column art `roster_column_bare` (B: four outer
    /// rivets, no side stripes) stretched by the lab's recipe `tallColumn/2` (the kit sprite cut at its clean body rows, a tone-matched block
    /// inserted in each of the four gaps: rings, rims and caps stay the sprite's own pixels), one weapon icon, no number tabs, no key numbers.
    /// Every number comes from the lab's export (`rosterSpec` in Resources/HudStyle/revisedA.tokens.json, px at 1080p with the column drawn at
    /// geometry.s); the alignment rules of the lab's ALIGN check run in the layout audit (RosterAlign).</summary>
    public sealed partial class HudToolkit
    {
        // ---------------------------------------------------------------- the export's roster spec (JsonUtility: fixed keys only)
        [System.Serializable] sealed class TokMark { public float dy, h, w, x; }
        [System.Serializable] sealed class TokDiv { public string id = ""; public float d2x = 55, d, gap, rowDy = 36.8f; public TokMark mark = new(); }
        [System.Serializable] sealed class TokDivs { public TokDiv none, space, hair, band, steel, notch, well; }
        [System.Serializable] sealed class TokColUnder { public string picked = "steel"; public TokDivs dividers = new(); }
        [System.Serializable] sealed class TokColumn { public string recipe = "tallColumn/2"; public int ease = 6, row = 222, w = 218, h = 833; public int[] cuts = { 223, 412, 601, 789 }, body = { 36, 182 }; public TokColUnder under = new(); }
        [System.Serializable] sealed class TokTab { public float dx = 9.48f, dy = 38.35f, w = 67, h = 16; }
        [System.Serializable] sealed class TokUnderSlots { public TokTab tab = new(); public float[] weapon = { 0, 0, 16, 16 }, status = { 19, 0, 16, 16 }, dots = { 39, 4, 28, 8 }; }
        [System.Serializable] sealed class TokSlots { public TokUnderSlots under = new(); }
        [System.Serializable] sealed class TokGeom { public float s = 0.79f, XC = 42.98f, WIN = 52.14f, MAT = 29.6f, RING = 36.34f; }
        [System.Serializable] sealed class TokRosterSpec { public TokGeom geometry = new(); public TokSlots slots = new(); public TokColumn column = new(); }
        [System.Serializable] sealed class TokRoster { public string layout = "under", divider = "steel", column = "bare"; }
        [System.Serializable] sealed class TokFontSide { public string file = ""; }
        [System.Serializable] sealed class TokFontSpec { public string id = ""; public TokFontSide body = new(), strong = new(); }

        static TokRosterSpec RS => Tok.rosterSpec ??= new TokRosterSpec();
        static TokRoster RosterTok => Tok.roster ??= new TokRoster();
        /// <summary>The roster column's scale on screen (rosterSpec.geometry.s: the lab draws the kit column at 0.79).</summary>
        static float RosterScale => RS.geometry.s > 0.1f ? RS.geometry.s : 0.79f;
        /// <summary>The picked divider's geometry (roster.divider; "steel" by default).</summary>
        static TokDiv Divider
        {
            get
            {
                var d = RS.column.under.dividers; var id = RosterTok.divider;
                var v = id switch { "none" => d.none, "space" => d.space, "hair" => d.hair, "band" => d.band, "notch" => d.notch, "well" => d.well, _ => d.steel };
                return v != null && v.d2x > 0 ? v : new TokDiv { id = "steel", d2x = 55, rowDy = 36.8f, mark = new TokMark { dy = 55.3f, h = 1, w = 54, x = 15.98f } };
            }
        }
        /// <summary>The gap each divider adds between portraits, in the column's own (1x art) px: the recipe's d2x rows at 2x.</summary>
        static float RosterGap => columnTall ? Divider.d2x / 2f : 0f;
        /// <summary>The column's height in its own px (416 for the kit column; taller with the icons under each portrait).</summary>
        static float ColH => 416f + 4f * RosterGap;
        static Vector2 BezelAt(int i) => ColumnBezels[i] + new Vector2(0f, i * RosterGap);
        static bool columnTall;

        // ---------------------------------------------------------------- the column art: tallColumn/2 on the picked sprite

        static Texture2D tallTex;
        /// <summary>The picked column (roster.column: kit, bare, inlay, rivetless) stretched by tallColumn/2 with the divider's d2x, built once
        /// from the sprite's own pixels (the sprite is imported readable). Null: the sprite is missing or unreadable (the kit column is drawn).</summary>
        Texture2D TallColumn()
        {
            if (tallTex != null) return tallTex;
            string file = RosterTok.column switch { "kit" => "roster/roster_column", "inlay" => "roster/roster_column_inlay", "rivetless" => "roster/roster_column_rivetless", _ => "roster/roster_column_bare" };
            var sp = Sp(file) ?? Sp("roster/roster_column"); if (sp == null) return null;
            var src = sp.texture; if (src == null || !src.isReadable) { Debug.LogWarning($"HUD: roster column '{file}' is not readable; the kit column is drawn"); return null; }
            var c = RS.column; int W = src.width, H = src.height, d2x = Mathf.RoundToInt(Divider.d2x), ease = Mathf.Max(1, c.ease);
            if (W != c.w || H != c.h || c.cuts == null || c.cuts.Length != 4) { Debug.LogWarning($"HUD: roster column {W}x{H} is not the spec's {c.w}x{c.h}; the kit column is drawn"); return null; }
            var px = src.GetPixels32(); // bottom-up rows
            float[] Row(int y) { var r = new float[W * 4]; int o = (H - 1 - y) * W; for (int x = 0; x < W; x++) { var p = px[o + x]; r[x * 4] = p.r; r[x * 4 + 1] = p.g; r[x * 4 + 2] = p.b; r[x * 4 + 3] = p.a; } return r; }
            int b0 = c.body[0], b1 = c.body[1];
            float[] Smooth(float[] r) { var o = (float[])r.Clone(); for (int x = b0; x <= b1; x++) for (int k = 0; k < 4; k++) { float sum = 0; int n = 0; for (int j = -5; j <= 5; j++) { int q = x + j; if (q < b0 || q > b1) continue; sum += r[q * 4 + k]; n++; } o[x * 4 + k] = sum / n; } return o; }
            int[][] edge = { new[] { 17, 30 }, new[] { 187, 200 } };
            float EdgeMean(float[] r, int k) { float sum = 0; int n = 0; foreach (var e in edge) for (int x = e[0]; x <= e[1]; x++) { sum += r[x * 4 + k]; n++; } return sum / n; }
            float SS(float t) => t * t * (3f - 2f * t);
            var plain = Smooth(Row(c.row));
            int OH = H + 4 * d2x; var outRows = new List<float[]>(OH); // top-down
            int y0 = 0;
            foreach (int cut in c.cuts)
            {
                for (int y = y0; y < cut; y++) outRows.Add(Row(y));
                var A = Smooth(Row(cut - 1)); var B = Smooth(Row(cut));
                var gain = new float[3]; for (int k = 0; k < 3; k++) gain[k] = Mathf.Clamp((EdgeMean(A, k) + EdgeMean(B, k)) / 2f / Mathf.Max(1f, EdgeMean(plain, k)), 0.6f, 1.4f);
                for (int r = 0; r < d2x; r++)
                {
                    float t = (r + 0.5f) / d2x, wa = SS(Mathf.Max(0f, 1f - (r + 0.5f) / ease)), wb = SS(Mathf.Max(0f, 1f - (d2x - r - 0.5f) / ease));
                    var o = new float[W * 4];
                    for (int x = 0; x < W; x++)
                        for (int k = 0; k < 4; k++)
                        {
                            int i = x * 4 + k; bool inBody = x >= b0 && x <= b1;
                            o[i] = k == 3 ? 255f : inBody ? plain[i] * gain[k] + (A[i] - plain[i] * gain[k]) * wa + (B[i] - plain[i] * gain[k]) * wb : A[i] * (1f - t) + B[i] * t;
                        }
                    outRows.Add(o);
                }
                y0 = cut;
            }
            for (int y = y0; y < H; y++) outRows.Add(Row(y));
            var outPx = new Color32[W * OH];
            for (int y = 0; y < OH; y++) { var r = outRows[y]; int o = (OH - 1 - y) * W; for (int x = 0; x < W; x++) outPx[o + x] = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(r[x * 4]), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(r[x * 4 + 1]), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(r[x * 4 + 2]), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(r[x * 4 + 3]), 0, 255)); }
            var tex = new Texture2D(W, OH, TextureFormat.RGBA32, false) { name = $"roster_column_{RosterTok.column}_tall_{Divider.id}", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(outPx); tex.Apply(false, true);
            Debug.Log($"HUD: roster column '{file}' by {c.recipe}, divider {Divider.id} (d2x {d2x}): {W}x{OH}");
            return tallTex = tex;
        }

        // ---------------------------------------------------------------- the row under each portrait

        /// <summary>Spec px (1080p, column at geometry.s) in the column's own px.</summary>
        static float Art(float specPx) => specPx / RosterScale;
        /// <summary>The row's origin under portrait i (column px): x from the ring centre (the lab centres the 67 px row on the column), y the
        /// divider's rowDy below the ring centre.</summary>
        /// <summary>The photo clip's top-left on whole column px: the layout snaps every element to whole px, so the row and its slots are placed
        /// in whole px from this one origin and every portrait's badges sit at the same offset from its ring (the audit measured 0.79 px apart
        /// when each was rounded on its own).</summary>
        static Vector2 SnapOrigin(int i) { var c = BezelAt(i); return new Vector2(Mathf.Round(c.x - BezelR), Mathf.Round(c.y - BezelR)); }
        static Vector2 RowAt(int i) { var t = RS.slots.under.tab; return SnapOrigin(i) + new Vector2(Mathf.Round(BezelR - Art(t.w / 2f)), Mathf.Round(BezelR + Art(Divider.rowDy))); }
        static Rect SlotRect(int i, float[] s) { var a = RowAt(i); return new Rect(a.x + Mathf.Round(Art(s[0])), a.y + Mathf.Round(Art(s[1])), Mathf.Round(Art(s[2])), Mathf.Round(Art(s[3]))); }

        /// <summary>The weapon, status and dots slots of portrait i in the tall column (the weapon disc is the window's Arm, so its hover fan,
        /// click and fire mark stay as they were); the steel divider under portraits 1 to 3.</summary>
        void BuildUnder(VisualElement frame, Window w, int i)
        {
            var sl = RS.slots.under; var so = SnapOrigin(i); w.Clip.style.left = so.x; w.Clip.style.top = so.y; // the clip on whole px, the row placed from it
            var wr = SlotRect(i, sl.weapon); w.Arm.style.left = wr.x; w.Arm.style.top = wr.y; w.Arm.style.width = wr.width; w.Arm.style.height = wr.height; Round(w.Arm, wr.width / 2f); Border(w.Arm, 0f, Color.clear);
            float gi = Mathf.Round(wr.width * 0.6f * 2f) / 2f; // the lab: the icon is 60 % of the disc's box
            w.GunIcon.style.left = (wr.width - gi) / 2f; w.GunIcon.style.top = (wr.height - gi) / 2f; w.GunIcon.style.width = gi; w.GunIcon.style.height = gi;
            w.StanceIn.style.left = (wr.width - gi) / 2f; w.StanceIn.style.top = (wr.height - gi) / 2f; w.StanceIn.style.width = gi; w.StanceIn.style.height = gi;
            var sr = SlotRect(i, sl.status); w.Status = E(frame, null, sr.x, sr.y, sr.width, sr.height); w.Status.name = "roster-status";
            w.StatusIcon = E(w.Status, null, (sr.width - gi) / 2f, (sr.height - gi) / 2f, gi, gi); w.StatusIcon.name = "g"; Show(w.Status, false);
            var dr = SlotRect(i, sl.dots); w.Dots = E(frame, null, dr.x, dr.y, dr.width, dr.height); w.Dots.name = "roster-dots";
            w.Dots.style.backgroundColor = Hex(0x142c2c); Round(w.Dots, Art(3f)); // kit.uss --pip-empty, the lab's track
            w.Pips.RemoveFromHierarchy(); w.Dots.Add(w.Pips); w.Pips.style.left = 0; w.Pips.style.top = 0; w.Pips.style.width = dr.width; w.Pips.style.height = dr.height; w.Pips.style.alignItems = Align.Center;
            w.Under = true;
            var mk = Divider.mark;
            if (i < 3 && mk != null && mk.h > 0 && mk.w > 0)
            {   // steel: a 1 px chrome line at 45 %, centred on the column (kit.uss --chrome; Sky: divider 5 without its screws)
                var dv = E(frame, null, so.x + Mathf.Round(BezelR - Art(mk.w / 2f)), so.y + Mathf.Round(BezelR + Art(mk.dy)), Mathf.Round(Art(mk.w)), Mathf.Max(1f, Mathf.Round(Art(mk.h)))); dv.name = "roster-divider";
                dv.style.backgroundColor = Divider.id switch { "hair" => Brass, "band" => Hex(0x72b5a7), "well" => Hex(0x142c2c), _ => new Color(Chrome.r, Chrome.g, Chrome.b, 0.45f) };
            }
        }

        /// <summary>The cause icon of a hostile's strongest cause (brief 046 why.icons: kit icons only; the gun never repeats: "drawn" rides on
        /// the weapon disc).</summary>
        static string WhyIcon(string cause) => cause switch { "fired" => "bang", "vale" => "s_carrying", "violence" => "takedown", "face" => "papers", "seen" => "sawyou", "range" or "final" => "bang", _ => "" };

        /// <summary>One portrait's row this frame: the weapon (drawn = light disc, ink gun; holstered = dark disc, the gun at 55 %; "drawn" as the
        /// cause = the kit's selected light disc), the status slot by priority (left or in the car > under fire's cause > down > seen (a known
        /// face) > watched > the armed verb > busy), the dots.</summary>
        void UnderFrame(int i, Window w, HudModel.Slot s)
        {
            var fw = host.FireWhyNow[i]; string cause = fw.Count > 0 ? fw[0].main : "";
            bool causeDrawn = cause == "drawn" && !s.Down;
            bool drawn = s.Drawn || causeDrawn;
            if (s.Armed && !s.Down)
            {
                Bg(w.Arm, drawn ? (causeDrawn ? "markers/disc_light_24_selected" : "markers/disc_light_24") : "markers/disc_dark_24", Color.white);
                w.Arm.style.backgroundColor = Color.clear; w.Arm.style.unityBackgroundImageTintColor = Color.white;
                w.GunIcon.style.unityBackgroundImageTintColor = drawn ? Col(Tok.type.card, Ink) : Cream; w.GunIcon.style.opacity = drawn ? 1f : 0.55f;
                int warn = host.WarnS(i); bool pulse = warn >= 0 && Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.4f;
                Border(w.Arm, warn >= 0 ? (pulse ? 2f : 1.2f) : 0f, warn >= 0 ? Amber : Color.clear); // a watcher's warning: the amber "running out" ring, its seconds beside it
                var wl = w.Arm.Q<Label>("warn"); Show(wl, warn >= 0); if (warn >= 0) Text(wl, $"{warn}");
            }
            // the status slot
            string icon = ""; bool light = false;
            bool inCar = s.InCar, left = s.LeftFate.Length > 0;
            bool known = false; try { known = host.Game != null && System.Linq.Enumerable.Contains(host.Game.KnownFaces, Op.Name((OpId)i)); } catch (System.Exception) { known = false; }
            int pk = host.Picked; bool verb = !PlateOn && pk >= 0 && pk < 5 && i == m.Agent && s.Selected && m.VerbHas[pk] && !m.Ended;
            if (inCar) icon = "car";
            else if (left) icon = s.LeftFate == "captured" ? "restrain" : "s_moving";
            else if (cause.Length > 0 && !causeDrawn && !s.Down && WhyIcon(cause) is { Length: > 0 } wi) { icon = wi; light = true; }
            else if (s.Down) icon = "s_downed";
            else if (known) icon = "sawyou";
            else if (host.EyeStage(i) >= 1 && !m.Ended) icon = "cone";
            else if (verb) icon = ArmedIcon();
            else if (s.Busy && s.Icon.Length > 0 && s.Icon != "s_moving") icon = s.Icon; // an act (the lab's "busy"); a plain walk or run is not a state: the slot stays empty (Glyph 0.2.79 #3: a footprint under all four)
            if (icon == "fire") icon = ""; // one gun per portrait (brief 046): firing is the weapon disc and its fire mark, never a second gun
            Show(w.Status, icon.Length > 0);
            if (icon.Length > 0) { Bg(w.Status, light ? "markers/disc_light_24" : "markers/disc_dark_24", Color.white); Icon(w.StatusIcon, icon, light ? Col(Tok.type.card, Ink) : Cream); }
            Show(w.Dots, s.HpMax > 0);
        }

        /// <summary>The roster's four pips in the dots slot: 5 px dots, 2 px apart, centred on the 28 px track (the lab's `.pips.row`).</summary>
        void UnderPips(Window st, int hp, int max)
        {
            // four pips at 5 px with 1 px each side fill the 28 px track; the Enforcer's six (rulebook health-enforcer 6) are drawn smaller and
            // still round, 1 px apart, inside the same track (Glyph 0.2.79 #6: flex had squeezed them into slivers)
            float track = 28f, d5 = max <= 4 ? 5f : Mathf.Max(2.5f, (track - (max - 1) * 1f - 1f) / max), g1 = max <= 4 ? 1f : 0.5f;
            float dot = Art(d5), gap = Art(g1);
            while (st.PipDots.Count < max) { var d = E(st.Pips, null, w: dot, h: dot); st.PipDots.Add(d); }
            foreach (var d in st.PipDots) { d.style.width = dot; d.style.height = dot; d.style.minWidth = dot; d.style.flexShrink = 0f; d.style.marginLeft = gap; d.style.marginRight = gap; d.style.alignSelf = Align.Center; }
            for (int i = 0; i < st.PipDots.Count; i++) { Show(st.PipDots[i], i < max); if (i < max) Bg(st.PipDots[i], i < hp ? "markers/pip_full" : "markers/pip_empty"); }
        }

        // ---------------------------------------------------------------- the lab's ALIGN rules, in the build (Splice lifts the full check)

        /// <summary>The roster's alignment (brief 046 ALIGN, for the `under` layout): (1) every badge inside its slot (0.6 px), (3) no badge within
        /// geometry.RING of a face centre, (4) no two badges touching, (5) every slot at the same offset from its ring centre on all four
        /// portraits (0.7 px), (6) one gun per portrait. Screen px at 1080p. Empty = clean.</summary>
        static void RosterAlign(List<string> bad)
        {
            if (live == null || live.column == null || !Visible(live.column) || !columnTall) return;
            float k = 1080f / Mathf.Max(1f, live.root.panel.visualTree.layout.height), ringR = RS.geometry.RING;
            var offs = new Dictionary<string, List<Vector2>>();
            for (int i = 0; i < 4; i++)
            {
                var w = live.colWins[i]; if (w == null || !w.Under) continue;
                var cb = w.Clip.worldBound; var centre = cb.center * k;
                var badges = new List<(string slot, Rect r)>();
                void Add(string slot, VisualElement e, float[] spec)
                {
                    if (e == null || !Visible(e)) return; var b = e.worldBound; var r = new Rect(b.x * k, b.y * k, b.width * k, b.height * k);
                    var want = SlotRect(i, spec); var cw = live.column.worldBound; var sr = new Rect((cw.x + want.x * RosterScale) * k, (cw.y + want.y * RosterScale) * k, want.width * RosterScale * k, want.height * RosterScale * k);
                    if (r.xMin < sr.xMin - 0.6f || r.yMin < sr.yMin - 0.6f || r.xMax > sr.xMax + 0.6f || r.yMax > sr.yMax + 0.6f) bad.Add($"roster {i + 1}/{slot}: {r} outside its slot {sr}");
                    // the drawn disc (48 of 64 px of the kit's disc sprite) and the dots track must clear the ring
                    float rad = slot == "dots" ? 0f : r.width * 0.75f / 2f; var cc = r.center;
                    var nearest = slot == "dots" ? new Vector2(Mathf.Clamp(centre.x, r.xMin, r.xMax), Mathf.Clamp(centre.y, r.yMin, r.yMax)) : cc;
                    if (Vector2.Distance(nearest, centre) - rad < ringR - 0.6f) bad.Add($"roster {i + 1}/{slot}: {Vector2.Distance(nearest, centre) - rad:0.0} px from the face centre (ring {ringR:0.00})");
                    (offs.TryGetValue(slot, out var l) ? l : offs[slot] = new List<Vector2>()).Add(r.position - centre);
                    badges.Add((slot, r));
                }
                var sl = RS.slots.under; Add("weapon", w.Arm, sl.weapon); Add("status", w.Status, sl.status); Add("dots", w.Dots, sl.dots);
                for (int a = 0; a < badges.Count; a++) for (int b = a + 1; b < badges.Count; b++)
                    { var ra = badges[a].r; var rb = badges[b].r; if (ra.xMin < rb.xMax && rb.xMin < ra.xMax && ra.yMin < rb.yMax && rb.yMin < ra.yMax) bad.Add($"roster {i + 1}: {badges[a].slot} touches {badges[b].slot}"); }
                int guns = 0; if (w.Arm != null && Visible(w.Arm)) guns++; if (w.Status != null && Visible(w.Status) && w.StatusIcon.resolvedStyle.backgroundImage.sprite is Sprite gs && gs.name.StartsWith("fire")) guns++;
                if (guns > 1) bad.Add($"roster {i + 1}: {guns} guns");
            }
            foreach (var (slot, l) in offs) for (int a = 1; a < l.Count; a++) if (Vector2.Distance(l[a], l[0]) > 0.7f) bad.Add($"roster {slot}: offset {l[a]} differs from {l[0]} by {Vector2.Distance(l[a], l[0]):0.00} px");
        }

        // ---------------------------------------------------------------- the HUD type (brief 046 round 4/5, Sky: "Jost Regular")

        /// <summary>The kit's Jost styles (kit.uss) and the role each takes in the picked type (fontSpec): strong for the clock, the clock bar's
        /// values, counters, heads and status words (and every SemiBold), the body face for the rest. Barlow (speech, reading lines) is kept.</summary>
        static readonly Dictionary<string, bool> JostStyles = new()
        {
            ["clock"] = true, ["pill-value"] = true, ["role-tag"] = true, ["slot-state-ready"] = true, ["slot-state-busy"] = true, ["slot-state-hurt"] = true, ["slot-state-down"] = true,
            ["slot-time"] = true, ["numplate"] = false, ["keycap"] = false, ["verb-name"] = false, ["objective"] = false, ["hint-tab"] = true, ["section-label"] = true, ["panel-title"] = false,
            ["card-title"] = false, ["card-tag"] = false, ["briefing-title"] = true, ["button-label"] = true, ["loud-title"] = true, ["loud-hold"] = false, ["feed-title"] = true,
            ["feed-clock"] = false, ["feed-bar"] = true, ["feed-filter"] = false, ["chip-value"] = false, ["fight-chip"] = false, ["tooltip-title"] = false,
        };
        static readonly Dictionary<string, Font> typeFonts = new();
        static Font TypeFont(string file)
        {
            if (string.IsNullOrEmpty(file)) return null; var name = System.IO.Path.GetFileNameWithoutExtension(file);
            if (typeFonts.TryGetValue(name, out var f)) return f;
            f = Resources.Load<Font>("Kit/fonts/" + name); if (f == null) Debug.LogWarning($"HUD: font '{name}' is not in Kit/fonts; the kit's Jost kept");
            return typeFonts[name] = f;
        }
        /// <summary>A kit label's face by the type token: the body or strong file of fontSpec for a Jost style, drawn as cut (no synthetic bold).</summary>
        static void TypeOf(Label l, string style)
        {
            if (!JostStyles.TryGetValue(style, out bool strong)) return;
            var fs = Tok.fontSpec; if (fs == null) return;
            var f = TypeFont(strong ? fs.strong.file : fs.body.file); if (f == null) return;
            l.style.unityFontDefinition = FontDefinition.FromFont(f); l.style.unityFontStyleAndWeight = FontStyle.Normal;
        }
    }
}
