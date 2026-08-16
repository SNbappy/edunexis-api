using EduNexis.Application.Abstractions;
using EduNexis.Domain.Exceptions;

namespace EduNexis.Application.Behaviors;

/// <summary>
/// Refuses any write to an archived course.
///
/// Archiving used to be cosmetic: the course dropped off the active list and
/// the card went grey, but every endpoint still accepted writes, so a teacher
/// or student could keep posting, submitting and grading inside a course that
/// was supposedly closed. An archived semester has to be a fixed record.
///
/// Reading is untouched. Teachers and students can open an archived course and
/// see everything it ever held — materials, marks, submissions, the register.
/// Only changes are blocked, and unarchiving lifts the block.
/// </summary>
public sealed class ArchivedCourseGuardBehavior<TMessage, TResponse>(
    IUnitOfWork uow)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken ct)
    {
        if (message is ICourseScopedWrite scoped and not IArchiveExempt)
        {
            var courseId = await scoped.ResolveCourseIdAsync(uow, ct);

            if (courseId is not null)
            {
                var course = await uow.GetRepository<Course>()
                    .GetByIdAsync(courseId.Value, ct);

                if (course is { IsArchived: true })
                    throw new CourseArchivedException();
            }
        }

        return await next(message, ct);
    }
}
