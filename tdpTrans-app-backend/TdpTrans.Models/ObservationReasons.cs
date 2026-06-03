namespace TdpTrans.Models
{
    public static class ObservationReasons
    {
        public const string FailedLogins = "Multiple failed logins";
        public const string PermissionProbe = "Repeated permission denials";
        public const string ChatBurst = "Chat burst pattern";
        public const string MultiSessionIpDrift = "Multiple active sessions from distinct IPs";
        public const string AiAnomaly = "AI anomaly detector";
    }
}
