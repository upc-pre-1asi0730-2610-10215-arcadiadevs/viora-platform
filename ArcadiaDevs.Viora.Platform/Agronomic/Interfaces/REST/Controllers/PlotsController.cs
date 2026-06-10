using ArcadiaDevs.Viora.Platform.Agronomic.Application.Commands.CreatePlot;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates.Plot;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.REST.Assemblers;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.REST.DTOs.Requests;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.REST.DTOs.Responses;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

using Cortex.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.REST.Controllers;

/// <summary>
///     REST controller for plot operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class PlotsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PlotsController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator.</param>
    public PlotsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    ///     Creates a new plot with geospatial polygon coordinates.
    /// </summary>
    /// <param name="request">The create plot request.</param>
    /// <returns>201 Created with the plot resource, or 400 Bad Request with error details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePlotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePlotRequest request)
    {
        var command = request.ToCommand();
        var result = await _mediator.SendCommandAsync<CreatePlotCommand, Result<Plot, Error>>(command);

        return result.Match<IActionResult>(
            plot => CreatedAtAction(nameof(Create), new { id = plot.Id }, plot.ToResponse()),
            error => BadRequest(error));
    }
}