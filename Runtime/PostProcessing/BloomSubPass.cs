using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class BloomSubPass : PostProcessingSubPass
    {
        private class BloomPassData
        {
            public Material material;

            public TextureHandle inputTexture;
            
            public TextureHandle bloomTexture;
            public TextureHandle bloomPrefilteredTexture;
            public TextureHandle[] bloomPyramidUpTextures = new TextureHandle[k_MaxBloomPyramidLevels];
            public TextureHandle[] bloomPyramidDownTextures = new TextureHandle[k_MaxBloomPyramidLevels];
            
            public bool isBloomEnabled;
            public BloomMode bloomMode;
            public int iterationCount;
            public Vector4 bloomParams;
            public Vector4 bloomThreshold;
            public bool isBloomBicubicUpsampling;
        }
        
        private const int k_MaxBloomPyramidLevels = 12;
        
        private Bloom m_Bloom;
        
        private Material m_BloomMaterial;

        protected override void Initialize(ref YPipelineData data)
        {
            m_BloomMaterial = new Material(data.runtimeResources.BloomShader);
            m_BloomMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        public override void OnDispose()
        {
            m_Bloom = null;
            
            CoreUtils.Destroy(m_BloomMaterial);
            m_BloomMaterial = null;
        }

        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_Bloom = stack.GetComponent<Bloom>();

            // TODO：看看 URP 怎么 RasterPass 的。
            using (var builder = data.renderGraph.AddUnsafePass<BloomPassData>("Bloom", out var passData, ProfilingSampler.Get(YPipelineProfileIDs.Bloom)))
            {
                passData.material = m_BloomMaterial;
                passData.isBloomEnabled = m_Bloom.IsActive();
                
                builder.AllowPassCulling(false);
                
                if (m_Bloom.IsActive())
                {
                    // do bloom at half or quarter resolution
                    int width;
                    int height;
                    if (m_Bloom.ignoreRenderScale.value)
                    {
                        width = data.camera.pixelWidth >> (int)m_Bloom.bloomDownscale.value;
                        height = data.camera.pixelHeight >> (int)m_Bloom.bloomDownscale.value;
                    }
                    else
                    {
                        width = data.BufferSize.x >> (int)m_Bloom.bloomDownscale.value;
                        height = data.BufferSize.y >> (int)m_Bloom.bloomDownscale.value;
                    }

                    // Determine the iteration count
                    int minSize = Mathf.Min(width, height);
                    int iterationCount = Mathf.FloorToInt(Mathf.Log(minSize, 2.0f) - 1);
                    iterationCount = Mathf.Clamp(iterationCount, 1, m_Bloom.maxIterations.value);
                    passData.iterationCount = iterationCount;

                    // Texture Recording
                    if (data.asset.antiAliasingMode == AntiAliasingMode.TAA)
                    {
                        passData.inputTexture = data.TAATarget;
                        builder.UseTexture(data.TAATarget, AccessFlags.Read);
                    }
                    else
                    {
                        passData.inputTexture = data.CameraColorAttachment;
                        builder.UseTexture(data.CameraColorAttachment, AccessFlags.Read);
                    }
                    
                    TextureDesc bloomTextureDesc = new TextureDesc(width >> 1, height >> 1)
                    {
                        colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                        filterMode = FilterMode.Bilinear,
                        name = "Bloom Texture"
                    };
                    data.BloomTexture = data.renderGraph.CreateTexture(bloomTextureDesc);
                    passData.bloomTexture = data.BloomTexture;
                    builder.UseTexture(data.BloomTexture, AccessFlags.Write);

                    TextureDesc bloomPrefilteredTextureDesc = new TextureDesc(width, height)
                    {
                        colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                        filterMode = FilterMode.Bilinear,
                        name = "Bloom Prefiltered Texture"
                    };
                    passData.bloomPrefilteredTexture = builder.CreateTransientTexture(bloomPrefilteredTextureDesc);
                    
                    for (int i = 0; i < iterationCount; i++)
                    {
                        width >>= 1;
                        height >>= 1;
                        TextureDesc bloomPyramidUpDesc = new TextureDesc(width, height)
                        {
                            colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                            filterMode = FilterMode.Bilinear,
                            name = "Bloom Pyramid Up"
                            //name = "Bloom Pyramid Up" + i
                        };
                        passData.bloomPyramidUpTextures[i] = builder.CreateTransientTexture(bloomPyramidUpDesc);
                        
                        TextureDesc bloomPyramidDownDesc = new TextureDesc(width, height)
                        {
                            colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                            filterMode = FilterMode.Bilinear,
                            name = "Bloom Pyramid Down"
                            //name = "Bloom Pyramid Down" + i
                        };
                        passData.bloomPyramidDownTextures[i] = builder.CreateTransientTexture(bloomPyramidDownDesc);
                    }

                    // Data Recording
                    Vector4 bloomParams = m_Bloom.mode.value == BloomMode.Additive ? new Vector4(m_Bloom.additiveStrength.value, 0.0f) : new Vector4(m_Bloom.scatter.value, 0.0f);
                    passData.bloomParams = bloomParams;
                    float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
                    float knee = threshold * m_Bloom.thresholdKnee.value;
                    passData.bloomThreshold = new Vector4(threshold, knee - threshold, 2.0f * knee, 0.25f / (knee + 1e-6f));
                    passData.isBloomBicubicUpsampling = m_Bloom.bicubicUpsampling.value;
                    passData.bloomMode = m_Bloom.mode.value;
                }

                builder.SetRenderFunc((BloomPassData data, UnsafeGraphContext context) =>
                {
                    if (data.isBloomEnabled)
                    {
                        // Shader property and keyword setup
                        data.material.SetVector(YPipelineShaderIDs.k_BloomParamsID, data.bloomParams);
                        data.material.SetVector(YPipelineShaderIDs.k_BloomThresholdID, data.bloomThreshold);
                        CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_BloomBicubicUpsampling, data.isBloomBicubicUpsampling);
                        
                        // Prefilter
                        context.cmd.BeginSample("Prefilter");
                        BlitHelper.BlitGlobalTexture(context.cmd, data.inputTexture, data.bloomPrefilteredTexture, data.material, 0);
                        context.cmd.EndSample("Prefilter");
                        
                        // Downsample - gaussian pyramid
                        context.cmd.BeginSample("Downsample");
                        TextureHandle source = data.bloomPrefilteredTexture;
                        for (int i = 0; i < data.iterationCount; i++)
                        {
                            BlitHelper.BlitGlobalTexture(context.cmd, source, data.bloomPyramidUpTextures[i], data.material, 1);
                            BlitHelper.BlitGlobalTexture(context.cmd, data.bloomPyramidUpTextures[i], data.bloomPyramidDownTextures[i], data.material, 2);
                            source = data.bloomPyramidDownTextures[i];
                        }
                        context.cmd.EndSample("Downsample");
                        
                        // Upsample - bilinear or bicubic
                        context.cmd.BeginSample("Upsample");
                        int upsamplePass = data.bloomMode == BloomMode.Additive ? 3 : 4;
                        TextureHandle lastDst = data.bloomPyramidDownTextures[data.iterationCount - 1];
                        for (int i = data.iterationCount - 2; i >= 0; i--)
                        {
                            context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_BloomLowerTextureID, lastDst);
                            if (i == 0) BlitHelper.BlitGlobalTexture(context.cmd, data.bloomPyramidDownTextures[i], data.bloomTexture, data.material, upsamplePass);
                            else BlitHelper.BlitGlobalTexture(context.cmd, data.bloomPyramidDownTextures[i], data.bloomPyramidUpTextures[i], data.material, upsamplePass);
                            lastDst = data.bloomPyramidUpTextures[i];
                        }
                        context.cmd.EndSample("Upsample");
                    }
                });

            }
        }
    }
}