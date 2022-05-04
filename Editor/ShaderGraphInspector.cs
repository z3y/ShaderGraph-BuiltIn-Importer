using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace ShaderGraphImporter
{
    public enum SurfaceType
    {
        Opaque,
        Transparent
    }

    public enum BlendingMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply
    }

    public class DefaultInspector : ShaderGUI
    {

        

        private bool _hasOverrideProperties = false;

        private bool _firstTime = true;
        private Shader _shader;

        private int _BUILTIN_Surface;
        private int _BUILTIN_Blend;
        private int _BUILTIN_AlphaClip;
        private int _BUILTIN_SrcBlend;
        private int _BUILTIN_DstBlend;
        private int _BUILTIN_ZWrite;
        private int _BUILTIN_ZTest;
        private int _BUILTIN_CullMode;
        private int _AlphaToMask;

        private static bool surfaceOptionsFoldout = true;
        private static bool surfaceInputsFoldout = true;

        private int propCount = 0;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {

            if (_firstTime || propCount != properties.Length)
            {
                propCount = properties.Length;
                _shader = (materialEditor.target as Material).shader;
                _hasOverrideProperties = Array.FindIndex(properties, x => x.name.Equals("_BUILTIN_Surface", StringComparison.Ordinal)) != -1;

                _BUILTIN_Surface = Array.FindIndex(properties, x => x.name.Equals("_BUILTIN_Surface", StringComparison.Ordinal));
                _BUILTIN_Blend = Array.FindIndex(properties, x => x.name.Equals("_BUILTIN_Blend", StringComparison.Ordinal));
                _BUILTIN_AlphaClip = Array.FindIndex(properties, x => x.name.Equals("_BUILTIN_AlphaClip", StringComparison.Ordinal));
                _BUILTIN_SrcBlend = Array.FindIndex(properties, x => x.name.Equals("_BUILTIN_SrcBlend", StringComparison.Ordinal));
                _BUILTIN_DstBlend = Array.FindIndex(properties, x => x.name.Equals("_BUILTIN_DstBlend", StringComparison.Ordinal));
                _BUILTIN_ZWrite = Array.FindIndex(properties, x => x.name.Equals("_BUILTIN_ZWrite", StringComparison.Ordinal));
                _BUILTIN_ZTest = Array.FindIndex(properties, x => x.name.Equals("_BUILTIN_ZTest", StringComparison.Ordinal));
                _BUILTIN_CullMode = Array.FindIndex(properties, x => x.name.Equals("_BUILTIN_CullMode", StringComparison.Ordinal));

                _AlphaToMask = Array.FindIndex(properties, x => x.name.Equals("_AlphaToMask", StringComparison.Ordinal));

                _firstTime = false;
            }

            EditorGUI.indentLevel++;
            if (_hasOverrideProperties)
            {
                if (surfaceOptionsFoldout = DrawHeaderFoldout(new GUIContent("Surface Options"), surfaceOptionsFoldout))
                {
                    EditorGUI.BeginChangeCheck();


                    materialEditor.ShaderProperty(properties[_BUILTIN_Surface], new GUIContent("Surface Type"));
                    var type = (SurfaceType)properties[_BUILTIN_Surface].floatValue;

                    if (type == SurfaceType.Transparent)
                    {
                        materialEditor.ShaderProperty(properties[_BUILTIN_Blend], new GUIContent("Blending Mode"));
                    }

                    materialEditor.ShaderProperty(properties[_BUILTIN_CullMode], new GUIContent("Cull"));
                    materialEditor.ShaderProperty(properties[_BUILTIN_AlphaClip], new GUIContent("Alpha Clipping"));

                    bool alphaToMaskEnabled = false;
                    if (properties[_BUILTIN_AlphaClip].floatValue == 1 && _AlphaToMask >= 0)
                    {
                        alphaToMaskEnabled = true;
                    }




                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var o in materialEditor.targets)
                        {
                            var m = (Material)o;
                            SetupMaterialRenderingMode(m, type, (BlendingMode)properties[_BUILTIN_Blend].floatValue, properties[_BUILTIN_AlphaClip].floatValue == 1, alphaToMaskEnabled);
                        }
                    }

                    EditorGUILayout.Space();

                }
            }

            if (surfaceInputsFoldout = DrawHeaderFoldout(new GUIContent("Surface Inputs"), surfaceInputsFoldout))
            {

                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];

                    if ((property.flags & MaterialProperty.PropFlags.HideInInspector) != 0)
                    {
                        continue;
                    }

                    if (property.type == MaterialProperty.PropType.Texture)
                    {
                        materialEditor.TextureProperty(property, property.displayName);
                        if ((property.flags & MaterialProperty.PropFlags.NoScaleOffset) == 0)
                        {
                        //    materialEditor.TextureScaleOffsetProperty(property);
                        }
                    }
                    else
                    {
                        materialEditor.ShaderProperty(property, new GUIContent(property.displayName));
                    }
                }
            }

            EditorGUI.indentLevel = 0;
            EditorGUILayout.Space();
            DrawSplitter();
            EditorGUILayout.Space();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
            materialEditor.LightmapEmissionProperty();
        }

        public static void SetupMaterialRenderingMode(Material material, SurfaceType surfaceType, BlendingMode blendingMode, bool alphaClipping, bool alphaToCoverage)
        {
            switch (surfaceType)
            {
                case SurfaceType.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_BUILTIN_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_BUILTIN_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_BUILTIN_ZWrite", 1);
                    material.SetInt("_AlphaToMask", 0);
                    material.renderQueue = -1;
                    material.DisableKeyword("_BUILTIN_SURFACE_TYPE_TRANSPARENT");
                    break;
                case SurfaceType.Transparent:
                    material.EnableKeyword("_BUILTIN_SURFACE_TYPE_TRANSPARENT");
                    SetupTransparentMaterial(material);
                    SetupMaterialBlendMode(material, blendingMode);
                    break;
            }
            if (alphaClipping)
            {
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt("_BUILTIN_ZWrite", 1);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                material.SetInt("_AlphaToMask", 0);
                material.EnableKeyword("_BUILTIN_AlphaClip");
                //material.EnableKeyword("_BUILTIN_ALPHATEST_ON");

                if (alphaToCoverage)
                {
                    material.SetInt("_AlphaToMask", 1);
                }
            }
            else
            {
                material.DisableKeyword("_BUILTIN_AlphaClip");
                //material.DisableKeyword("_BUILTIN_ALPHATEST_ON");
            }
        }

        public static void SetupMaterialBlendMode(Material material, BlendingMode type)
        {
            switch (type)
            {
                case BlendingMode.Alpha:
                    material.SetInt("_BUILTIN_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_BUILTIN_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendingMode.Premultiply:
                    material.EnableKeyword("_BUILTIN_ALPHAPREMULTIPLY_ON");
                    material.SetInt("_BUILTIN_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_BUILTIN_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendingMode.Additive:
                    material.SetInt("_BUILTIN_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_BUILTIN_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    break;
                case BlendingMode.Multiply:
                    material.SetInt("_BUILTIN_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_BUILTIN_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    break;
            }
        }

        private static void SetupTransparentMaterial(Material material)
        {
            material.DisableKeyword("_BUILTIN_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetInt("_BUILTIN_ZWrite", 0);
            material.SetInt("_AlphaToMask", 0);
        }

        #region CoreEditorUtils.cs
        /// <summary>Draw a header</summary>
        /// <param name="title">Title of the header</param>
        public static void DrawHeader(GUIContent title)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
        }

        public static bool DrawHeaderFoldout(GUIContent title, bool state, bool isBoxed = false)
        {
            DrawSplitter();
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);
            float xMin = backgroundRect.xMin;

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;
            foldoutRect.x = labelRect.xMin + 15 * (EditorGUI.indentLevel - 1); //fix for presset


            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;
            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));


            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }
            EditorGUI.indentLevel = previousIndent;
            return state;
        }

        /// <summary>Draw a splitter separator</summary>
        /// <param name="isBoxed">[Optional] add margin if the splitter is boxed</param>
        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (isBoxed)
            {
                rect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }
        #endregion

    }
}
