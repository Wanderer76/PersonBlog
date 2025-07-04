﻿namespace Shared.Utils
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

        public static implicit operator Result<TValue, TError>(TValue value) => Success(value);

        public static implicit operator Result<TValue, TError>(TError error) => Failure(error);
        public static Result<TValue, TError> From<TException>(
            Func<TValue> func,
            Func<TException, TError> errorFactory)
            where TException : Exception
        {
            try { return Success(func()); }
            catch (TException ex) { return Failure(errorFactory(ex)); }
        }

    }

    public sealed class Result<TValue>
    {
        private readonly Result<TValue, Error> _result;

        private Result(TValue value)
        {
            _result = Result<TValue, Error>.Success(value);
        }
        private Result(Error error)
        {
            _result = Result<TValue, Error>.Failure(error);
        }

        public TValue Value
        {
            get => _result.Value;
        }
        public Error? Error
        {
            get => _result.Error;
        }
        public bool IsFailure => _result.IsFailure;
        public bool ISSuccess => !IsFailure;

        public static Result<TValue> Success(TValue value) => new(value);
        public static Result<TValue> Failure(Error error) => new(error);

        public static implicit operator Result<TValue>(TValue value)
            => Success(value);
        public static implicit operator Result<TValue>(Error error)
            => Failure(error);

    }
}
