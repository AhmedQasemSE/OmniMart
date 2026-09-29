using OmniMart.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Domain.Events;

public record OrderCancelledEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount
    ) : IDomainEvent;



