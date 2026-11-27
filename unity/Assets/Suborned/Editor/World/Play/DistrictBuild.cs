using Suborned.Play;
using Suborned.Sim;
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

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var h = root.GetComponentInChildren<HandoverHost>(true);
                if (!h || h.rangeMode)
                    continue;
                h.districtCrowd = DistrictCrowdBake.Make(h.figures);
                DistrictPublicRealm.Install(h, scene);
                h.districtTram = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Kit/Prefabs/Vehicles/DK_Tram.prefab");
                h.districtTramMid = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Kit/Models/Vehicles/DK_Tram_L1.fbx");
                h.districtTramFar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Kit/Models/Vehicles/DK_Tram_L2.fbx");
                h.districtRobotMid = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Kit/Models/Robots/DK_PatrolBot104_L1.fbx");
                h.districtRobotFar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Kit/Models/Robots/DK_PatrolBot104_L2.fbx");
                if (!h.districtRobotMid || !h.districtRobotFar)
                    throw new BuildFailedException("The patrol robot LODs are required.");
                h.districtServiceRobots = new[] {
                    Service(StreetRobot.Porter), Service(StreetRobot.Shopkeeper), Service(StreetRobot.Waiter),
                    Service(StreetRobot.Nanny), Service(StreetRobot.Butler)
                };
                if (!h.districtTram || !h.districtTramMid || !h.districtTramFar)
                    throw new BuildFailedException("The district tram and both kit LODs are required.");
            }
        }

        static HandoverHost.ServiceRobotModel Service(StreetRobot kind)
        {
            var path = "Assets/Suborned/Kit/Models/Robots/DK_Robot" + kind + "_Play";
            GameObject Load(string suffix) => AssetDatabase.LoadAssetAtPath<GameObject>(path + suffix + ".fbx")
                ?? throw new BuildFailedException("Missing service robot mesh " + path + suffix);
            return new HandoverHost.ServiceRobotModel { kind = kind, near = Load(""), middle = Load("_L1"), far = Load("_L2") };
        }
    }
}
