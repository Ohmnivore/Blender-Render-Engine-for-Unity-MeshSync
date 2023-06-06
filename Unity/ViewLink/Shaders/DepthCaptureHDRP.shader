Shader "Hidden/BlenderBridge/DepthCaptureHDRP"
{
    Properties
    {
    }

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.high-definition" : "14.0.5"
        }

        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
        }

        Pass
        {
            Name "Depth Capture"

            Cull Off
            ZTest Always
            ZWrite Off
            Blend Off

            HLSLPROGRAM
            #pragma fragment Frag
            #pragma vertex Vert
            #include "DepthCaptureHDRP.cginc"
            ENDHLSL
        }
    }
    Fallback Off
}
