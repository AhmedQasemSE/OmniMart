namespace OmniMart.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public List<string>? ValidationErrors { get; }

    public ErrorType ErrorType { get; }

    private Result(bool isSuccess, T? value, string? errorMessage, List<string>? validationErrors, ErrorType errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value)
        => new Result<T>(true, value, null, null, default);

    public static Result<T> Failure(string errorMessage, ErrorType errorType = ErrorType.Failure)
        => new Result<T>(false, default, errorMessage, null, errorType);

    public static Result<T> ValidationError(List<string> errors)
        => new Result<T>(false, default, null, errors, ErrorType.Validation);
}