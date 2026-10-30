using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 084, the host's side of the cue switches (FightCues): who was shot at last, by the cell; the body's jolts. Read only; view only.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The cell's last shot on each target (unscaled time, hit): the target corners' snap and twitch.</summary>
        public readonly Dictionary<string, (float at, bool hit)> CueShotOn = new();
        /// <summary>From Shot, per engine shot.</summary>
        void CueShot(string by, string target, bool hit, Vector3 aim)
        {
            if (!FightCues.Any) return;
            if (by is "scout" or "fixer" or "tech" or "enforcer") CueShotOn[target] = (Time.unscaledTime, hit);
            if (!FightCues.Jolt) return;
            if (hit) views.CueHit(ViewKey(target), aim);
            views.CueKick(ViewKey(by), aim);
        }
        /// <summary>This frame's learned fans (ShowSight): the hollow cones read them.</summary>
        public IReadOnlyList<Fan> CueFans => shownFans;
        /// <summary>The cursor is on this watcher.</summary>
        public bool CueHovers(string watcher) => WatcherOf(Hover) == watcher;
    }
}
