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
    /// Controls: left click an agent or 1–4 to select (Shift adds to the group) · right click the ground to move (roads cross anywhere: the engine's
    /// walk grid leaves carriageways open) · right click a person or object for the selected agent's verb on it · hover first: the price chip at
    /// the cursor shows $, time, Trail and Alert before the click commits (§02) · Q W E R pick another verb (the hotbar lights the one a right
    /// click gives) · Space pauses (on at the start) · = and - change speed · N jumps to the next event · arrows or the middle mouse pan · the
    /// wheel zooms 12.5–18.5, where a 1.75 m person stays 40–60 px at 1080p (§10) · F1 the debug layer · Return restarts after the end card.
    /// Engine exceptions stop the host at this boundary, never the Editor (Redline E1). Preview (one A* and four temperament evaluations) is
    /// called only when the hovered target, the agent or the verb changes.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class HandoverHost : MonoBehaviour
    {
        [Header("Baked by PlayInstall (Editor) from the plan, the export and the civic look")]
        public SiteFrame frame = new();
        public Material litTemplate, unlitTemplate;
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
        public Camera View => cam;

        // ---------------------------------------------------------------- verbs: four slots an agent, Q W E R

        public sealed class Slot { public string Id, Label, Glyph; public Slot(string id, string label, string glyph) { Id = id; Label = label; Glyph = glyph; } }
        /// <summary>The agents' verbs (§03): Scout watches, listens, misleads; Fixer turns (and calls a turned contact: right click it); Tech loops and
        /// reroutes; Enforcer acts (enter at the door, take Vale at his car, put him in the getaway car) and fires.</summary>
        public static readonly Slot[][] Slots =
        {
            new[] { new Slot("watch", "Watch", "eye"), new Slot("listen", "Listen", "ear"), new Slot("mislead", "False tip", "tip") },
            new[] { new Slot("ask", "Ask", "ask"), new Slot("pay", "Pay", "$"), new Slot("help", "Help", "help"), new Slot("pressure", "Pressure", "press") },
            new[] { new Slot("loop", "Loop camera", "loop"), new Slot("reroute", "Reroute patrol", "route") },
            new[] { new Slot("act", "Enter / Take / Car", "act"), new Slot("fire", "Fire", "fire") },
        };
        public static readonly string[] Keys = { "Q", "W", "E", "R" };

        public sealed class Target { public string Kind = "", Key = "", Label = ""; } // person (role) · door · cabinet · camera (id) · car · patrol · rivals · security · hostile
        /// <summary>What a right click on the hovered thing would do now, and its price (the chip).</summary>
        public sealed class Intent
        {
            public Order Order; public int Slot = -1; public string Label = "", Usd = "$0", Time = "", Trail = "T 0", Alert = "A 0", Outcome = ""; public bool Move;
        }

        public OpId Agent { get; private set; } = OpId.Scout;   // the agent the hotbar is for (the first selected)
        public int Picked { get; private set; } = -1;           // Q/W/E/R pick, -1 = the default for the target
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
                focus = cam.transform.position + cam.transform.forward * (shadows ? shadows.focusDepth : 80f);
                focus.y = 0f; PlaceCamera();
            }
            background = GameObject.Find("people");
            if (background) background.SetActive(false); // the engine's crowd steps the city's people; the editor snapshot would double them
            Begin();
        }

        /// <summary>A new run from the start state (Start, and Return after the end card).</summary>
        public void Begin()
        {
            Fault = null; Ending = ""; EndDebrief = null; lastLog = 0; acc = 0f; steps = 0; stepMs = 0; StepMsMax = 0; frames = 0; frameEngineMs = 0;
            Paused = startPaused; Speed = 1; Picked = -1; intentKey = ""; HoverIntent = null; Hover = null;
            if (viewRoot) Destroy(viewRoot.gameObject);
            Guard("load", () =>
            {
                Bundle ??= SimBundle.Load();
                Game = Engine.New(Bundle.Inputs, new Start());
                if (Game.S.Root.Opt("cast")?.Opt("startsAt") is JNode st && st.Kind == JKind.Str) MissionStartsAt = st.Str();
            });
            if (Fault != null) return;
            viewRoot = new GameObject("engine").transform; viewRoot.SetParent(transform, false);
            views = new HostViews(this, viewRoot, frame);
            views.Tick(); views.Tick(); views.Frame(1f);
            Select(OpId.Scout, false);
            // open on the cell at the getaway bay with Inez's café door in frame
            var inez = Game.PersonPos(Game.Inez); if (inez.HasValue) LookAt(Vector3.Lerp(frame.World(Game.Ops[0].Pos), frame.World(inez.Value), 0.45f));
            SessionPath = SessionFile();
            Save();
            Status = "Paused (Space). Select an agent (1–4 or left click), hover a target to see the price, right click to act.";
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
            if (kb != null && kb.f1Key.wasPressedThisFrame) ShowDebug = !ShowDebug;
            if (Fault != null) return;
            if (Ending.Length > 0) { if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) Begin(); views?.Frame(1f); return; }
            Controls();
            if (Fault != null) return;
            if (!Paused && !Game.Over)
            {
                acc += Time.unscaledDeltaTime * Speed;
                int n = 0; double t0 = Now();
                while (acc >= 0.1f && n < 80 && !Game.Over) { if (!StepOnce()) return; acc -= 0.1f; n++; }
                if (n == 80) acc = 0f;
                frameEngineMs += Now() - t0;
            }
            frames++;
            if (Game.Over && Ending.Length == 0) End();
            views.Frame(Paused || Game.Over ? 1f : Mathf.Clamp01(acc / 0.1f));
        }

        bool StepOnce()
        {
            double t0 = Now();
            bool ok = Guard("step", () => Game.Step());
            double ms = Now() - t0; stepMs += ms; steps++; if (ms > StepMsMax) StepMsMax = ms;
            if (ok) views.Tick();
            if (Game.Log.Count > lastLog) { var l = Game.Log[Game.Log.Count - 1]; Status = $"{Report.Clock(l.Tick)} {l.Text}"; lastLog = Game.Log.Count; }
            return ok;
        }

        double Now() { if (!sw.IsRunning) sw.Start(); return sw.Elapsed.TotalMilliseconds; }

        // ---------------------------------------------------------------- controls

        void Controls()
        {
            var kb = Keyboard.current; var mouse = UnityEngine.InputSystem.Mouse.current;
            bool shift = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) { Paused = !Paused; Status = Paused ? "Paused." : "Running."; }
                if (kb.digit1Key.wasPressedThisFrame) Select(OpId.Scout, shift);
                if (kb.digit2Key.wasPressedThisFrame) Select(OpId.Fixer, shift);
                if (kb.digit3Key.wasPressedThisFrame) Select(OpId.Tech, shift);
                if (kb.digit4Key.wasPressedThisFrame) Select(OpId.Enforcer, shift);
                if (kb.qKey.wasPressedThisFrame) Pick(0);
                if (kb.wKey.wasPressedThisFrame) Pick(1);
                if (kb.eKey.wasPressedThisFrame) Pick(2);
                if (kb.rKey.wasPressedThisFrame) Pick(3);
                if (kb.nKey.wasPressedThisFrame) NextEvent();
                if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) Speed = Mathf.Min(8, Speed * 2);
                if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) Speed = Mathf.Max(1, Speed / 2);
                var pan = Vector2.zero;
                if (kb.upArrowKey.isPressed) pan.y += 1; if (kb.downArrowKey.isPressed) pan.y -= 1;
                if (kb.rightArrowKey.isPressed) pan.x += 1; if (kb.leftArrowKey.isPressed) pan.x -= 1;
                if (pan != Vector2.zero && cam) Pan(pan * panSpeed * Time.unscaledDeltaTime);
            }
            if (mouse == null || !cam) return;
            if (mouse.middleButton.isPressed) { var d = mouse.delta.ReadValue(); Pan(-d * (2f * cam.orthographicSize / Screen.height)); }
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel != 0f) cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - Mathf.Sign(wheel) * 0.5f, zoomMin, zoomMax);
            var p = mouse.position.ReadValue();
            if (p != lastMouse) { lastMouse = p; PointerHover(p); }      // the price is redone only on change (Preview is ~500 KB a call)
            else if (intentKey.Length == 0) PointerHover(lastHover);    // the agent, the verb or the world changed under a still mouse
            if (PlayHud.Blocks(p) || DebugHud.Blocks(p)) return;
            if (mouse.leftButton.wasPressedThisFrame) PointerLeft(p, shift);
            if (mouse.rightButton.wasPressedThisFrame) PointerRight(p);
        }

        void Pan(Vector2 screen)
        {
            var right = cam.transform.right; right.y = 0; right.Normalize();
            var up = cam.transform.forward; up.y = 0; up.Normalize();
            focus += right * screen.x + up * screen.y / Mathf.Sin(cam.transform.eulerAngles.x * Mathf.Deg2Rad);
            PlaceCamera();
        }

        void PlaceCamera()
        {
            cam.transform.position = focus - cam.transform.forward * standOff;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, standOff + 1500f);
            if (shadows) shadows.focusDepth = standOff;
            if (fade) fade.focus = focus;
        }

        /// <summary>Pan so the camera centres on a world point (zoom, pitch and yaw stay).</summary>
        public void LookAt(Vector3 world) { if (!cam) return; focus = new Vector3(world.x, 0f, world.z); PlaceCamera(); }

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
            if (slot >= Slots[(int)Agent].Length) { Status = $"{Op.Name(Agent)} has no verb on {Keys[slot]}"; return; }
            Picked = slot; intentKey = "";
            Status = $"{Keys[slot]}: {Slots[(int)Agent][slot].Label} — right click a target";
        }

        /// <summary>The mouse over the world (pixels, origin bottom left): what is under it, and what a right click would do there, priced once per change.</summary>
        public void PointerHover(Vector2 screen)
        {
            lastHover = screen; Mouse = new Vector2(screen.x, (cam ? cam.pixelHeight : Screen.height) - screen.y); // GUI space: origin top left
            var t = TargetAt(screen);
            string key = $"{t?.Kind}:{t?.Key}:{Agent}:{Picked}:{Game.Orders.Count}";
            if (key == intentKey) return;
            intentKey = key; Hover = t;
            HoverIntent = t == null ? null : IntentFor(t);
        }

        public void PointerLeft(Vector2 screen, bool shift = false)
        {
            var t = TargetAt(screen, true);
            if (t != null && t.Kind == "op") Select(Op.Parse(t.Key), shift);
        }

        /// <summary>Right click: on the ground, the selection moves (a group stands in a 0.9 m square); on a target, the hovered intent is given.</summary>
        public void PointerRight(Vector2 screen)
        {
            if (Game == null || Fault != null || !cam) return;
            var t = TargetAt(screen);
            if (t != null && t.Kind != "op")
            {
                PointerHover(screen);
                var i = HoverIntent;
                if (i?.Order == null) { Status = $"{Op.Name(Agent)}: nothing to do on {t.Label}"; return; }
                Issue(i.Order);
                return;
            }
            if (!frame.Ground(cam.ScreenPointToRay(screen), out var w)) return;
            var at = frame.Engine(w); int k = 0;
            for (int n = 0; n < 4; n++)
            {
                if (!Selected[n]) continue;
                Issue(new Order { Op = Op.Name((OpId)n), Do = "move", Point = new Pt(at.X + (k % 2) * 90, at.Y + (k / 2) * 90) }); k++;
            }
        }

        /// <summary>The thing under a screen point. Left clicks look for agents first; hover and right clicks for the city's people, then things
        /// (an agent standing in front of Inez must not swallow the order meant for her).</summary>
        Target TargetAt(Vector2 screen, bool agents = false)
        {
            if (!cam || viewRoot == null) return null;
            // people before things: the door, cabinet and car markers are generous boxes, and the guard stands in the door's
            Target best = null; int rank = 9; float dist = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(cam.ScreenPointToRay(screen), 5000f))
            {
                if (!hit.collider.transform.IsChildOf(viewRoot)) continue;
                var t = TargetOf(hit.collider.transform); if (t == null) continue;
                int r = t.Kind == "op" ? (agents ? 0 : 2) : t.Kind is "person" or "hostile" or "patrol" or "rivals" or "security" ? (agents ? 1 : 0) : (agents ? 2 : 1);
                if (r < rank || (r == rank && hit.distance < dist)) { best = t; rank = r; dist = hit.distance; }
            }
            return best;
        }

        Target TargetOf(Transform t)
        {
            for (var x = t; x && x != viewRoot; x = x.parent)
            {
                var n = x.name;
                if (n.StartsWith("op/")) return new Target { Kind = "op", Key = n[3..], Label = n[3..] };
                if (n is "waitress" or "driver" or "guard" or "councillor") return new Target { Kind = "person", Key = n, Label = n == "councillor" ? "Vale" : n == "waitress" ? "Inez" : $"the {n}" };
                if (n == "patrol") return new Target { Kind = "patrol", Key = "patrol", Label = "the patrol" };
                if (n.StartsWith("rivals/")) return new Target { Kind = "rivals", Key = "rivals", Label = "the rival crew" };
                if (n.StartsWith("security/")) return new Target { Kind = "security", Key = "security", Label = "the response unit" };
                if (n.StartsWith("hostile/")) return new Target { Kind = "hostile", Key = "hostiles", Label = "a hostile" };
                if (n == "car/official") return new Target { Kind = "car", Key = "official", Label = "the official car" };
                if (n == "car/getaway") return new Target { Kind = "car", Key = "getaway", Label = "the getaway car" };
                if (n == "marker/door") return new Target { Kind = "door", Key = "door", Label = "the public door" };
                if (n == "marker/cabinet") return new Target { Kind = "cabinet", Key = "cabinet", Label = "the control cabinet" };
                if (n.StartsWith("camera/")) return new Target { Kind = "camera", Key = n[7..], Label = $"the {n[7..]} camera" };
            }
            return null;
        }

        /// <summary>The agent's verb on a target: the picked slot when it applies, else the first slot that does, else walking there.</summary>
        Intent IntentFor(Target t)
        {
            if (t.Kind == "op") return null;
            Intent got = null;
            Guard("price", () =>
            {
                var slots = Slots[(int)Agent];
                if (Agent == OpId.Fixer && t.Kind == "person" && t.Key != "councillor" && Game.PersonByRole(t.Key).Turned) { got = Call(t); return; }
                if (Picked >= 0) got = Verb(Agent, Picked, t);
                for (int i = 0; got == null && i < slots.Length; i++) got = Verb(Agent, i, t);
                got ??= Go(t);
            });
            return got;
        }

        Intent Verb(OpId by, int slot, Target t)
        {
            var g = Game; var R = g.R; string op = Op.Name(by); var id = Slots[(int)by][slot].Id;
            Intent I(string verb, string target, string approach, string label) => new Intent { Slot = slot, Label = label, Order = new Order { Op = op, Do = verb, Target = target, Approach = approach } };
            switch (id)
            {
                case "watch": case "listen":
                    if (t.Kind != "person") return null;
                    {
                        var q = g.Quote(R.Ev("scout." + id), g.PersonByRole(t.Key));
                        var i = I(id, t.Key, "", $"{Slots[0][slot].Label} {t.Label}"); i.Time = $"{q.S} s"; i.Trail = $"T +{q.Trail}"; i.Outcome = id == "watch" ? "routine" : "need, temperament"; return i;
                    }
                case "mislead":
                    if (t.Kind != "rivals") return null;
                    { var i = I("mislead", "rivals", "", "False tip"); i.Time = $"{R.MisleadS} s"; i.Trail = $"T +{R.MisleadTrail}"; return i; }
                case "ask": case "pay": case "help": case "pressure":
                    if (t.Kind != "person" || t.Key == "councillor") return null;
                    {
                        var q = g.Preview(id, t.Key, OpId.Fixer);
                        var i = I("turn", t.Key, id, $"{Slots[1][slot].Label} {t.Label}");
                        i.Usd = q.WorstUsd > q.Usd ? $"${q.Usd:N0}–{q.WorstUsd:N0}" : $"${q.Usd:N0}";
                        i.Time = $"{q.S} s{(q.WalkS > 0 ? $" +{q.WalkS} walk" : "")}"; i.Trail = $"T +{q.Trail}";
                        i.Alert = q.Reports == "no" ? "A 0" : q.Reports == "yes" ? $"A →{q.AlertIfReported}" : $"A ?→{q.AlertIfReported}";
                        i.Outcome = q.Outcome == "?" ? "?" : q.Outcome;
                        return i;
                    }
                case "loop":
                    if (!(t.Kind == "cabinet" || (t.Kind == "camera" && t.Key == "entrance"))) return null;
                    { var i = I("loop", "entrance", "", "Loop the entrance camera"); i.Time = $"{R.LoopS} s"; i.Trail = $"T +{R.LoopTrail}"; i.Outcome = "tethered"; return i; }
                case "reroute":
                    if (t.Kind is not ("cabinet" or "patrol")) return null;
                    { var i = I("reroute", "patrol", "", "Reroute the patrol"); i.Time = $"{R.RerouteS} s"; i.Trail = $"T +{R.RerouteTrail}"; return i; }
                case "act":
                    if (t.Kind == "door") { var i = I("enter", "", "", "Enter the Office"); i.Time = $"{R.EnterS} s"; i.Alert = "seen = fail"; return i; }
                    if (t.Kind == "person" && t.Key == "councillor") { var i = I("take", "", "", "Take Vale"); i.Time = $"{R.TakeS} s"; i.Trail = g.Driver.Turned ? "T 0" : $"T +{R.TakeTrailUnturned}"; return i; }
                    if (t.Kind == "car" && t.Key == "getaway") { var i = I("extract", "", "", "Vale into the car"); i.Time = "now"; i.Outcome = "win"; return i; }
                    return null;
                case "fire":
                    {
                        string target = t.Kind == "person" ? t.Key : t.Kind is "patrol" or "rivals" or "security" ? t.Kind : t.Kind == "hostile" ? "hostiles" : "";
                        if (target.Length == 0) return null;
                        var i = I("fire", target, "", $"Fire on {t.Label}"); i.Time = "now"; i.Trail = $"T +{R.ShotTrail}"; i.Alert = $"A +{R.PerShot}"; i.Outcome = "loud"; return i;
                    }
            }
            return null;
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
                "person" => g.PersonPos(g.PersonByRole(t.Key)), "patrol" => g.PatrolPose()?.pos, "rivals" => g.RivalsPose()?.pos, "security" => g.SecurityPose()?.pos,
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

        /// <summary>Give an order now, stamped with the engine's tick. A refusal is the engine's, with its reason, and is in the log like any order.</summary>
        public bool Issue(Order o)
        {
            if (Game == null || Fault != null) return false;
            o.Tick = Game.Tick;
            (bool ok, string why) r = (false, "");
            if (!Guard("order", () => r = Game.Issue(o))) return false;
            Status = r.ok ? $"{o.Op}: {o.Do} {o.Approach} {o.Target}".Replace("  ", " ") : $"{o.Op} {o.Do} {o.Target}: {r.why}";
            Save();
            views.Tick(); views.Frame(1f); intentKey = "";
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
            frameEngineMs += Now() - t0; acc = 0f; views.Frame(1f);
            if (Game.Over && Ending.Length == 0) End();
        }

        /// <summary>Let the clock run to `tick`, one engine step at a time (what time passing does).</summary>
        public void RunTo(int tick)
        {
            if (Game == null || Fault != null) return;
            double t0 = Now();
            while (!Game.Over && Game.Tick < tick) if (!StepOnce()) break;
            frameEngineMs += Now() - t0; acc = 0f; views.Frame(1f);
            if (Game.Over && Ending.Length == 0) End();
        }

        /// <summary>The next scheduled change: a timeline mark, an order landing, an arrival, a guard or rival timer.</summary>
        public int NextMark()
        {
            var g = Game; int t = g.Tick, best = int.MaxValue;
            void C(int x) { if (x > t && x < best) best = x; }
            var s = g.S; foreach (var k in new[] { s.RivalsArrive, s.Chime, s.DriverLast, s.ValeExits, s.ValeAtCar, s.Over }) C(Game.T(k));
            int speed = g.R.CellSpeed;
            foreach (var op in g.Ops) { if (op.Busy != null) C(op.BusyEnd); if (op.Walk != null) C(t + (int)((op.Walk.Length - op.WalkMm + speed - 1) / speed)); }
            if (g.LureTick >= 0) { int w = Game.T(s.GuardWalkS), st = Game.T(s.GuardStayS); C(g.LureTick + w); C(g.LureTick + w + st); C(g.GuardAwayUntil); }
            C(g.RivalsDelayedUntil); C(g.BlockedFrom); C(g.BlockedUntil); C(g.SecurityArrives); C(g.Lockdown); C(g.PatrolAwayUntil); C(g.MissingNotice);
            return best == int.MaxValue ? t + 1 : best;
        }

        // ---------------------------------------------------------------- the session, the end

        string SessionFile()
        {
            var dir = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "out", "play")) : Path.Combine(Application.persistentDataPath, "play");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"handover-{DateTime.Now:yyyyMMdd-HHmmss}.script.json");
        }

        /// <summary>The session as a replayable script with its run record: start state, every order (taken or refused), checkpoints, until this tick.</summary>
        void Save()
        {
            if (Game == null || SessionPath.Length == 0) return;
            try { File.WriteAllText(SessionPath, Script.Write(Game, Bundle.Inputs.Hash) + "\n"); } catch (Exception e) { Debug.LogWarning($"Play: session not saved: {e.Message}"); }
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
            Debug.Log($"Play: {Ending}; session {SessionPath}");
        }

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
