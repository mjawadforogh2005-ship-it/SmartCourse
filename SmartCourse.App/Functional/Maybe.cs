namespace SmartCourse.App.Functional;

public abstract record Maybe<T>
{
    private Maybe() { }
    public sealed record Some(T Value) : Maybe<T>;
    public sealed record None : Maybe<T>;

    public static Maybe<T> FromNullable(T? value) => value is null ? new None() : new Some(value);

    public Result<T> ToResult(string error) => this switch
    {
        Some(var value) => new Result<T>.Success(value),
        _ => new Result<T>.Failure(error)
    };
}
