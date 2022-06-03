using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif


namespace ShaderGraphImporter
{
    internal static class Importer
    {
        internal const int ImporterFeatureVersion = 2;

        private const string AudioLinkInclude = "#include \"/Assets/AudioLink/Shaders/AudioLink.cginc\"";
        private const string LTCGIInclude = "#include \"Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc\"";
        static readonly Texture2D dfg = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.z3y.shadergraph-builtin/Editor/dfg-multiscatter.exr");

        const string GrabPassName = "_CameraOpaqueTexture"; // allows the scene color node to be used
        

        private static readonly string[] WrongMulticompiles =
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

        private static readonly string[] CoreRPMatch =
        {
            "#include \"Packages/com.unity.render-pipelines.core/",
            "#include \"Packages/com.z3y.shadergraph-builtin/CoreRP/"
        };

        private static readonly string[] ShaderGraphLibraryMatch =
        {
            "#include \"Packages/com.unity.shadergraph/",
            "#include \"Packages/com.z3y.shadergraph-builtin/ShaderGraph/"
        };

 
        public static void ImportShader(ImporterSettings importerSettings, string source, bool applySettings = false)
        {
            if (!applySettings) importerSettings.shaderCode = source;
            
            

            var fileLines = source.Split('\n');

            var importDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(importerSettings));

            // replace shader name
            var shaderName = fileLines[0].TrimStart().Replace("Shader \"", "").TrimEnd('"').Replace("/", " ");
            var rawShaderName = shaderName;
            shaderName = $"Imported Shader Graphs/{shaderName}";
            fileLines[0] = $"Shader \"{shaderName}\"";
            
            
            EditShaderFile(ref fileLines, importerSettings);
            
            string shaderPath = importDirectory + '/' + rawShaderName.Replace('/', ' ') + ".shader";

            importerSettings.shaderPath = shaderPath;

            File.WriteAllLines(shaderPath, fileLines);
            AssetDatabase.Refresh();

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);

            EditorMaterialUtility.SetShaderNonModifiableDefaults(shader, new[] { "_DFG" }, new Texture[] { dfg });
            AssetDatabase.ImportAsset(shaderPath);
        }
        

        private static void EditShaderFile(ref string[] lines, ImporterSettings importerSettings)
        {
            
            bool parsingProperties = true;
            bool materialOverrideOn = false;

            for (var index = 0; index < lines.Length; index++)
            {
                var trimmed = lines[index].TrimStart();


                // replace hlsl include paths
                if (trimmed.StartsWith(CoreRPMatch[0], StringComparison.Ordinal))
                {
                    lines[index] = lines[index].Replace(CoreRPMatch[0], CoreRPMatch[1]);
                }
                else if (trimmed.StartsWith(ShaderGraphLibraryMatch[0], StringComparison.Ordinal))
                {
                    lines[index] = lines[index].Replace(ShaderGraphLibraryMatch[0], ShaderGraphLibraryMatch[1]);
                }


                if (parsingProperties)
                {
                    // just adds attributes for the inspector, could be done differently
                    if (trimmed.StartsWith("[HideInInspector]_BUILTIN_Surface", StringComparison.Ordinal))
                    {
                        materialOverrideOn = true;
                        lines[index] = "[Enum(ShaderGraphImporter.SurfaceType)]" + lines[index];
                    }
                    else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_Blend", StringComparison.Ordinal))
                    {
                        lines[index] = "[Enum(ShaderGraphImporter.BlendingMode)]" + lines[index];
                    }
                    else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_AlphaClip", StringComparison.Ordinal))
                    {
                        lines[index] = "[ToggleUI]" + lines[index];
                    }
                    else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_SrcBlend", StringComparison.Ordinal))
                    {
                        lines[index] = "[Enum(UnityEngine.Rendering.BlendMode)]" + lines[index];
                    }
                    else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_DstBlend", StringComparison.Ordinal))
                    {
                        lines[index] = "[Enum(UnityEngine.Rendering.BlendMode)]" + lines[index];
                    }
                    else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_ZWrite", StringComparison.Ordinal))
                    {
                        lines[index] = "[Enum(Off, 0, On, 1)] " + lines[index];
                    }
                    else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_ZTest", StringComparison.Ordinal))
                    {
                        lines[index] = "[Enum(UnityEngine.Rendering.CompareFunction)]" + lines[index];
                    }
                    else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_CullMode", StringComparison.Ordinal))
                    {
                        lines[index] = "[Enum(UnityEngine.Rendering.CullMode)]" + lines[index];
                    }
                    
                    
                    else if (trimmed.StartsWith("[NoScaleOffset]" + GrabPassName + "(", StringComparison.Ordinal)
                             || trimmed.StartsWith(GrabPassName + "(", StringComparison.Ordinal))
                    {
                        var property = trimmed.Split('=');

                        lines[index] = property[0] + "= \"\" {}";
                    }

                    // additional properties
                    else if (trimmed.StartsWith("[HideInInspector]_BUILTIN_QueueControl", StringComparison.Ordinal))
                    {
                        var additionalProperties = new StringBuilder();
                        
                        additionalProperties.AppendLine("[HideInInspector][NonModifiableTextureData]_DFG(\"DFG Lut\", 2D) = \"white\" {}");
                        additionalProperties.AppendLine("[HideInInspector][Enum(Off, 0, On, 1)]_AlphaToMask (\"Alpha To Coverage\", Int) = 0");

                        if (importerSettings.shadingModel == ShadingModel.FlatLit)
                        {
                            additionalProperties.AppendLine("[ToggleOff(_SPECULARHIGHLIGHTS_OFF)]_SPECULARHIGHLIGHTS_OFF(\"Specular Highlights\", Float) = 0");
                            additionalProperties.AppendLine("[ToggleOff(_GLOSSYREFLECTIONS_OFF)]_GLOSSYREFLECTIONS_OFF(\"Reflections\", Float) = 0");
                        }
                        else
                        {
                            additionalProperties.AppendLine("[ToggleOff(_SPECULARHIGHLIGHTS_OFF)]_SPECULARHIGHLIGHTS_OFF(\"Specular Highlights\", Float) = 1");
                            additionalProperties.AppendLine("[ToggleOff(_GLOSSYREFLECTIONS_OFF)]_GLOSSYREFLECTIONS_OFF(\"Reflections\", Float) = 1");
                        }

                        if (importerSettings.specularOcclusion)
                        {
                            additionalProperties.AppendLine("_SpecularOcclusion(\"Specular Occlusion\", Range(0,1)) = 0");
                        }

                        if (importerSettings.bakeryFeatures)
                        {
                            additionalProperties.AppendLine("[Toggle(BAKERY_SH)]_BakerySH(\"Bakery SH\", Int) = 0");
                            additionalProperties.AppendLine("[Toggle(LIGHTMAPPED_SPECULAR)]_LightmappedSpecular(\"Lightmapped Specular\", Int) = 0");
                            additionalProperties.AppendLine("[Toggle(BAKERY_PROBESHNONLINEAR)]_NonLinearLightProbeSH(\"Non-Linear LightProbe SH\", Int) = 0");
                        }
                        if (importerSettings.ltcgi)
                        {
                            additionalProperties.AppendLine("[Toggle(LTCGI)] _LTCGI(\"LTCGI\", Int) = 0");
                            additionalProperties.AppendLine("[Toggle(LTCGI_DIFFUSE_OFF)]_LTCGI_DIFFUSE_OFF(\"LTCGI Disable Diffuse\", Int) = 0");
                        }
                        if (importerSettings.bicubicLightmap)
                        {
                            additionalProperties.AppendLine("[Toggle(_BICUBICLIGHTMAP)]_BicubicLightmapToggle(\"Bicubic Lightmap\", Int) = 0");
                        }

                        if (importerSettings.dps)
                        {
                            additionalProperties.AppendLine("[Header(DPS Settings)][Space(10)][Toggle(RALIV_PENETRATOR)] _RALIV_PENETRATOR(\"Penetrator\", Int) = 0");
                            // TODO: fix oriface bug. The vertex id might not be passed in correctly
                            // additionalProperties.AppendLine("[Toggle(RALIV_ORIFICE)] _RALIV_ORIFICE(\"Oriface\", Int) = 0");
                            var dpsProperties = File.ReadAllText("Assets/RalivDynamicPenetrationSystem/Plugins/RalivDPS_Properties.cginc");
                            additionalProperties.AppendLine(dpsProperties);
                        }

                        lines[index] += Environment.NewLine + additionalProperties;
                    }
                }

                // predefined keywords
                if (trimmed.Equals("SubShader", StringComparison.Ordinal))
                {
                    parsingProperties = false;

                    var sb = new StringBuilder().AppendLine("HLSLINCLUDE");

                    sb.AppendLine("#define IMPORTER_VERSION " + ImporterFeatureVersion);
                    
                    if (importerSettings.alphaToCoverage && materialOverrideOn) sb.AppendLine("#define PREDEFINED_A2C");
                    if (importerSettings.specularOcclusion) sb.AppendLine("#define _SPECULAR_OCCLUSION");
                    
                    sb.AppendLine("#pragma skip_variants UNITY_HDR_ON");
                    sb.AppendLine("#pragma skip_variants _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                    sb.AppendLine("#pragma skip_variants LIGHTPROBE_SH");

                    if (importerSettings.dps)
                    {
                        sb.AppendLine("#pragma shader_feature_local_vertex RALIV_PENETRATOR");
                        sb.AppendLine("#pragma shader_feature_local_vertex RALIV_ORIFICE");
                    }
                    
                    if (importerSettings.lodFadeCrossfade) sb.AppendLine("#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE");
                    

                    switch (importerSettings.shadingModel)
                    {
                        case ShadingModel.Lit:
                            sb.AppendLine("#define SHADINGMODEL_LIT");
                            break;
                        case ShadingModel.FlatLit:
                            sb.AppendLine("#define SHADINGMODEL_FLATLIT");
                            sb.AppendLine("#pragma skip_variants SHADOWS_SCREEN");
                            // sb.AppendLine("#pragma skip_variants SHADOWS_SOFT");
                            // sb.AppendLine("#pragma skip_variants SHADOWS_CUBE");
                            break;
                    }

                    if (importerSettings.cgInclude != null)
                    {
                        foreach (var t in importerSettings.cgInclude)
                        {
                            sb.AppendLine(t);
                        }
                    }
                    sb.AppendLine("ENDHLSL");


                    lines[index] = sb.ToString() + '\n' + lines[index];

                    if (importerSettings.grabPass)
                    {
                        lines[index+1] += Environment.NewLine + "GrabPass { \"" + GrabPassName + "\" }";
                    }
                }


                // pass fixes
                else if (trimmed.Equals("Name \"BuiltIn Forward\"", StringComparison.Ordinal) || trimmed.Equals("Name \"Pass\"", StringComparison.Ordinal))
                {
                    if (importerSettings.alphaToCoverage && materialOverrideOn) lines[index] += '\n' + "AlphaToMask [_AlphaToMask]";

                    
                }
                else if (trimmed.Equals("Blend SrcAlpha One, One One", StringComparison.Ordinal))
                {
                    // forward add pass
                    if (materialOverrideOn)
                    {
                        lines[index] = "Blend [_BUILTIN_SrcBlend] One";
                        if (materialOverrideOn) lines[index] += "\nCull [_BUILTIN_CullMode]";
                        if (materialOverrideOn) lines[index] += "\nZTest LEqual";
                    }

                    lines[index] += "\nFog { Color (0,0,0,0) }";
                    if (importerSettings.alphaToCoverage && materialOverrideOn) lines[index] += '\n' + "AlphaToMask [_AlphaToMask]";
                }
                else if (trimmed.StartsWith("#pragma multi_compile_shadowcaster", StringComparison.Ordinal))
                {
                    // multicompile missing
                    lines[index] += "\n#pragma multi_compile_instancing";
                }
                else if (trimmed.StartsWith("#pragma multi_compile_fwdbase", StringComparison.Ordinal))
                {
                    var forwardBaseKeywords = new StringBuilder();

                    forwardBaseKeywords.AppendLine(importerSettings.allowVertexLights
                        ? "#pragma multi_compile_fragment _ VERTEXLIGHT_ON"
                        : "#pragma skip_variants VERTEXLIGHT_ON");

                    forwardBaseKeywords.AppendLine("#pragma shader_feature_local_fragment _GLOSSYREFLECTIONS_OFF");
                    forwardBaseKeywords.AppendLine("#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF");

                    if (importerSettings.bakeryFeatures)
                    {
                        forwardBaseKeywords.AppendLine("#pragma shader_feature_local_fragment BAKERY_SH");
                        forwardBaseKeywords.AppendLine("#pragma shader_feature_local_fragment LIGHTMAPPED_SPECULAR");
                        forwardBaseKeywords.AppendLine("#pragma shader_feature_local_fragment BAKERY_PROBESHNONLINEAR");
                    }
                    if (importerSettings.ltcgi)
                    {
                        forwardBaseKeywords.AppendLine("#pragma shader_feature_local_fragment LTCGI");
                        forwardBaseKeywords.AppendLine("#pragma shader_feature_local_fragment LTCGI_DIFFUSE_OFF");
                    }
                    
                    if (importerSettings.bicubicLightmap)
                    {
                        forwardBaseKeywords.AppendLine("#pragma shader_feature_local_fragment _BICUBICLIGHTMAP");
                    }

                    lines[index] += Environment.NewLine + forwardBaseKeywords;

                }
                else if (trimmed.StartsWith("#pragma multi_compile_fwdadd_fullshadows", StringComparison.Ordinal))
                {
                    lines[index] += "\n#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF";
                }

                if (importerSettings.ltcgi && lines[index].EndsWith("/ShaderGraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl\"", StringComparison.Ordinal))
                {
                    lines[index] = LTCGIInclude + '\n' + lines[index];
                }

                if (importerSettings.includeAudioLink && lines[index].EndsWith("/ShaderGraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl\"", StringComparison.Ordinal))
                {
                    lines[index] = lines[index] + '\n' + AudioLinkInclude;
                }
                
                else if (trimmed.Equals("#pragma target 3.0", StringComparison.Ordinal))
                {
                    lines[index] = "#pragma target 4.5";
                }


                //remove unneeded passes
                else if (trimmed.Equals("Name \"BuiltIn Deferred\"", StringComparison.Ordinal))
                {
                    index = RemovePass(ref lines, index);
                }
                else if (trimmed.Equals("Name \"DepthOnly\"", StringComparison.Ordinal))
                {
                    index = RemovePass(ref lines, index);
                }
                else if (trimmed.Equals("Name \"SceneSelectionPass\"", StringComparison.Ordinal))
                {
                    index = RemovePass(ref lines, index);
                }
                else if (trimmed.Equals("Name \"ScenePickingPass\"", StringComparison.Ordinal))
                {
                    index = RemovePass(ref lines, index);
                }

                

                //tags
                else if (trimmed.Equals("\"ShaderGraphShader\"=\"true\"", StringComparison.Ordinal))
                {
                    if (importerSettings.ltcgi)
                    {
                        lines[index] += "\n\"LTCGI\" = \"_LTCGI\"";
                    }

                    if (!string.IsNullOrEmpty(importerSettings.VRCFallback))
                    {
                        lines[index] += "\n\"VRCFallback\" = \"" + importerSettings.VRCFallback + "\"";
                    }
                }


                // additional properties
                else if (trimmed.Equals("CBUFFER_START(UnityPerMaterial)", StringComparison.Ordinal))
                {
                    lines[index] = string.Empty;
                }
                else if (trimmed.Equals("CBUFFER_END", StringComparison.Ordinal))
                {
                    lines[index] = string.Empty;
                }


                // remove unneeded multicompiles
                foreach (var replaceLine in WrongMulticompiles)
                {
                    if (trimmed.StartsWith(replaceLine, StringComparison.Ordinal))
                    {
                        lines[index] = string.Empty;
                    }
                }

                

                if (trimmed.StartsWith("CustomEditor \"UnityEditor.ShaderGraph.", StringComparison.Ordinal))
                {
                    lines[index] = string.Empty;
                }
                // replace default shader graph editor with default editor, keeps the same if its custom
                else if (trimmed.StartsWith("CustomEditorForRenderPipeline \"", StringComparison.Ordinal))
                {
                    // string customEditor = trimmed.Remove(0, ("CustomEditorForRenderPipeline ").Length);
                    // if (customEditor.EndsWith("\"\"")) customEditor = customEditor.Remove(customEditor.Length - 4, 3); // remove double quotes at the end

                    //input[index] = "CustomEditor " + (customEditor.Contains("UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInLitGUI") ? importerSettings.shaderInspector : customEditor);
                    lines[index] = "CustomEditor \"" + importerSettings.CustomEditor + "\"";
                }
                else if (trimmed.StartsWith("FallBack \"Hidden", StringComparison.Ordinal))
                {
                    lines[index] = "Fallback \"" + importerSettings.fallback + "\"";
                }



            }
        }

        private static int RemovePass(ref string[] input, int index)
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
    }
}
