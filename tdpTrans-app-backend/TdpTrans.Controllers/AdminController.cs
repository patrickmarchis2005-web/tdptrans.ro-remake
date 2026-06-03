using Microsoft.AspNetCore.Mvc;
using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Services;

namespace TdpTrans.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IUserAccessService _userAccessService;
        private readonly IActivityLogService _activityLogService;
        private readonly ISuspiciousActivityService _suspiciousActivityService;
        private readonly ISecurityLabService _securityLabService;

        public AdminController(
            IUserAccessService userAccessService,
            IActivityLogService activityLogService,
            ISuspiciousActivityService suspiciousActivityService,
            ISecurityLabService securityLabService)
        {
            _userAccessService = userAccessService;
            _activityLogService = activityLogService;
            _suspiciousActivityService = suspiciousActivityService;
            _securityLabService = securityLabService;
        }

        [HttpGet("activity-logs")]
        public async Task<ActionResult<IReadOnlyList<ActivityLogResponse>>> GetActivityLogs([FromQuery] int take = 30)
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                await _userAccessService.EnsurePermission(actorUserId, PermissionNames.LogsView, "Tried to view the activity logs.");
                var logs = await _activityLogService.GetRecentLogs(Math.Clamp(take, 10, 100));
                await _activityLogService.Log(actorUserId, ActivityActionNames.LogsViewed, "Viewed the recent activity logs.");
                return Ok(logs);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }

        [HttpGet("observations")]
        public async Task<ActionResult<IReadOnlyList<ObservationResponse>>> GetObservations()
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                await _userAccessService.EnsurePermission(actorUserId, PermissionNames.ObservationsView, "Tried to view suspicious-user observations.");
                var observations = await _suspiciousActivityService.GetActiveObservations();
                await _activityLogService.Log(actorUserId, ActivityActionNames.ObservationsViewed, "Viewed suspicious-user observations.");
                return Ok(observations);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }

        [HttpGet("security-statistics")]
        public async Task<ActionResult<SecurityStatisticsResponse>> GetSecurityStatistics(
            [FromQuery] string mode = "optimized",
            [FromQuery] int lookbackHours = 24)
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                await _userAccessService.EnsurePermission(actorUserId, PermissionNames.SecurityLabManage, "Tried to view the security benchmark.");
                var statistics = await _securityLabService.GetSecurityStatistics(mode, lookbackHours);
                await _activityLogService.Log(actorUserId, ActivityActionNames.SecurityStatisticsViewed, $"Viewed the {statistics.Mode} security benchmark.");
                return Ok(statistics);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }

        [HttpPost("security-seed")]
        public async Task<ActionResult<SecuritySeedResponse>> SeedSecurityLab([FromBody] SecuritySeedRequest? request)
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                await _userAccessService.EnsurePermission(actorUserId, PermissionNames.SecurityLabManage, "Tried to generate security benchmarking data.");
                var response = await _securityLabService.GenerateSecuritySeed(request ?? new SecuritySeedRequest());
                await _activityLogService.Log(
                    actorUserId,
                    ActivityActionNames.SecuritySeedGenerated,
                    $"Generated {response.CreatedUsers} users, {response.CreatedMissions} missions and {response.CreatedActivityLogs} activity logs for the security lab.");
                return Ok(response);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }
    }
}
