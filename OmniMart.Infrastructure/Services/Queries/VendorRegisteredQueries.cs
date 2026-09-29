using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Services.Queries;

public class VendorRegisteredQueries : IVendorRegisteredQueries
{
    private readonly IAppDbContext _context;

    public VendorRegisteredQueries(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<VendorRegisteredDTO?> GetVendorRegistrationDetailsAsync(Guid vendorId)
    {
        var vendorData = await _context.VendorProfiles
            .AsNoTracking()
            .Where(vp => vp.Id == vendorId)
            .Select(vp => new { vp.User!.Email, vp.StoreName })
            .FirstOrDefaultAsync();

        if (vendorData == null) return null;

        var adminEmails = await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == SystemRole.Admin || u.Role == SystemRole.Manager)
            .Select(u => u.Email)
            .Where(email => !string.IsNullOrEmpty(email))
            .Distinct()
            .ToListAsync();

        return new VendorRegisteredDTO(vendorData.Email, vendorData.StoreName, adminEmails!);
    }
}