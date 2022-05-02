using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using Directory = UnityEngine.Windows.Directory;

namespace ShaderGraphImporter
{
    public class Importer : EditorWindow
    {
        [MenuItem("Window/ShaderGraphImporter")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            Importer window = (Importer)EditorWindow.GetWindow(typeof(Importer));
            window.Show();
        }

        private static string _shaderCodeEditor = "";
        private static string _customEditor = "";

        private const string DefaultEditor = "ShaderGraphImporter.DefaultInspector";


        void OnGUI()
        {
            if (GUILayout.Button("Paste & Import"))
            {
                _shaderCodeEditor = GUIUtility.systemCopyBuffer;
                ImportShader(_shaderCodeEditor, _customEditor);
            }

            _customEditor = GUILayout.TextField(_customEditor);
            _shaderCodeEditor = GUILayout.TextArea(_shaderCodeEditor, GUILayout.Height(200));



        }

        private const string ImportPath = "Assets/ShaderGraph/";

        private static readonly string[] FixUnityBug =
        {
            "CustomEditorForRenderPipeline",
            "#pragma multi_compile _ _SCREEN_SPACE_OCCLUSION",
            "#pragma multi_compile _ LIGHTMAP_ON",
            "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
            "#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN",
            "#pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF",
            "#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS",
            "#pragma multi_compile _ _SHADOWS_SOFT",
            "#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING",
            "#pragma multi_compile _ SHADOWS_SHADOWMASK",
            "#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE",
            "#pragma multi_compile _ _GBUFFER_NORMALS_OCT",
            "#pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW"
        };

        private static readonly string[] ReplaceCoreRP =
        {
            "#include \"Packages/com.unity.render-pipelines.core/",
            "#include \"Packages/com.z3y.shadergraph-builtin/CoreRP/"
        };

        private static readonly string[] ReplaceShaderGraphLibrary =
        {
            "#include \"Packages/com.unity.shadergraph/",
            "#include \"Packages/com.z3y.shadergraph-builtin/ShaderGraph/"
        };

        private static void FixBugs(ref string[] input, string[] replaceLines, string customEditor)
        {
            for (var index = 0; index < input.Length; index++)
            {
                var trimmed = input[index].TrimStart();

                foreach (var replaceLine in replaceLines)
                {
                    if (trimmed.StartsWith(replaceLine, StringComparison.Ordinal))
                    {
                        input[index] = string.Empty;
                    }
                }

                if (trimmed.StartsWith(ReplaceCoreRP[0], StringComparison.Ordinal))
                {
                    input[index] = input[index].Replace(ReplaceCoreRP[0], ReplaceCoreRP[1]);
                }

                else if (trimmed.StartsWith(ReplaceShaderGraphLibrary[0], StringComparison.Ordinal))
                {
                    input[index] = input[index].Replace(ReplaceShaderGraphLibrary[0], ReplaceShaderGraphLibrary[1]);
                }

                else if (trimmed.StartsWith("#pragma multi_compile_shadowcaster", StringComparison.Ordinal))
                {
                    input[index] = input[index] + '\n' + "#pragma multi_compile_instancing";
                }

                else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_Surface", StringComparison.Ordinal))
                {
                    input[index] = "[Enum(ShaderGraphImporter.SurfaceType)]" + input[index];
                }
                else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_Blend", StringComparison.Ordinal))
                {
                    input[index] = "[Enum(ShaderGraphImporter.BlendingMode)]" + input[index];
                }
                else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_AlphaClip", StringComparison.Ordinal))
                {
                    input[index] = "[ToggleUI]" + input[index];
                }
                else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_SrcBlend", StringComparison.Ordinal))
                {
                    input[index] = "[Enum(UnityEngine.Rendering.BlendMode)]" + input[index];
                }
                else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_DstBlend", StringComparison.Ordinal))
                {
                    input[index] = "[Enum(UnityEngine.Rendering.BlendMode)]" + input[index];
                }
                else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_ZWrite", StringComparison.Ordinal))
                {
                    input[index] = "[Enum(Off, 0, On, 1)] " + input[index];
                }
                else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_ZTest", StringComparison.Ordinal))
                {
                    input[index] = "[Enum(Off, 0, On, 1)] " + input[index];
                }
                else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_CullMode", StringComparison.Ordinal))
                {
                    input[index] = "[Enum(UnityEngine.Rendering.CullMode)]" + input[index];
                }
                else if (trimmed.StartsWith("CustomEditor \"", StringComparison.Ordinal))
                {
                    input[index] = "CustomEditor \"" + (customEditor == "" ? DefaultEditor : customEditor) + "\"";
                }

            }
        }

        private static void ImportShader(string shaderCode, string customEditor = "")
        {
            var fileLines = shaderCode.Split('\n');

            var fileName = fileLines[0].TrimStart().Replace("Shader \"", "").TrimEnd('"').Replace("/", " ");

            fileLines[0] = $"Shader \"Shader Graphs/{fileName}\"";

            FixBugs(ref fileLines, FixUnityBug, customEditor);

            if (!Directory.Exists(ImportPath))
            {
                System.IO.Directory.CreateDirectory(ImportPath);
            }
            File.WriteAllLines(ImportPath + fileName + ".shader", fileLines);

            AssetDatabase.Refresh();
        }
    }
}