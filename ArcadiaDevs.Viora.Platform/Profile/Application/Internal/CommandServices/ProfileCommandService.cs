using ArcadiaDevs.Viora.Platform.Profile.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Profile.Application.Internal.CommandServices;

/// <summary>
///     Handles profile create-or-update commands with upsert semantics.
/// </summary>
public class ProfileCommandService(
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork) : IProfileCommandService
{
    /// <inheritdoc />
    public async Task<Result<(Profile Profile, bool Created), Error>> Handle(
        CreateOrUpdateProfileCommand command,
        CancellationToken ct)
    {
        var existing = await profileRepository.FindByUserIdAsync(command.UserId, ct);

        if (existing is null)
        {
            // Create path — default role to Producer per spec REQ.
            var profile = new Profile(
                command.UserId,
                ProfileRole.Producer,
                command.FullName ?? string.Empty,
                command.Email ?? string.Empty,
                command.Phone,
                command.JobTitle,
                command.Language,
                command.Location,
                command.SpecialtyArea);

            await profileRepository.AddAsync(profile, ct);
            await unitOfWork.CompleteAsync(ct);

            return new Result<(Profile Profile, bool Created), Error>.Success((profile, true));
        }

        // Update path — null-safe partial update, Role untouched.
        existing.ApplyUpdate(
            command.FullName,
            command.Email,
            command.Phone,
            command.JobTitle,
            command.Language,
            command.Location,
            command.SpecialtyArea);

        profileRepository.Update(existing);
        await unitOfWork.CompleteAsync(ct);

        return new Result<(Profile Profile, bool Created), Error>.Success((existing, false));
    }
}
