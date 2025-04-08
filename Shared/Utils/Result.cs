namespace Shared.Utils
{
    public sealed class Result<TValue, TError> where TError : class
    {
        private readonly TValue? _value;
        public TError? Error { get; }
        public bool IsSuccess { get; }

        private Result(TValue value)
        {
            Value = value;
            IsSuccess = true;
            Error = null;
        }
        private Result(TError error)
        {
            IsSuccess = false;
            Error = error ?? throw new ArgumentException("invalid error", nameof(error));
        }

        public TValue Value
        {
            get
            {
                if (IsFailure)
                {
                    throw new InvalidOperationException("there is no value for failure");
                }
                return _value!;
            }
            private init => _value = value;
        }

        public bool IsFailure => !IsSuccess;

        public static Result<TValue, TError> Success(TValue value) => new(value);
        public static Result<TValue, TError> Failure(TError error) => new(error);
    }
}
