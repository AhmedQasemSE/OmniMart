using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Services.BackgroundJobs;

public class AbandonedCartCleanupJob
{
    private readonly IAppDbContext _context;
    private readonly ILogger<AbandonedCartCleanupJob> _logger;

    public AbandonedCartCleanupJob(IAppDbContext context, ILogger<AbandonedCartCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("⏳ Checking for abandoned payment groups...");

        var expirationTime = DateTimeOffset.UtcNow.AddMinutes(-15);

        var abandonedGroups = await _context.PaymentGroups
            .IgnoreQueryFilters()
            .Include(pg => pg.Orders)
                .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
            .Where(pg => !pg.IsPaid && pg.CreatedAt < expirationTime)
            .ToListAsync();

        if (!abandonedGroups.Any()) return;

        foreach (var group in abandonedGroups)
        {
            _logger.LogWarning("⚠️ PaymentGroup {Id} expired. Restoring stock.", group.Id);

            group.MarkAsFailed();

            foreach (var order in group.Orders)
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.ProductVariant != null)
                    {
                        item.ProductVariant.ReleaseReservedStock(item.Quantity);
                    }
                }
            }
        }

        await ((DbContext)_context).SaveChangesAsync();
        _logger.LogInformation("✅ Restored stock for {Count} abandoned payment groups.", abandonedGroups.Count);
    }
}