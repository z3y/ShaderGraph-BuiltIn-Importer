struct SurfaceDataCustom
{
    half3 albedo;
    half3 tangentNormal;
    half3 emission;
    half metallic;
    half perceptualRoughness;
    half occlusion;
    half reflectance;
    half alpha;
};

void InitializeDefaultSurfaceData(inout SurfaceDataCustom surf)
{
    surf.albedo = 1.0;
    surf.tangentNormal = half3(0,0,1);
    surf.emission = 0.0;
    surf.metallic = 0.0;
    surf.perceptualRoughness = 0.0;
    surf.occlusion = 1.0;
    surf.reflectance = 0.5;
    surf.alpha = 1.0;
}

void CopyStandardToCustomSurfaceData(inout SurfaceDataCustom surf, SurfaceOutputStandard standardSurf)
{
    surf.albedo = standardSurf.Albedo;
    surf.tangentNormal = standardSurf.Normal;
    surf.emission = standardSurf.Emission;
    surf.metallic = standardSurf.Metallic;
    surf.perceptualRoughness = 1.0f - standardSurf.Smoothness;
    surf.occlusion = standardSurf.Occlusion;
    surf.reflectance = 0.5;
    surf.alpha = standardSurf.Alpha;
}