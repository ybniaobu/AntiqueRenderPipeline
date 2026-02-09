using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using YPipeline;

namespace YPipeline.Editor
{
    public abstract class YPipelineBaseShaderGUI : ShaderGUI
    {
        protected virtual bool ShowDefaultGUI { get; set; } = false;
        
        // ----------------------------------------------------------------------------------------------------
        // ShaderGUI Event Functions
        // ----------------------------------------------------------------------------------------------------
        
        protected MaterialEditor m_MaterialEditor;
        protected Object[] m_Materials;
        protected Material m_Material;
        protected MaterialProperty[] m_Properties;
        protected string[] m_Keywords;
            
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (ShowDefaultGUI) base.OnGUI(materialEditor, properties);
            m_MaterialEditor = materialEditor;
            m_Materials = materialEditor.targets;
            m_Material = materialEditor.target as Material;
            m_Properties = properties;
        }

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // GUI Content 
        // ----------------------------------------------------------------------------------------------------

        protected static class Styles
        {
            public static readonly GUIContent k_BaseTex = EditorGUIUtility.TrTextContent("Albedo Texture",
                "Specifies the base color of the surface. If using Alpha Blending or Alpha Clipping, material uses the base texture’s & base color's alpha channel.");
            public static readonly GUIContent k_EmissionTex = EditorGUIUtility.TrTextContent("Emission Texture",
                "Determines the color and intensity of light that the surface of the material emits.");
            
            public static readonly GUIContent k_AlembicMotionVectors = EditorGUIUtility.TrTextContent("Enable Precomputed Alembic Motion Vectors",
                "When enabled, the material will use motion vectors from the Alembic animation cache. Should NOT be used on regular meshes or Alembic caches without precomputed motion vectors.");
            
            
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Find Properties
        // ----------------------------------------------------------------------------------------------------

        // Common Material Properties
        protected MaterialProperty m_BaseTexProperty;
        protected MaterialProperty m_BaseColorProperty;
        protected MaterialProperty m_EmissionTexProperty;
        protected MaterialProperty m_EmissionColorProperty;
        protected MaterialProperty m_AlphaClippingProperty;
        protected MaterialProperty m_AlphaCutoffProperty;
        
        protected MaterialProperty m_AddPrecomputedVelocityProperty;
        
        public virtual void FindProperties(MaterialProperty[] properties)
        {
            if (m_Material == null) return;
            
            m_BaseTexProperty = FindProperty(YPipelineMaterialProperties.k_BaseTex, properties, false);
            m_BaseColorProperty = FindProperty(YPipelineMaterialProperties.k_BaseColor, properties, false);
            m_EmissionTexProperty = FindProperty(YPipelineMaterialProperties.k_EmissionTex, properties, false);
            m_EmissionColorProperty = FindProperty(YPipelineMaterialProperties.k_EmissionColor, properties, false);
            m_AlphaClippingProperty = FindProperty(YPipelineMaterialProperties.k_AlphaClipping, properties, false);
            m_AlphaCutoffProperty = FindProperty(YPipelineMaterialProperties.k_AlphaCutoff, properties, false);
            
            m_AddPrecomputedVelocityProperty = FindProperty(YPipelineMaterialProperties.k_AddPrecomputedVelocity, properties, false);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Properties
        // ----------------------------------------------------------------------------------------------------
        
        protected static void DrawFloatToggleProperty(GUIContent styles, MaterialProperty prop, int indentLevel = 0, bool isDisabled = false)
        {
            if (prop == null)
                return;

            EditorGUI.BeginDisabledGroup(isDisabled);
            EditorGUI.indentLevel += indentLevel;
            EditorGUI.BeginChangeCheck();
            MaterialEditor.BeginProperty(prop);
            bool newValue = EditorGUILayout.Toggle(styles, prop.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue ? 1.0f : 0.0f;
            MaterialEditor.EndProperty();
            EditorGUI.indentLevel -= indentLevel;
            EditorGUI.EndDisabledGroup();
        }
        
        public virtual void DrawBaseTexAndColor(Material material)
        {
            if (m_BaseTexProperty != null && m_BaseColorProperty != null)
            {
                m_MaterialEditor.ColorProperty(m_BaseColorProperty, "base color");
                m_MaterialEditor.TexturePropertySingleLine(Styles.k_BaseTex, m_BaseTexProperty, m_BaseColorProperty);
            }
        }
        
        public virtual void DrawEmissionTexAndColor(Material material)
        {
            if (m_EmissionTexProperty != null && m_EmissionColorProperty != null)
            {
                m_MaterialEditor.TexturePropertyWithHDRColor(Styles.k_EmissionTex, m_EmissionTexProperty, m_EmissionColorProperty, false);
            }
        }

        public virtual void DrawAlembicMotionVectorsOption(Material material)
        {
            if (m_AddPrecomputedVelocityProperty != null)
            {
                DrawFloatToggleProperty(Styles.k_AlembicMotionVectors, m_AddPrecomputedVelocityProperty);
            }
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Motion Vector Related
        // ----------------------------------------------------------------------------------------------------
        
        private const string k_MotionVectorPassName = "MotionVectors";
        
        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        protected static void SetupMotionVectorsPassAndKeywords(Material material)
        {
            bool motionVectorPassEnabled = false;
            
            if (material.HasProperty(YPipelineMaterialProperties.k_AddPrecomputedVelocity))
            {
                motionVectorPassEnabled = material.GetFloat(YPipelineMaterialProperties.k_AddPrecomputedVelocity) != 0.0f;
                CoreUtils.SetKeyword(material, YPipelineKeywords.k_AddPrecomputedVelocity, motionVectorPassEnabled);
            }
            
            if (material.GetShaderPassEnabled(k_MotionVectorPassName) != motionVectorPassEnabled)
            {
                material.SetShaderPassEnabled(k_MotionVectorPassName, motionVectorPassEnabled);
            }
        }
    }
}