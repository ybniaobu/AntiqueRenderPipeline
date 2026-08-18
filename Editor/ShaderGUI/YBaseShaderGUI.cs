using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    internal abstract partial class YBaseShaderGUI : ShaderGUI
    {
        protected enum Expandable
        {
            Properties = 1 << 0,
            Transparency = 1 << 1,
            Options = 1 << 2
        }
        
        protected readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue);
        protected virtual uint MaterialFilter => uint.MaxValue;
        
        protected virtual bool ShowDefaultGUI => false;
        protected virtual bool ShowCustomGUI => true;
        private bool m_FirstTimeApply = true;
        
        protected MaterialEditor m_MaterialEditor;
        protected Object[] m_Materials;
        protected Material m_Material;
        protected MaterialProperty[] m_Properties;
        
        // ----------------------------------------------------------------------------------------------------
        // OnGUI Related
        // ----------------------------------------------------------------------------------------------------
            
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (ShowDefaultGUI) base.OnGUI(materialEditor, properties);
            if (!ShowCustomGUI) return;
            
            m_MaterialEditor = materialEditor;
            m_Materials = materialEditor.targets;
            m_Material = materialEditor.target as Material;
            m_Properties = properties;
            
            FindProperties(properties);

            if (m_FirstTimeApply)
            {
                OnOpenGUI();
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI();
        }

        protected virtual void OnOpenGUI()
        {
            var filter = (Expandable) MaterialFilter;
            // Generate the foldouts
            if (filter.HasFlag(Expandable.Properties)) m_MaterialScopeList.RegisterHeaderScope(k_PropertiesHeader, (uint) Expandable.Properties, DrawProperties);
            if (filter.HasFlag(Expandable.Transparency)) m_MaterialScopeList.RegisterHeaderScope(k_TransparencyHeader, (uint) Expandable.Transparency, DrawTransparency);
            if (filter.HasFlag(Expandable.Options)) m_MaterialScopeList.RegisterHeaderScope(k_OptionsHeader, (uint) Expandable.Options, DrawOptions);
        }

        private void ShaderPropertiesGUI()
        {
            EditorGUIUtility.labelWidth = 0f;
            m_MaterialScopeList.DrawHeaders(m_MaterialEditor, m_Material);
        }
        
        protected virtual void DrawProperties(Material material) { }
        protected virtual void DrawTransparency(Material material) { }
        protected virtual void DrawOptions(Material material) { }
        
        // ----------------------------------------------------------------------------------------------------
        // Validate Material Related
        // ----------------------------------------------------------------------------------------------------
        
        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
            SetMaterialKeywords(material);
        }

        protected virtual void SetMaterialKeywords(Material material)
        {
            SetEmissiveFlag(material);
            SetupMotionVectorsPassAndKeywords(material);
        }
        
        protected void SetEmissiveFlag(Material material)
        {
            if (material.HasProperty(PropertiesName.k_EmissionColor))
            {
                bool emissionEnabled = material.GetColor(PropertiesName.k_EmissionColor).maxColorComponent > 0.0f;
                material.globalIlluminationFlags = emissionEnabled ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.None;
            }
        }
        
        private const string k_MotionVectorPassName = "MotionVectors";
        
        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        protected static void SetupMotionVectorsPassAndKeywords(Material material)
        {
            bool motionVectorPassEnabled = false;
            
            if (material.HasProperty(PropertiesName.k_AddPrecomputedVelocity))
            {
                motionVectorPassEnabled = material.GetFloat(PropertiesName.k_AddPrecomputedVelocity) != 0.0f;
                CoreUtils.SetKeyword(material, YPipelineKeywords.k_AddPrecomputedVelocity, motionVectorPassEnabled);
            }
            
            if (material.GetShaderPassEnabled(k_MotionVectorPassName) != motionVectorPassEnabled)
            {
                material.SetShaderPassEnabled(k_MotionVectorPassName, motionVectorPassEnabled);
            }
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Assign New Shader Related
        // ----------------------------------------------------------------------------------------------------
        
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            material.shaderKeywords = null; // Clear all keywords for fresh start
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            SetMaterialKeywords(material);
        }
    }
}