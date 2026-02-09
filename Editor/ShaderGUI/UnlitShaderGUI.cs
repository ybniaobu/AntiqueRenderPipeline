using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    public class UnlitShaderGUI : YPipelineBaseShaderGUI
    {
        protected override bool ShowDefaultGUI => true;
        
        // ----------------------------------------------------------------------------------------------------
        // ShaderGUI Event Functions
        // ----------------------------------------------------------------------------------------------------
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            
            FindProperties(properties);
            // DrawBaseTexAndColor(m_Material);
            // DrawEmissionTexAndColor(m_Material);
            
            UnityEmissionProperty();
            DrawAlembicMotionVectorsOption(m_Material);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Find Properties
        // ----------------------------------------------------------------------------------------------------

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Properties
        // ----------------------------------------------------------------------------------------------------

        private void UnityEmissionProperty()
        {
            EditorGUI.BeginChangeCheck();
            m_MaterialEditor.LightmapEmissionProperty();
            //m_MaterialEditor.LightmapEmissionFlagsProperty(0, true);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material m in m_Materials) 
                {
                    m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
            }
        }
        
        // ----------------------------------------------------------------------------------------------------
        // ValidateMaterial Related
        // ----------------------------------------------------------------------------------------------------

        public override void ValidateMaterial(Material material)
        {
            SetupMotionVectorsPassAndKeywords(material);
        }
    }
}