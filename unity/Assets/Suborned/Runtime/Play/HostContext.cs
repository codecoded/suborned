using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 030 (Glyph, final 2026-10-20): the contextual right click and the ring. A right click does one act chosen by the target
    /// (ContextRank over the rows the action panel builds, so no price or lock logic is rewritten); it acts on release (under 0.22 s and 6 px),
    /// except Fire, which acts on press. Held 0.22 s without moving, or Shift + right click, it opens the ring at the press: the ways open there,
    /// best first from 12 o'clock, at most five and the full panel (⋯). Release over a wedge takes it; released in the middle a young ring stays
    /// (sticky) for a click. In third person E is the quick click on the focus and a held E opens the ring on it. Keys (§8, Sky): 1–4 select,
    /// Shift+1–4 pick the selected agent's action (in third person only while standing), Tab and Shift+Tab step through the actions, Z and
    /// Shift+Z step the target.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>A target's ways, merged across the selection (one way two agents can give is one), and the quick click's choice.</summary>
        public sealed class ContextPlan
        {
            public Target T; public ActionPanel Pn; public ContextRank.Choice Choice;
            public readonly List<int> Rep = new();                 // a way's row in Pn.Rows (the primary agent's when it can give it)
            public readonly List<List<OpId>> Givers = new();       // who can give each way
            public PanelRow Row(int way) => way >= 0 && way < Rep.Count ? Pn.Rows[Rep[way]] : null;
        }

        /// <summary>The ways on a target for the selection, ranked (the panel's rows, so the chip, the ring and the panel agree).</summary>
        public ContextPlan ContextFor(Target t, Vector2 screen)
        {
            var plan = new ContextPlan { T = t, Pn = BuildPanel(t, screen) };
            var groups = new Dictionary<string, int>(); var ways = new List<ContextRank.Way>();
            for (int i = 0; i < plan.Pn.Rows.Count; i++)
            {
                var r = plan.Pn.Rows[i]; var o = r.I?.Order; if (o == null) continue;
                string key = r.Available ? $"{o.Do}|{o.Approach}|{o.How}|{o.Target}|{o.To}|{o.Point}" : "locked|" + i;
                if (groups.TryGetValue(key, out var gi)) { plan.Givers[gi].Add(r.By); if (r.By == Agent) plan.Rep[gi] = i; continue; }
                groups[key] = plan.Rep.Count; plan.Rep.Add(i); plan.Givers.Add(new List<OpId> { r.By });
            }
            for (int w = 0; w < plan.Rep.Count; w++)
            {
                var r = plan.Row(w); var i = r.I;
                ways.Add(new ContextRank.Way
                {
                    Do = i.Order.Do ?? "", Approach = i.Order.Approach ?? "", How = i.How ?? "", Outcome = i.Outcome ?? "", Available = r.Available,
                    Mine = plan.Givers[w].Contains(Agent), Usd = ContextRank.Num(i.Usd), Trail = ContextRank.Num(i.Trail), Orders = i.Orders, Index = w,
                });
            }
            plan.Choice = ContextRank.Choose(ways, t.Kind);
            return plan;
        }

        /// <summary>The hover chip for a target with no verb picked: exactly what the quick click will do (More: other ways are one hold away;
        /// Opens: the click opens the ring and takes nothing).</summary>
        Intent ContextIntent(Target t)
        {
            ContextPlan plan = null; Guard("price", () => plan = ContextFor(t, lastHover)); if (plan == null) return null;
            var ch = plan.Choice; int more = ch.Order.Count;
            switch (ch.Act)
            {
                case ContextRank.Act.Default: { var i = plan.Row(ch.Pick).I; i.More = more > 1; return i; }
                case ContextRank.Act.Ring: { var i = plan.Row(ch.Pick).I; i.More = true; i.Opens = true; return i; }
                case ContextRank.Act.Lock: { var r = plan.Row(ch.Pick); return new Intent { Label = t.Label, Hint = r?.Reason ?? "" }; }
                case ContextRank.Act.Hint: return new Intent { Label = t.Label, Hint = WhoCan(t) };
                default: return new Intent { Label = t.Label, More = more > 0 };
            }
        }

        /// <summary>The quick right click (released under 0.22 s), or E on the focus: the target's one act, else the ring, else nothing.</summary>
        public void ActContext(Target t, Vector2 screen)
        {
            if (!ModalGate.AllowsClicks(Modal) || Game == null || Fault != null) return;
            if (t != null && t.Kind == "op") { if (CarryDowned(t, screen)) return; t = null; }
            if (t == null) { if (Panel != null) { ClosePanel(); return; } Cancel(); return; } // ground: stop (drop the picked verb, else stop walks)
            if (Panel == null && BoardSelection(t, screen)) return; // brief 018: the whole cell selected, the car boards each
            if (Picked >= 0)
            {
                Intent i = null; Guard("price", () => i = IntentFor(t)); // the target given (the focus in third person), not what is under the cursor
                if (i != null && i.Actionable && !controls.quickAction && i.Order.Do != "fire" && Harmful(i.Order, t, AnyDrawn())) { Picked = -1; intentKey = ""; RightRing(t, screen); return; } // brief 038 §4.5: a picked Kill or Pressure still goes through the ring
                if (i != null && i.Actionable) { Picked = -1; intentKey = ""; if (i.Order.Do == "go") GoGive(Agent, screen, "right", i.Order.Target); else if (!SeenGate(i, t, screen, "right")) Give(Agent, i.Order, t, screen); return; } // a picked action wins (Shift+1–4, Tab), spent by the click
                PickedRefused(i, t, screen); return; // brief 038 §4.5: it can't apply: its lock, and never another act
            }
            if (controls.panelOnRight) { OpenPanel(t, screen); return; } // Sky may bring the panel back (controls.panelOnRight)
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, screen)) || plan == null) return;
            var ch = plan.Choice;
            switch (ch.Act)
            {
                case ContextRank.Act.Default: DoWay(plan.Row(ch.Pick), t, screen, plan.Pn.World, "right"); return;
                case ContextRank.Act.Ring: OpenRing(plan, screen, false); return;
                case ContextRank.Act.Lock: LockPop(plan.Row(ch.Pick), screen, plan.Pn.World); return;
                case ContextRank.Act.Hint:
                    { var sel = new List<int>(); for (int k = 0; k < 4; k++) if (Selected[k]) sel.Add(k); Pop(WhoCan(t, sel), screen, null, Feedback.Info, 2f); return; }
                default: return; // a passer-by, a unit, a stash with nothing carried: silent
            }
        }

        /// <summary>A way taken (the panel's row, a ring wedge, a quick click): to that row's agent; a locked row says its shortest lock.</summary>
        void DoRow(PanelRow r, Target t, Vector2 at, Vector3? world, string source)
        {
            if (r == null) return;
            if (!r.Available) { LockPop(r, at, world); return; }
            if (r.I.Order?.Do == "go") { GoGive(r.By, at, source, r.I.Order.Target); return; } // brief 018: the commit hold when anyone stays
            if (SeenGate(r.I, t, at, source)) return;
            if (r.I.Order?.Do == "fire") { FireRow(r.By, r.I.Order, t, at, source); return; } // a Fire way: the fire path (the going-loud card or the wedge's hold; the engine walks it into reach)
            Give(r.By, r.I.Order, t, at);
        }

        void LockPop(PanelRow r, Vector2 at, Vector3? world)
        {
            if (r == null) return;
            if (r.Lock.code is "car-held" or "car-boxed") HolderPulseAt = Time.unscaledTime;
            var full = r.Unlock.Length > 0 ? r.Unlock : $"{r.Verb}: {r.Reason}";
            popCode = r.Lock.code ?? ""; Pop(Text?.ShortLock(r.Lock.code) is string ? ShortOf(r.I.Order, r.Lock) ?? full : full, at, world, Feedback.Refused, 2f, PopIcon(r.Lock.code ?? ""), full);
        }

        // ---------------------------------------------------------------- the ring

        /// <summary>One wedge: a way (its row, who can give it), or the full panel (Way = -1).</summary>
        public sealed class RingSeg
        {
            public int Way = -1; public PanelRow Row; public readonly List<OpId> By = new(); public string Word = "", Icon = "";
            public string Named = "";
            public bool Violent, Draws, More, Harmful, HoldWedge; public int Key = -1;
            /// <summary>Brief 057: Shut = a way shown shut (Go while the car is held: struck, dim, never taken; its pill shows the lock and the holders);
            /// Focus = the way moves something the ring is not on (Reroute: the patrol), shown in the world on hover; Obj = that thing's cameo badge.</summary>
            public bool Shut, Focus, ObjStruck; public string Obj = "", Holders = "", ShutText = "", ShutIcon = "p_time"; /* ShutText: a shut Loop's end (m:ss), shown after its lock; brief 067 E: any shut way's short lock (ShutIcon "" then) */ // HoldWedge: Kill, Fire on a non-combatant: the 0.5 s hold (brief 038 §4.3)
        }
        public sealed class RingState
        {
            public ContextPlan Plan; public Vector2 Gui, Screen; public float OpenedAt; public bool Sticky, ByE, FromHold, Always, ByClick; public int Hover = -1, Lit = -1; /** ByClick: opened by a left click (item 33): a double click's second press in its middle keeps it */
            public Vector2 PointerAtOpen; public readonly List<RingSeg> Segs = new();
            /// <summary>A harmful wedge's hold under way (HostAim.RingHoldFrame): which wedge, since when, by what input (left, right, E); Ctrl: Ctrl + right's Fire.</summary>
            public int HoldSeg = -1; public float HoldStart = -1f; public string HoldSrc = ""; public bool HoldCtrl;
        }
        /// <summary>The wedge the pointer is on (else the lit one) when its word names a target among same-verb wedges: the thing to show in
        /// the world (Sky 2026-10-22: hovering a Loop wedge shows which camera). `view` is its view key ("camera/entrance", a person's key),
        /// `watcher` its watcher id when it has sight ("camera:entrance"), else "".</summary>
        public (string view, string watcher)? RingFocus
        {
            get
            {
                var rs = Ring; if (rs == null || Game == null) return null; int i = rs.Hover >= 0 ? rs.Hover : rs.Lit;
                if (i < 0 || i >= rs.Segs.Count) return null; var s = rs.Segs[i]; if (s.More || s.Named.Length == 0 && !s.Focus || s.Row?.I?.Order is not Order o || o.Target.Length == 0) return null; // brief 057 §2: Reroute shows the patrol
                bool cam = o.Do is "loop" or "disable" || Game.S.Cameras.Exists(c => c.Id == o.Target);
                return cam ? ("camera/" + o.Target, "camera:" + o.Target) : (o.Target, Game.WatcherKind(o.Target).Length > 0 ? o.Target : "");
            }
        }

        /// <summary>The open ring, else null (the HUD draws it; the host reads the pointer against it).</summary>
        public RingState Ring { get; private set; }
        public const float RingHoldS = 0.22f, RingMovePx = 6f, RingDeadPx = 18f, RingStickyS = 0.5f, RingOuter = 84f, RingInner = 30f;
        const int RingMaxWays = 5;

        /// <summary>The kit's px per screen px (HudToolkit's integer scale).</summary>
        static float KitScale => Mathf.Max(1f, Mathf.Round(Screen.height / 1080f));

        void OpenRing(ContextPlan plan, Vector2 screen, bool byE)
        {
            if (plan == null) return;
            var rs = new RingState { Plan = plan, OpenedAt = Time.unscaledTime, ByE = byE };
            var ch = plan.Choice; bool drawn = AnyDrawn();
            // brief 067 E2 (Glyph; Suture's 0.2.122 findings §2, Sky: "there are times I couldn't perform action on Inez"): with no calm way open
            // for the selection, its locked calm ways are the ring's first wedges, shut (struck, dim, the short lock), at most five, the best-ranked
            // at 12 o'clock; the open violent ways follow, last and dashed, keeping at least one slot. The open ring is never widened
            bool calmOpen = ch.Order.Exists(w => plan.Row(w)?.I?.Order is Order co && !ContextRank.Violent(new ContextRank.Way { Do = co.Do }));
            if (!calmOpen)
            {
                var shut = ShutCalmSegs(plan); int cap = RingMaxWays - (ch.Order.Count > 0 ? 1 : 0); // Glyph 067 Rev 2 E2: up to the ring's five ways (the open violent ways keep at least one slot)
                for (int i = 0; i < shut.Count && i < cap; i++) rs.Segs.Add(shut[i]);
            }
            foreach (var w in ch.Order)
            {
                if (rs.Segs.Count >= RingMaxWays) break;
                var r = plan.Row(w); var o = r.I.Order;
                var s = new RingSeg { Way = w, Row = r, Word = RingWord(r.I), Icon = r.I.Lead.Length > 0 ? r.I.Lead : HudModel.AtlasIcon(o.Do, o.Approach, ""), Violent = o.Do is "takedown" or "fire" };
                s.By.AddRange(o.Do == "fire" ? FireFaces(o, plan.Givers[w]) : plan.Givers[w]); // a Fire wedge shows who it will reach (HostSquad)
                s.Draws = o.Do == "fire" && !drawn; // the draw is in the price (brief 030 §5): the pill's "draws 0.6 s" (DrawS)
                s.Harmful = Harmful(o, plan.T, drawn); // brief 038 §4.3: never lit, never taken by the second click
                s.HoldWedge = HoldOn ? o.Do == "fire" : o.Do == "takedown" && o.How == "kill" || o.Do == "fire" && !Combatant(plan.T); // brief 041 (Sky): Kill and Knock out are on a click in the ring; only Fire is held // brief 024 rule 2, brief 038 §4.3: Kill and Fire on a non-combatant take the 0.5 s hold
                if (o.Do == "go" && r.I.Outcome == "withdraw") { s.Harmful = true; s.HoldWedge = true; s.Obj = "badges/npc_councillor"; s.ObjStruck = true; } // brief 067 C5b (Glyph Rev 2, Aster 2026-10-26): Go before Vale ends the attempt: last (ContextRank tier 9), never lit, a struck Vale cameo, the 0.5 s hold; no dashed outline and no loud mark (those are the violent ways')
                s.Key = r.Key >= 0 && Selected[(int)r.By] ? r.Key : -1;
                rs.Segs.Add(s);
            }
            // Sky on the cabinet's ring (2026-10-22: three "Loop · 66 s" wedges, "looping what?"): a word two wedges share names its target, in
            // the intent's own words (text.json's target names: "Loop the entrance camera", "Loop the square's mast camera")
            foreach (var s in rs.Segs)
            {
                if (rs.Segs.FindAll(x => x.Word == s.Word).Count < 2 || s.Row?.I == null) continue;
                var it = s.Row.I; string tn = it.Order?.Target ?? "";
                s.Named = it.Label.Length > 0 && it.Label != s.Word ? it.Label : tn.Length > 0 ? $"{s.Word} {(it.Order?.Do is "loop" or "disable" ? CameraName(tn) : Name(tn))}" : s.Word;
            }
            foreach (var s in rs.Segs) if (s.Named.Length > 0) s.Word = s.Named;
            // brief 057 §2: a way whose object is not what the ring is open on wears that object's cameo after its word (Reroute: the patrol;
            // Loop from the cabinet: the camera; Call: the guard or the driver's car); Reroute names the patrol for the focus marks
            foreach (var s in rs.Segs)
            {
                var o2 = s.Row?.I?.Order; if (o2 == null) continue;
                if (o2.Do == "reroute") { s.Obj = "badges/npc_patrol_face"; /* Glyph, brief 057 §5: the face in the cameo's own ring, no amber on the pill */ s.Focus = true; }
                else if (o2.Do is "loop" or "disable" && plan.T?.Kind == "cabinet") s.Obj = "badges/signal_camera";
                else if (o2.Do == "call") s.Obj = o2.Target == "driver" ? "icon:car" : "badges/signal_guard";
            }
            // brief 057 §4.2: Go while the car is held shows shut (struck, dim), its pill the lock and one badge a holder; a press says why and pulses them
            if (plan.Pn.Rows.Find(r => r.I?.Order?.Do == "go" && !r.Available && r.Lock.code == "car-held") is PanelRow gr && !rs.Segs.Exists(x => x.Row?.I?.Order?.Do == "go") && rs.Segs.Count < RingMaxWays)
                rs.Segs.Insert(0, new RingSeg { Way = -1, Row = gr, Word = RingWord(gr.I), Icon = "car", Shut = true, Holders = gr.Lock.ids ?? "" });
            // engine 0.31.0 (brief 058 §2, Glyph 058 §1): one loop at a time: the Tech's Loop on another camera while one runs shows shut (struck,
            // dim), its pill the lock and the time the running loop ends; a press says why (loop-holds)
            if (plan.T?.Kind == "camera" && plan.Pn.Rows.Find(r => r.I?.Order?.Do == "loop" && !r.Available && r.Lock.code == "loop-holds") is PanelRow lr && !rs.Segs.Exists(x => x.Row?.I?.Order?.Do == "loop") && rs.Segs.Count < RingMaxWays)
                rs.Segs.Insert(0, new RingSeg { Way = -1, Row = lr, Word = RingWord(lr.I), Icon = "loop", Shut = true, ShutText = lr.Lock.tick >= 0 ? MissionClock(lr.Lock.tick) : "" });
            if (plan.T?.Kind != "camera" || rs.Segs.Count == 0) rs.Segs.Add(new RingSeg { More = true, Icon = "", Word = "" }); // ⋯: the full panel, every locked row and its reason (Glyph 058 §1: a camera's ring is its ways alone: Loop · Disable · Fire, or Fire as the whole ring)
            rs.Lit = ch.Act == ContextRank.Act.Ring || ch.Act == ContextRank.Act.Default ? Math.Max(0, rs.Segs.FindIndex(x => x.Way == ch.Pick)) : controls.quickAction ? 0 : -1; // ring by default: nothing ranked (a passer-by's takedowns), nothing lit
            if (rs.Segs.Count == 1) rs.Lit = 0;
            if (Picked >= 0) { int ai = rs.Segs.FindIndex(x => x.Key == Picked && x.By.Contains(Agent)); if (ai >= 0) rs.Lit = ai; } // brief 030 (no plate): the ring opens on the armed verb's wedge
            if (rs.Lit >= 0 && rs.Lit < rs.Segs.Count && (rs.Segs[rs.Lit].Harmful || rs.Segs[rs.Lit].Shut)) rs.Lit = -1; // brief 038: a harmful way is never lit (the second click takes nothing)
            // never off screen: it slides in to fit (the pills reach ~230 px past the wedges)
            float k = KitScale, mx = (RingOuter + 250f) * k, my = (RingOuter + 40f) * k;
            var c = new Vector2(Mathf.Clamp(screen.x, Mathf.Min(mx, Screen.width / 2f), Mathf.Max(Screen.width - mx, Screen.width / 2f)), Mathf.Clamp(screen.y, Mathf.Min(my, Screen.height / 2f), Mathf.Max(Screen.height - my, Screen.height / 2f)));
            rs.Screen = c; rs.Gui = new Vector2(c.x, (cam ? cam.pixelHeight : Screen.height) - c.y);
            rs.PointerAtOpen = UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? screen;
            if (Panel != null) ClosePanel();
            Ring = rs; Status = $"{OneWord(plan.T)}: choose";
        }

        /// <summary>Brief 067 E2 (Glyph): the shut wedges of the selection's locked calm ways: one a verb, target and lock code (the agents who have it
        /// merged: their cameos), Ask, Pay, Help and Pressure under one lock merged into one Turn wedge (two or more; a lone approach keeps its
        /// word), ranked by ContextRank.Compare. Go held keeps its holders (and the rivals' countdown), the one-loop Loop its end (as the 057 and
        /// 058 inserts did); every other way prints its short lock after the lock (ShortOf: {tick} {m} {usd} filled).</summary>
        List<RingSeg> ShutCalmSegs(ContextPlan plan)
        {
            var groups = new List<(string key, List<PanelRow> rows)>();
            if (plan?.Pn == null) return new List<RingSeg>();
            foreach (var r in plan.Pn.Rows)
            {
                if (r.Available || r.I?.Order is not Order o || ContextRank.Violent(new ContextRank.Way { Do = o.Do })) continue;
                string key = (o.Do == "turn" ? "turn" : o.Do + "|" + o.Approach) + "|" + o.Target + "|" + o.How + "|" + (r.Lock.code ?? "");
                int gi = groups.FindIndex(g => g.key == key); if (gi < 0) groups.Add((key, new List<PanelRow> { r })); else groups[gi].rows.Add(r);
            }
            var segs = new List<(RingSeg seg, ContextRank.Way way)>();
            foreach (var (key, rows) in groups)
            {
                var rep = rows.Find(x => x.By == Agent) ?? rows[0]; var o = rep.I.Order;
                bool turnGroup = o.Do == "turn" && rows.ConvertAll(x => x.I.Order.Approach).FindAll(a => a != null).Count > 1 && new HashSet<string>(rows.ConvertAll(x => x.I.Order.Approach ?? "")).Count > 1;
                string word = turnGroup ? (Text != null && Text.Actions.TryGetValue("ring-turn", out var rt) && rt.Label.Length > 0 ? rt.Label : "Turn") : RingWord(rep.I);
                string icon = turnGroup ? "turn_person" : rep.I.Lead.Length > 0 ? rep.I.Lead : HudModel.AtlasIcon(o.Do, o.Approach, "");
                var sg = new RingSeg { Way = -1, Row = rep, Word = word, Icon = icon, Shut = true };
                foreach (var x in rows) if (!sg.By.Contains(x.By)) sg.By.Add(x.By);
                if (rep.Lock.code == "car-held") { sg.Icon = "car"; sg.Holders = rep.Lock.ids ?? ""; } // brief 057 §4.2: the holders' badges (and the rivals' countdown) say it
                else if (rep.Lock.code == "loop-holds") { sg.Icon = "loop"; sg.ShutText = rep.Lock.tick >= 0 ? MissionClock(rep.Lock.tick) : ""; } // brief 058 §1: when the running loop ends
                else { sg.ShutText = ShortOf(o, rep.Lock) ?? rep.Reason; sg.ShutIcon = ""; }
                segs.Add((sg, new ContextRank.Way { Do = o.Do, Approach = o.Approach ?? "", How = o.How ?? "", Outcome = rep.I.Outcome ?? "", Available = false, Mine = sg.By.Contains(Agent), Usd = ContextRank.Num(rep.I.Usd), Trail = ContextRank.Num(rep.I.Trail), Orders = rep.I.Orders, Index = plan.Pn.Rows.IndexOf(rep) }));
            }
            segs.Sort((a, b) => ContextRank.Compare(a.way, b.way));
            return segs.ConvertAll(x => x.seg);
        }

        /// <summary>Brief 038 §4.3 (Glyph): a way never lit and never taken blind: Pressure, Kill, Fire on anyone not a combatant, any Fire with the
        /// gun holstered.</summary>
        bool Harmful(Order o, Target t, bool drawn) => o.Do == "turn" && o.Approach == "pressure" || o.Do == "takedown" && o.How == "kill" || o.Do == "fire" && (!drawn || !Combatant(t));
        /// <summary>A combatant: a final-fight hostile, or security and the response once a fight is on.</summary>
        bool Combatant(Target t) => t != null && (t.Kind == "hostile" || Game != null && Game.FightOn && t.Kind is "unit" or "security" or "patrol" or "rivals");

        /// <summary>One word for a wedge (Fable's labels): Listen, Watch, Ask, Pay, Help, Pressure, Loop, Reroute, Take, Enter, Board, Go, Carry,
        /// Tie, Hide, Drop, Knockout, Kill, Fire.</summary>
        string RingWord(Intent i)
        {
            var o = i.Order; string key = o.Do == "turn" ? o.Approach : o.Do;
            if (Text != null && Text.Actions.TryGetValue("ring-" + key, out var a) && a.Label.Length > 0) return a.Label;
            return key switch
            {
                "takedown" => o.How == "kill" ? "Kill" : "Knockout", "restrain" => "Tie", "stash" => "Hide", "drop" => "Drop", "mislead" => "Tip", "drive-car" => "Drive", // brief 067 C: the car's own Drive wedge (text.json has no ring-drive-car)
                "act" => "Enter", _ => key.Length > 0 ? char.ToUpperInvariant(key[0]) + key[1..] : "",
            };
        }

        /// <summary>The draw's seconds (the ring's "draws 0.6 s" before a holstered Fire).</summary>
        public float DrawS => Game == null ? 0f : Game.R.DrawTicks / (float)Game.Hz;

        /// <summary>The portrait's sidearm mark clicked: that one agent draws, or holsters (F does the selection).</summary>
        public void DrawOrHolsterAgent(int k)
        {
            if (Game == null || k < 0 || k > 3 || Loud != null || !ModalGate.AllowsClicks(Modal)) return;
            if (!Armed(k)) { PopKey("unarmed", null, frame.World(Game.Ops[k].Pos), Feedback.Info, "Unarmed."); return; }
            bool d = Drawn(k); var o = new Order { Op = Op.Name((OpId)k), Do = d ? "holster" : "draw" };
            Fight(new List<Order> { o }, "left", null, frame.World(Game.Ops[k].Pos), d ? "holster" : "draw");
        }

        /// <summary>Brief 030 (no plate): the stance fan on a portrait's sidearm mark: that one agent's stance (hold, return, free), an engine order as V gives.</summary>
        public void SetStanceAgent(int k, string stance)
        {
            if (Game == null || k < 0 || k > 3 || Loud != null || !ModalGate.AllowsClicks(Modal) || Array.IndexOf(Stances, stance) < 0 || OneInput) return; // engine 0.35: no stances
            var at = frame.World(Game.Ops[k].Pos);
            if (!Armed(k)) { PopKey("unarmed", null, at, Feedback.Info, "Unarmed."); return; }
            if (Game.Ops[k].Gun.Stance == stance) return;
            Send(new List<Order> { new Order { Op = Op.Name((OpId)k), Do = "stance", Stance = stance } }, null, at);
            PopKey("stance-" + stance, null, at, Feedback.Info, $"stance: {StanceName(stance)}");
        }

        /// <summary>Seconds left in a watcher's warning to holster (the lab's police warning), else -1.</summary>
        public int WarnS(int k)
        {
            if (Game == null) return -1; int best = -1;
            foreach (var w in Game.Warnings) if ((int)w.Op == k && w.Until > Game.Tick) { int s = (w.Until - Game.Tick + Game.Hz - 1) / Game.Hz; if (s > best) best = s; }
            return best;
        }

        public void CloseRing() { if (Ring != null) { Ring = null; views?.ShowPath(null); PreviewWalk = null; } }

        /// <summary>Suture's bot triage (0.2.115): an open ring was never rechecked, so a wedge whose giver went down (carried, inside, in the car) a
        /// second before the click still looked open. Each frame the open ring's givers are checked; when one is no longer able the ring is built
        /// again on the same thing at the same place (the panel never offers an unable agent, so the way re-ranks to another giver or goes), its
        /// state kept (sticky, by click, by hold). Counted for checks.</summary>
        public int RingRebuilds { get; private set; }
        void RingGiversFrame()
        {
            var rs = Ring; if (rs == null || Game == null || rs.Plan?.T == null) return;
            bool stale = false;
            foreach (var sg in rs.Segs) { if (sg.More || sg.Shut) continue; foreach (var k in sg.By) if (!Able((int)k)) { stale = true; break; } if (stale) break; }
            // the 0.2.117 bot row (Q-OFFER no-target x2): a Fire wedge on a camera or car another agent has just shot out; the ring is built again (Locked then shows the engine's no-target)
            if (!stale && Time.frameCount % 5 == 0) foreach (var sg in rs.Segs) { var fo = sg.Row?.I?.Order; if (sg.More || sg.Shut || fo?.Do != "fire" || sg.Row?.Available != true || !(fo.Target.StartsWith("camera:") || fo.Target.StartsWith("car:"))) continue; if (NoFireTarget(sg.Row.By, fo.Target)) { stale = true; break; } }
            if (!stale) return;
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(rs.Plan.T, rs.Screen)) || plan == null) { CloseRing(); return; }
            var keep = (rs.Sticky, rs.Always, rs.ByClick, rs.FromHold, rs.ByE, rs.PointerAtOpen, rs.OpenedAt);
            OpenRing(plan, rs.Screen, keep.ByE); RingRebuilds++;
            var nr = Ring; if (nr == null) return;
            (nr.Sticky, nr.Always, nr.ByClick, nr.FromHold) = (keep.Sticky, keep.Always, keep.ByClick, keep.FromHold); nr.PointerAtOpen = keep.PointerAtOpen; nr.OpenedAt = keep.OpenedAt;
        }

        /// <summary>The wedge under a screen point, -1 in the dead zone or off the ring's reach.</summary>
        int RingSegAt(Vector2 screen)
        {
            var rs = Ring; if (rs == null || rs.Segs.Count == 0) return -1;
            var d = (screen - rs.Screen) / KitScale; d.y = -d.y; // GUI-like: y down
            float r = d.magnitude; if (r < RingDeadPx || r > RingOuter + 260f) return -1;
            float ang = Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg; if (ang < 0) ang += 360f; // 0 at 12 o'clock, clockwise
            int n = rs.Segs.Count; float w = 360f / n;
            return Mathf.FloorToInt(Mathf.Repeat(ang + w / 2f, 360f) / w) % n;
        }

        void RingTake(int seg)
        {
            var rs = Ring; if (rs == null || seg < 0 || seg >= rs.Segs.Count) return;
            var s = rs.Segs[seg]; var plan = rs.Plan; var at = rs.Screen; SideTake(s, plan?.T); CloseRing();
            if (s.More) { OpenPanel(plan.T, at); return; }
            if (HoldOn && s.Way >= 0) { HoldGive(plan, s.Way, plan.T, at, ringRun, "left"); return; } // brief 041: Shift on a wedge runs there
            DoWay(s.Row, plan.T, at, plan.Pn.World, "left");
        }

        /// <summary>Proofs: the open ring's first wedge giving `verb` (on `target` when given) taken as a left click on it takes it; false when none.</summary>
        public bool RingTakeScripted(string verb, string target = null)
        {
            var rs = Ring; if (rs == null) return false;
            for (int i = 0; i < rs.Segs.Count; i++) { var o = rs.Segs[i].Row?.I?.Order; if (o != null && o.Do == verb && (target == null || o.Target == target)) { RingTakeTry(i, "left"); return true; } }
            return false;
        }
        /// <summary>Proofs: the open ring's wedges, "~" before a shut one ("Turn (Alarm up)"), "..." the panel.</summary>
        public string RingWords => Ring == null ? "" : string.Join(" | ", Ring.Segs.ConvertAll(x => x.More ? "..." : (x.Shut ? "~" : "") + x.Word + (x.Shut && x.ShutText.Length > 0 ? " (" + x.ShutText + ")" : "") + (x.Violent ? " [dashed]" : "")));
        /// <summary>Proofs: the screen point of the open ring's first wedge giving `verb` (on `target`), for the pointer to rest on; null when none.</summary>
        public Vector2? RingWedgeScreen(string verb, string target = null)
        {
            var rs = Ring; if (rs == null) return null;
            for (int i = 0; i < rs.Segs.Count; i++) { var o = rs.Segs[i].Row?.I?.Order; if (o != null && o.Do == verb && (target == null || o.Target == target)) return WedgeScreen(rs, i); }
            return null;
        }

        /// <summary>Shift+1–4 inside the ring: the selected agent's way on that action slot.</summary>
        void RingKey(int slot)
        {
            var rs = Ring; if (rs == null) return;
            for (int i = 0; i < rs.Segs.Count; i++) if (rs.Segs[i].Key == slot) { if (!RingKeyFire(rs, i)) RingTake(i); return; }
            var plan = rs.Plan; // not one of the five shown: the agent's row in the plan
            for (int w = 0; w < plan.Rep.Count; w++) { var r = plan.Row(w); if (r.By == Agent && r.Key == slot) { CloseRing(); DoRow(r, plan.T, rs.Screen, plan.Pn.World, "left"); return; } }
            PopKey("nothing-here", null, plan.Pn.World, Feedback.Info, $"Nothing for the {AgentNames[(int)Agent]} there.");
        }

        // ---------------------------------------------------------------- the right button (press, hold, release)

        Target rTarget; Vector2 rPressAt; float rPressT = -1f; bool rPending, rRingThisPress;

        /// <summary>Per frame: the right button's press, hold and release, and the ring under the pointer. True when the ring took the input.</summary>
        bool RightFrame(UnityEngine.InputSystem.Mouse mouse, Vector2 p, bool onHud)
        {
            var kb = Keyboard.current;
            if (Ring != null)
            {
                if (RingHoldFrame(mouse, p)) return true; // a harmful wedge's hold under way (HostAim)
                var rs = Ring; if (rs == null) return true; rs.Hover = RingSegAt(p);
                if (rs.Hover >= 0 && rs.Hover != rs.Lit && rs.Segs[rs.Hover].Row is PanelRow hr) RingPath(hr); else if (rs.Hover < 0) views?.ShowPath(null);
                bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
                if (esc) { CloseRing(); return true; }
                // Sky 2026-10-21 (ring by default): Enter takes the lit way (the one under the pointer, else the likely one)
                if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) { RingTakeTry(rs.Hover >= 0 ? rs.Hover : rs.Lit, "release"); return true; }
                bool middle = ((p - rs.Screen) / KitScale).magnitude < RingDeadPx; // the dead zone: the likely way stays lit
                if (rs.FromHold && mouse.rightButton.wasReleasedThisFrame)
                {
                    rs.FromHold = false;
                    if (rs.Hover >= 0 && (!rs.Always || (p - rs.PointerAtOpen).magnitude > RingDeadPx * KitScale)) { RingTakeTry(rs.Hover, "release"); return true; } // a drag onto a wedge (a harmful one: held, never on release)
                    if (rs.Always || Time.unscaledTime - rs.OpenedAt < RingStickyS && (p - rs.PointerAtOpen).magnitude < RingMovePx * KitScale) { rs.Sticky = true; return true; }
                    CloseRing(); return true;
                }
                if (mouse.leftButton.wasPressedThisFrame && rs.ByClick && middle && Time.unscaledTime - rs.OpenedAt < controls.doubleSeconds) return true; // item 33: the second click of a double click on the target keeps its ring (nothing taken)
                if (mouse.leftButton.wasPressedThisFrame) { if (rs.Hover >= 0) RingTakeTry(rs.Hover, "left"); else if (rs.Always && middle) RingTakeTry(rs.Lit, "left"); else CloseRing(); return true; }
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    if (controls.Modern && !rs.Always) return RingRightDbl(p); // brief 041: the wedge under it, else close (a quick release elsewhere opens that ring)
                    if (rs.Always) // brief 038 §4.1: a second right click on the same target takes the lit way; on another target the ring moves there; on the ground it closes
                    {
                        if (rs.Hover >= 0) { RingTakeTry(rs.Hover, "right"); return true; }
                        var t2 = middle ? rs.Plan.T : TargetAt(p);
                        if (t2 != null && SameTarget(t2, rs.Plan.T)) { RingTakeTry(rs.Lit, "right"); return true; }
                        CloseRing(); if (t2 != null && t2.Kind != "op" && !OverHud(p)) RightRing(t2, p);
                        return true;
                    }
                    if (rs.Hover >= 0 && rs.Sticky) RingTakeTry(rs.Hover, "right"); else CloseRing(); return true;
                }
                return true; // a ring open: the world takes no click
            }
            if (mouse.rightButton.wasPressedThisFrame && !onHud)
            {
                RightClickUsed = true; // brief 030: the first-act hint retires
                if (!ModalGate.AllowsClicks(Modal) || Game == null || Fault != null || !cam) return false;
                lastRight = p; var t = TargetAt(p);
                if (CtrlFireRing(t, p)) return true; // brief 038 §4.3: Ctrl + right opens the ring with Fire held, never an instant shot
                bool shift = controls.Held(kb, PlayAction.AddToGroup), fire = IsFire(t) && (controls.quickAction || PressFires(t));
                if (controls.panelOnRight || (Picked >= 0 && !shift) || fire) { PointerRight(p); return false; } // Fire acts on press (the going-loud hold and the sweep belong to it)
                if (controls.Modern) { RightTapBegin(t, p); return true; } // brief 041: the ring opens on a quick release (a hold opens nothing)
                if (!controls.quickAction) { RightRing(t, p); return true; } // Sky 2026-10-21: a right click opens the ring at the target, the likely way lit
                if (shift && t != null && t.Kind != "op") { ContextPlan plan = null; if (Guard("panel", () => plan = ContextFor(t, p)) && plan != null) { OpenRing(plan, p, false); if (Ring != null) Ring.Sticky = true; } return true; } // Shift + right: the ring at once, to look
                rTarget = t; rPressAt = p; rPressT = Time.unscaledTime; rPending = true; rRingThisPress = false;
                return true;
            }
            if (RightTapFrame(mouse, p)) return true; // brief 041: the tap's release
            if (rPending)
            {
                if (!mouse.rightButton.isPressed) { rPending = false; ActContext(rTarget, rPressAt); return true; } // a click: the one act
                if ((p - rPressAt).magnitude > RingMovePx * KitScale) { rPending = false; return true; } // moved: acts on nothing (an orbit, if one comes back)
                if (Time.unscaledTime - rPressT >= RingHoldS)
                {
                    rPending = false; var t = rTarget;
                    if (t == null || t.Kind == "op") return true; // a hold on the ground opens nothing
                    ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, rPressAt)) || plan == null) return true;
                    if (plan.Choice.Order.Count == 0) { ActContext(t, rPressAt); return true; } // every way locked: the lock pop, no ring
                    OpenRing(plan, rPressAt, false); if (Ring != null) { Ring.FromHold = true; rRingThisPress = true; }
                    return true;
                }
                return true;
            }
            return false;
        }

        /// <summary>Sky 2026-10-21 (after 0.2.19: "wasn't clear to me I had to hold down … I made a lot of mistakes"): the right press opens the
        /// ring at the target with the likely way lit (ContextRank's pick, at 12 o'clock); a second right click, Enter, or a left click on the lit
        /// wedge takes it; a drag released over a wedge takes that one. On the ground, on an agent, or where nothing is open (every way locked, a
        /// passer-by, someone else's act) the press does what the quick click did: stop walks, carry the downed, the lock's pop, who can.
        /// Fire stays on press (IsFire, before this): one way, a sweep across hostiles, and the going-loud hold guards the first shot.</summary>
        static bool SameTarget(Target a, Target b) => a != null && b != null && a.Kind == b.Kind && a.Key == b.Key && a.Id == b.Id;

        void RightRing(Target t, Vector2 p)
        {
            if (t == null || t.Kind == "op" || Panel != null) { ActContext(t, p); return; }
            if (BoardSelection(t, p)) return; // brief 018: the whole cell selected, the car boards each (one way, no ring)
            ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, p)) || plan == null) return;
            if (plan.Choice.Order.Count == 0) { ActContext(t, p); return; }
            OpenRing(plan, p, false); if (Ring != null) { Ring.FromHold = true; Ring.Always = true; }
        }

        /// <summary>The hover marker (Sky 2026-10-21: "I thought this marker would change to show what action right click would cause"): the
        /// glyph of what a right click on the hovered thing takes or lights (the hover chip's way: ContextRank's pick, or the armed action), "⋯"
        /// when the ring has others, footprints when the agent walks there first, and the holstered mark when Fire draws first. Empty: the
        /// plain ring (nothing this selection can do there).</summary>
        public (string glyph, bool more, bool walk, bool draws) HoverAct
        {
            get
            {
                var t = Hover; if (t == null || Game == null || Ring != null || Panel != null || t.Kind == "op") return ("", false, false, false);
                if (t.Kind == "person" && t.Key == "councillor" && AimLine != null) return ("", false, false, false);
                if (IsFire(t) && (controls.quickAction || PressFires(t) || PickedId == "fire"))
                {
                    bool far = false; var at = PosOf(t);
                    if (at.HasValue) for (int k = 0; k < 4; k++) if (Selected[k] && Armed(k)) { var op = Game.Ops[k]; far |= !G.Within(op.Pos, at.Value, op.Gun.W.MaxCm) || !Game.S.Sight(op.Pos, at.Value, Game.R.Eye); }
                    return ("fire", false, far, !AnyDrawn());
                }
                var i = HoverIntent; if (i == null || !i.Actionable) return ("", false, false, false);
                var o = i.Order; string g = i.Lead.Length > 0 ? i.Lead : HudModel.AtlasIcon(o.Do, o.Approach, "");
                if (Picked < 0 && !controls.quickAction && Harmful(o, t, AnyDrawn())) return ("", true, false, false); // brief 038: a harmful way is never lit: the marker shows only that the ring has ways
                return (g, i.More, i.WalkS > 0 || i.Walk.Length > 0, o.Do == "fire" && !AnyDrawn());
            }
        }

        /// <summary>Brief 038 §4.3: with the ring by default, a press fires at once only on a combatant with a gun drawn (or Ctrl, a deliberate
        /// force: the going-loud hold still guards it); a guard, a civilian, Vale or anyone not engaged opens the ring.</summary>
        bool PressFires(Target t) => t != null && Combatant(t) && (AnyDrawn() || t.Kind == "hostile"); // Ctrl no longer fires on press: it opens the ring with Fire held (CtrlFireRing)

        /// <summary>A right click here fires (acts on press): a hostile, security with a gun drawn, Ctrl on a civilian; Vale never.</summary>
        bool IsFire(Target t)
        {
            if (t == null) return false;
            bool ctrl = controls.Held(Keyboard.current, PlayAction.Force);
            if (t.Kind == "person" && t.Key == "councillor" && ctrl) return true; // the never-Vale pop is the press's
            return ShotAt(t, ctrl, out _, out _);
        }

        /// <summary>Fire (or the never-Vale pop) on a given target: E on a hostile in front, as the right press on it.</summary>
        void PointerRightOn(Target t, Vector2 screen)
        {
            bool ctrl = controls.Held(Keyboard.current, PlayAction.Force);
            if (t.Kind == "person" && t.Key == "councillor") { PopKey("never-vale", screen, TargetWorld(t), Feedback.Refused, $"never fire on {ValeWord}: the mission fails"); return; }
            if (ShotAt(t, ctrl, out var id, out var force)) { if (Panel != null) ClosePanel(); FireSelected(id, force, screen, "E"); }
        }

        void RingPath(PanelRow rw)
        {
            if (Ring == null || !Ring.Plan.Pn.World.HasValue) return; var op = Game.Ops[(int)rw.By];
            Pt[] route = null; Guard("path", () => { var aw = ActPlace(rw.By, rw.I.Order, Ring.Plan.T); route = aw == null ? null : MoveRoute(op.Pos, aw.To); }); // engine 0.33.1: round the standing cars
            PreviewWalk = route; // brief 067 F rule 5: the lit wedge's walk names the cones it crosses
            var pts = new List<Vector3>(); if (route != null) foreach (var q in route) pts.Add(frame.World(q) + Vector3.up * 0.08f);
            views.ShowPath(pts.Count > 1 ? pts : null);
        }

        /// <summary>What the cursor shows over the hovered thing: "aim" (a press fires), "ring" (other ways are one hold away), else "".</summary>
        Pt noWalkCell = new(int.MinValue, 0); bool noWalkWas;
        /// <summary>The ground under the pointer has no walkable cell within 3 m (Playable's rule for a walk), cached per 1 m cell.</summary>
        bool NoWalkUnder()
        {
            if (!cam || !frame.Ground(cam.ScreenPointToRay(lastHover), out var w)) return false;
            var at = frame.Engine(w); var cell = new Pt(at.X / 100 * 100, at.Y / 100 * 100); if (cell == noWalkCell) return noWalkWas;
            noWalkCell = cell; var g = CarWalk; if (g.Free(at)) return noWalkWas = false; // engine 0.33.1: a standing car's ground is not walkable
            var nf = g.NearestFree(at); return noWalkWas = !nf.HasValue || G.Dist(nf.Value, at) > 300;
        }

        public string CursorKind
        {
            get
            {
                if (PointerHeld) return ""; // Glyph's N4: no pointer badge over the briefing or the glide
                if (Ring == null && Panel == null && Hover == null && Game != null && Array.IndexOf(Selected, true) >= 0 && NoWalkUnder()) return "nowalk"; // Suture's bot pass: ground the engine can't walk shows before the click (every preset: the hold preset has no move chip)
                if (Ring != null || Hover == null || Panel != null || Game == null || TargetMode) return "";
                if (IsFire(Hover) && (controls.quickAction || PressFires(Hover) || PickedId == "fire")) return "aim";
                return HoverIntent != null && HoverIntent.More ? "ring" : "";
            }
        }

        // ---------------------------------------------------------------- keys (§8)

        float eDownAt = -1f; bool eRing;

        /// <summary>Third person: E is the quick click on the focus; held 0.22 s it opens the ring on the focus, centred on it.</summary>
        void DriveE(Keyboard kb)
        {
            bool down = controls.Down(kb, PlayAction.TurnRight), held = controls.Held(kb, PlayAction.TurnRight);
            if (!controls.quickAction) // Sky 2026-10-21: E opens the ring on the focus, the likely way lit; E again (Enter, a second right click) takes it
            {
                if (!down || Paused) return;
                eDownAt = -1f;
                if (Ring != null) { var rs = Ring; RingTake(rs.Hover >= 0 ? rs.Hover : rs.Lit); return; }
                if (DriveFocus == null) { dFocusKey = "?"; DriveFocusFrame(); }
                if (DriveFocus is not Target ft || !cam) { PopKey("nothing-here", null, views.DrivePos, Feedback.Info); return; }
                if (Picked >= 0 || IsFire(ft) || ft.Kind == "op") { DriveAct(); return; } // an armed action acts at once; Fire on press, as the right press
                var fw = TargetWorld(ft) ?? views.DrivePos; var fsp = cam.WorldToScreenPoint(fw + Vector3.up);
                ContextPlan fplan = null; if (!Guard("panel", () => fplan = ContextFor(ft, fsp)) || fplan == null) return;
                if (fplan.Choice.Order.Count == 0) { ActContext(ft, fsp); return; }
                OpenRing(fplan, fsp, true); if (Ring != null) { Ring.Sticky = true; Ring.Always = true; }
                return;
            }
            if (down && !Paused) { eDownAt = Time.unscaledTime; eRing = false; if (Ring != null) { CloseRing(); eDownAt = -1f; } return; }
            if (eDownAt < 0f) return;
            if (!held)
            {
                eDownAt = -1f;
                if (eRing) { var rs = Ring; if (rs == null) return; var mp = UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? rs.Screen; int s = (mp - rs.PointerAtOpen).magnitude > RingDeadPx * KitScale ? RingSegAt(mp) : -1; if (s >= 0) RingTake(s); else rs.Sticky = true; return; }
                DriveAct(); return;
            }
            if (!eRing && Time.unscaledTime - eDownAt >= RingHoldS)
            {
                eRing = true;
                if (DriveFocus == null) { dFocusKey = "?"; DriveFocusFrame(); }
                if (DriveFocus is not Target t || !cam) { PopKey("nothing-here", null, views.DrivePos, Feedback.Info); return; }
                var w = TargetWorld(t) ?? views.DrivePos; var sp = cam.WorldToScreenPoint(w + Vector3.up);
                ContextPlan plan = null; if (!Guard("panel", () => plan = ContextFor(t, sp)) || plan == null) return;
                OpenRing(plan, sp, true); if (Ring != null) Ring.Sticky = false;
            }
        }

        float lastMoveKeyAt = -10f;
        /// <summary>Third person: Shift+1–4 picks an action only while the agent stands (no move key, under 0.3 m/s, 0.15 s after the last move key).</summary>
        bool DriveStanding(Keyboard kb)
        {
            if (!Driving) return true;
            bool moveKey = controls.Held(kb, PlayAction.PanUp) || controls.Held(kb, PlayAction.PanDown) || controls.Held(kb, PlayAction.PanLeft) || controls.Held(kb, PlayAction.PanRight);
            if (moveKey) lastMoveKeyAt = Time.unscaledTime;
            return !moveKey && Time.unscaledTime - lastMoveKeyAt >= 0.15f && (views == null || views.DriveSpeed < 0.3f);
        }
        /// <summary>The plate's ⇧ caps dim while the driven agent moves (an ignored press is never a mystery).</summary>
        public bool PlateKeysDim => Driving && !DriveStanding(Keyboard.current);

        const string RightClickPref = "Suborned.Hud.RightClickUsed";
        bool? rightClickUsed;
        /// <summary>Brief 030 (no plate): a right click has been used on this profile (Fable's first-act hint retires for good).</summary>
        public bool RightClickUsed { get => rightClickUsed ??= PlayerPrefs.GetInt(RightClickPref, 0) == 1; private set { if (RightClickUsed == value) return; rightClickUsed = value; PlayerPrefs.SetInt(RightClickPref, value ? 1 : 0); PlayerPrefs.Save(); } }
        /// <summary>The first-act hint is due: the text has it, no right click yet, the clock running 3 s, an agent selected.</summary>
        public bool FirstActDue => Text != null && Text.FirstActHint.Length > 0 && !RightClickUsed && ClockStarted > 0 && Time.unscaledTime - ClockStarted > 3f && Array.IndexOf(Selected, true) >= 0;

        /// <summary>Brief 030 (Sky 2026-10-21, option A, no plate): when Tab or Shift last showed the selected agent's verb strip (unscaled s); it shows VerbStripS.</summary>
        public float VerbStripAt { get; private set; } = -10f;
        public const float VerbStripS = 2.5f;
        /// <summary>For scripted shots: the strip as Tab shows it.</summary>
        public void ShowVerbStrip() => VerbStripAt = Time.unscaledTime;

        /// <summary>Tab / Shift+Tab: the selected agent's next (previous) action: none, slot 1, 2, 3, …, back to none. Never moves the camera or the selection.</summary>
        void CycleAction(bool back)
        {
            var plate = PlateOf(Agent); var open = new List<int>();
            for (int i = 0; i < plate.Length; i++) if (!plate[i].Dim && plate[i].Id is not ("drop" or "stash")) open.Add(i); // the carry knobs act at once: not stepped onto
            if (open.Count == 0) { Picked = -1; intentKey = ""; return; }
            int at = open.IndexOf(Picked); // -1 = none
            int next = back ? (at < 0 ? open.Count - 1 : at - 1) : (at < 0 ? 0 : at + 1);
            if (next < 0 || next >= open.Count) { Picked = -1; intentKey = ""; Status = "action: the contextual default"; return; }
            Pick(open[next]);
        }
        int dFocusStep;
    }
}
