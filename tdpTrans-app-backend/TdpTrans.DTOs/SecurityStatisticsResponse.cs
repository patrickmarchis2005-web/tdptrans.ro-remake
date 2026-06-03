namespace TdpTrans.DTOs
{
    public class PermissionLoadStatisticResponse
    {
        public string PermissionName { get; set; } = string.Empty;
        public int RoleCount { get; set; }
        public int UserCount { get; set; }
        public int AssignmentEdges { get; set; }
        public int SuccessfulActionCount { get; set; }
        public int FailedActionCount { get; set; }
        public int ObservedRiskUsers { get; set; }
    }

    public class SecurityRiskUserResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public int FailedLogins { get; set; }
        public int PermissionDenials { get; set; }
        public int ChatMessagesLastTwoMinutes { get; set; }
        public int DistinctRecentIpCount { get; set; }
    }

    public class SecurityStatisticsResponse
    {
        public string Mode { get; set; } = string.Empty;
        public bool IsCached { get; set; }
        public int LookbackHours { get; set; }
        public long DurationMs { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public int TotalUsers { get; set; }
        public int TotalRoleAssignments { get; set; }
        public int TotalPermissionAssignments { get; set; }
        public int TotalActivityLogs { get; set; }
        public IReadOnlyList<PermissionLoadStatisticResponse> PermissionStatistics { get; set; } = Array.Empty<PermissionLoadStatisticResponse>();
        public IReadOnlyList<SecurityRiskUserResponse> TopRiskUsers { get; set; } = Array.Empty<SecurityRiskUserResponse>();
    }
}
