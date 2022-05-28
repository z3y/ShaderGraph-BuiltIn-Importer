using UnityEngine;

namespace ShaderGraphImporter
{
    [CreateAssetMenu(fileName = "new Shader", menuName = "Shader/Shader Graph Importer")]
    public class ImporterSettings : ScriptableObject
    {
        public string shaderName;
        public string shaderCode;
        public bool alphaToCoverage = true;
        public bool grabPass = false;
        public string grabPassName = "_GrabTexture";
        public bool bicubicLightmap = true;
        public string shaderGraphProjectPath; // for handling custom .hlsl includes
        public string importPath;
        public string CustomEditor;
        public bool showCode = false; // prevent inspector lag
        public bool bakeryFeatures = true;
        public bool specularOcclusion = false;
        public bool ltcgi = false;
        public bool dps = false;
        public bool stencil = false;
        public bool includeAudioLink = false;
        public string fileName;
        public string fallback;
        public string[] cgInclude;
        public ShadingModel shadingModel = ShadingModel.Lit;
    }

    public enum ShadingModel { Lit, FlatLit };

}
