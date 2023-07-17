#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

namespace SearchGameObjectWindow.SimpleEditor
{
    // UnityEditor.TransformInspector���x�[�X�Ɏ���
    // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/TransformInspector.cs#L26
    [CanEditMultipleObjects]
    internal class TransformInspectorSimpleEditor : Editor
    {
        private SerializedProperty _position;
        private SerializedProperty _scale;
        private object _rotationGUI;
        private static Type _transformRotationGUIType;
        private static SafetyMethodInfo _onEnableForRotationGUI;
        private static SafetyMethodInfo _rotationFieldForRotationGUI;

        public void OnEnable()
        {
            this.Initialize();

            _position = serializedObject.FindProperty("m_LocalPosition");
            _scale = serializedObject.FindProperty("m_LocalScale");
            _rotationGUI = Activator.CreateInstance(_transformRotationGUIType);

            _onEnableForRotationGUI.Invoke(_rotationGUI, new object[]
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
            _rotationFieldForRotationGUI.Invoke(_rotationGUI, null);
            EditorGUILayout.PropertyField(_scale, EditorGUIUtility.TrTextContent("Scale"));

            serializedObject.ApplyModifiedProperties();
        }

        private void Initialize()
        {
            _transformRotationGUIType ??= Assembly.GetAssembly(typeof(EditorApplication)).GetType("UnityEditor.TransformRotationGUI");
            _onEnableForRotationGUI ??= new SafetyMethodInfo(_transformRotationGUIType, "OnEnable");
            // �������X�g�w��i�����Ȃ��j�Ŏ擾����B
            _rotationFieldForRotationGUI ??= new SafetyMethodInfo(_transformRotationGUIType, "RotationField", Array.Empty<Type>());
        }
    }
}
#endif