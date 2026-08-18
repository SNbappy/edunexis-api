using EduNexis.Application.DTOs;
using EduNexis.Domain.Entities;

namespace EduNexis.Application.Extensions;

public static class CourseExtensions
{
    /// <summary>
    /// Maps a Course to a CourseDto with viewer-aware field stripping.
    /// </summary>
    /// <param name="viewerRole">"Owner", "Member", or "None"</param>
    public static CourseDto ToDto(
        this Course course,
        string teacherName = "",
        string? teacherProfilePhotoUrl = null,
        int memberCount = 0,
        string viewerRole = "None") =>
        new(
            course.Id,
            course.Title,
            course.CourseCode,
            course.CreditHours,
            course.Department,
            course.AcademicSession,
            course.Semester,
            course.Section,
            course.CourseType.ToString(),
            course.Description,
            course.CoverImageUrl,
            viewerRole == "Owner" ? course.JoiningCode : null,   // hide from non-owners
            course.TeacherId,
            teacherName,
            teacherProfilePhotoUrl,
            course.IsArchived,
            memberCount,
            course.CreatedAt,
            viewerRole
        );

    public static CourseSummaryDto ToSummaryDto(
        this Course course,
        string teacherName = "",
        string? teacherProfilePhotoUrl = null,
        int memberCount = 0) =>
        new(
            course.Id,
            course.Title,
            course.CourseCode,
            course.Department,
            course.AcademicSession,
            course.Semester,
            course.CourseType.ToString(),
            course.CoverImageUrl,
            course.TeacherId,
            teacherName,
            teacherProfilePhotoUrl,
            course.IsArchived,
            memberCount,
            course.CreatedAt
        );

    public static CourseMemberDto ToMemberDto(this CourseMember member) =>
        new(
            member.UserId,
            member.User.Profile?.FullName ?? "",
            member.User.Email,
            member.User.Profile?.StudentId,
            member.User.Profile?.ProfilePhotoUrl,
            member.IsCR,
            member.JoinedAt
        );
}
