namespace TdpTrans.DTOs
{
    public class SecuritySeedRequest
    {
        public int UserCount { get; set; } = 250;
        public int ClientCount { get; set; } = 250;
        public int TruckCount { get; set; } = 120;
        public int MissionCount { get; set; } = 2000;
        public int ActivityLogCount { get; set; } = 6000;
        public int SessionCount { get; set; } = 800;
    }

    public class SecuritySeedResponse
    {
        public int CreatedUsers { get; set; }
        public int CreatedClients { get; set; }
        public int CreatedTrucks { get; set; }
        public int CreatedMissions { get; set; }
        public int CreatedActivityLogs { get; set; }
        public int CreatedSessions { get; set; }
        public int SuspiciousProfilesSeeded { get; set; }
    }
}
