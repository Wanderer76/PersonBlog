namespace Authentication.Contract.Models;

/// <summary>
/// Grants and selects a resource context after the calling Gateway has verified access.
/// This contract is intended for trusted internal service-to-service calls.
/// </summary>
public sealed record ActivateUserContextRequest(
    Guid UserId,
    string ContextType,
    Guid ContextId,
    Guid? RoleId = null);
