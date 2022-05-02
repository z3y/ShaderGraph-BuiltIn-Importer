#ifndef ADDITIONALFUNCTIONS_INCLUDED
#define ADDITIONALFUNCTIONS_INCLUDED


half3 GetF0(half metallic, half3 albedo)
{
    half reflectance = 0.5;
    return 0.16 * reflectance * reflectance * (1.0 - metallic) + albedo * metallic;
}

float3 getBoxProjection (float3 direction, float3 position, float4 cubemapPosition, float3 boxMin, float3 boxMax)
{
    #if defined(UNITY_SPECCUBE_BOX_PROJECTION) || defined(FORCE_SPECCUBE_BOX_PROJECTION)
        UNITY_FLATTEN
        if (cubemapPosition.w > 0.0)
        {
            float3 factors = ((direction > 0.0 ? boxMax : boxMin) - position) / direction;
            float scalar = min(min(factors.x, factors.y), factors.z);
            direction = direction * scalar + (position - cubemapPosition.xyz);
        }
    #endif

    return direction;
}

half computeSpecularAO(half NoV, half ao, half roughness)
{
    return clamp(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao, 0.0, 1.0);
}

Texture2D _DFG;
SamplerState sampler_DFG;

half4 SampleDFG(half NoV, half perceptualRoughness)
{
    return _DFG.Sample(sampler_DFG, float3(NoV, perceptualRoughness, 0));
}
half3 EnvBRDF(half2 dfg, half3 f0)
{
    return f0 * dfg.x + dfg.y;
}
half3 EnvBRDFMultiscatter(half2 dfg, half3 f0)
{
    return lerp(dfg.xxx, dfg.yyy, f0);
}
half3 EnvBRDFEnergyCompensation(half2 dfg, half3 f0)
{
    return 1.0 + f0 * (1.0 / dfg.y - 1.0);
}

half3 EnvBRDFApprox(half perceptualRoughness, half NoV, half3 f0)
{
    // original code from https://blog.selfshadow.com/publications/s2013-shading-course/lazarov/s2013_pbs_black_ops_2_notes.pdf
    half g = 1 - perceptualRoughness;
    half4 t = half4(1 / 0.96, 0.475, (0.0275 - 0.25 * 0.04) / 0.96, 0.25);
    t *= half4(g, g, g, g);
    t += half4(0.0, 0.0, (0.015 - 0.75 * 0.04) / 0.96, 0.75);
    half a0 = t.x * min(t.y, exp2(-9.28 * NoV)) + t.z;
    half a1 = t.w;
    return saturate(lerp(a0, a1, f0));
}

half3 GetReflections(float3 normalWS, float3 positionWS, float3 viewDir, half3 f0, half roughness, half NoV, half3 indirectDiffuse, half occlusion)
{
    half3 indirectSpecular = 0;
    #if defined(UNITY_PASS_FORWARDBASE)

        float3 reflDir = reflect(-viewDir, normalWS);
        #ifndef SHADER_API_MOBILE
        reflDir = lerp(reflDir, normalWS, roughness * roughness);
        #endif
        
        half perceptualRoughness = sqrt(roughness);
        Unity_GlossyEnvironmentData envData;
        envData.roughness = perceptualRoughness;
        envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);

        half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
        indirectSpecular = probe0;

        #if defined(UNITY_SPECCUBE_BLENDING) && !defined(SHADER_API_MOBILE)
            UNITY_BRANCH
            if (unity_SpecCube0_BoxMin.w < 0.99999)
            {
                envData.reflUVW = getBoxProjection(reflDir, positionWS, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin.xyz, unity_SpecCube1_BoxMax.xyz);
                float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
                indirectSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
            }
        #endif

        float horizon = min(1.0 + dot(reflDir, normalWS), 1.0);
        indirectSpecular *= horizon * horizon;
        
        #ifdef USE_SPECULAR_OCCLUSION
            half lightmapOcclusion = lerp(1.0, saturate(dot(indirectDiffuse, 1.0)), _SpecularOcclusion);

            #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
                occlusion *= lightmapOcclusion;
            #endif
        #endif


        indirectSpecular *= computeSpecularAO(NoV, occlusion, perceptualRoughness * perceptualRoughness);

    #endif

    return indirectSpecular;
}


#endif