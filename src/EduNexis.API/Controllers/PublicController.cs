using EduNexis.Application.Features.Public.Queries;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

/// <summary>
/// Public-facing endpoints. NO authentication required.
/// Strictly read-only. Returns DTOs that exclude private fields (email, phone for non-faculty data).
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private IMediator? _mediator;
    private IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    /// <summary>List of teachers who opted into public visibility.</summary>
    [HttpGet("faculty")]
    public async Task<IActionResult> GetFaculty(
        [FromQuery] string? department,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken ct = default) =>
        Ok(await Mediator.Send(new GetPublicFacultyListQuery(department, page, pageSize), ct));

    /// <summary>Single teacher by slug.</summary>
    [HttpGet("faculty/{slug}")]
    public async Task<IActionResult> GetFacultyBySlug(string slug, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPublicFacultyBySlugQuery(slug), ct));

    /// <summary>Departments that have at least one public teacher (for filter chips).</summary>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPublicDepartmentsQuery(), ct));

    /// <summary>Site-wide stats for the homepage.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPublicStatsQuery(), ct));
}