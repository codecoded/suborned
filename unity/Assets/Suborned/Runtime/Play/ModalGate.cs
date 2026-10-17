namespace Suborned.Play
{
    /// <summary>What reaches the world while a modal is up (Sky 2026-10-16, playing the build: "i'm on this screen yet when I move mouse
    /// everything is showing up as label, and responding"). The briefing (until Begin) and the end card are modal: no hover, no price chip, no
    /// pips or labels reacting, no click or key reaches the street. Allowed: Begin (Space, the button), the briefing key, Esc, the debug key,
    /// and looking round: the camera's pan keys, the middle-drag grab, the wheel zoom and the view's turn (Q / E, Alt + middle-drag; brief 016) (Latch's call: a player reading the briefing may want
    /// to look at the square; looking changes nothing in the run). Restart works on the end card. The history drawer and the Alt layer are
    /// not modal: the clock runs under them, and the city keeps answering. One table, so a test can read it.</summary>
    public static class ModalGate
    {
        public enum Modal { None, Briefing, EndCard, Menu }

        /// <summary>Whether a key action reaches the host while `modal` is up.</summary>
        public static bool Allows(Modal modal, PlayAction a)
        {
            if (modal == Modal.None) return true;
            if (modal == Modal.Menu) return false; // the pause menu (Esc): its three buttons and Esc only; nothing reaches the street or the camera
            switch (a)
            {
                case PlayAction.PanUp: case PlayAction.PanDown: case PlayAction.PanLeft: case PlayAction.PanRight: case PlayAction.Debug:
                case PlayAction.TurnLeft: case PlayAction.TurnRight: case PlayAction.TurnDrag: return true;
                case PlayAction.Pause: case PlayAction.Briefing: return modal == Modal.Briefing;
                case PlayAction.Restart: return modal == Modal.EndCard;
                default: return false;
            }
        }

        /// <summary>The pointer: hover (chips, labels, rings, pips), left and right clicks, the box and the sweep never reach the street under a
        /// modal; the camera's grab and zoom do.</summary>
        public static bool AllowsHover(Modal modal) => modal == Modal.None;
        public static bool AllowsClicks(Modal modal) => modal == Modal.None;
        public static bool AllowsCamera(Modal modal) => modal != Modal.Menu;
    }
}
