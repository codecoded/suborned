using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.Play
{
    sealed class DistrictTradeProps
    {
        readonly Dictionary<string, DistrictFerryGardens.Kit> kits = new Dictionary<string, DistrictFerryGardens.Kit>();
        Material enamel;
        Material florist;

        public bool Place(string kit, Transform parent, Vector3 at, int yaw)
        {
            if (kit != "FlowerBarrow" && kit != "ProduceStall" && kit != "PneumaticPost" && kit != "LuggageTrolley")
                return false;
            if (!kits.TryGetValue(kit, out var model))
            {
                if (!enamel) enamel = Finish();
                var finish = enamel;
                if (kit == "FlowerBarrow")
                {
                    if (!florist)
                    {
                        florist = new Material(enamel) { name = "Harbour florist • cut stems" };
                        florist.SetFloat("_AlphaClip", 1);
                        florist.SetFloat("_Cutoff", .34f);
                        florist.EnableKeyword("_ALPHATEST_ON");
                        florist.SetOverrideTag("RenderType", "TransparentCutout");
                        florist.renderQueue = (int)RenderQueue.AlphaTest;
                    }
                    finish = florist;
                }
                model = new DistrictFerryGardens.Kit(kit, finish, .17f, .061f, .012f);
                kits.Add(kit, model);
            }
            model.Place(parent, at, Quaternion.Euler(0, yaw, 0));
            return true;
        }

        static Material Finish()
        {
            Texture2D Sheet(string name) =>
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Suborned/Materials/Play/StreetTrades" + name + ".png")
                ?? throw new BuildFailedException("Missing street trade atlas " + name);
            var mat = new Material(Shader.Find("DecoKit/Lit Cutaway")) { name = "Harbour street trades • enamel and canvas", enableInstancing = true };
            mat.SetTexture("_BaseMap", Sheet("Colour"));
            mat.SetTexture("_MetallicGlossMap", Sheet("Metal"));
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Smoothness", .82f);
            mat.SetFloat("_SpecularHighlights", 0);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.SetTexture("_EmissionMap", Sheet("Lights"));
            mat.SetColor("_EmissionColor", new Color(1.12f, .97f, .78f));
            mat.EnableKeyword("_EMISSION");
            return mat;
        }
    }
}
