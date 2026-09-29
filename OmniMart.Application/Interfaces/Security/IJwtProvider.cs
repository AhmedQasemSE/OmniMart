using OmniMart.Domain.Entities;

namespace OmniMart.Application.Interfaces.Security
{
    public interface IJwtProvider
    {
        string GenerateJwtToken(User user);
        string GenerateRefreshToken();
    }
}
