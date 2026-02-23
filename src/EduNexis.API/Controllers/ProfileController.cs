using EduNexis.Application.Features.Profile.Commands;
using EduNexis.Application.Features.Profile.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
public class ProfileController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetProfileQuery(CurrentUserId), ct));

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateProfileCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { UserId = CurrentUserId }, ct));

    [HttpPost("photo")]
    public async Task<IActionResult> UploadPhoto(
        IFormFile file, CancellationToken ct)
    {
        var command = new UploadProfilePhotoCommand(
            CurrentUserId, file.OpenReadStream(), file.FileName);
        return Ok(await Mediator.Send(command, ct));
    }
}
