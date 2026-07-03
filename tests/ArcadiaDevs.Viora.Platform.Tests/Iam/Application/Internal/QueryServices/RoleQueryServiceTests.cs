using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Application.Internal.QueryServices;

/// <summary>
///     Unit tests for <see cref="RoleQueryService"/>.
///     Template B: command/query service with NSubstitute collaborators.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class RoleQueryServiceTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly RoleQueryService _sut;

    public RoleQueryServiceTests()
    {
        _sut = new RoleQueryService(_roleRepository);
    }

    /// <summary>
    ///     GIVEN the repository returns multiple roles
    ///     WHEN <see cref="RoleQueryService.Handle"/> is called with <see cref="GetAllRolesQuery"/>
    ///     THEN all roles from the repository are returned.
    /// </summary>
    [Fact]
    public async Task Handle_GetAllRoles_ReturnsAllRoles()
    {
        // GIVEN the repository returns multiple roles
        var roles = new List<Role>
        {
            CreateRole(id: 1, name: "Grower"),
            CreateRole(id: 2, name: "Specialist"),
            CreateRole(id: 3, name: "Admin"),
        };
        _roleRepository.ListAsync(Arg.Any<CancellationToken>())
                        .Returns(roles);

        // WHEN the query service handles the get-all query
        var result = await _sut.Handle(new GetAllRolesQuery(), CancellationToken.None);

        // THEN all three roles are returned
        var enumerable = result.ToList();
        Assert.Equal(3, enumerable.Count);
        Assert.Equal("Grower", enumerable[0].Name);
        Assert.Equal("Specialist", enumerable[1].Name);
        Assert.Equal("Admin", enumerable[2].Name);

        // AND the repository was called exactly once
        await _roleRepository.Received(1).ListAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN the repository returns an empty collection
    ///     WHEN <see cref="RoleQueryService.Handle"/> is called
    ///     THEN an empty enumerable is returned (never null).
    /// </summary>
    [Fact]
    public async Task Handle_GetAllRoles_EmptyDatabase_ReturnsEmptyList()
    {
        // GIVEN the repository returns an empty collection
        _roleRepository.ListAsync(Arg.Any<CancellationToken>())
                        .Returns(new List<Role>());

        // WHEN the query service handles the get-all query
        var result = await _sut.Handle(new GetAllRolesQuery(), CancellationToken.None);

        // THEN an empty enumerable is returned (never null)
        var enumerable = result.ToList();
        Assert.Empty(enumerable);
        Assert.NotNull(result);
    }

    /// <summary>
    ///     GIVEN the repository returns a single role
    ///     WHEN the query service handles the query
    ///     THEN the single role is returned with correct properties.
    /// </summary>
    [Fact]
    public async Task Handle_GetAllRoles_SingleRole_ReturnsCorrectly()
    {
        // GIVEN the repository returns a single role
        var role = CreateRole(id: 42, name: "Grower");
        _roleRepository.ListAsync(Arg.Any<CancellationToken>())
                        .Returns(new List<Role> { role });

        // WHEN the query service handles the get-all query
        var result = await _sut.Handle(new GetAllRolesQuery(), CancellationToken.None);

        // THEN the single role is returned with correct properties
        var enumerable = result.ToList();
        Assert.Single(enumerable);
        Assert.Equal(42, enumerable[0].Id);
        Assert.Equal("Grower", enumerable[0].Name);
    }

    // ---- helpers ----

    private static Role CreateRole(int id, string name)
    {
        var result = Role.Create(name);
        var role = ((Result<Role, Error>.Success)result).Value!;
        typeof(Role).GetProperty(nameof(Role.Id))!.SetValue(role, id);
        return role;
    }
}
