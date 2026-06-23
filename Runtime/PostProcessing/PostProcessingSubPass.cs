using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    internal abstract class PostProcessingSubPass
    {
        public bool isActivated = true;
        
        public static T Create<T>(ref YPipelineData data) where T : PostProcessingSubPass, new()
        {
            T subPass = new T();
            subPass.Initialize(ref data);
            return subPass;
        }

        protected abstract void Initialize(ref YPipelineData data);

        public abstract void OnDispose();

        public abstract void OnRecord(ref YPipelineData data);
    }
}