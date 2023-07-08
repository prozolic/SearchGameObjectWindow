#if UNITY_EDITOR
using System;
using UnityEngine;

namespace SearchGameObjectWindow
{
    internal static class RectExtensions
    {
        public static Rect Union(this Rect value, Rect r)
        {
            var xMin = Math.Min(value.x, r.x);
            var xMax = Math.Max(value.x + value.width, r.x + r.width);
            var yMin = Math.Min(value.y, r.y);
            var yMax = Math.Max(value.y + value.height, r.y + r.height);

            return new Rect(xMin, yMin, xMax, yMax);
        }
    }
}
#endif
