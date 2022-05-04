#ifndef BAKERY_INCLUDED
#define BAKERY_INCLUDED

Texture2D _RNM0, _RNM1, _RNM2;
float4 _RNM0_TexelSize;

#if defined(SHADER_API_MOBILE)
    #define BAKERY_SHNONLINEAR_OFF
#endif

void BakerySHLightmapAndSpecular(inout half3 lightMap, float2 lightmapUV, inout half3 directSpecular, float3 normalWS, float3 viewDir, half roughness, half3 f0)
{
    #ifdef BAKERY_SH

        half3 L0 = lightMap;
        float3 nL1x = _RNM0.Sample(custom_bilinear_clamp_sampler, lightmapUV) * 2.0 - 1.0;
        float3 nL1y = _RNM1.Sample(custom_bilinear_clamp_sampler, lightmapUV) * 2.0 - 1.0;
        float3 nL1z = _RNM2.Sample(custom_bilinear_clamp_sampler, lightmapUV) * 2.0 - 1.0;
        float3 L1x = nL1x * L0 * 2.0;
        float3 L1y = nL1y * L0 * 2.0;
        float3 L1z = nL1z * L0 * 2.0;

        #ifndef BAKERY_SHNONLINEAR_OFF
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

        #ifdef LIGHTMAPPED_SPECULAR
            float3 dominantDir = float3(dot(nL1x, GRAYSCALE), dot(nL1y, GRAYSCALE), dot(nL1z, GRAYSCALE));
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