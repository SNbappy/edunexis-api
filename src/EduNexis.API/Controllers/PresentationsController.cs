using EduNexis.Application.Features.Presentations.Commands;
using EduNexis.Application.Features.Presentations.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
[Route("api")]
public class PresentationsController : BaseController
{
    [HttpGet("courses/{courseId:guid}/presentations")]
    public async Task<IActionResult> GetAll(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPresentationsQuery(courseId, CurrentUserId), ct));

    [HttpPost("courses/{courseId:guid}/presentations")]
    public async Task<IActionResult> CreateEvent(
        Guid courseId, [FromBody] CreatePresentationEventCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { CourseId = courseId, TeacherId = CurrentUserId }, ct));

    [HttpGet("presentations/{presentationId:guid}/results")]
    public async Task<IActionResult> GetResults(Guid presentationId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPresentationResultsQuery(presentationId, CurrentUserId), ct));

    [HttpPost("presentations/{presentationId:guid}/marks")]
    public async Task<IActionResult> SaveMarks(
        Guid presentationId, [FromBody] SavePresentationMarksCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { PresentationEventId = presentationId, TeacherId = CurrentUserId }, ct));

    [HttpPatch("presentations/{presentationId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid presentationId, [FromBody] UpdatePresentationStatusCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { PresentationEventId = presentationId, TeacherId = CurrentUserId }, ct));

    [HttpDelete("presentations/{presentationId:guid}")]
    public async Task<IActionResult> Delete(Guid presentationId, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeletePresentationCommand(presentationId, CurrentUserId), ct));

    [HttpPost("presentations/{presentationId:guid}/grade")]
    public async Task<IActionResult> Grade(
        Guid presentationId, [FromBody] GradePresentationCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { PresentationEventId = presentationId, TeacherId = CurrentUserId }, ct));
}
