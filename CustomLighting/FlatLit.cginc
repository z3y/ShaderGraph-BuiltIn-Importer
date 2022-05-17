
half3 BetterSH9(half4 normal)
{
    half3 indirect;
    half3 L0 = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w) + half3(unity_SHBr.z, unity_SHBg.z, unity_SHBb.z) / 3.0;
    indirect.r = shEvaluateDiffuseL1Geomerics(L0.r, unity_SHAr.xyz, normal.xyz);
    indirect.g = shEvaluateDiffuseL1Geomerics(L0.g, unity_SHAg.xyz, normal.xyz);
    indirect.b = shEvaluateDiffuseL1Geomerics(L0.b, unity_SHAb.xyz, normal.xyz);
    indirect = max(0, indirect);
    indirect += SHEvalLinearL2(normal);
    return indirect;
}

float calculateluminance(float3 color)
{
    return color.r * 0.299 + color.g * 0.587 + color.b * 0.114;
}