using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.OldTown.Crowd
{
    /// <summary>F swaps the crowd test between the 2.5D camera and first person at street level: the street's walker
    /// (FirstPersonController, eye 1.62 m) dropped on the walk-graph node nearest the 2.5D view's focus,
    /// facing its heading; F again lifts the 2.5D view back over wherever the walker got to. The crowd culls against
    /// whichever camera is live (both are tagged MainCamera, one is ever active).</summary>
    public sealed class CrowdTestViews : MonoBehaviour
    {
        [SerializeField] VatCrowd crowd;
        [SerializeField] CrowdTestCamera iso;
        [SerializeField] FirstPersonController walker;

        public bool FirstPerson => walker != null && walker.gameObject.activeSelf;

        void Update()
        {
            if (walker != null && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) Set(!FirstPerson);
        }

        /// <summary>First person at the node nearest `at` (default: the 2.5D focus), facing yaw deg (default: its heading).</summary>
        public void Set(bool firstPerson, Vector3? at = null, float? yaw = null)
        {
            if (firstPerson)
            {
                var spot = Nearest(at ?? iso.Focus);
                var view = walker.GetComponentInChildren<Camera>(true).transform;
                walker.gameObject.SetActive(false);  // the CharacterController would fight a teleport
                walker.Configure(view, spot + Vector3.up * .05f, Quaternion.Euler(0f, yaw ?? iso.Yaw, 0f));
                walker.gameObject.SetActive(true);
                iso.gameObject.SetActive(false);
            }
            else
            {
                var p = walker.transform.position;
                iso.Set(new Vector3(p.x, 0f, p.z), iso.Size, iso.Yaw);
                iso.gameObject.SetActive(true);
                walker.gameObject.SetActive(false);
            }
        }

        Vector3 Nearest(Vector3 at)
        {
            if (crowd == null || crowd.Graph == null || crowd.Graph.nodes.Count == 0) return at;
            var best = crowd.Graph.nodes[0];
            float d = float.MaxValue;
            foreach (var n in crowd.Graph.nodes)
            {
                float dx = n.x - at.x, dz = n.z - at.z;
                if (dx * dx + dz * dz < d) (best, d) = (n, dx * dx + dz * dz);
            }
            return best;
        }
    }
}
