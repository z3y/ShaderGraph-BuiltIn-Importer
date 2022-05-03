PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    #if _BUILTIN_AlphaClip
        half alpha = surfaceDescription.Alpha;

        #if defined(UNITY_PASS_SHADOWCASTER) || !defined(_ALPHA_TO_COVERAGE)
            clip(alpha - surfaceDescription.AlphaClipThreshold);
        #else
            alpha = (alpha - surfaceDescription.AlphaClipThreshold) / max(fwidth(alpha), 0.0001) + 0.5;
        #endif

    #elif _SURFACE_TYPE_TRANSPARENT
        half alpha = surfaceDescription.Alpha;
    #else
        half alpha = 1;
    #endif

#ifdef _ALPHAPREMULTIPLY_ON
    surfaceDescription.BaseColor *= alpha;
#endif

    return half4(surfaceDescription.BaseColor, alpha);
}
