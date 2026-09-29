using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Vendors.Queries;

public record PendingVendorDto(
    Guid VendorId,
    string StoreName,
    string VendorNumber,
    string CommercialRegisterNumber,
    string OwnerName,
    string Email,
    string PhoneNumber,
    DateTimeOffset RegisteredAt
);

public record GetPendingVendorsQuery(int Page = 1, int PageSize = 10) : IRequest<Result<PaginatedResult<PendingVendorDto>>>;

public class GetPendingVendorsQueryHandler : IRequestHandler<GetPendingVendorsQuery, Result<PaginatedResult<PendingVendorDto>>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetPendingVendorsQueryHandler> _logger;

    public GetPendingVendorsQueryHandler(
        IAppDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetPendingVendorsQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<PendingVendorDto>>> Handle(GetPendingVendorsQuery request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            return Result<PaginatedResult<PendingVendorDto>>.Failure("Only Administrators can view pending vendor registrations.", ErrorType.Unauthorized);
        }

        var query = _context.VendorProfiles
            .AsNoTracking()
            .Include(v => v.User)
            .Where(v => v.IsApproved == false);

        int totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            var emptyResult = new PaginatedResult<PendingVendorDto>(new List<PendingVendorDto>(), 0, request.Page, request.PageSize);
            return Result<PaginatedResult<PendingVendorDto>>.Success(emptyResult);
        }

        var pendingVendors = await query
            .OrderBy(v => v.User!.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new PendingVendorDto(
                v.Id,
                v.StoreName,
                v.VendorNumber,
                v.CommercialRegisterNumber,
                $"{v.User!.FirstName} {v.User.LastName}",
                v.User.Email,
                v.User.PhoneNumber!,
                v.User.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var paginatedResult = new PaginatedResult<PendingVendorDto>(pendingVendors, totalCount, request.Page, request.PageSize);

        return Result<PaginatedResult<PendingVendorDto>>.Success(paginatedResult);
    }
}

public class GetPendingVendorsQueryValidator : AbstractValidator<GetPendingVendorsQuery>
{
    public GetPendingVendorsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page number must be greater than 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100).WithMessage("Page size must be between 1 and 100.");
    }
}