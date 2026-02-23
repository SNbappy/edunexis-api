using EduNexis.Application.Features.Notifications.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class NotificationsController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetMyNotificationsQuery(CurrentUserId), ct));
}
