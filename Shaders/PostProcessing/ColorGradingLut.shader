Shader "Hidden/YPipeline/ColorGradingLut"
{
    HLSLINCLUDE
    #include "ColorGradingLutPass.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }
        
        ZTest Always
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "Color Grading - None" // 0
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingNoneFrag
            ENDHLSL
        }

        Pass
        {
            Name "Color Grading - Reinhard Simple" // 1
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingReinhardSimpleFrag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading - Reinhard Extended" // 2
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingReinhardExtendedFrag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading - Reinhard Luminance" // 3
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingReinhardLuminanceFrag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading - Uncharted2 Filmic" // 4
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingUncharted2FilmicFrag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading - Khronos PBR Neutral" // 5
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingKhronosPBRNeutralFrag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading - ACES Full" // 6
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingACESFullFrag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading - ACES Stephen Hill Fit" // 7
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingACESStephenHillFitFrag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading - ACES Approximation Fit" // 8
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingACESApproxFitFrag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading - AgX Approximation Default" // 9
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingAgXDefaultFrag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading - AgX Approximation Golden" // 10
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingAgXGoldenFrag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading - AgX Approximation Punchy" // 11
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingAgXPunchyFrag
            ENDHLSL
        }
    }
}