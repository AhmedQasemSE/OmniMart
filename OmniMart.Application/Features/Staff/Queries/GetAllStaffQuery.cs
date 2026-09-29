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

namespace OmniMart.Application.Features.Staff.Queries;

public record StaffSummaryDto(
    Guid UserId,
    string FullName,
    string Email,
    string StaffNumber,
    string SystemRole,
    string Department,
    decimal Salary,
    bool IsActive
);

public record GetAllStaffQuery(
    string? SearchTerm = null,
    Department? DepartmentFilter = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedResult<StaffSummaryDto>>>;

public class GetAllStaffQueryHandler : IRequestHandler<GetAllStaffQuery, Result<PaginatedResult<StaffSummaryDto>>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetAllStaffQueryHandler> _logger;

    public GetAllStaffQueryHandler(
        IAppDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetAllStaffQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<StaffSummaryDto>>> Handle(GetAllStaffQuery request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            _logger.LogWarning("Unauthorized attempt to access staff records by user {UserId}.", _currentUserService.UserId);
            return Result<PaginatedResult<StaffSummaryDto>>.Failure("Only Administrators can view staff records.", ErrorType.Unauthorized);
        }

        var query = _context.StaffProfiles
            .AsNoTracking()
            .Include(s => s.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(s =>
                s.StaffNumber.ToLower().Contains(term) ||
                (s.User != null && (s.User.FirstName.ToLower().Contains(term) ||
                                    s.User.LastName.ToLower().Contains(term) ||
                                    s.User.Email.ToLower().Contains(term)||
                                    s.User.AccountNumber.ToLower().Contains(term)))
            );
        }

        if (request.DepartmentFilter.HasValue)
        {
            query = query.Where(s => s.DepartmentRole == request.DepartmentFilter.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            var emptyResult = new PaginatedResult<StaffSummaryDto>(new List<StaffSummaryDto>(), 0, request.Page, request.PageSize);
            return Result<PaginatedResult<StaffSummaryDto>>.Success(emptyResult);
        }

        var staffList = await query
            .OrderByDescending(s => s.User!.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StaffSummaryDto(
                s.UserId,
                $"{s.User!.FirstName} {s.User.LastName}",
                s.User.Email,
                s.StaffNumber,
                s.User.Role.ToString(),
                s.DepartmentRole.ToString(),
                s.Salary,
                s.User.IsActive
            ))
            .ToListAsync(cancellationToken);

        var paginatedResult = new PaginatedResult<StaffSummaryDto>(staffList, totalCount, request.Page, request.PageSize);

        return Result<PaginatedResult<StaffSummaryDto>>.Success(paginatedResult);
    }
}

public class GetAllStaffQueryValidator : AbstractValidator<GetAllStaffQuery>
{
    public GetAllStaffQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page number must be greater than 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.DepartmentFilter).IsInEnum().When(x => x.DepartmentFilter.HasValue).WithMessage("Invalid department specified.");
    }
}