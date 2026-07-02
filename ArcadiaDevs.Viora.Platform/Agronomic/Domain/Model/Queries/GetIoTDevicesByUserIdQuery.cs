namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

/// <summary>
///     Query that requests all IoT devices across all plots owned by a user.
///     <para>
///         Backs the dashboard's aggregate Water Stress view. The user does
///         not pass an explicit plot filter; the query service loads every
///         active plot owned by the user, then enriches each plot's devices
///         with the current (simulated) telemetry. No ownership check is
///         needed at the service layer because the user IS the owner.
///     </para>
///     <para>
///         C# port of the OS <c>GetIoTDevicesByUserIdQuery.java</c> (14 lines).
///         Constructor validation mirrors the OS: the user id is required and
///         must be positive.
///     </para>
/// </summary>
/// <param name="UserId">The owning user identifier; must be positive.</param>
/// <exception cref="ArgumentException">
///     Thrown when <paramref name="UserId"/> is non-positive.
/// </exception>
public record GetIoTDevicesByUserIdQuery
{
    /// <summary>
    ///     The owning user identifier; must be positive.
    /// </summary>
    public int UserId { get; }

    /// <summary>
    ///     Builds a new <see cref="GetIoTDevicesByUserIdQuery"/> with the
    ///     supplied <paramref name="userId"/>.
    /// </summary>
    /// <param name="userId">The owning user identifier; must be positive.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="userId"/> is non-positive.
    /// </exception>
    public GetIoTDevicesByUserIdQuery(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("GetIoTDevicesByUserIdQuery requires a valid (positive) userId.", nameof(userId));

        UserId = userId;
    }
}
