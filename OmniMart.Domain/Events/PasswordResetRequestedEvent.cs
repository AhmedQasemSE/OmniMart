using OmniMart.Domain.Common;
using System;

namespace OmniMart.Domain.Events;

public record PasswordResetRequestedEvent(
    Guid UserId,
    string ResetToken
) : IDomainEvent;