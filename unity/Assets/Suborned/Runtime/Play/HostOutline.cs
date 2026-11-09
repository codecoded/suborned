using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using HoverOutlinePass = Suborned.HoverOutlinePass;

namespace Suborned.Play
{
    /// <summary>Brief 101, the hover outline (Sky 2026-11-09 picked Glyph's B, briefs/101/glyph/outline/notes.md): whatever a left click would act on
    /// wears a thin ink line with a fine cream edge (Suborned.HoverOutlinePass draws it; HostViews.Outline says what each thing draws), so things
    /// close together read apart: the guard in the Records Office door, or the door beside him.
    ///  · What: an open ring's or panel's thing (it keeps the line while the ring is open); a hold's thing; in the hold preset your own agent near
    ///    the pointer (a click selects her: HoldClick's first pick), else the hovered thing (Tab, Z and the wheel step it through an overlap, and
    ///    the line moves with each step; it holds the pill's 0.15 s on open ground, HostHoldStatus.Sticky). A passer-by or a passing car only when
    ///    the selection has a way on them (HudModel's rule). One look for every thing and every agent: no colour meaning.
    ///  · When: the map view only (an orthographic play camera; not third person, the book, the start page, a conversation scene, the fail
    ///    camera or the end), and never with the pointer on the HUD.
    ///  · Fades: in and out over 0.1 s (unscaled: a pause fades too); a new thing fades in as the old one fades out.
    /// It replaces the cream ring at a person's feet when there is no pill (brief 085, Fable A6, A7), the dashed rings at the feet of the others in
    /// a stack (brief 067 F9), and in the classic controls the cream ground ring and ellipse; the stack dots, the name chip and the pill stay.
    /// Driven from the play camera's beginCameraRendering (before its culling), so no frame of the host's own is touched. View only.</summary>
    public sealed partial class HandoverHost
    {
        public const float OutlineFadeS = 0.1f;
        sealed class Outlined { public Target T; public string Key = ""; public float Alpha, Clip = HostViews.NoClip; public bool Sil; public uint Bit; public readonly List<Renderer> On = new(); }
        Outlined olNow, olWas; float olAt = -1f; int olFrame = -1; readonly List<Renderer> olWant = new();

        /// <summary>The proof reads these: the outlined thing now ("kind/key", "" none), its fade, and the one fading out.</summary>
        public string OutlineOn => olNow?.T == null ? "" : olNow.T.Kind + "/" + olNow.T.Key;
        public float OutlineAlpha => olNow?.Alpha ?? 0f;
        public string OutlineFading => olWas?.T == null ? "" : olWas.T.Kind + "/" + olWas.T.Key;
        /// <summary>The proof reads it: how many renderers draw the outlined thing's mask now.</summary>
        public int OutlineParts => olNow?.On.Count ?? 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OutlineHook() { RenderPipelineManager.beginCameraRendering -= OutlineBegin; RenderPipelineManager.beginCameraRendering += OutlineBegin; }
        static HandoverHost outlineHost; static float outlineLookedAt = -9f; static bool outlineFailed;
        static void OutlineBegin(ScriptableRenderContext _, Camera c)
        {
            if (!outlineHost) { if (Time.unscaledTime - outlineLookedAt < 1f) return; outlineLookedAt = Time.unscaledTime; outlineHost = FindAnyObjectByType<HandoverHost>(); if (!outlineHost) return; }
            if (c != outlineHost.cam || outlineFailed) return;
            try { outlineHost.OutlineFrame(c); }
            catch (System.Exception e) { outlineFailed = true; Debug.LogException(e); Debug.LogWarning("HandoverHost: the hover outline stopped (an error above); the game goes on without it"); } // view only: never the run's fault
        }

        /// <summary>What a click would act on now (see the class), else null.</summary>
        Target OutlineTarget()
        {
            if (Game == null || Fault != null || views == null || !cam || !cam.orthographic || Following || Driving || ConvoOn || FailCamOn || Modal != ModalGate.Modal.None) return null;
            if (Ring != null) return Ring.Plan?.T;
            if (Panel != null) return Panel.T;
            if (Charge?.T != null) return Charge.T;
            if (OverHud(lastMouse)) return null;
            if (HoldOn && AgentNear(lastMouse) is Target own) return own;
            var t = Hover; if (t == null) return null;
            if (Passer(t) || IsTrafficCar(t)) { var (glyph, more, _, _) = HoverAct; if (glyph.Length == 0 && !more) return null; } // a passer-by or a passing car: only with a way on them
            return t;
        }

        /// <summary>A target's view (TargetWorld's map; a stand has no view of its own, so no line).</summary>
        string OutlineViewKey(Target t) => t.Kind switch
        {
            "person" => t.Key, "door" => "marker/door", "cabinet" => "marker/cabinet", "camera" => "camera/" + t.Key, "car" => "car/" + t.Key, "patrol" => "patrol",
            "rivals" => t.Key == "car:rivals" ? "car/rivals" : t.Key, "security" => OfficerView(t.Key), "hostile" => ViewKey(t.Key), "op" => "op/" + t.Key, "crowd" => t.Key,
            "body" => "body/" + t.Key, "stash" => "stash/" + t.Key, "unit" => "unit/" + t.Key, "bystander" => views.BystanderKey(Game, t.Key), _ => null,
        };

        /// <summary>Per play-camera render, before its culling: the thing, the fades, the bits on its renderers, and the pass for this frame.</summary>
        void OutlineFrame(Camera c)
        {
            if (olFrame == Time.frameCount) return; olFrame = Time.frameCount;
            if (!c.orthographic) { if (olNow != null) Unmark(olNow); if (olWas != null) Unmark(olWas); olNow = olWas = null; return; } // a perspective view (third person) takes none, not even a fade
            float now = Time.unscaledTime, dt = olAt < 0f ? 0f : Mathf.Clamp(now - olAt, 0f, 0.1f); olAt = now;
            views?.OutlineWarm(); // the one scene walk the stand-ins need, at the start rather than on the first hover
            var want = OutlineTarget(); var key = want != null ? OutlineViewKey(want) : null; if (key == null) want = null;
            if (want == null ? olNow != null : olNow == null || olNow.Key != key)
            {
                if (want != null && olWas != null && olWas.Key == key) (olNow, olWas) = (olWas, olNow); // back onto the one fading out: it fades in from where it is
                else { if (olWas != null) Unmark(olWas); olWas = olNow; olNow = want != null ? new Outlined { T = want, Key = key } : null; }
            }
            if (olNow != null) { olNow.T = want; olNow.Alpha = Mathf.MoveTowards(olNow.Alpha, 1f, dt / OutlineFadeS); }
            if (olWas != null) { olWas.Alpha = Mathf.MoveTowards(olWas.Alpha, 0f, dt / OutlineFadeS); if (olWas.Alpha <= 0f) { Unmark(olWas); olWas = null; } }
            Mark(olNow, HoverOutlinePass.LayerA); Mark(olWas, HoverOutlinePass.LayerB);
            bool a = Box(olNow, c, out var boxA), b = Box(olWas, c, out var boxB);
            if (!a && !b) return;
            var box = a && b ? Rect.MinMaxRect(Mathf.Min(boxA.xMin, boxB.xMin), Mathf.Min(boxA.yMin, boxB.yMin), Mathf.Max(boxA.xMax, boxB.xMax), Mathf.Max(boxA.yMax, boxB.yMax)) : a ? boxA : boxB;
            HoverOutlinePass.Enqueue(c, new HoverOutlinePass.Request
            {
                Box = box, AlphaA = a ? Ease(olNow.Alpha) : 0f, AlphaB = b ? Ease(olWas.Alpha) : 0f, ClipA = olNow?.Clip ?? HostViews.NoClip, ClipB = olWas?.Clip ?? HostViews.NoClip,
                SilA = a && olNow.Sil, SilB = b && olWas.Sil,
            });
        }
        static float Ease(float x) => x * x * (3f - 2f * x);

        /// <summary>Puts `bit` on what the thing draws now and takes it off what it no longer draws (a change of clothes, a view hidden, a lens swapped).</summary>
        void Mark(Outlined o, uint bit)
        {
            if (o == null) return;
            views.OutlineRenderers(o.Key, olWant, out o.Clip);
            o.Sil = HostViews.SilhouetteKey(o.Key) && fade && fade.silhouettes && fade.fade; // a person the silhouettes draw through cover
            for (int i = o.On.Count - 1; i >= 0; i--) { var r = o.On[i]; if (r && o.Bit == bit && olWant.Contains(r)) continue; HostViews.OutlineMark(r, o.Bit, false); o.On.RemoveAt(i); }
            o.Bit = bit;
            foreach (var r in olWant) if (!o.On.Contains(r)) { HostViews.OutlineMark(r, bit, true); o.On.Add(r); }
        }
        void Unmark(Outlined o) { foreach (var r in o.On) HostViews.OutlineMark(r, o.Bit, false); o.On.Clear(); }

        /// <summary>The thing's box on the camera's viewport (0..1 from the bottom left): its renderers' bounds, from its clip height up.</summary>
        static bool Box(Outlined o, Camera c, out Rect box)
        {
            box = default; if (o == null || o.Alpha <= 0f) return false;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var r in o.On)
            {
                if (!r || !r.gameObject.activeInHierarchy) continue; var b = r.bounds;
                float lo = Mathf.Max(b.min.y, Mathf.Min(b.max.y, o.Clip)); if (lo >= b.max.y && b.max.y > b.min.y) continue;
                for (int k = 0; k < 8; k++)
                {
                    var w = new Vector3((k & 1) == 0 ? b.min.x : b.max.x, (k & 2) == 0 ? lo : b.max.y, (k & 4) == 0 ? b.min.z : b.max.z); var v = c.WorldToViewportPoint(w);
                    x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y);
                }
            }
            if (x1 < x0 || x1 < 0f || y1 < 0f || x0 > 1f || y0 > 1f) return false;
            box = Rect.MinMaxRect(x0, y0, x1, y1); return true;
        }
    }
}
