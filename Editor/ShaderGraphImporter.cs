using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ShaderGraphImporter
{
    [CustomEditor(typeof(ImporterSettings))]
    public class SettingsEditor : Editor
    {
        private ImporterSettings _settings;

        private Vector2 _scrollPosition;
        private ReorderableList _reorderableList;

        private bool firstTime = true;

        SerializedProperty elements;
        public override void OnInspectorGUI()
        {
            _settings = (ImporterSettings)target;

            if (firstTime)
            {
                elements = serializedObject.FindProperty("cgInclude");
                _reorderableList = new ReorderableList(serializedObject, elements);
                _reorderableList.drawElementCallback = DrawListItems;
                _reorderableList.drawHeaderCallback = DrawHeader;
                firstTime = false;
            }
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(_settings, "Shader Graph Importer Settings");

            EditorGUILayout.Space();
            if (GUILayout.Button("Paste & Import"))
            {
                _settings.shaderCode = GUIUtility.systemCopyBuffer;
                Importer.ImportShader(ref _settings);
            }

            EditorGUILayout.Space();


            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Re-Import"))
                {
                    Importer.ImportShader(ref _settings);
                }
                if (GUILayout.Button("Ping"))
                {
                    var shaderObject = AssetDatabase.LoadAssetAtPath(_settings.importPath + _settings.fileName + ".shader", typeof(Shader));
                    if (shaderObject is null) return;
                    EditorGUIUtility.PingObject(shaderObject);
                }
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Features", EditorStyles.boldLabel);
            GUILayout.BeginVertical("box");
            _settings.alphaToCoverage = EditorGUILayout.ToggleLeft("Alpha To Coverage", _settings.alphaToCoverage);
            _settings.bakeryFeatures = EditorGUILayout.ToggleLeft("Bakery Features", _settings.bakeryFeatures);
            _settings.specularOcclusion = EditorGUILayout.ToggleLeft("Specular Occlusion", _settings.specularOcclusion);
            //_settings.stencil = EditorGUILayout.ToggleLeft("Stencil", _settings.stencil);
            _settings.ltcgi = EditorGUILayout.ToggleLeft("LTCGI", _settings.ltcgi);

            GUILayout.EndVertical();

            EditorGUILayout.Space();
            GUILayout.BeginVertical("box");

            _settings.shaderName = EditorGUILayout.TextField("Shader Name", _settings.shaderName);
            _settings.CustomEditor = EditorGUILayout.TextField("Custom Editor", _settings.CustomEditor);

            _settings.fallback = EditorGUILayout.TextField("Fallback", _settings.fallback);

            _settings.fileName = EditorGUILayout.TextField("File Name", _settings.fileName);

            using (new GUILayout.HorizontalScope())
            {
                _settings.importPath = EditorGUILayout.TextField("File Path", _settings.importPath);
                if (GUILayout.Button("Select"))
                {
                    _settings.importPath = EditorUtility.OpenFolderPanel("Shader Path", "", "");
                    if (_settings.importPath.StartsWith(Application.dataPath))
                    {
                        _settings.importPath = "Assets" + _settings.importPath.Substring(Application.dataPath.Length);
                    }
                }
            }


            using (new GUILayout.HorizontalScope())
            {
                _settings.shaderGraphProjectPath = EditorGUILayout.TextField("ShaderGraph Project", _settings.shaderGraphProjectPath);
                if (GUILayout.Button("Select"))
                {
                    _settings.shaderGraphProjectPath = EditorUtility.OpenFolderPanel("ShaderGraph Project Assets Path", "", "");
                }
            }



            GUILayout.EndVertical();
            _reorderableList.DoLayoutList();

            //EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            _settings.showCode = EditorGUILayout.ToggleLeft("Show Shader Code", _settings.showCode);
            if (_settings.showCode)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(500));
                EditorGUILayout.TextArea(_settings.shaderCode, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }


            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_settings);
            }
        }

        void DrawListItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = elements.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect,element,GUIContent.none);
        }

        void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "HLSLINCLUDE");
        }
    }

    internal static class Importer
    {
        private const string DefaultShaderEditor = "ShaderGraphImporter.DefaultInspector";
        private const string DefaultImportPath = "Assets/ShaderGraph/";

        private const string ltcgiInclude = "#include \"Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc\"";

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

        private static void EditShaderFile(ref string[] input, ImporterSettings importerSettings)
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
                        input[index] = input[index] + "\n[HideInInspector][NonModifiableTextureData]_DFG(\"DFG Lut\", 2D) = \"white\" {}";
                        input[index] += "\n[HideInInspector] [Enum(Off, 0, On, 1)] _AlphaToMask (\"Alpha To Coverage\", Int) = 0";

                        if (importerSettings.specularOcclusion)
                        {
                            input[index] += "\n _SpecularOcclusion(\"Specular Occlusion\", Range(0,1)) = 0";
                        }

                        if (importerSettings.bakeryFeatures)
                        {
                            input[index] += "\n[Toggle(BAKERY_SH)] _BakerySH (\"Bakery SH\", Int) = 0";
                            input[index] += "\n[Toggle(LIGHTMAPPED_SPECULAR)] _LightmappedSpecular (\"Lightmapped Specular\", Int) = 0";
                            input[index] += "\n[Toggle(BAKERY_PROBESHNONLINEAR)] _NonLinearLightProbeSH (\"Non-Linear LightProbe SH\", Int) = 0";
                        }
                        if (importerSettings.ltcgi)
                        {
                            input[index] += "\n[Toggle(LTCGI)] _LTCGI(\"LTCGI\", Int) = 0";
                            input[index] += "\n[Toggle(LTCGI_DIFFUSE_OFF)] _LTCGI_DIFFUSE_OFF(\"LTCGI Disable Diffuse\", Int) = 0";
                        }
                        
                    }
                }

                // predefined keywords
                if (trimmed.Equals("SubShader", StringComparison.Ordinal))
                {
                    parsingProperties = false;
                    var predefined = new List<string>();

                    if (importerSettings.alphaToCoverage && materialOverrideOn) predefined.Add("#define PREDEFINED_A2C");
                    if (importerSettings.specularOcclusion) predefined.Add("#define _SPECULAR_OCCLUSION");


                    var sb = new StringBuilder().AppendLine("HLSLINCLUDE");

                    sb.AppendLine("#define IMPORTER_VERSION 1");

                    sb.AppendLine("#pragma skip_variants UNITY_HDR_ON");
                    sb.AppendLine("#pragma skip_variants _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                    sb.AppendLine("#pragma skip_variants LIGHTPROBE_SH");

                    for (int j = 0; j < importerSettings.cgInclude.Length; j++)
                    {
                        sb.AppendLine(importerSettings.cgInclude[j]);
                    }
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
                    if (importerSettings.alphaToCoverage && materialOverrideOn) input[index] += '\n' + "AlphaToMask [_AlphaToMask]";

                    
                }
                else if (trimmed.Equals("Blend SrcAlpha One, One One", StringComparison.Ordinal))
                {
                    // forward add pass
                    if (materialOverrideOn) input[index] = "Blend [_BUILTIN_SrcBlend] One";
                    if (materialOverrideOn) input[index] += '\n' + "Cull [_BUILTIN_CullMode]";
                    if (materialOverrideOn) input[index] += '\n' + "ZTest LEqual";
                    input[index] += '\n' + "Fog { Color (0,0,0,0) }";
                    if (importerSettings.alphaToCoverage && materialOverrideOn) input[index] += '\n' + "AlphaToMask [_AlphaToMask]";
                }
                else if (trimmed.StartsWith("#pragma multi_compile_shadowcaster", StringComparison.Ordinal))
                {
                    // multicompile missing
                    input[index] = input[index] + '\n' + "#pragma multi_compile_instancing";
                }
                else if (trimmed.StartsWith("#pragma multi_compile_fwdbase", StringComparison.Ordinal))
                {
                    input[index] += "\n#pragma multi_compile_fragment _ VERTEXLIGHT_ON";

                    if (importerSettings.bakeryFeatures)
                    {
                        input[index] += "\n#pragma shader_feature_local_fragment BAKERY_SH";
                        input[index] += "\n#pragma shader_feature_local_fragment LIGHTMAPPED_SPECULAR";
                        input[index] += "\n#pragma shader_feature_local_fragment BAKERY_PROBESHNONLINEAR";
                    }
                    if (importerSettings.ltcgi)
                    {
                        input[index] += "\n#pragma shader_feature_local_fragment LTCGI";
                        input[index] += "\n#pragma shader_feature_local_fragment LTCGI_DIFFUSE_OFF";
                    }
                }

                if (importerSettings.ltcgi && input[index].EndsWith("/ShaderGraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl\"", StringComparison.Ordinal))
                {
                    input[index] =  ltcgiInclude + '\n' + input[index];
                }


                //remove unneeded passes
                else if (trimmed.Equals("Name \"BuiltIn Deferred\"", StringComparison.Ordinal))
                {
                    index = RemovePass(input, index);
                }
                else if (trimmed.Equals("Name \"DepthOnly\"", StringComparison.Ordinal))
                {
                    index = RemovePass(input, index);
                }
                else if (trimmed.Equals("Name \"SceneSelectionPass\"", StringComparison.Ordinal))
                {
                    index = RemovePass(input, index);
                }
                else if (trimmed.Equals("Name \"ScenePickingPass\"", StringComparison.Ordinal))
                {
                    index = RemovePass(input, index);
                }

                

                //tags
                else if (trimmed.Equals("\"ShaderGraphShader\"=\"true\"", StringComparison.Ordinal))
                {
                    if (importerSettings.ltcgi)
                    {
                        input[index] += "\n \"LTCGI\" = \"_LTCGI\"";
                    }
                }


                // additional properties
                else if (trimmed.Equals("CBUFFER_START(UnityPerMaterial)", StringComparison.Ordinal))
                {
                    if (importerSettings.specularOcclusion)
                    {
                        input[index] += "\nhalf _SpecularOcclusion;";
                    }
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
                    // string customEditor = trimmed.Remove(0, ("CustomEditorForRenderPipeline ").Length);
                    // if (customEditor.EndsWith("\"\"")) customEditor = customEditor.Remove(customEditor.Length - 4, 3); // remove double quotes at the end

                    //input[index] = "CustomEditor " + (customEditor.Contains("UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInLitGUI") ? importerSettings.shaderInspector : customEditor);
                    input[index] = "CustomEditor \"" + importerSettings.CustomEditor + "\"";
                }
                else if (trimmed.StartsWith("FallBack \"Hidden", StringComparison.Ordinal))
                {
                    input[index] = "Fallback \"" + importerSettings.fallback + "\"";
                }



            }
        }

        private static int RemovePass(string[] input, int index)
        {
            for (int i = index - 2; i < input.Length; i++)
            {
                if (input[i].TrimStart().Equals("ENDHLSL"))
                {
                    input[i] = string.Empty;
                    input[i + 1] = string.Empty;
                    break;
                }
                input[i] = string.Empty;
                index = i;
            }

            return index;
        }

        internal static void ImportShader(ref ImporterSettings importerSettings)
        {
            if (string.IsNullOrEmpty(importerSettings.importPath)) importerSettings.importPath = DefaultImportPath;
            if (string.IsNullOrEmpty(importerSettings.CustomEditor)) importerSettings.CustomEditor = DefaultShaderEditor;


            var fileLines = importerSettings.shaderCode.Split('\n');

            var shaderName = fileLines[0].TrimStart().Replace("Shader \"", "").TrimEnd('"').Replace("/", " ");

            shaderName = $"Shader Graphs/{shaderName}";

            if (string.IsNullOrEmpty(importerSettings.shaderName)) importerSettings.shaderName = shaderName;

            fileLines[0] = $"Shader \"{importerSettings.shaderName}\"";

            EditShaderFile(ref fileLines, importerSettings);

            if (!Directory.Exists(importerSettings.importPath))
            {
                System.IO.Directory.CreateDirectory(importerSettings.importPath);
            }

            var fileName = importerSettings.shaderName.Replace('/', ' ');

            if (string.IsNullOrEmpty(importerSettings.fileName)) importerSettings.fileName = fileName;


            

            if (!importerSettings.importPath.EndsWith("/")) importerSettings.importPath += "/";


            string shaderPath = importerSettings.importPath + importerSettings.fileName + ".shader";
            File.WriteAllLines(shaderPath, fileLines);

            AssetDatabase.Refresh();

            ApplyDFG(shaderPath);
        }

        const string DFGLutPath = "Packages/com.z3y.shadergraph-builtin/Editor/dfg-multiscatter.exr";
        private static void ApplyDFG(string shaderPath)
        {

            var texture = AssetDatabase.LoadAssetAtPath(DFGLutPath, typeof(Texture2D)) as Texture2D;

            var importer = ShaderImporter.GetAtPath(shaderPath) as ShaderImporter;
            importer.SetNonModifiableTextures(new[] { "_DFG" }, new[] { texture });
        }
    }
}