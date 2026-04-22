using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Courses.Commands;
using EduNexis.Application.Features.Courses.Queries;
using EduNexis.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

public record ReviewJoinRequestBody(bool Approve);
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

    /// <summary>
    /// Admin-only course listing. Regular users use /my-courses.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? teacherId,
        [FromQuery] Guid? studentId,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCoursesQuery(teacherId, studentId), ct));

    /// <summary>
    /// Returns the caller's enrolled + pending + rejected courses.
    /// </summary>
    [HttpGet("my-courses")]
    public async Task<IActionResult> GetMyCourses(CancellationToken ct)
    {
        var userId = Guid.Parse(_currentUser.UserId);
        var role   = Enum.Parse<UserRole>(_currentUser.Role ?? "Student");
        return Ok(await Mediator.Send(new GetMyCoursesQuery(userId, role), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCourseQuery(id), ct));

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCourseMembersQuery(id), ct));

    [HttpGet("{id:guid}/join-requests")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetJoinRequests(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new GetPendingJoinRequestsQuery(id, Guid.Parse(_currentUser.UserId)), ct));

    // ──────────────────────────────────────────────────────────────
    // CRUD (teachers)
    // ──────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCourseCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateCourseCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteCourseCommand(id), ct));

    [HttpPatch("{id:guid}/archive")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new ArchiveCourseCommand(id, Guid.Parse(_currentUser.UserId)), ct));

    [HttpPatch("{id:guid}/unarchive")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new UnarchiveCourseCommand(id), ct));

    // ──────────────────────────────────────────────────────────────
    // Join requests
    // ──────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/join-requests/{requestId:guid}/review")]
    [Authorize(Roles = "Teacher,Admin")]
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
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new LeaveCourseCommand(id), ct));
}
