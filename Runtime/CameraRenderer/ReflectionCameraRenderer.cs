using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class ReflectionCameraRenderer : CameraRenderer
    {
        protected override void Initialize(ref YPipelineData data)
        {
            // TODO：反射探针不能用 depth prepass 渲染，效果不好 ！！！！！！！！！！！！！！
            
        }
        
        public override void Dispose()
        {
            base.Dispose();
        }
        
        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
        }
    }
}