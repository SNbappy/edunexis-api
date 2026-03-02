using System.Text.Json.Serialization;

namespace EduNexis.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttendanceStatus
{
    Present,
    Absent,
    Unmarked
}
