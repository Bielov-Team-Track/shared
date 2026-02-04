namespace Shared.Enums
{
    public enum ErrorCodeEnum
    {
        // General (0-99)
        ValidationError = 0,
        Unauthorized = 1,
        InternalError = 2,
        EntityNotFound = 3,
        Forbidden = 4,

        // Authentication (100-199)
        InvalidCredentials = 100,
        TokenExpired = 101,
        TokenInvalid = 102,
        EmailNotVerified = 103,
        AccountLocked = 104,

        // User/Profile (200-299)
        UserNotFound = 200,
        EmailAlreadyExists = 201,
        InvalidEmail = 202,
        WeakPassword = 203,

        // Events (300-399)
        EventNotFound = 300,
        EventFull = 301,
        EventCancelled = 302,
        AlreadyJoined = 303,
        NotParticipant = 304,
        RegistrationClosed = 305,
        PositionAlreadyClaimed = 306,

        // Clubs (400-499)
        ClubNotFound = 400,
        NotClubMember = 401,
        AlreadyClubMember = 402,
        InvitationNotFound = 403,
        InvitationExpired = 404,
        InsufficientPermissions = 405,

        // Payments (500-599)
        PaymentFailed = 500,
        PaymentRequired = 501,
        RefundFailed = 502,
    }
}
