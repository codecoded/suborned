using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 027 B5, the host side of Keel's cover snap: where a hovered move will end. Engine 0.35 (brief 039 B, GDD v0.30.1 §06 cover:
    /// "a move ending within 1.5 m of it snaps to its kneel point"): out of the lab the engine's own move-snap test (Game.PreviewMoveSnap) moves
    /// the ring to the kneel point, and says whether that end flanks the focus's cover (Glyph b-final: one struck shield disc over it). Read
    /// only; the engine snaps the move order itself, so nothing here changes a run.</summary>
    public sealed partial class HandoverHost
    {
        partial void CoverSnapPreview(int k, Pt to, MoveParts mp)
        {
            if (mp == null || mp.Refused.Length > 0 || Game == null || !Game.CoverSnapOn) return;
            if (!Game.Lab)
            {
                var s = Game.PreviewMoveSnap((OpId)k, to); if (!s.Snaps) return;
                mp.SnapFrom = frame.World(to); mp.SnapId = s.Cover; mp.At = frame.World(s.EndAt); // the ring stands where the agent will kneel
                if (s.FocusCover.Length > 0 && !s.FocusCovered && Game.PostureOf(s.Focus) is Posture fp && fp.Cover == s.FocusCover) mp.FlankAt = frame.World(fp.KneelAt);
                return;
            }
            var c = Game.PreviewSnap((OpId)k, to); if (c == null) return;
            mp.SnapFrom = frame.World(to); mp.SnapId = c.Id; mp.At = frame.World(c.At); // the ring stands where the agent will kneel
        }
    }
}
