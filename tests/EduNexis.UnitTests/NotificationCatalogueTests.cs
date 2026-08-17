using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Application.Features.Notifications.Queries;
using EduNexis.Domain.Enums;

namespace EduNexis.UnitTests;

/// <summary>
/// The Settings page is built from a hand-written catalogue. A notification type
/// missing from it is invisible there — it keeps firing and the user has no way
/// to turn it off, which is the exact complaint notification settings exist to
/// answer. These tests make that a build failure rather than a support ticket.
/// </summary>
public class NotificationCatalogueTests
{
    [Fact]
    public void Every_notification_type_is_manageable_from_settings()
    {
        var described = GetNotificationPreferencesQueryHandler.Catalogue
            .Select(c => c.Type)
            .ToHashSet();

        var missing = Enum.GetValues<NotificationType>()
            .Where(t => !described.Contains(t))
            .Select(t => t.ToString())
            .OrderBy(n => n)
            .ToList();

        Assert.True(missing.Count == 0,
            "These notification types have no entry in the Settings catalogue, so a "
            + "user cannot turn them off:\n  - " + string.Join("\n  - ", missing));
    }

    [Fact]
    public void Catalogue_has_no_entry_for_a_type_that_no_longer_exists()
    {
        var real = Enum.GetValues<NotificationType>().ToHashSet();

        var stale = GetNotificationPreferencesQueryHandler.Catalogue
            .Select(c => c.Type)
            .Where(t => !real.Contains(t))
            .Select(t => t.ToString())
            .ToList();

        Assert.True(stale.Count == 0,
            "Catalogue describes types that are not in the enum: " + string.Join(", ", stale));
    }

    [Fact]
    public void Catalogue_has_no_duplicates()
    {
        var duplicates = GetNotificationPreferencesQueryHandler.Catalogue
            .GroupBy(c => c.Type)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.ToString())
            .ToList();

        Assert.True(duplicates.Count == 0,
            "Duplicated catalogue entries render twice in Settings: "
            + string.Join(", ", duplicates));
    }

    [Fact]
    public void Sms_is_a_strict_subset_of_email_eligibility()
    {
        // SMS costs money per message, so it must never reach a type that is not
        // already considered important enough to email about.
        var smsOnly = SendNotificationCommandHandler.SmsEligibleTypes
            .Where(t => !SendNotificationCommandHandler.EmailEligibleTypes.Contains(t))
            .Select(t => t.ToString())
            .ToList();

        Assert.True(smsOnly.Count == 0,
            "These types would text a user without being email-worthy: "
            + string.Join(", ", smsOnly));
    }

    [Fact]
    public void Channel_eligible_types_all_appear_in_the_catalogue()
    {
        var described = GetNotificationPreferencesQueryHandler.Catalogue
            .Select(c => c.Type).ToHashSet();

        var orphans = SendNotificationCommandHandler.EmailEligibleTypes
            .Concat(SendNotificationCommandHandler.SmsEligibleTypes)
            .Distinct()
            .Where(t => !described.Contains(t))
            .Select(t => t.ToString())
            .ToList();

        Assert.True(orphans.Count == 0,
            "These types can send email or SMS but cannot be switched off: "
            + string.Join(", ", orphans));
    }
}
