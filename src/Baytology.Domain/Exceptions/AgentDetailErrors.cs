using Baytology.Domain.Common.Results;

namespace Baytology.Domain.Exceptions;

public static class AgentDetailErrors
{
    public static readonly Error UserIdRequired =
        Error.Validation("AgentDetail_UserId_Required", "User ID is required.");

    public static readonly Error CommissionRateInvalid =
        Error.Validation("AgentDetail_CommissionRate_Invalid", "Commission rate must be greater than 0 and less than 1.");

    public static readonly Error RatingInvalid =
        Error.Validation("AgentDetail_Rating_Invalid", "Rating must be between 0 and 5.");

    public static readonly Error ReviewCountInvalid =
        Error.Validation("AgentDetail_ReviewCount_Invalid", "Review count cannot be negative.");

    public static readonly Error NotFound =
        Error.NotFound("AgentDetail_Not_Found", "Agent details not found.");

    public static readonly Error AlreadyExists =
        Error.Conflict("AgentDetail_Already_Exists", "Agent details already exist for this user.");
}
