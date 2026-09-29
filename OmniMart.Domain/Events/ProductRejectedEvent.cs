using OmniMart.Domain.Common;
using System;

namespace OmniMart.Domain.Events;

public record ProductRejectedEvent(
    Guid ProductId,
    Guid VendorId,
    string Reason
) : IDomainEvent;