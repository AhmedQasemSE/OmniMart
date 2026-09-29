using System;
using OmniMart.Domain.Common;

namespace OmniMart.Domain.Events;

public record OrderRefundedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal RefundAmount
) : IDomainEvent;