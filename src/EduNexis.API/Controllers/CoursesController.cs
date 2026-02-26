using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Courses.Commands;
using EduNexis.Application.Features.Courses.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class CoursesController : BaseController
{
    private readonly ICurrentUserService _currentUser;

    public CoursesController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    // ── Queries ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? teacherId,
        [FromQuery] Guid? studentId,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCoursesQuery(teacherId, studentId), ct));

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

    // ── Teacher / Admin Commands ───────────────────────────────────────────

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

    [HttpPost("{id:guid}/join-requests/{requestId:guid}/review")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> ReviewJoinRequest(
        Guid id, Guid requestId,
        [FromBody] ReviewJoinRequestCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { CourseId = id, RequestId = requestId }, ct));

    // ── Student Commands ───────────────────────────────────────────────────

    [HttpPost("{id:guid}/join")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> RequestJoin(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new RequestJoinCourseCommand(id), ct));

    [HttpPost("{id:guid}/leave")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new LeaveCourseCommand(id), ct));
}
