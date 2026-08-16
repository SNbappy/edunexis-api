using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Application.Features.Notifications.Queries;
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

    /* ── Preferences ─────────────────────────────────────────────────
       Everything is on unless the user turns it off, so a brand-new
       account and a brand-new notification type both start switched on. */

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetNotificationPreferencesQuery(CurrentUserId), ct));

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest body,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new UpdateNotificationPreferencesCommand(
            CurrentUserId, body.Preferences ?? []), ct));
}

public record UpdateNotificationPreferencesRequest(
    List<NotificationPreferenceInput> Preferences);
