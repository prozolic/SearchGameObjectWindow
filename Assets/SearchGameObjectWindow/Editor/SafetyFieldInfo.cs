
#if UNITY_EDITOR
using System;
using System.Reflection;

namespace SearchGameObjectWindow
{
    internal sealed class SafetyFieldInfo
    {
        public FieldInfo RawFieldInfo { get; }
        public Type ClassType { get; }
        public bool IsStaticField => this.RawFieldInfo?.IsStatic ?? false;
        public bool IsInstanceField => !this.RawFieldInfo?.IsStatic ?? false;

        public SafetyFieldInfo(Type classType, string fieldName)
        {
            this.ClassType = classType;
            this.RawFieldInfo = this.ClassType?.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.GetField | BindingFlags.SetField);
        }

        public ExecutionResult<VoidStructure> SetValue(object instance, object fieldValue)
        {
            if (this.RawFieldInfo == null) return ExecutionResult<VoidStructure>.Error(new Exception("フィールド情報が取得できていません"));

            try
            {
                this.SetValueDirect(instance, fieldValue);
            }
            catch (Exception e)
            {
                return ExecutionResult<VoidStructure>.Error(e);
            }

            return ExecutionResult<VoidStructure>.OK(VoidStructure.Value);
        }

        public ExecutionResult<T> GetValue<T>(object instance)
        {
            if (this.RawFieldInfo == null) return ExecutionResult<T>.Error(new Exception("フィールド情報が取得できていません"));

            try
            {
                return ExecutionResult<T>.OK((T)GetValueDirect(instance));
            }
            catch (Exception e)
            {
                return ExecutionResult<T>.Error(e);
            }
        }

        public ExecutionResult<object> GetValue(object instance)
        {
            if (this.RawFieldInfo == null) return ExecutionResult<object>.Error(new Exception("フィールド情報が取得できていません"));

            try
            {
                return ExecutionResult<object>.OK(this.GetValueDirect(instance));
            }
            catch (Exception e)
            {
                return ExecutionResult<object>.Error(e);
            }
        }

        private void SetValueDirect(object instance, object fieldValue)
        {
            this.RawFieldInfo.SetValue(instance, fieldValue);
        }

        private object GetValueDirect(object instance)
        {
            return this.RawFieldInfo.GetValue(instance);
        }

    }

}

#endif