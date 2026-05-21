namespace TdpTrans.Models
{
    public static class ActivityActionNames
    {
        public const string LoginSucceeded = "auth.login.succeeded";
        public const string LoginFailed = "auth.login.failed";
        public const string SignupCreated = "auth.signup.created";
        public const string PermissionDenied = "security.permission.denied";
        public const string MissionsViewed = "missions.viewed";
        public const string MissionCreated = "missions.created";
        public const string MissionUpdated = "missions.updated";
        public const string MissionDeleted = "missions.deleted";
        public const string StatisticsViewed = "missions.statistics.viewed";
        public const string ChatConnected = "chat.connected";
        public const string ChatDisconnected = "chat.disconnected";
        public const string ChatHistoryViewed = "chat.history.viewed";
        public const string ChatMessageSent = "chat.message.sent";
        public const string ObservationsViewed = "admin.observations.viewed";
        public const string LogsViewed = "admin.logs.viewed";
    }
}
