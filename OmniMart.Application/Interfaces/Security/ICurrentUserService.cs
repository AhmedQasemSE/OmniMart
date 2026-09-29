
    namespace OmniMart.Application.Interfaces.Security;

    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserNumber { get; } 
        string? Email { get; }      
        string? Name { get; }       
        string? Role { get; }       
    }

