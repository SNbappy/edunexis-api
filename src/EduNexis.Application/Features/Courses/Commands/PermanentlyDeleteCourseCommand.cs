using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Courses.Commands;

/// <summary>
/// Hard-deletes a course that is already in the teacher's Recently Deleted
/// list. Skips password/code re-confirmation since the course already went
/// through that gate on soft-delete; the UI should still confirm intent.
///
/// EF Core defaults required foreign keys to ON DELETE RESTRICT unless a
/// cascade is explicitly configured, so removing the course row directly
/// fails with an FK violation the moment any dependent row exists (this
/// was discovered in production via a FinalMarks constraint failure).
/// Every dependent table is cleared explicitly, bottom-up, before the
/// course row itself is removed.
/// </summary>
public record PermanentlyDeleteCourseCommand(Guid Id) : ICommand<ApiResponse>;

public sealed class PermanentlyDeleteCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<PermanentlyDeleteCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        PermanentlyDeleteCourseCommand cmd, CancellationToken ct)
    {
        var viewerId = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByIdAsync(cmd.Id, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        if (course.TeacherId != viewerId)
            return ApiResponse.Fail("You don't have permission to delete this course.");

        if (!course.IsDeletedByOwner)
            return ApiResponse.Fail("Only courses in Recently Deleted can be permanently removed.");

        var courseId = course.Id;

        // Assignment -> AssignmentSubmission -> GradeComplaint -> GradeComplaintMessage / PlagiarismReport
        var assignments = await uow.GetRepository<Assignment>().FindAsync(a => a.CourseId == courseId, ct);
        var assignmentIds = assignments.Select(a => a.Id).ToList();

        var submissions = await uow.GetRepository<AssignmentSubmission>().FindAsync(s => assignmentIds.Contains(s.AssignmentId), ct);
        var submissionIds = submissions.Select(s => s.Id).ToList();

        var complaints = await uow.GetRepository<GradeComplaint>().FindAsync(gc => submissionIds.Contains(gc.SubmissionId), ct);
        var complaintIds = complaints.Select(c => c.Id).ToList();

        var complaintMessages = await uow.GetRepository<GradeComplaintMessage>().FindAsync(m => complaintIds.Contains(m.ComplaintId), ct);
        foreach (var m in complaintMessages) uow.GetRepository<GradeComplaintMessage>().Delete(m);

        foreach (var gc in complaints) uow.GetRepository<GradeComplaint>().Delete(gc);

        var plagiarismReports = await uow.GetRepository<PlagiarismReport>().FindAsync(p => submissionIds.Contains(p.SubmissionId), ct);
        foreach (var p in plagiarismReports) uow.GetRepository<PlagiarismReport>().Delete(p);

        foreach (var s in submissions) uow.GetRepository<AssignmentSubmission>().Delete(s);
        foreach (var a in assignments) uow.GetRepository<Assignment>().Delete(a);

        // AttendanceSession -> AttendanceRecord
        var sessions = await uow.GetRepository<AttendanceSession>().FindAsync(s => s.CourseId == courseId, ct);
        var sessionIds = sessions.Select(s => s.Id).ToList();
        var records = await uow.GetRepository<AttendanceRecord>().FindAsync(r => sessionIds.Contains(r.SessionId), ct);
        foreach (var r in records) uow.GetRepository<AttendanceRecord>().Delete(r);
        foreach (var s in sessions) uow.GetRepository<AttendanceSession>().Delete(s);

        // CTEvent -> CTSubmission
        var ctEvents = await uow.GetRepository<CTEvent>().FindAsync(e => e.CourseId == courseId, ct);
        var ctEventIds = ctEvents.Select(e => e.Id).ToList();
        var ctSubmissions = await uow.GetRepository<CTSubmission>().FindAsync(s => ctEventIds.Contains(s.CTEventId), ct);
        foreach (var s in ctSubmissions) uow.GetRepository<CTSubmission>().Delete(s);
        foreach (var e in ctEvents) uow.GetRepository<CTEvent>().Delete(e);

        // PresentationEvent -> PresentationMark
        var presentations = await uow.GetRepository<PresentationEvent>().FindAsync(p => p.CourseId == courseId, ct);
        var presentationIds = presentations.Select(p => p.Id).ToList();
        var presentationMarks = await uow.GetRepository<PresentationMark>().FindAsync(m => presentationIds.Contains(m.PresentationEventId), ct);
        foreach (var m in presentationMarks) uow.GetRepository<PresentationMark>().Delete(m);
        foreach (var p in presentations) uow.GetRepository<PresentationEvent>().Delete(p);

        // Announcement -> AnnouncementComment
        var announcements = await uow.GetRepository<Announcement>().FindAsync(a => a.CourseId == courseId, ct);
        var announcementIds = announcements.Select(a => a.Id).ToList();
        var comments = await uow.GetRepository<AnnouncementComment>().FindAsync(c => announcementIds.Contains(c.AnnouncementId), ct);
        foreach (var c in comments) uow.GetRepository<AnnouncementComment>().Delete(c);
        foreach (var a in announcements) uow.GetRepository<Announcement>().Delete(a);

        // GradingFormula -> FormulaComponent
        var formulas = await uow.GetRepository<GradingFormula>().FindAsync(f => f.CourseId == courseId, ct);
        var formulaIds = formulas.Select(f => f.Id).ToList();
        var formulaComponents = await uow.GetRepository<FormulaComponent>().FindAsync(c => formulaIds.Contains(c.FormulaId), ct);
        foreach (var c in formulaComponents) uow.GetRepository<FormulaComponent>().Delete(c);
        foreach (var f in formulas) uow.GetRepository<GradingFormula>().Delete(f);

        // Flat course-level tables (no further children)
        var finalMarks = await uow.GetRepository<FinalMark>().FindAsync(f => f.CourseId == courseId, ct);
        foreach (var f in finalMarks) uow.GetRepository<FinalMark>().Delete(f);

        var members = await uow.GetRepository<CourseMember>().FindAsync(m => m.CourseId == courseId, ct);
        foreach (var m in members) uow.GetRepository<CourseMember>().Delete(m);

        var joinRequests = await uow.GetRepository<JoinRequest>().FindAsync(j => j.CourseId == courseId, ct);
        foreach (var j in joinRequests) uow.GetRepository<JoinRequest>().Delete(j);

        var materials = await uow.GetRepository<Material>().FindAsync(m => m.CourseId == courseId, ct);
        foreach (var m in materials) uow.GetRepository<Material>().Delete(m);

        // Persist all the child deletions first, then remove the course itself.
        await uow.SaveChangesAsync(ct);

        uow.Courses.Delete(course);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Course permanently deleted.");
    }
}