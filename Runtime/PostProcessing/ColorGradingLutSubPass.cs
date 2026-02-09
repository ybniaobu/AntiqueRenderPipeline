using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class ColorGradingLutSubPass : PostProcessingSubPass
    {
        private class ColorGradingLutPassData
        {
            public Material material;
            
            public Vector4 colorGradingLUTParams;
            
            public Vector4 whiteBalance;
            public Vector4 colorAdjustmentsParams;
            public Color colorFilter;
            public Texture2D curveMaster;
            public Texture2D curveRed;
            public Texture2D curveGreen;
            public Texture2D curveBlue;
            public Texture2D curveHueVsHue;
            public Texture2D curveHueVsSat;
            public Texture2D curveLumVsSat;
            public Texture2D curveSatVsSat;
            
            public Vector4 smhShadows;
            public Vector4 smhMidtones;
            public Vector4 smhHighlights;
            public Vector4 smhRange;
            
            public Vector4 lggLift;
            public Vector4 lggGamma;
            public Vector4 lggGain;

            public int toneMappingPass;
            public Vector4 toneMappingParams;
        }
        
        private GlobalColorCorrections m_GlobalColorCorrections;
        private ShadowsMidtonesHighlights m_ShadowsMidtonesHighlights;
        private LiftGammaGain m_LiftGammaGain;
        private ToneMapping m_ToneMapping;
        
        private Material m_ColorGradingLutMaterial;

        protected override void Initialize(ref YPipelineData data)
        {
            m_ColorGradingLutMaterial = new Material(data.runtimeResources.ColorGradingLutShader);
            m_ColorGradingLutMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        public override void OnDispose()
        {
            m_GlobalColorCorrections = null;
            m_ShadowsMidtonesHighlights = null;
            m_LiftGammaGain = null;
            m_ToneMapping = null;
            
            CoreUtils.Destroy(m_ColorGradingLutMaterial);
            m_ColorGradingLutMaterial = null;
        }

        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_GlobalColorCorrections = stack.GetComponent<GlobalColorCorrections>();
            m_ShadowsMidtonesHighlights = stack.GetComponent<ShadowsMidtonesHighlights>();
            m_LiftGammaGain = stack.GetComponent<LiftGammaGain>();
            m_ToneMapping = stack.GetComponent<ToneMapping>();

            using (var builder = data.renderGraph.AddRasterRenderPass<ColorGradingLutPassData>("Color Grading Lut", out var passData))
            {
                passData.material = m_ColorGradingLutMaterial;
                
                builder.AllowPassCulling(false);
                
                // Lut
                int lutHeight = data.asset.bakedLUTResolution;
                int lutWidth = lutHeight * lutHeight;
                TextureDesc desc = new TextureDesc(lutWidth, lutHeight)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR),
                    filterMode = FilterMode.Bilinear,
                    name = "Color Grading Baked Lut"
                };
                data.ColorGradingLutTexture = data.renderGraph.CreateTexture(desc);
                builder.SetRenderAttachment(data.ColorGradingLutTexture, 0, AccessFlags.Write);
                
                passData.colorGradingLUTParams = new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1.0f));
                
                // Global Color Corrections
                passData.whiteBalance = ColorUtils.ColorBalanceToLMSCoeffs(m_GlobalColorCorrections.temperature.value, m_GlobalColorCorrections.tint.value);
                float hue = m_GlobalColorCorrections.hue.value - 0.5f;
                float exposure = Mathf.Pow(2.0f, m_GlobalColorCorrections.exposure.value);
                float contrast = m_GlobalColorCorrections.contrast.value;
                float saturation = m_GlobalColorCorrections.saturation.value;
                passData.colorAdjustmentsParams = new Vector4(hue, exposure, contrast, saturation);
                passData.colorFilter = m_GlobalColorCorrections.colorFilter.value.linear;
                
                passData.curveMaster = m_GlobalColorCorrections.master.value.GetTexture();
                passData.curveRed = m_GlobalColorCorrections.red.value.GetTexture();
                passData.curveGreen = m_GlobalColorCorrections.green.value.GetTexture();
                passData.curveBlue = m_GlobalColorCorrections.blue.value.GetTexture();
            
                passData.curveHueVsHue = m_GlobalColorCorrections.hueVsHue.value.GetTexture();
                passData.curveHueVsSat = m_GlobalColorCorrections.hueVsSat.value.GetTexture();
                passData.curveLumVsSat = m_GlobalColorCorrections.lumVsSat.value.GetTexture();
                passData.curveSatVsSat = m_GlobalColorCorrections.satVsSat.value.GetTexture();
                
                // Shadows Midtones Highlights
                var (shadows, midtones, highlights) = ColorUtils.PrepareShadowsMidtonesHighlights(m_ShadowsMidtonesHighlights.shadows.value, 
                    m_ShadowsMidtonesHighlights.midtones.value, m_ShadowsMidtonesHighlights.highlights.value);
                passData.smhShadows = shadows;
                passData.smhMidtones = midtones;
                passData.smhHighlights = highlights;
                passData.smhRange = new Vector4(m_ShadowsMidtonesHighlights.shadowsStart.value, m_ShadowsMidtonesHighlights.shadowsEnd.value, 
                    m_ShadowsMidtonesHighlights.highlightsStart.value, m_ShadowsMidtonesHighlights.highlightsEnd.value);
                
                // Lift Gamma Gain
                var (lift, gamma, gain) = ColorUtils.PrepareLiftGammaGain(m_LiftGammaGain.lift.value, m_LiftGammaGain.gamma.value, m_LiftGammaGain.gain.value);
                passData.lggLift = lift;
                passData.lggGamma = gamma;
                passData.lggGain = gain;
                
                // Tone Mapping
                TonemappingMode mode = m_ToneMapping.mode.value;
                int toneMappingPass;

                switch (mode)
                {
                    case TonemappingMode.Reinhard:
                        ReinhardMode reinhardMode = m_ToneMapping.reinhardMode.value;
                        if (reinhardMode == ReinhardMode.Simple) toneMappingPass = 1;
                        else if (reinhardMode == ReinhardMode.Extended) toneMappingPass = 2;
                        else toneMappingPass = 3;
                        passData.toneMappingParams = new Vector4(m_ToneMapping.minWhite.value, 0.0f);
                        break;
                    case TonemappingMode.Uncharted2Filmic:
                        toneMappingPass = 4;
                        passData.toneMappingParams = new Vector4(m_ToneMapping.exposureBias.value, 0.0f);
                        break;
                    case TonemappingMode.KhronosPBRNeutral:
                        toneMappingPass = 5;
                        break;
                    case TonemappingMode.ACES:
                        ACESMode acesMode = m_ToneMapping.aCESMode.value;
                        if (acesMode == ACESMode.Full) toneMappingPass = 6;
                        else if (acesMode == ACESMode.StephenHillFit) toneMappingPass = 7;
                        else toneMappingPass = 8;
                        break;
                    case TonemappingMode.AGXApproximation:
                        AgXMode agxMode = m_ToneMapping.aGXMode.value;
                        if (agxMode == AgXMode.Default) toneMappingPass = 9;
                        else if (agxMode == AgXMode.Golden) toneMappingPass = 10;
                        else toneMappingPass = 11;
                        break;
                    default:
                        toneMappingPass = 0;
                        break;
                }
                passData.toneMappingPass = toneMappingPass;

                builder.SetRenderFunc((ColorGradingLutPassData data, RasterGraphContext context) =>
                {
                    // Lut
                    data.material.SetVector(YPipelineShaderIDs.k_ColorGradingLUTParamsID, data.colorGradingLUTParams);
                    
                    // Global Color Corrections
                    data.material.SetVector(YPipelineShaderIDs.k_WhiteBalanceID, data.whiteBalance);
                    data.material.SetVector(YPipelineShaderIDs.k_ColorAdjustmentsParamsID, data.colorAdjustmentsParams);
                    data.material.SetColor(YPipelineShaderIDs.k_ColorFilterID, data.colorFilter);
            
                    data.material.SetTexture(YPipelineShaderIDs.k_CurveMasterID, data.curveMaster);
                    data.material.SetTexture(YPipelineShaderIDs.k_CurveRedID, data.curveRed);
                    data.material.SetTexture(YPipelineShaderIDs.k_CurveGreenID, data.curveGreen);
                    data.material.SetTexture(YPipelineShaderIDs.k_CurveBlueID, data.curveBlue);
            
                    data.material.SetTexture(YPipelineShaderIDs.k_CurveHueVsHueID, data.curveHueVsHue);
                    data.material.SetTexture(YPipelineShaderIDs.k_CurveHueVsSatID, data.curveHueVsSat);
                    data.material.SetTexture(YPipelineShaderIDs.k_CurveLumVsSatID, data.curveLumVsSat);
                    data.material.SetTexture(YPipelineShaderIDs.k_CurveSatVsSatID, data.curveSatVsSat);
                    
                    // Shadows Midtones Highlights
                    data.material.SetVector(YPipelineShaderIDs.k_SMHShadowsID, data.smhShadows);
                    data.material.SetVector(YPipelineShaderIDs.k_SMHMidtonesID, data.smhMidtones);
                    data.material.SetVector(YPipelineShaderIDs.k_SMHHighlightsID, data.smhHighlights);
                    data.material.SetVector(YPipelineShaderIDs.k_SMHRangeID, data.smhRange);
                    
                    // Lift Gamma Gain
                    data.material.SetVector(YPipelineShaderIDs.k_LGGLiftID, data.lggLift);
                    data.material.SetVector(YPipelineShaderIDs.k_LGGGammaID, data.lggGamma);
                    data.material.SetVector(YPipelineShaderIDs.k_LGGGainID, data.lggGain);
                    
                    // Tone Mapping
                    if (data.toneMappingPass <= 4) data.material.SetVector(YPipelineShaderIDs.k_ToneMappingParamsID, data.toneMappingParams);
                    
                    // Blit
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, data.toneMappingPass, MeshTopology.Triangles, 3);
                });
            }
        }
    }
}