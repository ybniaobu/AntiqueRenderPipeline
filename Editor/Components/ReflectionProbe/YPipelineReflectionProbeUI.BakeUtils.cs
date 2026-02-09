using System.IO;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Rendering;
using YPipeline;

namespace YPipeline.Editor
{
    public static partial class YPipelineReflectionProbeUI
    {
        /// <summary>
        /// 生成包含 7 个 mipmap 的 Octahedral Atlas：低质量的高度为 cubemapSize * 0.5，宽度为 cubemapSize * 0.75；
        /// 中质量高度为 cubemapSize，宽度为 cubemapSize * 1.5；高质量的高度为 cubemapSize * 2，宽度为 cubemapSize * 3；
        /// </summary>
        private static void GenerateOctahedralAtlas(SerializedYPipelineReflectionProbe serialized, ReflectionProbe probe, string directoryPath, Quality3Tier quality)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings(out YPipelineEditorResources editorResources))
            {
                Debug.LogError("未找到烘焙所需着色器资源文件 (YPipelineEditorResources)！");
                return;
            }
            
            // Render Texture
            int qualityLevel = (int) Mathf.Pow(2, (int) quality);
            int width = (int) (qualityLevel * 0.75 * probe.resolution);
            int height = (int) (qualityLevel * 0.5 * probe.resolution);
            RenderTexture renderTexture = new RenderTexture(width, height, 0)
            {
                format =  RenderTextureFormat.ARGBHalf,
                enableRandomWrite = true,
            };
            
            // Octahedral Mapping
            ComputeShader cs = editorResources.OctahedralMappingCS;
            int kernel = cs.FindKernel("OctahedralMappingKernel");
            cs.SetTexture(kernel, "_OutputTexture", renderTexture);
            cs.SetTexture(kernel, "_Cubemap", probe.texture);
            cs.SetVector("_TextureSize", new Vector4(width, height, 1.0f / width, 1.0f / height));

            for (int i = 0; i < 7; i++)
            {
                cs.SetInt("_MipMap", i);
                int threadGroups = Mathf.CeilToInt(height / 8.0f / Mathf.Pow(2, i));
                cs.Dispatch(kernel, threadGroups, threadGroups, 1);
            }
            
            // GPU to CPU
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            
            // Save to EXR
            string filePath = Path.Combine(directoryPath, probe.name);
            switch (quality)
            {
                case Quality3Tier.Low:
                    filePath = filePath + "_OctahedralAtlas_Low" + ".exr";
                    break;
                case Quality3Tier.Medium:
                    filePath = filePath + "_OctahedralAtlas_Medium" + ".exr";
                    break;
                case Quality3Tier.High:
                    filePath = filePath + "_OctahedralAtlas_High" + ".exr";
                    break;
            }
            var bytes = ImageConversion.EncodeToEXR(tex, Texture2D.EXRFlags.CompressZIP);
            File.WriteAllBytes(filePath, bytes);
            AssetDatabase.Refresh();
            
            // Clear Resources
            RenderTexture.active = null;
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(tex);
            
            // Texture Import Settings
            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 8192;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            
            // Serialize
            Texture atlas = AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture)) as Texture;
            switch (quality)
            {
                case Quality3Tier.Low:
                    serialized.octahedralAtlasLow.objectReferenceValue = atlas;
                    break;
                case Quality3Tier.Medium:
                    serialized.octahedralAtlasMedium.objectReferenceValue = atlas;
                    break;
                case Quality3Tier.High:
                    serialized.octahedralAtlasHigh.objectReferenceValue = atlas;
                    break;
            }
        }

        private static void CalculateSHData(SerializedYPipelineReflectionProbe serialized, ReflectionProbe probe)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings(out YPipelineEditorResources editorResources))
            {
                Debug.LogError("未找到烘焙所需着色器资源文件 (YPipelineEditorResources)！");
                return;
            }
            
            // First Dispatch
            ComputeShader cs = editorResources.CubemapSHCoefficientsCS;
            int kernel01 = cs.FindKernel("ComputeCubemapSHKernel");
            int cubemapSize = probe.texture.width;
            cs.SetTexture(kernel01, "_Cubemap", probe.texture);
            cs.SetVector("_Params", new Vector4(cubemapSize, 0.0f));
            
            int groupNum01 = cubemapSize / 8;
            ComputeBuffer buffer01 = new ComputeBuffer(groupNum01 * groupNum01 * 6 * 9, 16);
            cs.SetBuffer(kernel01, "_Result", buffer01);
            cs.Dispatch(kernel01, groupNum01, groupNum01, 6);
            
            // Second Dispatch
            int kernel02 = cs.FindKernel("SecondAddReductionKernel");
            
            int groupNum02 = groupNum01 / 8;
            ComputeBuffer buffer02 = new ComputeBuffer(groupNum02 * groupNum02 * 6 * 9, 16);
            cs.SetBuffer(kernel02, "_Input", buffer01);
            cs.SetBuffer(kernel02, "_Result", buffer02);
            cs.Dispatch(kernel02, groupNum02, groupNum02, 6);
            
            // 回读
            AsyncGPUReadback.Request(buffer02, (callback) =>
            {
                if (callback.hasError)
                {
                    Debug.LogError("回读出错");
                    buffer01.Release();
                    buffer02.Release();
                    return;
                }
                
                NativeArray<Vector4> data = callback.GetData<Vector4>();
                int count = data.Length / 9;
                Vector4[] SH = new Vector4[9];
                for (int i = 0; i < count; i++)
                {
                    SH[0] += data[i * 9 + 0] * SHUtils.k_ZHCoefficients[0];
                    SH[1] += data[i * 9 + 1] * SHUtils.k_ZHCoefficients[1];
                    SH[2] += data[i * 9 + 2] * SHUtils.k_ZHCoefficients[1];
                    SH[3] += data[i * 9 + 3] * SHUtils.k_ZHCoefficients[1];
                    SH[4] += data[i * 9 + 4] * SHUtils.k_ZHCoefficients[2];
                    SH[5] += data[i * 9 + 5] * SHUtils.k_ZHCoefficients[2];
                    SH[6] += data[i * 9 + 6] * SHUtils.k_ZHCoefficients[3];
                    SH[7] += data[i * 9 + 7] * SHUtils.k_ZHCoefficients[2];
                    SH[8] += data[i * 9 + 8] * SHUtils.k_ZHCoefficients[4];
                }
                
                serialized.SHData.GetArrayElementAtIndex(0).vector4Value = new Vector4(SH[3].x, SH[1].x, SH[2].x, SH[0].x - SH[6].x);
                serialized.SHData.GetArrayElementAtIndex(1).vector4Value = new Vector4(SH[4].x, SH[5].x, SH[6].x * 3.0f, SH[7].x);
                serialized.SHData.GetArrayElementAtIndex(2).vector4Value = new Vector4(SH[3].y, SH[1].y, SH[2].y, SH[0].y - SH[6].y);
                serialized.SHData.GetArrayElementAtIndex(3).vector4Value = new Vector4(SH[4].y, SH[5].y, SH[6].y * 3.0f, SH[7].y);
                serialized.SHData.GetArrayElementAtIndex(4).vector4Value = new Vector4(SH[3].z, SH[1].z, SH[2].z, SH[0].z - SH[6].z);
                serialized.SHData.GetArrayElementAtIndex(5).vector4Value = new Vector4(SH[4].z, SH[5].z, SH[6].z * 3.0f, SH[7].z);
                serialized.SHData.GetArrayElementAtIndex(6).vector4Value = new Vector4(SH[8].x, SH[8].y, SH[8].z);
                serialized.ApplyModifiedProperties();
                
                buffer01.Release();
                buffer02.Release();
            });
        }
    }
}