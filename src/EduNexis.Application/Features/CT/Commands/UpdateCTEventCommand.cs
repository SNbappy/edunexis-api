using EduNexis.Application.Abstractions;
namespace EduNexis.Application.Features.CT.Commands;

public record UpdateCTEventCommand(
    Guid CTEventId,
    Guid TeacherId,
    string Title,
    decimal MaxMarks,
    DateTime? HeldOn
) : ICommand<ApiResponse<CTEventDto>>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
    {
        var e = await uow.GetRepository<CTEvent>().GetByIdAsync(CTEventId, ct);
        return e?.CourseId;
    }
}

public sealed class UpdateCTEventCommandValidator : AbstractValidator<UpdateCTEventCommand>
{
    public UpdateCTEventCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxMarks).GreaterThan(0);
    }
}

public sealed class UpdateCTEventCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UpdateCTEventCommand, ApiResponse<CTEventDto>>
{
    public async ValueTask<ApiResponse<CTEventDto>> Handle(
        UpdateCTEventCommand command, CancellationToken ct)
    {
        var ctEvent = await uow.GetRepository<CTEvent>().GetByIdAsync(command.CTEventId, ct)
            ?? throw new NotFoundException("CTEvent", command.CTEventId);

        var course = await uow.Courses.GetByIdAsync(ctEvent.CourseId, ct)
            ?? throw new NotFoundException("Course", ctEvent.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, command.TeacherId, ct))
            return ApiResponse<CTEventDto>.Fail("Only the teacher can update CT events.");

        if (ctEvent.Status == CTStatus.Published)
            return ApiResponse<CTEventDto>.Fail("Cannot edit a published CT. Unpublish it first.");

        ctEvent.Update(command.Title, command.MaxMarks, command.HeldOn);
        uow.GetRepository<CTEvent>().Update(ctEvent);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<CTEventDto>.Ok(new CTEventDto(
            ctEvent.Id, ctEvent.CourseId, ctEvent.CTNumber,
            ctEvent.Title, ctEvent.MaxMarks, ctEvent.HeldOn,
            ctEvent.Status.ToString(), ctEvent.KhataUploaded, ctEvent.CreatedAt,
            ctEvent.BestScriptUrl, ctEvent.BestStudentId,
            ctEvent.WorstScriptUrl, ctEvent.WorstStudentId,
            ctEvent.AverageScriptUrl, ctEvent.AverageStudentId));
    }
}

