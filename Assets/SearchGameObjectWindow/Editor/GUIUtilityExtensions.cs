#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SearchGameObjectWindow
{
    public static class GUIUtilityExtensions
    {
        private static Dictionary<string, SafetyMethodInfo> _guiUtilityInternalMethodCache = new ();

        public static bool HasKeyFocus(int controlId)
        {
            var grabMouseControl = GetOrCreateGUIGUIUtilityInternalMethodInfo("HasKeyFocus");
            var result = grabMouseControl.Invoke<bool>(null, new object[] { controlId });
            return result.Value;
        }

        private static SafetyMethodInfo GetOrCreateGUIGUIUtilityInternalMethodInfo(string methodName)
        {
            if (!_guiUtilityInternalMethodCache.ContainsKey(methodName))
            {
                _guiUtilityInternalMethodCache[methodName] = new SafetyMethodInfo(typeof(GUIUtility), methodName);
            }
            return _guiUtilityInternalMethodCache[methodName];
        }
    }
}

#endif