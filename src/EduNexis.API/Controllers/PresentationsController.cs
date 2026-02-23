using EduNexis.Application.Features.Presentations.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class PresentationsController : BaseController
{
    [HttpPost("courses/{courseId:guid}/presentations")]
    public async Task<IActionResult> CreateEvent(
        Guid courseId, [FromBody] CreatePresentationEventCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId = courseId,
            TeacherId = CurrentUserId
        }, ct));

    [HttpPost("presentations/{presentationId:guid}/grade")]
    public async Task<IActionResult> Grade(
        Guid presentationId, [FromBody] GradePresentationCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            PresentationEventId = presentationId,
            TeacherId = CurrentUserId
        }, ct));
}
