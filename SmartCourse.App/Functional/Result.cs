namespace SmartCourse.App.Functional;

public abstract record Result<T>
{
    private Result() { }
    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(string Error) : Result<T>;

    public Result<U> Map<U>(Func<T, U> transform) => this switch
    {
        Success(var value) => new Result<U>.Success(transform(value)),
        Failure(var error) => new Result<U>.Failure(error),
        _ => throw new InvalidOperationException()
    };

    public Result<U> Bind<U>(Func<T, Result<U>> next) => this switch
    {
        Success(var value) => next(value),
        Failure(var error) => new Result<U>.Failure(error),
        _ => throw new InvalidOperationException()
    };
}
