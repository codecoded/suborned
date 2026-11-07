namespace Suborned.OldTown
{
    /// <summary>Where the Handover zone and our network stand in the old town: the plan's (x, y) is old Unity (x, y + DZ) on z, its root 206 m
    /// along Unity z (OldTownSwap, brief 075). The one copy every builder reads (the swap, the play install, the lean cut, the perf planner, the
    /// 097 vents), so a move of the zone moves them all; before, five builders kept their own 206, and a part that kept a world point baked
    /// before the move (the fountain's orbits, Sky 2026-11-07: "anywhere else that has been positioned around world coords when it is part of a
    /// larger object?") went 206 m astray. Lives here, in the old town's runtime assembly, because every editor assembly can reach it.</summary>
    public static class OldTownFrame
    {
        public const float DZ = 206f;
    }
}
