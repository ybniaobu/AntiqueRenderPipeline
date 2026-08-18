using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    internal abstract partial class YBaseShaderGUI
    {
        // ----------------------------------------------------------------------------------------------------
        // Draw Properties
        // ----------------------------------------------------------------------------------------------------
        
        protected void DrawBaseTexAndColor()
        {
            // 不知道为什么 TexturePropertySingleLine 有显示 Bug，但 Unity URP 自己使用就没问题，暂时使用自定义函数代替
            // m_MaterialEditor.TexturePropertySingleLine(k_BaseTexText, m_BaseTexProperty, m_BaseColorProperty);
            DrawTextureColorProps(m_BaseTexProperty, m_BaseColorProperty, k_BaseTexText);
        }
        
        protected void DrawEmissionTexAndColor()
        {
            m_MaterialEditor.TexturePropertyWithHDRColor(k_EmissionTexText, m_EmissionTexProperty, m_EmissionColorProperty, false);
        }

        protected void DrawEmission()
        {
            m_MaterialEditor.TexturePropertyWithHDRColor(k_EmissionTexText, m_EmissionTexProperty, m_EmissionColorProperty, false);
            
            // bool emissionEnabled = (m_EmissionColorProperty.colorValue.maxColorComponent > 0.0f) || (m_EmissionTexProperty.textureValue != null);
            // EditorGUI.BeginDisabledGroup(!emissionEnabled);
            // EditorGUI.indentLevel += 2;
            // DrawUnityEmissionBakedGIProperty();
            // EditorGUI.indentLevel -= 2;
            // EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.indentLevel += 2;
            DrawUnityEmissionBakedGIProperty();
            EditorGUI.indentLevel -= 2;
            EditorGUI.EndDisabledGroup();
        }
        
        protected void DrawTileOffset()
        {
            m_MaterialEditor.TextureScaleOffsetProperty(m_BaseTexProperty);
        }
        
        protected void DrawAlphaClippingAndCutoff()
        {
            m_MaterialEditor.ShaderProperty(m_AlphaClippingProperty, m_AlphaClippingProperty.displayName);
            m_MaterialEditor.ShaderProperty(m_AlphaCutoffProperty, m_AlphaCutoffProperty.displayName);
        }

        protected void DrawAlphaBlending()
        {
            m_MaterialEditor.ShaderProperty(m_SrcBlendProperty, m_SrcBlendProperty.displayName);
            m_MaterialEditor.ShaderProperty(m_DstBlendProperty, m_DstBlendProperty.displayName);
            m_MaterialEditor.ShaderProperty(m_SrcBlendAlphaProperty, m_SrcBlendAlphaProperty.displayName);
            m_MaterialEditor.ShaderProperty(m_DstBlendAlphaProperty, m_DstBlendAlphaProperty.displayName);
            m_MaterialEditor.ShaderProperty(m_BlendOpProperty, m_BlendOpProperty.displayName);
            // m_MaterialEditor.ShaderProperty(m_AlphaToCoverageProperty, m_AlphaToCoverageProperty.displayName);
        }

        protected void DrawZWrite()
        {
            m_MaterialEditor.ShaderProperty(m_ZWriteProperty, m_ZWriteProperty.displayName);
        }

        protected void DrawZTest()
        {
            m_MaterialEditor.ShaderProperty(m_ZTestProperty, m_ZTestProperty.displayName);
        }

        protected void DrawCullMode()
        {
            m_MaterialEditor.ShaderProperty(m_CullModeProperty, m_CullModeProperty.displayName);
        }

        protected void DrawAlembicMotionVectorsToggle()
        {
            DrawFloatToggleProperty(m_AddPrecomputedVelocityProperty, k_AlembicMotionVectorsText);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Unity Built-in Properties
        // ----------------------------------------------------------------------------------------------------
        
        protected void DrawRenderQueue()
        {
            m_MaterialEditor.RenderQueueField();
        }

        /// <summary>
        /// 即使调用了此函数，若 Shader 中不包含 #pragma multi_compile_instancing 仍然不会绘制
        /// </summary>
        protected void DrawGPUInstancing()
        {
            m_MaterialEditor.EnableInstancingField();
        }

        protected void DrawDoubleSidedGI()
        {
            m_MaterialEditor.DoubleSidedGIField();
        }
        
        protected void DrawUnityEmissionBakedGIProperty()
        {
            m_MaterialEditor.LightmapEmissionProperty();
            // m_MaterialEditor.LightmapEmissionFlagsProperty(0, true);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Utility Functions
        // ----------------------------------------------------------------------------------------------------
        
        protected void DrawShaderProperty(MaterialProperty prop, GUIContent label = null)
        {
            m_MaterialEditor.ShaderProperty(prop, label ?? EditorGUIUtility.TrTextContent(prop.displayName));
        }

        protected void DrawFloatToggleProperty(MaterialProperty prop, GUIContent label, int indentLevel = 0, bool isDisabled = false)
        {
            if (prop == null) return;

            EditorGUI.BeginDisabledGroup(isDisabled);
            EditorGUI.indentLevel += indentLevel;
            EditorGUI.BeginChangeCheck();
            MaterialEditor.BeginProperty(prop);
            bool newValue = EditorGUILayout.Toggle(label, Mathf.Approximately(prop.floatValue, 1.0f));
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue ? 1.0f : 0.0f;
            MaterialEditor.EndProperty();
            EditorGUI.indentLevel -= indentLevel;
            EditorGUI.EndDisabledGroup();
        }
        
        protected Texture DrawTextureProperty(MaterialProperty textureProp, GUIContent label)
        {
            MaterialEditor.BeginProperty(textureProp);
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.showMixedValue = textureProp.hasMixedValue;
            Texture tex = m_MaterialEditor.TexturePropertyMiniThumbnail(rect, textureProp, label.text, label.tooltip);
            EditorGUI.showMixedValue = false;
            MaterialEditor.EndProperty();
            return tex;
        }

        protected Texture DrawTextureColorProps(MaterialProperty textureProp, MaterialProperty colorProp, GUIContent label, bool alpha = true, bool hdr = false)
        {
            MaterialEditor.BeginProperty(textureProp);
            MaterialEditor.BeginProperty(colorProp);

            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.showMixedValue = textureProp.hasMixedValue;
            Texture tex = m_MaterialEditor.TexturePropertyMiniThumbnail(rect, textureProp, label.text, label.tooltip);
            EditorGUI.showMixedValue = false;
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = colorProp.hasMixedValue;
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var col = EditorGUI.ColorField(MaterialEditor.GetRectAfterLabelWidth(rect), GUIContent.none, colorProp.colorValue, true, alpha, hdr);
            EditorGUI.indentLevel = indentLevel;
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo(colorProp.displayName);
                colorProp.colorValue = col;
            }
            EditorGUI.showMixedValue = false;

            MaterialEditor.EndProperty();
            MaterialEditor.EndProperty();
            return tex;
        }
    }
}