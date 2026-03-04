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

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetPublic(Guid userId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPublicProfileQuery(userId), ct));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { UserId = CurrentUserId }, ct));

    [HttpPost("photo")]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken ct) =>
        Ok(await Mediator.Send(new UploadProfilePhotoCommand(CurrentUserId, file.OpenReadStream(), file.FileName), ct));

    [HttpDelete("photo")]
    public async Task<IActionResult> RemovePhoto(CancellationToken ct) =>
        Ok(await Mediator.Send(new RemoveProfilePhotoCommand(CurrentUserId), ct));

    [HttpPost("cover")]
    public async Task<IActionResult> UploadCover(IFormFile file, CancellationToken ct) =>
        Ok(await Mediator.Send(new UploadCoverPhotoCommand(CurrentUserId, file.OpenReadStream(), file.FileName), ct));

    [HttpDelete("cover")]
    public async Task<IActionResult> RemoveCover(CancellationToken ct) =>
        Ok(await Mediator.Send(new RemoveCoverPhotoCommand(CurrentUserId), ct));

    [HttpPost("education")]
    public async Task<IActionResult> AddEducation([FromBody] AddEducationCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { UserId = CurrentUserId }, ct));

    [HttpPut("education/{id:guid}")]
    public async Task<IActionResult> UpdateEducation(Guid id, [FromBody] UpdateEducationCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { UserId = CurrentUserId, EducationId = id }, ct));

    [HttpDelete("education/{id:guid}")]
    public async Task<IActionResult> DeleteEducation(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteEducationCommand(CurrentUserId, id), ct));
}
