using System.Net;

namespace EnfoqueAutomotriz.Domain.Shared;

public class APIResponse<T>
{
    public APIResponse() => ErrorMessages = [];

    public HttpStatusCode StatusCode { get; set; }
    public bool IsSuccess { get; set; } = true;
    public List<string> ErrorMessages { get; set; }
    public T? Result { get; set; }

    public static APIResponse<T> Success(T result, HttpStatusCode code = HttpStatusCode.OK) =>
        new() { StatusCode = code, IsSuccess = true, Result = result };

    public static APIResponse<T> Failure(string error, HttpStatusCode code = HttpStatusCode.BadRequest) =>
        new() { StatusCode = code, IsSuccess = false, ErrorMessages = [error] };

    public static APIResponse<T> Failure(List<string> errors, HttpStatusCode code = HttpStatusCode.BadRequest) =>
        new() { StatusCode = code, IsSuccess = false, ErrorMessages = errors };
}

public class APIResponse : APIResponse<object>
{
    public static APIResponse NoContent() =>
        new() { StatusCode = HttpStatusCode.NoContent, IsSuccess = true };

    public static new APIResponse Success(object? result = null, HttpStatusCode code = HttpStatusCode.OK) =>
        new() { StatusCode = code, IsSuccess = true, Result = result };
}
