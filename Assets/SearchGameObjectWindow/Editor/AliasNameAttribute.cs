
#if UNITY_EDITOR

using System;

namespace SearchGameObjectWindow
{
    internal class AliasNameAttribute : Attribute
    {
        public string AliasName { get; }

        public AliasNameAttribute(string aliasName)
        {
            this.AliasName = aliasName ?? string.Empty;
        }
    }
}
#endif