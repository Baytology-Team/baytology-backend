using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Entities;

using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Admin.Commands.ReviewRefund;

public record ReviewRefundCommand(Guid RefundId, bool Approve, string AdminUserId) : IRequest<Result<Success>>;
