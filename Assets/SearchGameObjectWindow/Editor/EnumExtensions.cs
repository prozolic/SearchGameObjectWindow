
#if UNITY_EDITOR

using Codice.Client.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SearchGameObjectWindow
{
    internal static class EnumExtensions
    {
        public static ExecutionResult<AliasNameAttribute> ToAlias<T>(this T value) where T : Enum
        {
            var fieldInfo = typeof(T).GetField(value.ToString());
            if (fieldInfo.GetCustomAttribute<AliasNameAttribute>() is AliasNameAttribute atr)
            {
                return ExecutionResult<AliasNameAttribute>.OK(atr);
            }
            return ExecutionResult<AliasNameAttribute>.Error(new NotImplementedException($@"{nameof(AliasNameAttribute)} is not implemented."));
        }

        public static string ToAliasName<T>(this T value) where T : Enum
        {
            var result = value.ToAlias();
            return result.IsOk ? result.Value.AliasName : value.ToString() ?? string.Empty;
        }

        public static T CastInDefined<T>(int value) where T : Enum
        {
            if (Enum.IsDefined(typeof(T), value))
            {
                return (T)Enum.ToObject(typeof(T), value);
            }
            return default;
        }

        public static IEnumerable<T> GetValues<T>() where T : Enum
        {
            foreach(T e in Enum.GetValues(typeof(T)))
            {
                yield return e;
            }
        }
    }
}
#endif