using EduNexis.Application.Features.Materials.Commands;
using EduNexis.Application.Features.Materials.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class MaterialsController : BaseController
{
    [HttpGet("courses/{courseId:guid}/materials")]
    public async Task<IActionResult> GetAll(
        Guid courseId, [FromQuery] string? category, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetMaterialsQuery(courseId, category), ct));

    [HttpPost("courses/{courseId:guid}/materials")]
    public async Task<IActionResult> Upload(
        Guid courseId, [FromForm] UploadMaterialCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId = courseId,
            UploadedById = CurrentUserId
        }, ct));
}
