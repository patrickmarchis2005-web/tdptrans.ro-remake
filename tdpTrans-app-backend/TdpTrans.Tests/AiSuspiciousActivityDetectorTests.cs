using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TdpTrans.Models;
using TdpTrans.Repositories;
using TdpTrans.Services;

namespace TdpTrans.Tests
{
    public class AiSuspiciousActivityDetectorTests
    {
        [Fact]
        public async Task Assess_WithSuspiciousFeaturePattern_FlagsUser()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

            await using var dbContext = new ApplicationDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            var now = DateTime.UtcNow;
            dbContext.Users.AddRange(
                BuildUser(101, "rule-positive@tdptrans.ro"),
                BuildUser(102, "mostly-normal@tdptrans.ro"),
                BuildUser(103, "target@tdptrans.ro"));

            dbContext.UserObservations.Add(new UserObservation
            {
                UserId = 101,
                Reason = ObservationReasons.FailedLogins,
                Details = "Rule detector training label.",
                RiskScore = 88,
                IsActive = true,
                FirstDetectedAtUtc = now.AddMinutes(-5),
                LastDetectedAtUtc = now.AddMinutes(-2)
            });

            dbContext.ActivityLogs.AddRange(
                BuildLog(101, ActivityActionNames.LoginFailed, false, now.AddMinutes(-12)),
                BuildLog(101, ActivityActionNames.LoginFailed, false, now.AddMinutes(-10)),
                BuildLog(101, ActivityActionNames.LoginFailed, false, now.AddMinutes(-9)),
                BuildLog(101, ActivityActionNames.PermissionDenied, false, now.AddMinutes(-7)),
                BuildLog(101, ActivityActionNames.PermissionDenied, false, now.AddMinutes(-6)),
                BuildLog(101, ActivityActionNames.PermissionDenied, false, now.AddMinutes(-5)),
                BuildLog(102, ActivityActionNames.MissionsViewed, true, now.AddHours(-2)),
                BuildLog(102, ActivityActionNames.ChatMessageSent, true, now.AddMinutes(-1)),
                BuildLog(102, ActivityActionNames.MissionUpdated, true, now.AddMinutes(-30)),
                BuildLog(103, ActivityActionNames.LoginFailed, false, now.AddMinutes(-8)),
                BuildLog(103, ActivityActionNames.LoginFailed, false, now.AddMinutes(-7)),
                BuildLog(103, ActivityActionNames.PermissionDenied, false, now.AddMinutes(-6)),
                BuildLog(103, ActivityActionNames.PermissionDenied, false, now.AddMinutes(-5)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-110)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-105)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-100)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-95)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-90)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-85)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-80)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-75)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-70)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-65)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-60)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-55)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-50)),
                BuildLog(103, ActivityActionNames.ChatMessageSent, true, now.AddSeconds(-45)));

            dbContext.AuthSessions.AddRange(
                BuildSession(101, "10.0.0.1", now.AddMinutes(-15)),
                BuildSession(101, "10.0.0.2", now.AddMinutes(-10)),
                BuildSession(101, "10.0.0.3", now.AddMinutes(-5)),
                BuildSession(102, "192.168.0.10", now.AddMinutes(-20)),
                BuildSession(103, "172.16.0.10", now.AddMinutes(-15)),
                BuildSession(103, "172.16.0.11", now.AddMinutes(-10)),
                BuildSession(103, "172.16.0.12", now.AddMinutes(-5)));

            await dbContext.SaveChangesAsync();

            var detector = new AiSuspiciousActivityDetector(
                dbContext,
                new MemoryCache(new MemoryCacheOptions()));

            var result = await detector.Assess(103);

            Assert.NotNull(result);
            Assert.True(result!.ShouldFlag);
            Assert.True(result.Probability >= 0.78d);
            Assert.Contains("Scor AI", result.Details);
        }

        private static AppUser BuildUser(int id, string email)
        {
            return new AppUser
            {
                Id = id,
                FullName = $"User {id}",
                Email = email,
                PasswordHash = CredentialHasher.HashPassword("12345678"),
                SecurityCodeHash = CredentialHasher.HashSecurityCode("123456"),
                AuthenticationPhraseHash = CredentialHasher.HashAuthenticationPhrase("PHRASE-USER"),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                IsActive = true
            };
        }

        private static ActivityLog BuildLog(int userId, string actionType, bool isSuccess, DateTime timestampUtc)
        {
            return new ActivityLog
            {
                UserId = userId,
                GroupId = RoleNames.User,
                ActionType = actionType,
                ActionInformation = actionType,
                IsSuccess = isSuccess,
                TimestampUtc = timestampUtc
            };
        }

        private static AuthSession BuildSession(int userId, string remoteIpAddress, DateTime createdAtUtc)
        {
            return new AuthSession
            {
                UserId = userId,
                TokenHash = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = createdAtUtc,
                LastActivityAtUtc = createdAtUtc.AddMinutes(1),
                ExpiresAtUtc = createdAtUtc.AddHours(12),
                ClientKey = "test-client",
                UserAgent = "UnitTest",
                RemoteIpAddress = remoteIpAddress
            };
        }
    }
}
