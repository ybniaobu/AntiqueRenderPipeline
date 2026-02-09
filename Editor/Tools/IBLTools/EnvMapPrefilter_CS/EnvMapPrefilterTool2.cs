// using UnityEngine;
// using UnityEditor;
// using Sirenix.OdinInspector;
// using Sirenix.OdinInspector.Editor;
//
// public class EnvMapPrefilterTool2 : OdinEditorWindow
// {
//     [MenuItem("Tools/IBL Tools/Prefilter Environment Map(Compute Shader)")]
//     private static void ShowWindow()
//     {
//         EnvMapPrefilterTool2 window = OdinEditorWindow.GetWindow<EnvMapPrefilterTool2>();
//         window.Show();
//     }
//     
//     [InfoBox("这个工具预过滤时生成的 Cubemap 在高粗糙度下可能会有边缘接缝衔接问题，特别是在 Mipmap 分辨率较小的时候，若设定的 Mipmap 数量比较大，建议拖拽进去更大的 Cubemap。")]
//     [InfoBox("请将拖拽进去的 Cubemap 的 Compress 设置为 None，Filter Mode 设置为 Trilinear，Read/Write 打开。")]
//     [Title("Environment Cubemap Settings")]
//     public Cubemap envMap;
//     [Range(0, 360)] public float rotation = 0.0f;
//     [Range(0.0f, 8.0f)] public float exposure = 0.5f;
//     public int sampleNumber = 2048;
//     
//     [Title("Save Settings")]
//     public int sizePerFace = 2048;
//     public int mipCount = 7;
//     [FolderPath] public string savePath = "Assets";
//     public string saveName = "PrefilteredCubemap";
//     public bool BC6HCompress = true;
//     
//     [Title("Compute Shader Settings")] 
//     [Sirenix.OdinInspector.FilePath] public string csPath = "Assets/YPipeline/Editor/IBLUtilities/EnvMapPrefilter_CS/EnvMapPrefilter.compute";
//     [ReadOnly] public ComputeShader envMapPrefilterCs;
//     [ReadOnly] public string csKernelName = "PrefilterEnvMap";
//     
//     [Title("Result Preview")]
//     [ReadOnly] [InlineEditor(InlineEditorModes.LargePreview)] public Cubemap prefilteredEnvMap;
//
//     [InfoBox("This tool prefilters the environment map with GGX distribution.")]
//     [Button(ButtonSizes.Large), GUIColor(1, 1, 1)]
//     private void PrefilterEnvMap()
//     {
//         envMapPrefilterCs = AssetDatabase.LoadAssetAtPath<ComputeShader>(csPath);
//         int kernelIndex = envMapPrefilterCs.FindKernel(csKernelName);
//         
//         prefilteredEnvMap = new Cubemap(sizePerFace, TextureFormat.RGBAFloat, mipCount);
//
//         for (int i = 0; i < mipCount; i++)
//         {
//             for (int j = 0; j < 6; j++)
//             {
//                 ComputeBuffer buffer = new ComputeBuffer(sizePerFace / (int) Mathf.Pow(2, i) * sizePerFace / (int) Mathf.Pow(2, i), sizeof(float) * 4);
//                 var data = new float[sizePerFace / (int) Mathf.Pow(2, i) * sizePerFace / (int) Mathf.Pow(2, i) * 4];
//                 
//                 envMapPrefilterCs.SetBuffer(kernelIndex, "_Result", buffer);
//                 envMapPrefilterCs.SetTexture(kernelIndex, "_EnvMap", envMap);
//                 envMapPrefilterCs.SetFloat("_Rotation", rotation);
//                 envMapPrefilterCs.SetFloat("_Exposure", exposure);
//                 envMapPrefilterCs.SetFloat("_Roughness", 1.0f / (mipCount - 1) * i);
//                 envMapPrefilterCs.SetInt("_SizePerFace", sizePerFace);
//                 envMapPrefilterCs.SetInt("_Face", j);
//                 envMapPrefilterCs.SetInt("_SampleNumber", sampleNumber);
//                 envMapPrefilterCs.SetInt("_MipMapLevel", i);
//                 
//                 int size = sizePerFace / (int) Mathf.Pow(2, i);
//                 envMapPrefilterCs.Dispatch(kernelIndex, size / 8, size / 8, 1);
//                 
//                 buffer.GetData(data);
//                 buffer.Release();
//                 prefilteredEnvMap.SetPixelData(data, i, (CubemapFace)j);
//             }
//         }
//         
//         prefilteredEnvMap.Apply(false);
//         if(BC6HCompress) EditorUtility.CompressCubemapTexture(prefilteredEnvMap, TextureFormat.BC6H, TextureCompressionQuality.Normal);
//         AssetDatabase.CreateAsset(prefilteredEnvMap, savePath + "/" + saveName + ".asset");
//     }
// }
