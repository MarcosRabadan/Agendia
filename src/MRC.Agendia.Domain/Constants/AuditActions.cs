namespace MRC.Agendia.Domain.Constants
{
    /// <summary>Stable action codes for <see cref="Entities.AuditLog"/>.</summary>
    public static class AuditActions
    {
        public const string LoginSuccess = "LOGIN_SUCCESS";
        public const string LoginFailed = "LOGIN_FAILED";
        public const string PasswordChanged = "PASSWORD_CHANGED";
        public const string PasswordReset = "PASSWORD_RESET";
        public const string UserCreated = "USER_CREATED";
        public const string ScheduleTemplateCreated = "SCHEDULE_TEMPLATE_CREATED";
        public const string ScheduleTemplateUpdated = "SCHEDULE_TEMPLATE_UPDATED";
        public const string ScheduleTemplateDeleted = "SCHEDULE_TEMPLATE_DELETED";
        public const string ScheduleOverrideCreated = "SCHEDULE_OVERRIDE_CREATED";
        public const string ScheduleOverrideUpdated = "SCHEDULE_OVERRIDE_UPDATED";
        public const string ScheduleOverrideDeleted = "SCHEDULE_OVERRIDE_DELETED";
        public const string AppointmentStatusChanged = "APPOINTMENT_STATUS_CHANGED";
    }
}
