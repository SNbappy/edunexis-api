using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Assignments.Queries;

public record GetAssignmentsQuery(
    Guid CourseId,
    Guid RequestedByUserId,
    bool IsTeacher
) : IQuery<ApiResponse<List<AssignmentDto>>>;

public sealed class GetAssignmentsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetAssignmentsQuery, ApiResponse<List<AssignmentDto>>>
{
    public async ValueTask<ApiResponse<List<AssignmentDto>>> Handle(
        GetAssignmentsQuery query, CancellationToken ct)
    {
        var assignments = (await uow.GetRepository<Assignment>()
            .FindAsync(a => a.CourseId == query.CourseId && !a.IsDeleted, ct))
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        if (assignments.Count == 0)
            return ApiResponse<List<AssignmentDto>>.Ok(new List<AssignmentDto>());

        var assignmentIds = assignments.Select(a => a.Id).ToHashSet();

        // Single batch fetch — kills N+1.
        var allSubmissions = (await uow.GetRepository<AssignmentSubmission>()
            .FindAsync(s => assignmentIds.Contains(s.AssignmentId), ct))
            .ToList();

        // Group submissions by assignment for fast lookup.
        var submissionsByAssignment = allSubmissions
            .GroupBy(s => s.AssignmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Caller's own submissions (only meaningful for students).
        var myByAssignment = query.IsTeacher
            ? new Dictionary<Guid, AssignmentSubmission>()
            : allSubmissions
                .Where(s => s.StudentId == query.RequestedByUserId)
                .GroupBy(s => s.AssignmentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.SubmittedAt).First());

        int totalActiveStudents = 0;
        if (query.IsTeacher)
        {
            var course = await uow.Courses.GetByIdAsync(query.CourseId, ct);
            if (course is not null)
            {
                var teacherIds = await CourseAccess.TeacherIdsAsync(uow, course, ct);
                var members = await uow.CourseMembers
                    .FindAsync(m => m.CourseId == query.CourseId && m.IsActive, ct);
                totalActiveStudents = members.Count(m => !teacherIds.Contains(m.UserId));
            }
        }

        var dtos = new List<AssignmentDto>(assignments.Count);
        foreach (var a in assignments)
        {
            var subs = submissionsByAssignment.TryGetValue(a.Id, out var list)
                ? list
                : new List<AssignmentSubmission>();

            var submissionCount = subs.Count;
            var gradedCount = subs.Count(s => s.IsGraded);
            var isMarksComplete = query.IsTeacher && totalActiveStudents > 0 && gradedCount >= totalActiveStudents;

            AssignmentMyStatus? myStatus = null;
            decimal? myMarks = null;
            DateTime? mySubmittedAt = null;
            bool? myIsLate = null;

            if (!query.IsTeacher)
            {
                if (myByAssignment.TryGetValue(a.Id, out var mine))
                {
                    if (a.IsPublished && mine.IsGraded)
                    {
                        myStatus = AssignmentMyStatus.Graded;
                        myMarks = mine.Marks;
                    }
                    else
                    {
                        myStatus = AssignmentMyStatus.Submitted;
                        myMarks = null;
                    }
                    mySubmittedAt = mine.SubmittedAt;
                    myIsLate = mine.IsLate;
                }
                else
                {
                    myStatus = AssignmentMyStatus.NotSubmitted;
                }
            }

            dtos.Add(new AssignmentDto(
                Id: a.Id,
                CourseId: a.CourseId,
                Title: a.Title,
                Instructions: a.Instructions,
                Deadline: a.Deadline,
                AllowLateSubmission: a.AllowLateSubmission,
                MaxMarks: a.MaxMarks,
                RubricNotes: a.RubricNotes,
                ReferenceFileUrl: a.ReferenceFileUrl,
                IsOpen: a.IsOpen(),
                SubmissionCount: submissionCount,
                GradedCount: gradedCount,
                MyStatus: myStatus,
                MyMarks: myMarks,
                MySubmittedAt: mySubmittedAt,
                MyIsLate: myIsLate,
                CreatedAt: a.CreatedAt,
                IsPublished: a.IsPublished,
                PublishedAt: a.PublishedAt,
                TotalStudentsCount: totalActiveStudents,
                IsMarksComplete: isMarksComplete
            ));
        }

        return ApiResponse<List<AssignmentDto>>.Ok(dtos);
    }
}
