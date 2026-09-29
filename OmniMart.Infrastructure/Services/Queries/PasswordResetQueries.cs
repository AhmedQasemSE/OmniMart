using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Infrastructure.Services.Queries;

public class PasswordResetQueries: IPasswordResetQueries
{
    private readonly IAppDbContext _context;

    public PasswordResetQueries(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetRequestedDTO?> GetUserDetailsForResetAsync(Guid userId)
    {
        var result = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new PasswordResetRequestedDTO(
                u.Email ?? ""
            ))
            .FirstOrDefaultAsync();

        return result;
    }
}

