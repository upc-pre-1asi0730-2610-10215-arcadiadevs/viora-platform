using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;

/**
 * <summary>
 *     The role aggregate
 * </summary>
 * <remarks>
 *     This class is used to represent a role that can be assigned to users
 * </remarks>
 */
public partial class Role
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ICollection<User> Users { get; private set; } = [];

    private Role()
    {
    }

    /**
     * <summary>
     *     Create a new role
     * </summary>
     * <param name="name">The role name</param>
     * <param name="description">The optional role description</param>
     * <returns>A result containing the role or an error</returns>
     */
    public static Result<Role, Error> Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
            return new Result<Role, Error>.Failure(IamErrors.InvalidRoleName);
        return new Result<Role, Error>.Success(new Role { Name = name.Trim(), Description = description?.Trim() });
    }
}
