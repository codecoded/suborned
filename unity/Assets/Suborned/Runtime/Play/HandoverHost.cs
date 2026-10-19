using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using Camera = UnityEngine.Camera;
using Debug = UnityEngine.Debug;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>The Handover played live (brief 011 D; Sky 2026-10-15: v10 "signage hotbar", right-click orders, provisional). Keel's engine (the
    /// Suborned.Sim DLL the headless CLI runs) steps at 10 Hz; the capsules move by it (HostViews), never by a recorded stream; every order goes in
    /// as an engine order and the engine's own order log is written after each one, with the run's checkpoint hashes, to out/play/ (a player:
    /// persistentDataPath/play), so any session replays and can become a route script.
    /// Controls (Sky, 2026-10-15, "everything but rotation"; one table, PlayControls): left click an agent to select (Shift adds; drag a box
    /// on the ground to select every agent in it) · left click the ground to walk there (roads cross anywhere; a double click runs, the
    /// engine's opt-in run) · right click a person or object opens the action panel (every action the cell has on it, priced, §02); with a
    /// verb picked on the hotbar the right click gives it at once · right click empty ground closes the panel, else cancels the picked verb,
    /// else stops the selection's walks · 1–4 pick a verb (the hotbar lights the
    /// one a right click gives) · F1–F4 or a roster portrait select an agent, twice quickly centres on them · Tab cycles agents · WASD,
    /// arrows or the middle mouse pan (edge scrolling is a setting, off) · the wheel zooms 12.5–18.5, where a 1.75 m person stays 40–60 px
    /// at 1080p (§10) · Q / E turn the view a quarter round, Alt + middle-drag turns it freely and snaps to the nearest quarter (brief 016,
    /// HostTurn; the opening view keeps yaw 30) · Space pauses (on at the start) · = and − change speed · N jumps to the next event ·
    /// F12 the debug layer · Return restarts after the end card.
    /// Engine exceptions stop the host at this boundary, never the Editor (Redline E1). Preview (one A* and four temperament evaluations) is
    /// called only when the hovered target, the agent or the verb changes.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed partial class HandoverHost : MonoBehaviour
    {
        [Header("Baked by PlayInstall (Editor) from the plan, the export and the civic look")]
        public SiteFrame frame = new();
        public Material litTemplate, unlitTemplate;
        [Tooltip("Brief 020: the stand-in figures (Forma); null = capsules everywhere.")]
        public FigureLibrary figures;
        [Tooltip("Brief 020 (Vela's kit pick): the cars and the response robots as kit models; null = the boxes and drums.")]
        public GameObject sedanOfficial, sedanGetaway, robotPorter;
        [Tooltip("The patrol robot's model (the lighter butler, RobotButler_Play, 2.6k tris: the café's foreground robot is the same piece); null = the capsule.")]
        [UnityEngine.Serialization.FormerlySerializedAs("serviceRobot")] public GameObject patrolRobot;
        [Tooltip("Transparent unlit, both faces: the sight fans' fill (PlayInstall makes it, so a player build carries the variant).")]
        public Material fanTemplate;
        public Color cellColour = new(0.25f, 0.27f, 0.29f), cellHeadColour = new(0.93f, 0.9f, 0.82f), headColour = new(0.95f, 0.92f, 0.84f);
        public Color waitressColour = new(0.93f, 0.52f, 0.45f), civicColour = new(0.2f, 0.55f, 0.55f), securityColour = new(0.45f, 0.48f, 0.52f);
        public Color rivalBodyColour = new(0.4f, 0.36f, 0.5f), carColour = new(0.55f, 0.58f, 0.6f), getawayColour = new(0.85f, 0.83f, 0.78f);
        [Tooltip("Crowd capsule colours by the engine's job (people.json look blocks); a job not listed draws as `resident`.")]
        public string[] jobs = Array.Empty<string>(); public Color[] jobColours = Array.Empty<Color>();
        public Color selectColour = Color.white, coneColour = new(0.878f, 0.227f, 0.184f), guardColour = new(0.941f, 0.627f, 0.125f), rivalColour = new(0.541f, 0.31f, 0.816f);
        [Tooltip("Tactical pause at the start (Sky, brief 011: on by default; T1 tests six players without it).")]
        public bool startPaused = true;
        public string playCamera = "cameras/play";
        [Tooltip("The camera stands this far back along its view (m): far enough that no roof in the block crosses the near plane.")]
        public float standOff = 160f;
        public float panSpeed = 18f;
        [Tooltip("§10: characters 40–60 px tall at 1080p. A 1.75 m capsule is 60 px at ortho size 12.5 and 40 px at 18.5 (14 = play zoom, 53 px).")]
        public float zoomMin = 12.5f, zoomMax = 18.5f;

        public Game Game { get; private set; }
        public SimBundle.Loaded Bundle { get; private set; }
        public bool Paused { get; private set; }
        /// <summary>The briefing's Begin (the kit's button; Space does the same): the clock runs and the briefing closes.</summary>
        public void StartClock() { Paused = false; ShowBriefing = false; Status = "Running."; ClockStarted = Time.unscaledTime; }
        /// <summary>Brief 014: when the clock last started from the briefing (the goal line shows for its seconds from here).</summary>
        public float ClockStarted { get; private set; } = -100f;
        /// <summary>The Next Step line: whether the HUD shows it now (its ring shows with it), and when the player asked for it again (H, the "?").</summary>
        public bool HintLineShowing { get; set; } public float HintReopenAt { get; set; } = -1f;
        /// <summary>For scripted captures and checks (the route scripts, the budget shots): hold the clock, or let it run.</summary>
        public void SetPaused(bool p) { Paused = p; if (!p) { ShowBriefing = false; if (ClockStarted < 0) ClockStarted = Time.unscaledTime; } }
        public int Speed { get; private set; } = 1;
        public bool ShowDebug { get; private set; }
        public readonly bool[] Selected = new bool[4];
        public Exception Fault { get; private set; }
        public string Status { get; private set; } = "";
        public string SessionPath { get; private set; } = "";
        public string Ending { get; private set; } = "";
        public Debrief EndDebrief { get; private set; }
        public double StepMsMean => steps == 0 ? 0 : stepMs / steps;
        public double StepMsMax { get; private set; }
        public double FrameMsMean => frames == 0 ? 0 : frameEngineMs / frames;
        public string MissionStartsAt { get; private set; } = "17:24:30";
        public HostViews Views => views;
        /// <summary>The kit view's compact strips show only the busy, hurt, down, selected or queued agents; this shows all four (a key).</summary>
        public bool ShowAllStrips { get => allStrips ??= PlayerPrefs.GetInt(StripsPref, 0) == 1; set { allStrips = value; PlayerPrefs.SetInt(StripsPref, value ? 1 : 0); PlayerPrefs.Save(); } }
        /// <summary>Intel (the kit's panel under the objective) open or collapsed to its strip.</summary>
        public bool IntelOpen { get => intelOpen ??= PlayerPrefs.GetInt(IntelPref, 0) == 1; set { if (value == IntelOpen) return; intelOpen = value; PlayerPrefs.SetInt(IntelPref, value ? 1 : 0); PlayerPrefs.Save(); } }
        bool? intelOpen, allStrips; // the remembered collapse states (PlayerPrefs), read once
        const string IntelPref = "Suborned.Hud.IntelOpen", StripsPref = "Suborned.Hud.AllStrips";
        public Camera View => cam;

        // ---------------------------------------------------------------- verbs: four slots an agent, Q W E R

        public sealed class Slot { public string Id, Label, Glyph; public bool Dim; public Slot(string id, string label, string glyph, bool dim = false) { Id = id; Label = label; Glyph = glyph; Dim = dim; } }
        /// <summary>The agents' verbs (§03): Scout watches, listens, misleads; Fixer turns (and calls a turned contact: right click it); Tech loops and
        /// reroutes; Enforcer acts (enter at the door, take Vale at his car, put him in the getaway car) and fires.</summary>
        public static readonly Slot[][] Slots =
        {
            new[] { new Slot("watch", "Watch", "eye"), new Slot("listen", "Listen", "ear"), new Slot("mislead", "False tip", "tip") },
            new[] { new Slot("ask", "Ask", "ask"), new Slot("pay", "Pay", "$"), new Slot("help", "Help", "help"), new Slot("pressure", "Pressure", "press") },
            new[] { new Slot("loop", "Loop camera", "loop"), new Slot("reroute", "Reroute patrol", "route"), new Slot("watch", "Watch", "eye") }, // Sky 2026-10-18: the Tech watches machines (rulebook sight.kinds[*].by)
            new[] { new Slot("act", "Enter / Take / Car", "act"), new Slot("fire", "Fire", "fire"), new Slot("takedown", "Takedown", "takedown") }, // brief 013: Takedown on the first empty knob (Glyph §1)
        };
        [Tooltip("Key bindings and HUD settings: one table (remap here; a settings screen later edits the same object).")]
        public PlayControls controls = new();
        public string VerbKey(int i) => controls.Label(PlayAction.Verb1 + i);
        public string AgentKeyLabel(int i) => controls.Label(PlayAction.Agent1 + i);
        /// <summary>The box being dragged, in GUI space (the HUD draws it), else null.</summary>
        public Rect? BoxScreen { get; private set; }
        Vector2 dragFrom, lastGround, lastRight, orbitLast; bool dragging, orbiting; float lastGroundAt = -10f, lastAgentKeyAt = -10f; int lastAgentKey = -1;

        public sealed class Target { public string Kind = "", Key = "", Label = "", Id = ""; } // Id: the person a crowd capsule or body is (engine id or role), a stash id // person (role) · door · cabinet · camera (id) · car · patrol · rivals · security · hostile
        /// <summary>What a right click on the hovered thing would do now, and its price (the chip).</summary>
        public sealed class Intent
        {
            public Order Order; public int Slot = -1; public string Label = "", Usd = "$0", Time = "", Trail = "T 0", Alert = "A 0", Outcome = "", Hint = "", Walk = "", Name = "", Does = ""; public bool Move;
            /// <summary>For the kit's card: orders a turn takes (more than one = "one step worse"), a Pressure on a known secret, the walk first (s).</summary>
            public long Orders = 1; public bool Leverage; public int WalkS;
            /// <summary>"seen by" where the act is done (the engine's SeenQuote in words): "" no one, "?" an unknown watcher could, else who.</summary>
            public string Seen = "";
            /// <summary>Brief 013: a takedown's way and the engine's quote (the card's full price); extra card rows (icon, text); the chip's lead icon
            /// and seen mark ("ok", "unk", "no").</summary>
            public string How = "", Lead = "", SeenMark = ""; public TakedownQuote Td; public SeenQuote SeenQ; public readonly List<(string icon, string text)> Rows = new();
            /// <summary>A verb the selected agent can give here (a right click acts); else Hint says who can.</summary>
            public bool Actionable => Order != null && !Move;
        }

        public OpId Agent { get; private set; } = OpId.Scout;   // the agent the hotbar is for (the first selected)
        public int Picked { get; private set; } = -1;           // the 1–4 pick, -1 = the default for the target
        public Target Hover { get; private set; }
        public Intent HoverIntent { get; private set; }
        public int LitSlot => HoverIntent?.Slot ?? (Picked >= 0 ? Picked : 0);
        public Vector2 Mouse { get; private set; }

        HostViews views; Transform viewRoot; Camera cam; TacticalShadows shadows; ArchitectureFade fade;
        Vector3 focus; float acc; int lastLog; double stepMs, frameEngineMs; long steps, frames;
        string intentKey = ""; GameObject background; Vector2 lastMouse = new(-1, -1), lastHover; readonly List<Camera> covered = new();
        readonly Stopwatch sw = new();

        void Start()
        {
            var cg = GameObject.Find(playCamera); cam = cg ? cg.GetComponent<Camera>() : Camera.main;
            if (cam)
            {
                shadows = cam.GetComponent<TacticalShadows>(); fade = cam.GetComponent<ArchitectureFade>();
                if (cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() is { } ucd) { ucd.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing; ucd.antialiasingQuality = UnityEngine.Rendering.Universal.AntialiasingQuality.Medium; } // brief 020 (Glint §3): no AA anywhere made every figure's edge jagged
                focus = cam.transform.position + cam.transform.forward * (shadows ? shadows.focusDepth : 80f);
                focus.y = 0f; PlaceCamera(); TurnInit();
            }
            if (!GetComponent<HudToolkit>()) gameObject.AddComponent<HudToolkit>(); // Glyph's kit on UI Toolkit (off until the kit is imported)
            if (!GetComponent<CameraLab>()) gameObject.AddComponent<CameraLab>(); // brief 025: the camera lab (P)
            CamApply(); // the camera values (CameraLab loads the default preset on its first frame)
            background = GameObject.Find("people");
            if (background) background.SetActive(false); // the engine's crowd steps the city's people; the editor snapshot would double them
            Begin();
        }

        /// <summary>A new run from the start state (Start, and Return after the end card).</summary>
        public void Begin()
        {
            if (FailCamOn) FailCamStop(); endGame = null;
            if (Following) FollowExit(); // a restart opens on the overhead view
            DriveReset(); // brief 025: nobody driven
            if (Game != null && Fault == null) Save(); // a restart closes the run being left at its last tick
            Fault = null; Ending = ""; EndDebrief = null; lastLog = 0; acc = 0f; steps = 0; stepMs = 0; StepMsMax = 0; frames = 0; frameEngineMs = 0;
            Paused = startPaused; Speed = 1; Picked = -1; intentKey = ""; HoverIntent = null; Hover = null; Panel = null; PanelHover = -1; Array.Clear(pending, 0, 4); Pops.Clear(); Pulses.Clear();
            if (viewRoot) Destroy(viewRoot.gameObject);
            Guard("load", () =>
            {
                Bundle ??= SimBundle.Load();
                Text ??= PlayText.Load(); LoadSeats(); // the café seats (HostSeats), view only
                Game = Engine.New(Bundle.Inputs, Lab.ToStart()); // brief 025: the playground switches (P) are the start state's; none set = the plain start
                if (Game.S.Root.Opt("cast")?.Opt("startsAt") is JNode st && st.Kind == JKind.Str) MissionStartsAt = st.Str();
            });
            if (Fault != null) return;
            viewRoot = new GameObject("engine").transform; viewRoot.SetParent(transform, false);
            views = new HostViews(this, viewRoot, frame);
            views.Tick(); views.Tick(); views.Frame(1f);
            Select(OpId.Scout, false);
            Text.ResetUses(); Talk.Clear(); ShowBriefing = true; LogOpen = false; Array.Clear(pendingAt, 0, 4); Loud = null; sweeping = false; AimLine = null; MoveChip = null; ResetHints(); ResetFeed(); beatAt.Clear(); crowdSt = null; sawCount.Clear();
            ResetLess(); Track(); Notice(); lastWatchOrders = -1;
            // open on the cell (Waymark 2026-10-16: the four stand by the getaway car's boarding pavilion): centred on block (-2.5, -40.5), play zoom 14,
            // so the cell sits below the briefing card at any resolution; Replay opens the same way
            TurnReset(); if (cam) { cam.orthographicSize = Mathf.Clamp(Cam.overhead.zoom, zoomMin, zoomMax); CamProjection(); } LookAt(frame.World(OpeningFocus)); // the preset's zoom (brief 025; built-in 14)
            SessionPath = SessionFile();
            Save();
            Status = $"Paused. {controls.Label(PlayAction.Pause)} starts the clock; {controls.Label(PlayAction.Briefing)} shows the briefing again.";
        }

        /// <summary>Only the play camera draws to the screen: a review tool's camera (an open Flyby window makes one at depth 100, sometimes
        /// after this host starts) would cover the game. Checked every 15 frames; each is turned back on when the host goes.</summary>
        void Uncover()
        {
            if (!cam || Time.frameCount % 15 != 1) return;
            foreach (var c in Resources.FindObjectsOfTypeAll<Camera>())
                if (c != cam && c.enabled && c.gameObject.activeInHierarchy && c.targetTexture == null && c.cameraType == CameraType.Game && c.depth >= cam.depth)
                { c.enabled = false; if (!covered.Contains(c)) covered.Add(c); }
        }

        void Update()
        {
            Uncover();
            if (Game == null) return;
            var kb = Keyboard.current;
            if (controls.Down(kb, PlayAction.Debug)) ShowDebug = !ShowDebug;
            if (Fault != null) return;
            if (MenuKeys(kb)) return; // the pause menu is up: Esc resumes, its buttons act; nothing else this frame
            if (FailCamFrame()) return; // the fail camera plays (HostFailCam): nothing else this frame
            if (Ending.Length > 0) { ClearHover(); if (kb != null && kb.escapeKey.wasPressedThisFrame) { OpenMenu(); return; } if (controls.Down(kb, PlayAction.Restart)) Begin(); CameraOnly(kb, UnityEngine.InputSystem.Mouse.current); views?.Frame(1f); FollowFrame(); FadePoints(); return; } // the end card is modal (ModalGate)
            if (!CameraLab.Typing) Controls(); // brief 025: no keys reach the street while a preset name is typed
            if (Fault != null) return;
            DriveFrame(kb); // brief 025: the driven agent's controller, before the clock steps (HostDrive)
            if (!Paused && !Game.Over && !(Panel != null && controls.pauseWithPanel))
            {
                acc += Time.unscaledDeltaTime * Speed;
                int n = 0; double t0 = Now();
                while (acc >= 0.1f && n < 80 && !Game.Over) { if (!StepOnce()) return; acc -= 0.1f; n++; }
                if (n == 80) acc = 0f;
                frameEngineMs += Now() - t0;
                if (Game.Tick - savedTick >= 5 * Game.Hz && Fault == null && !Game.Over) Save();
            }
            frames++;
            if (Game.Over && Ending.Length == 0) End();
            views.Frame(Paused || Game.Over ? 1f : Mathf.Clamp01(acc / 0.1f));
            FollowFrame(); // the test follow view (HostFollow): after the views moved
            FadePoints();
            DoorViewFrame(); // GDD v0.24: the Office door's clear-view clock (H13's door cause)
            ShowSight();
            Guard("street marks", StreetMarks);
            for (int k = 0; k < 4; k++)
            {
                var t = views.Find("op/" + Op.Name((OpId)k));
                views.ShowRange(k, t ? t.position : Vector3.zero, Selected[k] && Armed(k) && Drawn(k) && t && t.gameObject.activeSelf ? Game.Ops[k].Gun.W.EffCm / 100f : 0f);
            }
            if (moveChipDirty && Time.unscaledTime - moveChipTime >= 0.2f) UpdateMoveChip(lastHover, true);
        }

        bool StepOnce()
        {
            double t0 = Now();
            DriveTick(); // brief 025: the driven agent's sample for this tick, before the step (HostDrive)
            bool ok = Guard("step", () => Game.Step());
            double ms = Now() - t0; stepMs += ms; steps++; if (ms > StepMsMax) StepMsMax = ms;
            if (ok) { DriveAfterStep(); views.Tick(); FollowUp(); Track(); Notice(); ClockBeats(); UpdateAim(); Hints(); CityBeats(); FeedClock(); if (MoveChip != null && Game.Tick % 10 == 0) moveChipDirty = true; }
            Drain();
            return ok;
        }

        /// <summary>New lines in the engine's log (a step's, or an order's between steps): pops over their people, and the dialogue log.</summary>
        void Drain()
        {
            if (Game.Log.Count <= lastLog) return;
            for (int i = lastLog; i < Game.Log.Count; i++) { var x = Game.Log[i]; HintLog(x); if (x.Code.StartsWith("shot.")) { Shot(x); continue; } FeedEvent(x); SayLanded(x); }
            var l = Game.Log[Game.Log.Count - 1]; Status = $"{Report.Clock(l.Tick)} {l.Text}"; lastLog = Game.Log.Count;
        }

        double Now() { if (!sw.IsRunning) sw.Start(); return sw.Elapsed.TotalMilliseconds; }

        // ---------------------------------------------------------------- controls

        /// <summary>The modal up now, if any (ModalGate): the briefing until Begin, the end card.</summary>
        public ModalGate.Modal Modal => MenuOpen ? ModalGate.Modal.Menu : Ending.Length > 0 ? ModalGate.Modal.EndCard : ShowBriefing ? ModalGate.Modal.Briefing : ModalGate.Modal.None;

        /// <summary>Nothing hovered: no price chip, ring, aim or walk chip (a modal opened, or the run ended).</summary>
        public void ClearHover() { HoverAnchor = null; HoverName = ""; Hover = null; HoverIntent = null; MoveChip = null; MoveNow = null; AimLine = null; AimNow = null; intentKey = "modal"; dragging = false; BoxScreen = null; }

        /// <summary>Under a modal: the camera only (pan keys, middle-drag grab, wheel zoom).</summary>
        void CameraOnly(Keyboard kb, UnityEngine.InputSystem.Mouse mouse)
        {
            var c = controls;
            FollowKey(kb);
            if (Following) { FollowInput(kb, mouse, mouse != null && HudToolkit.Blocks(mouse.position.ReadValue())); return; } // the follow view: its own turn and distance, no pan
            if (kb != null)
            {
                var pan = Vector2.zero;
                if (c.Held(kb, PlayAction.PanUp)) pan.y += 1; if (c.Held(kb, PlayAction.PanDown)) pan.y -= 1;
                if (c.Held(kb, PlayAction.PanRight)) pan.x += 1; if (c.Held(kb, PlayAction.PanLeft)) pan.x -= 1;
                if (pan != Vector2.zero && cam) Pan(pan * KeyPanSpeed * Time.unscaledDeltaTime);
            }
            TurnInput(kb, mouse);
            if (mouse == null || !cam) return;
            var p = mouse.position.ReadValue();
            if (!turnDragging) Grab(p, mouse.middleButton.wasPressedThisFrame, mouse.middleButton.isPressed);
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel != 0f && !HudToolkit.Blocks(p)) OverheadWheel(wheel); // stepped or proportional (brief 025: the camera lab)
        }

        void Controls()
        {
            var kb = Keyboard.current; var mouse = UnityEngine.InputSystem.Mouse.current; var c = controls;
            if (Modal == ModalGate.Modal.Briefing) // modal until Begin: Space or the button begins; B and Esc close it; the camera may look round
            {
                ClearHover(); if (Panel != null) ClosePanel();
                if (c.Down(kb, PlayAction.Pause)) { Paused = false; ClockStarted = Time.unscaledTime; ShowBriefing = false; Status = "Running."; }
                else if (c.Down(kb, PlayAction.Briefing) || (kb != null && kb.escapeKey.wasPressedThisFrame)) ShowBriefing = false;
                CameraOnly(kb, mouse);
                return;
            }
            bool shift = c.Held(kb, PlayAction.AddToGroup);
            if (kb != null)
            {
                if (c.Down(kb, PlayAction.Pause)) { Paused = !Paused; Status = Paused ? "Paused." : "Running."; if (!Paused) { if (ShowBriefing) ClockStarted = Time.unscaledTime; ShowBriefing = false; } else Save(); }
                if (c.Down(kb, PlayAction.Briefing)) ShowBriefing = !ShowBriefing;
                if (c.Down(kb, PlayAction.DialogueLog)) FeedHistoryOpen = !FeedHistoryOpen; // L: the feed's history (the old dialogue log is the ticker now)
                if (c.Down(kb, PlayAction.Cancel)) CancelSelected();
                if (Loud == null && c.Down(kb, PlayAction.Draw)) DrawOrHolster();
                if (c.Down(kb, PlayAction.Cease)) CeaseFire();
                if (c.Down(kb, PlayAction.Stance)) CycleStance();
                if (c.Down(kb, PlayAction.Hints)) { if (shift) RelearnHints(); else if (HintNow != null && !HintLineShowing && HintsOn) HintReopenAt = Time.unscaledTime; else ToggleHints(); } // H: a folded hint comes back, else the layer on or off; Shift+H: the retired control hints come back
                if (c.Down(kb, PlayAction.Intel)) IntelOpen = !IntelOpen;
                if (c.Down(kb, PlayAction.Response)) ResponseOpen = !ResponseOpen;
                if (c.Down(kb, PlayAction.Gait) && !Driving) ToggleGaitSelected(); // driven: G latches the run (HostDrive)
                if (c.Down(kb, PlayAction.AllStrips)) ShowAllStrips = !ShowAllStrips;
                if (c.Down(kb, PlayAction.HudView)) controls.view = controls.view == HudView.Kit ? HudView.Imgui : HudView.Kit;
                if (c.Down(kb, PlayAction.Acknowledge)) AcknowledgeNewest();
                FollowKey(kb); // T: the test follow view (HostFollow)
                if (kb.leftCtrlKey.wasPressedThisFrame || kb.leftCtrlKey.wasReleasedThisFrame || kb.rightCtrlKey.wasPressedThisFrame || kb.rightCtrlKey.wasReleasedThisFrame) UpdateAim();
                for (int i = 0; i < 4; i++)
                {
                    if (c.Down(kb, PlayAction.Verb1 + i)) { if (Panel != null) PanelKey(i); else Pick(i); }
                    if (c.Down(kb, PlayAction.Agent1 + i)) AgentKey(i, shift);
                }
                if (kb.escapeKey.wasPressedThisFrame && Loud == null) { if (Panel != null) ClosePanel(); else if (ShowBriefing) ShowBriefing = false; else if (Picked >= 0) { Picked = -1; intentKey = ""; } else if (FeedHistoryOpen) FeedHistoryOpen = false; else OpenMenu(); } // nothing nearer to close: the pause menu
                if (c.Down(kb, PlayAction.CycleAgent) && HoverStackCount > 1 && !TargetMode) StepStack(); // things overlap under the cursor: Tab steps through them
                else if (c.Down(kb, PlayAction.CycleAgent) && TargetMode) FocusNext(); // a verb picked: Tab steps through its targets, nearest first (HostPick)
                else if (c.Down(kb, PlayAction.CycleAgent)) { var next = (OpId)(((int)Agent + 1) % 4); Select(next, false); Status = $"{Op.Name(next)} selected"; }
                if (!Driving) // driven: real time (the controller's pace), no skipping ahead
                {
                    if (c.Down(kb, PlayAction.NextEvent)) NextEvent();
                    if (c.Down(kb, PlayAction.Faster)) Speed = Mathf.Min(8, Speed * 2);
                    if (c.Down(kb, PlayAction.Slower)) Speed = Mathf.Max(1, Speed / 2);
                }
                var pan = Vector2.zero;
                if (c.Held(kb, PlayAction.PanUp)) pan.y += 1; if (c.Held(kb, PlayAction.PanDown)) pan.y -= 1;
                if (c.Held(kb, PlayAction.PanRight)) pan.x += 1; if (c.Held(kb, PlayAction.PanLeft)) pan.x -= 1;
                if (pan != Vector2.zero && cam && !Following) Pan(pan * KeyPanSpeed * Time.unscaledDeltaTime);
            }
            GateFrame(kb, mouse);
            if (Following) FollowInput(kb, mouse, mouse != null && (PlayHud.Blocks(mouse.position.ReadValue()) || HudToolkit.Blocks(mouse.position.ReadValue()))); else TurnInput(kb, mouse);
            if (mouse == null || !cam) return;
            var p = mouse.position.ReadValue();
            // both buttons together: panic, read only once a fight is on (outside one they are just two clicks: no order, no refusal, no card);
            // the drag and the right click's own act give way
            if (Game.FightOn && ((mouse.leftButton.wasPressedThisFrame && mouse.rightButton.isPressed) || (mouse.rightButton.wasPressedThisFrame && mouse.leftButton.isPressed)))
            { dragging = false; BoxScreen = null; if (Loud == null) Panic(p); return; }
            Sweep(p, mouse.rightButton.isPressed);
            if (!turnDragging && !Following) Grab(p, mouse.middleButton.wasPressedThisFrame, mouse.middleButton.isPressed);
            if (c.edgeScroll && Application.isFocused && !Following)
            {
                float band = c.edgePixels * Screen.height / 1080f; var e = Vector2.zero;
                if (p.x <= band) e.x -= 1; if (p.x >= Screen.width - band) e.x += 1; if (p.y <= band) e.y -= 1; if (p.y >= Screen.height - band) e.y += 1;
                if (e != Vector2.zero) Pan(e * KeyPanSpeed * Time.unscaledDeltaTime);
            }
            float wheel = mouse.scroll.ReadValue().y; // over the HUD the wheel scrolls the HUD (the action panel's cards), not the zoom
            if (wheel != 0f && HoverStackCount > 1 && !(PlayHud.Blocks(p) || HudToolkit.Blocks(p))) { StepStack(); wheel = 0f; } // over overlapping things the wheel steps through them
            if (wheel != 0f && !Following && !(PlayHud.Blocks(p) || HudToolkit.Blocks(p))) OverheadWheel(wheel); // the follow view's wheel is its distance (FollowInput); stepped or proportional (brief 025)
            if (Driving && DriveFocus != null) lastMouse = p;             // driven: the chip is on what E acts on (HostDrive), not under the cursor
            else if (p != lastMouse) { lastMouse = p; PointerHover(p); }      // the price is redone only on change (Preview is ~500 KB a call)
            else if (intentKey.Length == 0) PointerHover(lastHover);    // the agent, the verb or the world changed under a still mouse
            bool onHud = PlayHud.Blocks(p) || DebugHud.Blocks(p) || HudToolkit.Blocks(p);
            if (mouse.leftButton.wasPressedThisFrame && !onHud && Panel != null) { ClosePanel(); return; } // a left click elsewhere closes the panel
            if (mouse.leftButton.wasPressedThisFrame && !onHud) { dragFrom = p; dragging = true; }
            if (dragging && Following && Cam.follow.orbitDrag) // brief 025 (the camera lab): in the follow view a left-drag orbits; a click still moves
            { if (mouse.leftButton.isPressed && (orbiting || (p - dragFrom).magnitude > 6f)) { if (!orbiting) { orbiting = true; orbitLast = dragFrom; } FollowOrbit(p - orbitLast); orbitLast = p; } }
            else if (dragging && mouse.leftButton.isPressed && (p - dragFrom).magnitude > 6f) BoxScreen = GuiRect(dragFrom, p);
            if (dragging && mouse.leftButton.wasReleasedThisFrame)
            {
                dragging = false;
                if (orbiting) orbiting = false;
                else if (BoxScreen.HasValue) { BoxSelect(BoxScreen.Value, shift); BoxScreen = null; }
                else PointerLeft(p, shift);
            }
            if (mouse.rightButton.wasPressedThisFrame && !onHud) PointerRight(p);
        }

        /// <summary>An agent's key (F1–F4): select (Shift adds to the group); pressed twice quickly, the camera centres on them.</summary>
        public void AgentKey(int i, bool add)
        {
            bool twice = lastAgentKey == i && Time.unscaledTime - lastAgentKeyAt < controls.doubleSeconds;
            lastAgentKey = i; lastAgentKeyAt = Time.unscaledTime;
            Select((OpId)i, add);
            if (twice) CentreOn((OpId)i);
        }

        /// <summary>A roster portrait clicked: as the agent's key (double click centres).</summary>
        public void PortraitClick(int i, bool add) => AgentKey(i, add);

        public void CentreOn(OpId id) { var op = Game.Ops[(int)id]; LookAt(frame.World(op.Pos)); Status = $"centred on {Op.Name(id)}"; }

        static Rect GuiRect(Vector2 a, Vector2 b) => Rect.MinMaxRect(Mathf.Min(a.x, b.x), Screen.height - Mathf.Max(a.y, b.y), Mathf.Max(a.x, b.x), Screen.height - Mathf.Min(a.y, b.y));

        /// <summary>Every agent whose feet are inside the dragged box (GUI space) is selected; Shift adds them to the selection.</summary>
        public void BoxSelect(Rect gui, bool add)
        {
            var pick = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                var op = Game.Ops[i]; if (op.Inside) continue;
                var sp = cam.WorldToScreenPoint(frame.World(op.Pos)); if (sp.z <= 0) continue;
                if (gui.Contains(new Vector2(sp.x, Screen.height - sp.y))) pick.Add(i);
            }
            if (pick.Count == 0) { if (!add) Status = "no agent in the box"; return; }
            if (!add) Array.Clear(Selected, 0, 4);
            foreach (var i in pick) Selected[i] = true;
            Agent = (OpId)Array.IndexOf(Selected, true); Picked = -1; intentKey = "";
            Status = $"{pick.Count} agent{(pick.Count > 1 ? "s" : "")} selected";
        }

        /// <summary>The keys' (and the edge's) pan speed in m/s: controls.keyPanSpeed at play zoom 14, scaled with the zoom so the screen moves at
        /// the same rate whatever the zoom.</summary>
        float KeyPanSpeed => controls.keyPanSpeed * (cam ? cam.orthographicSize / 14f : 1f);

        bool grabbing; Plane grabPlane; Vector3 grabWorld;

        /// <summary>Middle-drag grabs the map (Sky: "middle wheel drag is really slow compared to WASD"): the ground point under the cursor at the
        /// press stays under the cursor while it is held, 1:1 at any zoom (controls.dragPan scales it; 1 = grab). The point is found on the ground
        /// (the walk heights), then held on its horizontal plane, so the drag is exact for an orthographic camera.</summary>
        void Grab(Vector2 screen, bool pressed, bool held)
        {
            if (!held) { grabbing = false; return; }
            if (pressed || !grabbing)
            {
                var ray = cam.ScreenPointToRay(screen);
                if (!frame.Ground(ray, out grabWorld)) { var flat = new Plane(Vector3.up, Vector3.zero); if (!flat.Raycast(ray, out var d0)) return; grabWorld = ray.GetPoint(d0); }
                grabPlane = new Plane(Vector3.up, grabWorld); grabbing = true; return;
            }
            var r = cam.ScreenPointToRay(screen);
            if (!grabPlane.Raycast(r, out var d)) return;
            var under = r.GetPoint(d); var move = (grabWorld - under) * controls.dragPan; move.y = 0f;
            if (move.sqrMagnitude < 1e-8f) return;
            focus += move; TurnMoved(move); PlaceCamera(); // the grabbed point is back under the cursor
        }

        void Pan(Vector2 screen)
        {
            var right = cam.transform.right; right.y = 0; right.Normalize();
            var up = cam.transform.forward; up.y = 0; up.Normalize();
            var d = right * screen.x + up * screen.y / Mathf.Sin(cam.transform.eulerAngles.x * Mathf.Deg2Rad); // the screen's own axes: at any yaw a key pans along the screen
            focus += d; TurnMoved(d);
            PlaceCamera();
        }

        void PlaceCamera()
        {
            if (Following) return; // the follow view places itself; the overhead view comes back as it was left
            float dist = CamDistance; // the stand-off, or (perspective, brief 025) the distance that shows the zoom's half-height at the focus
            cam.transform.position = focus - cam.transform.forward * dist;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, dist + 1500f);
            if (shadows) shadows.focusDepth = dist;
            if (fade) fade.focus = focus;
        }

        /// <summary>Pan so the camera centres on a world point (zoom, pitch and yaw stay; a turn under way keeps turning about it).</summary>
        public void LookAt(Vector3 world) { if (!cam) return; focus = new Vector3(world.x, 0f, world.z); TurnLookAt(world); PlaceCamera(); }

        public void Select(OpId id, bool add)
        {
            if (!add) Array.Clear(Selected, 0, 4);
            Selected[(int)id] = add ? !Selected[(int)id] : true;
            int first = Array.IndexOf(Selected, true);
            var agent = first < 0 ? id : (OpId)first;
            if (agent != Agent) Picked = -1;
            Agent = agent; intentKey = "";
        }

        public void Pick(int slot)
        {
            var plate = PlateOf(Agent);
            if (slot < plate.Length && plate[slot].Dim && plate[slot].Id == "fire" && Game?.Vale == ValeAt.WithEnforcer && Agent == OpId.Enforcer) { var lk = new Answer { ok = false, code = "fire-carrying-vale", why = "walking Vale, the Enforcer doesn't fire", m = -1, cm = -1, usd = -1, tick = -1 }; var o = new Order { Op = "enforcer", Do = "fire" }; Pop(ShortOf(o, lk) ?? lk.why, null, SelectedWorld(), Feedback.Refused, 2f, PopIcon(lk.code), LockLine(o, lk) ?? lk.why); return; } // the greyed Fire knob says why
            if (slot >= plate.Length || plate[slot].Dim) { Status = $"{Op.Name(Agent)} has no verb on {VerbKey(slot)}"; return; }
            if (plate[slot].Id is "drop" or "stash") { PlateAct(plate[slot].Id); return; } // carrying: Put down and Hide act at once (Glyph §4)
            if (plate[slot].Id == "takedown") { Way = Picked == slot && Way == "knockout" ? "kill" : "knockout"; } // 4 again flips; kill is never the default (Glyph §1)
            Picked = slot; intentKey = "";
            Status = $"{VerbKey(slot)}: {plate[slot].Label}{(plate[slot].Id == "takedown" ? $" ({(Way == "kill" ? "Kill" : "Knock out")})" : "")} — right click a target";
        }

        /// <summary>The mouse over the world (pixels, origin bottom left): what is under it, and what a right click would do there, priced once per change.</summary>
        public void PointerHover(Vector2 screen)
        {
            if (!ModalGate.AllowsHover(Modal)) { ClearHover(); return; } // nothing in the street answers under a modal
            lastHover = screen; Mouse = new Vector2(screen.x, (cam ? cam.pixelHeight : Screen.height) - screen.y); // GUI space: origin top left
            var t = TargetAt(screen);
            string key = $"{t?.Kind}:{t?.Key}:{Agent}:{Picked}:{Game.Orders.Count}";
            if (key == intentKey) { if (t == null) UpdateMoveChip(screen, false); return; }
            intentKey = key; Hover = t; HoverFeedback(t);
            HoverIntent = t == null ? null : IntentFor(t);
            UpdateAim();
            if (t == null) UpdateMoveChip(screen, false); else MoveChip = null;
        }

        /// <summary>Left click: on an agent, select (Shift adds); anywhere else, the selection walks there (roads cross anywhere). A second
        /// click on the same spot within the double-click time asks to run, which the engine has no rule for yet: the agents walk, and the HUD
        /// says so once (Keel and Motive are writing the running rule; no speed is invented here).</summary>
        public void PointerLeft(Vector2 screen, bool shift = false)
        {
            if (!ModalGate.AllowsClicks(Modal)) return;
            var t = TargetAt(screen, true);
            if (t != null && t.Kind == "op") { Select(Op.Parse(t.Key), shift); return; }
            bool twice = Time.unscaledTime - lastGroundAt < controls.doubleSeconds && (screen - lastGround).magnitude < 24f;
            lastGroundAt = Time.unscaledTime; lastGround = screen;
            if (twice)
            {
                if (!RunInRulebook) { if (!runNoted) { runNoted = true; Status = "run: not yet in the rulebook (walking)"; } return; } // the first click already sent the walk
                MoveSelection(screen, true); // the second click turns the walk into a run (Keel: "run": true, 350 cm/s; carrying Vale stays at walking pace)
                return;
            }
            MoveSelection(screen);
        }

        /// <summary>Whether the engine's order format has a run flag (Order.Run; Keel a295f76: opt-in on a move).</summary>
        public static readonly bool RunInRulebook = typeof(Order).GetField("Run") != null || typeof(Order).GetProperty("Run") != null;
        bool runNoted;

        /// <summary>Right click: on a person or object, the selected agent's current verb (the hovered intent, priced first); on empty ground, Cancel.</summary>
        public void PointerRight(Vector2 screen)
        {
            if (!ModalGate.AllowsClicks(Modal)) return;
            lastRight = screen;
            if (Game == null || Fault != null || !cam) return;
            var t = TargetAt(screen);
            if (t != null && t.Kind == "op") { if (CarryDowned(t, screen)) return; t = null; }
            bool ctrl = controls.Held(Keyboard.current, PlayAction.Force);
            if (t != null && ShotAt(t, ctrl, out var fireAt, out var force)) { if (Panel != null) ClosePanel(); FireSelected(fireAt, force, screen, "right"); sweeping = true; return; }
            if (t != null && t.Kind == "person" && t.Key == "councillor" && ctrl) { PopKey("never-vale", screen, TargetWorld(t), Feedback.Refused, "never fire on Vale: the mission fails"); return; }
            if (t != null && t.Kind != "op")
            {
                if (Panel == null && BoardSelection(t, screen)) return; // brief 018: the whole cell selected, a right click on the car boards each
                PointerHover(screen);
                var i = HoverIntent;
                if (Picked >= 0 && i != null && i.Actionable) { if (i.Order.Do == "go") GoGive(Agent, screen, "right"); else if (!SeenGate(i, t, screen, "right")) Give(Agent, i.Order, t, screen); return; } // the fast path: a verb picked on the hotbar fires at once (a seen takedown asks for the hold first)
                OpenPanel(t, screen);                                                                       // else the action panel (Sky: "expecting an actions panel to pop out")
                return;
            }
            if (Panel != null) { ClosePanel(); return; } // right click on empty ground closes the panel, and cancels nothing else
            Cancel();
        }

        /// <summary>Right click on empty ground: drop the picked verb; with none picked, stop the selected agents' walks (the engine's cancel order,
        /// and the act they walked for is dropped). An act under way is cancelled with C or an ✕ only.</summary>
        public void Cancel()
        {
            if (Picked >= 0) { Picked = -1; intentKey = ""; Status = "verb cancelled"; return; } // silent: the knob goes dark
            // a right click on empty ground is easy to make by mistake: it stops walks only (an act is cancelled with C or an ✕, which say the cost)
            int n = 0;
            for (int k = 0; k < 4; k++) if (Selected[k] && doing[k].Phase is "walking" or "running") { CancelAgent(k, lastRight); n++; }
            if (n == 0) Status = "nothing to stop"; // nothing said (Fable B10)
        }

        /// <summary>The selection walks to the ground under a screen point; a group stands in a 0.9 m square.</summary>
        public void MoveSelection(Vector2 screen, bool run = false)
        {
            for (int n = 0; n < 4; n++) if (Selected[n]) pending[n] = null; // a new walk replaces the order an agent was walking for
            if (!frame.Ground(cam.ScreenPointToRay(screen), out var w)) return;
            var at = frame.Engine(w); int k = 0; bool said = false; // said: the one "can't walk there" for the group
            for (int n = 0; n < 4; n++)
            {
                if (!Selected[n]) continue;
                // snap to the nearest walkable cell of the engine's grid within 3 m (the café terrace, planters, the fountain are solids), and
                // send the snapped point, so the order is what the agent does and the replay is exact
                var want = new Pt(at.X + (k % 2) * 90, at.Y + (k / 2) * 90); k++;
                Pt? to = Game.S.Grid.Free(want) ? want : Game.S.Grid.NearestFree(want);
                if (!to.HasValue || G.Dist(to.Value, want) > 300) { if (!said) PopKey("no-way", screen, null, Feedback.Refused, "Can't walk there."); said = true; continue; }
                if (Issue(new Order { Op = Op.Name((OpId)n), Do = "move", Point = to.Value, Run = run }, screen, null)) // a walk needs no voice (Fable §4: move and run acks are silent)
                { Pulses.Add((frame.World(to.Value), Feedback.Ok, Time.unscaledTime)); while (Pulses.Count > 8) Pulses.RemoveAt(0); } // the click's pulse at the destination
            }
        }

        /// <summary>The thing under a screen point. Left clicks look for agents first; hover and right clicks for the city's people, then things
        /// (an agent standing in front of Inez must not swallow the order meant for her).</summary>
        Target TargetAt(Vector2 screen, bool agents = false)
        {
            if (!cam || viewRoot == null) return null;
            // people before things: the door, cabinet and car markers are generous boxes, and the guard stands in the door's. Everything under the
            // ray is kept, best first (the overlap stack, HostPick): the selected agent's best verb breaks ties (Sky, route B: "it kept landing on
            // the driver", Vale at his car with the Enforcer selected), and Tab or the wheel steps through a stack
            var found = new List<(Target t, float score)>();
            foreach (var hit in Physics.RaycastAll(cam.ScreenPointToRay(screen), 5000f))
            {
                if (!hit.collider.transform.IsChildOf(viewRoot)) continue;
                var t = TargetOf(hit.collider.transform); if (t == null) continue;
                int r = t.Kind == "op" ? (agents ? 0 : 3) : t.Kind is "person" or "hostile" or "patrol" or "rivals" or "security" or "body" or "unit" ? (agents ? 1 : 0) : t.Kind == "crowd" ? (agents ? 3 : 2) : (agents ? 2 : 1);
                float score = r * 10000f + hit.distance - (agents ? 0 : Prefer(t));
                int at = found.FindIndex(x => x.t.Kind == t.Kind && x.t.Key == t.Key);
                if (at < 0) found.Add((t, score)); else if (score < found[at].score) found[at] = (t, score);
            }
            // brief 016 (Sky: "hard to select areas, like a camera"): nothing under the ray, or only a passer-by: the nearest thing within the pick
            // radius of the cursor wins over the ground (HostPick)
            if (found.Count == 0 || found.TrueForAll(x => x.t.Kind == "crowd")) { var np = NearestPick(screen, agents); if (np != null) found.Insert(0, (np, -1f)); }
            found.Sort((a, b) => a.score.CompareTo(b.score));
            var list = new List<Target>(); foreach (var x in found) if (x.t.Kind != "crowd" || list.Count == 0 || found.TrueForAll(y => y.t.Kind == "crowd")) list.Add(x.t);
            return Stacked(screen, list, agents);
        }

        Target TargetOf(Transform t)
        {
            for (var x = t; x && x != viewRoot; x = x.parent)
            {
                var n = x.name;
                if (n.StartsWith("op/")) return new Target { Kind = "op", Key = n[3..], Label = Game.Ops[(int)Op.Parse(n[3..])].Down ? $"the downed {n[3..]}" : n[3..] };
                if (n is "waitress" or "driver" or "guard" or "councillor") return new Target { Kind = "person", Key = n, Label = Name(n), Id = n };
                if (n == "patrol") return new Target { Kind = "patrol", Key = "patrol", Label = "the patrol" };
                if (n.StartsWith("rivals/")) return new Target { Kind = "rivals", Key = "rivals", Label = "the rival crew" };
                if (n.StartsWith("security/")) return new Target { Kind = "security", Key = "security", Label = "the response unit" };
                if (n.StartsWith("hostile/"))
                {
                    int i = int.Parse(n[8..]); if (i >= Game.Hostiles.Count || Game.Hostiles[i].Down) return null;
                    var id = Game.Hostiles[i].Id;
                    return new Target { Kind = "hostile", Key = id, Label = id.StartsWith("rival") ? "a rival" : "an officer" };
                }
                if (n.StartsWith("crowd/")) { int ci = int.Parse(n[6..]); var cp = ci < Game.Walkers.Count ? Game.Walkers[ci].P : null; return new Target { Kind = "crowd", Key = n, Label = "a passer-by", Id = cp?.Id ?? "" }; }
                if (n.StartsWith("body/")) { var bp = Game.PersonById(n[5..]); return bp == null ? null : new Target { Kind = "body", Key = n[5..], Id = n[5..], Label = PersonName(bp) }; }
                if (n.StartsWith("stash/")) { var st = Game.S.Stashes.Find(x => x.Id == n[6..]); return st == null ? null : new Target { Kind = "stash", Key = st.Id, Id = st.Id, Label = StashName(st) }; }
                if (n.StartsWith("unit/")) return new Target { Kind = "unit", Key = n[5..], Label = n.Contains("robot") ? "a response robot" : "an officer" };
                if (n == "car/official") return new Target { Kind = "car", Key = "official", Label = "the official car" };
                if (n == "car/getaway") return new Target { Kind = "car", Key = "getaway", Label = "the getaway car" };
                if (n == "marker/door") return new Target { Kind = "door", Key = "door", Label = "the public door" };
                if (n == "marker/cabinet") return new Target { Kind = "cabinet", Key = "cabinet", Label = "the control cabinet" };
                if (n.StartsWith("camera/")) return new Target { Kind = "camera", Key = n[7..], Label = CameraName(n[7..]) };
            }
            return null;
        }

        /// <summary>The agent's verb on a target: the picked slot when it applies, else the first slot that does, else walking there.</summary>
        Intent IntentFor(Target t)
        {
            if (t.Kind == "op") return null;
            if (t.Kind == "unit") return new Intent { Label = t.Label };
            if (t.Kind == "crowd" && Agent != OpId.Enforcer) return new Intent { Label = "a passer-by" }; // takes no orders but the Enforcer's takedown (brief 013): a hover says nothing (Fable A8)
            if (t.Kind is "body" or "stash") { Intent b = null; Guard("price", () => { var l = BodyIntents(Agent, t); b = l.Count > 0 ? l[0] : new Intent { Label = t.Label, Hint = WhoCan(t) }; }); return b; }
            Intent got = null;
            if (IsGetaway(t)) { Guard("price", () => got = Game.Ops[(int)Agent].InCar || (Game.AnyInCar && BoardLock(Agent).code is "in-car" or "agent-down") ? GoIntent(Agent) : BoardIntent(Agent)); if (got != null) { if (got.Order?.Do == "board" && got.WalkS == 0) Guard("walk", () => { var aw = ActPlace(Agent, got.Order, t); if (aw != null) got.WalkS = aw.Seconds; }); return got; } } // brief 018: Board, or Go from the car
            Guard("price", () =>
            {
                var slots = PlateOf(Agent);
                if (Agent == OpId.Fixer && t.Kind == "person" && t.Key != "councillor" && CastOf(t.Key) is { Turned: true }) { got = Call(t); return; }
                if (Picked >= 0) got = Verb(Agent, Picked, t);
                // the default never fires on a person who is not a hostile: shooting is picked on purpose (2 = Fire)
                // and never takes anyone down unless Takedown is armed (brief 013: a kill is never a default)
                for (int i = 0; got == null && i < slots.Length; i++) if (!(slots[i].Id == "fire" && t.Kind == "person") && slots[i].Id != "takedown") got = Verb(Agent, i, t);
                got ??= new Intent { Label = t.Label, Hint = WhoCan(t) };
            });
            // brief 014 (Fable §2): the chip's time is the walk plus the act, so the hover prices the walk too (one A*, on change only)
            if (got != null && got.Actionable && got.WalkS == 0) Guard("walk", () => { var aw = ActPlace(Agent, got.Order, t); if (aw != null) got.WalkS = aw.Seconds; });
            return got;
        }

        static readonly string[] AgentNames = { "Scout", "Fixer", "Tech", "Enforcer" };

        /// <summary>"Nothing here for the Scout. The Fixer can.": the first other agent with a verb on the target, stated, never a control
        /// instruction (Fable F10; the controls are taught once, by the hints).</summary>
        string WhoCan(Target t, IList<int> mine = null)
        {
            var me = mine == null || mine.Count == 0 ? AgentNames[(int)Agent] : string.Join(" or the ", new List<int>(mine).ConvertAll(k => AgentNames[k]));
            for (int k = 0; k < 4; k++)
            {
                if (k == (int)Agent || (mine != null && mine.Contains(k))) continue;
                for (int i = 0; i < Slots[k].Length; i++)
                {
                    Intent v = null; Guard("price", () => v = Verb((OpId)k, i, t));
                    if (v != null) return $"Nothing here for the {me}. The {AgentNames[k]} can.";
                }
            }
            return $"Nothing here for the {me}.";
        }

        /// <summary>The cell knows who this person is (GDD §04: knowledge only witnessed or told): their need, and their secret if they have one.
        /// Until then the panel shows no "about" and the approaches' generic lines, never the person's own (which name the debt or the secret).</summary>
        bool KnownPerson(Target t)
        {
            if (t.Kind != "person") return true;
            var p = CastOf(t.Key); return p == null || (p.KNeed && (p.Secret.Length == 0 || p.Knows("secret")));
        }

        /// <summary>The engine's refusal, said for the player: who, what to do about it.</summary>
        static string Title(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

        Intent Verb(OpId by, int slot, Target t)
        {
            var g = Game; var R = g.R; string op = Op.Name(by); var plate = PlateOf(by); if (slot < 0 || slot >= plate.Length || plate[slot].Dim) return null; var id = plate[slot].Id;
            Intent I(string verb, string target, string approach, string label) => new Intent { Slot = slot, Label = label, Order = new Order { Op = op, Do = verb, Target = target, Approach = approach } };
            switch (id)
            {
                case "watch" when t.Kind is "camera" or "patrol": // a watcher: the Scout learns its sight (rulebook sight.watch: within reach, on a clear line)
                    {
                        var w = t.Kind == "camera" ? "camera:" + t.Key : "patrol";
                        var i = I("watch", w, "", $"Watch {t.Label}"); i.Time = $"{R.SightWatchS} s"; i.Trail = "T +0"; i.Outcome = "its sight"; return i;
                    }
                case "watch": case "listen":
                    if (t.Kind != "person" || by != OpId.Scout || CastOf(t.Key) is not Person sp) return null; // a person's Watch and every Listen are the Scout's, on a cast person only
                    {
                        var q = g.Quote(R.Ev("scout." + id), sp);
                        var i = I(id, t.Key, "", $"{Slots[0][slot].Label} {t.Label}"); i.Time = $"{q.S} s"; i.Trail = $"T +{q.Trail}"; i.Outcome = id == "watch" ? "routine" : "need, temperament"; return i;
                    }
                case "mislead":
                    if (t.Kind != "rivals") return null;
                    { var i = I("mislead", "rivals", "", "False tip"); i.Time = $"{R.MisleadS} s"; i.Trail = $"T +{R.MisleadTrail}"; return i; }
                case "ask": case "pay": case "help": case "pressure":
                    if (t.Kind != "person" || t.Key == "councillor" || CastOf(t.Key) == null) return null; // a turn is on a cast person only
                    {
                        var q = g.Preview(id, t.Key, OpId.Fixer);
                        var i = I("turn", t.Key, id, $"{Slots[1][slot].Label} {t.Label}");
                        i.Usd = q.WorstUsd > q.Usd ? $"${q.Usd:N0}–{q.WorstUsd:N0}" : $"${q.Usd:N0}";
                        i.Time = $"{q.S} s"; i.Trail = $"T +{q.Trail}"; i.Orders = q.Orders; i.Leverage = q.Leverage; // the walk is the card's own (Walk, WalkS)
                        i.Alert = q.Reports == "no" ? "A 0" : q.Reports == "yes" ? $"A →{q.AlertIfReported}" : $"A ?→{q.AlertIfReported}";
                        i.Outcome = q.Outcome == "?" ? "?" : q.Outcome; i.Seen = SeenWords(q.Seen);
                        return i;
                    }
                case "loop":
                    if (!(t.Kind == "cabinet" || (t.Kind == "camera" && Loopable(t.Key)))) return null;
                    return LoopIntent(slot, t.Kind == "camera" ? t.Key : "entrance");
                case "reroute":
                    if (t.Kind is not ("cabinet" or "patrol")) return null;
                    { var i = I("reroute", "patrol", "", "Reroute the patrol"); i.Time = $"{R.RerouteS} s"; i.Trail = $"T +{R.RerouteTrail}"; return i; }
                case "act":
                    if (t.Kind == "door") { var i = I("enter", "", "", "Enter the Office"); i.Time = $"{R.EnterS} s"; i.Alert = "seen = fail"; var sq = g.SeenAt(g.S.Stand(g.S.GuardPost)); i.Seen = SeenWords(sq); i.SeenQ = sq; return i; } // brief 014: the eye on the line (the engine's card-only SeenAt)
                    if ((t.Kind == "person" && t.Key == "councillor") || (t.Kind == "car" && t.Key == "official") || (t.Kind == "person" && t.Key == "driver" && g.Vale == ValeAt.Pickup)) { var i = I("take", "", "", "Take Vale"); i.Time = $"{R.TakeS} s"; i.Trail = g.Driver.Turned ? "T 0" : $"T +{R.TakeTrailUnturned}"; return i; } // Sky, route B: Vale stands in (or is hidden by) his car at the pickup; the car takes the click too
                    if (t.Kind == "car" && t.Key == "getaway") return g.Ops[(int)by].InCar ? GoIntent(by) : BoardIntent(by); // brief 018: Extract retired into Board and Go
                    return null;
                case "takedown": return TakedownIntent(slot, t, Way);
                case "drop": case "stash": { var l = BodyIntents(by, t, id); return l.Count > 0 ? l[0] : null; }
                case "fire":
                    {
                        string target = t.Kind == "person" ? (t.Key == "councillor" ? "" : t.Key) : t.Kind is "patrol" or "rivals" or "security" ? t.Kind : t.Kind == "hostile" ? t.Key : "";
                        if (target.Length == 0) return null;
                        var i = I("fire", target, "", $"Fire on {t.Label}"); i.Time = "until down"; i.Trail = $"T +{R.HitTrail} a hit, +{R.DownTrail} down"; i.Alert = $"A ≥{R.GunfireAlert}"; i.Outcome = "loud"; return i;
                    }
            }
            return null;
        }

        /// <summary>Fire on a fight target for any armed agent (the Enforcer's knob intent, for an agent with no Fire knob).</summary>
        Intent FireIntent(OpId by, Target t)
        {
            var R = Game.R; string target = t.Kind is "patrol" or "rivals" or "security" ? t.Kind : t.Kind == "hostile" ? t.Key : ""; if (target.Length == 0) return null;
            var i = new Intent { Slot = -1, Label = $"Fire on {t.Label}", Order = new Order { Op = Op.Name(by), Do = "fire", Target = target } };
            i.Time = "until down"; i.Trail = $"T +{R.HitTrail} a hit, +{R.DownTrail} down"; i.Alert = $"A ≥{R.GunfireAlert}"; i.Outcome = "loud"; return i;
        }

        Intent Call(Target t)
        {
            var i = new Intent { Slot = -1, Label = $"Call {t.Label}", Order = new Order { Op = "fixer", Do = "call", Target = t.Key } };
            i.Time = "now"; i.Trail = $"T +{Game.R.CallTrail}"; i.Outcome = Game.CanCall(t.Key) ? "ready" : "not now";
            return i;
        }

        /// <summary>Walk to a target: a place's stand point, or the free ground nearest a person or thing (one A* for the time, on change only).</summary>
        Intent Go(Target t)
        {
            var g = Game; var s = g.S; int first = Array.IndexOf(Selected, true); if (first < 0) return null;
            string place = t.Kind switch { "door" => s.GuardPost, "cabinet" => "cabinet", "car" when t.Key == "getaway" => s.GetawayCar, _ => "" };
            Pt? to = place.Length > 0 ? s.Stand(place) : t.Kind switch
            {
                "person" => CastOf(t.Key) is Person cp ? g.PersonPos(cp) : null, "patrol" => g.PatrolPose()?.pos, "rivals" => g.RivalsPose()?.pos, "security" => g.SecurityPose()?.pos,
                "car" => g.CarPos(), "camera" => CameraAt(t.Key), _ => null,
            };
            if (!to.HasValue) return null;
            if (place.Length == 0) to = s.Grid.NearestFree(to.Value); if (!to.HasValue) return null;
            var route = s.Grid.Route(g.Ops[first].Pos, to.Value);
            var i = new Intent { Move = true, Label = $"Go to {t.Label}", Order = place.Length > 0 ? new Order { Op = Op.Name((OpId)first), Do = "move", To = place } : new Order { Op = Op.Name((OpId)first), Do = "move", Point = to } };
            i.Time = route == null ? "no way" : $"{(new Sim.Path(route).Length + g.R.CellSpeed * Game.Hz - 1) / Math.Max(1, g.R.CellSpeed * Game.Hz)} s walk"; // CellSpeed: mm a tick
            return i;
        }

        Pt? CameraAt(string id) { foreach (var c in Game.S.Cameras) if (c.Id == id) return c.At; return null; }

        /// <summary>The cast person in this role, or null for anything else (a camera, the patrol, the rivals, a unit, a crowd id): Game.PersonByRole
        /// throws on those (Sky 2026-10-19: "engine fault in panel: no person in the role 'patrol'"). Every host read of a role goes through here.</summary>
        public Person CastOf(string role)
        {
            if (Game == null || string.IsNullOrEmpty(role)) return null;
            foreach (var p in Game.People) if (p.Role == role) return p;
            return null;
        }

        /// <summary>Give an order now, stamped with the engine's tick. A refusal is the engine's, with its reason, and is in the log like any order.</summary>
        public bool Issue(Order o) => Issue(o, null, null);

        /// <summary>As Issue, and the answer shows where the player clicked: a refusal's reason at the cursor and on the target for ~2.5 s, a
        /// taken order as a short confirmation and a ring pulse on the target (Sky: "right click doesn't seem to do anything").</summary>
        public bool Issue(Order o, Vector2? screen, Vector3? target, bool quietRefusal = false)
        {
            if (Game == null || Fault != null) return false;
            o.Tick = Game.Tick;
            Answer r = default;
            if (!Guard("order", () => r = Game.Issue(o))) return false;
            r.why ??= ""; r.code ??= "";
            Status = r.ok ? $"{o.Op}: {o.Do} {o.Approach} {o.Target}".Replace("  ", " ") : $"{o.Op} {o.Do} {o.Target}: {r.why}";
            // one fact, one place (Glyph's review #4, Fable B4/B7): an order taken shows on the strip and its card, so it pops nothing; a refusal is one
            // pill at the click (else over its target, else the agent), with a pulse on the target, and never becomes a card, a ticker or a history line
            if (!r.ok && !quietRefusal)
            {
                var full = Humanise(o, r); // brief 014: the pop is the short form with its family glyph; the long line is Alt
                Pop(ShortOf(o, r) ?? full, screen, screen.HasValue ? null : target ?? (o.Op == "cell" ? null : SelectedWorld()), Feedback.Refused, 2f, PopIcon(r.code), full);
                if (screen.HasValue && target.HasValue) Pulses.Add((target.Value, Feedback.Refused, Time.unscaledTime));
            }
            if (!r.ok && o.Do == "call" && r.code.StartsWith("summon-")) FeedSummonRefused(o.Target, o, r); // the contact and their lover say why not
            if (r.ok) { var keep = Status; Drain(); Status = keep; OrderTaken(); }
            Save();
            views.Tick(); views.Frame(1f); intentKey = ""; Track();
            if (Game.Over && Ending.Length == 0) End();
            return r.ok;
        }

        /// <summary>Next Event (§10): run the clock to the next change the engine logs or schedules, at most to the close. One tick at a time.</summary>
        public void NextEvent()
        {
            if (Game == null || Game.Over || Fault != null) return;
            int n0 = Game.Log.Count, until = Game.T(Game.S.Over) + 1, next = NextMark();
            double t0 = Now();
            while (!Game.Over && Game.Log.Count == n0 && Game.Tick < next && Game.Tick < until) if (!StepOnce()) break;
            frameEngineMs += Now() - t0; acc = 0f; views.Frame(1f); if (!Game.Over) Save();
            if (Game.Over && Ending.Length == 0) End();
        }

        /// <summary>Let the clock run to `tick`, one engine step at a time (what time passing does).</summary>
        public void RunTo(int tick)
        {
            if (Game == null || Fault != null) return;
            double t0 = Now();
            while (!Game.Over && Game.Tick < tick) if (!StepOnce()) break;
            frameEngineMs += Now() - t0; acc = 0f; views.Frame(1f); if (!Game.Over) Save();
            if (Game.Over && Ending.Length == 0) End();
        }

        /// <summary>The next scheduled change: a timeline mark, an order landing, an arrival, a guard or rival timer.</summary>
        public int NextMark()
        {
            var g = Game; int t = g.Tick, best = int.MaxValue;
            void C(int x) { if (x > t && x < best) best = x; }
            var s = g.S; foreach (var k in new[] { s.RivalsArrive, s.Chime, s.DriverLast, s.ValeExits, s.ValeAtCar, s.Over }) C(Game.T(k));
            int speed = g.R.CellSpeed;
            foreach (var op in g.Ops) { if (op.Busy != null) C(op.BusyEnd); int sp = op.WalkSpeed > 0 ? op.WalkSpeed : speed; if (op.Walk != null) C(t + (int)((op.Walk.Length - op.WalkMm + sp - 1) / sp)); }
            if (g.LureTick >= 0) { int w = g.VisitWalkTicks, st = Game.T(g.R.SummonStayS); C(g.LureTick + w); C(g.LureTick + w + st); C(g.GuardAwayUntil); } // the summoned lover's walk (the engine's) and stay (the rulebook's)
            C(g.RivalsDelayedUntil); C(g.BlockedFrom); C(g.BlockedUntil); C(g.SecurityArrives); C(g.Lockdown); C(g.PatrolAwayUntil); C(g.MissingNotice);
            return best == int.MaxValue ? t + 1 : best;
        }

        // ---------------------------------------------------------------- the action panel (GDD §10: click a person → action card with the full price)

        public sealed class PanelRow { public OpId By; public int Slot, Key = -1; public Intent I; public string Verb = "", Reason = "", Unlock = ""; public bool Available; public Answer Lock; }
        public sealed class ActionPanel { public Target T; public Vector2 Gui; public Vector3? World; public string Note = "", About = ""; public readonly List<PanelRow> Rows = new(); /** brief 013: the takedown card's "Before you act" rows (icon, text) */ public readonly List<(string icon, string text)> Before = new(); }
        /// <summary>The open panel, else null. Built once on opening (Preview is ~500 KB a call); the clock runs on unless controls.pauseWithPanel.</summary>
        public ActionPanel Panel { get; private set; }
        public int PanelHover { get; private set; } = -1;

        /// <summary>Every action the cell can take on a target, grouped by agent, the selected agent's first: available rows first within each
        /// agent, locked rows dimmed with their reason. Distance is never a lock: an agent out of reach walks there first.</summary>
        public void OpenPanel(Target t, Vector2 screen)
        {
            var pn = new ActionPanel { T = t, Gui = new Vector2(screen.x, (cam ? cam.pixelHeight : Screen.height) - screen.y), World = TargetWorld(t) };
            if (Text != null && KnownPerson(t) && Text.TargetAbout.TryGetValue(TextKey(t), out var about)) pn.About = about; // a person's about only once the Scout has heard them
            if (t.Kind == "crowd") pn.Note = "passer-by — not yet known to the cell";
            else Guard("panel", () =>
            {
                // Sky: "the action menu should only show the actions for the player(s) I've selected"
                var order = new List<int>(); for (int k = 0; k < 4; k++) if (Selected[k]) order.Add(k);
                if (order.Count == 0) { pn.Note = "select an agent: F1–F4 (or click one)"; return; }
                if (IsGetaway(t)) { CarRows(pn, order, t); KeyRows(pn); return; } // brief 018: Board for each selected agent, then Go
                foreach (var k in order)
                {
                    var rows = new List<PanelRow>();
                    if (k == (int)OpId.Fixer && t.Kind == "person" && t.Key != "councillor" && CastOf(t.Key) is { Turned: true })
                    { var c = Call(t); Words(c, t); var cl = CallLock(t.Key); rows.Add(new PanelRow { By = OpId.Fixer, Slot = -1, I = c, Verb = c.Name, Lock = cl, Reason = cl.ok ? "" : cl.why }); }
                    else
                    {
                        var plate = PlateOf((OpId)k); var vs = new List<(int slot, Intent v)>();
                        for (int i = 0; i < plate.Length; i++)
                        {
                            if (plate[i].Dim || plate[i].Id is "drop" or "stash") continue; // the carry knobs are the body rows below
                            if (plate[i].Id == "takedown") { foreach (var how in new[] { "knockout", "kill" }) if (TakedownIntent(i, t, how) is Intent tv) vs.Add((i, tv)); continue; } // both ways, priced (Glyph §2)
                            var v = Verb((OpId)k, i, t); if (v != null) vs.Add((i, v));
                        }
                        foreach (var bv in BodyIntents((OpId)k, t)) vs.Add((-1, bv)); // carry, put down, hide, tie: rows, never knobs (Glyph §1)
                        // Sky 2026-10-18 ("I thought they were unarmed"): every armed agent fires, not only the Enforcer, whose plate has the knob;
                        // in a fight, Fire tops the rows on a hostile
                        if (Armed(k) && t.Kind is "hostile" or "rivals" or "security" or "patrol" && !vs.Exists(x => x.v.Order?.Do == "fire") && FireIntent((OpId)k, t) is Intent fv)
                        { if (Game.FightOn) vs.Insert(0, (-1, fv)); else vs.Add((-1, fv)); }
                        foreach (var (i, v) in vs)
                        {
                            var aw = ActPlace((OpId)k, v.Order, t); // distance never hides an action: it adds the walk to the price
                            if (aw != null) { v.Walk = $"walks {aw.Cm / 100} m ({aw.Seconds} s) to {(aw.Place.Length > 0 ? PlaceName(aw.Place) : "within reach")}, then"; v.WalkS = aw.Seconds; }
                            Words(v, t);
                            var lk = Locked((OpId)k, v, t);
                            rows.Add(new PanelRow { By = (OpId)k, Slot = i, I = v, Verb = v.Name, Lock = lk, Reason = lk.ok ? "" : lk.code is "car-held" or "fight-over" or "too-far" or "run-tethered" or "vale-not-at-car" or "already-heard" or "already-watched" or "needs-routine" ? ShortOf(v.Order, lk) ?? lk.why : lk.why }); // Take too early: when (Sky, route B)
                        }
                    }
                    foreach (var r in rows) { r.Available = r.Reason.Length == 0; if (!r.Available) r.Unlock = LockLine(r.I.Order, r.Lock) ?? ""; }
                    pn.Rows.AddRange(rows.FindAll(r => r.Available)); pn.Rows.AddRange(rows.FindAll(r => !r.Available));
                }
                // Sky, route B ("couldn't get the take action to appear"): Vale or his car with the Enforcer not selected: his Take, greyed, says who does it
                if (!Selected[(int)OpId.Enforcer] && ((t.Kind == "person" && t.Key == "councillor") || (t.Kind == "car" && t.Key == "official")) && Verb(OpId.Enforcer, 0, t) is Intent tk)
                {
                    Words(tk, t); var only = new Answer { ok = false, code = "only-enforcer", why = "Enforcer only", m = -1, cm = -1, tick = -1, usd = -1 };
                    pn.Rows.Add(new PanelRow { By = OpId.Enforcer, Slot = 0, I = tk, Verb = tk.Name, Lock = only, Reason = only.why, Available = false, Unlock = $"Select the Enforcer ({AgentKeyLabel((int)OpId.Enforcer)})." });
                }
                if (pn.Rows.Count == 0) pn.Note = WhoCan(t, order);
                KeyRows(pn); Before(pn);
                int ti = pn.Rows.FindIndex(r => r.I.Order?.Do == "take" && r.Available); if (ti > 0) { var tr = pn.Rows[ti]; pn.Rows.RemoveAt(ti); pn.Rows.Insert(0, tr); } // Take Vale tops the panel (Sky, route B), above a takedown
            });
            Panel = pn; PanelHover = -1; views.ShowPath(null);
        }

        public void ClosePanel() { Panel = null; PanelHover = -1; views?.ShowPath(null); }

        /// <summary>Hovering a row draws that agent's walk to the target (one A* when the hovered row changes).</summary>
        public void SetPanelHover(int row)
        {
            if (row == PanelHover || Panel == null) return;
            PanelHover = row;
            if (row < 0 || row >= Panel.Rows.Count || !Panel.World.HasValue) { views.ShowPath(null); return; }
            var rw = Panel.Rows[row]; var op = Game.Ops[(int)rw.By];
            Pt[] route = null; Guard("path", () => { var aw = ActPlace(rw.By, rw.I.Order, Panel.T); route = aw == null ? null : Game.S.Grid.Route(op.Pos, aw.To); });
            var pts = new List<Vector3>();
            if (route != null) foreach (var p in route) pts.Add(frame.World(p) + Vector3.up * 0.08f);
            else { pts.Add(frame.World(op.Pos) + Vector3.up * 0.08f); pts.Add(Panel.World.Value + Vector3.up * 0.08f); }
            views.ShowPath(pts);
        }

        /// <summary>A row clicked: the order goes to that row's agent, even if another is selected; the panel closes.</summary>
        public void PanelDo(int row)
        {
            if (Panel == null || row < 0 || row >= Panel.Rows.Count) return;
            var r = Panel.Rows[row]; var t = Panel.T; var at = new Vector2(Panel.Gui.x, (cam ? cam.pixelHeight : Screen.height) - Panel.Gui.y);
            if (!r.Available && r.Lock.code == "car-held") HolderPulseAt = Time.unscaledTime; // brief 018 §6.5: a refused Go pulses the holders
            if (!r.Available) { var full = r.Unlock.Length > 0 ? r.Unlock : $"{r.Verb}: {r.Reason}"; Pop(Text?.ShortLock(r.Lock.code) is string sh ? ShortOf(r.I.Order, r.Lock) : full, at, Panel.World, Feedback.Refused, 2f, PopIcon(r.Lock.code ?? ""), full); return; }
            ClosePanel();
            if (r.I.Order?.Do == "go") { GoGive(r.By, at, "left"); return; } // brief 018: the commit hold when anyone stays
            if (SeenGate(r.I, t, at, "left")) return;
            Give(r.By, r.I.Order, t, at);
        }

        /// <summary>1–4 inside the panel: the selected agent's row for that verb slot.</summary>
        void PanelKey(int slot)
        {
            if (Panel == null) return;
            for (int i = 0; i < Panel.Rows.Count; i++) if (Panel.Rows[i].By == Agent && Panel.Rows[i].Key == slot) { PanelDo(i); return; }
            PopKey("nothing-here", null, Panel.World, Feedback.Info, $"Nothing for the {AgentNames[(int)Agent]} there.");
        }

        /// <summary>A row's words: its name ("Pay Inez") and plain line, from the text's actions (Fable), by verb and target.</summary>
        void Words(Intent v, Target t)
        {
            var o = v.Order; var key = o.Do == "turn" ? o.Approach : o.Do;
            if (TakedownWords(v, t)) return;
            // an approach's own line for this person names their debt or secret: only once the cell knows them (§04), else the generic line
            var (label, does) = Text != null ? Text.Action(key, o.Do == "turn" && !KnownPerson(t) ? null : TextKey(t), o.Op) : (key, "");
            bool person = t.Kind == "person" && o.Do != "take";
            v.Name = person ? $"{(label == "Listen" ? "Listen to" : label)} {Name(t.Key)}" : o.Do == "fire" ? $"{label} on {t.Label}" : label;
            v.Does = does;
        }

        /// <summary>A target's key in the text's `targets` and `onTarget` (camera-entrance, car-getaway; a person by role).</summary>
        public string TextKey(Target t) => t.Kind switch
        {
            "camera" => "camera-" + t.Key, "car" => "car-" + t.Key, "hostile" => "hostile", "crowd" => "crowd",
            "stash" => Game.S.Stashes.Find(x => x.Id == t.Key)?.Kind == "door" ? "stash-door" : "stash-bin",
            "body" => Game.PersonById(t.Key) is Person bp && bp.Role.Length > 0 ? bp.Role : "crowd", _ => t.Key,
        };

        /// <summary>Why a row cannot be given now, from the engine's own state (the engine still answers the order; its refusal shows as a pop).</summary>
        Answer Locked(OpId by, Intent v, Target t)
        {
            var g = Game; var op = g.Ops[(int)by]; var o = v.Order;
            Answer No(string code, string why, int tick = -1, long usd = -1) => new Answer { ok = false, code = code, why = why, m = -1, cm = -1, usd = usd, tick = tick };
            var yes = new Answer { ok = true, code = "", why = "" };
            if (op.Inside) return No("enforcer-inside", "inside the Office");
            if (op.Busy != null) return No("agent-busy", $"busy until {MissionClock(op.BusyEnd)} ({doing[(int)by].Label}); {controls.Label(PlayAction.Cancel)} cancels it", op.BusyEnd);
            if (o.Do is "listen" or "turn" && ServingBackS(o.Target) > 0) return No("too-far", $"serving a table; back in {ServingBackS(o.Target)} s"); // brief 022 (N130): out at a table she is out of reach; the row says when she is back
            if (o.Do is "watch" or "listen") { var sq = g.LearnQuery(Op.Name(by), o.Do, o.Target); /* any agent (the Tech watches machines): only-scout, run-tethered, already-watched */ if (!sq.ok && sq.code is not ("too-far" or "agent-busy" or "mission-over")) return sq; } // Keel's refusals (a second Watch or Listen teaches nothing): the engine's, not the host's
            switch (o.Do)
            {
                case "watch" when Game.WatcherKind(o.Target) is "camera" or "patrol":
                    return g.ConeFan(o.Target) != null ? yes : No("watcher-away", "not out on the street");
                case "watch": case "listen": return CastOf(o.Target) is not Person wp ? No("target-unknown", "no one to watch or listen to") : g.PersonPos(wp).HasValue ? yes : No("person-away", "not out on the street"); // a cast person only: PersonByRole throws on anything else
                case "turn":
                    {
                        var p = CastOf(o.Target); if (p == null) return No("target-unknown", "no one to turn");
                        if (!g.PersonPos(p).HasValue) return No("person-away", "not out on the street");
                        var need = g.R.ApproachRequires(o.Approach);
                        if (need != "nothing" && !p.Knows(need)) return No("needs-" + need, $"needs their {need} — {(need == "routine" ? "Watch" : "Listen")} (Scout) first");
                        if (p.KnownRefused.Contains(o.Approach)) return No("refusal-remembered", $"{Cap(Name(o.Target))} refused this already");
                        var q = g.Preview(o.Approach, o.Target, OpId.Fixer); if (q.WorstUsd > g.Cash) return No("no-cash", $"not enough cash (${q.WorstUsd:N0})", -1, q.WorstUsd);
                        return yes;
                    }
                case "loop": return g.LoopOn ? No("loop-holds", "the loop already holds") : yes;
                case "board": return BoardLock(by);
                case "go": return GoLock();
                case "takedown": return v.Td != null && v.Td.Refused.Length > 0 ? No(v.Td.RefusedCode, v.Td.Refused) : op.CarryBody.Length > 0 || op.Carrying.HasValue ? No("already-carrying", "already carrying") : yes;
                case "stash": return op.CarryBody.Length == 0 ? No("not-carrying", "carry the body to the stash first") : StashFree(o.Target) <= 0 ? No("stash-full", "full") : yes;
                case "drop": return op.CarryBody.Length == 0 ? No("not-carrying", "carrying nobody") : yes;
                case "restrain": case "carry" when !IsOpName(o.Target): return op.CarryBody.Length > 0 || op.Carrying.HasValue ? No("already-carrying", "already carrying") : yes;
                case "reroute": return g.PatrolAwayUntil > g.Tick ? No("patrol-away", "the patrol is already away", g.PatrolAwayUntil) : yes;
                case "mislead": return g.RivalsPresent ? yes : No("no-rivals", "no rivals here yet");
                case "enter": return g.Vale == ValeAt.Office ? yes : No("vale-not-inside", "Vale is not inside");
                case "take": return g.Vale == ValeAt.Pickup ? yes : No("vale-not-at-car", "Vale is not at his car yet");
                case "fire" when by == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer: return No("fire-carrying-vale", "walking Vale, the Enforcer doesn't fire: the others cover him");
                case "fire" when g.FightOverTick <= g.Tick && g.Hostiles.Exists(x => x.Id == o.Target): return No("fight-over", "the fight is over: nobody is left to fire on"); // engine 0.8.2 (rulebook fight.over): the row says so before the click
                case "fire": return op.Gun.W == null ? No("no-weapon", "has no weapon") : op.Running ? No("fire-running", "can't fire while running")
                        : o.Target is "waitress" or "driver" ? No("civilian-target", $"{o.Target} is a civilian or a contact: a target only when forced (Ctrl)") : yes;
            }
            return yes;
        }

        /// <summary>Why the Fixer can't call a turned contact now, by the engine's codes: Inez only while the guard is on the door; the driver
        /// not while his car is still moving, and not after his last chance.</summary>
        Answer CallLock(string role)
        {
            // the engine's own answer before the click (Game.CallQuery: the same Answer Issue would give, read-only), so the card is locked with
            // the right reason (Fable's lock line by code); a race between hover and click still gets the refusal card on the feed
            Answer a = new Answer { ok = true, code = "", why = "" };
            Guard("call query", () => a = Game.CallQuery(role));
            a.why ??= ""; a.code ??= "";
            return a;
        }

        /// <summary>Give an order for an agent; if the engine says they stand too far, they walk to the target first and the order follows on arrival
        /// (both are engine orders at their own ticks, so the session replays as played).</summary>
        /// <summary>Where an act happens (Sky: "they need to be moved next to it when the action is done"): the Tech loops and reroutes standing at
        /// the control cabinet, the Enforcer enters at the public door and puts Vale in at the getaway car (those are the mission's places, so
        /// the walk is a place-based engine order); a Watch, Listen, Turn, Take, False tip or shot happens within that verb's reach, so the agent
        /// walks the route until they are inside it (half a metre to spare), not onto the person. Null when the agent is already there.</summary>
        public sealed class ActWalk { public string Place = ""; public Pt To; public int Cm, Seconds; }

        public ActWalk ActPlace(OpId by, Order o, Target t)
        {
            var g = Game; var s = g.S; var R = g.R; var op = g.Ops[(int)by];
            string place = ""; int reach = 0; Pt? tp = null;
            switch (o.Do)
            {
                case "loop": case "reroute": place = "cabinet"; break;
                case "enter": place = s.GuardPost; break;
                case "board": place = s.GetawayCar; break; // brief 018: board within at-reach of the car
                case "watch" when t.Kind is "camera" or "patrol": tp = PosOf(t); reach = R.SightReachCm; break; // within the watch reach of the watcher (a clear line is the engine's check)
                case "watch": case "listen": tp = PosOf(t); reach = ReachOf("scout." + o.Do); break;
                case "turn": tp = PosOf(t); reach = R.ApproachReachCm; break;
                case "take": tp = g.ValePos(); reach = R.ActReachCm; break;
                case "mislead": tp = g.RivalsPose()?.pos; reach = R.ActReachCm; break;
                case "fire": tp = PosOf(t); if (op.Gun.W == null) return null; reach = op.Gun.W.EffCm; break; // walk into the weapon's effective band
                case "carry" when IsOpName(o.Target): tp = g.Ops[(int)Op.Parse(o.Target)].Pos; reach = R.ActReachCm; break;
                case "carry": case "restrain": { var b = g.BodyById(o.Target); if (b == null) return null; tp = g.BodyPos(b); reach = R.ActReachCm; break; }
                case "takedown": { var p = g.PersonById(o.Target); tp = p == null ? null : g.PosOfPerson(p); reach = R.TakedownReachCm; break; }
                case "stash": { var st = g.S.Stashes.Find(x => x.Id == o.Target); if (st == null) return null; tp = st.Stand; reach = R.AtReachCm; break; }
                default: return null; // a call is made from wherever the Fixer stands
            }
            if (place.Length > 0)
            {
                var st = s.Stand(place); if (G.Dist(op.Pos, st) <= 100) return null;
                var r = s.Grid.Route(op.Pos, st); return r == null ? null : Walk(place, st, r);
            }
            if (!tp.HasValue) return null;
            // watching a camera or the patrol needs a clear line as well as reach (the engine's own check, Site.Sight at the rulebook's eye
            // height): the stand point is the first route point that has both, never one the engine then refuses with "no clear line"
            bool line = o.Do == "watch" && t.Kind is "camera" or "patrol"; var aim = tp.Value;
            bool Fits(Pt q) => G.Dist(q, aim) <= reach - 50 && s.Grid.Free(q) && (!line || s.Sight(q, aim, R.Eye));
            if (G.Dist(op.Pos, aim) <= reach - 50 && (!line || s.Sight(op.Pos, aim, R.Eye))) return null;
            var goal = s.Grid.NearestFree(aim); if (!goal.HasValue) return null;
            var route = s.Grid.Route(op.Pos, goal.Value); if (route == null) return null;
            var cut = new List<Pt> { route[0] };
            for (int i = 1; i < route.Length; i++) // corners can be far apart: 25 cm steps, stop at the first free point within reach (and line)
            {
                Pt a = route[i - 1], c = route[i]; int n = Math.Max(1, G.Dist(a, c) / 25);
                for (int k = 1; k <= n; k++) { var q = G.Lerp(a, c, k, n); if (Fits(q)) { cut.Add(q); return Walk("", q, cut.ToArray()); } }
                cut.Add(c);
            }
            if (line && !s.Sight(goal.Value, aim, R.Eye)) // the route never sees it: the nearest free point in reach that does, by rings of 1 m
                for (int rr = 100; rr < reach - 50; rr += 100)
                    for (int d = 0; d < 360; d += 15)
                    {
                        var q = new Pt(aim.X + (int)(rr * Math.Cos(d * Math.PI / 180)), aim.Y + (int)(rr * Math.Sin(d * Math.PI / 180)));
                        if (Fits(q) && s.Grid.Route(op.Pos, q) is Pt[] rq) return Walk("", q, rq);
                    }
            return Walk("", goal.Value, route);
        }

        ActWalk Walk(string place, Pt to, Pt[] route)
        {
            long mm = new Sim.Path(route).Length; int speed = Math.Max(1, Game.R.CellSpeed);
            return new ActWalk { Place = place, To = to, Cm = (int)(mm / 10), Seconds = (int)((mm + speed * Game.Hz - 1) / (speed * Game.Hz)) };
        }

        Pt? PosOf(Target t) => t.Kind switch
        {
            "person" => CastOf(t.Key) is Person pp ? Game.PersonPos(pp) : null, "patrol" => Game.PatrolPose()?.pos, "rivals" => Game.RivalsPose()?.pos,
            "security" => Game.SecurityPose()?.pos, "hostile" => Game.PosOf(t.Key), "op" => Game.Ops[(int)Op.Parse(t.Key)].Pos, "camera" => CameraAt(t.Key),
            "crowd" => Game.PersonById(t.Id) is Person cp ? Game.PosOfPerson(cp) : null, "body" => Game.BodyById(t.Key) is Body b ? Game.BodyPos(b) : null,
            "stash" => Game.S.Stashes.Find(x => x.Id == t.Key)?.Stand, _ => null,
        };

        int ReachOf(string ev)
        {
            foreach (var c in Game.R.Ev(ev).Needs) if (c.K == "reach") return (int)Game.R.Num(c.S("m")) * 100;
            return Game.R.ActReachCm;
        }

        /// <summary>Give an order for an agent: if the act has a place they are not at, they walk there first (a place-based move for the
        /// mission's places, else the point within reach) and the order follows on arrival. Every step is an engine order at its own tick.</summary>
        void Give(OpId by, Order o, Target t, Vector2? screen)
        {
            var order = new Order { Op = Op.Name(by), Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How };
            pending[(int)by] = null;
            ActWalk w = null; Guard("walk", () => w = ActPlace(by, order, t));
            var tw = TargetWorld(t);
            if (w != null && Driving && by == (OpId)dAgent) { PopKey("too-far", screen, tw, Feedback.Refused, "Walk closer first: a driven agent does not walk to reach."); return; } // brief 025 (Keel): no auto-walk while driven
            if (w == null) { if (Issue(order, screen, tw)) SayOrder(by, order, null); return; }
            var walk = w.Place.Length > 0 ? new Order { Op = order.Op, Do = "move", To = w.Place } : new Order { Op = order.Op, Do = "move", Point = w.To };
            if (Issue(walk, screen, null))
            {
                pending[(int)by] = new Order { Op = order.Op, Do = order.Do, Target = order.Target, Approach = order.Approach, How = order.How };
                pendingAt[(int)by] = tw;
                pendingFrom[(int)by] = Game.Tick;
                SayOrder(by, order, w); Track();
            }
        }

        string PlaceName(string place) => place == "cabinet" ? "the control cabinet" : place == Game.S.GuardPost ? "the public door" : place == Game.S.GetawayCar ? "the getaway car" : place;

        /// <summary>The act an agent walks to give (verb, approach), or empty (brief 014: its glyph at the walk's end).</summary>
        public (string verb, string approach) PendingOf(int k) => pending[k] == null ? ("", "") : (pending[k].Do, pending[k].Approach ?? "");
        readonly Order[] pending = new Order[4]; readonly Vector3?[] pendingAt = new Vector3?[4]; readonly int[] pendingFrom = new int[4];

        /// <summary>After each step: an agent who has arrived (no walk, nothing in hand) gives the order they walked for.</summary>
        void FollowUp()
        {
            for (int k = 0; k < 4; k++)
            {
                var o = pending[k]; if (o == null) continue;
                var op = Game.Ops[k]; if (op.Walk != null || op.Busy != null || op.Inside) continue;
                pending[k] = null;
                var act = new Order { Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, How = o.How };
                if (Issue(act, null, pendingAt[k]) && Game.Tick - pendingFrom[k] >= 5 * Game.Hz) SayArrive((OpId)k, act); // a short walk: the ack said it (Fable §4)
            }
        }

        // ---------------------------------------------------------------- feedback: every click answers where it was made

        public enum Feedback { Info, Ok, Refused }
        public sealed class PopUp { public string Text = "", Icon = "", Long = ""; public Vector2? Screen; public Vector3? World; public Feedback Kind; public float Until, Born; }
        /// <summary>Pops the HUD draws (at the cursor, or over a world point), newest last; and ring pulses on targets.</summary>
        public readonly List<PopUp> Pops = new();
        public readonly List<(Vector3 world, Feedback kind, float born)> Pulses = new();

        public void Pop(string text, Vector2? screen, Vector3? world, Feedback kind, float seconds = 2.5f, string icon = "", string longLine = "")
        {
            float now = Time.unscaledTime;
            var gui = screen.HasValue ? new Vector2(screen.Value.x, (cam ? cam.pixelHeight : Screen.height) - screen.Value.y) : (Vector2?)null;
            Pops.Add(new PopUp { Text = text, Icon = icon, Long = longLine.Length > 0 ? longLine : text, Screen = gui, World = world, Kind = kind, Born = now, Until = now + seconds });
            if (world.HasValue) Pulses.Add((world.Value, kind, now));
            while (Pops.Count > 6) Pops.RemoveAt(0);
            while (Pulses.Count > 6) Pulses.RemoveAt(0);
        }

        public Vector3? TargetWorld(Target t)
        {
            var key = t.Kind switch { "person" => t.Key, "door" => "marker/door", "cabinet" => "marker/cabinet", "camera" => "camera/" + t.Key, "car" => "car/" + t.Key, "patrol" => "patrol", "rivals" => "rivals/0", "security" => "security/0", "hostile" => ViewKey(t.Key), "op" => "op/" + t.Key, "crowd" => t.Key, "body" => "body/" + t.Key, "stash" => "stash/" + t.Key, "unit" => "unit/" + t.Key, _ => null };
            var tr = key != null ? views.Find(key) : null; return tr ? tr.position : (Vector3?)null;
        }

        // ---------------------------------------------------------------- the session, the end

        string SessionFile()
        {
            var dir = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "out", "play")) : Path.Combine(Application.persistentDataPath, "play");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"handover-{DateTime.Now:yyyyMMdd-HHmmss}.script.json");
        }

        /// <summary>The session as a replayable script with its run record: start state, every order (taken or refused), checkpoints, until this
        /// tick. Written on every order, every 5 s of mission time while the clock runs, on pause, restart, quit and the end, so `until` is the
        /// session's last tick and a replay plays everything after the last order too (Keel: a fight after the last order was left out).</summary>
        void Save()
        {
            if (Game == null || SessionPath.Length == 0) return;
            savedTick = Game.Tick;
            try
            {
                // a session still running has no closing checkpoint; the replay takes one at `until` (Engine.Play calls Finish), so the record
                // carries it too, written beside the live list and taken out again (the live game's checkpoints are untouched)
                var cps = Game.Checkpoints; bool add = !Game.Over && (cps.Count == 0 || cps[cps.Count - 1].tick != Game.Tick);
                if (add) cps.Add((Game.Tick, Hash.State(Game)));
                try { File.WriteAllText(SessionPath, Script.Write(Game, Bundle.Inputs.Hash) + "\n"); }
                finally { if (add) cps.RemoveAt(cps.Count - 1); }
            }
            catch (Exception e) { Debug.LogWarning($"Play: session not saved: {e.Message}"); }
        }

        void End()
        {
            Guard("finish", () => Game.Finish()); // the closing checkpoint, as Engine.Play takes it (an order can end the run between steps)
            Save();
            bool same = false;
            Guard("replay", () =>
            {
                var r = Engine.Replay(Bundle.Inputs, Game);
                same = r.Checkpoints.Count == Game.Checkpoints.Count;
                for (int i = 0; same && i < r.Checkpoints.Count; i++) same = r.Checkpoints[i] == Game.Checkpoints[i];
            });
            try { EndDebrief = Debrief.Of(Game); } catch { EndDebrief = null; }
            var final = Hash.Hex(Game.Checkpoints[Game.Checkpoints.Count - 1].hash);
            Ending = $"{Game.Result}{(Game.FailCode.Length > 0 ? $" ({Game.FailCode})" : "")} at {Report.Clock(Game.Tick)} · final {final[..12]} · replay {(same ? "identical" : "DIFFERS")}";
            try { File.WriteAllText(Path.ChangeExtension(SessionPath, null) + ".end.txt", $"{Ending}\n{Game.FailText}\nengine {Bundle.Source} dll {Bundle.DllSha}\nstep mean {StepMsMean:F4} ms, max {StepMsMax:F3} ms over {steps} steps; engine per frame {FrameMsMean:F4} ms over {frames} frames\n"); } catch { }
            Debug.Log($"Play: {Ending}; session {SessionPath}"); ClearHover(); if (Panel != null) ClosePanel();
            FailCamAfterEnd(); // a failure: the camera orbits what failed it first (HostFailCam)
        }

        int savedTick = -1;

        void OnDestroy() { if (Game != null) Save(); if (background) background.SetActive(true); foreach (var c in covered) if (c) c.enabled = true; }

        /// <summary>Every call into the engine passes here: an exception stops the host (the session is kept), never the Editor.</summary>
        bool Guard(string what, Action a)
        {
            try { a(); return true; }
            catch (Exception e)
            {
                Fault = e; Status = $"engine fault in {what}: {e.GetType().Name}: {e.Message}";
                Debug.LogError($"Play: the engine threw in {what}; the host stopped (the Editor runs on). {e}");
                Save();
                return false;
            }
        }

        public string CityClock()
        {
            if (Game == null) return "";
            var p = MissionStartsAt.Split(':'); int s0 = int.Parse(p[0]) * 3600 + int.Parse(p[1]) * 60 + (p.Length > 2 ? int.Parse(p[2]) : 0);
            int s = s0 + Game.Tick / Game.Hz;
            return $"{s / 3600 % 24:00}:{s / 60 % 60:00}:{s % 60:00}";
        }

        public static string MissionClock(int tick) { int s = tick / Game.Hz; return $"{s / 60}:{s % 60:00}"; }

        public Color JobColour(string job) { int i = Array.IndexOf(jobs, job); if (i < 0) i = Array.IndexOf(jobs, "resident"); return i >= 0 && i < jobColours.Length ? jobColours[i] : securityColour; }
    }
}
