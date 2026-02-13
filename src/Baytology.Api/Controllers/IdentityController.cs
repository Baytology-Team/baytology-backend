using System.Security.Claims;

using Asp.Versioning;

using Baytology.Application.Features.Identity.Commands.ChangePassword;
using Baytology.Application.Features.Identity.Commands.ConfirmEmail;
using Baytology.Application.Features.Identity.Commands.DeleteAccount;
using Baytology.Application.Features.Identity.Commands.ExternalLogin;
using Baytology.Application.Features.Identity.Commands.ForgotPassword;
using Baytology.Application.Features.Identity.Commands.Logout;
using Baytology.Application.Features.Identity.Commands.RegisterUser;
using Baytology.Application.Features.Identity.Commands.ResendConfirmation;
using Baytology.Application.Features.Identity.Commands.ResetPassword;
using Baytology.Application.Features.Identity.Dtos;
using Baytology.Application.Features.Identity.Queries.GenerateTokens;
using Baytology.Application.Features.Identity.Queries.GetUserInfo;
using Baytology.Application.Features.Identity.Queries.RefreshTokens;
using Baytology.Contracts.Requests.Identity;
using Baytology.Contracts.Responses.Identity;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ApplicationTokenResponse = Baytology.Application.Features.Identity.TokenResponse;
using ContractTokenResponse = Baytology.Contracts.Responses.Identity.TokenResponse;

namespace Baytology.Api.Controllers;

[ApiVersion("1")]
[Route("api/identity")]
[Route("api/v{version:apiVersion}/identity")]
public sealed class IdentityController(ISender sender) : ApiControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        var command = new RegisterUserCommand(request.Email, request.Password, request.DisplayName, request.Role);
        var result = await sender.Send(command, ct);

        return result.Match(userId => Ok(new RegisterUserResponse(userId)), Problem);
    }

    [HttpPost("token/generate")]
    [ProducesResponseType(typeof(ContractTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateToken([FromBody] GenerateTokenRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GenerateTokenQuery(request.Email, request.Password), ct);

        return result.Match(tokens => Ok(MapToken(tokens)), Problem);
    }

    [HttpPost("token/refresh")]
    [ProducesResponseType(typeof(ContractTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RefreshTokenQuery(request.RefreshToken, request.ExpiredAccessToken), ct);

        return result.Match(tokens => Ok(MapToken(tokens)), Problem);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new GetUserByIdQuery(userId), ct);

        return result.Match(user => Ok(MapUser(user)), Problem);
    }

    [HttpPost("external-login")]
    [ProducesResponseType(typeof(ExternalLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request, CancellationToken ct)
    {
        var command = new ExternalLoginCommand(request.Provider, request.IdToken);
        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(new ExternalLoginResponse(
                MapToken(response.Tokens),
                response.IsNewUser,
                response.UserId)),
            Problem);
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
        var result = await sender.Send(command, ct);

        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var command = new ForgotPasswordCommand(request.Email);
        var result = await sender.Send(command, ct);

        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var command = new ResetPasswordCommand(request.Email, request.Token, request.NewPassword);
        var result = await sender.Send(command, ct);

        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        var command = new ConfirmEmailCommand(request.UserId, request.Token);
        var result = await sender.Send(command, ct);

        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("resend-confirmation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request, CancellationToken ct)
    {
        var command = new ResendConfirmationCommand(request.Email);
        var result = await sender.Send(command, ct);

        return result.Match(_ => Ok(), Problem);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var command = new LogoutCommand(userId);
        var result = await sender.Send(command, ct);

        return result.Match(_ => Ok(), Problem);
    }

    [HttpDelete("account")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var command = new DeleteAccountCommand(userId);
        var result = await sender.Send(command, ct);

        return result.Match(_ => Ok(), Problem);
    }

    private static ContractTokenResponse MapToken(ApplicationTokenResponse response)
        => new(response.AccessToken, response.RefreshToken, response.ExpiresOnUtc);

    private static CurrentUserResponse MapUser(AppUserDto user)
        => new(
            user.UserId,
            user.Email,
            user.Roles.ToArray(),
            user.Claims.Select(claim => new ClaimResponse(claim.Type, claim.Value)).ToArray(),
            user.DisplayName);
}
