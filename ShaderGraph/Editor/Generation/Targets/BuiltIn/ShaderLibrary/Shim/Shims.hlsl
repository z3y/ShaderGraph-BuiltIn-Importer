#ifndef UNITY_SHIMS_INCLUDED
#define UNITY_SHIMS_INCLUDED

// This file serves as the shim between the legacy cginc files that required for the built-in pipeline and the core srp library.
// For the built-in RP to work correctly, all the lighting in the cginc files is necessary, but there's a lot of utility
// required (especially for shader graph) in the core SRP library. There are also some duplicate symbols and other complications.
// This set of files helps to bridge the gap by hiding and redefining some symbols and other helpful declarations.


// built-in uses a different keyword, fix for SPI
#if defined(STEREO_INSTANCING_ON)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

#include "Packages/com.z3y.shadergraph-builtin/CoreRP/ShaderLibrary/Common.hlsl"

// Duplicate define in Macros.hlsl
#if defined (TRANSFORM_TEX)
#undef TRANSFORM_TEX
#endif

#include "HLSLSupportShim.hlsl"
#include "InputsShim.hlsl"
#include "SurfaceShaderProxy.hlsl"

#include "UnityShaderVariables.cginc"
#include "UnityShaderUtilities.cginc"
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"


struct  appdata_full_custom {
    float4 vertex : POSITION;
    float4 tangent : TANGENT;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    float4 texcoord2 : TEXCOORD2;
    float4 texcoord3 : TEXCOORD3;
    fixed4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    uint vertexId : SV_VertexID;
};

// need vertexId for dps
#define appdata_full appdata_full_custom

#if defined(RALIV_PENETRATOR) || defined(RALIV_ORIFICE)
  #include "Assets/RalivDynamicPenetrationSystem/Plugins/RalivDPS_Defines.cginc"
  #include "Assets/RalivDynamicPenetrationSystem/Plugins/RalivDPS_Functions.cginc"
#endif



struct LightDataCustom
{
    half3 Color;
    float3 Direction;
    half NoL;
    half LoH;
    half NoH;
    float3 HalfVector;
    half3 FinalColor;
    half3 Specular;
    half Attenuation;
};


#ifdef POINT
#   define COPY_FROM_LIGHT_COORDS(dest, src) dest = src._LightCoord
#   define COPY_TO_LIGHT_COORDS(dest, src) dest._LightCoord.xyz = src.xyz
#endif

#ifdef SPOT
#   define COPY_FROM_LIGHT_COORDS(dest, src) dest = src._LightCoord.xyz
#   define COPY_TO_LIGHT_COORDS(dest, src) dest._LightCoord.xyz = src.xyz
#endif

#ifdef DIRECTIONAL
#   define COPY_FROM_LIGHT_COORDS(dest, src)
#   define COPY_TO_LIGHT_COORDS(dest, src)
#endif

#ifdef POINT_COOKIE
#   define COPY_FROM_LIGHT_COORDS(dest, src) dest = src._LightCoord.xyz
#   define COPY_TO_LIGHT_COORDS(dest, src) dest._LightCoord.xyz = src.xyz
#endif

#ifdef DIRECTIONAL_COOKIE
#   define COPY_FROM_LIGHT_COORDS(dest, src) dest = float3(src._LightCoord.xy, 1)
#   define COPY_TO_LIGHT_COORDS(dest, src) dest._LightCoord.xy = src.xy
#endif

#endif // UNITY_SHIMS_INCLUDED
