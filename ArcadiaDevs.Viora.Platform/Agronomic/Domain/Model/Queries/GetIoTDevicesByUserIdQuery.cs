namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

/// <summary>
///     Query that requests all IoT devices across all plots owned by a user.
///     <para>
///         Stub record — expanded in T1.17.0-7 to add the validation body and
///         the full XML documentation. The 3-line shape is enough for the
///         <see cref="Agronomic.Application.Internal.QueryServices.IIoTDeviceQueryService"/>
///         interface to reference the type in T1.17.0-6 (D-D11 buildability).
///     </para>
/// </summary>
public record GetIoTDevicesByUserIdQuery(int UserId);
