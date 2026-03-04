using EduNexis.Application.Features.CT.Commands;
using EduNexis.Application.Features.CT.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

public class UploadCTCopiesRequest
{
    public IFormFile? BestCopy { get; set; }
    public Guid? BestStudentId { get; set; }
    public IFormFile? WorstCopy { get; set; }
    public Guid? WorstStudentId { get; set; }
    public IFormFile? AvgCopy { get; set; }
    public Guid? AvgStudentId { get; set; }
}

[Authorize]
[Route("api/ct")]
public class CTController : BaseController
{
    // POST api/ct/courses/{courseId}/events
    [HttpPost("courses/{courseId:guid}/events")]
    public async Task<IActionResult> CreateEvent(
        Guid courseId, [FromBody] CreateCTEventCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId  = courseId,
            TeacherId = CurrentUserId
        }, ct));

    // GET api/ct/courses/{courseId}/events
    [HttpGet("courses/{courseId:guid}/events")]
    public async Task<IActionResult> GetEvents(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCTEventsQuery(courseId, CurrentUserId), ct));

    // PUT api/ct/events/{ctEventId}
    [HttpPut("events/{ctEventId:guid}")]
    public async Task<IActionResult> UpdateEvent(
        Guid ctEventId, [FromBody] UpdateCTEventCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CTEventId = ctEventId,
            TeacherId = CurrentUserId
        }, ct));

    // DELETE api/ct/events/{ctEventId}
    [HttpDelete("events/{ctEventId:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid ctEventId, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteCTEventCommand(ctEventId, CurrentUserId), ct));

    // GET api/ct/events/{ctEventId}/marks
    [HttpGet("events/{ctEventId:guid}/marks")]
    public async Task<IActionResult> GetMarks(Guid ctEventId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCTMarksQuery(ctEventId, CurrentUserId), ct));

    // POST api/ct/events/{ctEventId}/upload-khata
    [HttpPost("events/{ctEventId:guid}/upload-khata")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadKhata(
        Guid ctEventId,
        [FromForm] UploadCTCopiesRequest request,
        CancellationToken ct)
    {
        if (request.BestCopy is null && request.WorstCopy is null && request.AvgCopy is null)
            return BadRequest("At least one khata file is required.");

        var command = new UploadCTCopiesCommand(
            CTEventId:         ctEventId,
            TeacherId:         CurrentUserId,
            BestCopyStream:    request.BestCopy?.OpenReadStream(),
            BestCopyFileName:  request.BestCopy?.FileName,
            BestStudentId:     request.BestStudentId,
            WorstCopyStream:   request.WorstCopy?.OpenReadStream(),
            WorstCopyFileName: request.WorstCopy?.FileName,
            WorstStudentId:    request.WorstStudentId,
            AvgCopyStream:     request.AvgCopy?.OpenReadStream(),
            AvgCopyFileName:   request.AvgCopy?.FileName,
            AvgStudentId:      request.AvgStudentId
        );
        return Ok(await Mediator.Send(command, ct));
    }

    // POST api/ct/events/{ctEventId}/grade
    [HttpPost("events/{ctEventId:guid}/grade")]
    public async Task<IActionResult> BulkGrade(
        Guid ctEventId, [FromBody] BulkGradeCTCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CTEventId = ctEventId,
            TeacherId = CurrentUserId
        }, ct));

    // POST api/ct/events/{ctEventId}/publish
    [HttpPost("events/{ctEventId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid ctEventId, CancellationToken ct) =>
        Ok(await Mediator.Send(new PublishCTCommand(ctEventId, CurrentUserId), ct));

    // POST api/ct/events/{ctEventId}/unpublish
    [HttpPost("events/{ctEventId:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid ctEventId, CancellationToken ct) =>
        Ok(await Mediator.Send(new UnpublishCTCommand(ctEventId, CurrentUserId), ct));
}



