using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Public.Queries;

public record GetPublicDepartmentsQuery() : IQuery<ApiResponse<List<string>>>;

public sealed class GetPublicDepartmentsQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetPublicDepartmentsQuery, ApiResponse<List<string>>>
{
    public async ValueTask<ApiResponse<List<string>>> Handle(
        GetPublicDepartmentsQuery query, CancellationToken ct)
    {
        var departments = await uow.UserProfiles.ListPublicDepartmentsAsync(ct);
        return ApiResponse<List<string>>.Ok(departments);
    }
}