using System.Security.Claims;

using Baytology.Application.Common.Interfaces;
using Baytology.Application.Features.Identity.Dtos;
using Baytology.Domain.Common.Results;

namespace Baytology.Application.Tests.Support;

internal sealed class TestIdentityService : IIdentityService
{
    public Task<Result<List<UserSummaryDto>>> GetUsersAsync() => Task.FromResult<Result<List<UserSummaryDto>>>(new List<UserSummaryDto>());
    public Task<string?> GetUserNameAsync(string userId) => Task.FromResult<string?>("Test User");
    public Task<bool> IsInRoleAsync(string userId, string role) => Task.FromResult(true);
    public Task<bool> AuthorizeAsync(string userId, string? policyName) => Task.FromResult(true);
    public Task<Result<AppUserDto>> AuthenticateAsync(string email, string password) => Task.FromResult<Result<AppUserDto>>(Error.Failure("NotImplemented", "Not implemented in test"));
    public Task<Result<string>> RegisterUserAsync(string email, string password, string role) => Task.FromResult<Result<string>>("test-user-id");
    public Task<Result<Success>> ToggleUserStatusAsync(string userId, bool isActive) => Task.FromResult<Result<Success>>(Result.Success);
    public Task<Result<Success>> AssignRoleAsync(string userId, string role) => Task.FromResult<Result<Success>>(Result.Success);

    public Task<Result<AppUserDto>> GetUserByIdAsync(string userId) =>
        Task.FromResult<Result<AppUserDto>>(new AppUserDto(userId, "buyer@test.local", new List<string> { "Buyer" }, new List<Claim>(), "Test Buyer"));

    public Task<Result<ExternalLoginResultDto>> ExternalLoginAsync(string provider, string providerSubjectId, string email, string? firstName, string? lastName) => Task.FromResult<Result<ExternalLoginResultDto>>(Error.Failure("NotImplemented", "Not implemented in test"));
    public Task<Result<Success>> ChangePasswordAsync(string userId, string currentPassword, string newPassword) => Task.FromResult<Result<Success>>(Result.Success);
    public Task<Result<Success>> ForgotPasswordAsync(string email) => Task.FromResult<Result<Success>>(Result.Success);
    public Task<Result<Success>> ResetPasswordAsync(string email, string token, string newPassword) => Task.FromResult<Result<Success>>(Result.Success);
    public Task<Result<Success>> ResendEmailConfirmationAsync(string email) => Task.FromResult<Result<Success>>(Result.Success);
    public Task<Result<Success>> ConfirmEmailAsync(string userId, string token) => Task.FromResult<Result<Success>>(Result.Success);
    public Task<Result<string>> GenerateEmailConfirmationTokenAsync(string userId) => Task.FromResult<Result<string>>("test-token");
    public Task<Result<Success>> DeleteAccountAsync(string userId) => Task.FromResult<Result<Success>>(Result.Success);
    public Task<Result<Success>> RevokeRefreshTokensAsync(string userId) => Task.FromResult<Result<Success>>(Result.Success);
}
