using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
            return searchTerm.IsNullOrEmpty() ? 
                await _context.Missions
                    .Include(m => m.Client)
                    .Include(m => m.Truck)
                    .AsQueryable().ToListAsync() : 
                await _context.Missions
                    .Include(m => m.Client)
                    .Include(m => m.Truck)
                    .Where(m => (m.Client.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                m.Client.Phone.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                m.Id.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                    .AsQueryable()
                    .ToListAsync();
        }

        public async Task<Mission?> GetMissionById(int missionId)
        {
            return await _context.Missions.FirstOrDefaultAsync(m => m.Id == missionId);
        }
    }
}
