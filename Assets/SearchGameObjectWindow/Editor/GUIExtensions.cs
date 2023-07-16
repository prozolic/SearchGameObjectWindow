#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SearchGameObjectWindow
{
    public static class GUIExtensions
    {
        private static Dictionary<string, SafetyMethodInfo> _guiInternalMethodCache = new ();

        public static void GrabMouseControl(int id)
        {
            var grabMouseControl = GetOrCreateGUIInternalMethodInfo("GrabMouseControl");
            grabMouseControl.Invoke(null, new object[] { id });
        }

        public static bool HasMouseControl(int id)
        {
            var hasMouseControl = GetOrCreateGUIInternalMethodInfo("HasMouseControl");
            var result = hasMouseControl.Invoke<bool>(null, new object[] { id });
            return result.Value;
        }

        public static void ReleaseMouseControl()
        {
            var releaseMouseControl = GetOrCreateGUIInternalMethodInfo("ReleaseMouseControl");
            releaseMouseControl.Invoke(null, null);
        }

        private static SafetyMethodInfo GetOrCreateGUIInternalMethodInfo(string methodName)
        {
            if (!_guiInternalMethodCache.ContainsKey(methodName))
            {
                _guiInternalMethodCache[methodName] = new SafetyMethodInfo(typeof(GUI), methodName);
            }
            return _guiInternalMethodCache[methodName];
        }
    }
}

#endif