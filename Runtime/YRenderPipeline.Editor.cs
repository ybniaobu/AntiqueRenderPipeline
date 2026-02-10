using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;

namespace YPipeline
{
    public partial class YRenderPipeline
    {
#if UNITY_EDITOR
        // ----------------------------------------------------------------------------------------------------
        // Supported Rendering Features
        // ----------------------------------------------------------------------------------------------------

        private void SetSupportedRenderingFeatures()
        {
            // SupportedRenderingFeatures.active.rendersUIOverlay = true;
            SupportedRenderingFeatures.active.overridesRealtimeReflectionProbes = true;
            SupportedRenderingFeatures.active.overridesShadowmask = true;
            
            SupportedRenderingFeatures.active.reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.Rotation;

            SupportedRenderingFeatures.active.enlighten = false;
            SupportedRenderingFeatures.active.mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly;
            SupportedRenderingFeatures.active.defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly;
            SupportedRenderingFeatures.active.lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime;
            SupportedRenderingFeatures.active.lightmapsModes = LightmapsMode.NonDirectional;
            SupportedRenderingFeatures.active.overridesFog = true;
            SupportedRenderingFeatures.active.overridesOtherLightingSettings = true;
            
            SupportedRenderingFeatures.active.receiveShadows = false;
            SupportedRenderingFeatures.active.rendererProbes = false;
            SupportedRenderingFeatures.active.lightProbeProxyVolumes = false;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Lightmap
        // ----------------------------------------------------------------------------------------------------

        private static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] lights, NativeArray<LightDataGI> output) => 
        {
            LightDataGI lightData = new LightDataGI();

            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                switch (light.type)
                {
                    case LightType.Directional:
                        DirectionalLight directionalLight = new DirectionalLight();
                        LightmapperUtils.Extract(light, ref directionalLight);
                        directionalLight.color.intensity /= Mathf.PI;
                        directionalLight.indirectColor.intensity /= Mathf.PI;
                        lightData.Init(ref directionalLight);
                        break;
                    case LightType.Point:
                        PointLight pointLight = new PointLight();
                        LightmapperUtils.Extract(light, ref pointLight);
                        pointLight.color.intensity /= Mathf.PI;
                        pointLight.indirectColor.intensity /= Mathf.PI;
                        lightData.Init(ref pointLight);
                        break;
                    case LightType.Spot:
                        SpotLight spotLight = new SpotLight();
                        LightmapperUtils.Extract(light, ref spotLight);
                        spotLight.color.intensity /= Mathf.PI;
                        spotLight.indirectColor.intensity /= Mathf.PI;
                        spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                        spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                        lightData.Init(ref spotLight);
                        break;
                    case LightType.Rectangle:
                        RectangleLight rectangleLight = new RectangleLight();
                        LightmapperUtils.Extract(light, ref rectangleLight);
                        rectangleLight.color.intensity /= Mathf.PI;
                        rectangleLight.indirectColor.intensity /= Mathf.PI;
                        rectangleLight.mode = LightMode.Baked;
                        lightData.Init(ref rectangleLight);
                        break;
                    default:
                        lightData.InitNoBake(light.GetEntityId());
                        break;
                }
                
                lightData.falloff = FalloffType.InverseSquared;
                output[i] = lightData;
            }
        };

        private void SetLightmapper()
        {
            Lightmapping.SetDelegate(lightsDelegate);
        }
#endif
    }
}