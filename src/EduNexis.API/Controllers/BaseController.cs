using EduNexis.API.Extensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    private IMediator? _mediator;

    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected Guid CurrentUserId => User.GetUserId();
    protected string CurrentUserEmail => User.GetEmail();
    protected string CurrentFirebaseUid => User.GetFirebaseUid();
}
