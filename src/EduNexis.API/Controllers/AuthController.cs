using EduNexis.Application.Features.Auth.Commands;
using EduNexis.Application.Features.Auth.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

public class AuthController : BaseController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command, ct));

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] SyncUserCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command, ct));

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCurrentUserQuery(CurrentUserId), ct));
}
