using EduNexis.Application.Features.Attendance.Commands;
using EduNexis.Application.Features.Attendance.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class AttendanceController : BaseController
{
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

    [HttpGet("courses/{courseId:guid}/summary")]
    public async Task<IActionResult> GetSummary(
        Guid courseId,
        [FromQuery] Guid? studentId,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new GetAttendanceSummaryQuery(courseId, studentId), ct));
}
