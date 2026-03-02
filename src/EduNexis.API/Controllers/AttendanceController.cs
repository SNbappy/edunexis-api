using EduNexis.Application.Features.Attendance.Commands;
using EduNexis.Application.Features.Attendance.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class AttendanceController : BaseController
{
    [HttpGet("courses/{courseId:guid}/sessions")]
    public async Task<IActionResult> GetSessions(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetAttendanceSessionsQuery(courseId), ct));

    [HttpPost("courses/{courseId:guid}/sessions")]
    public async Task<IActionResult> CreateSession(
        Guid courseId,
        [FromBody] CreateAttendanceSessionCommand command,
        CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId = courseId,
            CreatedById = CurrentUserId
        }, ct));

    [HttpPut("courses/{courseId:guid}/sessions/{sessionId:guid}")]
    public async Task<IActionResult> UpdateSession(
        Guid courseId, Guid sessionId,
        [FromBody] UpdateAttendanceEntryCommand command,
        CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId = courseId,
            SessionId = sessionId,
            RequesterId = CurrentUserId
        }, ct));

    [HttpDelete("courses/{courseId:guid}/sessions/{sessionId:guid}")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> DeleteSession(
        Guid courseId, Guid sessionId, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteAttendanceSessionCommand(courseId, sessionId), ct));

    [HttpGet("courses/{courseId:guid}/summary")]
    public async Task<IActionResult> GetSummary(
        Guid courseId,
        [FromQuery] Guid? studentId,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new GetAttendanceSummaryQuery(courseId, studentId), ct));
}

