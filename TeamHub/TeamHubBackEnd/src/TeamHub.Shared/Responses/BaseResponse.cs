namespace TeamHub.Shared.Responses;

public abstract class BaseResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}