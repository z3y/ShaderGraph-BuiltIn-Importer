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
                if (!importedAssets[i].EndsWith(ShaderGraphScriptedImporter.EXT, StringComparison.Ordinal)) continue;
                var shaderObj = AssetDatabase.LoadAssetAtPath(importedAssets[i], typeof(Shader));
                if (!(shaderObj is Shader shader)) continue;

                ShaderUtil.RegisterShader(shader);
            }
        }
    }
}