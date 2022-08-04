#ifndef BAKERY_INCLUDED
#define BAKERY_INCLUDED

Texture2D _RNM0, _RNM1, _RNM2;
float4 _RNM0_TexelSize;

SamplerState bakery_bilinear_clamp_sampler;

#if !defined(SHADER_API_MOBILE)
    #define BAKERY_SHNONLINEAR
#endif

#ifdef BAKERY_SHNONLINEAR_OFF
    #undef BAKERY_SHNONLINEAR
#endif

float shEvaluateDiffuseL1Geomerics(float L0, float3 L1, float3 n)
{
    // average energy
    float R0 = L0;
    
    // avg direction of incoming light
    float3 R1 = 0.5f * L1;
    
    // directional brightness
    float lenR1 = length(R1);
    
    // linear angle between normal and direction 0-1
    //float q = 0.5f * (1.0f + dot(R1 / lenR1, n));
    //float q = dot(R1 / lenR1, n) * 0.5 + 0.5;
    float q = dot(normalize(R1), n) * 0.5 + 0.5;
    q = saturate(q); // Thanks to ScruffyRuffles for the bug identity.
    
    // power for q
    // lerps from 1 (linear) to 3 (cubic) based on directionality
    float p = 1.0f + 2.0f * lenR1 / R0;
    
    // dynamic range constant
    // should vary between 4 (highly directional) and 0 (ambient)
    float a = (1.0f - lenR1 / R0) / (1.0f + lenR1 / R0);
    
    return R0 * (a + (1.0f - a) * (p + 1.0f) * pow(q, p));
}


void BakerySHLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalWS, float3 viewDir, half roughness)
{
    #ifdef BAKERY_SH

        half3 L0 = lightMap;
        float3 nL1x = _RNM0.Sample(bakery_bilinear_clamp_sampler, lightmapUV).rgb * 2.0 - 1.0;
        float3 nL1y = _RNM1.Sample(bakery_bilinear_clamp_sampler, lightmapUV).rgb * 2.0 - 1.0;
        float3 nL1z = _RNM2.Sample(bakery_bilinear_clamp_sampler, lightmapUV).rgb * 2.0 - 1.0;
        float3 L1x = nL1x * L0 * 2.0;
        float3 L1y = nL1y * L0 * 2.0;
        float3 L1z = nL1z * L0 * 2.0;

        #ifdef BAKERY_SHNONLINEAR
            float lumaL0 = dot(L0, float(1));
            float lumaL1x = dot(L1x, float(1));
            float lumaL1y = dot(L1y, float(1));
            float lumaL1z = dot(L1z, float(1));
            float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), normalWS);

            lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
            float regularLumaSH = dot(lightMap, 1.0);
            lightMap *= lerp(1.0, lumaSH / regularLumaSH, saturate(regularLumaSH * 16.0));
        #else
            lightMap = L0 + normalWS.x * L1x + normalWS.y * L1y + normalWS.z * L1z;
        #endif

        #ifdef _LIGHTMAPPED_SPECULAR
            float3 grayScaleVec = float3(0.2125, 0.7154, 0.0721);
            float3 dominantDir = float3(dot(nL1x, grayScaleVec), dot(nL1y, grayScaleVec), dot(nL1z, grayScaleVec));
            float3 halfDir = Unity_SafeNormalize(normalize(dominantDir) + viewDir);
            half NoH = saturate(dot(normalWS, halfDir));
            half spec = D_GGX(NoH, roughness);
            half3 sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
            dominantDir = normalize(dominantDir);

            directSpecular += max(spec * sh, 0.0);
        #endif
        
    #endif
}


#endif