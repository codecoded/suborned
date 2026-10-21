using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 027 B5, the host side of Keel's cover snap (EXPERIMENTAL, lab preset, GameFightLab.cs): where a hovered move will end.
    /// Read only (Game.PreviewSnap); the engine snaps the move order itself, so nothing here changes a run.</summary>
    public sealed partial class HandoverHost
    {
        partial void CoverSnapPreview(int k, Pt to, MoveParts mp)
        {
            if (mp == null || mp.Refused.Length > 0 || Game == null || !Game.CoverSnapOn) return;
            var c = Game.PreviewSnap((OpId)k, to); if (c == null) return;
            mp.SnapFrom = frame.World(to); mp.SnapId = c.Id; mp.At = frame.World(c.At); // the ring stands where the agent will kneel
        }
    }
}
