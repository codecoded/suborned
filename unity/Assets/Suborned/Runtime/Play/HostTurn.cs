using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 016 (Sky, 2026-10-17: "yeah, camera rotation"): the view turns. View only: the engine, the rules and the replays never see it.
    /// Q and E turn it a quarter round about the screen centre's ground point, eased over 0.35 s (unscaled: it turns in a pause), between the
    /// four known yaws: the built yaw (30) and every 90° from it. Alt + middle-drag turns it freely and snaps to the nearest of the four on
    /// release. Pitch and zoom never change; a run opens at the built yaw. Pans follow the screen at any yaw (Pan reads the camera's own
    /// axes). The architecture fade keeps clear, besides the screen centre, every agent, the action target, the cursor's ground point and the
    /// key people (Inez, the guard, Vale while seen), so whatever a tower hides from one of the four views is a ghost there.</summary>
    public sealed partial class HandoverHost
    {
        [Header("Brief 016: the view turns")]
        [Tooltip("Seconds a quarter turn (Q / E, or the snap after a free turn) eases over.")]
        public float turnSeconds = 0.35f;
        [Tooltip("Alt + middle-drag: degrees of yaw per pixel of horizontal drag at 1080p.")]
        public float turnDragDegPerPixel = 0.3f;
        [Tooltip("Radius (m) of the disc the fade keeps clear round each agent, key person, the action target and the cursor's ground point.")]
        public float fadePointRadius = 4f;

        float baseYaw = 30f, pitch = 38f, yawNow = 30f, yawFrom = 30f, yawTo = 30f, turnStart = -1f, turnDragX;
        bool turnReady, turnDragging; Vector3 turnPivot;
        readonly List<Vector4> fadePoints = new();

        /// <summary>The view's yaw now (degrees, 0 = north), while easing too.</summary>
        public float Yaw => yawNow;
        /// <summary>Which of the four views the turn rests on or is heading to: 0 = the opening yaw, then each 90° clockwise.</summary>
        public int YawIndex => Mathf.RoundToInt(Mathf.Repeat(yawTo - baseYaw, 360f) / 90f) % 4;
        /// <summary>Alt + middle is turning the view (the Alt detail layer stays off while it does).</summary>
        public bool TurnDragging => turnDragging;
        /// <summary>The view has turned this run (the turn hint's gone-when).</summary>
        public bool TurnedThisRun { get; private set; }
        bool Turning => turnDragging || turnStart >= 0f;

        /// <summary>Once, when the camera stands: the built pitch and yaw (the opening view).</summary>
        void TurnInit()
        {
            if (turnReady || !cam) return;
            var e = cam.transform.eulerAngles; pitch = e.x; baseYaw = e.y; yawNow = yawFrom = yawTo = baseYaw; turnReady = true;
        }

        /// <summary>A new run opens at the built yaw.</summary>
        void TurnReset()
        {
            TurnInit(); if (!cam) return;
            turnStart = -1f; turnDragging = false; TurnedThisRun = false; TurnQueueCancel(); doorClearFor = 0f;
            yawNow = yawFrom = yawTo = baseYaw; cam.transform.rotation = Quaternion.Euler(pitch, yawNow, 0f);
        }

        /// <summary>Turn to one of the four views (k = 0 the opening yaw, 1 = +90°, …), eased; for scripted shots and checks too.</summary>
        public void TurnTo(int k) { TurnInit(); BeginTurn(baseYaw + 90f * (((k % 4) + 4) % 4)); }

        /// <summary>Turn a quarter round: -1 = Q (the view turns left), +1 = E (right). A press during an ease turns on from where it was heading.</summary>
        public void TurnBy(int quarters) { TurnInit(); BeginTurn(yawTo + 90f * quarters); }

        /// <summary>Snap now to one of the four, no ease (scripted shots: the same moment at every angle).</summary>
        public void TurnSnap(int k) { TurnTo(k); turnStart = Time.unscaledTime - turnSeconds - 1f; ApplyTurn(); }

        void BeginTurn(float to)
        {
            if (!cam) return;
            if (!Turning) turnPivot = TurnPivot();
            yawFrom = yawNow; yawTo = to; turnStart = Time.unscaledTime; TurnedThisRun = true;
        }

        /// <summary>The ground point at the screen centre (the walk heights), else the focus.</summary>
        Vector3 TurnPivot()
        {
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return frame.Ground(ray, out var hit) ? hit : focus;
        }

        /// <summary>The keys and the Alt + middle-drag (under a modal too: the camera may look round there), then the frame's turn.</summary>
        void TurnInput(Keyboard kb, UnityEngine.InputSystem.Mouse mouse) { TurnKeys(kb, mouse); ApplyTurn(); }

        void TurnKeys(Keyboard kb, UnityEngine.InputSystem.Mouse mouse)
        {
            TurnInit(); if (!cam) return;
            if (kb != null && !turnDragging)
            {
                if (controls.Down(kb, PlayAction.TurnLeft)) { TurnQueueCancel(); TurnBy(-1); }
                if (controls.Down(kb, PlayAction.TurnRight)) { TurnQueueCancel(); TurnBy(1); }
            }
            if (mouse == null) return;
            bool mod = controls.Held(kb, PlayAction.TurnDrag), held = mouse.middleButton.isPressed;
            float x = mouse.position.ReadValue().x;
            if (!turnDragging && mod && mouse.middleButton.wasPressedThisFrame)
            {
                TurnQueueCancel(); turnPivot = Turning ? turnPivot : TurnPivot(); turnDragging = true; turnStart = -1f; turnDragX = x; grabbing = false; TurnedThisRun = true;
                return;
            }
            if (!turnDragging) return;
            if (!held)
            {   // released: ease to the nearest of the four (brief 025's lab: or rest where it was left)
                turnDragging = false;
                if (Cam.overhead.freeYaw) { yawFrom = yawTo = yawNow = Mathf.Repeat(yawNow, 360f); return; }
                float rel = Mathf.Repeat(yawNow - baseYaw, 360f); int k = Mathf.RoundToInt(rel / 90f) % 4;
                yawFrom = yawNow; yawTo = yawNow + Mathf.DeltaAngle(yawNow, baseYaw + 90f * k); turnStart = Time.unscaledTime;
                return;
            }
            float dx = x - turnDragX; turnDragX = x;
            yawNow += dx * turnDragDegPerPixel * 1080f / Mathf.Max(1f, Screen.height); yawFrom = yawTo = yawNow;
        }

        /// <summary>Per frame: the eased yaw, the camera turned about the pivot so the pivot stays at the screen centre.</summary>
        void ApplyTurn()
        {
            if (!cam || !turnReady || Following) return; // the follow view turns itself (HostFollow)
            if (turnStart >= 0f)
            {
                float t = turnSeconds <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - turnStart) / turnSeconds);
                float s = t * t * (3f - 2f * t);
                yawNow = yawFrom + Mathf.DeltaAngle(yawFrom, yawTo) * s;
                if (t >= 1f) { yawNow = yawTo = Mathf.Repeat(yawTo, 360f); yawFrom = yawNow; turnStart = -1f; intentKey = ""; } // the price under a still mouse is redone
            }
            else if (!turnDragging) return;
            var rot = Quaternion.Euler(pitch, yawNow, 0f);
            if (cam.transform.rotation == rot && !turnDragging && turnStart < 0f) return;
            cam.transform.rotation = rot;
            var f = cam.transform.forward; // the point at the camera's own datum (y 0) on the line of sight through the pivot
            if (!AnchoredFocus()) { focus = turnPivot + f * (turnPivot.y / Mathf.Max(1e-3f, -f.y)); focus.y = 0f; } // the door turn: about the door (HostDoorView)
            PlaceCamera();
            if (turnStart < 0f && !turnDragging) TurnQueueNext(); // a quarter of the door turn done: the next, or the anchor lets go
        }

        /// <summary>A pan, a grab or a centring during a turn moves the pivot with it.</summary>
        void TurnMoved(Vector3 delta) { if (Turning) turnPivot += new Vector3(delta.x, 0f, delta.z); if (turnAnchor is Vector3 a) turnAnchor = a + new Vector3(delta.x, 0f, delta.z); }
        void TurnLookAt(Vector3 world) { TurnQueueCancel(); if (Turning) turnPivot = new Vector3(world.x, 0f, world.z); }

        /// <summary>Per frame: the points the fade keeps clear besides the screen centre, in order of need (the shader keeps the first
        /// ArchitectureFade.MaxPoints): the selected agents, the action target, the cursor's ground point, the other agents, then Inez, the
        /// guard and Vale while an agent sees him. Only what is near the frame.</summary>
        void FadePoints()
        {
            if (!fade || !cam || views == null || Game == null) return;
            fadePoints.Clear(); float r = fadePointRadius;
            void Add(Vector3? w, float rad)
            {
                if (!w.HasValue) return; var vp = cam.WorldToViewportPoint(w.Value);
                if (vp.z <= 0f || vp.x < -0.15f || vp.x > 1.15f || vp.y < -0.15f || vp.y > 1.15f) return;
                for (int i = 0; i < fadePoints.Count; i++)
                {
                    var q = fadePoints[i]; if (((Vector3)q - w.Value).sqrMagnitude >= 1f) continue; // one disc a spot
                    if (rad < 0f && q.w > 0f) fadePoints[i] = new Vector4(q.x, q.y, q.z, -Mathf.Max(-rad, q.w)); // a person stands there (the guard at his door): clean
                    return;
                }
                fadePoints.Add(new Vector4(w.Value.x, w.Value.y, w.Value.z, rad));
            }
            Vector3? OpAt(int k) { var o = Game.Ops[k]; if (o.Inside) return null; var t = views.Find("op/" + Suborned.Sim.Op.Name((Suborned.Sim.OpId)k)); return t && t.gameObject.activeSelf ? t.position : (Vector3?)null; }
            Vector3? Person(string key) { var t = views.Find(key); return t && t.gameObject.activeSelf ? t.position : (Vector3?)null; }
            float rp = -personRadius; // brief 020 (Glyph): a person's disc is cut clean, no screen door over the figure
            for (int k = 0; k < 4; k++) if (Selected[k]) Add(OpAt(k), rp);
            if (Panel != null) Add(Panel.World, r); else if (Hover != null) Add(TargetWorld(Hover), r);
            if (Ending.Length == 0 && (DoorMarked || EnterProgress >= 0)) Add(frame.World(Game.S.At(Game.S.GuardPost)), r); // Glyph techwatch fix 4: the door stays ghosted round its marker when the guard has gone (the marker itself reads on an ink halo, HudToolkit.StepMarks)
            if (Modal == ModalGate.Modal.None && UnityEngine.InputSystem.Mouse.current is { } mouse)
            {
                var p = mouse.position.ReadValue();
                if (p.x >= 0 && p.y >= 0 && p.x <= cam.pixelWidth && p.y <= cam.pixelHeight && !(PlayHud.Blocks(p) || HudToolkit.Blocks(p)) && frame.Ground(cam.ScreenPointToRay(p), out var g)) Add(g, r);
            }
            Add(Person("guard"), rp);
            for (int k = 0; k < 4; k++) if (!Selected[k]) Add(OpAt(k), rp);
            // brief 020 (Glint, figures-render §2 #7; Glyph): every signal figure (hostiles, the response's officers and robots, the patrol,
            // the rivals, the legacy officers), every carried body and Vale keep a clean disc, so none stands in the architecture's ghost dither
            foreach (var w in FigurePoints()) Add(w, rp);
            Add(Person("waitress"), rp);
            if (ValeSeen) Add(Person("councillor"), rp);
            ServedTable();
            fade.SetPoints(fadePoints);
        }

        /// <summary>World points of the signal figures on the street (hostiles, units, the patrol, the rivals, the legacy officers, as drawn),
        /// every carried body (at its carrier) and Vale when he is with the Enforcer.</summary>
        IEnumerable<Vector3> FigurePoints()
        {
            var g = Game; if (g == null || views == null) yield break;
            foreach (var w in views.SignalPeople()) yield return w;
            foreach (var b in g.Bodies) if (b.Carrier.HasValue) yield return frame.World(g.BodyPos(b));
            if (g.Vale == Suborned.Sim.ValeAt.WithEnforcer && g.ValePos() is Suborned.Sim.Pt vp) yield return frame.World(vp);
        }

        [Tooltip("Brief 020: the radius (m) of a person's clean disc (no ghost over the figure; its head hides ground about 2.3 m behind it at 38°).")]
        public float personRadius = 3.2f;

        Renderer[] servedParasol = System.Array.Empty<Renderer>(); string servedTable = "";
        readonly Dictionary<string, Transform> tables = new(), parasols = new();
        /// <summary>Brief 022 (Glyph): while Inez serves a table, the parasol over it is not drawn, so the table, its customers and her cup
        /// show (a ghosted canopy left a screen door over the seated customers); it is back as she leaves the table.</summary>
        void ServedTable()
        {
            string want = "";
            if (Game.PersonPose(Game.Inez) is { } ip && ip.phase == "serving")
            {
                if (tables.Count == 0) foreach (var t in Object.FindObjectsByType<Transform>()) if (t.name.StartsWith("civic/cafe-table-")) { if (t.name.IndexOf('/', 6) < 0) tables[t.name] = t; else if (t.name.EndsWith("/parasol")) parasols[t.name] = t; }
                var at = frame.World(ip.pos); float best = 3f;
                foreach (var kv in tables) { var d = kv.Value.position - at; d.y = 0f; if (d.magnitude < best) { best = d.magnitude; want = kv.Key; } }
            }
            if (want == servedTable) return;
            foreach (var r in servedParasol) if (r) r.forceRenderingOff = false;
            servedParasol = System.Array.Empty<Renderer>(); servedTable = want;
            if (want.Length > 0 && parasols.TryGetValue(want + "/parasol", out var pt)) // the builder's sibling "civic/cafe-table-N/parasol"
            { servedParasol = pt.GetComponentsInChildren<Renderer>(); foreach (var r in servedParasol) r.forceRenderingOff = true; }
        }
    }
}
