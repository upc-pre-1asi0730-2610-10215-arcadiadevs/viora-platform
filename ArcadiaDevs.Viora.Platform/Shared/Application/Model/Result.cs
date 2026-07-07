namespace ArcadiaDevs.Viora.Platform.Shared.Application.Model;

/// <summary>
/// Represents the outcome of an operation that can succeed with a value or fail with an error.
/// </summary>
/// <typeparam name="TValue">The type of the successful result value.</typeparam>
/// <typeparam name="TError">The type of the error information.</typeparam>
public abstract record Result<TValue, TError>
{
    /// <summary>
    /// Represents a successful operation outcome.
    /// </summary>
    /// <param name="Value">The resulting value from the successful operation.</param>
    public sealed record Success(TValue Value) : Result<TValue, TError>;

    /// <summary>
    /// Represents a failed operation outcome.
    /// </summary>
    /// <param name="Error">The error information from the failed operation.</param>
    public sealed record Failure(TError Error) : Result<TValue, TError>;

    /// <summary>
    /// Determines whether the result represents a successful outcome.
    /// </summary>
    public bool IsSuccess => this is Success;

    /// <summary>
    /// Determines whether the result represents a failed outcome.
    /// </summary>
    public bool IsFailure => this is Failure;

    /// <summary>
    /// Applies a transformation function to the success value if the result is successful.
    /// </summary>
    /// <typeparam name="TNext">The type of the next result value.</typeparam>
    /// <param name="onSuccess">Function to apply if successful.</param>
    /// <returns>A new Result with the transformed value, or the current Failure.</returns>
    public Result<TNext, TError> Map<TNext>(Func<TValue, TNext> onSuccess) =>
        this switch
        {
            Success s => new Result<TNext, TError>.Success(onSuccess(s.Value)),
            Failure f => new Result<TNext, TError>.Failure(f.Error),
            _ => throw new InvalidOperationException("Unknown Result type")
        };

    /// <summary>
    /// Chains a dependent operation that itself returns a <see cref="Result{TNext, TError}"/>,
    /// short-circuiting on the current Failure instead of nesting it.
    /// </summary>
    /// <typeparam name="TNext">The type of the next result value.</typeparam>
    /// <param name="onSuccess">Function to apply if successful; returns the next Result.</param>
    /// <returns>The Result returned by <paramref name="onSuccess"/>, or the current Failure.</returns>
    public Result<TNext, TError> FlatMap<TNext>(Func<TValue, Result<TNext, TError>> onSuccess) =>
        this switch
        {
            Success s => onSuccess(s.Value),
            Failure f => new Result<TNext, TError>.Failure(f.Error),
            _ => throw new InvalidOperationException("Unknown Result type")
        };

    /// <summary>
    /// Applies a transformation function to the error if the result is a failure.
    /// </summary>
    /// <typeparam name="TNextError">The type of the transformed error.</typeparam>
    /// <param name="onFailure">Function to apply to the error if failed.</param>
    /// <returns>A new Result with the transformed error, or the current Success.</returns>
    public Result<TValue, TNextError> MapError<TNextError>(Func<TError, TNextError> onFailure) =>
        this switch
        {
            Success s => new Result<TValue, TNextError>.Success(s.Value),
            Failure f => new Result<TValue, TNextError>.Failure(onFailure(f.Error)),
            _ => throw new InvalidOperationException("Unknown Result type")
        };

    /// <summary>
    /// Attempts to recover from a Failure by producing a fallback Result. The current
    /// Success, if any, passes through untouched.
    /// </summary>
    /// <param name="onFailure">Function that attempts a fallback Result from the error.</param>
    /// <returns>The current Success, or the Result returned by <paramref name="onFailure"/>.</returns>
    public Result<TValue, TError> Recover(Func<TError, Result<TValue, TError>> onFailure) =>
        this switch
        {
            Success => this,
            Failure f => onFailure(f.Error),
            _ => throw new InvalidOperationException("Unknown Result type")
        };

    /// <summary>
    /// Returns the success value, or <paramref name="defaultValue"/> if this is a Failure.
    /// </summary>
    public TValue GetOrElse(TValue defaultValue) =>
        this is Success s ? s.Value : defaultValue;

    /// <summary>
    /// Returns the success value, or <c>default</c> if this is a Failure.
    /// </summary>
    public TValue? ToOptional() =>
        this is Success s ? s.Value : default;

    /// <summary>
    /// Applies a function to either the success or failure case.
    /// </summary>
    /// <typeparam name="TResult">The type of the final result.</typeparam>
    /// <param name="onSuccess">Function to apply if successful.</param>
    /// <param name="onFailure">Function to apply if failed.</param>
    /// <returns>The result of applying the appropriate function.</returns>
    public TResult Fold<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onFailure) =>
        this switch
        {
            Success s => onSuccess(s.Value),
            Failure f => onFailure(f.Error),
            _ => throw new InvalidOperationException("Unknown Result type")
        };

    /// <summary>
    /// Executes an action based on the result type without transforming the value.
    /// </summary>
    /// <param name="onSuccess">Action to execute if successful.</param>
    /// <param name="onFailure">Action to execute if failed.</param>
    public void Match(Action<TValue> onSuccess, Action<TError> onFailure)
    {
        if (this is Success s)
            onSuccess(s.Value);
        else if (this is Failure f)
            onFailure(f.Error);
    }
}