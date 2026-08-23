using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Courses.Commands;
using EduNexis.Application.Features.Courses.Queries;
using EduNexis.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

public record ReviewJoinRequestBody(bool Approve);
public record DeleteCourseBody(string Password, string CourseCodeConfirmation);
public record RequestJoinBody(string JoiningCode);

[Authorize]
public class CoursesController : BaseController
{
    private readonly ICurrentUserService _currentUser;

    public CoursesController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    // ──────────────────────────────────────────────────────────────
    // Listing
    // ──────────────────────────────────────────────────────────────

    /// <summary>Admin-only course listing. Regular users use /my-courses.</summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? teacherId,
        [FromQuery] Guid? studentId,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCoursesQuery(teacherId, studentId), ct));

    /// <summary>Returns the caller's enrolled + pending + rejected courses.</summary>
    [HttpGet("my-courses")]
    public async Task<IActionResult> GetMyCourses(CancellationToken ct)
    {
        var userId = Guid.Parse(_currentUser.UserId);
        var role   = Enum.Parse<UserRole>(_currentUser.Role ?? "Student");
        return Ok(await Mediator.Send(new GetMyCoursesQuery(userId, role), ct));
    }

    /// <summary>Returns the caller's current course-creation quota status.</summary>
    [HttpGet("my-quota")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> GetMyQuota(CancellationToken ct)
    {
        var teacherId = Guid.Parse(_currentUser.UserId);
        return Ok(await Mediator.Send(new GetMyQuotaQuery(teacherId), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCourseQuery(id), ct));

    /// <summary>Looks up a course by its 8-char joining code. Used for the Join flow preview.</summary>
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCourseByCodeQuery(code), ct));

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCourseMembersQuery(id), ct));

    [HttpGet("{id:guid}/join-requests")]
    public async Task<IActionResult> GetJoinRequests(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new GetPendingJoinRequestsQuery(id, Guid.Parse(_currentUser.UserId)), ct));

    // ──────────────────────────────────────────────────────────────
    // CRUD (teachers)
    // ──────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCourseCommand command, CancellationToken ct)
    {
        // The owner is always the caller. TeacherId arrives on the body and was
        // previously trusted as sent, so a teacher could pass a colleague's id
        // and create a course owned by them — and, once quota enforcement is on,
        // spend that colleague's course allowance. There is no create-on-behalf
        // flow, so the value from the token wins unconditionally.
        var teacherId = Guid.Parse(_currentUser.UserId);
        return Ok(await Mediator.Send(command with { TeacherId = teacherId }, ct));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateCourseCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, [FromBody] DeleteCourseBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteCourseCommand(id, body.Password, body.CourseCodeConfirmation), ct));

    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeleted(CancellationToken ct)
    {
        var teacherId = Guid.Parse(_currentUser.UserId);
        return Ok(await Mediator.Send(new GetMyDeletedCoursesQuery(teacherId), ct));
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new RestoreDeletedCourseCommand(id), ct));

    [HttpDelete("{id:guid}/permanent")]
    public async Task<IActionResult> PermanentlyDelete(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new PermanentlyDeleteCourseCommand(id), ct));

    [HttpPatch("{id:guid}/archive")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new ArchiveCourseCommand(id, Guid.Parse(_currentUser.UserId)), ct));

    [HttpPatch("{id:guid}/unarchive")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new UnarchiveCourseCommand(id), ct));

    // ──────────────────────────────────────────────────────────────
    // Co-teaching
    // ──────────────────────────────────────────────────────────────

    /// <summary>Everyone teaching this course — the owner first, then colleagues.</summary>
    [HttpGet("{id:guid}/teachers")]
    public async Task<IActionResult> GetTeachers(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCourseTeachersQuery(id), ct));

    /// <summary>Pending co-teaching invitations for this course.</summary>
    [HttpGet("{id:guid}/invitations")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> GetCourseInvitations(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCourseInvitationsQuery(id), ct));

    /// <summary>Invites another teacher, by email, to help run this course.</summary>
    [HttpPost("{id:guid}/invitations")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> InviteTeacher(
        Guid id, [FromBody] InviteTeacherRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new InviteTeacherCommand(id, CurrentUserId, body.Email, body.Message), ct));

    /// <summary>Withdraws an invitation that has not been answered.</summary>
    [HttpDelete("{id:guid}/invitations/{invitationId:guid}")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> RevokeInvitation(
        Guid id, Guid invitationId, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new RevokeCourseInvitationCommand(id, invitationId, CurrentUserId), ct));

    /// <summary>Removes a co-teacher. Owner only, or a co-teacher stepping down.</summary>
    [HttpDelete("{id:guid}/teachers/{teacherId:guid}")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> RemoveCoTeacher(
        Guid id, Guid teacherId, CancellationToken ct) =>
        Ok(await Mediator.Send(new RemoveCoTeacherCommand(id, teacherId, CurrentUserId), ct));

    /// <summary>Co-teaching invitations addressed to the caller.</summary>
    [HttpGet("invitations/mine")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> GetMyInvitations(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetMyCourseInvitationsQuery(CurrentUserId), ct));

    /// <summary>Accepts or declines an invitation addressed to the caller.</summary>
    [HttpPost("invitations/{invitationId:guid}/respond")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> RespondToInvitation(
        Guid invitationId, [FromQuery] bool accept, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new RespondToCourseInvitationCommand(invitationId, CurrentUserId, accept), ct));

    /// <summary>
    /// Appoints or removes a class representative. Teacher (or platform admin)
    /// only; a course may have several CRs.
    /// </summary>
    [HttpPatch("{id:guid}/members/{studentId:guid}/cr")]
    [Authorize(Roles = "Teacher,SuperAdmin")]
    public async Task<IActionResult> SetClassRepresentative(
        Guid id, Guid studentId,
        [FromQuery] bool isCr,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new SetClassRepresentativeCommand(id, studentId, isCr), ct));

    // ──────────────────────────────────────────────────────────────
    // Join requests
    // ──────────────────────────────────────────────────────────────

    
    [HttpDelete("{id:guid}/members/{studentId:guid}")]
    public async Task<IActionResult> RemoveMember(
        Guid id, Guid studentId, CancellationToken ct) =>
        Ok(await Mediator.Send(new RemoveCourseMemberCommand(id, studentId), ct));

    [HttpPost("{id:guid}/join-requests/{requestId:guid}/review")]
    public async Task<IActionResult> ReviewJoinRequest(
        Guid id, Guid requestId,
        [FromBody] ReviewJoinRequestBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new ReviewJoinRequestCommand(id, requestId, body.Approve), ct));

    [HttpPost("{id:guid}/join")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> RequestJoin(
        Guid id,
        [FromBody] RequestJoinBody body,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new RequestJoinCourseCommand(id, body.JoiningCode), ct));

    [HttpPost("join-requests/{requestId:guid}/dismiss")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> DismissJoinRequest(
        Guid requestId, CancellationToken ct) =>
        Ok(await Mediator.Send(new DismissJoinRequestCommand(requestId), ct));

    [HttpPost("{id:guid}/leave")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Leave(
        Guid id,
        [FromBody] LeaveCourseRequest req,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new LeaveCourseCommand(id, req.Password), ct));
}

public record InviteTeacherRequest(string Email, string? Message = null);
public record LeaveCourseRequest(string Password);
