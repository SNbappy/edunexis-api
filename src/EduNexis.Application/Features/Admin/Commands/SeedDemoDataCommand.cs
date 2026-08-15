using EduNexis.Domain.Entities;
using EduNexis.Domain.Enums;
using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Application.Features.Admin.Commands;

/// <summary>
/// Seeds realistic demo students + attendance into an existing course so the
/// marketing site can be screenshotted against a populated UI. Every seeded
/// account uses the demo. prefix on its email so SeedDemoDataCleanupCommand
/// can remove them cleanly afterwards. Admin-only, and intended for one-off
/// manual use - not part of any normal application flow.
/// </summary>
public record SeedDemoDataCommand(Guid CourseId, int StudentCount = 15) : ICommand<ApiResponse>;

public sealed class SeedDemoDataCommandHandler(
    IUnitOfWork uow,
    IPasswordHasher passwordHasher
) : ICommandHandler<SeedDemoDataCommand, ApiResponse>
{
    private const string DemoPrefix = "demo.";

    private static readonly string[] Names =
    [
        "Tanvir Ahmed", "Nusrat Jahan", "Rakibul Hasan", "Sadia Islam",
        "Mahmudul Karim", "Farzana Akter", "Imran Hossain", "Tasnim Rahman",
        "Shakib Al Amin", "Mim Chowdhury", "Rafiqul Islam", "Sumaiya Binte Noor",
        "Arif Mahmud", "Jarin Tasnim", "Nayeem Uddin", "Lamia Sultana",
        "Fahim Shahriar", "Ishrat Jahan", "Mizanur Rahman", "Sanjida Haque",
    ];

    public async ValueTask<ApiResponse> Handle(
        SeedDemoDataCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        var count = Math.Clamp(cmd.StudentCount, 1, Names.Length);
        var hash = passwordHasher.Hash("DemoPass!2026");
        var rng = new Random(42); // fixed seed = reproducible screenshots

        var createdStudentIds = new List<Guid>();

        for (var i = 0; i < count; i++)
        {
            var roll = $"2001{(i + 10):D2}";
            var email = $"{DemoPrefix}{roll}@student.just.edu.bd";

            var exists = await uow.Users.GetByEmailAsync(email, ct);
            if (exists is not null) { createdStudentIds.Add(exists.Id); continue; }

            var user = User.Create(email, hash, UserRole.Student);
            user.MarkEmailVerified();
            user.MarkProfileComplete();
            await uow.Users.AddAsync(user, ct);

            var profile = UserProfile.Create(user.Id, Names[i]);
            profile.Update(
                fullName: Names[i],
                department: course.Department,
                designation: null,
                studentId: roll,
                bio: null, headline: null, phoneNumber: null,
                officeLocation: null, officeHours: null,
                researchInterestsCsv: null, fieldsOfWorkCsv: null,
                linkedInUrl: null, facebookUrl: null,
                twitterUrl: null, gitHubUrl: null, websiteUrl: null);
            await uow.UserProfiles.AddAsync(profile, ct);

            var member = CourseMember.Create(course.Id, user.Id);
            await uow.CourseMembers.AddAsync(member, ct);

            createdStudentIds.Add(user.Id);
        }

        await uow.SaveChangesAsync(ct);

        // Attendance: 8 past sessions, ~85% present so the sheet looks realistic
        var sessionRepo = uow.GetRepository<AttendanceSession>();
        var recordRepo = uow.GetRepository<AttendanceRecord>();

        var topics = new[]
        {
            "Arrays and complexity", "Linked lists", "Stacks and queues",
            "Recursion basics", "Sorting algorithms", "Binary search trees",
            "Hashing", "Graph traversal",
        };

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-21));

        for (var s = 0; s < topics.Length; s++)
        {
            var date = startDate.AddDays(s * 2);
            var session = AttendanceSession.Create(course.Id, date, topics[s], course.TeacherId);
            await sessionRepo.AddAsync(session, ct);

            foreach (var studentId in createdStudentIds)
            {
                var status = rng.NextDouble() < 0.85
                    ? AttendanceStatus.Present
                    : AttendanceStatus.Absent;
                await recordRepo.AddAsync(
                    AttendanceRecord.Create(session.Id, studentId, status), ct);
            }
        }

        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok(
            $"Seeded {createdStudentIds.Count} demo students and {topics.Length} attendance sessions into '{course.Title}'.");
    }
}