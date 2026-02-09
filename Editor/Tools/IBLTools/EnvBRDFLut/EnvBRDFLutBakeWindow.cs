using System.IO;
using UnityEditor;
using UnityEngine;

namespace YPipeline.Editor
{
    public class EnvBRDFLutBakeWindow : EditorWindow
    {
        [MenuItem("YPipeline/Tools/Generate Environment BRDF Lut")]
        private static void ShowWindow()
        {
            EnvBRDFLutBakeWindow window = GetWindow<EnvBRDFLutBakeWindow>();
            window.titleContent = new GUIContent("Generate Environment BRDF Lut");
            window.minSize = new Vector2(360, 500);
        }
        
        private string m_CSPath = "Assets/YPipeline/Editor/Tools/IBLTools/EnvBRDFLut/EnvBRDFLut.compute";
        public ComputeShader envBRDFLutCs;
        public int envBRDFLutSize = 1024;
        public string savePath = "Assets";
        public string saveName = "EnvBRDFLut";

        public void OnGUI()
        {
            if (AssetDatabase.GetAssetPath(envBRDFLutCs) != m_CSPath)
            {
                envBRDFLutCs = AssetDatabase.LoadAssetAtPath<ComputeShader>(m_CSPath);
            }
            EditorGUILayout.LabelField("Bake Settings", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Compute Shader", envBRDFLutCs, typeof(ComputeShader), false);
            EditorGUILayout.IntField("Output Texture Size", envBRDFLutSize);
            EditorGUILayout.TextField("Save Path", savePath);
            EditorGUILayout.TextField("Save Name", saveName);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Bake", GUILayout.Height(32)))
            {
                BakeEnvBRDFLut();
            }
            EditorGUILayout.EndHorizontal();
            
            string filePath = Path.Combine(savePath, saveName) + ".exr";
            Texture lut = AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture)) as Texture;
            if (lut != null)
            {
                Rect rect = EditorGUILayout.GetControlRect(true, 256);
                EditorGUI.DrawPreviewTexture(rect, lut, null, ScaleMode.ScaleToFit);
                var style = EditorStyles.label;
                style.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField($"Saved At {filePath}", style);
                style.alignment = TextAnchor.MiddleLeft;
            }
        }

        private void BakeEnvBRDFLut()
        {
            // Render Texture
            RenderTexture rt = new RenderTexture(envBRDFLutSize, envBRDFLutSize, 0)
            {
                format =  RenderTextureFormat.ARGBHalf,
                enableRandomWrite = true,
            };
            rt.Create();
            
            // Dispatch
            int kernelIndex = envBRDFLutCs.FindKernel("GenerateEnvBRDFLut");
            envBRDFLutCs.SetTexture(kernelIndex, "_RWTexture", rt);
            envBRDFLutCs.SetInt("_LutSize", envBRDFLutSize);
            envBRDFLutCs.Dispatch(kernelIndex, envBRDFLutSize / 8, envBRDFLutSize / 8, 1);
            
            // GPU to CPU
            Texture2D tex = new Texture2D(envBRDFLutSize, envBRDFLutSize, TextureFormat.RGBAHalf, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, envBRDFLutSize, envBRDFLutSize), 0, 0);
            tex.Apply();
            
            // Save to EXR
            string filePath = Path.Combine(savePath, saveName) + ".exr";
            var bytes = ImageConversion.EncodeToEXR(tex, Texture2D.EXRFlags.CompressZIP);
            File.WriteAllBytes(filePath, bytes); 
            AssetDatabase.Refresh();
            
            // Clear Resources
            RenderTexture.active = null;
            rt.Release();
            DestroyImmediate(rt);
            DestroyImmediate(tex);
            
            // Texture Import Settings
            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = envBRDFLutSize;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }
    }
}