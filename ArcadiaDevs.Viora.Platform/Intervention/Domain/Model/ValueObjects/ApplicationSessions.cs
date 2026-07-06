namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The number of application sessions for a single
///     <see cref="PrescribedProduct" /> (REQ-TP-3).
/// </summary>
public record ApplicationSessions
{
    public int Count { get; }

    public ApplicationSessions(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentException("Application sessions must be positive.", nameof(count));
        }

        Count = count;
    }
}
