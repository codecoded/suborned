#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// decokit_materials.json -> URP material assets. Clear-coated kinds (enamel, decal, art) use Complex Lit
    /// so the lacquer layer gets its own sharp highlight over the softer base, as in the Cycles icons.
    internal static class DecoKitMaterials
    {
        const string LitShader = "OldTown/DecoKit/Lit Cutaway";  // URP Lit + the 2.5D cutaway (make_cutaway_shaders.py)
        const string ComplexLitShader = "OldTown/DecoKit/Complex Lit Cutaway";

        internal static Dictionary<string, Material> BuildAll()
        {
            EnsureFolder(MaterialDir);
            var lib = new Dictionary<string, Material>();
            foreach (var row in DecoKitData.Materials().materials) lib[row.name] = Build(row);
            AssetDatabase.SaveAssets();
            return lib;
        }

        static Material Build(MaterialRow row)
        {
            var path = $"{MaterialDir}/{row.name}.mat";
            if (row.kind == "face") return Face(row, path);
            var shader = Shader.Find(row.coat > 0f ? ComplexLitShader : LitShader);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = row.name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;

            var srgb = new Color(row.color[0], row.color[1], row.color[2], 1f);
            mat.SetColor("_BaseColor", srgb);  // SetColor treats values as sRGB in a linear project
            mat.SetFloat("_WorkflowMode", 1f);
            mat.SetFloat("_Smoothness", row.smoothness);
            mat.SetFloat("_Metallic", row.metallic);
            bool foliage = row.kind == "cutout";  // matches mats.py's near-zero leaf specular
            mat.SetFloat("_EnvironmentReflections", foliage ? 0f : 1f);
            mat.SetFloat("_SpecularHighlights", foliage ? 0f : 1f);
            ApplyCoat(mat, row);
            Matte096(mat, row.name);
            ApplySurface(mat, row);
            ApplyTextures(mat, row);
            ApplyEmission(mat, row, srgb);
            BaseShaderGUI.SetMaterialKeywords(mat, LitGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// A robot's display face: DK_RobotFace.shader, LEDs in the row's colour at its emission strength (the glyphs
        /// and layout come per robot from RobotFace).
        static Material Face(MaterialRow row, string path)
        {
            var shader = Shader.Find("OldTown/DecoKit/RobotFace");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = row.name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            var linear = new Color(row.color[0], row.color[1], row.color[2], 1f).linear * row.emission;
            mat.SetColor("_Glow", linear.gamma);  // pre-encoded: SetColor converts gamma->linear (see ApplyEmission)
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void ApplyCoat(Material mat, MaterialRow row)
        {
            if (!mat.HasProperty("_ClearCoat")) return;
            mat.SetFloat("_ClearCoat", row.coat > 0f ? 1f : 0f);
            mat.SetFloat("_ClearCoatMask", row.coat);
            mat.SetFloat("_ClearCoatSmoothness", row.coatSmoothness);
        }

        /// <summary>Glint, brief 096 "K2" (Sky: "k2 is good"): the enamels go matte, as the kit's do (tools/decokit/unity_mats.py MATTE_KIT): smoothness 0.4, no clear coat,
        /// no environment reflections, so they stop mirroring the play scene's teal-grey street cube. The colourway Paint variants copy these properties. Cars keep their gloss.</summary>
        internal static bool Matte096(Material mat, string name)
        {
            if (!mat || !name.StartsWith("DK_Enamel_") && !name.StartsWith("DK_RobotEnamel_")) return false;
            mat.SetFloat("_Smoothness", 0.4f); mat.SetFloat("_EnvironmentReflections", 0f);
            if (mat.HasProperty("_ClearCoat")) { mat.SetFloat("_ClearCoat", 0f); mat.SetFloat("_ClearCoatMask", 0f); }
            BaseShaderGUI.SetMaterialKeywords(mat, LitGUI.SetMaterialKeywords); // _ENVIRONMENTREFLECTIONS_OFF on, _CLEARCOAT off (Build sets them after; SwapHq's copied materials need it here)
            EditorUtility.SetDirty(mat);
            return true;
        }

        static void ApplySurface(Material mat, MaterialRow row)
        {
            bool transparent = row.alpha < 1f;
            bool clip = row.kind == "decal" || row.kind == "cutout";
            mat.SetFloat("_Surface", transparent ? 1f : 0f);
            mat.SetFloat("_Blend", 0f);  // alpha
            mat.SetFloat("_AlphaClip", clip ? 1f : 0f);
            mat.SetFloat("_Cutoff", .5f);
            mat.SetFloat("_Cull", row.kind == "cutout" ? 0f : 2f);
            if (transparent)
            {
                var c = mat.GetColor("_BaseColor");
                c.a = row.alpha;  // same alpha-blended film as mats.py
                mat.SetColor("_BaseColor", c);
            }
        }

        static void ApplyTextures(Material mat, MaterialRow row)
        {
            mat.SetTexture("_BaseMap", null);
            mat.SetTexture("_BumpMap", null);
            mat.SetTexture("_MetallicGlossMap", null);
            mat.SetTexture("_OcclusionMap", null);
            if (string.IsNullOrEmpty(row.texture)) return;
            bool sheet = !string.IsNullOrEmpty(row.mask);  // pave_sheet.py set: 1 mm texels need the full 4K
            int max = sheet ? 4096 : 2048;
            var tiling = row.tileMetres > 0f ? Vector2.one / row.tileMetres : Vector2.one;
            var albedo = DecoKitTextures.Albedo(row.texture, row.kind == "decal" || row.kind == "cutout", max);
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTextureScale("_BaseMap", tiling);
            if (!string.IsNullOrEmpty(row.normal))
            {
                mat.SetTexture("_BumpMap", DecoKitTextures.Normal(row.normal, max));
                mat.SetFloat("_BumpScale", 1f);
            }
            if (!sheet) return;
            // Lit reads smoothness = mask.a * _Smoothness and occlusion = mask.g, all on _BaseMap's UVs
            var mask = DecoKitTextures.Mask(row.mask, max);
            mat.SetTexture("_MetallicGlossMap", mask);
            mat.SetTexture("_OcclusionMap", mask);
            mat.SetFloat("_Smoothness", 1f);
            mat.SetFloat("_OcclusionStrength", 1f);
        }

        static void ApplyEmission(Material mat, MaterialRow row, Color srgb)
        {
            bool emits = row.emission > 0f;
            // Linear radiance, same units as Blender's emission strength. SetColor/SetVector both convert colour
            // properties gamma->linear in a linear project, so pre-encode to land on the exact linear value.
            var linear = srgb.linear * row.emission;
            mat.SetColor("_EmissionColor", emits ? linear.gamma : Color.black);
            mat.SetTexture("_EmissionMap", emits ? mat.GetTexture("_BaseMap") : null);
            mat.globalIlluminationFlags = emits ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                                                : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            if (emits) mat.EnableKeyword("_EMISSION");
            else mat.DisableKeyword("_EMISSION");
        }
    }
}
#endif
