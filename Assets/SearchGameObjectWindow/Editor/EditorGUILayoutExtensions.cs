
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
        private static readonly GUIStyle LargeButton = new GUIStyle("LargeButton");

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

        public static int TabControl(int tabPosition, IEnumerable<string> tabNames)
        {
            var tabcontents = new List<GUIContent>();
            foreach(var tab in tabNames)
            {
                tabcontents.Add(EditorGUIUtility.TrTextContent(tab));
            }
            return GUILayout.Toolbar(tabPosition, tabcontents.ToArray(), LargeButton, GUI.ToolbarButtonSize.FitToContents);
        }

        public static bool FoldoutInspectorSimpleHeader(bool foldout, GUIContent content, string title)
        {
            using (var headerScope = new EditorGUILayout.HorizontalScope())
            {
                // EditorGUILayout.BeginFoldoutHeaderGroupの実装をベースに調整
                var style = new GUIStyle(EditorStyles.foldoutHeader);
                style.font = new GUIStyle(EditorStyles.label).font;
                style.fixedHeight = 18;

                Rect position = GUILayoutUtility.GetRect(16, 22f, style);
                Rect rect = default(Rect);
                rect.x = position.xMax - (float)style.padding.right - 16f;
                rect.y = position.y + (float)style.padding.top;
                rect.size = Vector2.one * 16f;
                int controlID = GUIUtility.GetControlID("FoldoutHeader".GetHashCode(), FocusType.Keyboard, position);

                if (Event.current.type == EventType.KeyDown && GUIUtility.keyboardControl == controlID)
                {
                    KeyCode keyCode = Event.current.keyCode;
                    if ((keyCode == KeyCode.LeftArrow && foldout) || (keyCode == KeyCode.RightArrow && !foldout))
                    {
                        foldout = !foldout;
                        GUI.changed = true;
                        Event.current.Use();
                    }
                }
                else
                {
                    foldout = FoldoutHeaderCore(position, controlID, foldout, content, title, style);
                }

                return foldout;
            }
        }

        private static bool FoldoutHeaderCore(Rect position, int id, bool value, GUIContent content,string title, GUIStyle style)
        {
            EventType type = Event.current.type;
            bool mouseDowning = type == EventType.MouseDown && Event.current.button != 0;
            if (mouseDowning)
            {
                Event.current.type = EventType.Ignore;
            }

            // GUI.DoControlメソッドの内部実装をベース微調整
            bool result = FoldoutHeaderEventFilter.FilterEvent(position, id, value, position.Contains(Event.current.mousePosition), content, title, style);

            if (mouseDowning)
            {
                Event.current.type = type;
            }
            else if (Event.current.type != type)
            {
                GUIUtility.keyboardControl = id;
            }

            return result;
        }

        private static class FoldoutHeaderEventFilter
        {
            public static bool FilterEvent(Rect position, int id, bool on, bool hover, GUIContent content, string title, GUIStyle style)
            {
                // GUI.DoControlメソッドの内部実装をベースに微調整
                Event current = Event.current;
                var contains = position.Contains(current.mousePosition);
                switch (current.type)
                {
                    case EventType.Repaint:
                        FilterRepaint(current, position, id, on, contains, content, title, style);
                        break;
                    case EventType.MouseDown:
                        FilterMouseDown(current, position, id, on, contains, content, style);
                        break;
                    case EventType.KeyDown:
                        if (FilterKeyDown(current, position, id, on, contains, content, style)) return !on;
                        break;
                    case EventType.MouseUp:
                        if (FilterMouseUp(current, position, id, on, contains, content, style)) return !on;
                        break;
                    case EventType.MouseDrag:
                        FilterMouseDrag(current, position, id, on, contains, content, style);
                        break;
                }

                return on;
            }

            internal static void FilterRepaint(Event current, Rect position, int id, bool on, bool hover, GUIContent content, string title, GUIStyle style)
            {
                style.Draw(position, content, id, on, hover);
                var titleContent = new GUIContent(title);
                var labelRect = new Rect(position.x + 60, position.y, style.CalcSize(titleContent).x, 18);
                EditorStyles.boldLabel.Draw(labelRect, titleContent, hover, GUIUtility.hotControl == id, on, GUIUtilityExtensions.HasKeyFocus(id));
            }

            internal static void FilterMouseDown(Event current, Rect position, int id, bool on, bool hover, GUIContent content, GUIStyle style)
            {
                if (HitTest(position, current))
                {
                    GUIExtensions.GrabMouseControl(id);
                    current.Use();
                }
            }

            internal static bool FilterKeyDown(Event current, Rect position, int id, bool on, bool hover, GUIContent content, GUIStyle style)
            {
                bool flag = current.alt || current.shift || current.command || current.control;
                if ((current.keyCode == KeyCode.Space || current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter) && !flag && GUIUtility.keyboardControl == id)
                {
                    current.Use();
                    GUI.changed = true;
                    return true;
                }
                return false;
            }

            internal static bool FilterMouseUp(Event current, Rect position, int id, bool on, bool hover, GUIContent content, GUIStyle style)
            {
                if (GUIExtensions.HasMouseControl(id))
                {
                    GUIExtensions.ReleaseMouseControl();
                    current.Use();
                    if (HitTest(position, current))
                    {
                        GUI.changed = true;
                        return true;
                    }
                }

                return false;
            }

            internal static void FilterMouseDrag(Event current, Rect position, int id, bool on, bool hover, GUIContent content, GUIStyle style)
            {
                if (GUIExtensions.HasMouseControl(id))
                {
                    current.Use();
                }
            }

            private static bool HitTest(Rect rect, Event evt) => HitTest(rect, evt.mousePosition);
            private static bool HitTest(Rect rect, Vector2 point) => point.x >= rect.xMin && point.x < rect.xMax && point.y >= rect.yMin && point.y < rect.yMax;
        }

    }

}

#endif