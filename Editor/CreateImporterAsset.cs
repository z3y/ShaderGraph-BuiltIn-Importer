using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShaderGraphImporter
{
    public static class CreateImporterAsset
    {
        [MenuItem("Assets/Create/Shader/Shader Graph Importer")]
        public static void Create()
        {
            var folder = Selection.activeObject;
            if (folder is null) return;
            
            string path = AssetDatabase.GetAssetPath(folder);
            if (folder == null || !Directory.Exists(path))
            {
                path = "Assets";
            }

            path += "/new Shader." + ShaderGraphScriptedImporter.EXT;

            var cleanPath = AssetDatabase.GenerateUniqueAssetPath(path) ;
            //File.WriteAllText(cleanPath, EmptyShader);
            //var importer = new SGScriptedImporter();

            ProjectWindowUtil.CreateAssetWithContent(cleanPath, EmptyShader);
        }

        private const string EmptyShader = @"Shader ""Hidden/ShaderGraphImporter/NewShader""
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return float4(0,0,0,1);
            }
            ENDCG
        }
    }
}";
    }
}