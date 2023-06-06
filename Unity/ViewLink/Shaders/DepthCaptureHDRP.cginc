#ifndef DEPTH_CAPTURE_HDRP_INCLUDED
#define DEPTH_CAPTURE_HDRP_INCLUDED

#define REQUIRE_DEPTH_TEXTURE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

#include "DepthCaptureCommon.cginc"

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_Position;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
    return output;
}

float Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    const float2 uv = input.texcoord.xy;

    const float depth = SampleCameraDepth(uv.xy);

    const float4 encodedDepth = ImprovedEncodeFloatRGBA(1.0 - depth);

    return float4(GammaToLinearSpaceExact(encodedDepth.r), GammaToLinearSpaceExact(encodedDepth.g), GammaToLinearSpaceExact(encodedDepth.b), encodedDepth.a);
}

#endif
