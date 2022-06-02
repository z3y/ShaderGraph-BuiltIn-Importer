using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace ShaderGraphImporter
{
        
    [CustomEditor(typeof(ImporterSettings))]
    public class ImporterEditor : Editor
    {
        
#pragma warning disable CS0649
        private SerializedProperty alphaToCoverage;
        private SerializedProperty grabPass;
        private SerializedProperty grabPassName;
        private SerializedProperty allowVertexLights;
        private SerializedProperty lodFadeCrossfade;
        private SerializedProperty bicubicLightmap;
        private SerializedProperty bakeryFeatures;
        private SerializedProperty specularOcclusion;
        private SerializedProperty ltcgi;
        private SerializedProperty dps;
        private SerializedProperty stencil;
        private SerializedProperty includeAudioLink;
        private SerializedProperty CustomEditor;
        private SerializedProperty fallback;
        private SerializedProperty cgInclude;
        private SerializedProperty shadingModel;
        private SerializedProperty defaultMapsNames;
        private SerializedProperty defaultMaps;
        private SerializedProperty fallbackTags;
        private SerializedProperty VRCFallback;
#pragma warning restore CS0649
        
        public bool firstTime = true;
        private static bool thirdPartyFoldout = false;

        public override void OnInspectorGUI()
        {
            if (firstTime)
            {
                var serializedProperties = typeof(ImporterEditor).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

                foreach (var field in serializedProperties)
                {
                    if (field.FieldType != typeof(SerializedProperty)) continue;
                
                    field.SetValue(this, serializedObject.FindProperty(field.Name));
                }

                firstTime = false;
            }
            
            
            if (GUILayout.Button("Paste and Import"))
            {
                var sourceFile = (ImporterSettings)serializedObject.targetObject;
                Importer.ImportShader(sourceFile, GUIUtility.systemCopyBuffer);
            }

            serializedObject.Update();
            
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.PropertyField(shadingModel, new GUIContent("Shading Model"));
                EditorGUILayout.PropertyField(CustomEditor, new GUIContent("CustomEditor"));
                EditorGUILayout.PropertyField(fallback, new GUIContent("Fallback"));
            }


            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Shader Features", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(alphaToCoverage, new GUIContent("Alpha To Coverage"));
                EditorGUILayout.PropertyField(bicubicLightmap, new GUIContent("Bicubic Lightmap"));
                EditorGUILayout.PropertyField(bakeryFeatures, new GUIContent("Bakery"));
                EditorGUILayout.PropertyField(specularOcclusion, new GUIContent("Specular Occlusion"));
                EditorGUILayout.PropertyField(grabPass, new GUIContent("Grab Pass"));
                if (grabPass.boolValue)
                {
                    EditorGUILayout.PropertyField(grabPassName, new GUIContent("Grab Pass Name"));
                }

                EditorGUI.indentLevel++;
                thirdPartyFoldout = EditorGUILayout.Foldout(thirdPartyFoldout, new GUIContent("Third Party"));
                if (thirdPartyFoldout)
                {
                    EditorGUILayout.PropertyField(ltcgi, new GUIContent("LTCGI"));
                    EditorGUILayout.PropertyField(includeAudioLink, new GUIContent("Audio Link", "Include AudioLink.cginc"));
                    EditorGUILayout.PropertyField(dps, new GUIContent("DPS", "Raliv Dynamic Penetration System"));
                }
                EditorGUI.indentLevel--;
            }


            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Multicompiles", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(allowVertexLights, new GUIContent("Vertex Lights"));
                EditorGUILayout.PropertyField(lodFadeCrossfade, new GUIContent("LOD Fade Crossfade"));
            }

            EditorGUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("VRChat Fallback", EditorStyles.boldLabel);
                var fallbackType = fallbackTags.FindPropertyRelative("type");
                var fallBackMode = fallbackTags.FindPropertyRelative("mode");
                var isDoubleSided = fallbackTags.FindPropertyRelative("doubleSided");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(fallbackType, new GUIContent("Type"));
                EditorGUILayout.PropertyField(fallBackMode, new GUIContent("Mode"));
                EditorGUILayout.PropertyField(isDoubleSided, new GUIContent("Double Sided"));
                if (EditorGUI.EndChangeCheck())
                {
                    var tags = new VRCFallbackTags()
                    {
                        doubleSided = isDoubleSided.boolValue,
                        type = (VRCFallbackTags.ShaderType)fallbackType.enumValueIndex,
                        mode = (VRCFallbackTags.ShaderMode)fallBackMode.enumValueIndex,
                    };
                    VRCFallback.stringValue = VRCFallbackTags.GetTag(tags);
                }
                if (!string.IsNullOrEmpty(VRCFallback.stringValue))
                    EditorGUILayout.LabelField(VRCFallback.stringValue, EditorStyles.boldLabel);
            }

            serializedObject.ApplyModifiedProperties();

        }
    }

}
