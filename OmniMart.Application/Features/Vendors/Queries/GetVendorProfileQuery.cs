using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Vendors.Queries;

public record VendorProfileDto(
    Guid VendorId,
    string StoreName,
    string VendorNumber,
    string CommercialRegisterNumber,
    decimal CurrentBalance,
    decimal CommissionRate,
    bool IsApproved,
    byte[] RowVersion
);

public record GetVendorProfileQuery() : IRequest<Result<VendorProfileDto>>;

public class GetVendorProfileQueryHandler : IRequestHandler<GetVendorProfileQuery, Result<VendorProfileDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetVendorProfileQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<VendorProfileDto>> Handle(GetVendorProfileQuery request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<VendorProfileDto>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var profile = await _context.VendorProfiles
            .AsNoTracking()
            .Where(v => v.UserId == userGuid)
            .Select(v => new VendorProfileDto(
                v.Id,
                v.StoreName,
                v.VendorNumber,
                v.CommercialRegisterNumber,
                v.CurrentBalance,
                v.CommissionRate,
                v.IsApproved,
                v.RowVersion
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
            return Result<VendorProfileDto>.Failure("Vendor profile not found.", ErrorType.NotFound);

        return Result<VendorProfileDto>.Success(profile);
    }
}