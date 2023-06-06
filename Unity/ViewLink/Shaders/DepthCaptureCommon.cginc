#ifndef DEPTH_CAPTURE_COMMON_INCLUDED
#define DEPTH_CAPTURE_COMMON_INCLUDED

// From https://stackoverflow.com/a/48138528
// This is an improved version of EncodeFloatRGBA from UnityCG.cginc
inline float4 ImprovedEncodeFloatRGBA(float v)
{
    uint vi = (uint)(v * (256.0f * 256.0f * 256.0f * 256.0f));
    int ex = (int)(vi / (256 * 256 * 256) % 256);
    int ey = (int)((vi / (256 * 256)) % 256);
    int ez = (int)((vi / (256)) % 256);
    int ew = (int)(vi % 256);
    float4 e = float4(ex / 255.0f, ey / 255.0f, ez / 255.0f, ew / 255.0f);
    return e;
}

// From UnityCG.cginc
inline float GammaToLinearSpaceExact(float value)
{
    if (value <= 0.04045F)
        return value / 12.92F;
    else if (value < 1.0F)
        return pow((value + 0.055F)/1.055F, 2.4F);
    else
        return pow(value, 2.2F);
}

#endif
