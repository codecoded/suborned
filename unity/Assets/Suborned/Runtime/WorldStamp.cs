using UnityEngine;

namespace Suborned
{
    /// <summary>What the scene was built from, on the world root, written by the builder: the export's source line and the
    /// SHA-256 of the export file's bytes. A scene, a walk note and a build report are traced to one export by it (the source
    /// line names the plan version only: two exports of one plan share it).</summary>
    public sealed class WorldStamp : MonoBehaviour
    {
        public string source;
        /// <summary>Lower-case hex.</summary>
        public string exportSha256;
        /// <summary>The export file the scene was built from, relative to the repo root (2026-10-15): the people layer and the plates read
        /// their ground from it, so a build from a pinned copy stays readable while out/world/ is being regenerated.</summary>
        public string exportPath;
    }
}
