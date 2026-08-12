using EduNexis.Application.Features.Admin.Commands;
using EduNexis.Application.Features.Admin.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize(Roles = "SuperAdmin,DepartmentAdmin")]
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

    [HttpPost("teachers/{teacherId:guid}/quota")]
    public async Task<IActionResult> GrantQuota(
        Guid teacherId, [FromBody] GrantTeacherQuotaBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(
            new GrantTeacherQuotaCommand(teacherId, body.TotalQuota, body.AccessDurationDays), ct));
}

public record GrantTeacherQuotaBody(int TotalQuota, int AccessDurationDays);