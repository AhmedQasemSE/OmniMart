using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;

namespace OmniMart.Infrastructure.Repositories;

public class PaymentGroupRepository : GenericRepository<PaymentGroup>, IPaymentGroupRepository
{
    public PaymentGroupRepository(AppDbContext context) : base(context)
    {
    }
}