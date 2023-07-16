#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SearchGameObjectWindow
{
    public static class GUIExtensions
    {
        private static Dictionary<string, MethodInfo> _guiInternalMethods = new ();

        public static void GrabMouseControl(int id)
        {
            var grabMouseControl = GetOrCreateGUIInternalMethodInfo("GrabMouseControl");
            grabMouseControl?.Invoke(null, new object[] { id });
        }

        public static bool HasMouseControl(int id)
        {
            var hasMouseControl = GetOrCreateGUIInternalMethodInfo("HasMouseControl");
            return (bool)hasMouseControl?.Invoke(null, new object[] { id });
        }

        public static void ReleaseMouseControl()
        {
            var releaseMouseControl = GetOrCreateGUIInternalMethodInfo("ReleaseMouseControl");
            releaseMouseControl?.Invoke(null, null);
        }

        private static MethodInfo GetOrCreateGUIInternalMethodInfo(string methodName)
        {
            if (!_guiInternalMethods.ContainsKey(methodName))
            {
                _guiInternalMethods[methodName] = typeof(GUI).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            }
            return _guiInternalMethods[methodName];
        }
    }
}

#endif