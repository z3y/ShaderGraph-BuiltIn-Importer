using UnityEngine;

namespace ShaderGraphImporter
{
    [CreateAssetMenu(fileName = "new Importer", menuName = "Shader/Shader Graph Importer")]
    public class ImporterSettings : ScriptableObject
    {
        public string shaderCode;
        public string shaderPath;
        public bool alphaToCoverage = true;
        public bool grabPass = false;
        public bool allowVertexLights = true;
        public bool lodFadeCrossfade = false;
        public bool bicubicLightmap = true;
        public bool bakeryFeatures = true;
        public bool specularOcclusion = false;
        public bool ltcgi = false;
        public bool dps = false;
        public bool stencil = false;
        public bool includeAudioLink = false;
        public string CustomEditor = "ShaderGraphImporter.DefaultInspector";
        public string fallback;
        public string[] cgInclude;
        public ShadingModel shadingModel = ShadingModel.Lit;
        public VRCFallbackTags fallbackTags = VRCFallbackTags.defaultTag;
        public string VRCFallback = string.Empty;
        public bool thirdPartyFoldout = false;
    }

    public enum ShadingModel { Lit, FlatLit };



}