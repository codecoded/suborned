using System.Collections.Generic;

namespace Suborned.Play
{
    public sealed partial class HostViews
    {
        HashSet<string> lastBodies = new(), nextBodies = new();
        readonly HashSet<string> deadBodies = new();

        string FallenState(string key, Fig f)
        {
            if (f.own != null && f.own.Contains("death")) return "death";
            if (deadBodies.Contains(key) && Has(f, "kill_victim")) return "kill_victim";
            if (f.state == "lying" || !Has(f, "knockout")) return "lying";
            var pose = f.anim.GetCurrentAnimatorStateInfo(0);
            return pose.IsName("knockout") && pose.normalizedTime >= 0.98f ? "lying" : "knockout";
        }

        public float FallProgress(string key)
        {
            if (key == null || !figs.TryGetValue(key, out var f) || !f.model.gameObject.activeInHierarchy) return -1f;
            var pose = f.anim.GetCurrentAnimatorStateInfo(0);
            return pose.IsName("death") || pose.IsName("kill_victim") || pose.IsName("knockout") ? pose.normalizedTime : f.state == "lying" ? 1f : 0f;
        }
    }
}
