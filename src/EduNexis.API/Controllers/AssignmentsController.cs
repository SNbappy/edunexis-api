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
        Ok(await Mediator.Send(new GetAssignmentsQuery(courseId, CurrentUserId, IsTeacher), ct));

    [HttpPost("courses/{courseId:guid}/assignments")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> Create(
        Guid courseId,
        [FromForm] string title,
        [FromForm] string? instructions,
        [FromForm] DateTime deadline,
        [FromForm] bool allowLateSubmission,
        [FromForm] decimal maxMarks,
        [FromForm] string? rubricNotes,
        IFormFile? referenceFile,
        List<IFormFile>? referenceFiles,
        CancellationToken ct)
    {
        var filesToUpload = new List<(Stream Stream, string FileName)>();
        if (referenceFile is not null)
            filesToUpload.Add((referenceFile.OpenReadStream(), referenceFile.FileName));
        if (referenceFiles is not null)
        {
            foreach (var f in referenceFiles)
            {
                filesToUpload.Add((f.OpenReadStream(), f.FileName));
            }
        }

        return Ok(await Mediator.Send(new CreateAssignmentCommand(
            CourseId: courseId,
            CreatedById: CurrentUserId,
            Title: title,
            Instructions: instructions,
            Deadline: deadline,
            AllowLateSubmission: allowLateSubmission,
            MaxMarks: maxMarks,
            RubricNotes: rubricNotes,
            ReferenceFiles: filesToUpload.Count > 0 ? filesToUpload : null
        ), ct));
    }

    [HttpPut("courses/{courseId:guid}/assignments/{id:guid}")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> Update(
        Guid courseId,
        Guid id,
        [FromForm] string title,
        [FromForm] string? instructions,
        [FromForm] DateTime deadline,
        [FromForm] bool allowLateSubmission,
        [FromForm] decimal maxMarks,
        [FromForm] string? rubricNotes,
        [FromForm] bool manageReferenceFiles,
        [FromForm] List<string>? keepReferenceFileUrls,
        IFormFile? referenceFile,
        List<IFormFile>? referenceFiles,
        CancellationToken ct)
    {
        var filesToUpload = new List<(Stream Stream, string FileName)>();
        if (referenceFile is not null)
            filesToUpload.Add((referenceFile.OpenReadStream(), referenceFile.FileName));
        if (referenceFiles is not null)
        {
            foreach (var f in referenceFiles)
            {
                filesToUpload.Add((f.OpenReadStream(), f.FileName));
            }
        }

        return Ok(await Mediator.Send(new UpdateAssignmentCommand(
            AssignmentId: id,
            CourseId: courseId,
            RequestedById: CurrentUserId,
            Title: title,
            Instructions: instructions,
            Deadline: deadline,
            AllowLateSubmission: allowLateSubmission,
            MaxMarks: maxMarks,
            RubricNotes: rubricNotes,
            ManageReferenceFiles: manageReferenceFiles,
            KeepReferenceFileUrls: keepReferenceFileUrls,
            NewReferenceFiles: filesToUpload.Count > 0 ? filesToUpload : null
        ), ct));
    }

    [HttpDelete("courses/{courseId:guid}/assignments/{id:guid}")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> Delete(
        Guid courseId, Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteAssignmentCommand(courseId, id, CurrentUserId), ct));

    [HttpPost("assignments/{assignmentId:guid}/submit")]
    [Authorize(Roles = "Student")]
    /// <param name="files">
    /// Repeat the field to attach several. `file` (singular) is still accepted
    /// so an older client keeps working.
    /// </param>
    /// <param name="linkUrls">Repeat for several links; `linkUrl` also accepted.</param>
    /// <param name="keepAttachmentIds">
    /// On an update, the ids of already-submitted attachments the student is
    /// keeping. Only honoured when <paramref name="manageAttachments"/> is true.
    /// </param>
    /// <param name="manageAttachments">
    /// Set by clients that show the existing attachments and let the student remove
    /// them individually. Without it the previous set is kept untouched, so an
    /// older client cannot wipe files it never displayed.
    /// </param>
    public async Task<IActionResult> Submit(
        Guid assignmentId,
        [FromForm] SubmissionType submissionType,
        [FromForm] string? textContent,
        [FromForm] IFormFileCollection? files,
        IFormFile? file,
        [FromForm] string[]? linkUrls,
        [FromForm] string? linkUrl,
        [FromForm] Guid[]? keepAttachmentIds,
        [FromForm] bool manageAttachments,
        CancellationToken ct)
    {
        var incoming = new List<IncomingFile>();
        foreach (var f in (IEnumerable<IFormFile>?)files ?? [])
            incoming.Add(new IncomingFile(f.OpenReadStream(), f.FileName, f.Length));
        if (file is not null)
            incoming.Add(new IncomingFile(file.OpenReadStream(), file.FileName, file.Length));

        var links = new List<string>(linkUrls ?? []);
        if (!string.IsNullOrWhiteSpace(linkUrl)) links.Add(linkUrl);

        return Ok(await Mediator.Send(new SubmitAssignmentCommand(
            AssignmentId: assignmentId,
            StudentId: CurrentUserId,
            SubmissionType: submissionType,
            TextContent: textContent,
            Files: incoming,
            Links: links,
            KeepAttachmentIds: manageAttachments ? keepAttachmentIds ?? [] : null
        ), ct));
    }

    /// <summary>Hands a saved draft in, making it visible to the teacher.</summary>
    [HttpPost("assignments/{assignmentId:guid}/turn-in")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> TurnIn(Guid assignmentId, CancellationToken ct) =>
        Ok(await Mediator.Send(new TurnInAssignmentCommand(assignmentId, CurrentUserId), ct));

    /// <summary>Takes work back for further editing while the assignment is open.</summary>
    [HttpPost("assignments/{assignmentId:guid}/unsubmit")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Unsubmit(Guid assignmentId, CancellationToken ct) =>
        Ok(await Mediator.Send(new UnsubmitAssignmentCommand(assignmentId, CurrentUserId), ct));

    /// <summary>
    /// Closes the assignment. By default every student who turned nothing in is
    /// marked 0; pass awardZeroToNonSubmitters=false to close without that.
    /// </summary>
    [HttpPost("courses/{courseId:guid}/assignments/{assignmentId:guid}/close")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> CloseAssignment(
        Guid courseId, Guid assignmentId,
        [FromQuery] bool awardZeroToNonSubmitters = true,
        CancellationToken ct = default) =>
        Ok(await Mediator.Send(new CloseAssignmentCommand(
            courseId, assignmentId, CurrentUserId, awardZeroToNonSubmitters), ct));

    /* ── Class comments on an assignment ──────────────────────────── */

    [HttpGet("courses/{courseId:guid}/assignments/{assignmentId:guid}/comments")]
    public async Task<IActionResult> GetComments(
        Guid courseId, Guid assignmentId, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new GetAssignmentCommentsQuery(courseId, assignmentId, CurrentUserId), ct));

    [HttpPost("courses/{courseId:guid}/assignments/{assignmentId:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid courseId, Guid assignmentId,
        [FromBody] AssignmentCommentRequest body,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new AddAssignmentCommentCommand(
            courseId, assignmentId, CurrentUserId, body.Content, body.ParentCommentId), ct));

    [HttpPut("courses/{courseId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> EditComment(
        Guid courseId, Guid commentId,
        [FromBody] AssignmentCommentRequest body,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new EditAssignmentCommentCommand(
            courseId, commentId, CurrentUserId, body.Content), ct));

    [HttpDelete("courses/{courseId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(
        Guid courseId, Guid commentId, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteAssignmentCommentCommand(
            courseId, commentId, CurrentUserId), ct));

    [HttpPost("submissions/{submissionId:guid}/grade")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> Grade(
        Guid submissionId,
        [FromBody] GradeSubmissionCommand command,
        CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            SubmissionId = submissionId,
            TeacherId = CurrentUserId
        }, ct));

    [HttpGet("assignments/{assignmentId:guid}/submissions")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> GetSubmissions(
        Guid assignmentId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetSubmissionsQuery(assignmentId), ct));

    [HttpGet("assignments/{assignmentId:guid}/my-submission")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMySubmission(
        Guid assignmentId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetMySubmissionQuery(assignmentId, CurrentUserId), ct));
}

/// <summary>ParentCommentId is optional — set it to reply to a comment.</summary>
public record AssignmentCommentRequest(string Content, Guid? ParentCommentId = null);
