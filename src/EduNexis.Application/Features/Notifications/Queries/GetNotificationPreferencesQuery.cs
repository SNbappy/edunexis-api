using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Domain.Interfaces.Services;
namespace EduNexis.Application.Features.Notifications.Queries;

/// <summary>One row per notification type, with the user's current choices.</summary>
public record NotificationPreferenceDto(
    string Type,
    string Label,
    string Description,
    bool InApp,
    bool Email,
    bool Sms,
    /// <summary>False for types that are never emailed, so the UI can grey the toggle.</summary>
    bool SupportsEmail,
    /// <summary>False for types that are never sent by SMS.</summary>
    bool SupportsSms
);

/// <summary>
/// What the platform can actually send, so the UI states facts rather than a
/// hard-coded guess that goes stale the moment a gateway is configured.
/// </summary>
public record NotificationChannelsDto(bool SmsConfigured);

public record NotificationPreferencesResponse(
    List<NotificationPreferenceDto> Preferences,
    NotificationChannelsDto Channels
);

public record GetNotificationPreferencesQuery(Guid UserId)
    : IQuery<ApiResponse<NotificationPreferencesResponse>>;

public sealed class GetNotificationPreferencesQueryHandler(
    IUnitOfWork uow,
    ISmsService smsService
) : IQueryHandler<GetNotificationPreferencesQuery, ApiResponse<NotificationPreferencesResponse>>
{
    /// <summary>
    /// Plain-language copy for each type.
    ///
    /// Lives here rather than in the client so the list cannot drift out of sync
    /// with the enum: adding a type without describing it fails to compile.
    /// </summary>
    public static readonly (NotificationType Type, string Label, string Description)[] Catalogue =
    [
        (NotificationType.NewAnnouncement,
            "Announcements",
            "When a teacher posts an announcement in one of your courses."),
        (NotificationType.NewAssignment,
            "New assignments",
            "When an assignment is posted."),
        (NotificationType.AssignmentDeadlineReminder,
            "Deadline reminders",
            "A reminder before an assignment is due."),
        (NotificationType.NewMaterial,
            "New materials",
            "When slides, notes or links are added to a course."),
        (NotificationType.MarksPublished,
            "Published marks",
            "When results are published for a test, assignment or the final total."),
        (NotificationType.JoinRequestReceived,
            "Join requests",
            "When a student asks to join a course you teach."),
        (NotificationType.CourseJoinApproved,
            "Join approved",
            "When your request to join a course is accepted."),
        (NotificationType.CourseJoinRejected,
            "Join declined",
            "When your request to join a course is turned down."),
        (NotificationType.GradeComplaint,
            "Grade queries",
            "When a student raises a question about a mark."),
        (NotificationType.AssignmentGraded,
            "Your work is marked",
            "When a teacher grades something you submitted."),
        (NotificationType.AssignmentUpdated,
            "Assignment changes",
            "When a deadline, mark total or the instructions change."),
        (NotificationType.AssignmentRemoved,
            "Assignment withdrawn",
            "When a teacher removes an assignment."),
        (NotificationType.SubmissionReceived,
            "Submissions",
            "When a student turns work in to a course you teach."),
        (NotificationType.NewComment,
            "Class comments",
            "When somebody replies under an announcement."),
        (NotificationType.AttendanceRecorded,
            "Attendance taken",
            "When the register is taken for one of your classes."),
        (NotificationType.MemberLeft,
            "Students leaving",
            "When a student leaves a course you teach."),
        (NotificationType.CourseArchived,
            "Course archived",
            "When a course is archived or restored."),
        (NotificationType.General,
            "Everything else",
            "Occasional notices that do not fit the categories above."),
    ];

    /* Channel eligibility comes from SendNotificationCommandHandler rather than
       being restated here. Two copies of the same list is how the settings page
       ends up offering a toggle for a channel that never fires. */
    internal static HashSet<NotificationType> EmailEligible
        => SendNotificationCommandHandler.EmailEligibleTypes;

    internal static HashSet<NotificationType> SmsEligible
        => SendNotificationCommandHandler.SmsEligibleTypes;

    public async ValueTask<ApiResponse<NotificationPreferencesResponse>> Handle(
        GetNotificationPreferencesQuery query, CancellationToken ct)
    {
        var saved = (await uow.GetRepository<NotificationPreference>()
                .FindAsync(p => p.UserId == query.UserId, ct))
            .ToDictionary(p => p.Type);

        // Anything the user has never touched is on. Absence means "default",
        // never "off".
        var dtos = Catalogue.Select(c =>
        {
            var supportsEmail = EmailEligible.Contains(c.Type);
            var supportsSms   = SmsEligible.Contains(c.Type);
            var has = saved.TryGetValue(c.Type, out var pref);

            // Absent row = defaults, and these must match SendNotificationCommand
            // exactly or the page shows a state the sender does not act on.
            // In-app and email on; SMS off, since it costs money and needs a
            // phone number the profile may not have.
            return new NotificationPreferenceDto(
                c.Type.ToString(),
                c.Label,
                c.Description,
                InApp: !has || pref!.InApp,
                Email: supportsEmail && (!has || pref!.Email),
                Sms:   supportsSms   && has && pref!.Sms,
                SupportsEmail: supportsEmail,
                SupportsSms:   supportsSms);
        }).ToList();

        return ApiResponse<NotificationPreferencesResponse>.Ok(
            new NotificationPreferencesResponse(
                dtos,
                new NotificationChannelsDto(smsService.IsConfigured)));
    }
}
