using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Repositories.Implementations
{
    public class MissionsRepository : IMissionsRepository
    {
        private readonly ApplicationDbContext _context;

        public MissionsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Mission> AddMission(Mission mission)
        {
            _context.Missions.Add(mission);
            await _context.SaveChangesAsync();
            return mission;
        }

        public async Task DeleteMission(Mission mission)
        {
            _context.Missions.Remove(mission);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Mission>> GetAllMissions(string? searchTerm = null)
        {
            return await _context.Missions.ToListAsync();
        }

        public async Task<Mission?> GetMissionById(int missionId)
        {
            return await _context.Missions.FirstOrDefaultAsync(m => m.Id == missionId);
        }
    }
}
