using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The end steps (Sky, playing, 2026-10-18: "it's really unclear on how to take Vale. When is that an option?"; Aster's queue, Fable's
    /// words H14 to H16 and the objective's steps in text.json). Read only: nothing here changes the run.
    /// The door is read from what the cell has learned, never the engine's live check (Glyph's ruling): DoorState go, unk or seen. H14 and the
    /// door marker both read it, so they always agree. While the Enforcer has no Enter order the
    /// door gets a marker (a dashed ring with the Enter glyph); while he is inside it shows his progress; once he is out with Vale the getaway car
    /// gets the marker with the car glyph and his first knob becomes "Vale into the car". A click on a marker with the Enforcer selected gives the
    /// order as his first knob and a right click would (walk, then the act; priced on hover as the target itself); without him selected, it
    /// selects him.</summary>
    public sealed partial class HandoverHost
    {
        int doorTick = -1; string doorState = ""; string doorEye = "";

        /// <summary>The door as the cell knows it (Glyph's ruling, Aster 2026-10-18: told versus learned): "go" when every watcher that can see
        /// the door is learned and, by what is learned, does not see it; "unk" when one of them is unlearned (a fact only told counts as
        /// unlearned); "seen" when a learned one sees it (DoorEye names whose eye: guard, camera, patrol); "" when Enter is not open at all
        /// (Vale not inside, the door held or locked). The engine still decides when the order runs: acting on "unk" can fail.</summary>
        public string DoorState { get { DoorRead(); return doorState; } }
        public string DoorEye { get { DoorRead(); return doorEye; } }

        void DoorRead()
        {
            var g = Game; if (g == null) { doorState = ""; return; }
            if (doorTick == g.Tick) return; doorTick = g.Tick; doorState = ""; doorEye = "";
            Guard("door state", () =>
            {
                if (g.Vale != ValeAt.Office || g.EntryInProgress) return;
                if (g.DoorHeld(g.EnterDoor) || (g.Alert >= 3 && g.DialNow.DoorsLock)) return;
                var door = g.S.At(g.S.GuardPost); bool unk = false;
                bool Learned(string w, bool routine) => g.Known(w) is { } k && k.Sight && k.How != "told" && (!routine || k.Routine);
                // the guard at his door is in plain sight: his eye
                if (g.GuardAtDoor) { doorState = "seen"; doorEye = "guard"; return; }
                // the entrance camera: the cell's own loop blinds it; else only its learned sight says
                if (!(g.LoopOn && g.LoopCam == "entrance"))
                {
                    if (!Learned("camera:entrance", false)) unk = true;
                    else if (g.CameraWatching("entrance", door)) { doorState = "seen"; doorEye = "camera"; return; }
                }
                // the patrol: down, or away on the cell's own reroute; else its learned sight and routine say
                if (!g.PatrolDown && g.PatrolAwayUntil <= g.Tick)
                {
                    if (!Learned("patrol", true)) unk = true;
                    else if (g.PatrolWatching(door)) { doorState = "seen"; doorEye = "patrol"; return; }
                }
                doorState = unk ? "unk" : "go";
            });
        }

        /// <summary>The Enforcer has been given Enter: walking to the door for it, at it, or inside.</summary>
        public bool EnterGiven { get { var op = Game?.Ops[(int)OpId.Enforcer]; return op != null && (op.Inside || op.Busy?.Do == "enter" || pending[(int)OpId.Enforcer]?.Do == "enter"); } }

        /// <summary>The door marker shows (any state) for an Enforcer who is outside, standing and has not been given Enter.</summary>
        public bool DoorMarked { get { var op = Game?.Ops[(int)OpId.Enforcer]; return op != null && !op.Inside && !op.Down && op.CarryBody.Length == 0 && !EnterGiven && DoorState.Length > 0; } }
        /// <summary>The door is learned open: the marker says go, the knob is ringed, H14 shows.</summary>
        public bool EnterAvailable => DoorMarked && DoorState == "go";

        /// <summary>Inside: Enter's progress per mille (the door marker's ring), else -1.</summary>
        public int EnterProgress { get { var op = Game?.Ops[(int)OpId.Enforcer]; return op != null && op.Inside ? doing[(int)OpId.Enforcer].Progress : -1; } }

        /// <summary>Out with Vale and no Extract given: the car marker.</summary>
        public bool ExtractAvailable { get { var g = Game; var op = g?.Ops[(int)OpId.Enforcer]; return op != null && g.Vale == ValeAt.WithEnforcer && !op.Down && op.Busy?.Do != "extract" && pending[(int)OpId.Enforcer]?.Do != "extract"; } }

        /// <summary>The objective's step (Fable's less.objective.steps): take, then bring once Enter is given, then carry once Vale is with him.</summary>
        public string ObjectiveKey => Game == null ? "take" : Game.Vale == ValeAt.WithEnforcer ? "carry" : Game.Vale == ValeAt.Office && EnterGiven ? "bring" : "take";

        /// <summary>A right click on a person's pin (the HUD's pip): as a right click on the person (Sky, route B: Vale's star over his car).</summary>
        public void RightOnPerson(string role)
        {
            if (!cam || views == null) return; var t = views.Find(role); if (!t || !t.gameObject.activeSelf) return;
            var sp = cam.WorldToScreenPoint(t.position + Vector3.up * 1.0f); if (sp.z <= 0) return;
            Physics.SyncTransforms(); PointerHover(sp); PointerRight(sp);
        }

        /// <summary>A marker clicked (door or car): with the Enforcer selected, his first knob on that target; else select him.</summary>
        public void StepMarkClick(string which)
        {
            if (Game == null || Modal != ModalGate.Modal.None || !cam) return;
            var e = (int)OpId.Enforcer;
            if (!Selected[e]) { Select(OpId.Enforcer, false); Status = "Enforcer selected"; return; }
            if (which == "door" && DoorState == "seen") { PopKey("door-seen", null, frame.World(Game.S.At(Game.S.GuardPost)), Feedback.Refused, "Seen there."); return; } // a learned eye on the door: no order
            if (Agent != OpId.Enforcer) Select(OpId.Enforcer, false);
            var t = views.Find(which == "car" ? "car/getaway" : "marker/door"); if (!t) return;
            var sp = cam.WorldToScreenPoint(t.position + Vector3.up * (which == "car" ? 0.8f : 1.0f)); if (sp.z <= 0) return;
            Physics.SyncTransforms(); int was = Picked; Pick(0); PointerHover(sp); PointerRight(sp);
            if (was != 0 && Picked == 0) { Picked = was; intentKey = ""; } // the click gave the order: the knob is not left armed
        }
    }
}
