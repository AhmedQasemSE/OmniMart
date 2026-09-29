using OmniMart.Domain.Common;
using System;

namespace OmniMart.Domain.Events;

public record OrderDeliveredEvent(
    Guid OrderId,
    Guid CustomerId
) : IDomainEvent;