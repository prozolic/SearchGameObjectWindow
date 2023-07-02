#if UNITY_EDITOR
using System;
using UnityEngine;

namespace SearchGameObjectWindow
{
    public sealed class TempGUIbackgroundColorScope : IDisposable
    {
        private Color _tempColor;

        public TempGUIbackgroundColorScope(Color backGroundColor)
        {
            _tempColor = GUI.backgroundColor;
            GUI.backgroundColor = backGroundColor;
        }

        public void Dispose()
        {
            GUI.backgroundColor = _tempColor;
        }
    }
}

#endif