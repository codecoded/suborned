using Suborned.Play;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.World.Play
{
    public sealed class DistrictBuild : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene,BuildReport report)
        {
            foreach(var root in scene.GetRootGameObjects()) {
                var h=root.GetComponentInChildren<HandoverHost>(true);
                if(!h || h.rangeMode) continue;
                h.districtTram=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Kit/Prefabs/Vehicles/DK_Tram.prefab");
                h.districtTramMid=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Kit/Models/Vehicles/DK_Tram_L1.fbx");
                h.districtTramFar=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Kit/Models/Vehicles/DK_Tram_L2.fbx");
                if(!h.districtTram || !h.districtTramMid || !h.districtTramFar) throw new BuildFailedException("The district tram and both kit LODs are required.");
            }
        }
    }
}
