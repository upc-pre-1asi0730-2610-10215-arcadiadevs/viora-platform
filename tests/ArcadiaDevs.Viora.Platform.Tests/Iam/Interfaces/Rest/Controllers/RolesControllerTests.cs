using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Controllers;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Interfaces.Rest.Controllers;

/// <summary>
///     Unit tests for <see cref="RolesController"/>.
///     Template C: controller tests with a fake <see cref="HttpContext"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class RolesControllerTests
{
    private readonly IRoleQueryService _roleQueryService = Substitute.For<IRoleQueryService>();

    private RolesController CreateController()
    {
        var controller = new RolesController(_roleQueryService);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };
        return controller;
    }

    /// <summary>
    ///     GIVEN roles exist in the repository
    ///     WHEN <see cref="RolesController.GetAll"/> is called
    ///     THEN the response contains all roles mapped to <see cref="RoleResource"/>.
    /// </summary>
    [Fact]
    public async Task GetAllRoles_ReturnsOkWithRoles()
    {
        // GIVEN roles exist in the repository
        var roles = new List<Role>
        {
            CreateRole(id: 1, name: "Grower", description: "Primary role"),
            CreateRole(id: 2, name: "Specialist", description: "Expert role"),
        };
        _roleQueryService.Handle(Arg.Any<GetAllRolesQuery>(), Arg.Any<CancellationToken>())
                         .Returns(roles);

        var controller = CreateController();

        // WHEN the controller handles GET /api/v1/roles
        var result = await controller.GetAll(CancellationToken.None);

        // THEN the result is 200 OK with the role resources
        var ok = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<RoleResource>>(ok.Value);
        Assert.Equal(2, resources.Count());
    }

    /// <summary>
    ///     GIVEN no roles exist
    ///     WHEN <see cref="RolesController.GetAll"/> is called
    ///     THEN an empty array is returned.
    /// </summary>
    [Fact]
    public async Task GetAllRoles_EmptyDatabase_ReturnsOkWithEmptyList()
    {
        // GIVEN no roles exist
        _roleQueryService.Handle(Arg.Any<GetAllRolesQuery>(), Arg.Any<CancellationToken>())
                         .Returns(new List<Role>());

        var controller = CreateController();

        // WHEN the controller handles GET /api/v1/roles
        var result = await controller.GetAll(CancellationToken.None);

        // THEN the result is 200 OK with an empty enumerable
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var resources = Assert.IsAssignableFrom<IEnumerable<RoleResource>>(ok.Value);
        Assert.Empty(resources);
    }

    /// <summary>
    ///     GIVEN roles with specific names and descriptions
    ///     WHEN <see cref="RolesController.GetAll"/> is called
    ///     THEN the <see cref="RoleResource"/> mapping preserves all entity fields.
    /// </summary>
    [Fact]
    public async Task GetAllRoles_ReturnsCorrectRoleNames()
    {
        // GIVEN roles with specific names and descriptions
        var roles = new List<Role>
        {
            CreateRole(id: 1, name: "Grower", description: "Primary role"),
            CreateRole(id: 3, name: "Admin", description: null),
        };
        _roleQueryService.Handle(Arg.Any<GetAllRolesQuery>(), Arg.Any<CancellationToken>())
                         .Returns(roles);

        var controller = CreateController();

        // WHEN the controller handles GET /api/v1/roles
        var result = await controller.GetAll(CancellationToken.None);

        // THEN each RoleResource maps from the entity correctly
        var ok = Assert.IsType<OkObjectResult>(result);
        var resources = Assert.IsAssignableFrom<IEnumerable<RoleResource>>(ok.Value).ToList();

        Assert.Equal(1, resources[0].Id);
        Assert.Equal("Grower", resources[0].Name);
        Assert.Equal("Primary role", resources[0].Description);

        Assert.Equal(3, resources[1].Id);
        Assert.Equal("Admin", resources[1].Name);
        Assert.Null(resources[1].Description);
    }

    // ---- helpers ----

    private static Role CreateRole(int id, string name, string? description)
    {
        var result = Role.Create(name, description);
        var role = ((Result<Role, Error>.Success)result).Value!;
        // Set the Id via backing field (EF Core materialisation pattern)
        typeof(Role).GetProperty(nameof(Role.Id))!.SetValue(role, id);
        return role;
    }
}
