namespace EduNexis.Application.Features.Notifications.Queries;

/// <summary>One row per notification type, with the user's current choices.</summary>
public record NotificationPreferenceDto(
    string Type,
    string Label,
    string Description,
    bool InApp,
    bool Email,
    /// <summary>False for types that are never emailed, so the UI can grey the toggle.</summary>
    bool SupportsEmail
);

public record GetNotificationPreferencesQuery(Guid UserId)
    : IQuery<ApiResponse<List<NotificationPreferenceDto>>>;

public sealed class GetNotificationPreferencesQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetNotificationPreferencesQuery, ApiResponse<List<NotificationPreferenceDto>>>
{
    /// <summary>
    /// Plain-language copy for each type.
    ///
    /// Lives here rather than in the client so the list cannot drift out of sync
    /// with the enum: adding a type without describing it fails to compile.
    /// </summary>
    internal static readonly (NotificationType Type, string Label, string Description)[] Catalogue =
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
        (NotificationType.General,
            "Everything else",
            "Occasional notices that do not fit the categories above."),
    ];

    /// <summary>Types that can also arrive by email — must match SendNotificationCommand.</summary>
    internal static readonly HashSet<NotificationType> EmailEligible =
    [
        NotificationType.NewAssignment,
        NotificationType.MarksPublished,
        NotificationType.AssignmentDeadlineReminder,
        NotificationType.JoinRequestReceived,
        NotificationType.CourseJoinApproved,
        NotificationType.CourseJoinRejected,
        NotificationType.GradeComplaint,
    ];

    public async ValueTask<ApiResponse<List<NotificationPreferenceDto>>> Handle(
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
            var has = saved.TryGetValue(c.Type, out var pref);
            return new NotificationPreferenceDto(
                c.Type.ToString(),
                c.Label,
                c.Description,
                InApp: !has || pref!.InApp,
                Email: supportsEmail && (!has || pref!.Email),
                SupportsEmail: supportsEmail);
        }).ToList();

        return ApiResponse<List<NotificationPreferenceDto>>.Ok(dtos);
    }
}
