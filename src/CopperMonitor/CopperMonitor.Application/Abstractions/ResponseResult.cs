using CopperMonitor.Domain.SeedWork;

namespace CopperMonitor.Application.Abstractions;

public class ResponseResult<T>
{
    public T? Data { get; set; }
    public ResponseError? Error { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }

    private ResponseResult(T? data)
    {
        Data = data;
        IsSuccess = true;
    }

    private ResponseResult(ResponseError error)
    {
        Error = error ?? throw new CopperDomainException(CopperExceptionCode.UnhandledException, "Error cannot be null.");
        IsSuccess = false;
        Data = default;
    }

    public static ResponseResult<T> Success(T? value) => new(value);
    public static ResponseResult<T> Success(T? value, string? message) => new(value) { Message = message };
    public static ResponseResult<T> SuccessWithEmptyBody() => Success(default);
    public static ResponseResult<T> Failure(ResponseError error) => new(error);

    public ResponseResult()
    {
    }
}
