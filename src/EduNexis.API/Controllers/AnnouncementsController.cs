using EduNexis.Application.Features.Announcements.Commands;
using EduNexis.Application.Features.Announcements.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class AnnouncementsController : BaseController
{
    [HttpGet("courses/{courseId:guid}/announcements")]
    public async Task<IActionResult> GetAll(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetAnnouncementsQuery(courseId), ct));

    [HttpPost("courses/{courseId:guid}/announcements")]
    public async Task<IActionResult> Create(
        Guid courseId,
        [FromForm] string content,
        IFormFile? attachment,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new CreateAnnouncementCommand(
            CourseId: courseId,
            AuthorId: CurrentUserId,
            Content: content,
            AttachmentStream: attachment?.OpenReadStream(),
            AttachmentFileName: attachment?.FileName
        ), ct));
}
