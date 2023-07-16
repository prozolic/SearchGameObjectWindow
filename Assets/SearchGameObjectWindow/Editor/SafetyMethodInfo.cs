
#if UNITY_EDITOR
using System;
using System.Reflection;

namespace SearchGameObjectWindow
{
    internal sealed class SafetyMethodInfo
    {
        public MethodInfo RawMethodInfo { get; }
        public Type ClassType { get; }
        public bool IsStaticMethod => this.RawMethodInfo?.IsStatic ?? false;
        public bool IsInstanceMethod => !this.RawMethodInfo?.IsStatic ?? false;

        public SafetyMethodInfo(Type classType, string methodName)
        {
            this.ClassType = classType;
            this.RawMethodInfo = this.ClassType?.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);
        }

        public SafetyMethodInfo(Type classType, string methodName, Type[] args)
        {
            this.ClassType = classType;
            this.RawMethodInfo = this.ClassType?.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static,
                null, args, null);
        }

        public ExecutionResult<T> Invoke<T>(object instance, object[] parameters = null)
        {
            if (this.RawMethodInfo == null) return ExecutionResult<T>.Error(new Exception("メソッド情報が取得できていません"));

            try
            {
                return ExecutionResult<T>.OK((T)this.InvokeDirect(instance, parameters));
            }
            catch (Exception e)
            {
                return ExecutionResult<T>.Error(e);
            }
        }

        public ExecutionResult<object> Invoke(object instance, object[] parameters = null)
        {
            if (this.RawMethodInfo == null) return ExecutionResult<object>.Error(new Exception("メソッド情報が取得できていません"));

            try
            {
                return ExecutionResult<object>.OK(this.InvokeDirect(instance, parameters));
            }
            catch (Exception e)
            {
                return ExecutionResult<object>.Error(e);
            }
        }

        private object InvokeDirect(object instance, object[] parameters)
        {
            return this.RawMethodInfo.Invoke(instance, parameters);
        }
    }
}

#endif
