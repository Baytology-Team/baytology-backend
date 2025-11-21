using Baytology.Domain.Common;
using Baytology.Domain.Common.Constants;
using Baytology.Domain.Common.Results;

namespace Baytology.Domain.AgentDetails;

public sealed class AgentDetail : Entity
{
    public string UserId { get; private set; } = null!;
    public string? AgencyName { get; private set; }
    public string? LicenseNumber { get; private set; }
    public decimal Rating { get; private set; }
    public int ReviewCount { get; private set; }
    public bool IsVerified { get; private set; }
    public decimal CommissionRate { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset UpdatedOnUtc { get; private set; }

    private AgentDetail() { }

    private AgentDetail(
        Guid id,
        string userId,
        string? agencyName,
        string? licenseNumber,
        decimal commissionRate) : base(id)
    {
        UserId = userId;
        AgencyName = agencyName;
        LicenseNumber = licenseNumber;
        CommissionRate = commissionRate;
        Rating = 0;
        ReviewCount = 0;
        IsVerified = false;
        CreatedOnUtc = DateTimeOffset.UtcNow;
        UpdatedOnUtc = DateTimeOffset.UtcNow;
    }

    public static Result<AgentDetail> Create(
        string userId,
        string? agencyName = null,
        string? licenseNumber = null,
        decimal commissionRate = BaytologyConstants.DefaultCommissionRate)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return AgentDetailErrors.UserIdRequired;

        if (!IsValidCommissionRate(commissionRate))
            return AgentDetailErrors.CommissionRateInvalid;

        return new AgentDetail(
            Guid.NewGuid(),
            userId.Trim(),
            string.IsNullOrWhiteSpace(agencyName) ? null : agencyName.Trim(),
            string.IsNullOrWhiteSpace(licenseNumber) ? null : licenseNumber.Trim(),
            commissionRate);
    }

    public Result<Success> Update(string? agencyName, string? licenseNumber, decimal commissionRate)
    {
        if (!IsValidCommissionRate(commissionRate))
            return AgentDetailErrors.CommissionRateInvalid;

        AgencyName = string.IsNullOrWhiteSpace(agencyName) ? null : agencyName.Trim();
        LicenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? null : licenseNumber.Trim();
        CommissionRate = commissionRate;
        UpdatedOnUtc = DateTimeOffset.UtcNow;

        return Result.Success;
    }

    public void Verify() => IsVerified = true;

    public Result<Success> UpdateRating(decimal newRating, int newCount)
    {
        if (newRating is < 0 or > 5)
            return AgentDetailErrors.RatingInvalid;

        if (newCount < 0)
            return AgentDetailErrors.ReviewCountInvalid;

        Rating = newRating;
        ReviewCount = newCount;
        UpdatedOnUtc = DateTimeOffset.UtcNow;

        return Result.Success;
    }

    private static bool IsValidCommissionRate(decimal commissionRate)
        => commissionRate is > 0 and < 1;
}
