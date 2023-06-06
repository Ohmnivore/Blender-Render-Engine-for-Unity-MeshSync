Shader "Hidden/BlenderBridge/DepthCaptureURP"
{
    Properties
    {
    }

    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal" : "14.0.5"
        }

        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"
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
            #include "DepthCaptureURP.cginc"
            ENDHLSL
        }
    }
    Fallback Off
}
