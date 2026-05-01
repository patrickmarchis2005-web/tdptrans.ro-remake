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
            try
            {
                var missionId = await _missionsService.AddMission(request);
                return Created(nameof(GetMissionById), missionId);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MissionResponse>>> GetAllMissions()
        {
            var missions = await _missionsService.GetAllMissions();
            return Ok(missions);
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResult>> GetMissionsPaginated([FromQuery] int page, [FromQuery] int pageSize)
        {
            try
            {
                var paginatedResult = await _missionsService.GetMissionsPaginated(page, pageSize);
                return Ok(paginatedResult);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
        }

        [HttpGet]
        [Route("/{id:int}")]
        public async Task<ActionResult<MissionResponse?>> GetMissionById([FromRoute] int id)
        {
            var mission = await _missionsService.GetMissionById(id);
            if (mission == null)
                return NotFound($"Mission with id {id} was not found");
            return Ok(mission);
        }

        [HttpPut]
        [Route("/{id:int}")]
        public async Task<ActionResult<int>> UpdateMissionById([FromRoute] int id, [FromBody] UpdateMissionRequest request)
        {
            try
            {
                var missionId = await _missionsService.UpdateMissionById(id, request);
                return Ok(missionId);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
        }

        [HttpDelete]
        [Route("/{id:int}")]
        public async Task<ActionResult> DeleteMissionById([FromRoute] int id)
        {
            try
            {
                await _missionsService.DeleteMissionById(id);
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
        }
    }
}
