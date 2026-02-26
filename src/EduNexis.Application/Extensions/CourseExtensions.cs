using EduNexis.Application.DTOs;
using EduNexis.Domain.Entities;

namespace EduNexis.Application.Extensions;

public static class CourseExtensions
{
    public static CourseDto ToDto(this Course course, string teacherName = "", int memberCount = 0) =>
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
            course.JoiningCode,
            course.TeacherId,
            teacherName,
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
