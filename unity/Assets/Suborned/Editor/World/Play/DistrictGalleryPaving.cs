using System.Linq;
using Suborned.Play;
using Suborned.Sim;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.Play
{
    sealed partial class DistrictStreetArchitecture
    {
        // The two housing podiums are built directly against the old road. Their threshold
        // fans and drainage run within the two metres outside the shop, clear of the traffic lane.
        void GalleryPaving(Transform root, SiteFrame frame, DistrictGround.Gallery[] bays)
        {
            var flags = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Suborned/OldTownArt/DecoKit/Generated/DK_Pave_Flags_Metric.mat");
            if (!flags) throw new BuildFailedException("The metric pavement flags are missing");
            var mats = new[] { flags, Colour("Terrazzo_Ivory", false), Colour("Tile_Teal", false), Colour("Bronze", false) };
            foreach (var run in bays.GroupBy(b => (b.Yaw, line: Mathf.RoundToInt(Vector3.Dot(
                frame.World(b.At), Quaternion.Euler(0,b.Yaw,0) * Vector3.forward) * 10))))
            {
                var first = run.First();
                var rotation = Quaternion.Euler(0, first.Yaw, 0);
                var direction = rotation * Vector3.right;
                var centre = frame.World(first.At);
                centre.y = 0;
                var s = new Surface(mats.Length);
                foreach (var b in run)
                {
                    float x = Vector3.Dot(frame.World(b.At) - centre, direction);
                    float half = b.WidthCm * .005f + .275f;
                    // The flags meet the existing podium at the high end and feather into the road.
                    Vector3 V(float u, float z, float lift = 0) =>
                        new Vector3(u, Mathf.Lerp(.215f,.031f,Mathf.Clamp01((z-.34f)/1.22f)) + lift, z);
                    void Strip(int material, float a, float d)
                    {
                        s.Face(material,V(x-half,a),V(x-half,d),V(x+half,d),V(x+half,a),true);
                    }
                    Strip(0,.03f,.34f); Strip(0,.34f,1.56f); Strip(0,1.56f,1.92f);
                    Strip(3,1.92f,1.95f); Strip(2,1.95f,2.02f); Strip(1,2.02f,2.11f);
                    float door = x + b.WidthCm * .01f * (b.Variant % 2 == 0 ? -.27f : .27f);
                    for (int j=0;j<16;j++)
                    {
                        float a=j*Mathf.PI/16, d=(j+1)*Mathf.PI/16;
                        Vector3 Point(float angle,float radius) => V(door + Mathf.Cos(angle)*radius,.35f + Mathf.Sin(angle)*radius,.012f);
                        s.Face(1,Point(a,1.23f),Point(d,1.23f),Point(d,1.32f),Point(a,1.32f),true);
                        s.Face(2,Point(a,1.12f),Point(d,1.12f),Point(d,1.19f),Point(a,1.19f),true);
                        if (j%2==0) s.Face(3,Point(a+.055f,.26f),Point(a+.095f,.26f),Point(a+.095f,1.09f),Point(a+.055f,1.09f),true);
                    }
                }
                var go = new GameObject("Shop pavement and terrazzo thresholds");
                go.transform.SetParent(root,false); go.transform.SetPositionAndRotation(centre,rotation);
                go.AddComponent<MeshFilter>().sharedMesh = s.Finish(go.name);
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = mats; r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }
}
