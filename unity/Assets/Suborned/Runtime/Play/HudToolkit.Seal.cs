using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The live seal deadline as a street label (brief 101; Sky 2026-11-07 on the old card: "really hate this card and design"; 2026-11-08: "street labels
    /// top right"): under the clock (HudToolkit.Clock), the lock and the count, orange on an ink keyline, 42 px (Glyph's S mocks: the orange count is the seal before
    /// you read it; Sky missed the old 38 px badge, brief 093). Under them the job, in marks and never a sentence: Popovic's face, four dots and our car while he
    /// is still to get there; our car and Go once he is aboard; our car and a lock while it is held. Hovered (or SealHoverForce), the cause shows before the
    /// lock: the shot, the desk's eye, the radio or the police badge (HandoverHost.SealCauseIcon). It pops once when set and beats once a second in its last 30 s
    /// (a motion cue: the colour is already the urgent one). It reads the engine's deadline only.
    /// Brief 096, the lend (engine 0.42.0): once Popovic is out the same row is the one countdown, the police closing in (Game.SplitPoliceAt, which already takes the
    /// seal and the close into account): the units icon and the count, the faces of those still to get clear (none once all are), and End, a button in
    /// Withdraw's place (HostLent.EndNow; never key-only). It retires the 420 x 104 seal card and its draining bar.</summary>
    public sealed partial class HudToolkit
    {
        int sealLeftShown = -1; string sealJobShown = "";
        VisualElement sealEl, sealRow, sealEnd, sealEndRing, sealJob, sealJobFace, sealCauseBadge; KeyText sealK; KeyIcon sealIc, sealCauseIc, sealJobCar, sealJobLock;
        KeyText sealJobGo; readonly List<VisualElement> sealJobDots = new(), sealLeftFaces = new();
        bool sealHover, sealPolice; float sealShownAt = -9f;
        /// <summary>The proof reads it: what the marker says now, its lock and count read as words ("District seals in 3:05"; "" hidden).</summary>
        public static string SealWords { get; private set; } = "";
        /// <summary>Proofs: the cause as if hovered.</summary>
        public static bool SealHoverForce;
        /// <summary>The proof checks the drawn row's bounds and its job, not just its backing string.</summary>
        public static Rect SealBounds => live?.sealEl?.worldBound ?? default;
        /// <summary>The job the marks under the count stand for (the seal's live line, or the police plate's who-is-left line).</summary>
        public static string SealActionWords { get; private set; } = "";
        public static bool SealVisible => live?.sealEl != null && live.sealEl.resolvedStyle.display != DisplayStyle.None && live.sealEl.resolvedStyle.opacity > 0.9f && SealBounds.width > 1 && SealBounds.height > 1
            && live.pill != null && live.pill.resolvedStyle.opacity > 0.9f;
        /// <summary>Proofs: the end card's lines as the HUD has them now.</summary>
        public static IReadOnlyList<string> EndLinesNow => live?.m?.EndLines;

        void BuildSeal()
        {
            sealEl = new VisualElement { pickingMode = PickingMode.Ignore, name = "seal" }; sealEl.style.flexDirection = FlexDirection.Column; sealEl.style.alignItems = Align.FlexEnd; sealEl.style.flexShrink = 0;
            pill.Insert(Mathf.Min(1, pill.childCount), sealEl); // under the time (the clock row is the stack's first)
            sealRow = StackRow(sealEl, 50f); sealRow.pickingMode = PickingMode.Position; sealRow.style.transformOrigin = new TransformOrigin(Length.Percent(100), Length.Percent(50), 0);
            sealRow.RegisterCallback<PointerEnterEvent>(_ => sealHover = true); sealRow.RegisterCallback<PointerLeaveEvent>(_ => sealHover = false);
            sealCauseIc = KIcon(sealRow, "", 24f); sealCauseIc.Root.style.marginRight = 10;
            sealCauseBadge = E(sealRow, null, w: 28, h: 28); sealCauseBadge.style.flexShrink = 0; sealCauseBadge.style.marginRight = 10; Show(sealCauseBadge, false);
            // brief 096: End, a button (the pause button's look, with its word)
            sealEnd = E(sealRow, null, pick: true); sealEnd.name = "police-end"; sealEnd.style.height = 30; sealEnd.style.flexShrink = 0; sealEnd.style.marginRight = 12; sealEnd.style.justifyContent = Justify.Center;
            sealEnd.style.paddingLeft = 14; sealEnd.style.paddingRight = 14; sealEnd.style.overflow = Overflow.Visible;
            var esh = E(sealEnd); esh.AddToClassList("e-shadow"); esh.style.position = Position.Absolute; esh.style.left = 0; esh.style.right = 0; esh.style.top = 2; esh.style.bottom = -2; Round(esh, 15f); esh.style.opacity = 0.6f;
            sealEndRing = E(sealEnd); sealEndRing.AddToClassList("e-body"); sealEndRing.AddToClassList("e-inline"); sealEndRing.style.position = Position.Absolute; sealEndRing.style.left = 0; sealEndRing.style.right = 0;
            sealEndRing.style.top = 0; sealEndRing.style.bottom = 0; Round(sealEndRing, 15f); BorderW(sealEndRing, 2f);
            var endWord = KLabel(sealEnd, "k-bold", "k-page", 14f); endWord.text = BookWordsFile.Get().PoliceEnd;
            sealEnd.RegisterCallback<PointerEnterEvent>(_ => RingColour(sealEndRing, TurqLight)); sealEnd.RegisterCallback<PointerLeaveEvent>(_ => RingColour(sealEndRing, null));
            sealEnd.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.EndNow(); e.StopPropagation(); } });
            manifest.Add((sealEnd, "lock", () => endWord.text));
            sealIc = KIcon(sealRow, "lock", 30f, "ki-hot", "ki-key-ink", 2f); sealIc.Root.style.marginRight = 8;
            sealK = KText(sealRow, 42f, "k-bold", "k-hot", "k-key-ink", KeyHotW);
            // the job, in marks
            sealJob = StackRow(sealEl, 28f);
            sealJobFace = E(sealJob, null, w: 24, h: 24); sealJobFace.style.flexShrink = 0; Round(sealJobFace, 12f); BorderW(sealJobFace, 2f); sealJobFace.AddToClassList("e-inline"); sealJobFace.AddToClassList("e-page"); Bg(sealJobFace, "portraits/cameo_councillor_normal");
            for (int i = 0; i < 4; i++) { var d = E(sealJob, null, w: 8.4f, h: 8.4f); d.style.flexShrink = 0; d.style.marginLeft = i == 0 ? 7f : 4f; Round(d, 4.2f); BorderW(d, 1.4f); d.AddToClassList("e-ink"); d.AddToClassList("e-inline"); sealJobDots.Add(d); }
            for (int i = 0; i < 4; i++) { var f = E(sealJob, null, w: 24, h: 24); f.style.flexShrink = 0; f.style.marginLeft = i == 0 ? 0 : 4; Round(f, 12f); BorderW(f, 2f); f.AddToClassList("e-inline"); f.AddToClassList("e-page"); sealLeftFaces.Add(f); Show(f, false); }
            sealJobCar = KIcon(sealJob, "car", 22f); sealJobCar.Root.style.marginLeft = 8;
            sealJobGo = KText(sealJob, 15f); sealJobGo.Root.style.marginLeft = 6; sealJobGo.Set(host.Text != null && host.Text.Actions.TryGetValue("go", out var go) && go.Label.Length > 0 ? go.Label : "Go"); // the car's own way (text.json actions.go)
            sealJobLock = KIcon(sealJob, "lock", 18f); sealJobLock.Root.style.marginLeft = 6;
            manifest.Add((sealRow, "marker", () => sealK.Words)); manifest.Add((sealIc.Root, "image", () => "seal lock")); manifest.Add((sealJob, "image", () => "seal job")); // the row (its lock, its keylines): a lock and m:ss
            manifest.Add((sealCauseIc.Root, "image", () => "seal cause")); manifest.Add((sealCauseBadge, "image", () => "seal cause"));
            Show(sealEl, false);
        }

        void SealFrame()
        {
            if (root == null || pill == null) return; if (sealEl == null) BuildSeal();
            var h = host; bool police = h.PoliceOn; bool on = (h.SealOn || police) && !m.Ended && !m.BriefingOpen;
            Show(sealEl, on); if (!on) { SealWords = SealActionWords = ""; sealShownAt = -9f; sealLeftShown = -1; sealJobShown = ""; return; }
            float now = Time.unscaledTime; if (sealShownAt < 0f) sealShownAt = now;
            var bw = BookWordsFile.Get();
            if (police != sealPolice) { sealPolice = police; sealLeftShown = -1; sealJobShown = ""; sealIc.Set(this, police ? "units" : "lock"); }
            Show(sealEnd, police);
            int left = police ? h.PoliceLeftS : h.SealLeftS;
            if (left != sealLeftShown)
            {
                sealLeftShown = left; int n = Mathf.Max(0, left); sealK.Set($"{n / 60}:{n % 60:00}");
                SealWords = police ? bw.PoliceHead.Replace("{time}", sealK.Words) : h.Text.SealHeading(left);
                SealActionWords = police ? h.PoliceAction() : h.Text.SealLive[h.ValeAboard ? h.Game.CarHolders().Count > 0 ? "held" : "ready" : "empty"];
                SealJob(police);
            }
            // the one pop when it is set; in the last 30 s a beat a second
            float pop = now - sealShownAt, sc = pop < 0.6f ? 1f + 0.18f * Mathf.Sin(pop / 0.6f * Mathf.PI) : 1f;
            if (pop >= 0.6f && left <= 30) { float b = Mathf.Repeat(now, 1f); sc = b < 0.25f ? 1f + 0.08f * Mathf.Sin(b / 0.25f * Mathf.PI) : 1f; }
            sealRow.style.scale = new Scale(new Vector2(sc, sc));
            if (h.Modal == ModalGate.Modal.None) pill.BringToFront(); // a live deadline is never buried under ordinary HUD pieces
            bool hv = (sealHover || SealHoverForce) && !police; // the police row has no cause: Popovic going out is the cause
            string ic = hv ? h.SealCauseIcon() : ""; bool badge = ic.StartsWith("badge:");
            Show(sealCauseBadge, hv && badge); if (hv && badge) Bg(sealCauseBadge, "badges/" + ic[6..], Color.white);
            sealCauseIc.Set(this, hv && !badge ? ic : "");
        }

        /// <summary>The marks under the count: "empty" Popovic, dots, the car; "ready" the car and Go; "held" the car and a lock; the police, the faces of those
        /// still to get clear (nothing once all are). Redrawn only when the job changes (checked once a second, with the count).</summary>
        void SealJob(bool police)
        {
            string job = police ? "police:" : !host.ValeAboard ? "empty" : host.Game.CarHolders().Count > 0 ? "held" : "ready";
            List<(string op, string fate, string why)> left = null;
            if (police) { left = host.LeftBehindNow(); foreach (var x in left) job += x.op + ","; }
            if (job == sealJobShown) return; sealJobShown = job;
            bool empty = job == "empty"; Show(sealJobFace, empty); foreach (var d in sealJobDots) Show(d, empty);
            Show(sealJobCar.Root, !police); Show(sealJobGo.Root, job == "ready"); Show(sealJobLock.Root, job == "held");
            for (int i = 0; i < sealLeftFaces.Count; i++)
            {
                bool has = police && left != null && i < left.Count; Show(sealLeftFaces[i], has);
                if (has) Bg(sealLeftFaces[i], $"portraits/cameo_{left[i].op}_normal");
            }
            Show(sealJob, !police || left != null && left.Count > 0);
        }
    }
}
