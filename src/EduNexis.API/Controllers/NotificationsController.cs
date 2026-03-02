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

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new MarkNotificationReadCommand(id, CurrentUserId), ct));

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct) =>
        Ok(await Mediator.Send(new MarkAllNotificationsReadCommand(CurrentUserId), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteNotificationCommand(id, CurrentUserId), ct));
}
