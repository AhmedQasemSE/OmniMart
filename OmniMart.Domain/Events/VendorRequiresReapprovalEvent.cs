using OmniMart.Domain.Common;
using System;

namespace OmniMart.Domain.Events;

public record VendorRequiresReapprovalEvent(
    Guid VendorId,
    string OldCommercialRegisterNumber,
    string NewCommercialRegisterNumber
) : IDomainEvent;