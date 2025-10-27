using System.Security.Claims;

namespace Baytology.Application.Common.Interfaces;

public interface ITokenProvider
{
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
