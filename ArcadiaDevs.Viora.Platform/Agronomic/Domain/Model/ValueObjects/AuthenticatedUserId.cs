using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Value object representing the authenticated user id.
/// </summary>
/// <remarks>
///     This value object is used to validate if the authenticated user
///     is allowed to access the requested agronomic statistics.
/// </remarks>
public record AuthenticatedUserId
{
    public int Value { get; init; }

    /// <summary>
    ///     Constructor with validation logic for AuthenticatedUserId.
    /// </summary>
    public AuthenticatedUserId(int authenticatedUserId)
    {
        if (authenticatedUserId < 1)
        {
            throw new ArgumentException("Authenticated user id cannot be less than 1", nameof(authenticatedUserId));
        }

        Value = authenticatedUserId;
    }
}