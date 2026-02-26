using EduNexis.Application.Features.Assignments.Commands;
using EduNexis.Application.Features.Assignments.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class AssignmentsController : BaseController
{
    [HttpGet("courses/{courseId:guid}/assignments")]
    public async Task<IActionResult> GetAll(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetAssignmentsQuery(courseId), ct));

    [HttpPost("courses/{courseId:guid}/assignments")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Create(
        Guid courseId,
        [FromForm] string title,
        [FromForm] string? instructions,
        [FromForm] DateTime deadline,
        [FromForm] bool allowLateSubmission,
        [FromForm] decimal maxMarks,
        [FromForm] string? rubricNotes,
        IFormFile? referenceFile,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new CreateAssignmentCommand(
            CourseId: courseId,
            CreatedById: CurrentUserId,
            Title: title,
            Instructions: instructions,
            Deadline: deadline,
            AllowLateSubmission: allowLateSubmission,
            MaxMarks: maxMarks,
            RubricNotes: rubricNotes,
            ReferenceFileStream: referenceFile?.OpenReadStream(),
            ReferenceFileName: referenceFile?.FileName
        ), ct));

    [HttpPost("assignments/{assignmentId:guid}/submit")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(
        Guid assignmentId,
        [FromForm] SubmissionType submissionType,
        [FromForm] string? textContent,
        IFormFile? file,
        [FromForm] string? linkUrl,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new SubmitAssignmentCommand(
            AssignmentId: assignmentId,
            StudentId: CurrentUserId,
            SubmissionType: submissionType,
            TextContent: textContent,
            FileStream: file?.OpenReadStream(),
            FileName: file?.FileName,
            LinkUrl: linkUrl
        ), ct));

    [HttpPost("submissions/{submissionId:guid}/grade")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Grade(
        Guid submissionId,
        [FromBody] GradeSubmissionCommand command,
        CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            SubmissionId = submissionId,
            TeacherId = CurrentUserId
        }, ct));
}
