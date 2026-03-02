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
        Guid courseId, [FromQuery] string? category, [FromQuery] Guid? parentFolderId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetMaterialsQuery(courseId, category, parentFolderId), ct));

    [HttpPost("courses/{courseId:guid}/materials")]
    public async Task<IActionResult> Upload(
        Guid courseId, [FromForm] UploadMaterialCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId = courseId,
            UploadedById = CurrentUserId
        }, ct));

    [HttpDelete("courses/{courseId:guid}/materials/{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid courseId, Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteMaterialCommand(courseId, id, CurrentUserId), ct));
}
