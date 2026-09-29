using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Enums;

namespace OmniMart.Infrastructure.Services.Queries;

public class VendorRequiresReapprovalQueries : IVendorRequiresReapprovalQueries
{
    private readonly IAppDbContext _context;
    public VendorRequiresReapprovalQueries(IAppDbContext context) => _context = context;

    public async Task<VendorRequiresReapprovalDTO?> GetVendorAndStaffDetailsAsync(Guid vendorId)
    {
        var storeName = await _context.VendorProfiles
            .AsNoTracking()
            .Where(v => v.Id == vendorId)
            .Select(v => v.StoreName)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(storeName)) return null;

        var staffEmails = await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == SystemRole.Admin || u.Role == SystemRole.Manager)
            .Select(u => u.Email)
            .Where(email => !string.IsNullOrEmpty(email))
            .Distinct()
            .ToListAsync();

        return new VendorRequiresReapprovalDTO(storeName, staffEmails);
    }
}