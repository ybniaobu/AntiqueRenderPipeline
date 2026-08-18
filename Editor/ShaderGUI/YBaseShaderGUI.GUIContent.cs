using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    internal abstract partial class YBaseShaderGUI
    {
        protected static readonly GUIContent k_SurfaceHeader = EditorGUIUtility.TrTextContent("Surface Options");
        protected static readonly GUIContent k_TransparencyHeader = EditorGUIUtility.TrTextContent("Transparency Options");
        protected static readonly GUIContent k_OptionsHeader = EditorGUIUtility.TrTextContent("Advanced Options");
        protected static readonly GUIContent k_BaseSubstrateHeader = EditorGUIUtility.TrTextContent("Base Substrate Options");
        protected static readonly GUIContent k_CoatLayerHeader = EditorGUIUtility.TrTextContent("Coat Layer Options");

        protected static readonly GUIContent k_BaseTexText = EditorGUIUtility.TrTextContent("Albedo Texture", "Specifies the base color of the surface. If using Alpha Blending or Alpha Clipping, material uses the base texture’s & base color's alpha channel.");
        protected static readonly GUIContent k_SpecularIntensityText = EditorGUIUtility.TrTextContent("Reflectance", "Specifies the specular intensity for dielectric(non-metallic) materials. The default value is 0.5 or 4% reflectance, which is the specular intensity of most dielectric materials.");
        
        protected static readonly GUIContent k_HybridTexText = EditorGUIUtility.TrTextContent("Hybrid Texture", "The hybrid texture stores roughness in the R channel, metallic in the G channel, and ambient occlusion in the Alpha channel.");
        protected static readonly GUIContent k_RoughnessText = EditorGUIUtility.TrTextContent("Roughness", "Specifies the microfacet roughness of the surface. A value of 0.0 is perfect mirror reflection, while a value of 1.0 is completely rough.");
        protected static readonly GUIContent k_MetallicText = EditorGUIUtility.TrTextContent("Metallic", "Blends between a dielectric and a metallic material model. At 0.0 the material consists of a diffuse base layer with a specular layer, while a value of 1.0 is fully specular reflection.");
        
        protected static readonly GUIContent k_RoughnessTexText = EditorGUIUtility.TrTextContent("Roughness Texture", "Specifies a grayscale texture that controls the roughness of the surface.");
        protected static readonly GUIContent k_MetallicTexText = EditorGUIUtility.TrTextContent("Metallic Texture", "Specifies a grayscale texture that controls the metallic property of the surface.");
        protected static readonly GUIContent k_AOTexText = EditorGUIUtility.TrTextContent("Ambient Occlusion Texture", "Specifies a grayscale texture that darkens the surface based on the amount of ambient light that reaches it.");
        
        protected static readonly GUIContent k_NormalTexText = EditorGUIUtility.TrTextContent("Normal Texture", "Specifies a normal map to create the illusion of surface detail and depth.");
        protected static readonly GUIContent k_NormalIntensityText = EditorGUIUtility.TrTextContent("Intensity", "Controls the strength of the normal map effect.");
        
        protected static readonly GUIContent k_AdvancedTexText = EditorGUIUtility.TrTextContent("Advanced Texture", "The advanced texture stores anisotropy in the R channel, anisotropy rotation in the G channel, coat in the B channel, and coat roughness in the Alpha channel.");
        protected static readonly GUIContent k_AnisotropyText = EditorGUIUtility.TrTextContent("Anisotropy", "Controls the amount of anisotropic reflection on the surface. A value of 0.0 is isotropic, while a value of 1.0 is fully anisotropic.");
        protected static readonly GUIContent k_AnisotropyRotationText = EditorGUIUtility.TrTextContent("Anisotropy Rotation", "Rotates the direction of the anisotropic reflection on the surface.");
        
        protected static readonly GUIContent k_CoatText = EditorGUIUtility.TrTextContent("Coat", "Controls the intensity of the coat layer on top of the base dielectric or conductor layer.");
        protected static readonly GUIContent k_CoatRoughnessText = EditorGUIUtility.TrTextContent("Coat Roughness", "Controls the roughness of the coat layer on the surface.");
        protected static readonly GUIContent k_CoatTexText = EditorGUIUtility.TrTextContent("Coat Texture", "The coat texture stores the coat intensity in the R channel and coat roughness in the G channel.");
        protected static readonly GUIContent k_CoatColorTexText = EditorGUIUtility.TrTextContent("Coat Color Texture", "Specifies a texture that controls the color of the coat layer on the surface.");
        protected static readonly GUIContent k_CoatIORText = EditorGUIUtility.TrTextContent("Coat IOR", "Controls the index of refraction for the coat layer. A value of 1.0 is no refraction, while a value of 1.5 is typical for most dielectric materials.");
        protected static readonly GUIContent k_CoatNormalTexText = EditorGUIUtility.TrTextContent("Coat Normal Texture", "Specifies a normal map that defines the coat layer's surface detail. It is independent of the base material's normal map.");
        protected static readonly GUIContent k_CoatNormalIntensityText = EditorGUIUtility.TrTextContent("Intensity", "Controls the strength of the coat normal map effect.");
        
        protected static readonly GUIContent k_EmissionTexText = EditorGUIUtility.TrTextContent("Emission Texture", "Determines the color and intensity of light that the surface of the material emits.");
        
        protected static readonly GUIContent k_AlembicMotionVectorsText = EditorGUIUtility.TrTextContent("Enable Alembic Motion Vectors", "When enabled, the material will use motion vectors from the Alembic animation cache. Should NOT be used on regular meshes or Alembic caches without precomputed motion vectors.");
    }
}