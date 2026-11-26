using System.Linq;
using Suborned.OldTown;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.World.Play
{
    static class DistrictDaylight
    {
        public static void Apply(Scene scene)
        {
            var old = RenderSettings.skybox;
            // Both shaders take the original sky textures. The play version also listens to the
            // camera palette, sun direction and evening blend supplied by AtmosphereLens.
            if (old && old.shader.name == "OldTown/DecoKit/Space Age Sky")
            {
                var shader = Shader.Find("DecoKit/Space Age Sky");
                if (!shader) throw new BuildFailedException("The Harbour daylight sky shader is missing");
                var sky = new Material(old) { name = "Harbour daylight", shader = shader };
                sky.SetColor("_Zenith", new Color(.13f,.35f,.58f));
                sky.SetColor("_Mid", new Color(.34f,.62f,.79f));
                sky.SetColor("_Horizon", new Color(.85f,.84f,.76f));
                sky.SetFloat("_HazeHeight", .085f);
                sky.SetVector("_Cirrus", new Vector4(.56f,.64f,2.85f,.50f));
                RenderSettings.skybox = sky;
            }

            var look = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OldTownPlayLook>(true)).FirstOrDefault();
            if (!look || !look.lightSwap) return;
            // Open sky fills horizontal ledges. Under the soffits the fill comes chiefly from the street.
            look.ambientSky = new Color(.56f,.65f,.75f);
            look.ambientEquator = new Color(.43f,.46f,.48f);
            look.ambientGround = new Color(.40f,.35f,.30f);
            look.ambientProbe = TimeOfDay097.Probe(look.ambientSky, look.ambientEquator, look.ambientGround);
        }
    }
}
