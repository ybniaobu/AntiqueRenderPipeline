using UnityEditor;
using UnityEngine;

namespace YPipeline.Editor
{
    public static class YPipelineMaterialProperties
    {
        public static readonly string k_BaseTex = "_BaseTex";
        public static readonly string k_BaseColor = "_BaseColor";
        public static readonly string k_EmissionTex = "_EmissionTex";
        public static readonly string k_EmissionColor = "_EmissionColor";
        
        public static readonly string k_AlphaClipping = "_Clipping";
        public static readonly string k_AlphaCutoff = "_Cutoff";
        
        public static readonly string k_AddPrecomputedVelocity = "_AddPrecomputedVelocity";
    }
}