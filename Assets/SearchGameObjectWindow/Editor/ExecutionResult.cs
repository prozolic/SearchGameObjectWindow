
#if UNITY_EDITOR
using System;

namespace SearchGameObjectWindow
{
    internal struct ExecutionResult<T>
    {
        public T Value { get; }
        public bool IsOk { get; }
        public bool IsError => !this.IsOk;
        public Exception Exception { get; }

        private ExecutionResult(T value)
        {
            this.Value = value;
            this.IsOk = true;
            this.Exception = null;
        }

        private ExecutionResult(Exception exception)
        {
            this.Value = default;
            this.Exception = exception;
            this.IsOk = false;
        }

        public static ExecutionResult<T> OK(T value)
        {
            return new ExecutionResult<T>(value);
        }

        public static ExecutionResult<T> Error(Exception exception)
        {
            return new ExecutionResult<T>(exception);
        }

    }
}
#endif