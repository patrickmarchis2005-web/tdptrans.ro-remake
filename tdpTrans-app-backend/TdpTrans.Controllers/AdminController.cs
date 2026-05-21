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
        private readonly ISuspiciousActivityService _suspiciousActivityService;
        private readonly IActivityLogService _activityLogService;

        public AdminController(
            IUserAccessService userAccessService,
            ISuspiciousActivityService suspiciousActivityService,
            IActivityLogService activityLogService)
        {
            _userAccessService = userAccessService;
            _suspiciousActivityService = suspiciousActivityService;
            _activityLogService = activityLogService;
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
                await _userAccessService.EnsurePermission(actorUserId, PermissionNames.ObservationsView, "Tried to view the observation list.");
                var observations = await _suspiciousActivityService.GetActiveObservations();
                await _activityLogService.Log(actorUserId, ActivityActionNames.ObservationsViewed, "Viewed the suspicious users observation list.");
                return Ok(observations);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
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
    }
}
