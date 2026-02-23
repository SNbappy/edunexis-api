using EduNexis.Application.Features.CT.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
[Route("api/ct")]
public class CTController : BaseController
{
    [HttpPost("courses/{courseId:guid}/events")]
    public async Task<IActionResult> CreateEvent(
        Guid courseId, [FromBody] CreateCTEventCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId = courseId,
            TeacherId = CurrentUserId
        }, ct));

    [HttpPost("events/{ctEventId:guid}/upload-copies")]
    public async Task<IActionResult> UploadCopies(
        Guid ctEventId, [FromForm] UploadCTCopiesCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CTEventId = ctEventId,
            StudentId = CurrentUserId
        }, ct));

    [HttpPost("events/{ctEventId:guid}/grade")]
    public async Task<IActionResult> Grade(
        Guid ctEventId, [FromBody] GradeCTSubmissionCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CTEventId = ctEventId,
            TeacherId = CurrentUserId
        }, ct));
}
