
#include "CommonFunctions.cginc"

half4 CustomLightingFrag (v2f_surf i, SurfaceDataCustom surf, uint facing)
{
    #if defined(LOD_FADE_CROSSFADE)
		UnityApplyDitherCrossFade(i.pos);
	#endif

#if defined(UNITY_PASS_SHADOWCASTER)

    #if defined(_MODE_CUTOUT)
        if (surf.alpha < _Cutoff) discard;
    #endif

    #ifdef _ALPHAPREMULTIPLY_ON
        surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
    #endif

    #if defined(_ALPHAPREMULTIPLY_ON) || defined(_MODE_FADE)
        half dither = Unity_Dither(surf.alpha, i.pos.xy);
        if (dither < 0.0) discard;
    #endif

    SHADOW_CASTER_FRAGMENT(i);
#else

    #if defined(_MODE_CUTOUT)
        AACutout(surf.alpha, _Cutoff);
    #endif

    float3 worldNormal = surf.tangentNormal;
    float3 bitangent = float3(i.tSpace0.y,i.tSpace1.y,i.tSpace2.y);
    float3 tangent = float3(i.tSpace0.x,i.tSpace1.x,i.tSpace2.x);;

    FlipBTN(facing, worldNormal, bitangent, tangent);

    half3 indirectSpecular = 0.0;
    half3 directSpecular = 0.0;
    half3 otherSpecular = 0.0;

    half roughness = surf.perceptualRoughness * surf.perceptualRoughness;
    half clampedRoughness = max(roughness, 0.002);

    float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
    half NoV = NormalDotViewDir(worldNormal, viewDir);

    half3 f0 = GetF0(surf.reflectance, surf.metallic, surf.albedo.rgb);
    DFGLut = SampleDFG(NoV, surf.perceptualRoughness).rg;
    DFGEnergyCompensation = EnvBRDFEnergyCompensation(DFGLut, f0);

    LightDataCustom lightData;
    InitializeLightData(lightData, worldNormal, viewDir, NoV, clampedRoughness, surf.perceptualRoughness, f0, i);

    #if !defined(SPECULAR_HIGHLIGHTS_OFF) && defined(USING_LIGHT_MULTI_COMPILE)
        directSpecular += lightData.Specular;
    #endif

    #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
//        lightData.FinalColor += i.vertexLight;
    #endif

    #if defined(VERTEXLIGHT_PS) && defined(VERTEXLIGHT_ON)
        NonImportantLightsPerPixel(lightData.FinalColor, directSpecular, i.worldPos, worldNormal, viewDir, NoV, f0, clampedRoughness);
    #endif

    

half3 indirectDiffuse = 0;
#ifdef UNITY_PASS_FORWARDBASE
    #if defined(LIGHTMAP_ON)

    float2 lightmapUV = i.lmap.xy;
    half4 bakedColorTex = SampleBicubic(unity_Lightmap, custom_bilinear_clamp_sampler, lightmapUV);
    half3 lightMap = DecodeLightmap(bakedColorTex);


    #ifdef BAKERY_SH
        BakerySHLightmapAndSpecular(lightMap, lightmapUV, otherSpecular, worldNormal, viewDir, roughness, f0);
    #endif

    #if defined(DIRLIGHTMAP_COMBINED)
        float4 lightMapDirection = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, lightmapUV);
        lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, worldNormal);
    #endif

    indirectDiffuse = lightMap;
#endif

#if defined(DYNAMICLIGHTMAP_ON)
    float3 realtimeLightMap = getRealtimeLightmap(i.lmap.zw, worldNormal);
    indirectDiffuse += realtimeLightMap; 
#endif

#if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
    lightData.FinalColor = 0.0;
    lightData.Specular = 0.0;
    indirectDiffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap (indirectDiffuse, lightData.Attenuation, bakedColorTex, worldNormal);
#endif


    #if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)
        #ifdef LIGHTPROBE_VERTEX
            indirectDiffuse = ShadeSHPerPixel(worldNormal, i.lightProbe, i.worldPos.xyz);
        #else
            indirectDiffuse = GetLightProbes(worldNormal, i.worldPos.xyz);
        #endif
    #endif

    indirectDiffuse = max(0.0, indirectDiffuse);
#endif



    // #if defined(BAKEDSPECULAR) && defined(UNITY_PASS_FORWARDBASE) && !defined(BAKERYLM_ENABLED)
    // {
    //     float3 bakedDominantDirection = 1.0;
    //     half3 bakedSpecularColor = 0.0;

    //     #if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON)
    //         bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
    //         bakedSpecularColor = indirectDiffuse;
    //     #endif

    //     #ifndef LIGHTMAP_ANY
    //         bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
    //         bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
    //     #endif

    //     bakedDominantDirection = normalize(bakedDominantDirection);
    //     directSpecular += GetSpecularHighlights(worldNormal, bakedSpecularColor, bakedDominantDirection, f0, viewDir, clampedRoughness, NoV, DFGEnergyCompensation);
    // }
    // #endif

#ifdef UNITY_PASS_FORWARDBASE
    #if !defined(_GLOSSYREFLECTIONS_OFF)
        indirectSpecular += GetReflections(worldNormal, i.worldPos.xyz, viewDir, f0, roughness, NoV, surf, indirectDiffuse);
    #endif
#endif

    #if defined(_ALPHAPREMULTIPLY_ON)
        surf.albedo.rgb *= surf.alpha;
        surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
    #endif

    #if defined(_ALPHAMODULATE_ON)
        surf.albedo.rgb = lerp(1.0, surf.albedo.rgb, surf.alpha);
    #endif

    otherSpecular *= EnvBRDFMultiscatter(DFGLut, f0) * DFGEnergyCompensation;
    
    half4 finalColor = half4(surf.albedo.rgb * (1.0 - surf.metallic) * (indirectDiffuse * surf.occlusion + (lightData.FinalColor)) + indirectSpecular + (directSpecular * UNITY_PI) + otherSpecular + surf.emission, surf.alpha);

    #ifdef UNITY_PASS_META
        UnityMetaInput metaInput;
        UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaInput);
        metaInput.Emission = surf.emission;
        metaInput.Albedo = surf.albedo.rgb;
        return float4(UnityMetaFragment(metaInput).rgb, surf.alpha);
    #endif

    
    UNITY_APPLY_FOG(i.fogCoord, finalColor);
    
    return finalColor;
#endif
}