using EduNexis.Application.Features.Courses.Commands;
using EduNexis.Application.Features.Courses.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class CoursesController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetMyCourses(
        [FromQuery] UserRole role, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetMyCoursesQuery(CurrentUserId, role), ct));

    [HttpGet("{courseId:guid}")]
    public async Task<IActionResult> Get(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCourseQuery(courseId), ct));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromForm] CreateCourseCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { TeacherId = CurrentUserId }, ct));

    [HttpPut("{courseId:guid}")]
    public async Task<IActionResult> Update(
        Guid courseId, [FromBody] UpdateCourseCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId = courseId,
            RequesterId = CurrentUserId
        }, ct));

    [HttpPost("{courseId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new ArchiveCourseCommand(courseId, CurrentUserId), ct));

    [HttpPost("join")]
    public async Task<IActionResult> Join(
        [FromBody] RequestJoinCourseCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { StudentId = CurrentUserId }, ct));

    [HttpGet("{courseId:guid}/join-requests")]
    public async Task<IActionResult> GetJoinRequests(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPendingJoinRequestsQuery(courseId, CurrentUserId), ct));

    [HttpPost("join-requests/{requestId:guid}/review")]
    public async Task<IActionResult> ReviewJoinRequest(
        Guid requestId, [FromBody] bool approve, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new ReviewJoinRequestCommand(requestId, CurrentUserId, approve), ct));
}
