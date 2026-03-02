namespace EduNexis.Application.Features.Materials.Commands;

public record DeleteMaterialCommand(
    Guid CourseId,
    Guid MaterialId,
    Guid RequestedById
) : ICommand<ApiResponse>;

public sealed class DeleteMaterialCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<DeleteMaterialCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(DeleteMaterialCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        var repo = uow.GetRepository<Material>();
        var material = await repo.GetByIdAsync(cmd.MaterialId, ct);

        if (material is null || material.IsDeleted || material.CourseId != cmd.CourseId)
            return ApiResponse.Fail("Material not found.");

        bool isTeacher = course.TeacherId == cmd.RequestedById;
        var member = await uow.CourseMembers.GetMemberAsync(course.Id, cmd.RequestedById, ct);
        bool isCR = member?.IsCR ?? false;

        if (!isTeacher && !isCR)
            return ApiResponse.Fail("You are not authorized to delete this material.");

        material.Delete();
        repo.Update(material);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Material deleted.");
    }
}
