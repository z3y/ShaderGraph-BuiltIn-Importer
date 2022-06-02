using System;
using UnityEditor;
using UnityEngine;

namespace ShaderGraphImporter
{
        
    class ShaderGraphImporterProcessor : AssetPostprocessor
    {
        internal static readonly Texture2D dfg = AssetDatabase.LoadAssetAtPath("Packages/com.z3y.shadergraph-builtin/Editor/dfg-multiscatter.exr", typeof(Texture2D)) as Texture2D;
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {

            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (!IsShaderGraphImporter(importedAssets[i])) continue;
                var shaderObj = AssetDatabase.LoadAssetAtPath(importedAssets[i], typeof(Shader));
                if (!(shaderObj is Shader shader)) continue;
                
               // ShaderUtil.ClearShaderMessages(shader);
                

                ShaderUtil.RegisterShader(shader);
            }

            for (int i = 0; i < deletedAssets.Length; i++)
            {
                Debug.Log("deleted");
            }
            
        }
        
        private static bool IsShaderGraphImporter(string path) => path.EndsWith(ShaderGraphScriptedImporter.EXT, StringComparison.Ordinal);
    }
}