using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        //private static string _shaderCodeEditor = "";
        //private static string _customEditor = "";

        private const string DefaultEditor = "\"ShaderGraphImporter.DefaultInspector\"";

        private static bool useAlphaToCoverage = true;
        private static bool useDFGMultiscatter = true;


        void OnGUI()
        {
            if (GUILayout.Button("Paste & Import"))
            {
                string code = GUIUtility.systemCopyBuffer;
                ImportShader(ref code);
            }

            useAlphaToCoverage = EditorGUILayout.ToggleLeft("Alpha To Coverage", useAlphaToCoverage);
            //useDFGMultiscatter = EditorGUILayout.ToggleLeft("DFG Multiscatter", useDFGMultiscatter);



        }

        private const string ImportPath = "Assets/ShaderGraph/";

        private static readonly string[] ReplaceLines =
        {
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

        private static void EditShaderFile(ref string[] input)
        {
            

            bool parsingProperties = true;
            bool materialOverrideOn = false;

            for (var index = 0; index < input.Length; index++)
            {
                var trimmed = input[index].TrimStart();


                // replace hlsl include paths
                if (trimmed.StartsWith(ReplaceCoreRP[0], StringComparison.Ordinal))
                {
                    input[index] = input[index].Replace(ReplaceCoreRP[0], ReplaceCoreRP[1]);
                }
                else if (trimmed.StartsWith(ReplaceShaderGraphLibrary[0], StringComparison.Ordinal))
                {
                    input[index] = input[index].Replace(ReplaceShaderGraphLibrary[0], ReplaceShaderGraphLibrary[1]);
                }


                if (parsingProperties)
                {
                    // just adds attributes for the inspector, could be done differently
                    if (trimmed.StartsWith("[HideInInspector]_BUILTIN_Surface", StringComparison.Ordinal))
                    {
                        materialOverrideOn = true;
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
                        input[index] = "[Enum(UnityEngine.Rendering.CompareFunction)]" + input[index];
                    }
                    else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_CullMode", StringComparison.Ordinal))
                    {
                        input[index] = "[Enum(UnityEngine.Rendering.CullMode)]" + input[index];
                    }

                    // additional properties
                    else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_QueueControl", StringComparison.Ordinal))
                    {
                        input[index] = input[index] + '\n' + "[HideInInspector][NonModifiableTextureData]_DFG(\"DFG Lut\", 2D) = \"white\" {}";
                        input[index] += '\n' + "[HideInInspector] [Enum(Off, 0, On, 1)] _AlphaToMask (\"Alpha To Coverage\", Int) = 0";
                    }
                }

                // predefined keywords
                if (trimmed.Equals("SubShader", StringComparison.Ordinal))
                {
                    parsingProperties = false;
                    var predefined = new List<string>();

                    if (useAlphaToCoverage && materialOverrideOn) predefined.Add("#define PREDEFINED_A2C");
                    if (useDFGMultiscatter) predefined.Add("#define PREDEFINED_DFGMULTISCATTER");


                    var sb = new StringBuilder().AppendLine("HLSLINCLUDE");

                    sb.AppendLine("#pragma skip_variants UNITY_HDR_ON");
                    sb.AppendLine("#pragma skip_variants _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                    sb.AppendLine("#pragma skip_variants LIGHTPROBE_SH");

                    
                    for (int j = 0; j < predefined.Count; j++)
                    {
                        sb.AppendLine(predefined[j]);
                    }
                    sb.AppendLine("ENDHLSL");


                    input[index] = sb.ToString() + '\n' + input[index];
                }


                // pass fixes
                else if (trimmed.Equals("Name \"BuiltIn Forward\"", StringComparison.Ordinal) || trimmed.Equals("Name \"Pass\"", StringComparison.Ordinal))
                {
                    if (useAlphaToCoverage && materialOverrideOn) input[index] += '\n' + "AlphaToMask [_AlphaToMask]";
                }
                else if (trimmed.Equals("Blend SrcAlpha One, One One", StringComparison.Ordinal))
                {
                    // forward add pass
                    if (materialOverrideOn) input[index] = "Blend [_BUILTIN_SrcBlend] One";
                    if (materialOverrideOn) input[index] += '\n' + "Cull [_BUILTIN_CullMode]";
                    if (materialOverrideOn) input[index] += '\n' + "ZTest LEqual";
                    input[index] += '\n' + "Fog { Color (0,0,0,0) }";
                    if (useAlphaToCoverage && materialOverrideOn) input[index] += '\n' + "AlphaToMask [_AlphaToMask]";
                }
                else if (trimmed.StartsWith("#pragma multi_compile_shadowcaster", StringComparison.Ordinal))
                {
                    // multicompile missing
                    input[index] = input[index] + '\n' + "#pragma multi_compile_instancing";
                }
                // remove unneeded multicompiles
                foreach (var replaceLine in ReplaceLines)
                {
                    if (trimmed.StartsWith(replaceLine, StringComparison.Ordinal))
                    {
                        input[index] = string.Empty;
                    }
                }

                if (trimmed.StartsWith("CustomEditor \"UnityEditor.ShaderGraph.", StringComparison.Ordinal))
                {
                    input[index] = "";
                }
                // replace default shader graph editor with default editor, keeps the same if its custom
                else if (trimmed.StartsWith("CustomEditorForRenderPipeline \"", StringComparison.Ordinal))
                {
                    string customEditor = trimmed.Remove(0, ("CustomEditorForRenderPipeline ").Length);
                   if (customEditor.EndsWith("\"\"")) customEditor = customEditor.Remove(customEditor.Length - 4, 3); // remove double quotes at the end

                     input[index] = "CustomEditor " + (customEditor.Contains("UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInLitGUI") ? DefaultEditor : customEditor);
                }
                


            }
        }

        private static void ImportShader(ref string shaderCode)
        {
            var fileLines = shaderCode.Split('\n');

            var fileName = fileLines[0].TrimStart().Replace("Shader \"", "").TrimEnd('"').Replace("/", " ");

            fileLines[0] = $"Shader \"Shader Graphs/{fileName}\"";

            EditShaderFile(ref fileLines);

            if (!Directory.Exists(ImportPath))
            {
                System.IO.Directory.CreateDirectory(ImportPath);
            }
            string shaderPath = ImportPath + fileName + ".shader";
            File.WriteAllLines(shaderPath, fileLines);

            AssetDatabase.Refresh();

            ApplyDFG(shaderPath);
        }

        const string DFGLutPath = "Packages/com.z3y.shadergraph-builtin/Editor/dfg-multiscatter.exr";
        private static void ApplyDFG(string shaderPath)
        {
            if (!useDFGMultiscatter) return;

            var texture = AssetDatabase.LoadAssetAtPath(DFGLutPath, typeof(Texture2D)) as Texture2D;

            var importer = ShaderImporter.GetAtPath(shaderPath) as ShaderImporter;
            importer.SetNonModifiableTextures(new[] { "_DFG" }, new[] { texture });
        }
    }
}