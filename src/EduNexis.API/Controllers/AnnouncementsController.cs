using EduNexis.Application.Features.Announcements.Commands;
using EduNexis.Application.Features.Announcements.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class AnnouncementsController : BaseController
{
    [HttpGet("courses/{courseId:guid}/announcements")]
    public async Task<IActionResult> GetAll(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetAnnouncementsQuery(courseId), ct));

    [HttpPost("courses/{courseId:guid}/announcements")]
    public async Task<IActionResult> Create(
        Guid courseId,
        [FromForm] string content,
        IFormFile? attachment,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new CreateAnnouncementCommand(
            CourseId: courseId,
            AuthorId: CurrentUserId,
            Content: content,
            AttachmentStream: attachment?.OpenReadStream(),
            AttachmentFileName: attachment?.FileName
        ), ct));

    [HttpDelete("courses/{courseId:guid}/announcements/{id:guid}")]
    public async Task<IActionResult> Delete(Guid courseId, Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteAnnouncementCommand(courseId, id), ct));

    [HttpPatch("courses/{courseId:guid}/announcements/{id:guid}/pin")]
    [Authorize(Roles = "Teacher,SuperAdmin,DepartmentAdmin")]
    public async Task<IActionResult> TogglePin(Guid courseId, Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new PinAnnouncementCommand(courseId, id), ct));

    /* ── Class comments ──────────────────────────────────────────────
       Open to students as well as teachers: the point is a conversation
       under the announcement, the way it works in a class group. The
       handlers check course membership, and the archive guard freezes
       all three on an archived course. */

    [HttpGet("courses/{courseId:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCommentsQuery(courseId, CurrentUserId), ct));

    [HttpPost("courses/{courseId:guid}/announcements/{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid courseId, Guid id,
        [FromBody] AddCommentRequest body,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new AddCommentCommand(
            CourseId: courseId,
            AnnouncementId: id,
            AuthorId: CurrentUserId,
            Content: body.Content
        ), ct));

    [HttpPut("courses/{courseId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> EditComment(
        Guid courseId, Guid commentId,
        [FromBody] AddCommentRequest body,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new EditCommentCommand(
            courseId, commentId, CurrentUserId, body.Content), ct));

    [HttpDelete("courses/{courseId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(
        Guid courseId, Guid commentId, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteCommentCommand(courseId, commentId, CurrentUserId), ct));
}

public record AddCommentRequest(string Content);
