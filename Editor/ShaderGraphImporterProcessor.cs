using System;
using UnityEditor;
using UnityEngine;

namespace ShaderGraphImporter
{
        
    class ShaderGraphImporterProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {

            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (!importedAssets[i].EndsWith(ShaderGraphScriptedImporter.EXT, StringComparison.Ordinal)) continue;

                var shaderObj = AssetDatabase.LoadAssetAtPath(importedAssets[i], typeof(Shader));

                if (shaderObj is Shader shader)
                {
                    ShaderUtil.RegisterShader(shader);
                }
            }
            
        }
    }
}