#ifndef CUSTOMSHADING_CORE_INCLUDED
#define CUSTOMSHADING_CORE_INCLUDED

#define VERTEXLIGHT_PS

#ifdef SHADER_API_MOBILE
    #undef VERTEXLIGHT_PS
    #undef _BICUBICLIGHTMAP
    #undef BAKERY_PROBESHNONLINEAR
    #undef LTCGI
#endif

#ifndef LIGHTMAP_ON
    #undef BAKERY_SH
#endif

#include "CommonFunctions.cginc"
#include "NonImportantLights.cginc"

#ifndef OVERRIDE_SHADING
#define OVERRIDE_SHADING 1;
#endif

#ifndef OVERRIDE_FINALCOLOR
#define OVERRIDE_FINALCOLOR 1;
#endif

half4 CustomLightingFrag (v2f_surf i, SurfaceDataCustom surf)
{
    #if defined(LOD_FADE_CROSSFADE)
		UnityApplyDitherCrossFade(i.pos);
	#endif

#if defined(UNITY_PASS_SHADOWCASTER)

    #if defined(_BUILTIN_AlphaClip)
        if (surf.alpha < surf.alphaClipThreshold) discard;
    #endif

    #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
    #endif

    #if defined(_BUILTIN_SURFACE_TYPE_TRANSPARENT)
        half dither = Unity_Dither(surf.alpha, i.pos.xy);
        if (dither < 0.0) discard;
    #endif

    SHADOW_CASTER_FRAGMENT(i);
#else

    #if defined(_BUILTIN_AlphaClip)
        #if defined(PREDEFINED_A2C) && !defined(UNITY_PASS_META)
            AACutout(surf.alpha, surf.alphaClipThreshold);
        #else
            if (surf.alpha < surf.alphaClipThreshold) discard;
        #endif
    #endif

    float3 worldNormal = surf.tangentNormal;

    half3 indirectSpecular = 0.0;
    half3 directSpecular = 0.0;

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

    #if defined(VERTEXLIGHT_ON) && !defined(LIGHTMAP_ON) && !defined(VERTEXLIGHT_PS)
        lightData.FinalColor += i.sh;
    #endif

    #if defined(VERTEXLIGHT_PS) && defined(VERTEXLIGHT_ON) && !defined(LIGHTMAP_ON)
        NonImportantLightsPerPixel(lightData.FinalColor, directSpecular, i.worldPos, worldNormal, viewDir, NoV, f0, clampedRoughness);
    #endif

    

half3 indirectDiffuse = 0;
#ifdef UNITY_PASS_FORWARDBASE
    #if defined(LIGHTMAP_ON)

    float2 lightmapUV = i.lmap.xy;
    half4 bakedColorTex = SampleBicubic(unity_Lightmap, custom_bilinear_clamp_sampler, lightmapUV);
    half3 lightMap = DecodeLightmap(bakedColorTex);


    #ifdef BAKERY_SH
        BakerySHLightmapAndSpecular(lightMap, lightmapUV, indirectSpecular, worldNormal, viewDir, roughness, f0);
    #endif

    #if defined(DIRLIGHTMAP_COMBINED)
        float4 lightMapDirection = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, lightmapUV);
        #ifndef BAKERY_MONOSH
            lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, worldNormal);
        #endif
    #endif

    #if defined(BAKERY_MONOSH)
        BakeryMonoSH(lightMap, indirectSpecular, lightmapUV, worldNormal, viewDir, roughness);
    #endif

    indirectDiffuse = lightMap;
#endif

#if defined(DYNAMICLIGHTMAP_ON)
    float3 realtimeLightMap = getRealtimeLightmap(i.lmap.zw, worldNormal);
    indirectDiffuse += realtimeLightMap; 
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

#if defined(LIGHTMAP_ON)
#if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
    lightData.FinalColor = 0.0;
    lightData.Specular = 0.0;
    directSpecular = 0.0;
    indirectDiffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap (indirectDiffuse, lightData.Attenuation, bakedColorTex, worldNormal);
#endif
#endif

    #if defined(_LIGHTMAPPED_SPECULAR) && defined(UNITY_PASS_FORWARDBASE) && !defined(BAKERY_SH) && !defined(BAKERY_MONOSH)
    {
        float3 bakedDominantDirection = 1.0;
        half3 bakedSpecularColor = 0.0;

        #if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON)
            bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
            bakedSpecularColor = indirectDiffuse;
        #endif

        #ifndef LIGHTMAP_ON
            bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
            bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
        #endif

        bakedDominantDirection = normalize(bakedDominantDirection);
        directSpecular += GetSpecularHighlights(worldNormal, bakedSpecularColor, bakedDominantDirection, f0, viewDir, clampedRoughness, NoV, DFGEnergyCompensation) * UNITY_PI;
    }
    #endif

#ifdef UNITY_PASS_FORWARDBASE
    #if !defined(_GLOSSYREFLECTIONS_OFF)
        indirectSpecular += GetReflections(worldNormal, i.worldPos.xyz, viewDir, f0, roughness, NoV, surf, indirectDiffuse);
    #endif
#endif

#ifdef LTCGI
    float2 ltcgi_lmuv;
    #if defined(LIGHTMAP_ON)
        ltcgi_lmuv = i.lmap.xy;
    #else
        ltcgi_lmuv = float2(0, 0);
    #endif

    float3 ltcgiSpecular = 0;
    LTCGI_Contribution(i.worldPos, worldNormal, viewDir, surf.perceptualRoughness, ltcgi_lmuv, indirectDiffuse
        #ifndef SPECULAR_HIGHLIGHTS_OFF
                , ltcgiSpecular
        #endif
    );
    indirectSpecular += ltcgiSpecular;
#endif


    #if defined(_BUILTIN_ALPHAPREMULTIPLY_ON)
        surf.albedo.rgb *= surf.alpha;
        surf.alpha = lerp(surf.alpha, 1.0, surf.metallic);
    #endif

    #if defined(_ALPHAMODULATE_ON)
        surf.albedo.rgb = lerp(1.0, surf.albedo.rgb, surf.alpha);
    #endif
    

    #ifdef SHADER_API_MOBILE
        indirectSpecular *= EnvBRDFApprox(surf.perceptualRoughness, NoV, f0);
    #else
        indirectSpecular *= DFGEnergyCompensation * EnvBRDFMultiscatter(DFGLut, f0);
    #endif
    

    OVERRIDE_SHADING

    half4 finalColor = half4(surf.albedo.rgb * (1.0 - surf.metallic) * (indirectDiffuse * surf.occlusion + lightData.FinalColor) + indirectSpecular + directSpecular + surf.emission, surf.alpha);

    #ifdef UNITY_PASS_META
        UnityMetaInput metaInput;
        UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaInput);
        metaInput.Emission = surf.emission;
        metaInput.Albedo = surf.albedo.rgb;
        return float4(UnityMetaFragment(metaInput).rgb, surf.alpha);
    #endif

    
    UNITY_APPLY_FOG(i.fogCoord, finalColor);

    OVERRIDE_FINALCOLOR
    
    return finalColor;
#endif
}
#endif