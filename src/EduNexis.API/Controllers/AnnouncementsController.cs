using EduNexis.Application.Features.Announcements.Commands;
using EduNexis.Application.Features.Announcements.Queries;
using Microsoft.AspNetCore.Authorization;
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
        Guid courseId, [FromForm] CreateAnnouncementCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId = courseId,
            AuthorId = CurrentUserId
        }, ct));
}
