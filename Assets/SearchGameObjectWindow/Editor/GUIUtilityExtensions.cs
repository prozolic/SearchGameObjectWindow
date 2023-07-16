#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SearchGameObjectWindow
{
    public static class GUIUtilityExtensions
    {
        private static Dictionary<string, MethodInfo> _guiUtilityInternalMethods = new ();

        public static bool HasKeyFocus(int controlId)
        {
            var grabMouseControl = GetOrCreateGUIGUIUtilityInternalMethodInfo("HasKeyFocus");
            return (bool)grabMouseControl?.Invoke(null, new object[] { controlId });
        }

        private static MethodInfo GetOrCreateGUIGUIUtilityInternalMethodInfo(string methodName)
        {
            if (!_guiUtilityInternalMethods.ContainsKey(methodName))
            {
                _guiUtilityInternalMethods[methodName] = typeof(GUIUtility).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            }
            return _guiUtilityInternalMethods[methodName];
        }
    }
}

#endif