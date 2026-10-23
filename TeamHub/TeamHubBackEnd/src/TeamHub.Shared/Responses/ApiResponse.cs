namespace TeamHub.Shared.Responses;

public class ApiResponse<T> : BaseResponse
{
    public T? Data { get; set; }
}