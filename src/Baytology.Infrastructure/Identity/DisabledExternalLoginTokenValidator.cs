using Baytology.Application.Common.Errors;
using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;

namespace Baytology.Infrastructure.Identity;

internal sealed class DisabledExternalLoginTokenValidator : IExternalLoginTokenValidator
{
    public Task<Result<ExternalUserInfoDto>> ValidateTokenAsync(string provider, string idToken)
    {
        if (!provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<Result<ExternalUserInfoDto>>(ApplicationErrors.ExternalLogin.ProviderInvalid);
        }

        return Task.FromResult<Result<ExternalUserInfoDto>>(
            ApplicationErrors.ExternalLogin.TokenValidationFailed(provider));
    }
}
