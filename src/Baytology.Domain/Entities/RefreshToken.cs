using Baytology.Domain.Common;
using Baytology.Domain.Exceptions;
using Baytology.Domain.Common.Results;

namespace Baytology.Domain.Entities;

public sealed class RefreshToken : AuditableEntity
{
    public string Token { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public DateTimeOffset ExpiresOnUtc { get; private set; }

    private RefreshToken()
    { }

    private RefreshToken(Guid id, string token, string userId, DateTimeOffset expiresOnUtc)
        : base(id)
    {
        Token = token;
        UserId = userId;
        ExpiresOnUtc = expiresOnUtc;
    }

    public static Result<RefreshToken> Create(Guid id, string? token, string? userId, DateTimeOffset expiresOnUtc)
    {
        if (id == Guid.Empty)
        {
            return RefreshTokenErrors.IdRequired;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return RefreshTokenErrors.TokenRequired;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return RefreshTokenErrors.UserIdRequired;
        }

        if (expiresOnUtc <= DateTimeOffset.UtcNow)
        {
            return RefreshTokenErrors.ExpiryInvalid;
        }

        return new RefreshToken(id, token.Trim(), userId.Trim(), expiresOnUtc);
    }
}
