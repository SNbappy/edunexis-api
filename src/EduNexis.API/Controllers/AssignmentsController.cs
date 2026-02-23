using EduNexis.Application.Features.Assignments.Commands;
using EduNexis.Application.Features.Assignments.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class AssignmentsController : BaseController
{
    [HttpGet("courses/{courseId:guid}/assignments")]
    public async Task<IActionResult> GetAll(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetAssignmentsQuery(courseId), ct));

    [HttpPost("courses/{courseId:guid}/assignments")]
    public async Task<IActionResult> Create(
        Guid courseId, [FromForm] CreateAssignmentCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId = courseId,
            CreatedById = CurrentUserId
        }, ct));

    [HttpPost("assignments/{assignmentId:guid}/submit")]
    public async Task<IActionResult> Submit(
        Guid assignmentId, [FromForm] SubmitAssignmentCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            AssignmentId = assignmentId,
            StudentId = CurrentUserId
        }, ct));

    [HttpPost("submissions/{submissionId:guid}/grade")]
    public async Task<IActionResult> Grade(
        Guid submissionId, [FromBody] GradeSubmissionCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            SubmissionId = submissionId,
            TeacherId = CurrentUserId
        }, ct));
}
