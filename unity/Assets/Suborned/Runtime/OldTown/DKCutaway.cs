using UnityEngine;

namespace Suborned.OldTown
{
    /// <summary>The 2.5D camera's cutaway (DecoKit Lit Cutaway shaders, DK_Cutaway.hlsl): architecture standing above
    /// the floor and hiding the ground within a disc round the view's focus thins to a dithered ghost, so towers and
    /// walls never bury the play space at any Q/E heading. On an orthographic camera it follows the ground point under
    /// screen centre.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class DKCutaway : MonoBehaviour
    {
        // A/B (Scratch/Captures/DecoKit/ab_*, sh20_*, col*_*): a ghost keeps the tower in the view and the street readable
        // through it (clean cuts left hollow shells and floating crowns; .35 let a striped tower swamp the street); at the
        // close play zoom a band bites a chunk out of a solid tower and its crown's shadow pools in the cut, so column
        public const float Ghost = .2f;

        [SerializeField] float radiusPerZoom = .8f;          // the disc's radius as a share of the ortho half-height
        [SerializeField] float maxRadius = 40f;              // m
        [SerializeField] float feather = 2f;                 // m of dithered edge
        [SerializeField, Range(0f, 1f)] float ghost = Ghost; // what stays inside the disc (0 = clean away)
        [SerializeField] float floor = 4.5f;                 // m: the shopfronts and canopies below it always stand
        [SerializeField] bool column = true;                 // also thin everything above a cut (no solid crowns)
        [SerializeField] bool shadows = true;                // the ghost casts a ghost shadow (no dark pit inside a cut shell)

        static readonly int FocusId = Shader.PropertyToID("_DKCutFocus"), ViewId = Shader.PropertyToID("_DKCutView"),
                            ShapeId = Shader.PropertyToID("_DKCutShape");
        Camera view;

        void OnEnable() => view = GetComponent<Camera>();
        void OnDisable() => Clear();

        void LateUpdate()
        {
            var ray = view.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            if (ray.direction.y > -.01f) { Clear(); return; }
            var focus = ray.GetPoint(-ray.origin.y / ray.direction.y);
            Set(focus, transform.forward, Mathf.Min(maxRadius, radiusPerZoom * view.orthographicSize), feather, ghost, floor, column, shadows);
        }

        public static void Set(Vector3 focus, Vector3 forward, float radius, float feather, float ghost, float floor, bool column,
                               bool shadows)
        {
            Shader.SetGlobalVector(FocusId, new Vector4(focus.x, focus.y, focus.z, radius));
            Shader.SetGlobalVector(ViewId, new Vector4(forward.x, forward.y, forward.z, floor));
            Shader.SetGlobalVector(ShapeId, new Vector4(feather, ghost, column ? 1f : 0f, shadows ? 1f : 0f));
        }

        public static void Clear() => Shader.SetGlobalVector(FocusId, Vector4.zero);
    }
}
