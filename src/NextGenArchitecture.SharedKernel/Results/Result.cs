namespace NextGenArchitecture.SharedKernel.Results;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// This pattern helps avoid exceptions for expected failure scenarios and provides explicit error handling.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The error message if the operation failed.</param>
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success()
    {
        return new Result(true, null);
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty.", nameof(error));

        return new Result(false, error);
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A successful result with a value.</returns>
    public static Result<T> Success<T>(T value)
    {
        return new Result<T>(value, true, null);
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure<T>(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty.", nameof(error));

        return new Result<T>(default, false, error);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The current result for method chaining.</returns>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
            action();

        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    /// <param name="action">The action to execute with the error message.</param>
    /// <returns>The current result for method chaining.</returns>
    public Result OnFailure(Action<string> action)
    {
        if (IsFailure && Error != null)
            action(Error);

        return this;
    }

    /// <summary>
    /// Transforms the result using the provided functions.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="onSuccess">Function to execute on success.</param>
    /// <param name="onFailure">Function to execute on failure.</param>
    /// <returns>The transformed result.</returns>
    public T Match<T>(Func<T> onSuccess, Func<string, T> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error!);
    }

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>A string that represents the current result.</returns>
    public override string ToString()
    {
        return IsSuccess ? "Success" : $"Failure: {Error}";
    }
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The error message if the operation failed.</param>
    internal Result(T? value, bool isSuccess, string? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when trying to access value of a failed result.</exception>
    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException("Cannot access value of a failed result.");

            return _value!;
        }
    }

    /// <summary>
    /// Gets the value if successful, otherwise returns the default value.
    /// </summary>
    /// <param name="defaultValue">The default value to return on failure.</param>
    /// <returns>The value or default value.</returns>
    public T GetValueOrDefault(T defaultValue = default!)
    {
        return IsSuccess ? _value! : defaultValue;
    }

    /// <summary>
    /// Executes an action with the value if the result is successful.
    /// </summary>
    /// <param name="action">The action to execute with the value.</param>
    /// <returns>The current result for method chaining.</returns>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
            action(_value!);

        return this;
    }

    /// <summary>
    /// Transforms the value if the result is successful.
    /// </summary>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="func">The transformation function.</param>
    /// <returns>A new result with the transformed value.</returns>
    public Result<TOut> Map<TOut>(Func<T, TOut> func)
    {
        return IsSuccess ? Success(func(_value!)) : Failure<TOut>(Error!);
    }

    /// <summary>
    /// Chains another operation that returns a result.
    /// </summary>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="func">The function that returns a result.</param>
    /// <returns>The result of the chained operation.</returns>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> func)
    {
        return IsSuccess ? func(_value!) : Failure<TOut>(Error!);
    }

    /// <summary>
    /// Transforms the result using the provided functions.
    /// </summary>
    /// <typeparam name="TOut">The type of the result value.</typeparam>
    /// <param name="onSuccess">Function to execute on success with the value.</param>
    /// <param name="onFailure">Function to execute on failure with the error.</param>
    /// <returns>The transformed result.</returns>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure)
    {
        return IsSuccess ? onSuccess(_value!) : onFailure(Error!);
    }

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A successful result containing the value.</returns>
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>A string that represents the current result.</returns>
    public override string ToString()
    {
        return IsSuccess ? $"Success: {_value}" : $"Failure: {Error}";
    }
}