namespace IdentityAuth.Application.Security.DTOs;

public class SecurityEventListResponse
{
    public List<SecurityEventResponse> Events { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
