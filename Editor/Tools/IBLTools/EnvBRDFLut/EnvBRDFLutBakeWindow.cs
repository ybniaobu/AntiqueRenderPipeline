using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace YPipeline.Editor
{
    internal class EnvBRDFLutBakeWindow : EditorWindow
    {
        [MenuItem("YStudio/YPipeline/Tools/Bake Environment BRDF Lut")]
        private static void ShowWindow()
        {
            EnvBRDFLutBakeWindow window = GetWindow<EnvBRDFLutBakeWindow>();
            window.titleContent = new GUIContent("Bake Environment BRDF Lut");
            window.minSize = new Vector2(360, 480);
            window.maxSize = new Vector2(512, 512);
        }
        
        private string m_CSPath = "Packages/com.ystudio.render-pipeline.antique/Editor/Tools/IBLTools/EnvBRDFLut/EnvBRDFLut.compute";
        private ComputeShader m_EnvBRDFLutCs;
        private bool m_EnvBRDFLutCsInited;
        public int envBRDFLutSize = 1024;
        public string savePath = "Assets";
        public string saveName = "EnvBRDFLut";

        // UI Toolkit elements
        private HelpBox m_ErrorHelpBox;
        private IntegerField m_LutSizeField;
        private TextField m_SaveNameField;
        private TextField m_SavePathField;
        private Image m_PreviewContainer;
        private Label m_PreviewLabel;

        public void OnEnable()
        {
            m_EnvBRDFLutCsInited = true;
            if (AssetDatabase.GetAssetPath(m_EnvBRDFLutCs) != m_CSPath)
            {
                m_EnvBRDFLutCs = AssetDatabase.LoadAssetAtPath<ComputeShader>(m_CSPath);
                if (m_EnvBRDFLutCs == null)
                {
                    Debug.LogError($"Failed to find compute shader at {m_CSPath}, please verify the path in the code.");
                    m_EnvBRDFLutCsInited = false;
                }
            }
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            
            rootVisualElement.Add(new VisualElement() { style = { height = 8 } });
            
            m_ErrorHelpBox = new HelpBox($"Failed to find compute shader at {m_CSPath}, please verify the path in the code.", HelpBoxMessageType.Error);
            if (m_EnvBRDFLutCsInited) m_ErrorHelpBox.style.display = DisplayStyle.None;
            rootVisualElement.Add(m_ErrorHelpBox);
            
            m_LutSizeField = new IntegerField("Output Texture Size")
            {
                value = envBRDFLutSize,
            };
            m_LutSizeField.RegisterValueChangedCallback(evt => envBRDFLutSize = evt.newValue);
            rootVisualElement.Add(m_LutSizeField);
            
            m_SaveNameField = new TextField("Save Name")
            {
                value = saveName,
            };
            m_SaveNameField.RegisterValueChangedCallback(evt => saveName = evt.newValue);
            rootVisualElement.Add(m_SaveNameField);
            
            m_SavePathField = new TextField("Save Path")
            {
                value = savePath,
            };
            m_SavePathField.RegisterValueChangedCallback(evt => savePath = evt.newValue);
            rootVisualElement.Add(m_SavePathField);
            
            rootVisualElement.Add(new VisualElement() { style = { height = 8 } });
            
            var choosePathBtn = new Button(() =>
            {
                string path = EditorUtility.OpenFolderPanel("Choose Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    path = path.Replace(Application.dataPath, "Assets");
                    savePath = path;
                    m_SavePathField.value = path;
                }
            })
            {
                text = "Choose Save Path",
                style = { height = 24 }
            };
            rootVisualElement.Add(choosePathBtn);
            
            var bakeBtn = new Button(BakeEnvBRDFLut)
            {
                text = "Bake",
                style = { height = 24 }
            };
            rootVisualElement.Add(bakeBtn);
            
            m_PreviewContainer = new Image()
            {
                style = { marginTop = 8, width = 256, height = 256, alignSelf = Align.Center },
                scaleMode = ScaleMode.ScaleAndCrop
            };
            rootVisualElement.Add(m_PreviewContainer);
        
            m_PreviewLabel = new Label()
            {
                style = { unityTextAlign = TextAnchor.MiddleCenter },
            };
            rootVisualElement.Add(m_PreviewLabel);
        }
        
        public void Update()
        {
            string filePath = Path.Combine(savePath + "/", saveName) + ".exr";
            var lut = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            if (lut != null)
            {
                m_PreviewContainer.style.backgroundImage = new StyleBackground(lut);
                m_PreviewLabel.text = $"Saved At {filePath}";
            }
            else
            {
                m_PreviewContainer.style.backgroundImage = new StyleBackground();
                m_PreviewLabel.text = "";
            }
        }
        
        // public void OnGUI()
        // {
        //     EditorGUILayout.Space(8);
        //
        //     if (!m_EnvBRDFLutCsInited)
        //     {
        //         EditorGUILayout.HelpBox($"Failed to find compute shader at {m_CSPath}, please verify the path in the code.", MessageType.Error);
        //     }
        // 
        //     EditorGUILayout.IntField("Output Texture Size", envBRDFLutSize);
        //     EditorGUILayout.TextField("Save Name", saveName);
        //     EditorGUILayout.TextField("Save Path", savePath);
        //     EditorGUILayout.Space(8);
        //     
        //     if (GUILayout.Button("Choose Save Path", GUILayout.Height(24)))
        //     {
        //         string path = EditorUtility.OpenFolderPanel("Choose Folder", "Assets", "");
        //         if (!string.IsNullOrEmpty(path))
        //         {
        //             path = path.Replace(Application.dataPath, "Assets");
        //             savePath = path;
        //         }
        //     }
        //     
        //     if (GUILayout.Button("Bake", GUILayout.Height(24)))
        //     {
        //         BakeEnvBRDFLut();
        //     }
        //     
        //     string filePath = Path.Combine(savePath + "/", saveName) + ".exr";
        //     Texture lut = AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture)) as Texture;
        //     if (lut != null)
        //     {
        //         EditorGUILayout.Space(8);
        //         Rect rect = EditorGUILayout.GetControlRect(true, 256);
        //         EditorGUI.DrawPreviewTexture(rect, lut, null, ScaleMode.ScaleToFit);
        //         var style = EditorStyles.label;
        //         style.alignment = TextAnchor.MiddleCenter;
        //         EditorGUILayout.LabelField($"Saved At {filePath}", style);
        //         style.alignment = TextAnchor.MiddleLeft;
        //     }
        // }

        private void BakeEnvBRDFLut()
        {
            // Render Texture
            RenderTexture rt = new RenderTexture(envBRDFLutSize, envBRDFLutSize, 0)
            {
                format = RenderTextureFormat.ARGBHalf,
                enableRandomWrite = true,
            };
            rt.Create();
            
            // Dispatch
            int kernelIndex = m_EnvBRDFLutCs.FindKernel("GenerateEnvBRDFLut");
            m_EnvBRDFLutCs.SetTexture(kernelIndex, "_RWTexture", rt);
            m_EnvBRDFLutCs.SetInt("_LutSize", envBRDFLutSize);
            m_EnvBRDFLutCs.Dispatch(kernelIndex, envBRDFLutSize / 8, envBRDFLutSize / 8, 1);
            
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