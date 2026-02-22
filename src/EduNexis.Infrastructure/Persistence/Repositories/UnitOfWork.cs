using Microsoft.EntityFrameworkCore.Storage;

namespace EduNexis.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public IUserRepository Users { get; }
    public IUserProfileRepository UserProfiles { get; }
    public ICourseRepository Courses { get; }
    public ICourseMemberRepository CourseMembers { get; }
    public IJoinRequestRepository JoinRequests { get; }
    public ITeacherQuotaRepository TeacherQuotas { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new UserRepository(context);
        UserProfiles = new UserProfileRepository(context);
        Courses = new CourseRepository(context);
        CourseMembers = new CourseMemberRepository(context);
        JoinRequests = new JoinRequestRepository(context);
        TeacherQuotas = new TeacherQuotaRepository(context);
    }

    public IBaseRepository<T> GetRepository<T>() where T : BaseEntity =>
        new BaseRepository<T>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default) =>
        _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            await _transaction.CommitAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            await _transaction.RollbackAsync(ct);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
