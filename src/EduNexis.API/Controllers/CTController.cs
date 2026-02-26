using EduNexis.Application.Features.CT.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

public class UploadCTCopiesRequest
{
    public IFormFile? BestCopy { get; set; }
    public IFormFile? WorstCopy { get; set; }
    public IFormFile? AvgCopy { get; set; }
}

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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCopies(
        Guid ctEventId,
        [FromForm] UploadCTCopiesRequest request,
        CancellationToken ct)
    {
        var command = new UploadCTCopiesCommand(
            CTEventId: ctEventId,
            StudentId: CurrentUserId,
            BestCopyStream: request.BestCopy?.OpenReadStream(),
            BestCopyFileName: request.BestCopy?.FileName,
            WorstCopyStream: request.WorstCopy?.OpenReadStream(),
            WorstCopyFileName: request.WorstCopy?.FileName,
            AvgCopyStream: request.AvgCopy?.OpenReadStream(),
            AvgCopyFileName: request.AvgCopy?.FileName
        );
        return Ok(await Mediator.Send(command, ct));
    }

    [HttpPost("events/{ctEventId:guid}/grade")]
    public async Task<IActionResult> Grade(
        Guid ctEventId, [FromBody] GradeCTSubmissionCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CTEventId = ctEventId,
            TeacherId = CurrentUserId
        }, ct));
}
