namespace EduNexis.Domain.Exceptions;

public class DomainException(string message) : Exception(message);

public class NotFoundException(string entity, object key)
    : Exception($"{entity} with key '{key}' was not found.");

public class UnauthorizedException(string message = "Unauthorized access.")
    : Exception(message);

public class ProfileIncompleteException()
    : Exception("Please complete your profile before proceeding.");

public class QuotaExceededException()
    : Exception("Class creation quota has been exhausted or access period has expired.");

public class AccessExpiredException()
    : Exception("Your classroom access period has expired. Contact admin to extend.");

public class DuplicateJoinRequestException()
    : Exception("A pending join request already exists for this course.");

public class AlreadyMemberException()
    : Exception("This student is already a member of the course.");
