using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class LightingDebugSettings
    {
        private const string k_PanelName = "Lighting";
        public static readonly int k_TilesDebugOpacityID = Shader.PropertyToID("_TilesDebugOpacity");
        public static readonly int k_TilesDebugParamsID =  Shader.PropertyToID("_TilesDebugParams");
        
        public bool showLightTiles;
        public bool showReflectionProbeTiles;
        public bool ShowTiles => showLightTiles || showReflectionProbeTiles;

        public bool showZeroTiles;
        public float tileOpacity = 0.5f;
        
        public Material lightCullingDebugMaterial;
        
        public void Initialize(YPipelineDebugResources debugResources)
        {
            lightCullingDebugMaterial = CoreUtils.CreateEngineMaterial(debugResources.lightCullingDebugShader);
            
            DebugManager.instance.GetPanel(k_PanelName, true, 0).children.Add(
                new DebugUI.RuntimeDebugShadersMessageBox(),
                new DebugUI.Foldout
                {
                    displayName = "Light Culling",
                    opened = false,
                    children =
                    {
                        new DebugUI.BoolField
                        {
                            displayName = "Show Light Tiles",
                            tooltip = "Whether the light tiles overlay is shown.",
                            getter = () => showLightTiles,
                            setter = value => showLightTiles = value
                        },
                        new DebugUI.BoolField
                        {
                            displayName = "Show Reflection Probe Tiles",
                            tooltip = "Whether the reflection probe tiles are shown.",
                            getter = () => showReflectionProbeTiles,
                            setter = value => showReflectionProbeTiles = value
                        },
                        new DebugUI.BoolField
                        {
                            displayName = "Show Zero Tiles",
                            tooltip = "Whether the zero count tiles are shown.",
                            getter = () => showZeroTiles,
                            setter = value => showZeroTiles = value
                        },
                        new DebugUI.FloatField
                        {
                            displayName = "Tile Opacity",
                            tooltip = "Opacity of the overlay.",
                            min = () => 0f,
                            max = () => 1f,
                            getter = () => tileOpacity,
                            setter = value => tileOpacity = value
                        },
                    }
                }
            );
            
            
        }
        
        public void OnDispose()
        {
            CoreUtils.Destroy(lightCullingDebugMaterial);
            DebugManager.instance.RemovePanel(k_PanelName);
        }
    }
#endif
}