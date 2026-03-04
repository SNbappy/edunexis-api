using EduNexis.Application.Features.Marks.Commands;
using EduNexis.Application.Features.Marks.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNexis.API.Controllers;

[Authorize]
[Route("api/courses/{courseId:guid}")]
public class MarksController : BaseController
{
    [HttpPost("grading-formula")]
    public async Task<IActionResult> SaveFormula(
        Guid courseId, [FromBody] SaveGradingFormulaCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with
        {
            CourseId = courseId,
            TeacherId = CurrentUserId
        }, ct));

    [HttpPost("grading-formula/calculate")]
    public async Task<IActionResult> Calculate(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new CalculateFinalMarksCommand(courseId, CurrentUserId), ct));

    [HttpPost("grading-formula/publish")]
    public async Task<IActionResult> Publish(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new PublishFinalMarksCommand(courseId, CurrentUserId), ct));

    [HttpGet("grading-formula")]
    public async Task<IActionResult> GetFormula(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetGradingFormulaQuery(courseId, CurrentUserId), ct));

    [HttpGet("marks")]
    public async Task<IActionResult> GetMarks(Guid courseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCourseMarksQuery(courseId, CurrentUserId), ct));
}

