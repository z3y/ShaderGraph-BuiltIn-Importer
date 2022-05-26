#ifndef SG_SHADOW_PASS_INCLUDED
#define SG_SHADOW_PASS_INCLUDED

#include "Packages/com.z3y.shadergraph-builtin/CustomLighting/Core.cginc"

v2f_surf ShadowCasterVertex(appdata_full v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    v2f_surf o;
    UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
    UNITY_TRANSFER_INSTANCE_ID(v,o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    #ifdef RALIV_PENETRATOR
        PenetratorReshape(v.vertex, v.normal);
    #endif
    #ifdef RALIV_ORIFICE
  		OrificeReshape(v.vertex, v.normal, v.tangent, v.vertexId);
    #endif

    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    float3 worldNormal = UnityObjectToWorldNormal(v.normal);
    o.worldPos.xyz = worldPos;
    TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
    return o;
}

void ShadowCasterVertex(Attributes input, VertexDescription vertexDescription, inout Varyings varyings)
{
    appdata_full v;
    ZERO_INITIALIZE(appdata_full, v);
    BuildAppDataFull(input, vertexDescription, v);

    #if UNITY_ANY_INSTANCING_ENABLED
    v.instanceID = input.instanceID;
    #endif

    v2f_surf o = ShadowCasterVertex(v);

    #if UNITY_ANY_INSTANCING_ENABLED
    varyings.instanceID = o.instanceID;
    #endif
    SurfaceVertexToVaryings(o, varyings);
}

half4 ShadowCasterFragment(v2f_surf IN)
{
    UNITY_SETUP_INSTANCE_ID(IN);
    #ifdef FOG_COMBINED_WITH_TSPACE
        UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
    #elif defined FOG_COMBINED_WITH_WORLD_POS
        UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
    #else
        UNITY_EXTRACT_FOG(IN);
    #endif
    float3 worldPos = IN.worldPos.xyz;
    #ifndef USING_DIRECTIONAL_LIGHT
        fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
    #else
        fixed3 lightDir = _WorldSpaceLightPos0.xyz;
    #endif
    fixed3 normalWorldVertex = fixed3(0,0,1);

    SHADOW_CASTER_FRAGMENT(IN)
}

half4 ShadowCasterFragment(SurfaceDescription surfaceDescription, Varyings varyings)
{
    v2f_surf vertexSurf;
    ZERO_INITIALIZE(v2f_surf, vertexSurf);
    #if UNITY_ANY_INSTANCING_ENABLED
    vertexSurf.instanceID = varyings.instanceID;
    #endif
    VaryingsToSurfaceVertex(varyings, vertexSurf);

    //return ShadowCasterFragment(vertexSurf);
    SurfaceDataCustom surf;
    InitializeDefaultSurfaceData(surf);
//    CopyStandardToCustomSurfaceData(surf, o);
    #ifdef _BUILTIN_AlphaClip
    surf.alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
    #endif
    #if defined(_BUILTIN_SURFACE_TYPE_TRANSPARENT) || defined(_BUILTIN_AlphaClip)
      surf.alpha = surfaceDescription.Alpha;
    #endif

    return CustomLightingFrag(vertexSurf, surf);
}

PackedVaryings vert(Attributes input)
{
    Varyings output;
    ZERO_INITIALIZE(Varyings, output);
    output = BuildVaryings(input);

    VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
    ShadowCasterVertex(input, vertexDescription, output);

    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    // #if _AlphaClip
    //    half alpha = surfaceDescription.Alpha;
    //    clip(alpha - surfaceDescription.AlphaClipThreshold);
    // #endif

    half4 color = ShadowCasterFragment(surfaceDescription, unpacked);
    return color;
}

#endif
