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
    [HttpPost("publications")]
    public async Task<IActionResult> AddPublication([FromBody] AddPublicationCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { UserId = CurrentUserId }, ct));

    [HttpPut("publications/{id:guid}")]
    public async Task<IActionResult> UpdatePublication(Guid id, [FromBody] UpdatePublicationCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { UserId = CurrentUserId, Id = id }, ct));

    [HttpDelete("publications/{id:guid}")]
    public async Task<IActionResult> DeletePublication(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new DeletePublicationCommand(id, CurrentUserId), ct));

    [HttpPost("publications/{id:guid}/pdf")]
    public async Task<IActionResult> UploadPublicationPdf(Guid id, IFormFile file, CancellationToken ct) =>
        Ok(await Mediator.Send(new UploadPublicationPdfCommand(
            id, CurrentUserId, file.OpenReadStream(), file.FileName, file.Length), ct));

    [HttpDelete("publications/{id:guid}/pdf")]
    public async Task<IActionResult> RemovePublicationPdf(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new RemovePublicationPdfCommand(id, CurrentUserId), ct));

    [HttpPatch("publications/{id:guid}/pdf-visibility")]
    public async Task<IActionResult> UpdatePublicationPdfVisibility(
        Guid id, [FromBody] UpdatePublicationPdfVisibilityRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new UpdatePublicationPdfVisibilityCommand(
            id, CurrentUserId, request.IsPublic), ct));

    public record UpdatePublicationPdfVisibilityRequest(bool IsPublic);

    [HttpPatch("visibility")]
    public async Task<IActionResult> UpdateVisibility(
        [FromBody] UpdateVisibilityRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new UpdateProfileVisibilityCommand(
            CurrentUserId, request.IsPublic, request.Slug), ct));

    public record UpdateVisibilityRequest(bool IsPublic, string? Slug);

    [HttpGet("{userId:guid}/courses")]
    public async Task<IActionResult> GetUserCourses(Guid userId, [FromQuery] string? status, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetUserCoursesQuery(userId, status), ct));
}
