using System.IO;
using System.Net;
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
        
    [CustomEditor(typeof(ShaderGraphScriptedImporter))]
    public class ShaderGraphScriptedImporterEditor: ScriptedImporterEditor
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
#pragma warning restore CS0649


        public bool firstTime = true;
        private static bool thirdPartyFoldout = false;

        public override void OnEnable()
        {
            base.OnEnable();
            
            var serializedProperties = typeof(ShaderGraphScriptedImporterEditor).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var field in serializedProperties)
            {
                if (field.FieldType != typeof(SerializedProperty)) continue;
                
                field.SetValue(this, serializedObject.FindProperty(field.Name));
            }
        }
        
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Paste and Import"))
            {
                var sourceFile = (ShaderGraphScriptedImporter)serializedObject.targetObject;
                var sourcePath = AssetDatabase.GetAssetPath(sourceFile);
                
                File.WriteAllText(sourcePath, GUIUtility.systemCopyBuffer);
                AssetDatabase.Refresh();
            }
            if (GUILayout.Button("View Generated Shader"))
            {
                
                var sourceFile = (ShaderGraphScriptedImporter)serializedObject.targetObject;
                var sourcePath = AssetDatabase.GetAssetPath(sourceFile);

                const string tempPath = "Temp/ShaderGraphImporterTemp.shader";
                File.WriteAllText(tempPath,Importer.ProcessShader((ShaderGraphScriptedImporter)serializedObject.targetObject, sourcePath));
                InternalEditorUtility.OpenFileAtLineExternal(tempPath, 0);
            }
            
            
            serializedObject.Update();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(shadingModel, new GUIContent("Shading Model"));
            EditorGUILayout.PropertyField(CustomEditor, new GUIContent("CustomEditor"));
            EditorGUILayout.PropertyField(fallback, new GUIContent("Fallback"));


            EditorGUILayout.Space(10);
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


            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Multicompiles", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(allowVertexLights, new GUIContent("Vertex Lights"));
            EditorGUILayout.PropertyField(lodFadeCrossfade, new GUIContent("LOD Fade Crossfade"));


            EditorGUILayout.Space(10);
            thirdPartyFoldout = EditorGUILayout.Foldout(thirdPartyFoldout, new GUIContent("Third Party"));
            if (thirdPartyFoldout)
            {
                EditorGUILayout.PropertyField(ltcgi, new GUIContent("LTCGI"));
                EditorGUILayout.PropertyField(includeAudioLink, new GUIContent("Audio Link", "Include AudioLink.cginc"));
                EditorGUILayout.PropertyField(dps, new GUIContent("DPS", "Raliv Dynamic Penetration System"));
            }


            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
            

            
        }
    }

}
