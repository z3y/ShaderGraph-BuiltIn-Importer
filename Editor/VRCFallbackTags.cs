using System;

namespace ShaderGraphImporter
{
    [Serializable]
    public class VRCFallbackTags
    {
        public enum ShaderType
        {
            Standard,
            Unlit,
            VertexLit,
            Toon,
            Particle,
            Sprite,
            Matcap,
            MobileToon,
            Hidden
        };
            
        public enum ShaderMode
        {
            Opaque,
            Cutout,
            Transparent,
            Fade
        };

        public ShaderType type;
        public ShaderMode mode;
        public bool doubleSided;
            
        public static string GetTag(VRCFallbackTags tags)
        {
            string result = string.Empty;
            result += Enum.GetName(typeof(ShaderType), tags.type);
            result += Enum.GetName(typeof(ShaderMode), tags.mode);
            if (tags.doubleSided) result += "DoubleSided";

            result = result.Replace("Standard", string.Empty);
            result = result.Replace("Opaque", string.Empty);
            
            return result;
        }
    }
}