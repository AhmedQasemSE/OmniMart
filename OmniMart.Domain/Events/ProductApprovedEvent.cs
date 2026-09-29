using OmniMart.Domain.Common;
using System;

namespace OmniMart.Domain.Events;

public record ProductApprovedEvent(
    Guid ProductId,
    Guid VendorId
) : IDomainEvent;