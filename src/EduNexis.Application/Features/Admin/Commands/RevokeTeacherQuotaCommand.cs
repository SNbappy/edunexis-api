using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Admin.Commands;

/// <summary>
/// Withdraws a grant's unspent allowance.
///
/// Soft by design: the row stays so the history of who granted what remains
/// readable, and — importantly — courses already created under the grant are
/// untouched. Revoking removes future headroom, never existing work.
/// </summary>
public record RevokeTeacherQuotaCommand(Guid GrantId) : ICommand<ApiResponse>;

public sealed class RevokeTeacherQuotaCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<RevokeTeacherQuotaCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        RevokeTeacherQuotaCommand cmd, CancellationToken ct)
    {
        var repo = uow.GetRepository<TeacherQuota>();

        var grant = await uow.TeacherQuotas.GetByIdAsync(cmd.GrantId, ct);
        if (grant is null)
            return ApiResponse.Fail("Grant not found.");

        if (grant.IsRevoked)
            return ApiResponse.Ok("That grant was already revoked.");

        var unspent = grant.RemainingQuota;
        grant.Revoke();
        repo.Update(grant);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok(
            unspent > 0
                ? $"Grant revoked. {unspent} unused course slot(s) withdrawn; existing courses are unaffected."
                : "Grant revoked. It was already fully used, so nothing changed.");
    }
}
