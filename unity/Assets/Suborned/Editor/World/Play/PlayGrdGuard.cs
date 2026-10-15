using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Play
{
    /// <summary>The GPU Resident Drawer stays off while the Handover plays in the Editor (brief 011 D; Sky's first hand play froze the Editor,
    /// 2026-10-15 18:16). Entering Play Mode re-creates the drawer, which registers every renderer in the generated scene (27,354 mesh renderers)
    /// and uploads their instance data in the first frame. With the Scene view open, that first frame ran out of the Metal graphics ring
    /// buffer ("Ran out of Graphics Ring Buffer space", from SceneView.DoDrawCamera, straight after "GPU Resident Drawer created" in
    /// Logs/Editor-prev.log), and the player loop stopped. It happened again here on the same Mac with the drawer on, and not with it off.
    /// When the active scene holds the Handover host, the URP asset's drawer mode is set to Disabled on the way into Play Mode and put back
    /// on the way out, keeping the old value in SessionState so a domain reload in between still restores it. Players, perf builds and Edit
    /// Mode keep the asset's own setting. The asset is never saved by this.</summary>
    [InitializeOnLoad]
    static class PlayGrdGuard
    {
        const string Key = "Suborned.Play.GrdWas"; // drawer mode + 1, 0 = nothing to restore

        static PlayGrdGuard()
        {
            EditorApplication.playModeStateChanged += Changed;
            if (!EditorApplication.isPlayingOrWillChangePlaymode) Restore(); // Edit Mode after a reload: put back anything left over
        }

        static void Changed(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.ExitingEditMode && PlayInstall.Root() != null && UniversalRenderPipeline.asset is { } urp && urp.gpuResidentDrawerMode != GPUResidentDrawerMode.Disabled)
            {
                SessionState.SetInt(Key, (int)urp.gpuResidentDrawerMode + 1);
                urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
            }
            else if (s == PlayModeStateChange.EnteredEditMode) Restore();
        }

        static void Restore()
        {
            int v = SessionState.GetInt(Key, 0);
            if (v <= 0 || UniversalRenderPipeline.asset is not { } urp) return;
            urp.gpuResidentDrawerMode = (GPUResidentDrawerMode)(v - 1);
            SessionState.EraseInt(Key);
        }
    }
}
