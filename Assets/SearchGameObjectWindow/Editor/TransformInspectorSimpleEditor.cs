#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

namespace SearchGameObjectWindow.SimpleEditor
{
    // UnityEditor.TransformInspectorをベースに実装
    // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/TransformInspector.cs#L26
    [CanEditMultipleObjects]
    internal class TransformInspectorSimpleEditor : Editor
    {
        private SerializedProperty _position;
        private SerializedProperty _scale;
        private object _rotationGUI;
        private MethodInfo _onEnableForRotationGUI;
        private MethodInfo _rotationFieldForRotationGUI;

        public void OnEnable()
        {
            _position = serializedObject.FindProperty("m_LocalPosition");
            _scale = serializedObject.FindProperty("m_LocalScale");

            var transformRotationGUIType = Assembly.GetAssembly(typeof(EditorApplication)).GetType("UnityEditor.TransformRotationGUI");
            _rotationGUI = Activator.CreateInstance(transformRotationGUIType);
            _onEnableForRotationGUI = transformRotationGUIType.GetMethod("OnEnable");

            // 引数リスト指定（引数なし）で取得する。
            _rotationFieldForRotationGUI = transformRotationGUIType.GetMethod("RotationField", Array.Empty<Type>());

            _onEnableForRotationGUI?.Invoke(_rotationGUI, new object[]
            {
                serializedObject.FindProperty("m_LocalRotation"),
                EditorGUIUtility.TrTextContent("Rotation")
            });
        }

        public override void OnInspectorGUI()
        {
            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
            }
            serializedObject.Update();

            EditorGUILayout.PropertyField(_position, EditorGUIUtility.TrTextContent("Position"));
            _rotationFieldForRotationGUI?.Invoke(_rotationGUI, null);
            EditorGUILayout.PropertyField(_scale, EditorGUIUtility.TrTextContent("Scale"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif