
#if UNITY_EDITOR
using UnityEditor;

namespace SearchGameObjectWindow
{
    internal static class EditorGUILayoutExtensions
    {
        public static ExecutionResult<VoidStructure> DropShadowLabel(string label)
        {
            // EditorGUILayout�N���X��DropShadowLabel�����݂��Ȃ����߁A�Ǝ���`
            // EditorGUILayout.LabelField�̓����������x�[�X�ɍ쐬

            var rect = EditorGUILayout.GetControlRect(hasLabel: true, 18f, EditorStyles.layerMaskField, null);
            var field = new SafetyFieldInfo(typeof(EditorGUILayout), "s_LastRect");
            var result = field.SetValue(null, rect);
            if (result.IsError) return result;

            EditorGUI.DropShadowLabel(rect, label);
            return result;
        }
    }

}

#endif