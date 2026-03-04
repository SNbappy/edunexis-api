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
        Guid courseId,
        [FromQuery] string? category,
        [FromQuery] Guid? parentFolderId,
        [FromQuery] bool flatten = false,
        CancellationToken ct = default) =>
        Ok(await Mediator.Send(new GetMaterialsQuery(courseId, category, parentFolderId, flatten), ct));

    [HttpPost("courses/{courseId:guid}/materials")]
    public async Task<IActionResult> Upload(
        Guid courseId,
        [FromForm] string title,
        [FromForm] string type,
        [FromForm] string? embedUrl,
        [FromForm] string? description,
        [FromForm] string? category,
        [FromForm] Guid? parentFolderId,
        IFormFile? file,
        CancellationToken ct)
    {
        if (!Enum.TryParse<MaterialType>(type, ignoreCase: true, out var materialType))
            return BadRequest("Invalid material type.");

        var command = new UploadMaterialCommand(
            CourseId:       courseId,
            UploadedById:   CurrentUserId,
            Title:          title,
            Type:           materialType,
            FileStream:     file?.OpenReadStream(),
            FileName:       file?.FileName,
            EmbedUrl:       embedUrl,
            Description:    description,
            Category:       category,
            ParentFolderId: parentFolderId
        );

        return Ok(await Mediator.Send(command, ct));
    }

    [HttpDelete("courses/{courseId:guid}/materials/{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid courseId, Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteMaterialCommand(courseId, id, CurrentUserId), ct));
}
