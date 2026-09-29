using OmniMart.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Domain.Events;

public record OrderPlacedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount) : IDomainEvent;

