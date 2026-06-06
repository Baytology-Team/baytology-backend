using Baytology.Domain.Common;
using Baytology.Domain.Exceptions;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using System.Text.RegularExpressions;

namespace Baytology.Domain.Entities;

public sealed class UserProfile : AuditableEntity
{
    public string UserId { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public string? PhoneNumber { get; private set; }
    public ContactMethod PreferredContactMethod { get; private set; }

    private static readonly Regex PhoneRegex = new(@"^\+?[0-9]{10,15}$", RegexOptions.Compiled);
    private static readonly Regex UrlRegex = new(@"^https?://[^\s/$.?#].[^\s]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private UserProfile() { }

    private UserProfile(
        Guid id,
        string userId,
        string displayName,
        string? avatarUrl,
        string? bio,
        string? phoneNumber,
        ContactMethod preferredContactMethod) : base(id)
    {
        UserId = userId;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        Bio = bio;
        PhoneNumber = phoneNumber;
        PreferredContactMethod = preferredContactMethod;
    }

    public static Result<UserProfile> Create(
        string userId,
        string displayName,
        string? avatarUrl = null,
        string? bio = null,
        string? phoneNumber = null,
        ContactMethod preferredContactMethod = ContactMethod.Email)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return UserProfileErrors.UserIdRequired;

        if (string.IsNullOrWhiteSpace(displayName))
            return UserProfileErrors.DisplayNameRequired;

        if (displayName.Trim().Length < 2)
            return Error.Validation("UserProfile_DisplayNameTooShort", "Display name must be at least 2 characters long.");

        if (displayName.Trim().Length > 100)
            return Error.Validation("UserProfile_DisplayNameTooLong", "Display name cannot exceed 100 characters.");

        if (!string.IsNullOrWhiteSpace(avatarUrl) && avatarUrl.Trim().Length > 500)
            return UserProfileErrors.AvatarUrlTooLong;

        if (!string.IsNullOrWhiteSpace(avatarUrl) && !IsValidUrl(avatarUrl.Trim()))
            return Error.Validation("UserProfile_AvatarUrlInvalid", "Avatar URL must be a valid URL.");

        if (!string.IsNullOrWhiteSpace(bio) && bio.Trim().Length > 2000)
            return UserProfileErrors.BioTooLong;

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            if (phoneNumber.Trim().Length > 20)
                return UserProfileErrors.PhoneNumberTooLong;

            if (!PhoneRegex.IsMatch(phoneNumber.Trim()))
                return UserProfileErrors.PhoneNumberInvalidFormat;
        }

        return new UserProfile(
            Guid.NewGuid(),
            userId.Trim(),
            displayName.Trim(),
            string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim(),
            string.IsNullOrWhiteSpace(bio) ? null : bio.Trim(),
            string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            preferredContactMethod);
    }

    public Result<Success> Update(
        string displayName,
        string? avatarUrl,
        string? bio,
        string? phoneNumber,
        ContactMethod preferredContactMethod)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return UserProfileErrors.DisplayNameRequired;

        if (displayName.Trim().Length < 2)
            return Error.Validation("UserProfile_DisplayNameTooShort", "Display name must be at least 2 characters long.");

        if (displayName.Trim().Length > 100)
            return Error.Validation("UserProfile_DisplayNameTooLong", "Display name cannot exceed 100 characters.");

        if (!string.IsNullOrWhiteSpace(avatarUrl) && avatarUrl.Trim().Length > 500)
            return UserProfileErrors.AvatarUrlTooLong;

        if (!string.IsNullOrWhiteSpace(avatarUrl) && !IsValidUrl(avatarUrl.Trim()))
            return Error.Validation("UserProfile_AvatarUrlInvalid", "Avatar URL must be a valid URL.");

        if (!string.IsNullOrWhiteSpace(bio) && bio.Trim().Length > 2000)
            return UserProfileErrors.BioTooLong;

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            if (phoneNumber.Trim().Length > 20)
                return UserProfileErrors.PhoneNumberTooLong;

            if (!PhoneRegex.IsMatch(phoneNumber.Trim()))
                return UserProfileErrors.PhoneNumberInvalidFormat;
        }

        DisplayName = displayName.Trim();
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        PreferredContactMethod = preferredContactMethod;

        return Result.Success;
    }
}
