
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SearchGameObjectWindow
{
    internal static class EditorGUILayoutExtensions
    {
        private static readonly GUIStyle _fontSizeStyle = new GUIStyle(EditorStyles.textField);
        private static readonly SafetyFieldInfo _sLastRectInfo = new SafetyFieldInfo(typeof(EditorGUILayout), "s_LastRect");

        public static ExecutionResult<VoidStructure> DropShadowLabel(string label)
        {
            // EditorGUILayoutクラスにDropShadowLabelが存在しないため、独自定義
            // EditorGUILayout.LabelFieldの内部実装をベースに作成

            var rect = EditorGUILayout.GetControlRect(hasLabel: true, 18f, EditorStyles.layerMaskField, null);
            var result = _sLastRectInfo.SetValue(null, rect);
            if (result.IsError) return result;

            EditorGUI.DropShadowLabel(rect, label);
            return result;
        }

        public static string TextFieldWithVariableFontSize(string label, string text, int fontSize)
        {
            var inputTextStyle = _fontSizeStyle;
            inputTextStyle.fontSize = fontSize;
            return EditorGUILayout.TextField(label, text, inputTextStyle, GUILayout.Height(inputTextStyle.lineHeight * 1.2f));
        }

        private static readonly GUIStyle LargeButton = new GUIStyle("LargeButton");

        public static int TabControl(int tabPosition, IEnumerable<string> tabNames)
        {
            var tabcontents = new List<GUIContent>();
            foreach(var tab in tabNames)
            {
                tabcontents.Add(EditorGUIUtility.TrTextContent(tab));
            }
            return GUILayout.Toolbar(tabPosition, tabcontents.ToArray(), LargeButton, GUI.ToolbarButtonSize.FitToContents);
        }
    }

}

#endif