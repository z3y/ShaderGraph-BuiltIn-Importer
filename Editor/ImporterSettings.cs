using UnityEngine;

namespace ShaderGraphImporter
{
    [CreateAssetMenu(fileName = "ImporterSettings", menuName = "Shader/ShaderGraphImporter")]
    public class ImporterSettings : ScriptableObject
    {
        public string shaderName;
        public string shaderCode;
        public bool alphaToCoverage = true;
        public bool grabPass = false;
        public string shaderGraphProjectPath; // for handling custom .hlsl includes
        public string importPath;
        public string CustomEditor;
        public bool showCode = false; // prevent inspector lag
        public bool bakeryFeatures = false;
        public bool specularOcclusion = false;
        public bool ltcgi = false;
        public bool stencil = false;
        public string fileName;
    }
}
