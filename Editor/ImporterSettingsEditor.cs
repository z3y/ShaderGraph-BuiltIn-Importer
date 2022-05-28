using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ShaderGraphImporter
{
    [CustomEditor(typeof(ImporterSettings))]
    public class SettingsEditor : Editor
    {
        private ImporterSettings _settings;

        private SerializedProperty _elements;
        private Vector2 _scrollPosition;
        private ReorderableList _reorderableList;

        private bool _firstTime = true;
        public override void OnInspectorGUI()
        {
            _settings = (ImporterSettings)target;

            if (_firstTime)
            {
                _elements = serializedObject.FindProperty("cgInclude");
                _reorderableList = new ReorderableList(serializedObject, _elements)
                {
                    drawElementCallback = DrawListItems,
                    drawHeaderCallback = DrawHeader
                };
                _firstTime = false;
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

            EditorGUILayout.LabelField("Features", EditorStyles.boldLabel);
            GUILayout.BeginVertical("box");
            _settings.shadingModel = (ShadingModel)EditorGUILayout.EnumPopup("Shading Model", _settings.shadingModel);
            _settings.alphaToCoverage = EditorGUILayout.ToggleLeft("Alpha To Coverage", _settings.alphaToCoverage);
            _settings.bakeryFeatures = EditorGUILayout.ToggleLeft("Bakery Features", _settings.bakeryFeatures);
            _settings.bicubicLightmap = EditorGUILayout.ToggleLeft("Bicubic Lightmap", _settings.bicubicLightmap);
            _settings.specularOcclusion = EditorGUILayout.ToggleLeft("Specular Occlusion", _settings.specularOcclusion);
            //_settings.stencil = EditorGUILayout.ToggleLeft("Stencil", _settings.stencil);
            _settings.ltcgi = EditorGUILayout.ToggleLeft("LTCGI", _settings.ltcgi);
            _settings.grabPass = EditorGUILayout.ToggleLeft("GrabPass", _settings.grabPass);
            if (_settings.grabPass) _settings.grabPassName = EditorGUILayout.TextField("GrabPass Name", _settings.grabPassName);
            
            EditorGUI.BeginChangeCheck();
            _settings.dps = EditorGUILayout.ToggleLeft(new GUIContent("DPS", "Raliv Dynamic Penetration System"), _settings.dps);
            if (EditorGUI.EndChangeCheck())
            {

                bool dpsIncluded = Directory.Exists("Assets/RalivDynamicPenetrationSystem");
                if (!dpsIncluded)
                {
                    _settings.dps = false;
                    Debug.LogError("RalivDynamicPenetrationSystem not found");
                }
            }
            _settings.includeAudioLink = EditorGUILayout.ToggleLeft(new GUIContent("Include AudioLink", "Only includes AudioLink.cginc so it can be used with graph"), _settings.includeAudioLink);

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


            // using (new GUILayout.HorizontalScope())
            // {
            //     _settings.shaderGraphProjectPath = EditorGUILayout.TextField("ShaderGraph Project", _settings.shaderGraphProjectPath);
            //     if (GUILayout.Button("Select"))
            //     {
            //         _settings.shaderGraphProjectPath = EditorUtility.OpenFolderPanel("ShaderGraph Project Assets Path", "", "");
            //     }
            // }



            GUILayout.EndVertical();
            _reorderableList.DoLayoutList();

            EditorGUILayout.Space();


            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select"))
                {
                    var shaderObject = AssetDatabase.LoadAssetAtPath(_settings.importPath + _settings.fileName + ".shader", typeof(Shader));
                    if (shaderObject is null) return;
                    EditorGUIUtility.PingObject(shaderObject);
                }
                if (GUILayout.Button("Apply"))
                {
                    Importer.ImportShader(ref _settings);
                }
            }


            EditorGUILayout.Space();
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
            var element = _elements.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect,element,GUIContent.none);
        }

        static void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "HLSLINCLUDE");
        }
    }
}