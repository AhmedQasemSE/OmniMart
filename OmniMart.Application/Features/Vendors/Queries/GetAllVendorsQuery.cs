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

public record VendorSummaryDto(
    Guid VendorId,
    string StoreName,
    string VendorNumber,
    string CommercialRegisterNumber,
    string OwnerFullName,
    string Email,
    string PhoneNumber,
    decimal CommissionRate,
    decimal WalletBalance, 
    bool IsActive
);

public record GetAllVendorsQuery(
    string? SearchTerm = null,
    bool? IsActiveFilter = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedResult<VendorSummaryDto>>>;

public class GetAllVendorsQueryHandler : IRequestHandler<GetAllVendorsQuery, Result<PaginatedResult<VendorSummaryDto>>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetAllVendorsQueryHandler> _logger;

    public GetAllVendorsQueryHandler(
        IAppDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetAllVendorsQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<VendorSummaryDto>>> Handle(GetAllVendorsQuery request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            _logger.LogWarning("Unauthorized attempt to access all vendors by user {UserId}.", _currentUserService.UserId);
            return Result<PaginatedResult<VendorSummaryDto>>.Failure("Only Administrators can view full vendor records.", ErrorType.Unauthorized);
        }

        var query = _context.VendorProfiles
            .AsNoTracking()
            .Include(v => v.User)
            .Where(v => v.IsApproved == true)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(v =>
                v.StoreName.ToLower().Contains(term) ||
                v.VendorNumber.ToLower().Contains(term) ||
                v.CommercialRegisterNumber.ToLower().Contains(term) ||
                (v.User != null && (v.User.FirstName.ToLower().Contains(term) ||
                                    v.User.LastName.ToLower().Contains(term) ||
                                    v.User.Email.ToLower().Contains(term) ||
                                    v.User.AccountNumber.ToLower().Contains(term)))
            );
        }

        if (request.IsActiveFilter.HasValue)
        {
            query = query.Where(v => v.User != null && v.User.IsActive == request.IsActiveFilter.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            var emptyResult = new PaginatedResult<VendorSummaryDto>(new List<VendorSummaryDto>(), 0, request.Page, request.PageSize);
            return Result<PaginatedResult<VendorSummaryDto>>.Success(emptyResult);
        }

        var vendorsList = await query
            .OrderByDescending(v => v.User!.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VendorSummaryDto(
                v.Id,
                v.StoreName,
                v.VendorNumber,
                v.CommercialRegisterNumber,
                $"{v.User!.FirstName} {v.User.LastName}",
                v.User.Email,
                v.User.PhoneNumber!,
                v.CommissionRate,
                0m, 
                v.User.IsActive
            ))
            .ToListAsync(cancellationToken);

        var paginatedResult = new PaginatedResult<VendorSummaryDto>(vendorsList, totalCount, request.Page, request.PageSize);

        return Result<PaginatedResult<VendorSummaryDto>>.Success(paginatedResult);
    }
}

public class GetAllVendorsQueryValidator : AbstractValidator<GetAllVendorsQuery>
{
    public GetAllVendorsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page number must be greater than 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100).WithMessage("Page size must be between 1 and 100.");
    }
}