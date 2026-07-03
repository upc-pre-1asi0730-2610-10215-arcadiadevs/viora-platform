using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Interfaces.Rest.Transform;

/// <summary>
///     Unit tests for <see cref="RoleResourceFromEntityAssembler"/>.
///     Template A: pure static function, no DI.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class RoleResourceFromEntityAssemblerTests
{
    /// <summary>
    ///     GIVEN a <see cref="Role"/> entity with Name and Description
    ///     WHEN <see cref="RoleResourceFromEntityAssembler.ToResource"/> is called
    ///     THEN all entity fields are correctly projected to <see cref="RoleResource"/>.
    /// </summary>
    [Fact]
    public void FromEntity_ValidRole_ReturnsCorrectResource()
    {
        // GIVEN a Role entity with Name and Description
        var role = CreateRole(id: 7, name: "Specialist", description: "Domain expert");

        // WHEN the assembler maps the entity to a resource
        var resource = role.ToResource();

        // THEN all entity fields are correctly projected
        Assert.Equal(7, resource.Id);
        Assert.Equal("Specialist", resource.Name);
        Assert.Equal("Domain expert", resource.Description);
    }

    /// <summary>
    ///     GIVEN a <see cref="Role"/> entity with a null Description
    ///     WHEN <see cref="RoleResourceFromEntityAssembler.ToResource"/> is called
    ///     THEN the resource's Description is null (graceful handling).
    /// </summary>
    [Fact]
    public void FromEntity_NullDescription_HandlesGracefully()
    {
        // GIVEN a Role entity with null Description
        var role = CreateRole(id: 1, name: "Grower", description: null);

        // WHEN the assembler maps the entity to a resource
        var resource = role.ToResource();

        // THEN the resource's Description is null
        Assert.Equal(1, resource.Id);
        Assert.Equal("Grower", resource.Name);
        Assert.Null(resource.Description);
    }

    // ---- helpers ----

    private static Role CreateRole(int id, string name, string? description = null)
    {
        var result = Role.Create(name, description);
        var role = ((Result<Role, Error>.Success)result).Value!;
        typeof(Role).GetProperty(nameof(Role.Id))!.SetValue(role, id);
        return role;
    }
}
