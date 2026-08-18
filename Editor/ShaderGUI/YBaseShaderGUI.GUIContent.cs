using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    internal abstract partial class YBaseShaderGUI
    {
        protected static readonly GUIContent k_PropertiesHeader = EditorGUIUtility.TrTextContent("Material Properties", "Material properties for the shader.");
        protected static readonly GUIContent k_TransparencyHeader = EditorGUIUtility.TrTextContent("Transparency Settings", "Transparency settings for the material.");
        protected static readonly GUIContent k_OptionsHeader = EditorGUIUtility.TrTextContent("Advanced Options", "Additional options for the material.");

        protected static readonly GUIContent k_BaseTexText = EditorGUIUtility.TrTextContent("Albedo Texture", "Specifies the base color of the surface. If using Alpha Blending or Alpha Clipping, material uses the base texture’s & base color's alpha channel.");
        protected static readonly GUIContent k_SpecularIntensityText = EditorGUIUtility.TrTextContent("Reflectance", "Specifies the specular intensity for dielectric(non-metallic) materials. The default value is 0.5 or 4% reflectance, which is the specular intensity of most dielectric materials.");
        
        protected static readonly GUIContent k_RoughnessText = EditorGUIUtility.TrTextContent("Roughness", "Specifies the microfacet roughness of the surface. A value of 0.0 is perfect mirror reflection, while a value of 1.0 is completely rough.");
        protected static readonly GUIContent k_MetallicText = EditorGUIUtility.TrTextContent("Metallic", "Blends between a dielectric and a metallic material model. At 0.0 the material consists of a diffuse base layer with a specular layer, while a value of 1.0 is fully specular reflection.");
        
        protected static readonly GUIContent k_RoughnessTexText = EditorGUIUtility.TrTextContent("Roughness Texture", "Specifies a grayscale texture that controls the roughness of the surface.");
        protected static readonly GUIContent k_MetallicTexText = EditorGUIUtility.TrTextContent("Metallic Texture", "Specifies a grayscale texture that controls the metallic property of the surface.");
        protected static readonly GUIContent k_AOTexText = EditorGUIUtility.TrTextContent("Ambient Occlusion Texture", "Specifies a grayscale texture that darkens the surface based on the amount of ambient light that reaches it.");
        
        protected static readonly GUIContent k_HybridTexText = EditorGUIUtility.TrTextContent("Hybrid Texture", "The hybrid texture is a packed texture that contains roughness(R channel), metallic(G channel), and ambient occlusion(Alpha channel) information.");
        protected static readonly GUIContent k_RoughnessScaleText = EditorGUIUtility.TrTextContent("Roughness Scale", "Scales the roughness value sampled from the hybrid texture.");
        protected static readonly GUIContent k_MetallicScaleText = EditorGUIUtility.TrTextContent("Metallic Scale", "Scales the metallic value sampled from the hybrid texture.");
        protected static readonly GUIContent k_AOScaleText = EditorGUIUtility.TrTextContent("Ambient Occlusion Scale", "Scales the ambient occlusion value sampled from the hybrid texture.");

        protected static readonly GUIContent k_NormalTexText = EditorGUIUtility.TrTextContent("Normal Texture", "Specifies a normal map to create the illusion of surface detail and depth.");
        protected static readonly GUIContent k_NormalIntensityText = EditorGUIUtility.TrTextContent("Intensity", "Controls the strength of the normal map effect.");
        
        protected static readonly GUIContent k_EmissionTexText = EditorGUIUtility.TrTextContent("Emission Texture", "Determines the color and intensity of light that the surface of the material emits.");
        
        protected static readonly GUIContent k_AlembicMotionVectorsText = EditorGUIUtility.TrTextContent("Enable Alembic Motion Vectors", "When enabled, the material will use motion vectors from the Alembic animation cache. Should NOT be used on regular meshes or Alembic caches without precomputed motion vectors.");
    }
}