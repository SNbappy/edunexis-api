using EduNexis.Application.Features.Admin.Commands;
using EduNexis.Application.Features.Admin.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class AdminController : BaseController
{
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPlatformSettingsQuery(), ct));

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdatePlatformSettingsCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command, ct));

    [HttpGet("teachers")]
    public async Task<IActionResult> GetTeachers(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetTeachersQuery(), ct));

    /// <summary>Full grant history for one teacher, newest first.</summary>
    [HttpGet("teachers/{teacherId:guid}/quota")]
    public async Task<IActionResult> GetTeacherGrants(Guid teacherId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetTeacherGrantsQuery(teacherId), ct));

    /// <summary>Issues a new grant. Additive — `courses` is how many to add.</summary>
    [HttpPost("teachers/{teacherId:guid}/quota")]
    public async Task<IActionResult> GrantQuota(
        Guid teacherId, [FromBody] GrantTeacherQuotaBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new GrantTeacherQuotaCommand(
                teacherId, body.Courses, body.AccessDurationDays, body.Note), ct));

    /// <summary>Withdraws a grant's unspent allowance. Existing courses are unaffected.</summary>
    [HttpDelete("quota/{grantId:guid}")]
    public async Task<IActionResult> RevokeGrant(Guid grantId, CancellationToken ct) =>
        Ok(await Mediator.Send(new RevokeTeacherQuotaCommand(grantId), ct));
}

public record GrantTeacherQuotaBody(int Courses, int AccessDurationDays, string? Note = null);