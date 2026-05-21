using Microsoft.AspNetCore.Mvc;
using TdpTrans.DTOs;
using TdpTrans.Services;

namespace TdpTrans.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MissionsController : ControllerBase
    {
        private readonly IMissionsService _missionsService;

        public MissionsController(IMissionsService missionsService)
        {
            _missionsService = missionsService;
        }

        [HttpPost]
        public async Task<ActionResult<int>> AddMission([FromBody] CreateMissionRequest request)
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                var missionId = await _missionsService.AddMission(request, actorUserId);
                return Created(nameof(GetMissionById), missionId);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<MissionResponse>>> GetAllMissions()
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                var missions = await _missionsService.GetAllMissions(actorUserId);
                return Ok(missions);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResult>> GetMissionsPaginated([FromQuery] int page = 1, [FromQuery] int pageSize = 5, [FromQuery] string? search = null)
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                var paginatedResult = await _missionsService.GetMissionsPaginated(page, pageSize, search, actorUserId);
                return Ok(paginatedResult);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<MissionStatisticsDTO>> GetStatistics()
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                var statistics = await _missionsService.GetMissionStatistics(actorUserId);
                return Ok(statistics);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MissionResponse?>> GetMissionById([FromRoute] int id)
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                var mission = await _missionsService.GetMissionById(id, actorUserId);
                if (mission == null)
                {
                    return NotFound($"Mission with id {id} was not found");
                }

                return Ok(mission);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<int>> UpdateMissionById([FromRoute] int id, [FromBody] UpdateMissionRequest request)
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                var missionId = await _missionsService.UpdateMissionById(id, request, actorUserId);
                return Ok(missionId);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteMissionById([FromRoute] int id)
        {
            if (!ActorHeaderReader.TryRead(Request, out var actorUserId))
            {
                return Unauthorized("Este necesar un utilizator autentificat.");
            }

            try
            {
                await _missionsService.DeleteMissionById(id, actorUserId);
                return NoContent();
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return NotFound(exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return StatusCode(StatusCodes.Status403Forbidden, exception.Message);
            }
        }
    }
}
